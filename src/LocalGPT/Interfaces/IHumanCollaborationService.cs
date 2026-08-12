using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the human collaboration service contract.
/// </summary>
public interface IHumanCollaborationService
{
    event Action? Changed;
    event Action<HumanCouncilContribution>? DirectUserMessageQueued;

    /// <summary>
    /// Gets snapshot async.
    /// </summary>
    Task<HumanCollaborationSnapshot> GetSnapshotAsync(
        bool includeResolved = true,
        int take = 80,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the authorize or enqueue async operation.
    /// </summary>
    Task<HumanApprovalGateResult> AuthorizeOrEnqueueAsync(
        HumanApprovalRequestSpec request,
        bool directHumanConfirmation = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves request async.
    /// </summary>
    Task<HumanCollaborationRequest?> ResolveRequestAsync(
        Guid requestId,
        HumanDecisionSubmission submission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets profile async.
    /// </summary>
    Task<HumanCouncilParticipantProfile> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves profile async.
    /// </summary>
    Task<HumanCouncilParticipantProfile> SaveProfileAsync(
        HumanCouncilParticipantProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the queue contribution async operation.
    /// </summary>
    Task<HumanCouncilContribution> QueueContributionAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the queue user message async operation.
    /// </summary>
    Task<HumanCouncilContribution> QueueUserMessageAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>Marks the participant currently owning ordered live presentation as the preferred immediate heartbeat consumer for this Council run.</summary>
    void SetPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey);

    /// <summary>Clears the preferred immediate heartbeat consumer when that participant leaves ordered live presentation.</summary>
    void ClearPreferredDirectUserMessageConsumer(Guid councilRunId, string consumerKey);

    /// <summary>Atomically claims one direct user message for immediate interruption of exactly one active model stream, preferring the participant currently visible in ordered live presentation.</summary>
    bool TryClaimDirectUserMessage(Guid contributionId, Guid councilRunId, string consumerKey);

    /// <summary>
    /// Reads queued contributions async.
    /// </summary>
    Task<IReadOnlyList<HumanCouncilContribution>> ReadQueuedContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the drain contributions async operation.
    /// </summary>
    Task<IReadOnlyList<HumanCouncilContribution>> DrainContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the mark contributions evaluated async operation.
    /// </summary>
    Task MarkContributionsEvaluatedAsync(
        Guid councilRunId,
        int afterRound,
        string evaluation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds council briefing async.
    /// </summary>
    Task<string> BuildCouncilBriefingAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets gate status async.
    /// </summary>
    Task<HumanCollaborationGateStatus> GetGateStatusAsync(
        Guid councilRunId,
        int upcomingRound,
        string upcomingPhase,
        HumanCollaborationBoundary boundary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether required pending input async.
    /// </summary>
    Task<bool> HasRequiredPendingInputAsync(
        Guid councilRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the begin council run operation.
    /// </summary>
    void BeginCouncilRun(Guid runId, IReadOnlyList<string> members);
    /// <summary>
    /// Updates council run.
    /// </summary>
    void UpdateCouncilRun(Guid runId, int currentRound, string phase, bool isWaitingForFinalHumanInput = false);
    /// <summary>
    /// Runs the end council run operation.
    /// </summary>
    void EndCouncilRun(Guid runId);
}
