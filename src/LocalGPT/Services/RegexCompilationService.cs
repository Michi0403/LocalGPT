using LocalGPT.Interfaces;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>Owns regular-expression size, option and timeout policy shared by persisted pattern services.</summary>
/// <param name="logger">Logger used for regex compilation diagnostics.</param>
public sealed class RegexCompilationService(ILogger<RegexCompilationService> logger) : IRegexCompilationService
{
    /// <summary>
    /// Stores the internal flag separators state used by <see cref="RegexCompilationService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly char[] FlagSeparators = [',', '|', ';'];

    /// <summary>
    /// Performs compile as part of the regex compilation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public Regex Compile(string pattern, string? flags = null, TimeSpan? timeout = null, string? contextName = null)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(pattern);
            if (pattern.Length > 16_000)
                throw new ArgumentException("Regex patterns are limited to 16,000 characters.", nameof(pattern));
            var requestedTimeout = timeout ?? TimeSpan.FromSeconds(2);
            var boundedTimeout = requestedTimeout <= TimeSpan.Zero || requestedTimeout > TimeSpan.FromSeconds(30)
                ? TimeSpan.FromSeconds(2)
                : requestedTimeout;
            return new Regex(pattern, ParseOptions(flags, contextName), boundedTimeout);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regular-expression compilation failed for {ContextName}; pattern content was omitted.", contextName ?? "unspecified context");
            throw;
        }
    }

    /// <summary>
    /// Parses options as part of the regex compilation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public RegexOptions ParseOptions(string? flags, string? contextName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(flags))
                return RegexOptions.CultureInvariant;
            var result = RegexOptions.CultureInvariant;
            foreach (var token in flags.Split(FlagSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result |= token.ToLowerInvariant() switch
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
                    _ => throw new InvalidDataException($"Unknown regular-expression option '{token}' for '{contextName ?? "unspecified context"}'.")
                };
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing regular-expression flags failed for {ContextName}; flag content was omitted.", contextName ?? "unspecified context");
            throw;
        }
    }
}
