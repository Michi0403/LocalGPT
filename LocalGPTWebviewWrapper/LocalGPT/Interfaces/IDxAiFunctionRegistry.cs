using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IDxAiFunctionHandler
{
    DxaichatFunctionInfo Descriptor { get; }

    Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDxAiFunctionRegistry
{
    IReadOnlyList<DxaichatFunctionInfo> GetFunctions();

    Task<DxAiFunctionInvocationResult> InvokeAsync(
        string functionName,
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default);
}
