using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IOllamaProcessService
{
    Task<OllamaProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<OllamaProcessStatus> StartAsync(CancellationToken cancellationToken = default);
    Task<OllamaProcessStatus> StopAsync(CancellationToken cancellationToken = default);
    Task<OllamaProcessStatus> RestartAsync(CancellationToken cancellationToken = default);
}
