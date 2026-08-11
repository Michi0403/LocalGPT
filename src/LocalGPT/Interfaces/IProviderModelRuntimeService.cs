using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using Microsoft.Extensions.AI;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the provider model runtime service contract.
/// </summary>
public interface IProviderModelRuntimeService
{
    /// <summary>
    /// Gets candidates async.
    /// </summary>
    Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Resolves async.
    /// </summary>
    Task<ProviderModelReference> ResolveAsync(string selectionOrModelName, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the remember operation.
    /// </summary>
    void Remember(ProviderModelReference model);
    /// <summary>
    /// Runs the from session operation.
    /// </summary>
    ProviderModelReference FromSession(ChatClientSession session);
    /// <summary>
    /// Creates chat client.
    /// </summary>
    IChatClient CreateChatClient(
        ProviderModelReference model,
        string keepAlive,
        int maxContextTokens,
        TimeSpan timeout,
        int? ollamaNumGpu,
        bool enableAutomaticTools = true,
        bool throwOnFailure = false);
    /// <summary>
    /// Creates session async.
    /// </summary>
    Task<ChatClientSession> CreateSessionAsync(ProviderModelReference model, CancellationToken cancellationToken = default);
}
