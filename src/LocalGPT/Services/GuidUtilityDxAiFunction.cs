using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Exposes the narrow GUID computation utility to permitted Council roles and MCP discovery.</summary>
/// <param name="utility">Shared identifier computation service.</param>
/// <param name="json">Registered function parameter and result binding service.</param>
/// <param name="logger">Function invocation diagnostics.</param>
public sealed class GuidUtilityDxAiFunction(IGuidUtilityService utility, IDxAiFunctionJsonService json,
    ILogger<GuidUtilityDxAiFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Discoverable computation function; generated identifiers never represent an existing database row.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "identity.guid", "POST", "/api/dxai/functions/identity.guid/invoke",
        "Generate a real unbound GUID for a new solution/class identifier, or validate and normalize a supplied GUID. For existing project/revision identifiers consult the project registry; use the actual creation receipt for a new project.",
        "operation: new or validate; value: optional GUID text for validate.",
        "Pure .NET computation, no guest process, storage, project creation or approval. The returned value does not attest database existence.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["operation"],"properties":{"operation":{"type":"string","enum":["new","validate"]},"value":{"type":["string","null"],"maxLength":100}},"additionalProperties":false}""");

    /// <summary>Invokes only the fixed GUID operation, not arbitrary submitted C#.</summary>
    /// <param name="request">Registered function invocation and its JSON arguments.</param>
    /// <param name="cancellationToken">Cancels before computation.</param>
    /// <returns>Exact utility result in the standard DXFunction envelope.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = json.Bind<GuidUtilityRequest>(request.Parameters);
            if (!binding.Succeeded) return Task.FromResult(json.InvalidParameters(binding.Error));
            return Task.FromResult(json.Success(utility.Execute(binding.Value)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (ArgumentException exception) { return Task.FromResult(json.InvalidParameters(exception.Message)); }
        catch (Exception exception)
        {
            logger.LogError(exception, "GUID utility function failed.");
            return Task.FromResult(new DxAiFunctionInvocationResult { Status = "Failed", Error = "GUID computation failed." });
        }
    }
}
