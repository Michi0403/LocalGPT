using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the DevExpress ai function handler contract.
/// </summary>
public interface IDxAiFunctionHandler
{
    DxaichatFunctionInfo Descriptor { get; }

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the DevExpress ai function registry contract.
/// </summary>
public interface IDxAiFunctionRegistry
{
    /// <summary>
    /// Gets functions.
    /// </summary>
    IReadOnlyList<DxaichatFunctionInfo> GetFunctions();

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    Task<DxAiFunctionInvocationResult> InvokeAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);
}
