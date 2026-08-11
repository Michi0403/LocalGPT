using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an embedded protocol keys.
/// </summary>
public sealed class EmbeddedProtocolKeys
{
    /// <summary>
    /// Runs the embedded protocol keys operation.
    /// </summary>
    private EmbeddedProtocolKeys() { }
    /// <summary>
    /// Stores digital gpio.
    /// </summary>
    public const string DigitalGpio = "gpio.digital";
    /// <summary>
    /// Stores analog adc.
    /// </summary>
    public const string AnalogAdc = "gpio.analog";
    /// <summary>
    /// Stores pwm.
    /// </summary>
    public const string Pwm = "gpio.pwm";
    /// <summary>
    /// Stores physical one wire.
    /// </summary>
    public const string PhysicalOneWire = "bus.onewire.physical";
    /// <summary>
    /// Stores i2c.
    /// </summary>
    public const string I2c = "bus.i2c";
    /// <summary>
    /// Stores spi.
    /// </summary>
    public const string Spi = "bus.spi";
    /// <summary>
    /// Stores uart.
    /// </summary>
    public const string Uart = "bus.uart";
    /// <summary>
    /// Stores can.
    /// </summary>
    public const string Can = "bus.can";
    /// <summary>
    /// Stores rs485.
    /// </summary>
    public const string Rs485 = "bus.rs485";
    /// <summary>
    /// Stores serial JSON lines.
    /// </summary>
    public const string SerialJsonLines = "transport.serial-json-lines";
    /// <summary>
    /// Stores HTTP JSON.
    /// </summary>
    public const string HttpJson = "transport.http-json";
    /// <summary>
    /// Stores mqtt.
    /// </summary>
    public const string Mqtt = "transport.mqtt";
    /// <summary>
    /// Stores local gpt one wire.
    /// </summary>
    public const string LocalGptOneWire = "localgpt.onewire-envelope";
    /// <summary>
    /// Stores organic peer.
    /// </summary>
    public const string OrganicPeer = "localgpt.organic-peer";
    /// <summary>
    /// Stores custom.
    /// </summary>
    public const string Custom = "custom";
}

/// <summary>
/// Represents an embedded firmware plan request.
/// </summary>
public sealed class EmbeddedFirmwarePlanRequest
{
    /// <summary>
    /// Gets or sets device name.
    /// </summary>
    [MaxLength(120)] public string DeviceName { get; set; } = "localgpt-embedded-node";
    /// <summary>
    /// Gets or sets board family.
    /// </summary>
    [MaxLength(120)] public string BoardFamily { get; set; } = "ESP32";
    /// <summary>
    /// Gets or sets board name.
    /// </summary>
    [MaxLength(160)] public string BoardName { get; set; } = "ESP32 Dev Module";
    /// <summary>
    /// Gets or sets board profile key.
    /// </summary>
    [MaxLength(160)] public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    /// <summary>
    /// Gets or sets framework.
    /// </summary>
    [MaxLength(80)] public string Framework { get; set; } = "Arduino";
    /// <summary>
    /// Gets or sets telemetry transport.
    /// </summary>
    [MaxLength(120)] public string TelemetryTransport { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    /// <summary>
    /// Gets or sets baud rate.
    /// </summary>
    public int BaudRate { get; set; } = 115200;
    /// <summary>
    /// Gets or sets telemetry interval milliseconds.
    /// </summary>
    public int TelemetryIntervalMilliseconds { get; set; } = 5000;
    /// <summary>
    /// Gets or sets pins.
    /// </summary>
    public List<EmbeddedPinRequirement> Pins { get; set; } = [];
    /// <summary>
    /// Gets or sets sensors.
    /// </summary>
    public List<EmbeddedSensorRequirement> Sensors { get; set; } = [];
    /// <summary>
    /// Gets or sets protocol bindings.
    /// </summary>
    public List<EmbeddedProtocolBinding> ProtocolBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets wiring draft.
    /// </summary>
    public EmbeddedWiringDraft? WiringDraft { get; set; }
    /// <summary>
    /// Gets or sets additional requirements.
    /// </summary>
    [MaxLength(8000)] public string AdditionalRequirements { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded pin requirement.
/// </summary>
public sealed class EmbeddedPinRequirement
{
    /// <summary>
    /// Gets or sets pin key.
    /// </summary>
    [MaxLength(80)] public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets gpio.
    /// </summary>
    public int Gpio { get; set; }
    /// <summary>
    /// Gets or sets function.
    /// </summary>
    [MaxLength(120)] public string Function { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mode.
    /// </summary>
    [MaxLength(80)] public string Mode { get; set; } = "Input";
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    /// <summary>
    /// Gets or sets bus key.
    /// </summary>
    [MaxLength(120)] public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets sensor key.
    /// </summary>
    [MaxLength(120)] public string SensorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets metric.
    /// </summary>
    [MaxLength(80)] public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets unit.
    /// </summary>
    [MaxLength(80)] public string Unit { get; set; } = "raw";
    /// <summary>
    /// Gets or sets supply voltage.
    /// </summary>
    public double SupplyVoltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded sensor requirement.
/// </summary>
public sealed class EmbeddedSensorRequirement
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets sensor type.
    /// </summary>
    [MaxLength(160)] public string SensorType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets driver key.
    /// </summary>
    [MaxLength(120)] public string DriverKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets interface.
    /// </summary>
    [MaxLength(120)] public string Interface { get; set; } = "Analog";
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.AnalogAdc;
    /// <summary>
    /// Gets or sets preferred pin key.
    /// </summary>
    [MaxLength(80)] public string PreferredPinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets preferred gpio.
    /// </summary>
    public int? PreferredGpio { get; set; }
    /// <summary>
    /// Gets or sets bus key.
    /// </summary>
    [MaxLength(120)] public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets metric.
    /// </summary>
    [MaxLength(80)] public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets unit.
    /// </summary>
    [MaxLength(80)] public string Unit { get; set; } = "raw";
    /// <summary>
    /// Gets or sets supply voltage.
    /// </summary>
    public double SupplyVoltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded protocol binding.
/// </summary>
public sealed class EmbeddedProtocolBinding
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    /// <summary>
    /// Gets or sets role.
    /// </summary>
    [MaxLength(120)] public string Role { get; set; } = "Sensor";
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    [MaxLength(120)] public string Direction { get; set; } = "Input";
    /// <summary>
    /// Gets or sets pin keys.
    /// </summary>
    public List<string> PinKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets target controller.
    /// </summary>
    [MaxLength(160)] public string TargetController { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target method.
    /// </summary>
    [MaxLength(160)] public string TargetMethod { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    [MaxLength(200)] public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets settings JSON.
    /// </summary>
    public string SettingsJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded firmware plan.
/// </summary>
public sealed class EmbeddedFirmwarePlan
{
    /// <summary>
    /// Gets or sets plan identifier.
    /// </summary>
    public Guid PlanId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets board family.
    /// </summary>
    public string BoardFamily { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets board name.
    /// </summary>
    public string BoardName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets board profile key.
    /// </summary>
    public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets framework.
    /// </summary>
    public string Framework { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets telemetry transport.
    /// </summary>
    public string TelemetryTransport { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets baud rate.
    /// </summary>
    public int BaudRate { get; set; }
    /// <summary>
    /// Gets or sets telemetry interval milliseconds.
    /// </summary>
    public int TelemetryIntervalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets overall status.
    /// </summary>
    public string OverallStatus { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets pin assignments.
    /// </summary>
    public List<EmbeddedPinAssignment> PinAssignments { get; set; } = [];
    /// <summary>
    /// Gets or sets protocol bindings.
    /// </summary>
    public List<EmbeddedProtocolBinding> ProtocolBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets findings.
    /// </summary>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
    /// <summary>
    /// Gets or sets wiring steps.
    /// </summary>
    public List<string> WiringSteps { get; set; } = [];
    /// <summary>
    /// Gets or sets transport contracts.
    /// </summary>
    public List<EmbeddedTransportContract> TransportContracts { get; set; } = [];
    /// <summary>
    /// Gets or sets one wire contract.
    /// </summary>
    public EmbeddedOneWireContract OneWireContract { get; set; } = new();
    /// <summary>
    /// Gets or sets wiring validation.
    /// </summary>
    public EmbeddedWiringValidationResult? WiringValidation { get; set; }
    /// <summary>
    /// Gets or sets wiring draft.
    /// </summary>
    public EmbeddedWiringDraft? WiringDraft { get; set; }
    /// <summary>
    /// Gets or sets arduino sketch.
    /// </summary>
    public string ArduinoSketch { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets platform io configuration.
    /// </summary>
    public string PlatformIoConfiguration { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets wiring markdown.
    /// </summary>
    public string WiringMarkdown { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets learning round advice.
    /// </summary>
    public string LearningRoundAdvice { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded pin assignment.
/// </summary>
public sealed class EmbeddedPinAssignment
{
    /// <summary>
    /// Gets or sets pin key.
    /// </summary>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets gpio.
    /// </summary>
    public int Gpio { get; set; }
    /// <summary>
    /// Gets or sets function.
    /// </summary>
    public string Function { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mode.
    /// </summary>
    public string Mode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    public string ProtocolKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets bus key.
    /// </summary>
    public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets sensor key.
    /// </summary>
    public string SensorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets metric.
    /// </summary>
    public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets unit.
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets supply voltage.
    /// </summary>
    public double SupplyVoltage { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Approved";
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded plan finding.
/// </summary>
public sealed record EmbeddedPlanFinding(string Severity, string Code, string Message, int? Gpio = null, string PinKey = "");

/// <summary>
/// Represents an embedded transport contract.
/// </summary>
public sealed class EmbeddedTransportContract
{
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    public string Direction { get; set; } = "DeviceToLocalGpt";
    /// <summary>
    /// Gets or sets boundary.
    /// </summary>
    public string Boundary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = "EmbeddedTelemetry";
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public string Method { get; set; } = "PublishSensorReading";
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = "embedded.sensor.telemetry.publish";
    /// <summary>
    /// Gets or sets requires gateway.
    /// </summary>
    public bool RequiresGateway { get; set; } = true;
    /// <summary>
    /// Gets or sets requires one wire security.
    /// </summary>
    public bool RequiresOneWireSecurity { get; set; } = true;
    /// <summary>
    /// Gets or sets example envelope JSON.
    /// </summary>
    public string ExampleEnvelopeJson { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded one wire contract.
/// </summary>
public sealed class EmbeddedOneWireContract
{
    /// <summary>
    /// Gets or sets protocol version.
    /// </summary>
    public string ProtocolVersion { get; set; } = "2.1";
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = "EmbeddedTelemetry";
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public string Method { get; set; } = "PublishSensorReading";
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = "embedded.sensor.telemetry.publish";
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    public string Direction { get; set; } = "Embedded gateway -> LocalGPT";
    /// <summary>
    /// Gets or sets transport boundary.
    /// </summary>
    public string TransportBoundary { get; set; } = "A validated edge packet is converted by a trusted LocalGPT gateway into a protected logical 1-Wire envelope.";
    /// <summary>
    /// Gets or sets example envelope JSON.
    /// </summary>
    public string ExampleEnvelopeJson { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded firmware artifact result.
/// </summary>
public sealed class EmbeddedFirmwareArtifactResult
{
    /// <summary>
    /// Gets or sets plan identifier.
    /// </summary>
    public Guid PlanId { get; set; }
    /// <summary>
    /// Gets or sets artifact directory.
    /// </summary>
    public string ArtifactDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets zip path.
    /// </summary>
    public string ZipPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets files.
    /// </summary>
    public List<string> Files { get; set; } = [];
}

/// <summary>
/// Represents an embedded board catalog.
/// </summary>
public sealed class EmbeddedBoardCatalog
{
    /// <summary>
    /// Gets or sets boards.
    /// </summary>
    public List<EmbeddedBoardProfile> Boards { get; set; } = [];
    /// <summary>
    /// Gets or sets protocols.
    /// </summary>
    public List<EmbeddedProtocolDescriptor> Protocols { get; set; } = [];
    /// <summary>
    /// Gets or sets publisher workbench.
    /// </summary>
    public EmbeddedPublisherWorkbenchContract PublisherWorkbench { get; set; } = new();
}

/// <summary>
/// Represents an embedded board profile.
/// </summary>
public sealed class EmbeddedBoardProfile
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets family.
    /// </summary>
    public string Family { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets framework.
    /// </summary>
    public string Framework { get; set; } = "Arduino";
    /// <summary>
    /// Gets or sets platform io board.
    /// </summary>
    public string PlatformIoBoard { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets logic voltage.
    /// </summary>
    public double LogicVoltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets documentation source.
    /// </summary>
    public string DocumentationSource { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "NeedsBoardReview";
    /// <summary>
    /// Gets or sets supported protocols.
    /// </summary>
    public List<string> SupportedProtocols { get; set; } = [];
    /// <summary>
    /// Gets or sets pins.
    /// </summary>
    public List<EmbeddedBoardPinProfile> Pins { get; set; } = [];
    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    public List<string> Notes { get; set; } = [];
}

/// <summary>
/// Represents an embedded board pin profile.
/// </summary>
public sealed class EmbeddedBoardPinProfile
{
    /// <summary>
    /// Gets or sets pin key.
    /// </summary>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets gpio.
    /// </summary>
    public int? Gpio { get; set; }
    /// <summary>
    /// Gets or sets label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capabilities.
    /// </summary>
    public List<string> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets is input only.
    /// </summary>
    public bool IsInputOnly { get; set; }
    /// <summary>
    /// Gets or sets is reserved.
    /// </summary>
    public bool IsReserved { get; set; }
    /// <summary>
    /// Gets or sets is boot strap.
    /// </summary>
    public bool IsBootStrap { get; set; }
    /// <summary>
    /// Gets or sets is power pin.
    /// </summary>
    public bool IsPowerPin { get; set; }
    /// <summary>
    /// Gets or sets is ground pin.
    /// </summary>
    public bool IsGroundPin { get; set; }
    /// <summary>
    /// Gets or sets voltage.
    /// </summary>
    public double? Voltage { get; set; }
    /// <summary>
    /// Gets or sets warning.
    /// </summary>
    public string Warning { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets canvas x.
    /// </summary>
    public double CanvasX { get; set; }
    /// <summary>
    /// Gets or sets canvas y.
    /// </summary>
    public double CanvasY { get; set; }
}

/// <summary>
/// Represents an embedded protocol descriptor.
/// </summary>
public sealed class EmbeddedProtocolDescriptor
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets layer.
    /// </summary>
    public string Layer { get; set; } = "Physical";
    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets required roles.
    /// </summary>
    public List<string> RequiredRoles { get; set; } = [];
    /// <summary>
    /// Gets or sets supports shared bus.
    /// </summary>
    public bool SupportsSharedBus { get; set; }
    /// <summary>
    /// Gets or sets requires external hardware.
    /// </summary>
    public bool RequiresExternalHardware { get; set; }
    /// <summary>
    /// Gets or sets requires gateway.
    /// </summary>
    public bool RequiresGateway { get; set; }
    /// <summary>
    /// Gets or sets supported by generated sketch.
    /// </summary>
    public bool SupportedByGeneratedSketch { get; set; }
    /// <summary>
    /// Gets or sets safety note.
    /// </summary>
    public string SafetyNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded wiring draft.
/// </summary>
public sealed class EmbeddedWiringDraft
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = "Embedded wiring";
    /// <summary>
    /// Gets or sets board profile key.
    /// </summary>
    public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    /// <summary>
    /// Gets or sets canvas width.
    /// </summary>
    public double CanvasWidth { get; set; } = 1600;
    /// <summary>
    /// Gets or sets canvas height.
    /// </summary>
    public double CanvasHeight { get; set; } = 900;
    /// <summary>
    /// Gets or sets coordinate system.
    /// </summary>
    public string CoordinateSystem { get; set; } = "PublisherStudioCanvasV1";
    /// <summary>
    /// Gets or sets nodes.
    /// </summary>
    public List<EmbeddedWiringNode> Nodes { get; set; } = [];
    /// <summary>
    /// Gets or sets connections.
    /// </summary>
    public List<EmbeddedWiringConnection> Connections { get; set; } = [];
    /// <summary>
    /// Gets or sets metadata JSON.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded wiring node.
/// </summary>
public sealed class EmbeddedWiringNode
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = "Sensor";
    /// <summary>
    /// Gets or sets label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets part key.
    /// </summary>
    public string PartKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets pin key.
    /// </summary>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    public string ProtocolKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets electrical role.
    /// </summary>
    public string ElectricalRole { get; set; } = "Signal";
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    public string Direction { get; set; } = "Input";
    /// <summary>
    /// Gets or sets voltage.
    /// </summary>
    public double Voltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets x.
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets y.
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets width.
    /// </summary>
    public double Width { get; set; } = 120;
    /// <summary>
    /// Gets or sets height.
    /// </summary>
    public double Height { get; set; } = 80;
    /// <summary>
    /// Gets or sets open scad part key.
    /// </summary>
    public string OpenScadPartKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets style key.
    /// </summary>
    public string StyleKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets properties JSON.
    /// </summary>
    public string PropertiesJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded wiring connection.
/// </summary>
public sealed class EmbeddedWiringConnection
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source node identifier.
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target node identifier.
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets protocol key.
    /// </summary>
    public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    /// <summary>
    /// Gets or sets bus key.
    /// </summary>
    public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signal name.
    /// </summary>
    public string SignalName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets direction.
    /// </summary>
    public string Direction { get; set; } = "SourceToTarget";
    /// <summary>
    /// Gets or sets voltage.
    /// </summary>
    public double Voltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets animated.
    /// </summary>
    public bool Animated { get; set; } = true;
    /// <summary>
    /// Gets or sets animation key.
    /// </summary>
    public string AnimationKey { get; set; } = "signal-arrow";
    /// <summary>
    /// Gets or sets style key.
    /// </summary>
    public string StyleKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets properties JSON.
    /// </summary>
    public string PropertiesJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded wiring validation request.
/// </summary>
public sealed class EmbeddedWiringValidationRequest
{
    /// <summary>
    /// Gets or sets draft.
    /// </summary>
    public EmbeddedWiringDraft Draft { get; set; } = new();
    /// <summary>
    /// Gets or sets require ground path.
    /// </summary>
    public bool RequireGroundPath { get; set; } = true;
    /// <summary>
    /// Gets or sets require board pin profile match.
    /// </summary>
    public bool RequireBoardPinProfileMatch { get; set; } = true;
}

/// <summary>
/// Represents an embedded wiring validation result.
/// </summary>
public sealed class EmbeddedWiringValidationResult
{
    /// <summary>
    /// Gets or sets draft identifier.
    /// </summary>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets findings.
    /// </summary>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
    /// <summary>
    /// Gets or sets used protocols.
    /// </summary>
    public List<string> UsedProtocols { get; set; } = [];
    /// <summary>
    /// Gets or sets shared buses.
    /// </summary>
    public List<string> SharedBuses { get; set; } = [];
    /// <summary>
    /// Gets or sets council review prompt.
    /// </summary>
    public string CouncilReviewPrompt { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded telemetry reading.
/// </summary>
public sealed class EmbeddedTelemetryReading
{
    /// <summary>
    /// Gets or sets sensor key.
    /// </summary>
    public string SensorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets pin key.
    /// </summary>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets gpio.
    /// </summary>
    public int? Gpio { get; set; }
    /// <summary>
    /// Gets or sets metric.
    /// </summary>
    public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public double Value { get; set; }
    /// <summary>
    /// Gets or sets unit.
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets quality.
    /// </summary>
    public string Quality { get; set; } = "raw";
}

/// <summary>
/// Represents an embedded telemetry bridge request.
/// </summary>
public sealed class EmbeddedTelemetryBridgeRequest
{
    /// <summary>
    /// Gets or sets device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets board profile key.
    /// </summary>
    public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets transport key.
    /// </summary>
    public string TransportKey { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    /// <summary>
    /// Gets or sets sequence.
    /// </summary>
    public long Sequence { get; set; }
    /// <summary>
    /// Gets or sets device timestamp milliseconds.
    /// </summary>
    public long DeviceTimestampMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets readings.
    /// </summary>
    public List<EmbeddedTelemetryReading> Readings { get; set; } = [];
    /// <summary>
    /// Gets or sets target peer identifier.
    /// </summary>
    public string TargetPeerId { get; set; } = "localgpt";
    /// <summary>
    /// Gets or sets metadata JSON.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded telemetry bridge result.
/// </summary>
public sealed class EmbeddedTelemetryBridgeResult
{
    /// <summary>
    /// Gets or sets succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Invalid";
    /// <summary>
    /// Gets or sets edge envelope JSON.
    /// </summary>
    public string EdgeEnvelopeJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets one wire envelope.
    /// </summary>
    public OneWireEnvelope? OneWireEnvelope { get; set; }
    /// <summary>
    /// Gets or sets findings.
    /// </summary>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
}

/// <summary>
/// Represents an embedded publisher workbench contract.
/// </summary>
public sealed class EmbeddedPublisherWorkbenchContract
{
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = "publisher.embedded.wiring.edit.request";
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = "EmbeddedWorkbenchController";
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public string Method { get; set; } = "EditWiring";
    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public string Route { get; set; } = "/api/embedded-workbench/wiring/edit";
    /// <summary>
    /// Gets or sets canvas contract.
    /// </summary>
    public string CanvasContract { get; set; } = "PublisherStudioCanvasV1";
    /// <summary>
    /// Gets or sets animation contract.
    /// </summary>
    public string AnimationContract { get; set; } = "signal-arrow";
    /// <summary>
    /// Gets or sets supported operations.
    /// </summary>
    public List<string> SupportedOperations { get; set; } = ["board.select", "pin.select", "part.place", "wire.connect", "wire.disconnect", "signal.animate", "wiring.validate", "firmware.plan"];
}

/// <summary>
/// Represents an embedded wiring draft create request.
/// </summary>
public sealed class EmbeddedWiringDraftCreateRequest
{
    /// <summary>
    /// Gets or sets board profile key.
    /// </summary>
    public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = "Embedded wiring";
}

/// <summary>
/// Represents an embedded telemetry ingress result.
/// </summary>
public sealed class EmbeddedTelemetryIngressResult
{
    /// <summary>
    /// Gets or sets accepted.
    /// </summary>
    public bool Accepted { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Rejected";
    /// <summary>
    /// Gets or sets device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets sequence.
    /// </summary>
    public long Sequence { get; set; }
    /// <summary>
    /// Gets or sets received at UTC.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets reading count.
    /// </summary>
    public int ReadingCount { get; set; }
    /// <summary>
    /// Gets or sets findings.
    /// </summary>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
}

/// <summary>
/// Represents an embedded telemetry snapshot.
/// </summary>
public sealed class EmbeddedTelemetrySnapshot
{
    /// <summary>
    /// Gets or sets device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets sequence.
    /// </summary>
    public long Sequence { get; set; }
    /// <summary>
    /// Gets or sets received at UTC.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets board profile key.
    /// </summary>
    public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets transport key.
    /// </summary>
    public string TransportKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets readings.
    /// </summary>
    public List<EmbeddedTelemetryReading> Readings { get; set; } = [];
}
