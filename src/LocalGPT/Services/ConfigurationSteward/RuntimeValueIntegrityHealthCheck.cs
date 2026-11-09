using System.Globalization;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Checks that typed SystemVariables rows can still be read as their declared type: numbers, booleans, GUIDs,
/// JSON documents and JSON string-array collections. An unreadable row makes the service that needs it fail at
/// runtime; the repair resets it to the shipped seed value.
/// </summary>
/// <param name="dbContextFactory">LocalGPT database read for the stored rows.</param>
/// <param name="catalog">Shipped seed values used as the reset target.</param>
/// <param name="logger">Logger for check diagnostics.</param>
public sealed class RuntimeValueIntegrityHealthCheck(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IInitialDataCatalog catalog,
    ILogger<RuntimeValueIntegrityHealthCheck> logger) : IConfigurationHealthCheck
{
    /// <inheritdoc />
    public string CheckKey => "runtime-value-integrity";

    /// <inheritdoc />
    public string Category => "runtime-values";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationFinding>> InspectAsync(ConfigurationStewardPolicy policy, CancellationToken cancellationToken = default)
    {
        try
        {
            var seeds = catalog.Variables
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var dbDisposal = db.ConfigureAwait(false);
            var rows = await db.SystemVariables.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            var findings = new List<ConfigurationFinding>();
            foreach (var row in rows.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                    continue;
                var problem = Validate(row.Name, row.ValueString ?? string.Empty, row.DataType ?? string.Empty);
                if (problem is null)
                    continue;
                seeds.TryGetValue(row.Name, out var seed);
                var seedUsable = seed is not null && Validate(row.Name, seed.Value, row.DataType ?? seed.DataType) is null;
                var finding = new ConfigurationFinding
                {
                    Key = $"{CheckKey}:{row.Name}",
                    CheckKey = CheckKey,
                    Category = Category,
                    Severity = "Error",
                    Title = $"Runtime value {row.Name} cannot be read as {row.DataType}",
                    Summary = $"{problem} Services that read {row.Name} fail or fall back to built-in defaults until it is repaired.",
                    VerificationNote = "The check proves only that the stored text does not parse as its declared type. It cannot tell which value was intended; resetting restores the value shipped with this LocalGPT version.",
                    Evidence =
                    [
                        new()
                        {
                            Source = $"SystemVariables.{row.Name}",
                            ObservedValue = Describe(row.Name, row.ValueString ?? string.Empty, row.DataType ?? string.Empty),
                            Reference = $"declared type {row.DataType}",
                            Note = $"Row last updated {row.LastUpdated:u}."
                        }
                    ],
                    Options = seedUsable
                        ?
                        [
                            new()
                            {
                                Key = "reset-to-seed",
                                Label = "Reset to the shipped value",
                                Description = $"Stores the value shipped with this version ({Describe(row.Name, seed!.Value, seed.DataType)}).",
                                Changes = [new() { TargetKind = ConfigurationRepairChange.SystemVariableKind, TargetKey = row.Name, ExpectedCurrentValue = row.ValueString ?? string.Empty, NewValue = seed.Value }]
                            },
                            new() { Key = "keep", Label = "Keep the stored value", Description = "Nothing changes; fix the value yourself in Settings or the database editor." }
                        ]
                        : [],
                    RecommendedOptionKey = seedUsable ? "reset-to-seed" : string.Empty
                };
                findings.Add(finding);
                if (findings.Count >= Math.Max(1, policy.MaximumFindings))
                    break;
            }
            if (findings.Count > 0)
                logger.LogWarning("Configuration check {CheckKey} found {Count} unreadable runtime value(s): {Names}.",
                    CheckKey, findings.Count, string.Join(", ", findings.Select(item => item.Key)));
            return findings;
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

    /// <summary>Validates a stored value against its declared type.</summary>
    /// <param name="name">Row name; names ending in "Json" are validated as JSON documents.</param>
    /// <param name="value">Stored value.</param>
    /// <param name="dataType">Declared data type.</param>
    /// <returns>A problem description, or <see langword="null"/> when the value is readable.</returns>
    private string? Validate(string name, string value, string dataType)
    {
        try
        {
            switch (dataType)
            {
                case "System.Int32":
                    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? null : "The stored text is not a whole number.";
                case "System.Int64":
                    return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? null : "The stored text is not a whole number.";
                case "System.Boolean":
                    return bool.TryParse(value, out _) ? null : "The stored text is not true or false.";
                case "System.Guid":
                    return Guid.TryParse(value, out _) ? null : "The stored text is not a GUID.";
                case "System.Double":
                    return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? null : "The stored text is not a number.";
                case LocalGptRuntimePolicySeedModel.CollectionDataType:
                    return JsonProblem(value, JsonValueKind.Array);
                case "System.String" when name.EndsWith("Json", StringComparison.OrdinalIgnoreCase):
                    return JsonProblem(value, null);
                default:
                    return null;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not validate runtime value {Name}.", name);
            throw;
        }
    }

    /// <summary>Checks that a value is valid JSON of the expected root kind.</summary>
    /// <param name="value">Stored JSON text.</param>
    /// <param name="expectedKind">Required root kind, or <see langword="null"/> for object or array.</param>
    /// <returns>A problem description, or <see langword="null"/> when valid.</returns>
    private string? JsonProblem(string value, JsonValueKind? expectedKind)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            var kind = document.RootElement.ValueKind;
            if (expectedKind is not null && kind != expectedKind)
                return $"The stored JSON is a {kind}, not a {expectedKind}.";
            return kind is JsonValueKind.Object or JsonValueKind.Array ? null : $"The stored JSON is a {kind}, not an object or array.";
        }
        catch (JsonException exception)
        {
            logger.LogDebug(exception, "A stored runtime JSON value failed validation.");
            return $"The stored JSON is invalid near line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1}.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not validate a stored runtime JSON value.");
            throw;
        }
    }

    /// <summary>Describes a value for evidence without exposing JSON documents or secret-like values.</summary>
    /// <param name="name">Row name.</param>
    /// <param name="value">Stored value.</param>
    /// <param name="dataType">Declared data type.</param>
    /// <returns>A safe description.</returns>
    private string Describe(string name, string value, string dataType)
    {
        try
        {
            var sensitive = new[] { "password", "secret", "token", "apikey", "api-key", "key", "connection" }
                .Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
            if (sensitive || dataType == LocalGptRuntimePolicySeedModel.CollectionDataType || name.EndsWith("Json", StringComparison.OrdinalIgnoreCase))
                return $"<{value.Length} characters, content not shown>";
            return value.Length <= 80 ? value : value[..77] + "...";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not describe runtime value {Name}.", name);
            throw;
        }
    }
}
