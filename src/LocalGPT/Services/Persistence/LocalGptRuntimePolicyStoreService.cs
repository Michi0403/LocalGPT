using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates LocalGPT runtime policy store behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the LocalGPT runtime policy store workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the LocalGPT runtime policy store workflow to provide the corresponding application capability.</param>
/// <param name="seedData">Local gpt runtime policy seed data service dependency used by the LocalGPT runtime policy store workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class LocalGptRuntimePolicyStoreService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILocalGptRuntimePolicySeedDataService seedData,
    ILogger<LocalGptRuntimePolicyStoreService> logger) : ILocalGptRuntimePolicyStoreService
{
    /// <summary>
    /// Retrieves definition as part of the LocalGPT runtime policy store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy definition produced by the operation.</returns>
    public LocalGptRuntimePolicyDefinition? GetDefinition()
    {
        try
        {
            databaseInitializer.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var db = dbContextFactory.CreateDbContext();
            var seed = seedData.GetSeed();
            var variableNames = seed.SystemVariables.Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var variables = db.SystemVariables.AsNoTracking()
                .Where(item => variableNames.Contains(item.Name))
                .ToDictionary(item => item.Name, item => item.ValueString, StringComparer.OrdinalIgnoreCase);
            var regexNames = seed.RegexPatterns.Select(item => item.Name).ToArray();
            var regexRows = db.RegexPatterns.AsNoTracking()
                .Where(item => regexNames.Contains(item.Name))
                .ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);

            string ReadRequired(string name)
            {
                try
                {
                    if (!variables.TryGetValue(name, out var value) || value is null)
                        throw new InvalidDataException($"Required runtime-policy value '{name}' is missing.");
                    logger.LogTrace($"Read runtime-policy value {name}.");
                    return value;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"Could not read runtime-policy value {name}: {exception.Message}");
                    throw;
                }
            }

            IReadOnlyList<string> ReadCollection(LocalGptRuntimeCollectionSeed item)
            {
                var persistedValue = ReadRequired(item.Name);
                try
                {
                    var values = JsonSerializer.Deserialize<string[]>(persistedValue);
                    if (values is not null)
                        return values;

                    logger.LogWarning(
                        $"Runtime-policy collection {item.Name} contained JSON null. Seed defaults are used for this load.");
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(
                        exception,
                        $"Runtime-policy collection {item.Name} contained legacy or malformed data. Seed defaults are used for this load.");
                }

                return item.Values.ToArray();
            }

            var valueMap = seed.Values.ToDictionary(item => item.Key, item => ReadRequired(item.Name));
            var collectionMap = seed.Collections.ToDictionary(item => item.Key, ReadCollection);
            var regexMap = seed.RegexPatterns.ToDictionary(
                item => item.Key,
                item =>
                {
                    try
                    {
                        if (!regexRows.TryGetValue(item.Name, out var row) || string.IsNullOrWhiteSpace(row.Pattern))
                            throw new InvalidDataException($"Required runtime-policy regex '{item.Name}' is missing.");
                        logger.LogTrace($"Read runtime-policy regex {item.Name}.");
                        return new LocalGptRuntimeRegexDefinition
                        {
                            Key = item.Key,
                            Name = row.Name,
                            Pattern = row.Pattern,
                            Flags = row.Flags ?? string.Empty,
                            UpdatedOn = row.UpdatedOn
                        };
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, $"Could not read runtime-policy regex {item.Name}: {exception.Message}");
                        throw;
                    }
                });
            logger.LogInformation(
                $"Loaded {valueMap.Count} values, {collectionMap.Count} collections and {regexMap.Count} regexes from the LocalGPT database.");
            return new LocalGptRuntimePolicyDefinition { Values = valueMap, Collections = collectionMap, RegexPatterns = regexMap };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not load the LocalGPT runtime policy: {exception.Message}");
            return null;
        }
    }
}
