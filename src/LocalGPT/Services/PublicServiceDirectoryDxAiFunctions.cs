using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Discovery-only AI capability for catalogued service methods; it never executes or exposes a disabled operation.</summary>
/// <param name="directory">Public service method directory service dependency used by the discover public service methods function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the discover public service methods function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DiscoverPublicServiceMethodsFunction(
    IPublicServiceMethodDirectoryService directory,
    IDxAiFunctionJsonService json,
    ILogger<DiscoverPublicServiceMethodsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the discover public service methods function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="DiscoverPublicServiceMethodsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.public_service.discover", "POST", "/api/dxai/functions/localgpt.public_service.discover/invoke",
        "Search the user's catalog of LocalGPT application service operations by service domain, name or purpose and inspect the exact parameter schemas and approval/exposure status.",
        "query and service are optional search terms; skip defaults 0; take defaults 50 (maximum 200). Output is capability metadata only.",
        "Discovery does not authorize or execute a service method. To invoke one, use its saved catalog key under the central human approval and catalog policy.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","properties":{"query":{"type":["string","null"],"maxLength":200},"service":{"type":["string","null"],"maxLength":200},"skip":{"type":"integer","minimum":0},"take":{"type":"integer","minimum":1,"maximum":200}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="DiscoverPublicServiceMethodsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding discover public service methods function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<DiscoverPublicServiceMethodsRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await directory.SearchAsync(binding.Value.Query, binding.Value.Service, binding.Value.Skip, binding.Value.Take, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public service discovery DXFunction failed; search content omitted.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Public service discovery unavailable. Review application diagnostics." };
        }
    }
}
