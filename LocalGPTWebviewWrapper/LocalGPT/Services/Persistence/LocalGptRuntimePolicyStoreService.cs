using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Database boundary for LocalGPT runtime policy. Executable names, identifiers,
/// timeouts, pattern text and pattern flags are loaded from seeded, user-maintainable rows.
/// </summary>
public sealed class LocalGptRuntimePolicyStoreService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILocalGptRuntimePolicySeedDataService seedData,
    ILogger<LocalGptRuntimePolicyStoreService> logger) : ILocalGptRuntimePolicyStoreService
{
    public LocalGptRuntimePolicyDefinition GetDefinition()
    {
        try
        {
            databaseInitializer.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var db = dbContextFactory.CreateDbContext();
            var seed = seedData.GetSeed();
            var requiredVariableNames = new[]
            {
                seed.LocalGptCoreProjectIdVariableName,
                seed.AllowedNativeExecutablesVariableName,
                seed.RegexTimeoutVariableName
            };
            var variables = db.SystemVariables.AsNoTracking()
                .Where(item => requiredVariableNames.Contains(item.Name))
                .ToDictionary(item => item.Name, item => item.ValueString, StringComparer.OrdinalIgnoreCase);
            var requiredRegexNames = new[]
            {
                seed.PowerShellInlineCommandRegexName,
                seed.PowerShellFileRegexName,
                seed.SensitiveArgumentRegexName
            };
            var regexRows = db.RegexPatterns.AsNoTracking()
                .Where(item => requiredRegexNames.Contains(item.Name))
                .ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);

            string ReadRequiredVariable(string name)
            {
                if (!variables.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException($"Required runtime-policy system variable '{name}' is missing or empty.");
                return value;
            }

            LocalGptRuntimeRegexDefinition ReadRequiredRegex(string name)
            {
                if (!regexRows.TryGetValue(name, out var row) || string.IsNullOrWhiteSpace(row.Pattern))
                    throw new InvalidDataException($"Required runtime-policy regex '{name}' is missing or empty.");
                return new LocalGptRuntimeRegexDefinition
                {
                    Name = row.Name,
                    Pattern = row.Pattern,
                    Flags = row.Flags ?? string.Empty,
                    UpdatedOn = row.UpdatedOn
                };
            }

            var projectIdText = ReadRequiredVariable(seed.LocalGptCoreProjectIdVariableName);
            if (!Guid.TryParse(projectIdText, out var projectId))
                throw new InvalidDataException($"Runtime-policy system variable '{seed.LocalGptCoreProjectIdVariableName}' does not contain a valid GUID.");

            var timeoutText = ReadRequiredVariable(seed.RegexTimeoutVariableName);
            if (!int.TryParse(timeoutText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutMilliseconds) || timeoutMilliseconds <= 0)
                throw new InvalidDataException($"Runtime-policy system variable '{seed.RegexTimeoutVariableName}' must contain a positive integer.");

            var executableJson = ReadRequiredVariable(seed.AllowedNativeExecutablesVariableName);
            var executableValues = JsonSerializer.Deserialize<string[]>(executableJson)
                ?? throw new InvalidDataException($"Runtime-policy system variable '{seed.AllowedNativeExecutablesVariableName}' does not contain a JSON string array.");
            var executables = executableValues
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (executables.Length == 0)
                throw new InvalidDataException($"Runtime-policy system variable '{seed.AllowedNativeExecutablesVariableName}' contains no executable names.");

            var definition = new LocalGptRuntimePolicyDefinition
            {
                LocalGptCoreProjectId = projectId,
                RegexTimeoutMilliseconds = timeoutMilliseconds,
                AllowedNativeExecutables = executables,
                PowerShellInlineCommandPattern = ReadRequiredRegex(seed.PowerShellInlineCommandRegexName),
                PowerShellFilePattern = ReadRequiredRegex(seed.PowerShellFileRegexName),
                SensitiveArgumentPattern = ReadRequiredRegex(seed.SensitiveArgumentRegexName)
            };
            logger.LogDebug($"Loaded the LocalGPT runtime policy from database rows with {definition.AllowedNativeExecutables.Count} native executable entries and {requiredRegexNames.Length} regex definitions; pattern content was omitted from logs.");
            return definition;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not load the LocalGPT runtime policy from database rows: {exception.Message}");
            throw;
        }
    }
}
