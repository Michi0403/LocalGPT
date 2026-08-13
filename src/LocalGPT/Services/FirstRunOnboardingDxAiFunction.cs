using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Lets Chat and Council members inspect first-run readiness without changing installer or onboarding state.
/// </summary>
/// <param name="onboarding">Builds the current bounded onboarding status.</param>
[DocumentationUpdated("2.1.20")]
public sealed class GetFirstRunOnboardingStatusFunction(IFirstRunOnboardingService onboarding) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the get first run onboarding status function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="GetFirstRunOnboardingStatusFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.onboarding.status",
        "POST",
        "/api/dxai/functions/localgpt.onboarding.status/invoke",
        "Returns first-run readiness, installed local models, seeded Council teams, model presets, installer profiles, documentation route and safe Chat quick starts.",
        "Set refreshConnectivity to true only when a current loopback provider probe is needed.",
        "Read-only. It never installs, downloads, starts, stops or removes models or repositories.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "FirstRunOnboardingDxAiFunction",
        ParameterSchemaJson: "{\"type\":\"object\",\"properties\":{\"refreshConnectivity\":{\"type\":\"boolean\"}},\"additionalProperties\":false}");

    /// <summary>
    /// Invokes the read-only onboarding status operation.
    /// </summary>
    /// <param name="request">Contains optional JSON parameters for the invocation.</param>
    /// <param name="cancellationToken">Cancels the asynchronous status operation.</param>
    /// <returns>A task that completes with a successful DXFunction result containing onboarding state.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
    try
    {
            var refresh = request.Parameters.ValueKind == System.Text.Json.JsonValueKind.Object
                && request.Parameters.TryGetProperty("refreshConnectivity", out var value)
                && value.ValueKind is System.Text.Json.JsonValueKind.True;
            var status = await onboarding.GetStatusAsync(refresh, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = status
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method GetFirstRunOnboardingStatusFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
