using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services;

/// <summary>Reports local OCR readiness for an installed Ollama vision/OCR model.</summary>
/// <param name="ocr">Workspace vision ocr service dependency used by the vision OCR capability function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the vision OCR capability function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class VisionOcrCapabilityFunction(
    IWorkspaceVisionOcrService ocr,
    IDxAiFunctionJsonService json,
    ILogger<VisionOcrCapabilityFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the vision OCR capability function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="VisionOcrCapabilityFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.vision.ocr.capability", "POST", "/api/dxai/functions/localgpt.vision.ocr.capability/invoke",
        "Checks configured Ollama hosts for a requested local OCR model and verifies runtime compatibility without processing image content.",
        "JSON: modelName optional; defaults to deepseek-ocr.",
        "Read-only local capability probe. DeepSeek OCR requires a compatible Ollama runtime and installed model.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","properties":{"modelName":{"type":"string","maxLength":160}},"additionalProperties":false}""", IsCoordinationOnly: true);

    /// <summary>
    /// Performs invoke for <see cref="VisionOcrCapabilityFunction"/>, keeping the operation consistent with the state and invariants of the surrounding vision OCR capability function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<VisionOcrCapabilityParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            return json.Success(await ocr.GetCapabilityAsync(binding.Value.ModelName, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "OCR capability DXFunction failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Error = exception.Message };
        }
    }
}

/// <summary>Recognizes text in one image that already belongs to a bounded chat-upload workspace.</summary>
/// <param name="ocr">Workspace vision ocr service dependency used by the workspace vision OCR function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the workspace vision OCR function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WorkspaceVisionOcrFunction(
    IWorkspaceVisionOcrService ocr,
    IDxAiFunctionJsonService json,
    ILogger<WorkspaceVisionOcrFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the workspace vision OCR function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="WorkspaceVisionOcrFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.vision.ocr.workspace", "POST", "/api/dxai/functions/localgpt.vision.ocr.workspace/invoke",
        "Runs local OCR over one quarantined or promoted image in a LocalGPT chat-upload workspace using an installed Ollama-compatible vision/OCR model.",
        "JSON: workspaceName and relativePath required; modelName, prompt and maximumOutputTokens optional.",
        "Read-only inference over user-uploaded workspace evidence. Arbitrary server paths are rejected and OCR output always requires human review before becoming trusted Knowledge.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["workspaceName","relativePath"],"properties":{"workspaceName":{"type":"string","minLength":1},"relativePath":{"type":"string","minLength":1},"modelName":{"type":"string","maxLength":160},"prompt":{"type":"string","maxLength":4000},"maximumOutputTokens":{"type":"integer","minimum":1,"maximum":16000}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="WorkspaceVisionOcrFunction"/>, keeping the operation consistent with the state and invariants of the surrounding workspace vision OCR function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<WorkspaceImageOcrRequest>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            return json.Success(await ocr.RecognizeWorkspaceImageAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Workspace OCR DXFunction failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Error = exception.Message };
        }
    }
}

/// <summary>
/// Represents a vision OCR capability parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class VisionOcrCapabilityParameters
{
    /// <summary>
    /// Gets or sets the model name value that forms part of the vision OCR capability parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="VisionOcrCapabilityParameters"/>.</value>
    public string ModelName { get; set; } = "deepseek-ocr";
}
