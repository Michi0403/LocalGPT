using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Represents a get learning round snapshot function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="learning">Learning round service dependency used by the get learning round snapshot function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GetLearningRoundSnapshotFunction(ILearningRoundService learning, ILogger<GetLearningRoundSnapshotFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the get learning round snapshot function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="GetLearningRoundSnapshotFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.learning.snapshot",
        "POST",
        "/api/dxai/functions/localgpt.learning.snapshot/invoke",
        "Builds a current learning-round evidence snapshot from chat memory, logs, council knowledge and database regexes.",
        "JSON parameters: takePerSource optional integer from 1 to 10000.",
        "Read-only. The snapshot contains local data evidence and grants no execution authority.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"takePerSource":{"type":"integer","minimum":1,"maximum":10000}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="GetLearningRoundSnapshotFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get learning round snapshot function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            logger.LogInformation("Learning-round snapshot DXFunction started.");
            var take = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("takePerSource", out var element) && element.TryGetInt32(out var parsed)
                ? Math.Clamp(parsed, 1, 10_000)
                : 200;
            var result = new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = await learning.BuildSnapshotAsync(take, cancellationToken).ConfigureAwait(false)
            };
            logger.LogInformation("Learning-round snapshot DXFunction completed.");
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetLearningRoundSnapshotFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetLearningRoundSnapshotFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a maintain learning round knowledge function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="learning">Learning round service dependency used by the maintain learning round knowledge function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class MaintainLearningRoundKnowledgeFunction(ILearningRoundService learning, ILogger<MaintainLearningRoundKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="MaintainLearningRoundKnowledgeFunction"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Gets the descriptor value that forms part of the maintain learning round knowledge function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="MaintainLearningRoundKnowledgeFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.learning.maintain",
        "POST",
        "/api/dxai/functions/localgpt.learning.maintain/invoke",
        "Stores model-suggested learning facts and validated regex definitions in LocalGPT's SQLite self-maintenance layer.",
        "JSON parameters: facts array and regexPatterns array; both optional.",
        "Knowledge maintenance only. New facts remain ModelSuggested/NeedsUserReview and regexes are timeout-validated. This function cannot run commands or authorize side effects.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type":"object",
          "properties":{
            "facts":{"type":"array","items":{"type":"object","required":["topic","content"],"properties":{"topic":{"type":"string","maxLength":240},"scope":{"type":"string","maxLength":120},"content":{"type":"string"},"helpfulSources":{"type":"string"},"tags":{"type":"string","maxLength":400},"confidence":{"type":"integer","minimum":0,"maximum":100}},"additionalProperties":false}},
            "regexPatterns":{"type":"array","items":{"type":"object","required":["name","pattern"],"properties":{"name":{"type":"string","maxLength":128},"pattern":{"type":"string","maxLength":16000},"flags":{"type":"string","maxLength":64}},"additionalProperties":false}}
          },
          "additionalProperties":false
        }
        """,
        IsCoordinationOnly: true);

    /// <summary>
    /// Performs invoke for <see cref="MaintainLearningRoundKnowledgeFunction"/>, keeping the operation consistent with the state and invariants of the surrounding maintain learning round knowledge function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            logger.LogInformation("Learning-round maintenance DXFunction started; maintenance payload content was omitted.");
            var maintenance = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new LearningMaintenanceRequest()
                : request.Parameters.Deserialize<LearningMaintenanceRequest>(JsonOptions) ?? new LearningMaintenanceRequest();
            var result = new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = await learning.MaintainAsync(maintenance, cancellationToken).ConfigureAwait(false)
            };
            logger.LogInformation("Learning-round maintenance DXFunction completed.");
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MaintainLearningRoundKnowledgeFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MaintainLearningRoundKnowledgeFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}
