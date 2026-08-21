using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using Microsoft.Extensions.AI;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for provider model runtime behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProviderModelRuntimeService
{
    /// <summary>
    /// Retrieves candidates as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs resolve as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="selectionOrModelName">Selection or model name value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider model reference produced by the operation.</returns>
    Task<ProviderModelReference> ResolveAsync(string selectionOrModelName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Waits for the exact provider-qualified model endpoint to become reachable again without changing provider,
    /// model identity, or hardware-road policy.
    /// </summary>
    /// <param name="model">Exact provider-qualified model reference whose endpoint should be checked.</param>
    /// <param name="maximumWait">Maximum bounded time to wait for provider reavailability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns><see langword="true"/> when the exact model is reachable before the bounded wait expires.</returns>
    Task<bool> WaitForAvailabilityAsync(
        ProviderModelReference model,
        TimeSpan maximumWait,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs remember as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the provider model runtime operation and used when producing its result.</param>
    void Remember(ProviderModelReference model);
    /// <summary>
    /// Performs from session as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <returns>The provider model reference produced by the operation.</returns>
    ProviderModelReference FromSession(ChatClientSession session);
    /// <summary>
    /// Creates chat client as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="keepAlive">Keep alive value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="maxContextTokens">Max context tokens value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="timeout">Timeout value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="enableAutomaticTools">Value indicating whether enable automatic tools should apply to this operation.</param>
    /// <param name="throwOnFailure">Value indicating whether throw on failure should apply to this operation.</param>
    /// <param name="automaticFunctionAllowList">Optional exact registered-function allow-list. Null or empty preserves the historical full policy-approved automatic catalog.</param>
    /// <returns>The i chat client produced by the operation.</returns>
    IChatClient CreateChatClient(
        ProviderModelReference model,
        string keepAlive,
        int maxContextTokens,
        TimeSpan timeout,
        int? ollamaNumGpu,
        bool enableAutomaticTools = true,
        bool throwOnFailure = false,
        IReadOnlyCollection<string>? automaticFunctionAllowList = null);
    /// <summary>
    /// Creates session as part of the provider model runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the provider model runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The chat client session produced by the operation.</returns>
    Task<ChatClientSession> CreateSessionAsync(ProviderModelReference model, CancellationToken cancellationToken = default);
}
