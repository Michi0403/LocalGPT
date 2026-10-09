using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>Applies database-backed regex limits before delegating framework construction to the policy-independent regex engine.</summary>
/// <param name="regexEngine">Policy-independent regex engine used to construct expressions from explicit bounds.</param>
/// <param name="runtimePolicy">Database-backed runtime-policy service that supplies configurable operational parameters for this component.</param>
/// <param name="logger">Logger used for regex compilation diagnostics.</param>
public sealed class RegexCompilationService(
    IRegexEngineService regexEngine,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<RegexCompilationService> logger) : IRegexCompilationService
{
    /// <summary>Compiles a regular expression using the active database-backed regex runtime parameters.</summary>
    /// <inheritdoc />
    public Regex Compile(string pattern, string? flags = null, TimeSpan? timeout = null, string? contextName = null)
    {
        try
        {
            var parameters = runtimePolicy.GetJson<RegexRuntimeParameters>(LocalGptRuntimeValue.RegexRuntimeParametersJson);
            return regexEngine.Compile(pattern, flags, parameters, timeout, contextName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regular-expression compilation failed for {ContextName}; pattern content was omitted.", contextName ?? "unspecified context");
            throw;
        }
    }

    /// <summary>Parses LocalGPT regular-expression option tokens through the policy-independent regex engine.</summary>
    /// <inheritdoc />
    public RegexOptions ParseOptions(string? flags, string? contextName = null)
    {
        try
        {
            return regexEngine.ParseOptions(flags, contextName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing regular-expression flags failed for {ContextName}; flag content was omitted.", contextName ?? "unspecified context");
            throw;
        }
    }
}
