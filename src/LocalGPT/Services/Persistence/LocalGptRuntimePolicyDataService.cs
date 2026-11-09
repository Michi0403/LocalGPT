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
    /// Stores the policy-independent regex engine used to compile persisted runtime patterns from explicit database-backed bounds.
    /// </summary>
    private readonly IRegexEngineService regexEngine;
    /// <summary>
    /// Stores the authoritative seed; its regex names must always compile, database-added rows may be skipped.
    /// </summary>
    private readonly ILocalGptRuntimePolicySeedDataService seedData;
    /// <summary>
    /// Stores the JSON text service used to deserialize typed runtime-policy documents without creating a second serializer policy.
    /// </summary>
    private readonly IJsonTextService jsonText;
    /// <summary>
    /// Stores the internal state state used by <see cref="LocalGptRuntimePolicyDataService"/> while executing its surrounding workflow.
    /// </summary>
    private LocalGptRuntimePolicyState state = null!;

    /// <summary>
    /// Initializes a new <see cref="LocalGptRuntimePolicyDataService"/> instance and captures the dependencies or initial state required by its LocalGPT runtime policy workflow.
    /// </summary>
    /// <param name="store">Local gpt runtime policy store service dependency used by the LocalGPT runtime policy workflow to provide the corresponding application capability.</param>
    /// <param name="seedData">Authoritative built-in runtime-policy seed used before persisted database overrides are available.</param>
    /// <param name="regexEngine">Policy-independent regex engine used during bootstrap and database reload from explicit runtime-policy bounds.</param>
    /// <param name="jsonText">JSON text service used to deserialize typed runtime-policy documents.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public LocalGptRuntimePolicyDataService(
        ILocalGptRuntimePolicyStoreService store,
        ILocalGptRuntimePolicySeedDataService seedData,
        IRegexEngineService regexEngine,
        IJsonTextService jsonText,
        ILogger<LocalGptRuntimePolicyDataService> logger)
    {
        this.store = store;
        this.seedData = seedData;
        this.regexEngine = regexEngine;
        this.jsonText = jsonText;
        this.logger = logger;
        try
        {
            // Boot must never synchronously enter the SQLite initializer from a singleton constructor.
            // The seed is the authoritative safe bootstrap; the hosted database initializer reloads
            // persisted user policy immediately after migrations and deterministic seeding complete.
            state = BuildState(CreateSeedDefinition(seedData.GetSeed()));
            logger.LogInformation("Initialized LocalGPT runtime policy from the built-in seed; persisted overrides will load after database initialization.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not initialize the LocalGPT runtime-policy seed state: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the stable LocalGPT core project identifier used to identify or correlate this LocalGPT runtime policy instance with related application state.
    /// </summary>
    /// <value>The LocalGPT core project identifier value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Guid LocalGptCoreProjectId => GetGuid("LocalGptCoreProjectId");
    /// <summary>
    /// Gets the regex timeout duration used to control timing in the LocalGPT runtime policy workflow.
    /// </summary>
    /// <value>The regex timeout value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public TimeSpan RegexTimeout => TimeSpan.FromMilliseconds(GetInt("RegexTimeoutMilliseconds"));
    /// <summary>
    /// Gets the allowed native executables value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed native executables value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public FrozenSet<string> AllowedNativeExecutables => GetCollection("AllowedNativeExecutables");
    /// <summary>
    /// Gets the power shell inline command pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The power shell inline command pattern value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Regex PowerShellInlineCommandPattern => GetPattern("runtime.native.powershell-inline-command");
    /// <summary>
    /// Gets the power shell file pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The power shell file pattern value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Regex PowerShellFilePattern => GetPattern("runtime.native.powershell-file");
    /// <summary>
    /// Gets the sensitive argument pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sensitive argument pattern value exposed by <see cref="LocalGptRuntimePolicyDataService"/>.</value>
    public Regex SensitiveArgumentPattern => GetPattern("runtime.native.sensitive-argument");

    /// <summary>
    /// Retrieves string as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetString(string key)
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
    public int GetInt(string key)
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
    public long GetLong(string key)
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
    public Guid GetGuid(string key)
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
    public T GetJson<T>(string key)
    {
        try
        {
            var raw = GetString(key);
            return jsonText.Deserialize<T>(raw)
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
    public Regex GetPattern(string key)
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
    public FrozenSet<string> GetCollection(string key)
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
            var next = BuildState(definition);
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

    /// <summary>Builds a runtime-policy definition from the authoritative compiled-in bootstrap seed without database I/O.</summary>
    /// <param name="seed">Seed value supplied to the local GPT runtime policy operation and used when producing its result.</param>
    /// <returns>The local GPT runtime policy definition produced by the operation.</returns>
    private LocalGptRuntimePolicyDefinition CreateSeedDefinition(LocalGptRuntimePolicySeedModel seed)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(seed);
            var definition = new LocalGptRuntimePolicyDefinition
            {
                Values = seed.SystemVariables.ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase),
                Collections = seed.Collections.ToDictionary(
                    item => item.Name,
                    item => (IReadOnlyList<string>)item.Values.ToArray()),
                RegexPatterns = seed.RegexPatterns.ToDictionary(
                    item => item.Name,
                    item => new LocalGptRuntimeRegexDefinition
                    {
                        Name = item.Name,
                        Pattern = item.Pattern,
                        Flags = item.Flags
                    })
            };
            logger.LogTrace($"Created the non-database LocalGPT runtime-policy bootstrap definition from the authoritative seed.");
            return definition;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not create the LocalGPT runtime-policy bootstrap definition from seed data: {exception.Message}");
            throw;
        }
    }

    /// <summary>Compiles an immutable runtime-policy state from either bootstrap seed data or persisted database data.</summary>
    /// <param name="definition">Definition value supplied to the local GPT runtime policy operation and used when producing its result.</param>
    /// <returns>The local GPT runtime policy state produced by the operation.</returns>
    private LocalGptRuntimePolicyState BuildState(LocalGptRuntimePolicyDefinition definition)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(definition);
            var values = definition.Values ?? throw new InvalidDataException("The runtime policy definition contains no values.");
            if (!values.TryGetValue("RegexTimeoutMilliseconds", out var timeoutRaw))
                throw new InvalidDataException("RegexTimeoutMilliseconds is missing from the runtime policy definition.");
            if (!int.TryParse(timeoutRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeoutMilliseconds) || timeoutMilliseconds <= 0)
                throw new InvalidDataException("RegexTimeoutMilliseconds must be positive.");

            if (!values.TryGetValue("RegexRuntimeParametersJson", out var regexParametersRaw))
                throw new InvalidDataException("RegexRuntimeParametersJson is missing from the runtime policy definition.");
            var regexParameters = jsonText.Deserialize<RegexRuntimeParameters>(regexParametersRaw)
                ?? throw new InvalidDataException("RegexRuntimeParametersJson could not be deserialized.");

            var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            var next = new LocalGptRuntimePolicyState(
                values.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                definition.Collections
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Select(value => value.Trim())
                            .ToFrozenSet(StringComparer.OrdinalIgnoreCase),
                        StringComparer.OrdinalIgnoreCase)
                    .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
                CompilePatterns(definition, regexParameters, timeout));
            logger.LogTrace($"Compiled immutable LocalGPT runtime-policy state with {next.Values.Count} values, {next.Collections.Count} collections, and {next.Patterns.Count} regex patterns.");
            return next;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not compile immutable LocalGPT runtime-policy state: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Compiles every regular expression of the database-defined policy. A pattern supplied by the seed must
    /// compile; a pattern added in the database that does not compile is skipped and logged, so one bad row
    /// cannot stop LocalGPT.
    /// </summary>
    /// <param name="definition">Database-defined runtime policy.</param>
    /// <param name="regexParameters">Bounded regex engine parameters from the policy.</param>
    /// <param name="timeout">Match timeout of every compiled pattern.</param>
    /// <returns>Compiled patterns keyed by RegexPatterns.Name (case-insensitive).</returns>
    private FrozenDictionary<string, Regex> CompilePatterns(
        LocalGptRuntimePolicyDefinition definition,
        RegexRuntimeParameters regexParameters,
        TimeSpan timeout)
    {
        try
        {
            var seeded = seedData.GetSeed().RegexPatterns
                .Select(item => item.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var compiled = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, pattern) in definition.RegexPatterns)
            {
                try
                {
                    compiled[name] = regexEngine.Compile(pattern.Pattern, pattern.Flags, regexParameters, timeout, pattern.Name);
                }
                catch (Exception exception) when (!seeded.Contains(name))
                {
                    logger.LogWarning(exception, $"Skipped database runtime regex {name}: it does not compile ({exception.Message}).");
                }
            }

            logger.LogDebug($"Compiled {compiled.Count} of {definition.RegexPatterns.Count} runtime regex patterns.");
            return compiled.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not compile the runtime regex patterns: {exception.Message}");
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
                Values = current.Values.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase),
                Collections = current.Collections.ToDictionary(
                    item => item.Key,
                    item => (IReadOnlyList<string>)item.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
                    StringComparer.OrdinalIgnoreCase),
                RegexPatterns = current.Patterns.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase)
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


}
