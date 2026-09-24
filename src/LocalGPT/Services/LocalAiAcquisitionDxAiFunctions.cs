using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lists reviewed direct/GitHub-first local-AI sources independently from provider-bound model hubs.</summary>
public sealed class ListKnownLocalAiSourcesFunction(ILocalAiAcquisitionService acquisition, IDxAiFunctionJsonService json, ILogger<ListKnownLocalAiSourcesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the direct local-AI source catalog function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.sources.known", "POST", "/api/dxai/functions/localai.sources.known/invoke",
        "Lists LocalGPT's reviewed direct/GitHub-first AI projects, runtime adapters and model variants. Model hubs are separate optional provider integrations.",
        "No parameters.", "Read-only embedded catalog. No network request occurs.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""", IsCoordinationOnly: true);

    /// <summary>Returns the reviewed direct local-AI source catalog.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(json.Success(acquisition.GetKnownModels())); }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Listing reviewed local-AI sources was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Listing reviewed local-AI sources failed."); return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local-AI source catalog could not be listed." }); }
    }
}

/// <summary>Downloads one reviewed local-AI source archive directly over HTTPS after human approval.</summary>
public sealed class DownloadKnownLocalAiSourceFunction(ILocalAiAcquisitionService acquisition, IDxAiFunctionJsonService json, ILogger<DownloadKnownLocalAiSourceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the confirmation-gated direct source download function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.source.download", "POST", "/api/dxai/functions/localai.source.download/invoke",
        "Downloads one reviewed upstream local-AI GitHub source archive directly over HTTPS without Git/GitHub CLI or a model-hub client.",
        "sourceKey is required.", "Network/filesystem mutation requiring human confirmation. Downloaded source is not executed automatically.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceKey"],"properties":{"sourceKey":{"type":"string","minLength":1,"maxLength":160}},"additionalProperties":false}""");

    /// <summary>Downloads one approved source archive.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiKnownModelInstallRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await acquisition.DownloadSourceAsync(binding.Value.SourceKey, userConfirmed: true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Downloading a reviewed local-AI source was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Downloading a reviewed local-AI source failed; source identity, URL and path were omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Local-AI source download failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Installs one executable reviewed direct local-AI model through the managed Python environment after human approval.</summary>
public sealed class InstallKnownLocalAiModelFunction(ILocalAiAcquisitionService acquisition, IDxAiFunctionJsonService json, ILogger<InstallKnownLocalAiModelFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes the confirmation-gated direct local-AI installation function.</summary>
    /// <value>The registered DXFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localai.known.install", "POST", "/api/dxai/functions/localai.known.install/invoke",
        "Downloads reviewed upstream source and model weights and installs an executable bounded local-AI Python adapter. OpenAI Whisper is supported in this release.",
        "sourceKey and variant are required.", "Potentially large network/Python mutation requiring human confirmation. The source key must exist in LocalGPT's reviewed catalog.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceKey","variant"],"properties":{"sourceKey":{"type":"string","minLength":1,"maxLength":160},"variant":{"type":"string","minLength":1,"maxLength":80}},"additionalProperties":false}""");

    /// <summary>Installs one approved known local-AI model.</summary>
    /// <param name="request">DXFunction request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The standard invocation result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<LocalAiKnownModelInstallRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            binding.Value.UserConfirmed = true;
            return json.Success(await acquisition.InstallKnownModelAsync(binding.Value, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Installing a reviewed direct local-AI model was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Installing a reviewed direct local-AI model failed; source, model identity and paths were omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Direct local-AI model installation failed. Review LocalGPT logs." }; }
    }
}
