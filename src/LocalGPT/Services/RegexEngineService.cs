using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>Owns framework regex construction while receiving all operational bounds explicitly from a database-backed policy owner.</summary>
/// <param name="logger">Logger used for regex-engine diagnostics without recording pattern content.</param>
public sealed class RegexEngineService(ILogger<RegexEngineService> logger) : IRegexEngineService
{
    /// <summary>Stores the LocalGPT regex flag token separators used by the persisted regex syntax.</summary>
    private readonly char[] FlagSeparators = [',', '|', ';'];

    /// <summary>Compiles a regular expression from explicit runtime-policy parameters.</summary>
    /// <inheritdoc />
    public Regex Compile(string pattern, string? flags, RegexRuntimeParameters parameters, TimeSpan? timeout = null, string? contextName = null)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(pattern);
            ArgumentNullException.ThrowIfNull(parameters);
            if (parameters.MaximumPatternCharacters <= 0)
                throw new InvalidDataException("Regex maximum pattern characters must be positive.");
            if (parameters.DefaultTimeoutSeconds <= 0)
                throw new InvalidDataException("Regex default timeout seconds must be positive.");
            if (parameters.MaximumTimeoutSeconds < parameters.DefaultTimeoutSeconds)
                throw new InvalidDataException("Regex maximum timeout seconds must be greater than or equal to the default timeout.");
            if (pattern.Length > parameters.MaximumPatternCharacters)
                throw new ArgumentException($"Regex patterns are limited to {parameters.MaximumPatternCharacters:N0} characters.", nameof(pattern));

            var defaultTimeout = TimeSpan.FromSeconds(parameters.DefaultTimeoutSeconds);
            var maximumTimeout = TimeSpan.FromSeconds(parameters.MaximumTimeoutSeconds);
            var requestedTimeout = timeout ?? defaultTimeout;
            var boundedTimeout = requestedTimeout <= TimeSpan.Zero || requestedTimeout > maximumTimeout
                ? defaultTimeout
                : requestedTimeout;
            return new Regex(pattern, ParseOptions(flags, contextName), boundedTimeout);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regular-expression engine compilation failed for {ContextName}; pattern content was omitted.", contextName ?? "unspecified context");
            throw;
        }
    }

    /// <summary>Parses LocalGPT regular-expression option tokens without entering runtime-policy resolution.</summary>
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
