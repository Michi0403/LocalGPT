using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Prepares separately reviewed training and evaluation samples through Human Collaboration.</summary>
public sealed class ModelTrainingKnowledgePrepareFunction(IModelTrainingService training, IDxAiFunctionJsonService json,
    ILogger<ModelTrainingKnowledgePrepareFunction> logger) : IDxAiFunctionHandler
{
    /// <inheritdoc />
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.training.knowledge.prepare", "POST", "/api/dxai/functions/localgpt.training.knowledge.prepare/invoke",
        "Prepare a LoRA dataset from selected Council knowledge revisions, such as reviewed Microsoft documentation, LocalGPT code or PublisherStudio examples. Each answer is taken from the exact approved source revision. Held-out samples are written to a separate evaluation dataset.",
        "Supply modelKey and samples, each with knowledgeId, contentSha256, prompt and evaluation. Include at least two training samples and one evaluation sample.",
        "Requires Human Collaboration approval for the exact samples and writes a new job under the training root. Inspect the train and evaluation dataset before approving a training run.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelKey","samples"],"properties":{"modelKey":{"type":"string","minLength":1},"samples":{"type":"array","minItems":3,"maxItems":256,"items":{"type":"object","required":["knowledgeId","contentSha256","prompt","evaluation"],"properties":{"knowledgeId":{"type":"string","format":"uuid"},"contentSha256":{"type":"string","minLength":64,"maxLength":64},"prompt":{"type":"string","minLength":1,"maxLength":8000},"evaluation":{"type":"boolean"}},"additionalProperties":false}}},"additionalProperties":false}""");

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object
                || !request.Parameters.TryGetProperty("modelKey", out var key) || key.ValueKind != JsonValueKind.String
                || !request.Parameters.TryGetProperty("samples", out var samples) || samples.ValueKind != JsonValueKind.Array
                || samples.GetArrayLength() is < 3 or > 256)
                return json.InvalidParameters("Supply modelKey and 3–256 exact knowledge samples.");

            var items = new List<ModelTrainingKnowledgeSample>();
            foreach (var sample in samples.EnumerateArray())
            {
                if (sample.ValueKind != JsonValueKind.Object || !sample.TryGetProperty("knowledgeId", out var id)
                    || id.ValueKind != JsonValueKind.String || !id.TryGetGuid(out var knowledgeId)
                    || !sample.TryGetProperty("contentSha256", out var hash) || hash.ValueKind != JsonValueKind.String
                    || !sample.TryGetProperty("prompt", out var prompt) || prompt.ValueKind != JsonValueKind.String
                    || !sample.TryGetProperty("evaluation", out var evaluation) || evaluation.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return json.InvalidParameters("Each sample requires knowledgeId, contentSha256, prompt and evaluation.");

                items.Add(new(knowledgeId, hash.GetString()!, prompt.GetString()!, evaluation.GetBoolean()));
            }

            var prepared = await training.BuildKnowledgeDatasetAsync(key.GetString()!, items, cancellationToken).ConfigureAwait(false);
            return json.Success(new { prepared.JobFolder, prepared.TrainSamples, prepared.EvalSamples });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Preparing the approved knowledge training dataset was canceled.");
            throw;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning("The approved knowledge training dataset could not be prepared.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message };
        }
        catch (Exception exception)
        {
            logger.LogError("Preparing the approved knowledge training dataset failed with {ExceptionType}.", exception.GetType().Name);
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Review the LocalGPT training diagnostics." };
        }
    }
}
