using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the regex pattern service contract.
/// </summary>
public interface IRegexPatternService
{
    /// <summary>
    /// Adds or update async.
    /// </summary>
    Task AddOrUpdateAsync(RegexPatternDto dto);

    /// <summary>
    /// Gets regex async.
    /// </summary>
    Task<Regex?> GetRegexAsync(string name);

    /// <summary>
    /// Runs the compile operation.
    /// </summary>
    Regex Compile(string pattern, string? flags = null);

    /// <summary>
    /// Runs the compile operation.
    /// </summary>
    Regex Compile(string pattern, string? flags, TimeSpan timeout);

    /// <summary>
    /// Runs the list all async operation.
    /// </summary>
    Task<List<RegexPattern>> ListAllAsync();

    /// <summary>
    /// Runs the list all async operation.
    /// </summary>
    Task<List<RegexPattern>> ListAllAsync(int? take);

    /// <summary>
    /// Deletes async.
    /// </summary>
    Task DeleteAsync(string name);
}
/// <summary>
/// Defines the regex function parameter service contract.
/// </summary>
public interface IRegexFunctionParameterService
{
    /// <summary>
    /// Gets required string.
    /// </summary>
    string GetRequiredString(System.Text.Json.JsonElement element, string propertyName);
}
