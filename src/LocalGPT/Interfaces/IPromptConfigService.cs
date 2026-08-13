using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for prompt config behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPromptConfigService
{
    /// <summary>
    /// Retrieves prompt as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="language">Language value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> GetPromptAsync(
        string key,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves prompt as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dto">Dto value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> GetPromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates prompt as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="dto">Dto value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task UpdatePromptAsync(
        PromptConfigDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists prompts as part of the prompt config service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="language">Language value supplied to the prompt config operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IEnumerable<PromptConfig>> ListPromptsAsync(
        string? language = null,
        CancellationToken cancellationToken = default);
}
