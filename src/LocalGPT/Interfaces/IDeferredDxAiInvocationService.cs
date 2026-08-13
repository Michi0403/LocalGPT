using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for deferred DevExpress AI invocation behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDeferredDxAiInvocationService
{
    /// <summary>
    /// Performs queue as part of the deferred DevExpress AI invocation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the deferred DevExpress AI invocation operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task QueueAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        Guid approvalRequestId,
        string correlationId,
        Guid? councilRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes approved for heartbeat as part of the deferred DevExpress AI invocation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="councilRunId">Identifier of the council run to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the deferred DevExpress AI invocation operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DeferredDxAiExecutionOutcome>> ExecuteApprovedForHeartbeatAsync(
        Guid councilRunId,
        int councilRound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes approved for approval request as part of the deferred DevExpress AI invocation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    /// <param name="councilRound">Council round value supplied to the deferred DevExpress AI invocation operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<DeferredDxAiExecutionOutcome>> ExecuteApprovedForApprovalRequestAsync(
        Guid approvalRequestId,
        int councilRound = 0,
        CancellationToken cancellationToken = default);
}
