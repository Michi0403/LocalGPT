using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilGameSessionService
{
    event Action<Guid>? Changed;

    Task<CouncilGameSessionSnapshot> StartAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken = default);

    Task<CouncilGameSessionSnapshot?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<CouncilGameSessionSnapshot?> GetActiveAsync(
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CouncilGameSessionSnapshot>> ListAsync(
        bool includeCompleted = false,
        CancellationToken cancellationToken = default);

    Task<CouncilGameDirectorDecision> PreviewControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default);

    Task<CouncilGameSessionSnapshot> ApplyControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default);

    Task<CouncilGameSessionSnapshot> SubmitFrameAsync(
        SubmitCouncilGameFrameRequest request,
        CancellationToken cancellationToken = default);

    Task<CouncilGameSessionSnapshot> SetInputGateAsync(
        SetCouncilGameInputGateRequest request,
        CancellationToken cancellationToken = default);

    Task<CouncilGameSessionSnapshot> SetControlModeAsync(
        Guid sessionId,
        CouncilGameControlMode mode,
        bool autoplayEnabled,
        int autoplayDelayMilliseconds = 1200,
        CancellationToken cancellationToken = default);
}
