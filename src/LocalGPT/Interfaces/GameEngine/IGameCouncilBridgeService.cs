using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Connects Council workflow steps to the Council-driven game a run owns: narrows members to the active entrants,
/// turns role answers into one engine turn and describes asset drawing turns. Everything game specific comes from the
/// game's ruleset (lineup rule parameters and texts); the Council service only runs the model turns.
/// </summary>
public interface IGameCouncilBridgeService
{
    /// <summary>Returns the newest Council-driven game session owned by a run, running or ended.</summary>
    /// <param name="councilRunId">Identifier of the Council run.</param>
    /// <param name="includeEnded"><c>true</c> to also consider sessions that are no longer running.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The session snapshot, or <c>null</c> when the run owns no Council-driven game.</returns>
    Task<CouncilGameSessionSnapshot?> FindRunGameAsync(Guid councilRunId, bool includeEnded, CancellationToken cancellationToken = default);

    /// <summary>
    /// Narrows the members of a role to the models of the two entrants the run's game currently pairs (owner role:
    /// owner models; any other role: member models). The full pool is returned when no complete pair is known.
    /// </summary>
    /// <param name="councilRunId">Identifier of the Council run.</param>
    /// <param name="roleName">Role of the workflow step.</param>
    /// <param name="roleParticipants">Configured members of the role.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The members that take the step.</returns>
    Task<IReadOnlyList<string>> LimitToActiveEntrantsAsync(Guid councilRunId, string roleName, IReadOnlyList<string> roleParticipants, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds one engine turn from the run's role answers (lineup, presentation frames, evidence), lets the game's
    /// council progression rule resolve it and returns the transcript-facing result.
    /// </summary>
    /// <param name="councilRunId">Identifier of the Council run.</param>
    /// <param name="steps">Transcript steps of the run so far.</param>
    /// <param name="pairings">Runtime role pairings of the run (role, member, paired role, paired member).</param>
    /// <param name="initializeOnly"><c>true</c> for the lineup/initialization step.</param>
    /// <param name="drawAsset">Runs one asset drawing turn; only used when initializing.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the step.</param>
    /// <returns>The engine step result.</returns>
    Task<GameCouncilEngineStep> ResolveAsync(
        Guid councilRunId,
        IReadOnlyList<MultiModelCouncilStep> steps,
        IReadOnlyList<(string Role, string Participant, string PairedRole, string PairedParticipant)> pairings,
        bool initializeOnly,
        Func<GameAssetDraftRequest, CancellationToken, Task<GameAssetDraft?>>? drawAsset,
        CancellationToken cancellationToken = default);

    /// <summary>Returns which workflow steps supply the drawers of the run's asset check.</summary>
    /// <param name="councilRunId">Identifier of the Council run.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The asset plan, or <c>null</c> when the run's game declares no asset check.</returns>
    Task<GameAssetPlan?> PlanAssetAsync(Guid councilRunId, CancellationToken cancellationToken = default);

    /// <summary>Builds the prompt and labels of one asset drawing turn from the game's <c>asset.*</c> texts.</summary>
    /// <param name="councilRunId">Identifier of the Council run.</param>
    /// <param name="request">Entrant, other assets and attempt number.</param>
    /// <param name="drawerModelName">Member that draws.</param>
    /// <param name="drawsSelf"><c>true</c> when the entrant's own member draws.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the lookup.</param>
    /// <returns>The drawing prompt.</returns>
    Task<GameAssetPrompt> BuildAssetPromptAsync(Guid councilRunId, GameAssetDraftRequest request, string drawerModelName, bool drawsSelf, CancellationToken cancellationToken = default);
}
