using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Project-owned scene authoring, curated AI submissions and editable Godot worktrees with explicit revision closure.</summary>
public interface IGameStudioService
{
    Task<GameStudioDraft?> GetDraftAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<GameStudioDraft> OpenDraftAsync(Guid projectId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<GameStudioDraft> SaveDraftAsync(Guid projectId, GameStudioDraft draft, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<string> ReadFileAsync(Guid projectId, Guid draftId, string path, CancellationToken cancellationToken = default);
    Task<GameStudioDraft> SaveFileAsync(Guid projectId, Guid draftId, string path, string content, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameSceneProposal>> GetProposalsAsync(Guid projectId, Guid draftId, CancellationToken cancellationToken = default);
    Task<GameSceneProposal> ProposeSceneAsync(Guid projectId, Guid draftId, GameSceneProposal proposal, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<GameSceneProposal> ProposePatchAsync(Guid projectId, Guid draftId, GameScenePatch patch, string artist, string description, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<GameSceneProposal?> GetProposalAsync(Guid projectId, Guid draftId, Guid proposalId, CancellationToken cancellationToken = default);
    Task<GameSceneReview> ReviewProposalAsync(Guid projectId, Guid draftId, GameSceneReview review, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<GameSceneReview?> GetReviewAsync(Guid projectId, Guid draftId, Guid proposalId, CancellationToken cancellationToken = default);
    Task<GameStudioDraft> AcceptProposalAsync(Guid projectId, Guid draftId, Guid proposalId, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<LocalGptProjectRevision> CloseRevisionAsync(Guid projectId, Guid draftId, string revisionName, string summary, bool userConfirmed, CancellationToken cancellationToken = default);
}
