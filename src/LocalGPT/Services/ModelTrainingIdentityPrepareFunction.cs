using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalGPT.Services;

/// <summary>Writes an identity training dataset (train.jsonl and eval.jsonl) into a new job folder after human approval.</summary>
/// <param name="training">Model training service that runs the packaged trainer and owns every tunable value.</param>
/// <param name="json">Devexpress ai function json service used to shape the invocation result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ModelTrainingIdentityPrepareFunction(IModelTrainingService training, IDxAiFunctionJsonService json, ILogger<ModelTrainingIdentityPrepareFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the model training identity prepare function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ModelTrainingIdentityPrepareFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.training.identity.prepare", "POST", "/api/dxai/functions/localgpt.training.identity.prepare/invoke",
        "Builds a small identity dataset that teaches a derived model who it is and where it runs: application name and version, machine, operating system, user-data layout, configured Council teams and roles, registered toolchains and connected 1-Wire peers, plus the habit of calling knowledge tools for exact versions. Returns the new job folder and the sample counts so the files can be inspected before training.",
        "modelKey is required: the name of the derived model the dataset teaches.",
        "Writes two dataset files into a new folder below the LocalGPT training root and requires Human Collaboration approval. Only the listed facts are used; no secrets, no user file contents and no private notes.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelKey"],"properties":{"modelKey":{"type":"string","minLength":1}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ModelTrainingIdentityPrepareFunction"/>, keeping the operation consistent with the state and invariants of the surrounding model training identity prepare function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var modelKey = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("modelKey", out var keyElement) && keyElement.ValueKind == JsonValueKind.String
                ? keyElement.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(modelKey))
                return json.InvalidParameters("modelKey is required.");
            var prepared = await training.BuildIdentityDatasetAsync(modelKey, cancellationToken).ConfigureAwait(false);
            return json.Success(new { prepared.JobFolder, prepared.TrainSamples, prepared.EvalSamples });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "The model training identity prepare function was canceled.");
            throw;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "The model training identity prepare function was refused or failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = exception.Message };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The model training identity prepare function failed with {ExceptionType}.", exception.GetType().Name);
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "The model training identity prepare function failed. Review LocalGPT logs." };
        }
    }
}
