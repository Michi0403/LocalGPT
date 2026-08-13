using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for DevExpress AI function service behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDxAiFunctionServiceClient
{
    /// <summary>
    /// Gets the stable current operation identifier used to identify or correlate this DevExpress AI function service instance with related application state.
    /// </summary>
    /// <value>The current operation identifier value exposed by <see cref="IDxAiFunctionServiceClient"/>.</value>
    Guid? CurrentOperationId { get; }
    /// <summary>
    /// Retrieves functions for <see cref="IDxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<DxaichatFunctionInfo> GetFunctions();

    /// <summary>
    /// Performs call for <see cref="IDxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <param name="requestedBy">Requested by value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        object? parameters = null,
        bool userConfirmed = false,
        bool automaticInvocation = false,
        string requestedBy = "CurrentUser",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs call for <see cref="IDxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> CallAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether cel for <see cref="IDxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    void Cancel();
    /// <summary>
    /// Determines whether cel with reason for <see cref="IDxAiFunctionServiceClient"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function service workflow.
    /// </summary>
    /// <param name="reason">Reason value supplied to the DevExpress AI function service operation and used when producing its result.</param>
    void CancelWithReason(string reason);
}
