using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

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

            var valueMap = seed.Values.ToDictionary(item => item.Key, item => ReadRequired(item.Name));
            var collectionMap = seed.Collections.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<string>)(JsonSerializer.Deserialize<string[]>(ReadRequired(item.Name))
                    ?? throw new InvalidDataException($"Runtime-policy collection '{item.Name}' is not a JSON string array.")));
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
            logger.LogInformation($"Loaded {valueMap.Count} values, {collectionMap.Count} collections and {regexMap.Count} regexes from the LocalGPT database.");
            return new LocalGptRuntimePolicyDefinition { Values = valueMap, Collections = collectionMap, RegexPatterns = regexMap };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not load the LocalGPT runtime policy: {exception.Message}");
            throw;
        }
    }
}
