using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IAiConnectivityProbe
    {
        Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions o, CancellationToken ct);
        Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions o, CancellationToken ct);
        Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions o, CancellationToken ct);
        Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions o, CancellationToken ct);
        Task<(bool ok, string message)> TryStartLocalAsync(ChatGPTLocalCoreOptions o, CancellationToken ct);
    }
}
