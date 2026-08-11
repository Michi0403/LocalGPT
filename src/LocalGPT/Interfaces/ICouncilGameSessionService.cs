using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council game session service contract.
/// </summary>
public interface ICouncilGameSessionService
{
    event Action<Guid>? Changed;

    /// <summary>
    /// Starts async.
    /// </summary>
    Task<CouncilGameSessionSnapshot> StartAsync(
        StartCouncilGameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets async.
    /// </summary>
    Task<CouncilGameSessionSnapshot?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active async.
    /// </summary>
    Task<CouncilGameSessionSnapshot?> GetActiveAsync(
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the list async operation.
    /// </summary>
    Task<IReadOnlyList<CouncilGameSessionSnapshot>> ListAsync(
        bool includeCompleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the preview control async operation.
    /// </summary>
    Task<CouncilGameDirectorDecision> PreviewControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies control async.
    /// </summary>
    Task<CouncilGameSessionSnapshot> ApplyControlAsync(
        CouncilGameControlRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the submit frame async operation.
    /// </summary>
    Task<CouncilGameSessionSnapshot> SubmitFrameAsync(
        SubmitCouncilGameFrameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets input gate async.
    /// </summary>
    Task<CouncilGameSessionSnapshot> SetInputGateAsync(
        SetCouncilGameInputGateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets control mode async.
    /// </summary>
    Task<CouncilGameSessionSnapshot> SetControlModeAsync(
        Guid sessionId,
        CouncilGameControlMode mode,
        bool autoplayEnabled,
        int autoplayDelayMilliseconds = 1200,
        CancellationToken cancellationToken = default);
}
