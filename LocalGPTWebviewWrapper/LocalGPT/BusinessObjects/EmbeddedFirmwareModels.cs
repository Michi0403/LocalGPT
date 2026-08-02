using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

public sealed class EmbeddedProtocolKeys
{
    private EmbeddedProtocolKeys() { }
    public const string DigitalGpio = "gpio.digital";
    public const string AnalogAdc = "gpio.analog";
    public const string Pwm = "gpio.pwm";
    public const string PhysicalOneWire = "bus.onewire.physical";
    public const string I2c = "bus.i2c";
    public const string Spi = "bus.spi";
    public const string Uart = "bus.uart";
    public const string Can = "bus.can";
    public const string Rs485 = "bus.rs485";
    public const string SerialJsonLines = "transport.serial-json-lines";
    public const string HttpJson = "transport.http-json";
    public const string Mqtt = "transport.mqtt";
    public const string LocalGptOneWire = "localgpt.onewire-envelope";
    public const string OrganicPeer = "localgpt.organic-peer";
    public const string Custom = "custom";
}

public sealed class EmbeddedFirmwarePlanRequest
{
    [MaxLength(120)] public string DeviceName { get; set; } = "localgpt-embedded-node";
    [MaxLength(120)] public string BoardFamily { get; set; } = "ESP32";
    [MaxLength(160)] public string BoardName { get; set; } = "ESP32 Dev Module";
    [MaxLength(160)] public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    [MaxLength(80)] public string Framework { get; set; } = "Arduino";
    [MaxLength(120)] public string TelemetryTransport { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    public int BaudRate { get; set; } = 115200;
    public int TelemetryIntervalMilliseconds { get; set; } = 5000;
    public List<EmbeddedPinRequirement> Pins { get; set; } = [];
    public List<EmbeddedSensorRequirement> Sensors { get; set; } = [];
    public List<EmbeddedProtocolBinding> ProtocolBindings { get; set; } = [];
    public EmbeddedWiringDraft? WiringDraft { get; set; }
    [MaxLength(8000)] public string AdditionalRequirements { get; set; } = string.Empty;
}

public sealed class EmbeddedPinRequirement
{
    [MaxLength(80)] public string PinKey { get; set; } = string.Empty;
    public int Gpio { get; set; }
    [MaxLength(120)] public string Function { get; set; } = string.Empty;
    [MaxLength(80)] public string Mode { get; set; } = "Input";
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    [MaxLength(120)] public string BusKey { get; set; } = string.Empty;
    [MaxLength(120)] public string SensorKey { get; set; } = string.Empty;
    [MaxLength(80)] public string Metric { get; set; } = string.Empty;
    [MaxLength(80)] public string Unit { get; set; } = "raw";
    public double SupplyVoltage { get; set; } = 3.3;
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
}

public sealed class EmbeddedSensorRequirement
{
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    [MaxLength(160)] public string SensorType { get; set; } = string.Empty;
    [MaxLength(120)] public string DriverKey { get; set; } = string.Empty;
    [MaxLength(120)] public string Interface { get; set; } = "Analog";
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.AnalogAdc;
    [MaxLength(80)] public string PreferredPinKey { get; set; } = string.Empty;
    public int? PreferredGpio { get; set; }
    [MaxLength(120)] public string BusKey { get; set; } = string.Empty;
    [MaxLength(80)] public string Metric { get; set; } = string.Empty;
    [MaxLength(80)] public string Unit { get; set; } = "raw";
    public double SupplyVoltage { get; set; } = 3.3;
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
}

public sealed class EmbeddedProtocolBinding
{
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    [MaxLength(120)] public string Role { get; set; } = "Sensor";
    [MaxLength(120)] public string Direction { get; set; } = "Input";
    public List<string> PinKeys { get; set; } = [];
    [MaxLength(160)] public string TargetController { get; set; } = string.Empty;
    [MaxLength(160)] public string TargetMethod { get; set; } = string.Empty;
    [MaxLength(200)] public string CapabilityKey { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
}

public sealed class EmbeddedFirmwarePlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string DeviceName { get; set; } = string.Empty;
    public string BoardFamily { get; set; } = string.Empty;
    public string BoardName { get; set; } = string.Empty;
    public string BoardProfileKey { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string TelemetryTransport { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int TelemetryIntervalMilliseconds { get; set; }
    public string OverallStatus { get; set; } = "Warning";
    public List<EmbeddedPinAssignment> PinAssignments { get; set; } = [];
    public List<EmbeddedProtocolBinding> ProtocolBindings { get; set; } = [];
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
    public List<string> WiringSteps { get; set; } = [];
    public List<EmbeddedTransportContract> TransportContracts { get; set; } = [];
    public EmbeddedOneWireContract OneWireContract { get; set; } = new();
    public EmbeddedWiringValidationResult? WiringValidation { get; set; }
    public EmbeddedWiringDraft? WiringDraft { get; set; }
    public string ArduinoSketch { get; set; } = string.Empty;
    public string PlatformIoConfiguration { get; set; } = string.Empty;
    public string WiringMarkdown { get; set; } = string.Empty;
    public string LearningRoundAdvice { get; set; } = string.Empty;
}

public sealed class EmbeddedPinAssignment
{
    public string PinKey { get; set; } = string.Empty;
    public int Gpio { get; set; }
    public string Function { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string ProtocolKey { get; set; } = string.Empty;
    public string BusKey { get; set; } = string.Empty;
    public string SensorKey { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double SupplyVoltage { get; set; }
    public string Status { get; set; } = "Approved";
    public string Notes { get; set; } = string.Empty;
}

public sealed record EmbeddedPlanFinding(string Severity, string Code, string Message, int? Gpio = null, string PinKey = "");

public sealed class EmbeddedTransportContract
{
    public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    public string DisplayName { get; set; } = string.Empty;
    public string Direction { get; set; } = "DeviceToLocalGpt";
    public string Boundary { get; set; } = string.Empty;
    public string Controller { get; set; } = "EmbeddedTelemetry";
    public string Method { get; set; } = "PublishSensorReading";
    public string CapabilityKey { get; set; } = "embedded.sensor.telemetry.publish";
    public bool RequiresGateway { get; set; } = true;
    public bool RequiresOneWireSecurity { get; set; } = true;
    public string ExampleEnvelopeJson { get; set; } = string.Empty;
}

public sealed class EmbeddedOneWireContract
{
    public string ProtocolVersion { get; set; } = "2.1";
    public string Controller { get; set; } = "EmbeddedTelemetry";
    public string Method { get; set; } = "PublishSensorReading";
    public string CapabilityKey { get; set; } = "embedded.sensor.telemetry.publish";
    public string Direction { get; set; } = "Embedded gateway -> LocalGPT";
    public string TransportBoundary { get; set; } = "A validated edge packet is converted by a trusted LocalGPT gateway into a protected logical 1-Wire envelope.";
    public string ExampleEnvelopeJson { get; set; } = string.Empty;
}

public sealed class EmbeddedFirmwareArtifactResult
{
    public Guid PlanId { get; set; }
    public string ArtifactDirectory { get; set; } = string.Empty;
    public string ZipPath { get; set; } = string.Empty;
    public List<string> Files { get; set; } = [];
}

public sealed class EmbeddedBoardCatalog
{
    public List<EmbeddedBoardProfile> Boards { get; set; } = [];
    public List<EmbeddedProtocolDescriptor> Protocols { get; set; } = [];
    public EmbeddedPublisherWorkbenchContract PublisherWorkbench { get; set; } = new();
}

public sealed class EmbeddedBoardProfile
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Framework { get; set; } = "Arduino";
    public string PlatformIoBoard { get; set; } = string.Empty;
    public double LogicVoltage { get; set; } = 3.3;
    public string DocumentationSource { get; set; } = string.Empty;
    public string Status { get; set; } = "NeedsBoardReview";
    public List<string> SupportedProtocols { get; set; } = [];
    public List<EmbeddedBoardPinProfile> Pins { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

public sealed class EmbeddedBoardPinProfile
{
    public string PinKey { get; set; } = string.Empty;
    public int? Gpio { get; set; }
    public string Label { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = [];
    public bool IsInputOnly { get; set; }
    public bool IsReserved { get; set; }
    public bool IsBootStrap { get; set; }
    public bool IsPowerPin { get; set; }
    public bool IsGroundPin { get; set; }
    public double? Voltage { get; set; }
    public string Warning { get; set; } = string.Empty;
    public double CanvasX { get; set; }
    public double CanvasY { get; set; }
}

public sealed class EmbeddedProtocolDescriptor
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Layer { get; set; } = "Physical";
    public string Purpose { get; set; } = string.Empty;
    public List<string> RequiredRoles { get; set; } = [];
    public bool SupportsSharedBus { get; set; }
    public bool RequiresExternalHardware { get; set; }
    public bool RequiresGateway { get; set; }
    public bool SupportedByGeneratedSketch { get; set; }
    public string SafetyNote { get; set; } = string.Empty;
}

public sealed class EmbeddedWiringDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Embedded wiring";
    public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    public double CanvasWidth { get; set; } = 1600;
    public double CanvasHeight { get; set; } = 900;
    public string CoordinateSystem { get; set; } = "PublisherStudioCanvasV1";
    public List<EmbeddedWiringNode> Nodes { get; set; } = [];
    public List<EmbeddedWiringConnection> Connections { get; set; } = [];
    public string MetadataJson { get; set; } = "{}";
}

public sealed class EmbeddedWiringNode
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = "Sensor";
    public string Label { get; set; } = string.Empty;
    public string PartKey { get; set; } = string.Empty;
    public string PinKey { get; set; } = string.Empty;
    public string ProtocolKey { get; set; } = string.Empty;
    public string ElectricalRole { get; set; } = "Signal";
    public string Direction { get; set; } = "Input";
    public double Voltage { get; set; } = 3.3;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 120;
    public double Height { get; set; } = 80;
    public string OpenScadPartKey { get; set; } = string.Empty;
    public string StyleKey { get; set; } = string.Empty;
    public string PropertiesJson { get; set; } = "{}";
}

public sealed class EmbeddedWiringConnection
{
    public string Id { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    public string BusKey { get; set; } = string.Empty;
    public string SignalName { get; set; } = string.Empty;
    public string Direction { get; set; } = "SourceToTarget";
    public double Voltage { get; set; } = 3.3;
    public bool Animated { get; set; } = true;
    public string AnimationKey { get; set; } = "signal-arrow";
    public string StyleKey { get; set; } = string.Empty;
    public string PropertiesJson { get; set; } = "{}";
}

public sealed class EmbeddedWiringValidationRequest
{
    public EmbeddedWiringDraft Draft { get; set; } = new();
    public bool RequireGroundPath { get; set; } = true;
    public bool RequireBoardPinProfileMatch { get; set; } = true;
}

public sealed class EmbeddedWiringValidationResult
{
    public Guid DraftId { get; set; }
    public string Status { get; set; } = "Warning";
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
    public List<string> UsedProtocols { get; set; } = [];
    public List<string> SharedBuses { get; set; } = [];
    public string CouncilReviewPrompt { get; set; } = string.Empty;
}

public sealed class EmbeddedTelemetryReading
{
    public string SensorKey { get; set; } = string.Empty;
    public string PinKey { get; set; } = string.Empty;
    public int? Gpio { get; set; }
    public string Metric { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Quality { get; set; } = "raw";
}

public sealed class EmbeddedTelemetryBridgeRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string BoardProfileKey { get; set; } = string.Empty;
    public string TransportKey { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    public long Sequence { get; set; }
    public long DeviceTimestampMilliseconds { get; set; }
    public List<EmbeddedTelemetryReading> Readings { get; set; } = [];
    public string TargetPeerId { get; set; } = "localgpt";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class EmbeddedTelemetryBridgeResult
{
    public bool Succeeded { get; set; }
    public string Status { get; set; } = "Invalid";
    public string EdgeEnvelopeJson { get; set; } = string.Empty;
    public OneWireEnvelope? OneWireEnvelope { get; set; }
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
}

public sealed class EmbeddedPublisherWorkbenchContract
{
    public string CapabilityKey { get; set; } = "publisher.embedded.wiring.edit.request";
    public string Controller { get; set; } = "EmbeddedWorkbenchController";
    public string Method { get; set; } = "EditWiring";
    public string Route { get; set; } = "/api/embedded-workbench/wiring/edit";
    public string CanvasContract { get; set; } = "PublisherStudioCanvasV1";
    public string AnimationContract { get; set; } = "signal-arrow";
    public List<string> SupportedOperations { get; set; } = ["board.select", "pin.select", "part.place", "wire.connect", "wire.disconnect", "signal.animate", "wiring.validate", "firmware.plan"];
}

public sealed class EmbeddedWiringDraftCreateRequest
{
    public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    public string Name { get; set; } = "Embedded wiring";
}

public sealed class EmbeddedTelemetryIngressResult
{
    public bool Accepted { get; set; }
    public string Status { get; set; } = "Rejected";
    public string DeviceId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public int ReadingCount { get; set; }
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
}

public sealed class EmbeddedTelemetrySnapshot
{
    public string DeviceId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public string BoardProfileKey { get; set; } = string.Empty;
    public string TransportKey { get; set; } = string.Empty;
    public List<EmbeddedTelemetryReading> Readings { get; set; } = [];
}
