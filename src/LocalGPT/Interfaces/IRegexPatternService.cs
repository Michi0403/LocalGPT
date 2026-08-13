using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for regex pattern behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRegexPatternService
{
    /// <summary>
    /// Adds or update as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dto">Dto value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task AddOrUpdateAsync(RegexPatternDto dto);

    /// <summary>
    /// Retrieves regex as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    Task<Regex?> GetRegexAsync(string name);

    /// <summary>
    /// Performs compile as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="flags">Flags value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    Regex Compile(string pattern, string? flags = null);

    /// <summary>
    /// Performs compile as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="flags">Flags value supplied to the regex pattern operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    Regex Compile(string pattern, string? flags, TimeSpan timeout);

    /// <summary>
    /// Lists all as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    Task<List<RegexPattern>> ListAllAsync();

    /// <summary>
    /// Lists all as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<List<RegexPattern>> ListAllAsync(int? take);

    /// <summary>
    /// Performs delete as part of the regex pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex pattern operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DeleteAsync(string name);
}
/// <summary>
/// Defines the contract for regex function parameter behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IRegexFunctionParameterService
{
    /// <summary>
    /// Retrieves required string as part of the regex function parameter service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="element">Element value supplied to the regex function parameter operation and used when producing its result.</param>
    /// <param name="propertyName">Property name value supplied to the regex function parameter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetRequiredString(System.Text.Json.JsonElement element, string propertyName);
}
