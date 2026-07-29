using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Compiles the database-backed runtime-policy definition into reusable runtime objects.
/// Pattern text, flags, executable names, identifiers and timeout values remain owned by
/// <see cref="ILocalGptRuntimePolicyStoreService"/> and its database rows.
/// </summary>
public sealed class LocalGptRuntimePolicyDataService : ILocalGptRuntimePolicyDataService
{
    private readonly ILocalGptRuntimePolicyStoreService store;
    private readonly ILogger<LocalGptRuntimePolicyDataService> logger;
    private readonly object sync = new();
    private Guid localGptCoreProjectId;
    private TimeSpan regexTimeout;
    private FrozenSet<string> allowedNativeExecutables = null!;
    private Regex powerShellInlineCommandPattern = null!;
    private Regex powerShellFilePattern = null!;
    private Regex sensitiveArgumentPattern = null!;

    public LocalGptRuntimePolicyDataService(
        ILocalGptRuntimePolicyStoreService store,
        ILogger<LocalGptRuntimePolicyDataService> logger)
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
            logger.LogError(exception, $"Could not initialize the database-backed LocalGPT runtime policy service: {exception.Message}");
            throw;
        }
    }

    public Guid LocalGptCoreProjectId
    {
        get { lock (sync) return localGptCoreProjectId; }
    }

    public TimeSpan RegexTimeout
    {
        get { lock (sync) return regexTimeout; }
    }

    public FrozenSet<string> AllowedNativeExecutables
    {
        get { lock (sync) return allowedNativeExecutables; }
    }

    public Regex PowerShellInlineCommandPattern
    {
        get { lock (sync) return powerShellInlineCommandPattern; }
    }

    public Regex PowerShellFilePattern
    {
        get { lock (sync) return powerShellFilePattern; }
    }

    public Regex SensitiveArgumentPattern
    {
        get { lock (sync) return sensitiveArgumentPattern; }
    }

    public LocalGptRuntimePolicySnapshot Reload()
    {
        try
        {
            var definition = store.GetDefinition();
            var timeout = TimeSpan.FromMilliseconds(definition.RegexTimeoutMilliseconds);

            RegexOptions ParseFlags(LocalGptRuntimeRegexDefinition regexDefinition)
            {
                var options = RegexOptions.CultureInvariant;
                foreach (var token in regexDefinition.Flags.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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
                        _ => throw new InvalidDataException($"Unknown regular-expression option '{token}' in runtime-policy regex '{regexDefinition.Name}'.")
                    };
                }

                return options;
            }

            var executableSet = definition.AllowedNativeExecutables.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            var inlinePattern = new Regex(
                definition.PowerShellInlineCommandPattern.Pattern,
                ParseFlags(definition.PowerShellInlineCommandPattern),
                timeout);
            var filePattern = new Regex(
                definition.PowerShellFilePattern.Pattern,
                ParseFlags(definition.PowerShellFilePattern),
                timeout);
            var argumentPattern = new Regex(
                definition.SensitiveArgumentPattern.Pattern,
                ParseFlags(definition.SensitiveArgumentPattern),
                timeout);

            lock (sync)
            {
                localGptCoreProjectId = definition.LocalGptCoreProjectId;
                regexTimeout = timeout;
                allowedNativeExecutables = executableSet;
                powerShellInlineCommandPattern = inlinePattern;
                powerShellFilePattern = filePattern;
                sensitiveArgumentPattern = argumentPattern;
            }

            var snapshot = GetSnapshot();
            logger.LogInformation($"Reloaded the LocalGPT runtime policy from database rows with {snapshot.AllowedNativeExecutables.Count} native executable entries and a {snapshot.RegexTimeout.TotalMilliseconds:0} ms regex timeout.");
            return snapshot;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not reload the LocalGPT runtime policy from database rows: {exception.Message}");
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
                    LocalGptCoreProjectId = localGptCoreProjectId,
                    RegexTimeout = regexTimeout,
                    AllowedNativeExecutables = allowedNativeExecutables.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray(),
                    PowerShellInlineCommandPattern = powerShellInlineCommandPattern.ToString(),
                    PowerShellFilePattern = powerShellFilePattern.ToString(),
                    SensitiveArgumentPattern = sensitiveArgumentPattern.ToString()
                };
                logger.LogTrace($"Returned the LocalGPT runtime policy snapshot with {snapshot.AllowedNativeExecutables.Count} native executable entries.");
                return snapshot;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the LocalGPT runtime policy snapshot: {exception.Message}");
            throw;
        }
    }
}
