using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// DXFunction <c>localgpt.configuration.repair.request</c>. Coordination function of the configuration steward: it
/// recomputes the named finding from live data and puts its options into the common Human Collaboration approval
/// queue. The same exact request is queued as a deferred invocation, so approving one option in Approvals &amp; team
/// applies it once (compare-and-set) without another model call. The caller cannot supply values or options.
/// </summary>
/// <param name="json">DXFunction JSON helper.</param>
/// <param name="steward">Configuration steward.</param>
/// <param name="deferredInvocations">Deferred execution store that runs this request again after approval.</param>
/// <param name="ambientContext">Current Council run and actor.</param>
/// <param name="logger">Logger for function diagnostics.</param>
public sealed class RequestConfigurationRepairFunction(
    IDxAiFunctionJsonService json,
    IConfigurationStewardService steward,
    IDeferredDxAiInvocationService deferredInvocations,
    IAmbientLocalGptContext ambientContext,
    ILogger<RequestConfigurationRepairFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Registered function name.</summary>
    private const string FunctionName = "localgpt.configuration.repair.request";

    /// <summary>Gets the function descriptor.</summary>
    /// <value>Descriptor of <c>localgpt.configuration.repair.request</c>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        FunctionName,
        "POST",
        "/api/dxai/functions/localgpt.configuration.repair.request/invoke",
        "Ask the local human to decide how to repair one configuration finding from localgpt.configuration.diagnose. The finding's options appear as selectable answers in Approvals & team; the approved option is applied once.",
        "JSON parameters: findingKey required (exact key from localgpt.configuration.diagnose).",
        "Writes nothing by itself. The finding and its options are recomputed from live data; the approved option is applied only if every stored value still equals the observed one. Declining or choosing 'keep' changes nothing.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"findingKey":{"type":"string","minLength":1,"maxLength":200}},"required":["findingKey"],"additionalProperties":false}
        """,
        IsCoordinationOnly: true);

    /// <summary>Requests (or, after approval, completes) the repair of one finding.</summary>
    /// <param name="request">Invocation request with the finding key.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The repair outcome.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ConfigurationRepairRequestParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            if (string.IsNullOrWhiteSpace(binding.Value.FindingKey))
                return json.InvalidParameters("findingKey is required.");
            var outcome = await steward.RequestRepairAsync(binding.Value.FindingKey, request.RequestedBy ?? string.Empty, cancellationToken).ConfigureAwait(false);
            if (outcome.Status == ConfigurationRepairOutcome.AwaitingUserDecision && outcome.ApprovalRequestId is Guid approvalRequestId)
            {
                // Approving in the card runs this exact request again through the deferred store; the second call
                // finds the approved decision with the chosen option and applies it once.
                await deferredInvocations.QueueAsync(FunctionName, request, approvalRequestId, outcome.CorrelationId,
                    ambientContext.Current.CouncilRunId, cancellationToken).ConfigureAwait(false);
            }
            return json.Success(outcome, outcome.Status);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, $"Service method {nameof(RequestConfigurationRepairFunction)}.{nameof(InvokeAsync)} was canceled.");
            else
                logger.LogError(exception, $"Service method {nameof(RequestConfigurationRepairFunction)}.{nameof(InvokeAsync)} failed.");
            throw;
        }
    }
}
