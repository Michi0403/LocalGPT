using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services;

/// <summary>Reports local OCR readiness for an installed Ollama vision/OCR model.</summary>
public sealed class VisionOcrCapabilityFunction(
    IWorkspaceVisionOcrService ocr,
    IDxAiFunctionJsonService json,
    ILogger<VisionOcrCapabilityFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.vision.ocr.capability", "POST", "/api/dxai/functions/localgpt.vision.ocr.capability/invoke",
        "Checks configured Ollama hosts for a requested local OCR model and verifies runtime compatibility without processing image content.",
        "JSON: modelName optional; defaults to deepseek-ocr.",
        "Read-only local capability probe. DeepSeek OCR requires a compatible Ollama runtime and installed model.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","properties":{"modelName":{"type":"string","maxLength":160}},"additionalProperties":false}""", IsCoordinationOnly: true);

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
public sealed class WorkspaceVisionOcrFunction(
    IWorkspaceVisionOcrService ocr,
    IDxAiFunctionJsonService json,
    ILogger<WorkspaceVisionOcrFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.vision.ocr.workspace", "POST", "/api/dxai/functions/localgpt.vision.ocr.workspace/invoke",
        "Runs local OCR over one quarantined or promoted image in a LocalGPT chat-upload workspace using an installed Ollama-compatible vision/OCR model.",
        "JSON: workspaceName and relativePath required; modelName, prompt and maximumOutputTokens optional.",
        "Read-only inference over user-uploaded workspace evidence. Arbitrary server paths are rejected and OCR output always requires human review before becoming trusted Knowledge.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "DIHandler", ParameterSchemaJson: """{"type":"object","required":["workspaceName","relativePath"],"properties":{"workspaceName":{"type":"string","minLength":1},"relativePath":{"type":"string","minLength":1},"modelName":{"type":"string","maxLength":160},"prompt":{"type":"string","maxLength":4000},"maximumOutputTokens":{"type":"integer","minimum":1,"maximum":16000}},"additionalProperties":false}""");

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

public sealed class VisionOcrCapabilityParameters
{
    public string ModelName { get; set; } = "deepseek-ocr";
}
