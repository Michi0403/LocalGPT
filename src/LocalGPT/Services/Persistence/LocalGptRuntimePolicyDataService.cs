using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates LocalGPT runtime policy behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class LocalGptRuntimePolicyDataService : ILocalGptRuntimePolicyDataService
{
    /// <summary>
    /// Stores the LocalGPT runtime policy store service dependency used by <see cref="LocalGptRuntimePolicyDataService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ILocalGptRuntimePolicyStoreService store;
    /// <summary>
    /// Stores the logger used by <see cref="LocalGptRuntimePolicyDataService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<LocalGptRuntimePolicyDataService> logger;
    /// <summary>
    /// Stores the internal state state used by <see cref="LocalGptRuntimePolicyDataService"/> while executing its surrounding workflow.
    /// </summary>
    private LocalGptRuntimePolicyState state = null!;

    /// <summary>
    /// Initializes a new <see cref="LocalGptRuntimePolicyDataService"/> instance and captures the dependencies or initial state required by its LocalGPT runtime policy workflow.
    /// </summary>
    /// <param name="store">Local gpt runtime policy store service dependency used by the LocalGPT runtime policy workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// Gets the stable LocalGPT core project identifier used to identify or correlate this LocalGPT runtime policy instance with related application state.
    /// </summary>
    /// <value>The LocalGPT core project identifier value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Guid LocalGptCoreProjectId => GetGuid(LocalGptRuntimeValue.LocalGptCoreProjectId);
    /// <summary>
    /// Gets the regex timeout duration used to control timing in the LocalGPT runtime policy workflow.
    /// </summary>
    /// <value>The regex timeout value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public TimeSpan RegexTimeout => TimeSpan.FromMilliseconds(GetInt(LocalGptRuntimeValue.RegexTimeoutMilliseconds));
    /// <summary>
    /// Gets the allowed native executables value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed native executables value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public FrozenSet<string> AllowedNativeExecutables => GetCollection(LocalGptRuntimeCollection.AllowedNativeExecutables);
    /// <summary>
    /// Gets the power shell inline command pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The power shell inline command pattern value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Regex PowerShellInlineCommandPattern => GetPattern(LocalGptRuntimePattern.PowerShellInlineCommand);
    /// <summary>
    /// Gets the power shell file pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The power shell file pattern value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Regex PowerShellFilePattern => GetPattern(LocalGptRuntimePattern.PowerShellFile);
    /// <summary>
    /// Gets the sensitive argument pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sensitive argument pattern value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Regex SensitiveArgumentPattern => GetPattern(LocalGptRuntimePattern.SensitiveArgument);

    /// <summary>
    /// Retrieves string as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Retrieves int as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
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
    /// Retrieves long as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The long produced by the operation.</returns>
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
    /// Retrieves GUID as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The GUID produced by the operation.</returns>
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
    /// Retrieves JSON as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="LocalGptRuntimePolicyDataService"/>.</typeparam>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
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
    /// Retrieves pattern as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
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
    /// Retrieves collection as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The frozen set string produced by the operation.</returns>
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
    /// Performs reload as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy snapshot produced by the operation.</returns>
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
    /// Retrieves snapshot as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy snapshot produced by the operation.</returns>
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
    /// Creates snapshot as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="current">Current value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The LocalGPT runtime policy snapshot produced by the operation.</returns>
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
    /// Parses flags as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="definition">Definition value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The regex options produced by the operation.</returns>
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
