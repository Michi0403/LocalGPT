using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for DevExpress AI function behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="IDxAiFunctionHandler"/>.</value>
    DxaichatFunctionInfo Descriptor { get; }

    /// <summary>
    /// Performs invoke for <see cref="IDxAiFunctionHandler"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for DevExpress AI function behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IDxAiFunctionRegistry
{
    /// <summary>
    /// Retrieves functions in the DevExpress AI function directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<DxaichatFunctionInfo> GetFunctions();

    /// <summary>
    /// Performs invoke in the DevExpress AI function directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="functionName">Function name value supplied to the DevExpress AI function operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    Task<DxAiFunctionInvocationResult> InvokeAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);
}
