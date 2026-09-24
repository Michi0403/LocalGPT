using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class LocalAiRuntimeStatusFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiRuntimeStatusFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.runtime.status", "POST", "/api/dxai/functions/localai.runtime.status/invoke",
        "Returns the LocalGPT specialized Python runtime, Python.NET lane, queue and installed-model status without running a model.",
        "No parameters.", "Read-only local runtime state; paths and credentials are not returned.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await runtime.GetStatusAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI runtime status failed with {ExceptionType}; exception text was omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI runtime status could not be loaded." }; }
    }
}

public sealed class LocalAiRuntimeConfigurationFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiRuntimeConfigurationFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.runtime.configuration", "POST", "/api/dxai/functions/localai.runtime.configuration/invoke",
        "Returns the effective LocalGPT-managed Python/Whisper execution policy, queue state and restart requirement.",
        "No parameters.", "Read-only runtime policy. Paths, environment values and credentials are not returned.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await runtime.GetRuntimeConfigurationAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI runtime configuration read failed with {ExceptionType}; exception text was omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI runtime configuration could not be loaded." }; }
    }
}

public sealed class UpdateLocalAiRuntimeConfigurationFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<UpdateLocalAiRuntimeConfigurationFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.runtime.configuration.update", "POST", "/api/dxai/functions/localai.runtime.configuration.update/invoke",
        "Updates LocalGPT's managed Python execution policy including device selection, Whisper defaults, queue capacity, bounded media limits and model caching.",
        "All runtime policy fields are required. Read localai.runtime.configuration first, change only the intended values, then submit the complete policy.",
        "Persistent runtime-policy mutation requiring exact Human Collaboration approval. Queue-capacity changes made after Python.NET initialization take effect after restart.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["defaultDevice","defaultWhisperModel","defaultSpeechLanguage","defaultSpeechTask","defaultSpeechInitialPrompt","queueCapacity","maximumInputMegabytes","maximumImagePixels","maximumAudioSeconds","minimumAudioBytesPerSecond","cacheModels"],"properties":{"defaultDevice":{"type":"string","enum":["auto","cpu","cuda","mps"]},"defaultWhisperModel":{"type":"string","enum":["tiny","base","small","medium","large-v3","turbo"]},"defaultSpeechLanguage":{"type":"string","maxLength":40},"defaultSpeechTask":{"type":"string","enum":["transcribe","translate"]},"defaultSpeechInitialPrompt":{"type":"string","maxLength":4000},"queueCapacity":{"type":"integer","minimum":1,"maximum":1024},"maximumInputMegabytes":{"type":"integer","minimum":1,"maximum":4096},"maximumImagePixels":{"type":"integer","minimum":1000000,"maximum":1000000000},"maximumAudioSeconds":{"type":"integer","minimum":1,"maximum":86400},"minimumAudioBytesPerSecond":{"type":"integer","minimum":1,"maximum":1048576},"cacheModels":{"type":"boolean"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiRuntimeConfigurationChangeRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.UpdateRuntimeConfigurationAsync(binding.Value, userConfirmed: true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI runtime configuration update failed with {ExceptionType}; exception text, prompt text and environment values were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI runtime configuration could not be updated. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiRuntimeProbeFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiRuntimeProbeFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.runtime.probe", "POST", "/api/dxai/functions/localai.runtime.probe/invoke",
        "Runs the fixed Python.NET runtime probe and reports Python/PyTorch device readiness without processing user media.",
        "No parameters.", "Read-only fixed bridge operation; arbitrary Python source cannot be supplied.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { return json.Success(await runtime.ProbeAsync(cancellationToken).ConfigureAwait(false)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI Python.NET probe failed with {ExceptionType}; exception text and runtime paths were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI runtime probe failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiInstalledModelsFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiInstalledModelsFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.models.installed", "POST", "/api/dxai/functions/localai.models.installed/invoke",
        "Lists LocalGPT-managed specialized model installations and their declared capabilities.",
        "No parameters.", "Read-only model inventory. Local filesystem paths are part of the internal model records and should not be repeated to remote peers.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var values = runtime.GetInstalledModels().Select(model => new
            {
                model.InstallationId,
                model.ModelId,
                model.Revision,
                model.Adapter,
                model.Capabilities,
                model.TrustRemoteCode,
                model.SourceProvider,
                model.SourceReference,
                model.RuntimeModelName,
                model.InstalledAtUtc
            }).ToList();
            return Task.FromResult(json.Success(values));
        }
        catch (Exception ex) { logger.LogError("Local AI model inventory failed with {ExceptionType}; exception text was omitted.", ex.GetType().Name); return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI model inventory failed." }); }
    }
}

public sealed class HuggingFaceModelSearchFunction(IHuggingFaceModelCatalogService catalog, IDxAiFunctionJsonService json, ILogger<HuggingFaceModelSearchFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.huggingface.search", "POST", "/api/dxai/functions/localai.huggingface.search/invoke",
        "Searches Hugging Face model metadata for specialized image, video, speech, audio, vision or embedding models without downloading or executing repository code.",
        "query optional; capability optional enum value; limit 1-50.", "Network metadata lookup only. Search does not clone repositories or execute remote code.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"query":{"type":"string","maxLength":300},"capability":{"type":"integer","minimum":0,"maximum":10},"limit":{"type":"integer","minimum":1,"maximum":50}},"additionalProperties":false}""", IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<HuggingFaceModelSearchRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await catalog.SearchAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Hugging Face model metadata search failed with {ExceptionType}; exception text, query and credentials were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Hugging Face model search failed." }; }
    }
}

public sealed class LocalAiModelInstallFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiModelInstallFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.model.install", "POST", "/api/dxai/functions/localai.model.install/invoke",
        "Downloads one reviewed Hugging Face model snapshot into LocalGPT's managed model root and registers its specialized capabilities.",
        "modelId required; revision, adapter, capabilities, trustRemoteCode and requiresAuthentication optional.",
        "Downloads can be large. Human confirmation is required. trustRemoteCode is off unless explicitly approved for this installation.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelId"],"properties":{"modelId":{"type":"string","minLength":1,"maxLength":300},"revision":{"type":"string","maxLength":160},"adapter":{"type":"string","maxLength":80},"capabilities":{"type":"array","maxItems":12,"items":{"type":"integer","minimum":0,"maximum":10}},"trustRemoteCode":{"type":"boolean"},"requiresAuthentication":{"type":"boolean"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiModelInstallRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.InstallModelAsync(binding.Value, userConfirmed: true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI model installation failed with {ExceptionType}; exception text, model identity, token and paths were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI model installation failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiModelRemoveFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiModelRemoveFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.model.remove", "POST", "/api/dxai/functions/localai.model.remove/invoke",
        "Evicts and removes one LocalGPT-managed specialized model installation.",
        "installationId required.", "Destructive managed-model deletion. Human confirmation is required and deletion is constrained to the managed model root.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["installationId"],"properties":{"installationId":{"type":"string","minLength":8,"maxLength":64}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiModelIdRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            await runtime.RemoveModelAsync(binding.Value.InstallationId, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            return json.Success(new { Removed = true, binding.Value.InstallationId });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI model removal failed with {ExceptionType}; exception text, model path and identity were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI model removal failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiImageGenerationFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiImageGenerationFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.image.generate", "POST", "/api/dxai/functions/localai.image.generate/invoke",
        "Generates an image through a capability-bound LocalGPT-managed diffusion model and returns a bounded media artifact URL for Chat/Council review.",
        "modelInstallationId and prompt required; negativePrompt, width, height, steps, guidanceScale and seed optional.",
        "Resource-intensive artifact creation. Human approval is queued non-blockingly for Council use; prompts and generated bytes are omitted from logs.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelInstallationId","prompt"],"properties":{"modelInstallationId":{"type":"string","minLength":8,"maxLength":64},"prompt":{"type":"string","minLength":1,"maxLength":16000},"negativePrompt":{"type":"string","maxLength":8000},"width":{"type":"integer","minimum":256,"maximum":4096},"height":{"type":"integer","minimum":256,"maximum":4096},"steps":{"type":"integer","minimum":1,"maximum":200},"guidanceScale":{"type":"number","minimum":0,"maximum":30},"seed":{"type":"integer"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiImageGenerationRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.GenerateImageAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI image generation failed with {ExceptionType}; exception text, prompt, model identity and media were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local image generation failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiImageEditFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiImageEditFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.image.edit.workspace", "POST", "/api/dxai/functions/localai.image.edit.workspace/invoke",
        "Edits one image from a bounded LocalGPT upload workspace through a capability-bound image-edit model and returns a generated artifact URL.",
        "modelInstallationId, workspaceName, relativePath and prompt required; negativePrompt, width, height, steps, guidanceScale and seed optional.",
        "Resource-intensive artifact creation over user-provided workspace evidence. The private runtime copy is deleted after the job and source media is not logged.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelInstallationId","workspaceName","relativePath","prompt"],"properties":{"modelInstallationId":{"type":"string","minLength":8,"maxLength":64},"workspaceName":{"type":"string","minLength":1,"maxLength":200},"relativePath":{"type":"string","minLength":1,"maxLength":500},"prompt":{"type":"string","minLength":1,"maxLength":16000},"negativePrompt":{"type":"string","maxLength":8000},"width":{"type":"integer","minimum":256,"maximum":4096},"height":{"type":"integer","minimum":256,"maximum":4096},"steps":{"type":"integer","minimum":1,"maximum":200},"guidanceScale":{"type":"number","minimum":0,"maximum":30},"seed":{"type":"integer"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiImageEditRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.EditWorkspaceImageAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI image editing failed with {ExceptionType}; exception text, prompt, model identity, workspace path and media were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local image editing failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiVideoGenerationFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiVideoGenerationFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.video.generate", "POST", "/api/dxai/functions/localai.video.generate/invoke",
        "Generates a video through a capability-bound LocalGPT-managed diffusion model and returns a bounded media artifact URL for Chat/Council review.",
        "modelInstallationId and prompt required; width, height, frames, steps, guidanceScale, framesPerSecond and seed optional.",
        "High-cost artifact creation. Human approval is queued non-blockingly for Council use; prompts and generated bytes are omitted from logs.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelInstallationId","prompt"],"properties":{"modelInstallationId":{"type":"string","minLength":8,"maxLength":64},"prompt":{"type":"string","minLength":1,"maxLength":16000},"width":{"type":"integer","minimum":256,"maximum":1920},"height":{"type":"integer","minimum":256,"maximum":1080},"frames":{"type":"integer","minimum":8,"maximum":241},"steps":{"type":"integer","minimum":1,"maximum":200},"guidanceScale":{"type":"number","minimum":0,"maximum":30},"framesPerSecond":{"type":"integer","minimum":1,"maximum":60},"seed":{"type":"integer"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiVideoGenerationRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.GenerateVideoAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI video generation failed with {ExceptionType}; exception text, prompt, model identity and media were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local video generation failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiImageToVideoFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiImageToVideoFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.video.from-image.workspace", "POST", "/api/dxai/functions/localai.video.from-image.workspace/invoke",
        "Generates a video from one image in a bounded LocalGPT upload workspace through a capability-bound image-to-video model.",
        "modelInstallationId, workspaceName and relativePath required; prompt, width, height, frames, steps, guidanceScale, framesPerSecond and seed optional.",
        "High-cost artifact creation over user-provided workspace evidence. The private runtime copy is deleted after the job and source media is not logged.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelInstallationId","workspaceName","relativePath"],"properties":{"modelInstallationId":{"type":"string","minLength":8,"maxLength":64},"workspaceName":{"type":"string","minLength":1,"maxLength":200},"relativePath":{"type":"string","minLength":1,"maxLength":500},"prompt":{"type":"string","maxLength":16000},"width":{"type":"integer","minimum":256,"maximum":1920},"height":{"type":"integer","minimum":256,"maximum":1080},"frames":{"type":"integer","minimum":8,"maximum":241},"steps":{"type":"integer","minimum":1,"maximum":200},"guidanceScale":{"type":"number","minimum":0,"maximum":30},"framesPerSecond":{"type":"integer","minimum":1,"maximum":60},"seed":{"type":"integer"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiImageToVideoRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.GenerateVideoFromWorkspaceImageAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI image-to-video generation failed with {ExceptionType}; exception text, prompt, model identity, workspace path and media were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local image-to-video generation failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiSpeechRecognitionFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiSpeechRecognitionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.audio.transcribe.workspace", "POST", "/api/dxai/functions/localai.audio.transcribe.workspace/invoke",
        "Transcribes or translates one audio file that already belongs to a bounded LocalGPT upload workspace through a capability-bound Whisper/ASR model.",
        "modelInstallationId, workspaceName and relativePath required; language, task, initialPrompt, device and cacheModel optional and otherwise use the saved runtime policy.",
        "Read-only inference over user-provided workspace evidence. The private runtime copy is deleted in a finally path and raw audio/content, prompts and transcripts are not logged.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["modelInstallationId","workspaceName","relativePath"],"properties":{"modelInstallationId":{"type":"string","minLength":8,"maxLength":64},"workspaceName":{"type":"string","minLength":1,"maxLength":200},"relativePath":{"type":"string","minLength":1,"maxLength":500},"language":{"type":"string","maxLength":40},"task":{"type":"string","enum":["transcribe","translate"]},"initialPrompt":{"type":"string","maxLength":4000},"device":{"type":"string","enum":["auto","cpu","cuda","mps"]},"cacheModel":{"type":"boolean"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiSpeechRecognitionRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await runtime.TranscribeWorkspaceAudioAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Local AI speech recognition failed with {ExceptionType}; exception text, media, transcript, model identity and paths were omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local speech recognition failed. Review LocalGPT logs." }; }
    }
}

public sealed class LocalAiRuntimeCacheClearFunction(ILocalAiRuntimeService runtime, IDxAiFunctionJsonService json, ILogger<LocalAiRuntimeCacheClearFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.runtime.cache.clear", "POST", "/api/dxai/functions/localai.runtime.cache.clear/invoke",
        "Unloads cached specialized Python model pipelines and releases available accelerator cache without deleting installed model snapshots or artifacts.",
        "No parameters.", "Runtime-memory operation only. It is direct-invocation only so an AI cannot repeatedly unload models during a Council run.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await runtime.ClearModelCacheAsync(cancellationToken).ConfigureAwait(false);
            return json.Success(new { status = "Completed", message = "Local AI model cache unloaded." });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { logger.LogError("Clearing the Local AI model cache failed with {ExceptionType}; exception text was omitted.", ex.GetType().Name); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local AI model cache could not be cleared." }; }
    }
}

public sealed class LocalAiModelIdRequest
{
    public string InstallationId { get; set; } = string.Empty;
}
