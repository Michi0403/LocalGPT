using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Registers a knowledge-backed provider loopback endpoint in the existing LocalGPT provider configuration after human confirmation.</summary>
/// <param name="providers">Provider bootstrap service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class ConfigureProviderBootstrapFunction(IAiProviderBootstrapService providers, IDxAiFunctionJsonService json, ILogger<ConfigureProviderBootstrapFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes provider endpoint registration.</summary>
    /// <value>The descriptor value exposed by <see cref="ConfigureProviderBootstrapFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.provider.configure", "POST", "/api/dxai/functions/initial.setup.provider.configure/invoke",
        "Registers the selected knowledge-backed loopback provider endpoint in LocalGPT's existing AI provider configuration.",
        "profileKey is required.", "Persists LocalGPT provider configuration. Requires fresh human confirmation; never automatic.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["profileKey"],"properties":{"profileKey":{"type":"string","maxLength":160}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ConfigureProviderBootstrapFunction"/>, keeping the operation consistent with the state and invariants of the surrounding configure provider bootstrap function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProviderProfileActionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var endpoint = await providers.ConfigureEndpointAsync(binding.Value.ProfileKey, true, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Endpoint = endpoint });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Provider configuration DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Provider configuration DXFunction failed; endpoint details omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Provider endpoint registration failed. Review LocalGPT logs." }; }
    }
}
