using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class GetEmbeddedHardwareCatalogFunction(
    IEmbeddedHardwareCatalogService catalog,
    IDxAiFunctionJsonService json) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.catalog.get", "GET", "/api/dxai/functions/embedded.catalog.get/invoke",
        "Returns installed ESP32/Arduino board profiles, pin capabilities, transport descriptors, and the PublisherStudio wiring-canvas contract.",
        "No parameters are required.",
        "Catalog data is guidance. Exact hardware, voltage, boot-strap and board schematic review remains mandatory before build or flash.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) =>
        json.Success(await catalog.GetCatalogAsync(cancellationToken).ConfigureAwait(false));
}

public sealed class CreateEmbeddedWiringDraftFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedWiringService wiring) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.wiring.draft.create", "POST", "/api/dxai/functions/embedded.wiring.draft.create/invoke",
        "Creates a canvas-neutral board and pin draft that Chat can review now and PublisherStudio can later render as clickable pins, wires, OpenSCAD-linked parts, and animated signal arrows.",
        "boardProfileKey and optional name.",
        "Creates only an in-memory draft. It does not change files, power hardware, compile, flash, or infer an exact board from a family placeholder.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"boardProfileKey":{"type":"string"},"name":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedWiringDraftCreateRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        return json.Success(await wiring.CreateDraftAsync(binding.Value.BoardProfileKey, binding.Value.Name, cancellationToken).ConfigureAwait(false));
    }
}

public sealed class ValidateEmbeddedWiringFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedWiringService wiring) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.wiring.validate", "POST", "/api/dxai/functions/embedded.wiring.validate/invoke",
        "Validates a transport-neutral wiring graph against board pin roles, voltage, direction, shared-bus, ground-path, and PublisherStudio canvas constraints.",
        "draft plus optional requireGroundPath and requireBoardPinProfileMatch.",
        "Deterministic validation is not an electrical certification. Danger findings block firmware artifact approval.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["draft"],"properties":{"draft":{"type":"object"},"requireGroundPath":{"type":"boolean"},"requireBoardPinProfileMatch":{"type":"boolean"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedWiringValidationRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        return json.Success(await wiring.ValidateAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    }
}

public sealed class PlanEmbeddedFirmwareFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedFirmwarePlanningService planning,
    ILogger<PlanEmbeddedFirmwareFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.firmware.plan", "POST", "/api/dxai/functions/embedded.firmware.plan/invoke",
        "Creates a deterministic, reviewable ESP32 or Arduino GPIO, sensor, protocol, wiring, firmware, and LocalGPT telemetry plan from user requirements or a Council-selected pin layout.",
        "EmbeddedFirmwarePlanRequest: board profile, pins, sensors, protocol bindings, optional wiring draft and telemetry transport.",
        "Planning only. It never compiles, flashes, opens a serial port, powers hardware, or forces physical/logical 1-Wire when I2C, SPI, UART, CAN, RS-485, analog, digital, HTTP, MQTT or a custom gateway is more appropriate.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"deviceName":{"type":"string"},"boardFamily":{"type":"string"},"boardName":{"type":"string"},"boardProfileKey":{"type":"string"},"framework":{"type":"string"},"telemetryTransport":{"type":"string"},"baudRate":{"type":"integer"},"telemetryIntervalMilliseconds":{"type":"integer"},"pins":{"type":"array"},"sensors":{"type":"array"},"protocolBindings":{"type":"array"},"wiringDraft":{"type":["object","null"]},"additionalRequirements":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedFirmwarePlanRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        var result = await planning.CreatePlanAsync(binding.Value, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DXAIFunction created embedded firmware plan {PlanId} with status {Status}.", result.PlanId, result.OverallStatus);
        return json.Success(result);
    }
}

public sealed class CreateEmbeddedFirmwareArtifactsFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedFirmwarePlanningService planning,
    ILogger<CreateEmbeddedFirmwareArtifactsFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.firmware.artifacts.create", "POST", "/api/dxai/functions/embedded.firmware.artifacts.create/invoke",
        "Writes an approved firmware plan as source, PlatformIO configuration, wiring review, protocol contracts, plan JSON and ZIP in LocalGPT's per-user artifact directory.",
        "EmbeddedFirmwarePlanRequest.",
        "Requires fresh human approval and refuses danger-level plans. It does not compile, flash, access a serial port, or execute generated code.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"deviceName":{"type":"string"},"boardFamily":{"type":"string"},"boardName":{"type":"string"},"boardProfileKey":{"type":"string"},"framework":{"type":"string"},"telemetryTransport":{"type":"string"},"baudRate":{"type":"integer"},"telemetryIntervalMilliseconds":{"type":"integer"},"pins":{"type":"array"},"sensors":{"type":"array"},"protocolBindings":{"type":"array"},"wiringDraft":{"type":["object","null"]},"additionalRequirements":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedFirmwarePlanRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        var result = await planning.CreateArtifactsAsync(binding.Value, userConfirmed: true, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Approved DXAIFunction created embedded firmware artifacts for plan {PlanId}; paths omitted from logs.", result.PlanId);
        return json.Success(result);
    }
}

public sealed class PreviewEmbeddedTelemetryFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedTelemetryBridgeService bridge) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.telemetry.preview", "POST", "/api/dxai/functions/embedded.telemetry.preview/invoke",
        "Normalizes bounded ESP32/Arduino sensor readings into the untrusted edge telemetry packet expected by a LocalGPT gateway.",
        "deviceId, boardProfileKey, transportKey, sequence, timestamp, readings and optional metadataJson.",
        "Preview only. The edge packet is not a trusted LocalGPT command and is not dispatched.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceId","readings"],"properties":{"deviceId":{"type":"string"},"boardProfileKey":{"type":"string"},"transportKey":{"type":"string"},"sequence":{"type":"integer"},"deviceTimestampMilliseconds":{"type":"integer"},"readings":{"type":"array"},"targetPeerId":{"type":"string"},"metadataJson":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedTelemetryBridgeRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        return json.Success(await bridge.PreviewAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    }
}

public sealed class PreviewEmbeddedOneWireEnvelopeFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedTelemetryBridgeService bridge) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.telemetry.onewire-envelope.preview", "POST", "/api/dxai/functions/embedded.telemetry.onewire-envelope.preview/invoke",
        "Creates a non-dispatched preview showing how a validated embedded edge packet maps into LocalGPT's protected logical 1-Wire work envelope.",
        "EmbeddedTelemetryBridgeRequest.",
        "The preview is unsigned and is never dispatched. Actual peers must use the normal linked, authenticated, replay-protected 1-Wire transport.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceId","readings"],"properties":{"deviceId":{"type":"string"},"boardProfileKey":{"type":"string"},"transportKey":{"type":"string"},"sequence":{"type":"integer"},"deviceTimestampMilliseconds":{"type":"integer"},"readings":{"type":"array"},"targetPeerId":{"type":"string"},"metadataJson":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedTelemetryBridgeRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        return json.Success(await bridge.CreateOneWireEnvelopeAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    }
}

public sealed class PublishEmbeddedTelemetryFunction(
    IDxAiFunctionJsonService json,
    IEmbeddedTelemetryIngressService ingress) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "embedded.sensor.telemetry.publish", "POST", "/api/dxai/functions/embedded.sensor.telemetry.publish/invoke",
        "Accepts one validated, bounded embedded sensor batch from Chat, a local gateway, or an approved logical 1-Wire peer and records it in the recent in-memory telemetry window.",
        "EmbeddedTelemetryBridgeRequest.",
        "Does not execute actuator commands, compile or flash firmware, and does not persist raw readings to the knowledge database. External invocation remains subject to normal 1-Wire link and exposure policy.",
        IsReadOnly: false, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["deviceId","readings"],"properties":{"deviceId":{"type":"string"},"boardProfileKey":{"type":"string"},"transportKey":{"type":"string"},"sequence":{"type":"integer"},"deviceTimestampMilliseconds":{"type":"integer"},"readings":{"type":"array"},"targetPeerId":{"type":"string"},"metadataJson":{"type":"string"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<EmbeddedTelemetryBridgeRequest>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        return json.Success(await ingress.PublishAsync(binding.Value, cancellationToken).ConfigureAwait(false));
    }
}
