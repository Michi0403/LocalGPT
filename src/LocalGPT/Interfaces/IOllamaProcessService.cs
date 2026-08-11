using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the ollama process service contract.
/// </summary>
public interface IOllamaProcessService
{
    /// <summary>
    /// Gets status async.
    /// </summary>
    Task<OllamaProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Starts async.
    /// </summary>
    Task<OllamaProcessStatus> StartAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Stops async.
    /// </summary>
    Task<OllamaProcessStatus> StopAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the restart async operation.
    /// </summary>
    Task<OllamaProcessStatus> RestartAsync(CancellationToken cancellationToken = default);
}
