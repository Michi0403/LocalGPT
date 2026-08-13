using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for AI connectivity probe behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IAiConnectivityProbe
    {
        /// <summary>
        /// Performs test azure for <see cref="IAiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
        /// </summary>
        /// <param name="o">O value supplied to the AI connectivity probe operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The bool ok string message produced by the operation.</returns>
        Task<(bool ok, string message)> TestAzureAsync(OpenAIServiceCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Performs test OpenAI for <see cref="IAiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
        /// </summary>
        /// <param name="o">O value supplied to the AI connectivity probe operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The bool ok string message produced by the operation.</returns>
        Task<(bool ok, string message)> TestOpenAIAsync(OpenAICompatOptions o, CancellationToken ct);
        /// <summary>
        /// Performs test Ollama for <see cref="IAiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
        /// </summary>
        /// <param name="o">O value supplied to the AI connectivity probe operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The bool ok string message produced by the operation.</returns>
        Task<(bool ok, string message)> TestOllamaAsync(OllamaCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Performs test local OpenAI compat for <see cref="IAiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
        /// </summary>
        /// <param name="o">O value supplied to the AI connectivity probe operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The bool ok string message produced by the operation.</returns>
        Task<(bool ok, string message)> TestLocalOpenAICompatAsync(ChatGPTLocalCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Attempts to start local for <see cref="IAiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
        /// </summary>
        /// <param name="o">O value supplied to the AI connectivity probe operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The bool ok string message produced by the operation.</returns>
        Task<(bool ok, string message)> TryStartLocalAsync(ChatGPTLocalCoreOptions o, CancellationToken ct);
        /// <summary>
        /// Discovers local hosts for <see cref="IAiConnectivityProbe"/>, keeping the operation consistent with the state and invariants of the surrounding AI connectivity probe workflow.
        /// </summary>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        Task<IReadOnlyList<LocalAiHostDiscoveryResult>> DiscoverLocalHostsAsync(CancellationToken ct);
    }
}
