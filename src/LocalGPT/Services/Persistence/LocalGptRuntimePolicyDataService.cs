using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Provides local gpt runtime policy data service operations.
/// </summary>
public sealed class LocalGptRuntimePolicyDataService : ILocalGptRuntimePolicyDataService
{
    private readonly ILocalGptRuntimePolicyStoreService store;
    private readonly ILogger<LocalGptRuntimePolicyDataService> logger;
    private LocalGptRuntimePolicyState state = null!;

    /// <summary>
    /// Runs the local gpt runtime policy data service operation.
    /// </summary>
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

    /// <summary>
    /// Gets or sets local gpt core project identifier.
    /// </summary>
    public Guid LocalGptCoreProjectId => GetGuid(LocalGptRuntimeValue.LocalGptCoreProjectId);
    /// <summary>
    /// Gets or sets regex timeout.
    /// </summary>
    public TimeSpan RegexTimeout => TimeSpan.FromMilliseconds(GetInt(LocalGptRuntimeValue.RegexTimeoutMilliseconds));
    /// <summary>
    /// Gets or sets allowed native executables.
    /// </summary>
    public FrozenSet<string> AllowedNativeExecutables => GetCollection(LocalGptRuntimeCollection.AllowedNativeExecutables);
    /// <summary>
    /// Gets or sets power shell inline command pattern.
    /// </summary>
    public Regex PowerShellInlineCommandPattern => GetPattern(LocalGptRuntimePattern.PowerShellInlineCommand);
    /// <summary>
    /// Gets or sets power shell file pattern.
    /// </summary>
    public Regex PowerShellFilePattern => GetPattern(LocalGptRuntimePattern.PowerShellFile);
    /// <summary>
    /// Gets or sets sensitive argument pattern.
    /// </summary>
    public Regex SensitiveArgumentPattern => GetPattern(LocalGptRuntimePattern.SensitiveArgument);

    /// <summary>
    /// Gets string.
    /// </summary>
    public string GetString(LocalGptRuntimeValue key)
    {
        try
        {
            var current = Volatile.Read(ref state);
            if (!current.Values.TryGetValue(key, out var value))
                throw new KeyNotFoundException($"Runtime value '{key}' is not loaded.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve LocalGPT runtime value {key}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets int.
    /// </summary>
    public int GetInt(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException($"Runtime value '{key}' is not a valid Int32.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse LocalGPT runtime integer {key}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets long.
    /// </summary>
    public long GetLong(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException($"Runtime value '{key}' is not a valid Int64.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse LocalGPT runtime long {key}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets guid.
    /// </summary>
    public Guid GetGuid(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            if (!Guid.TryParse(raw, out var value))
                throw new InvalidDataException($"Runtime value '{key}' is not a valid Guid.");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse LocalGPT runtime GUID {key}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets JSON.
    /// </summary>
    public T GetJson<T>(LocalGptRuntimeValue key)
    {
        try
        {
            var raw = GetString(key);
            var jsonOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            return System.Text.Json.JsonSerializer.Deserialize<T>(raw, jsonOptions)
                ?? throw new InvalidDataException($"Runtime value '{key}' could not be deserialized as {typeof(T).Name}.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not deserialize LocalGPT runtime document {key} as {typeof(T).Name}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets pattern.
    /// </summary>
    public Regex GetPattern(LocalGptRuntimePattern key)
    {
        try
        {
            var current = Volatile.Read(ref state);
            if (!current.Patterns.TryGetValue(key, out var pattern))
                throw new KeyNotFoundException($"Runtime pattern '{key}' is not loaded.");
            return pattern;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve LocalGPT runtime regex {key}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets collection.
    /// </summary>
    public FrozenSet<string> GetCollection(LocalGptRuntimeCollection key)
    {
        try
        {
            var current = Volatile.Read(ref state);
            if (!current.Collections.TryGetValue(key, out var collection))
                throw new KeyNotFoundException($"Runtime collection '{key}' is not loaded.");
            return collection;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve LocalGPT runtime collection {key}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the reload operation.
    /// </summary>
    public LocalGptRuntimePolicySnapshot Reload()
    {
        try
        {
            var definition = store.GetDefinition() ?? throw new InvalidDataException("The runtime policy store returned no definition.");
            var values = definition.Values ?? throw new InvalidDataException("The runtime policy definition contains no values.");
            if (!values.TryGetValue(LocalGptRuntimeValue.RegexTimeoutMilliseconds, out var timeoutRaw))
                throw new InvalidDataException("RegexTimeoutMilliseconds is missing from the runtime policy definition.");
            if (!int.TryParse(timeoutRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutMilliseconds) || timeoutMilliseconds <= 0)
                throw new InvalidDataException("RegexTimeoutMilliseconds must be positive.");

            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            var next = new LocalGptRuntimePolicyState(
                values.ToFrozenDictionary(),
                definition.Collections
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Select(value => value.Trim())
                            .ToFrozenSet(StringComparer.OrdinalIgnoreCase))
                    .ToFrozenDictionary(),
                definition.RegexPatterns
                    .ToDictionary(
                        item => item.Key,
                        item => new Regex(item.Value.Pattern, ParseFlags(item.Value), timeout))
                    .ToFrozenDictionary());

            Volatile.Write(ref state, next);
            var snapshot = CreateSnapshot(next);
            logger.LogInformation($"Reloaded {snapshot.Values.Count} values, {snapshot.Collections.Count} collections and {snapshot.RegexPatterns.Count} regexes from the LocalGPT database.");
            return snapshot;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not reload LocalGPT runtime policy data: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets snapshot.
    /// </summary>
    public LocalGptRuntimePolicySnapshot GetSnapshot()
    {
        try
        {
            return CreateSnapshot(Volatile.Read(ref state));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the LocalGPT runtime-policy snapshot: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates snapshot.
    /// </summary>
    private LocalGptRuntimePolicySnapshot CreateSnapshot(LocalGptRuntimePolicyState current)
    {
        try
        {
            var snapshot = new LocalGptRuntimePolicySnapshot
            {
                Values = current.Values.ToDictionary(item => item.Key.ToString(), item => item.Value),
                Collections = current.Collections.ToDictionary(
                    item => item.Key.ToString(),
                    item => (IReadOnlyList<string>)item.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()),
                RegexPatterns = current.Patterns.ToDictionary(item => item.Key.ToString(), item => item.Value.ToString())
            };
            logger.LogTrace($"Created a LocalGPT runtime-policy snapshot from the active immutable state.");
            return snapshot;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create a LocalGPT runtime-policy snapshot: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Parses flags.
    /// </summary>
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
            return options;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse runtime regex flags for {definition.Name}: {exception.Message}");
            throw;
        }
    }
}
