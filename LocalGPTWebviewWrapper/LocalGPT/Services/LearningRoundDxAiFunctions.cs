using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

public sealed class GetLearningRoundSnapshotFunction(ILearningRoundService learning) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var take = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("takePerSource", out var element) && element.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, 1, 10_000)
            : 200;
        return new DxAiFunctionInvocationResult
        {
            Succeeded = true,
            Status = "Completed",
            Value = await learning.BuildSnapshotAsync(take, cancellationToken).ConfigureAwait(false)
        };
    }
}

public sealed class MaintainLearningRoundKnowledgeFunction(ILearningRoundService learning) : IDxAiFunctionHandler
{
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var maintenance = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? new LearningMaintenanceRequest()
            : request.Parameters.Deserialize<LearningMaintenanceRequest>(JsonOptions) ?? new LearningMaintenanceRequest();
        return new DxAiFunctionInvocationResult
        {
            Succeeded = true,
            Status = "Completed",
            Value = await learning.MaintainAsync(maintenance, cancellationToken).ConfigureAwait(false)
        };
    }
}
