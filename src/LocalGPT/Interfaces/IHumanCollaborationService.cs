using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for human collaboration behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IHumanCollaborationService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="IHumanCollaborationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Occurs when direct user message queued changes or completes in <see cref="IHumanCollaborationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action<HumanCouncilContribution>? DirectUserMessageQueued;

    /// <summary>
    /// Retrieves snapshot as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeResolved">Value indicating whether include resolved should apply to this operation.</param>
    /// <param name="take">Take value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration snapshot produced by the operation.</returns>
    Task<HumanCollaborationSnapshot> GetSnapshotAsync(
        bool includeResolved = true,
        int take = 80,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs authorize or enqueue as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="directHumanConfirmation">Value indicating whether direct human confirmation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human approval gate result produced by the operation.</returns>
    Task<HumanApprovalGateResult> AuthorizeOrEnqueueAsync(
        HumanApprovalRequestSpec request,
        bool directHumanConfirmation = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves request as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestId">Identifier of the request to use for this operation.</param>
    /// <param name="submission">Submission value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration request produced by the operation.</returns>
    Task<HumanCollaborationRequest?> ResolveRequestAsync(
        Guid requestId,
        HumanDecisionSubmission submission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves profile as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council participant profile produced by the operation.</returns>
    Task<HumanCouncilParticipantProfile> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists profile as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council participant profile produced by the operation.</returns>
    Task<HumanCouncilParticipantProfile> SaveProfileAsync(
        HumanCouncilParticipantProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs queue contribution as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    Task<HumanCouncilContribution> QueueContributionAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs queue user message as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="content">Content value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human council contribution produced by the operation.</returns>
    Task<HumanCouncilContribution> QueueUserMessageAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>Marks the participant currently owning ordered live presentation as the preferred immediate heartbeat consumer for this Council run.</summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="consumerKey">Consumer key value supplied to the human collaboration operation and used when producing its result.</param>
    void SetPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey);

    /// <summary>Clears the preferred immediate heartbeat consumer when that participant leaves ordered live presentation.</summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="consumerKey">Consumer key value supplied to the human collaboration operation and used when producing its result.</param>
    void ClearPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey);

    /// <summary>Atomically claims one direct user message for immediate interruption of exactly one active model stream, preferring the participant currently visible in ordered live presentation.</summary>
    /// <param name="contributionId">Identifier of the contribution to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="consumerKey">Consumer key value supplied to the human collaboration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryClaimDirectUserMessage(Guid contributionId, Guid councilRunId, string consumerKey);

    /// <summary>
    /// Reads queued contributions as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<HumanCouncilContribution>> ReadQueuedContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs drain contributions as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<HumanCouncilContribution>> DrainContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs mark contributions evaluated as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="afterRound">After round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="evaluation">Evaluation value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task MarkContributionsEvaluatedAsync(
        Guid councilRunId,
        int afterRound,
        string evaluation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds council briefing as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> BuildCouncilBriefingAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves gate status as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="upcomingRound">Upcoming round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="upcomingPhase">Upcoming phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="boundary">Boundary value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The human collaboration gate status produced by the operation.</returns>
    Task<HumanCollaborationGateStatus> GetGateStatusAsync(
        Guid councilRunId,
        int upcomingRound,
        string upcomingPhase,
        HumanCollaborationBoundary boundary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether required pending input as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> HasRequiredPendingInputAsync(
        Guid councilRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs begin council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="members">String dependency used by the human collaboration workflow to provide the corresponding application capability.</param>
    void BeginCouncilRun(Guid runId, IReadOnlyList<string> members);
    /// <summary>
    /// Updates council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="currentRound">Current round value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the human collaboration operation and used when producing its result.</param>
    /// <param name="isWaitingForFinalHumanInput">Value indicating whether is waiting for final human input should apply to this operation.</param>
    void UpdateCouncilRun(Guid runId, int currentRound, string phase, bool isWaitingForFinalHumanInput = false);
    /// <summary>
    /// Performs end council run as part of the human collaboration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    void EndCouncilRun(Guid runId);
}
