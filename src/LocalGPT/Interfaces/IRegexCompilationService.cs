using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>Centralizes bounded regular-expression option parsing and compilation behind a DI-owned service boundary.</summary>
public interface IRegexCompilationService
{
    /// <summary>Compiles a bounded regular expression using LocalGPT flag syntax and timeout policy.</summary>
    /// <param name="pattern">Pattern value supplied to the regex compilation operation and used when producing its result.</param>
    /// <param name="flags">Flags value supplied to the regex compilation operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the regex compilation operation and used when producing its result.</param>
    /// <param name="contextName">Context name value supplied to the regex compilation operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    Regex Compile(string pattern, string? flags = null, TimeSpan? timeout = null, string? contextName = null);

    /// <summary>Parses LocalGPT regular-expression option tokens into framework flags.</summary>
    /// <param name="flags">Flags value supplied to the regex compilation operation and used when producing its result.</param>
    /// <param name="contextName">Context name value supplied to the regex compilation operation and used when producing its result.</param>
    /// <returns>The regex options produced by the operation.</returns>
    RegexOptions ParseOptions(string? flags, string? contextName = null);
}
