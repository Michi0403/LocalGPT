using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IHumanCollaborationService
{
    event Action? Changed;
    event Action<HumanCouncilContribution>? DirectUserMessageQueued;

    Task<HumanCollaborationSnapshot> GetSnapshotAsync(
        bool includeResolved = true,
        int take = 80,
        CancellationToken cancellationToken = default);

    Task<HumanApprovalGateResult> AuthorizeOrEnqueueAsync(
        HumanApprovalRequestSpec request,
        bool directHumanConfirmation = false,
        CancellationToken cancellationToken = default);

    Task<HumanCollaborationRequest?> ResolveRequestAsync(
        Guid requestId,
        HumanDecisionSubmission submission,
        CancellationToken cancellationToken = default);

    Task<HumanCouncilParticipantProfile> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<HumanCouncilParticipantProfile> SaveProfileAsync(
        HumanCouncilParticipantProfile profile,
        CancellationToken cancellationToken = default);

    Task<HumanCouncilContribution> QueueContributionAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default);

    Task<HumanCouncilContribution> QueueUserMessageAsync(
        Guid councilRunId,
        string content,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HumanCouncilContribution>> ReadQueuedContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HumanCouncilContribution>> DrainContributionsAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    Task MarkContributionsEvaluatedAsync(
        Guid councilRunId,
        int afterRound,
        string evaluation,
        CancellationToken cancellationToken = default);

    Task<string> BuildCouncilBriefingAsync(
        Guid councilRunId,
        int currentRound,
        CancellationToken cancellationToken = default);

    Task<bool> HasRequiredPendingInputAsync(
        Guid councilRunId,
        CancellationToken cancellationToken = default);

    void BeginCouncilRun(Guid runId, IReadOnlyList<string> members);
    void UpdateCouncilRun(Guid runId, int currentRound, string phase, bool isWaitingForFinalHumanInput = false);
    void EndCouncilRun(Guid runId);
}
