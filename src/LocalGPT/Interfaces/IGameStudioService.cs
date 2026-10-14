using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Project-owned scene authoring, curated AI submissions and editable Godot worktrees with explicit revision closure.</summary>
public interface IGameStudioService
{
    /// <summary>
    /// Retrieves draft as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft?> GetDraftAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Opens draft as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft> OpenDraftAsync(Guid projectId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists draft as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draft">Draft value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft> SaveDraftAsync(Guid projectId, GameStudioDraft draft, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Reads file as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ReadFileAsync(Guid projectId, Guid draftId, string path, CancellationToken cancellationToken = default);
    /// <summary>
    /// Persists file as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft> SaveFileAsync(Guid projectId, Guid draftId, string path, string content, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves proposals as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<GameSceneProposal>> GetProposalsAsync(Guid projectId, Guid draftId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs propose scene as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposal">Proposal value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene proposal produced by the operation.</returns>
    Task<GameSceneProposal> ProposeSceneAsync(Guid projectId, Guid draftId, GameSceneProposal proposal, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs propose patch as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="patch">Patch value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="artist">Artist value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene proposal produced by the operation.</returns>
    Task<GameSceneProposal> ProposePatchAsync(Guid projectId, Guid draftId, GameScenePatch patch, string artist, string description, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves proposal as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposalId">Identifier of the proposal to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene proposal produced by the operation.</returns>
    Task<GameSceneProposal?> GetProposalAsync(Guid projectId, Guid draftId, Guid proposalId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs review proposal as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="review">Review value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene review produced by the operation.</returns>
    Task<GameSceneReview> ReviewProposalAsync(Guid projectId, Guid draftId, GameSceneReview review, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves review as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposalId">Identifier of the proposal to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene review produced by the operation.</returns>
    Task<GameSceneReview?> GetReviewAsync(Guid projectId, Guid draftId, Guid proposalId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs accept proposal as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposalId">Identifier of the proposal to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    Task<GameStudioDraft> AcceptProposalAsync(Guid projectId, Guid draftId, Guid proposalId, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Closes revision as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="revisionName">Revision name value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local GPT project revision produced by the operation.</returns>
    Task<LocalGptProjectRevision> CloseRevisionAsync(Guid projectId, Guid draftId, string revisionName, string summary, bool userConfirmed, CancellationToken cancellationToken = default);
}
