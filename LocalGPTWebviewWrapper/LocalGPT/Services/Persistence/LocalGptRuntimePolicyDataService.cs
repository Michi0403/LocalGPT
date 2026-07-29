using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

public sealed class LocalGptRuntimePolicyDataService : ILocalGptRuntimePolicyDataService
{
    private readonly ILocalGptRuntimePolicyStoreService store;
    private readonly ILogger<LocalGptRuntimePolicyDataService> logger;
    private readonly System.Threading.Lock sync = new();
    private FrozenDictionary<LocalGptRuntimeValue, string> values = null!;
    private FrozenDictionary<LocalGptRuntimeCollection, FrozenSet<string>> collections = null!;
    private FrozenDictionary<LocalGptRuntimePattern, Regex> patterns = null!;

    public LocalGptRuntimePolicyDataService(ILocalGptRuntimePolicyStoreService store, ILogger<LocalGptRuntimePolicyDataService> logger)
    {
        this.store = store;
        this.logger = logger;
        try
        {
            Reload();
            logger.LogInformation($"Initialized the database-backed LocalGPT runtime policy service.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not initialize the LocalGPT runtime policy service: {exception.Message}");
            throw;
        }
    }

    public Guid LocalGptCoreProjectId => GetGuid(LocalGptRuntimeValue.LocalGptCoreProjectId);
    public TimeSpan RegexTimeout => TimeSpan.FromMilliseconds(GetInt(LocalGptRuntimeValue.RegexTimeoutMilliseconds));
    public FrozenSet<string> AllowedNativeExecutables => GetCollection(LocalGptRuntimeCollection.AllowedNativeExecutables);
    public Regex PowerShellInlineCommandPattern => GetPattern(LocalGptRuntimePattern.PowerShellInlineCommand);
    public Regex PowerShellFilePattern => GetPattern(LocalGptRuntimePattern.PowerShellFile);
    public Regex SensitiveArgumentPattern => GetPattern(LocalGptRuntimePattern.SensitiveArgument);

    public string GetString(LocalGptRuntimeValue key)
    {
        try
        {
            lock (sync)
            {
                if (!values.TryGetValue(key, out var value)) throw new KeyNotFoundException($"Runtime value '{key}' is not loaded.");
                logger.LogTrace($"Resolved LocalGPT runtime value {key}.");
                return value;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve LocalGPT runtime value {key}: {exception.Message}");
            throw;
        }
    }

    public int GetInt(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) throw new InvalidDataException($"Runtime value '{key}' is not a valid Int32.");
            logger.LogTrace($"Parsed LocalGPT runtime integer {key}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse LocalGPT runtime integer {key}: {exception.Message}");
            throw;
        }
    }

    public long GetLong(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) throw new InvalidDataException($"Runtime value '{key}' is not a valid Int64.");
            logger.LogTrace($"Parsed LocalGPT runtime long {key}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse LocalGPT runtime long {key}: {exception.Message}");
            throw;
        }
    }

    public Guid GetGuid(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            if (!Guid.TryParse(raw, out var value)) throw new InvalidDataException($"Runtime value '{key}' is not a valid Guid.");
            logger.LogTrace($"Parsed LocalGPT runtime GUID {key}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse LocalGPT runtime GUID {key}: {exception.Message}");
            throw;
        }
    }

    public T GetJson<T>(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            var jsonOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var value = System.Text.Json.JsonSerializer.Deserialize<T>(raw, jsonOptions)
                ?? throw new InvalidDataException($"Runtime value '{key}' could not be deserialized as {typeof(T).Name}.");
            logger.LogTrace($"Deserialized LocalGPT runtime document {key} as {typeof(T).Name}.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not deserialize LocalGPT runtime document {key} as {typeof(T).Name}: {exception.Message}");
            throw;
        }
    }

    public Regex GetPattern(LocalGptRuntimePattern key)
    {
        try
        {
            lock (sync)
            {
                if (!patterns.TryGetValue(key, out var pattern)) throw new KeyNotFoundException($"Runtime pattern '{key}' is not loaded.");
                logger.LogTrace($"Resolved LocalGPT runtime regex {key}.");
                return pattern;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve LocalGPT runtime regex {key}: {exception.Message}");
            throw;
        }
    }

    public FrozenSet<string> GetCollection(LocalGptRuntimeCollection key)
    {
        try
        {
            lock (sync)
            {
                if (!collections.TryGetValue(key, out var collection)) throw new KeyNotFoundException($"Runtime collection '{key}' is not loaded.");
                logger.LogTrace($"Resolved LocalGPT runtime collection {key} with {collection.Count} entries.");
                return collection;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve LocalGPT runtime collection {key}: {exception.Message}");
            throw;
        }
    }

    public LocalGptRuntimePolicySnapshot Reload()
    {
        try
        {
            var definition = store.GetDefinition();
            var timeoutRaw = definition.Values[LocalGptRuntimeValue.RegexTimeoutMilliseconds];
            if (!int.TryParse(timeoutRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutMilliseconds) || timeoutMilliseconds <= 0)
                throw new InvalidDataException($"RegexTimeoutMilliseconds must be positive.");
            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            var compiled = definition.RegexPatterns.ToDictionary(item => item.Key, item => new Regex(item.Value.Pattern, ParseFlags(item.Value), timeout)).ToFrozenDictionary();
            var frozenCollections = definition.Collections.ToDictionary(item => item.Key, item => item.Value.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToFrozenSet(StringComparer.OrdinalIgnoreCase)).ToFrozenDictionary();
            lock (sync)
            {
                values = definition.Values.ToFrozenDictionary();
                collections = frozenCollections;
                patterns = compiled;
            }
            var snapshot = GetSnapshot();
            logger.LogInformation($"Reloaded {snapshot.Values.Count} values, {snapshot.Collections.Count} collections and {snapshot.RegexPatterns.Count} regexes from the LocalGPT database.");
            return snapshot;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not reload LocalGPT runtime policy data: {exception.Message}");
            throw;
        }
    }

    public LocalGptRuntimePolicySnapshot GetSnapshot()
    {
        try
        {
            lock (sync)
            {
                var snapshot = new LocalGptRuntimePolicySnapshot
                {
                    Values = values.ToDictionary(item => item.Key.ToString(), item => item.Value),
                    Collections = collections.ToDictionary(item => item.Key.ToString(), item => (IReadOnlyList<string>)item.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()),
                    RegexPatterns = patterns.ToDictionary(item => item.Key.ToString(), item => item.Value.ToString())
                };
                logger.LogTrace($"Returned a LocalGPT runtime-policy snapshot.");
                return snapshot;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the LocalGPT runtime-policy snapshot: {exception.Message}");
            throw;
        }
    }

    private RegexOptions ParseFlags(LocalGptRuntimeRegexDefinition definition)
    {
        try
        {
            var options = RegexOptions.CultureInvariant;
            foreach (var token in definition.Flags.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                options |= token.ToLowerInvariant() switch
                {
                    "i" or "ignorecase" => RegexOptions.IgnoreCase,
                    "m" or "multiline" => RegexOptions.Multiline,
                    "s" or "singleline" => RegexOptions.Singleline,
                    "x" or "ignorepatternwhitespace" => RegexOptions.IgnorePatternWhitespace,
                    "n" or "explicitcapture" => RegexOptions.ExplicitCapture,
                    "compiled" => RegexOptions.Compiled,
                    "c" or "cultureinvariant" => RegexOptions.CultureInvariant,
                    "ecmascript" => RegexOptions.ECMAScript,
                    "none" => RegexOptions.None,
                    _ when Enum.TryParse<RegexOptions>(token, true, out var parsed) => parsed,
                    _ => throw new InvalidDataException($"Unknown regex option '{token}' for '{definition.Name}'.")
                };
            }
            logger.LogTrace($"Parsed runtime regex flags for {definition.Name}.");
            return options;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse runtime regex flags for {definition.Name}: {exception.Message}");
            throw;
        }
    }
}
