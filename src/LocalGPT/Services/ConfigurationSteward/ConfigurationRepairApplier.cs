using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Applies a human-approved configuration repair option. Every change first re-reads the stored value and compares it
/// with the value observed by the finding; one mismatch rolls the whole option back. After a commit the runtime policy
/// snapshot is reloaded so running services see the repaired values.
/// </summary>
/// <param name="dbContextFactory">LocalGPT database used for the compare-and-set transaction.</param>
/// <param name="runtimePolicy">Runtime policy snapshot reloaded after a committed repair.</param>
/// <param name="logger">Logger for repair diagnostics; changed values are logged because they are configuration, not secrets.</param>
public sealed class ConfigurationRepairApplier(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ConfigurationRepairApplier> logger) : IConfigurationRepairApplier
{
    /// <inheritdoc />
    public async Task<ConfigurationRepairOutcome> ApplyAsync(ConfigurationFinding finding, ConfigurationRepairOption option, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(finding);
            ArgumentNullException.ThrowIfNull(option);
            var outcome = new ConfigurationRepairOutcome { FindingKey = finding.Key, SelectedOptionKey = option.Key };
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var dbDisposal = db.ConfigureAwait(false);
            var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var transactionDisposal = transaction.ConfigureAwait(false);
            foreach (var change in option.Changes)
            {
                var row = await db.SystemVariables.SingleOrDefaultAsync(item => item.Name == change.TargetKey, cancellationToken).ConfigureAwait(false);
                if (row is null || !string.Equals(row.ValueString, change.ExpectedCurrentValue, StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    logger.LogWarning(
                        "Configuration repair {FindingKey}/{OptionKey} was not applied: {Target} changed after the finding (row {RowState}). Run the diagnosis again.",
                        finding.Key, option.Key, change.TargetKey, row is null ? "missing" : "value differs");
                    outcome.Status = ConfigurationRepairOutcome.StateChanged;
                    outcome.Message = $"{change.TargetKey} changed after the diagnosis; nothing was changed. Run the diagnosis again and decide on the current value.";
                    outcome.AppliedChanges.Clear();
                    return outcome;
                }

                var newValue = change.TargetKind switch
                {
                    ConfigurationRepairChange.SystemVariableKind => change.NewValue,
                    ConfigurationRepairChange.RuntimeCollectionAddKind => AddCollectionItem(row.ValueString, change.NewValue),
                    _ => throw new InvalidOperationException($"Unsupported configuration change kind '{change.TargetKind}'.")
                };
                outcome.AppliedChanges.Add($"{change.TargetKey}: {Shorten(row.ValueString)} → {Shorten(newValue)}");
                row.ValueString = newValue;
                row.LastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            try { runtimePolicy.Reload(); }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Configuration repair {FindingKey} was committed but the runtime policy snapshot could not be reloaded; restart LocalGPT to load it.", finding.Key);
            }
            logger.LogInformation("Applied approved configuration repair {FindingKey}/{OptionKey}: {Changes}.",
                finding.Key, option.Key, string.Join("; ", outcome.AppliedChanges));
            outcome.Status = ConfigurationRepairOutcome.Applied;
            outcome.Message = $"Applied \"{option.Label}\".";
            return outcome;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Configuration repair {FindingKey} was canceled before commit.", finding?.Key);
            else
                logger.LogError(exception, "Configuration repair {FindingKey}/{OptionKey} failed; the transaction was not committed.", finding?.Key, option?.Key);
            throw;
        }
    }

    /// <summary>Adds one item to a runtime collection stored as a JSON string array (no duplicate).</summary>
    /// <param name="storedJson">Stored JSON array.</param>
    /// <param name="item">Item to add.</param>
    /// <returns>The updated JSON array.</returns>
    private string AddCollectionItem(string storedJson, string item)
    {
        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(storedJson) ?? [];
            if (!values.Contains(item, StringComparer.OrdinalIgnoreCase))
                values.Add(item);
            return JsonSerializer.Serialize(values);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not add an item to a runtime collection; the stored value is not a JSON string array.");
            throw;
        }
    }

    /// <summary>Shortens a value for the change log.</summary>
    /// <param name="value">Value to shorten.</param>
    /// <returns>At most 120 characters.</returns>
    private string Shorten(string value)
    {
        try
        {
            return value.Length <= 120 ? value : value[..117] + "...";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not shorten a configuration value for the change log.");
            throw;
        }
    }
}
