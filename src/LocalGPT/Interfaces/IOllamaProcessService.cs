using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for Ollama process behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOllamaProcessService
{
    /// <summary>
    /// Retrieves status as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    Task<OllamaProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs start as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    Task<OllamaProcessStatus> StartAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs stop as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    Task<OllamaProcessStatus> StopAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs restart as part of the Ollama process service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The Ollama process status produced by the operation.</returns>
    Task<OllamaProcessStatus> RestartAsync(CancellationToken cancellationToken = default);
}
