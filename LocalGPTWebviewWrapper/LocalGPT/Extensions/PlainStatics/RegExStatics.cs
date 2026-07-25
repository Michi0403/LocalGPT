using System.Text.RegularExpressions;

namespace LocalGPT.Extensions.PlainStatics;

public static class RegExStatics
{
    public static RegexOptions? ParseFlags(string? flags, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(flags))
                return RegexOptions.None;

            var result = RegexOptions.None;
            foreach (var token in flags.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result |= token.ToLowerInvariant() switch
                {
                    "i" => RegexOptions.IgnoreCase,
                    "m" => RegexOptions.Multiline,
                    "s" => RegexOptions.Singleline,
                    "x" => RegexOptions.IgnorePatternWhitespace,
                    "n" => RegexOptions.ExplicitCapture,
                    "compiled" => RegexOptions.Compiled,
                    "cultureinvariant" => RegexOptions.CultureInvariant,
                    "ecmascript" => RegexOptions.ECMAScript,
                    "ignorecase" => RegexOptions.IgnoreCase,
                    "multiline" => RegexOptions.Multiline,
                    "singleline" => RegexOptions.Singleline,
                    "ignorepatternwhitespace" => RegexOptions.IgnorePatternWhitespace,
                    "explicitcapture" => RegexOptions.ExplicitCapture,
                    "none" => RegexOptions.None,
                    _ when Enum.TryParse<RegexOptions>(token, ignoreCase: true, out var parsed) => parsed,
                    _ => throw new ArgumentException($"Unknown regular-expression option '{token}'.", nameof(flags))
                };
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not parse regular-expression flags {Flags}.", flags);
            return null;
        }
    }
}
