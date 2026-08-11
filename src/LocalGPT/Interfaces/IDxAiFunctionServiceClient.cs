using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the DevExpress ai function service client contract.
/// </summary>
public interface IDxAiFunctionServiceClient
{
    Guid? CurrentOperationId { get; }
    /// <summary>
    /// Gets functions.
    /// </summary>
    IReadOnlyList<DxaichatFunctionInfo> GetFunctions();

    /// <summary>
    /// Runs the call async operation.
    /// </summary>
    Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        object? parameters = null,
        bool userConfirmed = false,
        bool automaticInvocation = false,
        string requestedBy = "CurrentUser",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the call async operation.
    /// </summary>
    Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether cel.
    /// </summary>
    void Cancel();
    /// <summary>
    /// Determines whether cel with reason.
    /// </summary>
    void CancelWithReason(string reason);
}
