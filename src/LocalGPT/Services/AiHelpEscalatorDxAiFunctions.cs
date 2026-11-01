using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Automatically discoverable Council coordination function for bounded recovery.</summary>
/// <param name="help">Durable recovery service shared with the controller.</param>
/// <param name="json">Existing structured result binder.</param>
/// <param name="logger">Redacted invocation diagnostics.</param>
public sealed class ReportAiHelpAttemptFunction(
    IAiHelpEscalatorService help,
    IDxAiFunctionJsonService json,
    ILogger<ReportAiHelpAttemptFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Seeded runtime catalog descriptor discovered by the shared DI handler scan.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "ai.help.report_attempt", "POST", "/api/dxai/functions/ai.help.report_attempt/invoke",
        "When an assigned task is genuinely stalled, report the actual attempt and receive a bounded alternative, one curator review, or a persistent human question. Never invent missing requirements; do not retry unchanged actions.",
        "taskId stable per goal, task actual goal, action attempted function/operation, status success|failed|no_progress|needs_input, optional functionName (registered DX function), failedModelSelectionKey (provider-qualified), error {code,message,source Agent|Tool|Validator}. No negative prompt or invented mandatory field.",
        "Coordination-only, no execution authority. An await_user decision means STOP this task until the existing Human Collaboration inbox answers. No approval bypass.",
        IsReadOnly: false, IsCoordinationOnly: true, AvailableToAi: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["taskId","task","action","status"],"properties":{"taskId":{"type":"string","minLength":1,"maxLength":160},"task":{"type":"string","minLength":1,"maxLength":2000},"action":{"type":"string","minLength":1,"maxLength":500},"functionName":{"type":"string","maxLength":160},"failedModelSelectionKey":{"type":"string","maxLength":160},"status":{"type":"string","enum":["success","failed","no_progress","needs_input"]},"attemptId":{"type":"string","format":"uuid"},"error":{"type":"object","properties":{"code":{"type":"string","maxLength":120},"message":{"type":"string","maxLength":2000},"source":{"type":"string","enum":["agent","tool","validator"]}},"additionalProperties":false},"userQuestion":{"type":"string","maxLength":1000},"councilRunId":{"type":"string","format":"uuid"}},"additionalProperties":false}
        """);

    /// <summary>Invoke the bounded owned service through normal DX policy.</summary>
    /// <param name="request">Validated tool-call envelope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Next decision, including any durable inbox request ID.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var bound = json.Bind<AiHelpAttemptReport>(request.Parameters);
            if (!bound.Succeeded) return json.InvalidParameters(bound.Error);
            bound.Value.AttemptId ??= request.OperationId;
            return json.Success(await help.ReportAttemptAsync(bound.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException error) { return json.InvalidParameters(error.Message); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error)
        {
            logger.LogError("AI help DX handler failed with {ErrorType}; payload omitted.", error.GetType().Name);
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Recovery service unavailable; inspect LocalGPT logs." };
        }
    }
}

/// <summary>Automatic, read-only task recovery status function; never restarts work.</summary>
/// <param name="help">Shared durable recovery service.</param>
/// <param name="json">Existing structured result binder.</param>
public sealed class InspectAiHelpAttemptFunction(IAiHelpEscalatorService help, IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>Discoverable function descriptor.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "ai.help.status", "POST", "/api/dxai/functions/ai.help.status/invoke",
        "Read the last recovery decision for an existing task without retrying, escalating, or executing anything.",
        "taskId required stable ID; checkHumanResponse optional boolean to refresh a prior inbox question after human response.",
        "Read-only coordination. Cannot override awaiting-user or permission gates.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["taskId"],"properties":{"taskId":{"type":"string","minLength":1,"maxLength":160},"checkHumanResponse":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>Read status and optionally refresh only an already-existing human answer.</summary>
    /// <param name="request">Read-only query envelope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>Durable decision without executing any failed tool.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var bound = json.Bind<AiHelpStatusRequest>(request.Parameters);
        if (!bound.Succeeded) return json.InvalidParameters(bound.Error);
        var decision = bound.Value.CheckHumanResponse
            ? await help.CheckHumanResponseAsync(bound.Value.TaskId, cancellationToken).ConfigureAwait(false)
            : await help.InspectAsync(bound.Value.TaskId, cancellationToken).ConfigureAwait(false);
        return json.Success(decision ?? new AiHelpRecoveryDecision { TaskId = bound.Value.TaskId, Type = "halt", Reason = "No recovery state has been created for this task." });
    }
}

/// <summary>Read-only query for the exact existing recovery state.</summary>
public sealed class AiHelpStatusRequest
{
    /// <summary>Stable task identifier.</summary>
    public string TaskId { get; set; } = string.Empty;
    /// <summary>Whether an already-queued human response should be inspected.</summary>
    public bool CheckHumanResponse { get; set; }
}
