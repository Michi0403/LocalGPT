using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents a get embedded hardware catalog function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="catalog">Embedded hardware catalog service dependency used by the get embedded hardware catalog function workflow to provide the corresponding application capability.</param>
/// <param name="json">Devexpress ai function json service dependency used by the get embedded hardware catalog function workflow to provide the corresponding application capability.</param>
public sealed class GetEmbeddedHardwareCatalogFunction(
    IEmbeddedHardwareCatalogService catalog,
    IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the get embedded hardware catalog function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="GetEmbeddedHardwareCatalogFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.catalog.get", "GET", "/api/dxai/functions/embedded.catalog.get/invoke",
        "Returns installed ESP32/Arduino board profiles, pin capabilities, transport descriptors, and the PublisherStudio wiring-canvas contract.",
        "No parameters are required.",
        "Catalog data is guidance. Exact hardware, voltage, boot-strap and board schematic review remains mandatory before build or flash.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="GetEmbeddedHardwareCatalogFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get embedded hardware catalog function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return json.Success(await catalog.GetCatalogAsync(cancellationToken).ConfigureAwait(false));
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method GetEmbeddedHardwareCatalogFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a create embedded wiring draft function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the create embedded wiring draft function workflow to provide the corresponding application capability.</param>
/// <param name="wiring">Embedded wiring service dependency used by the create embedded wiring draft function workflow to provide the corresponding application capability.</param>
public sealed class CreateEmbeddedWiringDraftFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedWiringService wiring) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the create embedded wiring draft function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="CreateEmbeddedWiringDraftFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.wiring.draft.create", "POST", "/api/dxai/functions/embedded.wiring.draft.create/invoke",
        "Creates a canvas-neutral board and pin draft that Chat can review now and PublisherStudio can later render as clickable pins, wires, OpenSCAD-linked parts, and animated signal arrows.",
        "boardProfileKey and optional name.",
        "Creates only an in-memory draft. It does not change files, power hardware, compile, flash, or infer an exact board from a family placeholder.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"boardProfileKey":{"type":"string"},"name":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="CreateEmbeddedWiringDraftFunction"/>, keeping the operation consistent with the state and invariants of the surrounding create embedded wiring draft function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedWiringDraftCreateRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await wiring.CreateDraftAsync(binding.Value.BoardProfileKey, binding.Value.Name, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method CreateEmbeddedWiringDraftFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a validate embedded wiring function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the validate embedded wiring function workflow to provide the corresponding application capability.</param>
/// <param name="wiring">Embedded wiring service dependency used by the validate embedded wiring function workflow to provide the corresponding application capability.</param>
public sealed class ValidateEmbeddedWiringFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedWiringService wiring) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the validate embedded wiring function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ValidateEmbeddedWiringFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.wiring.validate", "POST", "/api/dxai/functions/embedded.wiring.validate/invoke",
        "Validates a transport-neutral wiring graph against board pin roles, voltage, direction, shared-bus, ground-path, and PublisherStudio canvas constraints.",
        "draft plus optional requireGroundPath and requireBoardPinProfileMatch.",
        "Deterministic validation is not an electrical certification. Danger findings block firmware artifact approval.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["draft"],"properties":{"draft":{"type":"object"},"requireGroundPath":{"type":"boolean"},"requireBoardPinProfileMatch":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ValidateEmbeddedWiringFunction"/>, keeping the operation consistent with the state and invariants of the surrounding validate embedded wiring function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedWiringValidationRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await wiring.ValidateAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ValidateEmbeddedWiringFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a plan embedded firmware function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the plan embedded firmware function workflow to provide the corresponding application capability.</param>
/// <param name="planning">Embedded firmware planning service dependency used by the plan embedded firmware function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PlanEmbeddedFirmwareFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedFirmwarePlanningService planning,
    ILogger<PlanEmbeddedFirmwareFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the plan embedded firmware function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PlanEmbeddedFirmwareFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.firmware.plan", "POST", "/api/dxai/functions/embedded.firmware.plan/invoke",
        "Creates a deterministic, reviewable ESP32 or Arduino GPIO, sensor, protocol, wiring, firmware, and LocalGPT telemetry plan from user requirements or a Council-selected pin layout.",
        "EmbeddedFirmwarePlanRequest: board profile, pins, sensors, protocol bindings, optional wiring draft and telemetry transport.",
        "Planning only. It never compiles, flashes, opens a serial port, powers hardware, or forces physical/logical 1-Wire when I2C, SPI, UART, CAN, RS-485, analog, digital, HTTP, MQTT or a custom gateway is more appropriate.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"deviceName":{"type":"string"},"boardFamily":{"type":"string"},"boardName":{"type":"string"},"boardProfileKey":{"type":"string"},"framework":{"type":"string"},"telemetryTransport":{"type":"string"},"baudRate":{"type":"integer"},"telemetryIntervalMilliseconds":{"type":"integer"},"pins":{"type":"array"},"sensors":{"type":"array"},"protocolBindings":{"type":"array"},"wiringDraft":{"type":["object","null"]},"additionalRequirements":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="PlanEmbeddedFirmwareFunction"/>, keeping the operation consistent with the state and invariants of the surrounding plan embedded firmware function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedFirmwarePlanRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var result = await planning.CreatePlanAsync(binding.Value, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction created embedded firmware plan {PlanId} with status {Status}.", result.PlanId, result.OverallStatus);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PlanEmbeddedFirmwareFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PlanEmbeddedFirmwareFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a create embedded firmware artifacts function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the create embedded firmware artifacts function workflow to provide the corresponding application capability.</param>
/// <param name="planning">Embedded firmware planning service dependency used by the create embedded firmware artifacts function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CreateEmbeddedFirmwareArtifactsFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedFirmwarePlanningService planning,
    ILogger<CreateEmbeddedFirmwareArtifactsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the create embedded firmware artifacts function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="CreateEmbeddedFirmwareArtifactsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.firmware.artifacts.create", "POST", "/api/dxai/functions/embedded.firmware.artifacts.create/invoke",
        "Writes an approved firmware plan as source, PlatformIO configuration, wiring review, protocol contracts, plan JSON and ZIP in LocalGPT's per-user artifact directory.",
        "EmbeddedFirmwarePlanRequest.",
        "Requires fresh human approval and refuses danger-level plans. It does not compile, flash, access a serial port, or execute generated code.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"deviceName":{"type":"string"},"boardFamily":{"type":"string"},"boardName":{"type":"string"},"boardProfileKey":{"type":"string"},"framework":{"type":"string"},"telemetryTransport":{"type":"string"},"baudRate":{"type":"integer"},"telemetryIntervalMilliseconds":{"type":"integer"},"pins":{"type":"array"},"sensors":{"type":"array"},"protocolBindings":{"type":"array"},"wiringDraft":{"type":["object","null"]},"additionalRequirements":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="CreateEmbeddedFirmwareArtifactsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding create embedded firmware artifacts function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedFirmwarePlanRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var result = await planning.CreateArtifactsAsync(binding.Value, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved DXAIFunction created embedded firmware artifacts for plan {PlanId}; paths omitted from logs.", result.PlanId);
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CreateEmbeddedFirmwareArtifactsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CreateEmbeddedFirmwareArtifactsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a preview embedded telemetry function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the preview embedded telemetry function workflow to provide the corresponding application capability.</param>
/// <param name="bridge">Embedded telemetry bridge service dependency used by the preview embedded telemetry function workflow to provide the corresponding application capability.</param>
public sealed class PreviewEmbeddedTelemetryFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedTelemetryBridgeService bridge) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the preview embedded telemetry function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PreviewEmbeddedTelemetryFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.telemetry.preview", "POST", "/api/dxai/functions/embedded.telemetry.preview/invoke",
        "Normalizes bounded ESP32/Arduino sensor readings into the untrusted edge telemetry packet expected by a LocalGPT gateway.",
        "deviceId, boardProfileKey, transportKey, sequence, timestamp, readings and optional metadataJson.",
        "Preview only. The edge packet is not a trusted LocalGPT command and is not dispatched.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceId","readings"],"properties":{"deviceId":{"type":"string"},"boardProfileKey":{"type":"string"},"transportKey":{"type":"string"},"sequence":{"type":"integer"},"deviceTimestampMilliseconds":{"type":"integer"},"readings":{"type":"array"},"targetPeerId":{"type":"string"},"metadataJson":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="PreviewEmbeddedTelemetryFunction"/>, keeping the operation consistent with the state and invariants of the surrounding preview embedded telemetry function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedTelemetryBridgeRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await bridge.PreviewAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PreviewEmbeddedTelemetryFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a preview embedded one wire envelope function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the preview embedded one wire envelope function workflow to provide the corresponding application capability.</param>
/// <param name="bridge">Embedded telemetry bridge service dependency used by the preview embedded one wire envelope function workflow to provide the corresponding application capability.</param>
public sealed class PreviewEmbeddedOneWireEnvelopeFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedTelemetryBridgeService bridge) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the preview embedded one wire envelope function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PreviewEmbeddedOneWireEnvelopeFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.telemetry.onewire-envelope.preview", "POST", "/api/dxai/functions/embedded.telemetry.onewire-envelope.preview/invoke",
        "Creates a non-dispatched preview showing how a validated embedded edge packet maps into LocalGPT's protected logical 1-Wire work envelope.",
        "EmbeddedTelemetryBridgeRequest.",
        "The preview is unsigned and is never dispatched. Actual peers must use the normal linked, authenticated, replay-protected 1-Wire transport.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceId","readings"],"properties":{"deviceId":{"type":"string"},"boardProfileKey":{"type":"string"},"transportKey":{"type":"string"},"sequence":{"type":"integer"},"deviceTimestampMilliseconds":{"type":"integer"},"readings":{"type":"array"},"targetPeerId":{"type":"string"},"metadataJson":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="PreviewEmbeddedOneWireEnvelopeFunction"/>, keeping the operation consistent with the state and invariants of the surrounding preview embedded one wire envelope function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedTelemetryBridgeRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await bridge.CreateOneWireEnvelopeAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PreviewEmbeddedOneWireEnvelopeFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a publish embedded telemetry function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the publish embedded telemetry function workflow to provide the corresponding application capability.</param>
/// <param name="ingress">Embedded telemetry ingress service dependency used by the publish embedded telemetry function workflow to provide the corresponding application capability.</param>
public sealed class PublishEmbeddedTelemetryFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedTelemetryIngressService ingress) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the publish embedded telemetry function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="PublishEmbeddedTelemetryFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.sensor.telemetry.publish", "POST", "/api/dxai/functions/embedded.sensor.telemetry.publish/invoke",
        "Accepts one validated, bounded embedded sensor batch from Chat, a local gateway, or an approved logical 1-Wire peer and records it in the recent in-memory telemetry window.",
        "EmbeddedTelemetryBridgeRequest.",
        "Does not execute actuator commands, compile or flash firmware, and does not persist raw readings to the knowledge database. External invocation remains subject to normal 1-Wire link and exposure policy.",
        IsReadOnly: false, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceId","readings"],"properties":{"deviceId":{"type":"string"},"boardProfileKey":{"type":"string"},"transportKey":{"type":"string"},"sequence":{"type":"integer"},"deviceTimestampMilliseconds":{"type":"integer"},"readings":{"type":"array"},"targetPeerId":{"type":"string"},"metadataJson":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="PublishEmbeddedTelemetryFunction"/>, keeping the operation consistent with the state and invariants of the surrounding publish embedded telemetry function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<EmbeddedTelemetryBridgeRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            return json.Success(await ingress.PublishAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublishEmbeddedTelemetryFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
