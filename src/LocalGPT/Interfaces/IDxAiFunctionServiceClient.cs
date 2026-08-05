using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IDxAiFunctionServiceClient
{
    Guid? CurrentOperationId { get; }
    IReadOnlyList<DxaichatFunctionInfo> GetFunctions();

    Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        object? parameters = null,
        bool userConfirmed = false,
        bool automaticInvocation = false,
        string requestedBy = "CurrentUser",
        CancellationToken cancellationToken = default);

    Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);

    void Cancel();
    void CancelWithReason(string reason);
}
