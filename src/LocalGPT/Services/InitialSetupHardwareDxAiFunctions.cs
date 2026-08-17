using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Runs local hardware detection through the initial setup orchestration service after human confirmation.</summary>
/// <param name="setup">Initial setup orchestration service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class DetectInitialSetupHardwareFunction(IInitialSetupAssistantService setup, IDxAiFunctionJsonService json, ILogger<DetectInitialSetupHardwareFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes confirmed local hardware detection and persistence.</summary>
    /// <value>The descriptor value exposed by <see cref="DetectInitialSetupHardwareFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.hardware.detect", "POST", "/api/dxai/functions/initial.setup.hardware.detect/invoke",
        "Runs local hardware probes and persists the resulting multi-GPU host profile for setup and Council benchmarking.",
        "endpoint is required.", "Local read-only probing plus durable hardware-profile persistence. Requires fresh human confirmation.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["endpoint"],"properties":{"endpoint":{"type":"string","maxLength":512}},"additionalProperties":false}""");

    /// <summary>Runs confirmed local hardware detection.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<InitialSetupHardwareEndpointDxRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await setup.DetectHardwareAsync(binding.Value.Endpoint, true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Initial setup hardware detection DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Initial setup hardware detection DXFunction failed; hardware values omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Hardware detection failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Imports a bounded local HWiNFO report through the initial setup hardware pipeline after human confirmation.</summary>
/// <param name="setup">Initial setup orchestration service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class ImportInitialSetupHwInfoFunction(IInitialSetupAssistantService setup, IDxAiFunctionJsonService json, ILogger<ImportInitialSetupHwInfoFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes confirmed HWiNFO text import.</summary>
    /// <value>The descriptor value exposed by <see cref="ImportInitialSetupHwInfoFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.hardware.hwinfo.import", "POST", "/api/dxai/functions/initial.setup.hardware.hwinfo.import/invoke",
        "Parses a user-provided local HWiNFO text export and persists the resulting multi-GPU host profile.",
        "endpoint and reportText are required.", "The report stays local to LocalGPT processing but parsed hardware facts are persisted. Requires fresh human confirmation.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["endpoint","reportText"],"properties":{"endpoint":{"type":"string","maxLength":512},"reportText":{"type":"string","maxLength":4194304}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ImportInitialSetupHwInfoFunction"/>, keeping the operation consistent with the state and invariants of the surrounding import initial setup hw info function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<InitialSetupHwInfoDxRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await setup.ImportHwInfoAsync(binding.Value.Endpoint, binding.Value.ReportText, true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Initial setup HWiNFO DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Initial setup HWiNFO DXFunction failed; report/hardware values omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "HWiNFO import failed. Review LocalGPT logs." }; }
    }
}

/// <summary>Persists a user-reviewed accelerator list through the initial setup orchestration service after human confirmation.</summary>
/// <param name="setup">Initial setup orchestration service.</param>
/// <param name="json">DXFunction JSON service.</param>
/// <param name="logger">Logger used for diagnostics.</param>
public sealed class SaveInitialSetupHardwareFunction(IInitialSetupAssistantService setup, IDxAiFunctionJsonService json, ILogger<SaveInitialSetupHardwareFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Describes confirmed multi-GPU list persistence.</summary>
    /// <value>The descriptor value exposed by <see cref="SaveInitialSetupHardwareFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "initial.setup.hardware.save", "POST", "/api/dxai/functions/initial.setup.hardware.save/invoke",
        "Persists a user-reviewed list of GPUs/accelerators for the physical host represented by one provider endpoint.",
        "endpoint and devices are required.", "Persists local setup/benchmark hardware facts and requires fresh human confirmation.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["endpoint","devices"],"properties":{"endpoint":{"type":"string","maxLength":512},"devices":{"type":"array","minItems":1,"maxItems":32,"items":{"type":"object","required":["name"],"properties":{"key":{"type":"string","maxLength":160},"endpoint":{"type":"string","maxLength":512},"hostKey":{"type":"string","maxLength":240},"name":{"type":"string","maxLength":240},"vendor":{"type":"string","maxLength":120},"dedicatedVramGiB":{"type":["number","null"],"minimum":0,"maximum":1024},"source":{"type":"string","maxLength":120},"selected":{"type":"boolean"},"canIRunSlug":{"type":"string","maxLength":120}},"additionalProperties":false}}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="SaveInitialSetupHardwareFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save initial setup hardware function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<InitialSetupHardwareSaveDxRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await setup.SaveHardwareListAsync(binding.Value.Devices, binding.Value.Endpoint, true, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Initial setup hardware-save DXFunction was cancelled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Initial setup hardware-save DXFunction failed; hardware values omitted."); return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Hardware list could not be saved. Review LocalGPT logs." }; }
    }
}

/// <summary>Parameter object for one initial-setup hardware endpoint operation.</summary>
public sealed class InitialSetupHardwareEndpointDxRequest
{
    /// <summary>Gets or sets the provider endpoint whose physical host should be detected.</summary>
    /// <value>The endpoint value exposed by <see cref="InitialSetupHardwareEndpointDxRequest"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>Parameter object for one initial-setup HWiNFO text import.</summary>
public sealed class InitialSetupHwInfoDxRequest
{
    /// <summary>Gets or sets the provider endpoint whose physical host should receive the parsed hardware profile.</summary>
    /// <value>The endpoint value exposed by <see cref="InitialSetupHwInfoDxRequest"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the report text value that forms part of the initial setup hw info DevExpress state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The report text value exposed by <see cref="InitialSetupHwInfoDxRequest"/>.</value>
    public string ReportText { get; set; } = string.Empty;
}

/// <summary>Parameter object for one initial-setup reviewed hardware-list save.</summary>
public sealed class InitialSetupHardwareSaveDxRequest
{
    /// <summary>Gets or sets the provider endpoint whose physical host owns the device list.</summary>
    /// <value>The endpoint value exposed by <see cref="InitialSetupHardwareSaveDxRequest"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the devices collection maintained or exposed by this initial setup hardware save DevExpress instance for downstream processing.
    /// </summary>
    /// <value>The devices value exposed by <see cref="InitialSetupHardwareSaveDxRequest"/>.</value>
    public List<InitialSetupHardwareDevice> Devices { get; set; } = [];
}
