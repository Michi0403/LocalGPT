using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the ai connectivity probe contract.
    /// </summary>
    public interface IAiConnectivityProbe
    {
        /// <summary>
        /// Runs the test azure async operation.
        /// </summary>
        Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Runs the test open aiasync operation.
        /// </summary>
        Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions o, CancellationToken ct);
        /// <summary>
        /// Runs the test ollama async operation.
        /// </summary>
        Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Runs the test local open aicompat async operation.
        /// </summary>
        Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Attempts to start local async.
        /// </summary>
        Task<(bool ok, string message)> TryStartLocalAsync(ChatGPTLocalCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Runs the discover local hosts async operation.
        /// </summary>
        Task<IReadOnlyList<LocalAiHostDiscoveryResult>> DiscoverLocalHostsAsync(CancellationToken ct);
    }
}
