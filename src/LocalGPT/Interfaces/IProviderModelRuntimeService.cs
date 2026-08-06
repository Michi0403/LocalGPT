using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using Microsoft.Extensions.AI;

namespace LocalGPT.Interfaces;

public interface IProviderModelRuntimeService
{
    Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default);
    Task<ProviderModelReference> ResolveAsync(string selectionOrModelName, CancellationToken cancellationToken = default);
    void Remember(ProviderModelReference model);
    ProviderModelReference FromSession(ChatClientSession session);
    IChatClient CreateChatClient(
        ProviderModelReference model,
        string keepAlive,
        int maxContextTokens,
        TimeSpan timeout,
        int? ollamaNumGpu,
        bool enableAutomaticTools = true,
        bool throwOnFailure = false);
    Task<ChatClientSession> CreateSessionAsync(ProviderModelReference model, CancellationToken cancellationToken = default);
}
