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
    /// Loads the runtime policy as the database defines it: every SystemVariables row is a value (rows typed
    /// <see cref="LocalGptRuntimePolicySeedModel.CollectionDataType"/> are also collections) and every
    /// RegexPatterns row is a pattern, all keyed by their names. Keys the seed provides must exist.
    /// </summary>
    /// <returns>The database-defined policy, or <see langword="null"/> when it cannot be loaded.</returns>
    public LocalGptRuntimePolicyDefinition? GetDefinition()
    {
        try
        {
            databaseInitializer.InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var db = dbContextFactory.CreateDbContext();
            var seed = seedData.GetSeed();
            var rows = db.SystemVariables.AsNoTracking().ToList();
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var collections = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Name) || row.ValueString is null)
                    continue;
                values[row.Name] = row.ValueString;
                if (string.Equals(row.DataType, LocalGptRuntimePolicySeedModel.CollectionDataType, StringComparison.OrdinalIgnoreCase))
                    collections[row.Name] = ReadCollection(row.Name, row.ValueString, seed);
            }

            foreach (var item in seed.Values)
                RequireValue(values, item.Name);
            foreach (var item in seed.Collections)
            {
                if (!collections.ContainsKey(item.Name))
                    collections[item.Name] = ReadCollection(item.Name, RequireValue(values, item.Name), seed);
            }

            var patterns = new Dictionary<string, LocalGptRuntimeRegexDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in db.RegexPatterns.AsNoTracking().ToList())
            {
                if (string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.Pattern))
                    continue;
                patterns[row.Name] = new LocalGptRuntimeRegexDefinition
                {
                    Name = row.Name,
                    Pattern = row.Pattern,
                    Flags = row.Flags ?? string.Empty,
                    UpdatedOn = row.UpdatedOn
                };
            }

            foreach (var item in seed.RegexPatterns)
            {
                if (!patterns.ContainsKey(item.Name))
                    throw new InvalidDataException($"Required runtime-policy regex '{item.Name}' is missing.");
            }

            logger.LogInformation(
                $"Loaded {values.Count} values, {collections.Count} collections and {patterns.Count} regexes from the LocalGPT database.");
            return new LocalGptRuntimePolicyDefinition { Values = values, Collections = collections, RegexPatterns = patterns };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not load the LocalGPT runtime policy: {exception.Message}");
            return null;
        }
    }

    /// <summary>Returns a value that the seed requires, or throws when the database row is missing.</summary>
    /// <param name="values">Values loaded from SystemVariables.</param>
    /// <param name="name">Database name of the required value.</param>
    /// <returns>The stored value.</returns>
    private string RequireValue(IReadOnlyDictionary<string, string> values, string name)
    {
        try
        {
            if (!values.TryGetValue(name, out var value))
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

    /// <summary>
    /// Parses a runtime collection stored as a JSON string array. Legacy or malformed data falls back to the seed
    /// defaults of that collection (or to an empty collection for rows the seed does not know).
    /// </summary>
    /// <param name="name">Database name of the collection.</param>
    /// <param name="persistedValue">Stored JSON value.</param>
    /// <param name="seed">Authoritative seed with the default members.</param>
    /// <returns>The collection members.</returns>
    private IReadOnlyList<string> ReadCollection(string name, string persistedValue, LocalGptRuntimePolicySeedModel seed)
    {
        var defaults = seed.Collections.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))?.Values.ToArray() ?? [];
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(persistedValue);
            if (values is not null)
                return values;

            logger.LogWarning($"Runtime-policy collection {name} contained JSON null. Seed defaults are used for this load.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception,
                $"Runtime-policy collection {name} contained legacy or malformed data. Seed defaults are used for this load.");
        }

        return defaults;
    }
}
