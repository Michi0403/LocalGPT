using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services;

/// <summary>Runs one LoRA training job on a prepared job folder after human approval, producing a new derived model folder.</summary>
/// <param name="training">Model training service that runs the packaged trainer and owns every tunable value.</param>
/// <param name="json">Devexpress ai function json service used to shape the invocation result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ModelTrainingRunFunction(IModelTrainingService training, IDxAiFunctionJsonService json, ILogger<ModelTrainingRunFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the model training run function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ModelTrainingRunFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.training.run", "POST", "/api/dxai/functions/localgpt.training.run/invoke",
        "Trains a LoRA adapter (and a merged copy when the trainer supports it) from a prepared job folder on a safetensors base model. The base model is never changed and nothing is imported into a provider automatically. Run localgpt.training.probe first and train only when it says the model fits.",
        "jobFolder (from localgpt.training.identity.prepare) and baseModelFolder (a local Hugging Face safetensors model folder outside the training root) are required.",
        "Long-running process that uses the GPU or CPU and writes a new output folder inside the job folder; requires Human Collaboration approval. GGUF files are refused as a base and an existing output folder is never overwritten.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["jobFolder","baseModelFolder"],"properties":{"jobFolder":{"type":"string","minLength":1},"baseModelFolder":{"type":"string","minLength":1}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ModelTrainingRunFunction"/>, keeping the operation consistent with the state and invariants of the surrounding model training run function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobFolder = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("jobFolder", out var jobElement) && jobElement.ValueKind == JsonValueKind.String
                ? jobElement.GetString() ?? string.Empty
                : string.Empty;
            var baseModelFolder = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("baseModelFolder", out var baseElement) && baseElement.ValueKind == JsonValueKind.String
                ? baseElement.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(jobFolder) || string.IsNullOrWhiteSpace(baseModelFolder))
                return json.InvalidParameters("jobFolder and baseModelFolder are required.");
            var result = await training.TrainAsync(jobFolder, baseModelFolder, cancellationToken).ConfigureAwait(false);
            return json.Success(JsonNode.Parse(result));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "The model training run function was canceled.");
            throw;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "The model training run function was refused or failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The model training run function failed with {ExceptionType}.", exception.GetType().Name);
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "The model training run function failed. Review LocalGPT logs." };
        }
    }
}
