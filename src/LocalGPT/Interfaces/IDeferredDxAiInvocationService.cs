using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the deferred DevExpress ai invocation service contract.
/// </summary>
public interface IDeferredDxAiInvocationService
{
    /// <summary>
    /// Runs the queue async operation.
    /// </summary>
    Task QueueAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        Guid approvalRequestId,
        string correlationId,
        Guid? councilRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the execute approved for heartbeat async operation.
    /// </summary>
    Task<IReadOnlyList<DeferredDxAiExecutionOutcome>> ExecuteApprovedForHeartbeatAsync(
        Guid councilRunId,
        int councilRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the execute approved for approval request async operation.
    /// </summary>
    Task<IReadOnlyList<DeferredDxAiExecutionOutcome>> ExecuteApprovedForApprovalRequestAsync(
        Guid approvalRequestId,
        int councilRound = 0,
        CancellationToken cancellationToken = default);
}
