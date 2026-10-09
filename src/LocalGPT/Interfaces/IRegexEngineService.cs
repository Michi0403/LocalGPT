using LocalGPT.BusinessObjects;
using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>Compiles regular expressions from explicit caller-supplied runtime policy without depending on the runtime-policy service itself.</summary>
public interface IRegexEngineService
{
    /// <summary>Compiles a regular expression using explicit database-backed bounds supplied by the owning policy service.</summary>
    /// <param name="pattern">Pattern text to compile.</param>
    /// <param name="flags">LocalGPT regular-expression option tokens.</param>
    /// <param name="parameters">Database-backed regex bounds and defaults to enforce.</param>
    /// <param name="timeout">Optional caller-requested timeout.</param>
    /// <param name="contextName">Diagnostic context name that never includes pattern content.</param>
    /// <returns>The bounded compiled regular expression.</returns>
    Regex Compile(string pattern, string? flags, RegexRuntimeParameters parameters, TimeSpan? timeout = null, string? contextName = null);

    /// <summary>Parses LocalGPT regular-expression option tokens without consulting runtime-policy state.</summary>
    /// <param name="flags">LocalGPT regular-expression option tokens.</param>
    /// <param name="contextName">Diagnostic context name that never includes pattern content.</param>
    /// <returns>The framework regex options represented by the supplied tokens.</returns>
    RegexOptions ParseOptions(string? flags, string? contextName = null);
}
