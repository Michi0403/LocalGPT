using System.Globalization;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Checks the stored default Council power. A low value shrinks every participant's output and context on the
/// automatic hardware road (observed: truncated stories, empty answers and cut tool lists at 30%).
/// </summary>
/// <param name="dbContextFactory">LocalGPT database read for the stored row.</param>
/// <param name="systemVariables">Definitions of the built-in default and the legacy default value.</param>
/// <param name="logger">Logger for check diagnostics.</param>
public sealed class CouncilLoadDefaultHealthCheck(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ISystemVariableDefinitionService systemVariables,
    ILogger<CouncilLoadDefaultHealthCheck> logger) : IConfigurationHealthCheck
{
    /// <inheritdoc />
    public string CheckKey => "council-load-default";

    /// <inheritdoc />
    public string Category => "council";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationFinding>> InspectAsync(ConfigurationStewardPolicy policy, CancellationToken cancellationToken = default)
    {
        try
        {
            var name = systemVariables.DefaultCouncilResourceLoadPercent.Name;
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var dbDisposal = db.ConfigureAwait(false);
            var row = await db.SystemVariables.AsNoTracking().SingleOrDefaultAsync(item => item.Name == name, cancellationToken).ConfigureAwait(false);
            if (row is null || !int.TryParse(row.ValueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stored))
                return []; // A missing or malformed row is reported by the runtime value integrity check.
            if (stored >= policy.MinimumRecommendedCouncilLoadPercent)
                return [];

            var (output, context) = Interpolate(policy, stored);
            var (recommendedOutput, recommendedContext) = Interpolate(policy, policy.RecommendedCouncilLoadPercent);
            var finding = new ConfigurationFinding
            {
                Key = CheckKey,
                CheckKey = CheckKey,
                Category = Category,
                Severity = "Warning",
                Title = $"Default Council power is {stored}%",
                Summary = $"New Council runs start at {stored}% of each hardware road. On the automatic road a participant then gets about {output} output and {context} context tokens; at {policy.RecommendedCouncilLoadPercent}% it gets about {recommendedOutput} and {recommendedContext}. Small budgets truncate answers of thinking models and cut long prompts with tool lists.",
                VerificationNote = "The stored value may be intentional, for example to keep GPU memory free for other work. LocalGPT cannot see why it was set. Chat presets and the power slider can still choose another value for single runs; 1-Wire callers can send their own value.",
                RecommendedOptionKey = "set-recommended",
                Evidence =
                [
                    new() { Source = $"SystemVariables.{name}", ObservedValue = row.ValueString, Reference = $"built-in default {systemVariables.DefaultCouncilResourceLoadPercent.DefaultValue}", Note = $"Row last updated {row.LastUpdated:u}." },
                    new() { Source = "Automatic hardware road (reference values of ConfigurationStewardPolicyJson)", ObservedValue = $"{output} output / {context} context tokens at {stored}%", Reference = $"{recommendedOutput} output / {recommendedContext} context tokens at {policy.RecommendedCouncilLoadPercent}%", Note = "Custom hardware roads interpolate their own minimum and maximum the same way." }
                ],
                Options =
                [
                    Option("set-recommended", $"Set default Council power to {policy.RecommendedCouncilLoadPercent}%",
                        "New runs use the full road budget. Uses more GPU memory per participant.", name, row.ValueString, policy.RecommendedCouncilLoadPercent),
                    Option("set-alternative", $"Set default Council power to {policy.AlternativeCouncilLoadPercent}%",
                        "A middle ground that keeps some GPU memory free.", name, row.ValueString, policy.AlternativeCouncilLoadPercent),
                    new() { Key = "keep", Label = $"Keep {stored}%", Description = "Nothing changes; the finding stays visible in later diagnoses." }
                ]
            };
            if (systemVariables.LegacyCouncilResourceLoadPercent == stored)
                finding.Evidence.Add(new()
                {
                    Source = "Release history",
                    ObservedValue = stored.ToString(CultureInfo.InvariantCulture),
                    Reference = $"legacy built-in default {systemVariables.LegacyCouncilResourceLoadPercent}",
                    Note = "The value equals an older built-in default, so it may never have been chosen deliberately."
                });
            logger.LogInformation("Configuration check {CheckKey} found default Council power {Stored}% (< {Minimum}%).", CheckKey, stored, policy.MinimumRecommendedCouncilLoadPercent);
            return [finding];
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Configuration check {CheckKey} was canceled.", CheckKey);
            else
                logger.LogError(exception, "Configuration check {CheckKey} failed.", CheckKey);
            throw;
        }
    }

    /// <summary>Interpolates the reference automatic road at a load percentage, like the hardware road planner.</summary>
    /// <param name="policy">Steward policy with the reference road values.</param>
    /// <param name="loadPercent">Load percentage.</param>
    /// <returns>Output and context tokens.</returns>
    private (int Output, int Context) Interpolate(ConfigurationStewardPolicy policy, int loadPercent)
    {
        try
        {
            var fraction = Math.Clamp(loadPercent, 0, 100) / 100d;
            var output = policy.ReferenceAutoRoadMinimumOutputTokens + (int)Math.Round((policy.ReferenceAutoRoadMaximumOutputTokens - policy.ReferenceAutoRoadMinimumOutputTokens) * fraction);
            var context = policy.ReferenceAutoRoadMinimumContextTokens + (int)Math.Round((policy.ReferenceAutoRoadMaximumContextTokens - policy.ReferenceAutoRoadMinimumContextTokens) * fraction);
            return (output, context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not interpolate the reference automatic road at {LoadPercent}%.", loadPercent);
            throw;
        }
    }

    /// <summary>Creates a set-value option for the default Council power row.</summary>
    /// <param name="key">Option key.</param>
    /// <param name="label">Selectable label.</param>
    /// <param name="description">Effect of the option.</param>
    /// <param name="rowName">SystemVariables row name.</param>
    /// <param name="observed">Observed stored value.</param>
    /// <param name="newValue">New load percentage.</param>
    /// <returns>The repair option.</returns>
    private ConfigurationRepairOption Option(string key, string label, string description, string rowName, string observed, int newValue)
    {
        try
        {
            return new()
            {
                Key = key,
                Label = label,
                Description = description,
                Changes = [new() { TargetKind = ConfigurationRepairChange.SystemVariableKind, TargetKey = rowName, ExpectedCurrentValue = observed, NewValue = newValue.ToString(CultureInfo.InvariantCulture) }]
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not build configuration repair option {OptionKey}.", key);
            throw;
        }
    }
}
