using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the prompt config service contract.
/// </summary>
public interface IPromptConfigService
{
    /// <summary>
    /// Gets prompt async.
    /// </summary>
    Task<string> GetPromptAsync(
        string key,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets prompt async.
    /// </summary>
    Task<string> GetPromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates prompt async.
    /// </summary>
    Task UpdatePromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the list prompts async operation.
    /// </summary>
    Task<IEnumerable<PromptConfig>> ListPromptsAsync(
        string? language = null,
        CancellationToken cancellationToken = default);
}
