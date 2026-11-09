namespace LocalGPT.BusinessObjects;

/// <summary>
/// Parameter-free view of one approval-gated DXFunction request recorded for a Council run; used as technical
/// delivery evidence by goal review. Parameters, results and payloads are deliberately excluded.
/// </summary>
/// <param name="DeferredInvocationId">Identifier of the deferred invocation.</param>
/// <param name="ApprovalRequestId">Human Collaboration request that gates the invocation.</param>
/// <param name="FunctionName">Requested DXFunction.</param>
/// <param name="Status">Deferred lifecycle status (pending approval, executing, completed, failed, declined).</param>
/// <param name="ResultStatus">Registry result status once executed; empty while pending.</param>
/// <param name="UpdatedAtUtc">Last status change.</param>
public sealed record DeferredDxAiInvocationSummary(
    Guid DeferredInvocationId,
    Guid ApprovalRequestId,
    string FunctionName,
    string Status,
    string ResultStatus,
    DateTime UpdatedAtUtc);
