using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IDeferredDxAiInvocationService
{
    Task QueueAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        Guid approvalRequestId,
        string correlationId,
        Guid? councilRunId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeferredDxAiExecutionOutcome>> ExecuteApprovedForHeartbeatAsync(
        Guid councilRunId,
        int councilRound,
        CancellationToken cancellationToken = default);
}
