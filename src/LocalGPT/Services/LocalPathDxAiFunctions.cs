using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

public sealed class BrowseLocalPathFunction(ILocalPathExplorerService paths) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.path.browse", "POST", "/api/dxai/functions/localgpt.path.browse/invoke",
        "Browses one local folder so Chat or Council can present real filesystem choices instead of guessing paths.",
        "JSON parameters: path optional; includeFiles optional; maxEntries optional 1..1000.",
        "Read-only filesystem metadata. This function cannot create, edit, delete, execute, upload, or approve anything.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"path":{"type":"string","maxLength":2048},"includeFiles":{"type":"boolean"},"maxEntries":{"type":"integer","minimum":1,"maximum":1000}},"additionalProperties":false}
        """);

    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var value = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new LocalPathBrowseRequest()
                : request.Parameters.Deserialize<LocalPathBrowseRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }) ?? new();
            return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = paths.Browse(value) });
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method BrowseLocalPathFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

public sealed class ListLocalPathRootsFunction(ILocalPathExplorerService paths) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.path.roots", "POST", "/api/dxai/functions/localgpt.path.roots/invoke",
        "Lists platform-appropriate local root suggestions for path selection.", "No parameters.",
        "Read-only filesystem metadata. Suggested roots are choices, not permission grants.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = paths.GetSuggestedRoots() });
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ListLocalPathRootsFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
