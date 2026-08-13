using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the canonical embedded protocol keys identifiers shared by callers so protocol, persistence, and UI code refer to the same stable values.
/// </summary>
public sealed class EmbeddedProtocolKeys
{
    /// <summary>
    /// Initializes a new <see cref="EmbeddedProtocolKeys"/> instance and captures the dependencies or initial state required by its embedded protocol keys workflow.
    /// </summary>
    private EmbeddedProtocolKeys() { }
    /// <summary>
    /// Defines the digital GPIO constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string DigitalGpio = "gpio.digital";
    /// <summary>
    /// Defines the analog adc constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string AnalogAdc = "gpio.analog";
    /// <summary>
    /// Defines the PWM constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Pwm = "gpio.pwm";
    /// <summary>
    /// Defines the physical one wire constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string PhysicalOneWire = "bus.onewire.physical";
    /// <summary>
    /// Defines the I2C constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string I2c = "bus.i2c";
    /// <summary>
    /// Defines the SPI constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Spi = "bus.spi";
    /// <summary>
    /// Defines the UART constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Uart = "bus.uart";
    /// <summary>
    /// Defines the can constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Can = "bus.can";
    /// <summary>
    /// Defines the RS-485 constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Rs485 = "bus.rs485";
    /// <summary>
    /// Defines the serial JSON lines constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SerialJsonLines = "transport.serial-json-lines";
    /// <summary>
    /// Defines the HTTP JSON constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string HttpJson = "transport.http-json";
    /// <summary>
    /// Defines the MQTT constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Mqtt = "transport.mqtt";
    /// <summary>
    /// Defines the LocalGPT one wire constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string LocalGptOneWire = "localgpt.onewire-envelope";
    /// <summary>
    /// Defines the organic peer constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string OrganicPeer = "localgpt.organic-peer";
    /// <summary>
    /// Defines the custom constant used by <see cref="EmbeddedProtocolKeys"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Custom = "custom";
}

/// <summary>
/// Represents the input contract for embedded firmware plan, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class EmbeddedFirmwarePlanRequest
{
    /// <summary>
    /// Gets or sets the device name value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device name value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(120)] public string DeviceName { get; set; } = "localgpt-embedded-node";
    /// <summary>
    /// Gets or sets the board family value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The board family value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(120)] public string BoardFamily { get; set; } = "ESP32";
    /// <summary>
    /// Gets or sets the board name value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The board name value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(160)] public string BoardName { get; set; } = "ESP32 Dev Module";
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(160)] public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    /// <summary>
    /// Gets or sets the framework value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The framework value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(80)] public string Framework { get; set; } = "Arduino";
    /// <summary>
    /// Gets or sets the telemetry transport value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The telemetry transport value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(120)] public string TelemetryTransport { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    /// <summary>
    /// Gets or sets the baud rate value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The baud rate value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    public int BaudRate { get; set; } = 115200;
    /// <summary>
    /// Gets or sets the telemetry interval milliseconds value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The telemetry interval milliseconds value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    public int TelemetryIntervalMilliseconds { get; set; } = 5000;
    /// <summary>
    /// Gets or sets the pins collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The pins value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    public List<EmbeddedPinRequirement> Pins { get; set; } = [];
    /// <summary>
    /// Gets or sets the sensors collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The sensors value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    public List<EmbeddedSensorRequirement> Sensors { get; set; } = [];
    /// <summary>
    /// Gets or sets the protocol bindings collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The protocol bindings value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    public List<EmbeddedProtocolBinding> ProtocolBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets the wiring draft value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The wiring draft value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    public EmbeddedWiringDraft? WiringDraft { get; set; }
    /// <summary>
    /// Gets or sets the additional requirements value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The additional requirements value exposed by <see cref="EmbeddedFirmwarePlanRequest"/>.</value>
    [MaxLength(8000)] public string AdditionalRequirements { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded pin requirement application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedPinRequirement
{
    /// <summary>
    /// Gets or sets the stable pin key used to identify or correlate this embedded pin requirement instance with related application state.
    /// </summary>
    /// <value>The pin key value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(80)] public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the GPIO value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The GPIO value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    public int Gpio { get; set; }
    /// <summary>
    /// Gets or sets the function value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(120)] public string Function { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the mode value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mode value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(80)] public string Mode { get; set; } = "Input";
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded pin requirement instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    /// <summary>
    /// Gets or sets the stable bus key used to identify or correlate this embedded pin requirement instance with related application state.
    /// </summary>
    /// <value>The bus key value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(120)] public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable sensor key used to identify or correlate this embedded pin requirement instance with related application state.
    /// </summary>
    /// <value>The sensor key value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(120)] public string SensorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the metric value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The metric value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(80)] public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the unit value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The unit value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(80)] public string Unit { get; set; } = "raw";
    /// <summary>
    /// Gets or sets the supply voltage value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The supply voltage value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    public double SupplyVoltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets the notes value that forms part of the embedded pin requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="EmbeddedPinRequirement"/>.</value>
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded sensor requirement application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedSensorRequirement
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this embedded sensor requirement instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sensor type value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sensor type value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(160)] public string SensorType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable driver key used to identify or correlate this embedded sensor requirement instance with related application state.
    /// </summary>
    /// <value>The driver key value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(120)] public string DriverKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the interface value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interface value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(120)] public string Interface { get; set; } = "Analog";
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded sensor requirement instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.AnalogAdc;
    /// <summary>
    /// Gets or sets the stable preferred pin key used to identify or correlate this embedded sensor requirement instance with related application state.
    /// </summary>
    /// <value>The preferred pin key value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(80)] public string PreferredPinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the preferred GPIO value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preferred GPIO value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    public int? PreferredGpio { get; set; }
    /// <summary>
    /// Gets or sets the stable bus key used to identify or correlate this embedded sensor requirement instance with related application state.
    /// </summary>
    /// <value>The bus key value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(120)] public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the metric value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The metric value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(80)] public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the unit value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The unit value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(80)] public string Unit { get; set; } = "raw";
    /// <summary>
    /// Gets or sets the supply voltage value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The supply voltage value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    public double SupplyVoltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets the notes value that forms part of the embedded sensor requirement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="EmbeddedSensorRequirement"/>.</value>
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded protocol binding application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedProtocolBinding
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this embedded protocol binding instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(120)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded protocol binding instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(120)] public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    /// <summary>
    /// Gets or sets the role value that forms part of the embedded protocol binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(120)] public string Role { get; set; } = "Sensor";
    /// <summary>
    /// Gets or sets the direction value that forms part of the embedded protocol binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(120)] public string Direction { get; set; } = "Input";
    /// <summary>
    /// Gets or sets the pin keys collection maintained or exposed by this embedded protocol binding instance for downstream processing.
    /// </summary>
    /// <value>The pin keys value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    public List<string> PinKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the target controller value that forms part of the embedded protocol binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target controller value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(160)] public string TargetController { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target method value that forms part of the embedded protocol binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target method value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(160)] public string TargetMethod { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this embedded protocol binding instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    [MaxLength(200)] public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the settings JSON value that forms part of the embedded protocol binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The settings JSON value exposed by <see cref="EmbeddedProtocolBinding"/>.</value>
    public string SettingsJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded firmware plan application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedFirmwarePlan
{
    /// <summary>
    /// Gets or sets the stable plan identifier used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The plan identifier value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public Guid PlanId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the created at UTC associated with this embedded firmware plan state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the device name value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device name value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the board family value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The board family value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string BoardFamily { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the board name value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The board name value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string BoardName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the framework value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The framework value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string Framework { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the telemetry transport value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The telemetry transport value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string TelemetryTransport { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the baud rate value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The baud rate value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public int BaudRate { get; set; }
    /// <summary>
    /// Gets or sets the telemetry interval milliseconds value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The telemetry interval milliseconds value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public int TelemetryIntervalMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the overall status value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The overall status value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string OverallStatus { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets the pin assignments collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The pin assignments value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public List<EmbeddedPinAssignment> PinAssignments { get; set; } = [];
    /// <summary>
    /// Gets or sets the protocol bindings collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The protocol bindings value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public List<EmbeddedProtocolBinding> ProtocolBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets the findings collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The findings value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
    /// <summary>
    /// Gets or sets the wiring steps collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The wiring steps value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public List<string> WiringSteps { get; set; } = [];
    /// <summary>
    /// Gets or sets the transport contracts collection maintained or exposed by this embedded firmware plan instance for downstream processing.
    /// </summary>
    /// <value>The transport contracts value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public List<EmbeddedTransportContract> TransportContracts { get; set; } = [];
    /// <summary>
    /// Gets or sets the one wire contract value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The one wire contract value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public EmbeddedOneWireContract OneWireContract { get; set; } = new();
    /// <summary>
    /// Gets or sets the wiring validation value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The wiring validation value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public EmbeddedWiringValidationResult? WiringValidation { get; set; }
    /// <summary>
    /// Gets or sets the wiring draft value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The wiring draft value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public EmbeddedWiringDraft? WiringDraft { get; set; }
    /// <summary>
    /// Gets or sets the arduino sketch value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The arduino sketch value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string ArduinoSketch { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the platform I/O configuration value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The platform I/O configuration value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string PlatformIoConfiguration { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the wiring markdown value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The wiring markdown value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string WiringMarkdown { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the learning round advice value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The learning round advice value exposed by <see cref="EmbeddedFirmwarePlan"/>.</value>
    public string LearningRoundAdvice { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded pin assignment application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedPinAssignment
{
    /// <summary>
    /// Gets or sets the stable pin key used to identify or correlate this embedded pin assignment instance with related application state.
    /// </summary>
    /// <value>The pin key value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the GPIO value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The GPIO value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public int Gpio { get; set; }
    /// <summary>
    /// Gets or sets the function value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string Function { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the mode value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mode value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string Mode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded pin assignment instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string ProtocolKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable bus key used to identify or correlate this embedded pin assignment instance with related application state.
    /// </summary>
    /// <value>The bus key value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable sensor key used to identify or correlate this embedded pin assignment instance with related application state.
    /// </summary>
    /// <value>The sensor key value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string SensorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the metric value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The metric value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the unit value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The unit value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string Unit { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the supply voltage value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The supply voltage value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public double SupplyVoltage { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string Status { get; set; } = "Approved";
    /// <summary>
    /// Gets or sets the notes value that forms part of the embedded pin assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="EmbeddedPinAssignment"/>.</value>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded plan finding application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Severity">Severity value supplied to the embedded plan finding operation and used when producing its result.</param>
/// <param name="Code">Code value supplied to the embedded plan finding operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the embedded plan finding operation and used when producing its result.</param>
/// <param name="Gpio">Gpio value supplied to the embedded plan finding operation and used when producing its result.</param>
/// <param name="PinKey">Pin key value supplied to the embedded plan finding operation and used when producing its result.</param>
public sealed record EmbeddedPlanFinding(string Severity, string Code, string Message, int? Gpio = null, string PinKey = "");

/// <summary>
/// Represents an embedded transport contract application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedTransportContract
{
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded transport contract instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    /// <summary>
    /// Gets or sets the display name value that forms part of the embedded transport contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the direction value that forms part of the embedded transport contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string Direction { get; set; } = "DeviceToLocalGpt";
    /// <summary>
    /// Gets or sets the boundary value that forms part of the embedded transport contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The boundary value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string Boundary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the controller value that forms part of the embedded transport contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string Controller { get; set; } = "EmbeddedTelemetry";
    /// <summary>
    /// Gets or sets the method value that forms part of the embedded transport contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string Method { get; set; } = "PublishSensorReading";
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this embedded transport contract instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string CapabilityKey { get; set; } = "embedded.sensor.telemetry.publish";
    /// <summary>
    /// Gets or sets a value indicating whether requires gateway applies to the embedded transport contract state.
    /// </summary>
    /// <value>The requires gateway value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public bool RequiresGateway { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether requires one wire security applies to the embedded transport contract state.
    /// </summary>
    /// <value>The requires one wire security value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public bool RequiresOneWireSecurity { get; set; } = true;
    /// <summary>
    /// Gets or sets the example envelope JSON value that forms part of the embedded transport contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The example envelope JSON value exposed by <see cref="EmbeddedTransportContract"/>.</value>
    public string ExampleEnvelopeJson { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded one wire contract application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedOneWireContract
{
    /// <summary>
    /// Gets or sets the protocol version value that forms part of the embedded one wire contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol version value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string ProtocolVersion { get; set; } = "2.1";
    /// <summary>
    /// Gets or sets the controller value that forms part of the embedded one wire contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string Controller { get; set; } = "EmbeddedTelemetry";
    /// <summary>
    /// Gets or sets the method value that forms part of the embedded one wire contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string Method { get; set; } = "PublishSensorReading";
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this embedded one wire contract instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string CapabilityKey { get; set; } = "embedded.sensor.telemetry.publish";
    /// <summary>
    /// Gets or sets the direction value that forms part of the embedded one wire contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string Direction { get; set; } = "Embedded gateway -> LocalGPT";
    /// <summary>
    /// Gets or sets the transport boundary value that forms part of the embedded one wire contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport boundary value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string TransportBoundary { get; set; } = "A validated edge packet is converted by a trusted LocalGPT gateway into a protected logical 1-Wire envelope.";
    /// <summary>
    /// Gets or sets the example envelope JSON value that forms part of the embedded one wire contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The example envelope JSON value exposed by <see cref="EmbeddedOneWireContract"/>.</value>
    public string ExampleEnvelopeJson { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of embedded firmware artifact, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class EmbeddedFirmwareArtifactResult
{
    /// <summary>
    /// Gets or sets the stable plan identifier used to identify or correlate this embedded firmware artifact instance with related application state.
    /// </summary>
    /// <value>The plan identifier value exposed by <see cref="EmbeddedFirmwareArtifactResult"/>.</value>
    public Guid PlanId { get; set; }
    /// <summary>
    /// Gets or sets the artifact directory used by this embedded firmware artifact instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The artifact directory value exposed by <see cref="EmbeddedFirmwareArtifactResult"/>.</value>
    public string ArtifactDirectory { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ZIP path used by this embedded firmware artifact instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The ZIP path value exposed by <see cref="EmbeddedFirmwareArtifactResult"/>.</value>
    public string ZipPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this embedded firmware artifact instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="EmbeddedFirmwareArtifactResult"/>.</value>
    public List<string> Files { get; set; } = [];
}

/// <summary>
/// Maintains the authoritative directory of embedded board entries used for discovery, validation, and runtime lookup.
/// </summary>
public sealed class EmbeddedBoardCatalog
{
    /// <summary>
    /// Gets or sets the boards collection maintained or exposed by this embedded board instance for downstream processing.
    /// </summary>
    /// <value>The boards value exposed by <see cref="EmbeddedBoardCatalog"/>.</value>
    public List<EmbeddedBoardProfile> Boards { get; set; } = [];
    /// <summary>
    /// Gets or sets the protocols collection maintained or exposed by this embedded board instance for downstream processing.
    /// </summary>
    /// <value>The protocols value exposed by <see cref="EmbeddedBoardCatalog"/>.</value>
    public List<EmbeddedProtocolDescriptor> Protocols { get; set; } = [];
    /// <summary>
    /// Gets or sets the publisher workbench value that forms part of the embedded board state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The publisher workbench value exposed by <see cref="EmbeddedBoardCatalog"/>.</value>
    public EmbeddedPublisherWorkbenchContract PublisherWorkbench { get; set; } = new();
}

/// <summary>
/// Represents an embedded board profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedBoardProfile
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this embedded board profile instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the family value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The family value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string Family { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the framework value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The framework value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string Framework { get; set; } = "Arduino";
    /// <summary>
    /// Gets or sets the platform I/O board value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The platform I/O board value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string PlatformIoBoard { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the logic voltage value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The logic voltage value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public double LogicVoltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets the documentation source value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The documentation source value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string DocumentationSource { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the embedded board profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public string Status { get; set; } = "NeedsBoardReview";
    /// <summary>
    /// Gets or sets the supported protocols collection maintained or exposed by this embedded board profile instance for downstream processing.
    /// </summary>
    /// <value>The supported protocols value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public List<string> SupportedProtocols { get; set; } = [];
    /// <summary>
    /// Gets or sets the pins collection maintained or exposed by this embedded board profile instance for downstream processing.
    /// </summary>
    /// <value>The pins value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public List<EmbeddedBoardPinProfile> Pins { get; set; } = [];
    /// <summary>
    /// Gets or sets the notes collection maintained or exposed by this embedded board profile instance for downstream processing.
    /// </summary>
    /// <value>The notes value exposed by <see cref="EmbeddedBoardProfile"/>.</value>
    public List<string> Notes { get; set; } = [];
}

/// <summary>
/// Represents an embedded board pin profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedBoardPinProfile
{
    /// <summary>
    /// Gets or sets the stable pin key used to identify or correlate this embedded board pin profile instance with related application state.
    /// </summary>
    /// <value>The pin key value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the GPIO value that forms part of the embedded board pin profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The GPIO value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public int? Gpio { get; set; }
    /// <summary>
    /// Gets or sets the label value that forms part of the embedded board pin profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The label value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this embedded board pin profile instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public List<string> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether input only applies to the embedded board pin profile state.
    /// </summary>
    /// <value>The is input only value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public bool IsInputOnly { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether reserved applies to the embedded board pin profile state.
    /// </summary>
    /// <value>The is reserved value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public bool IsReserved { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether boot strap applies to the embedded board pin profile state.
    /// </summary>
    /// <value>The is boot strap value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public bool IsBootStrap { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether power pin applies to the embedded board pin profile state.
    /// </summary>
    /// <value>The is power pin value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public bool IsPowerPin { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether ground pin applies to the embedded board pin profile state.
    /// </summary>
    /// <value>The is ground pin value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public bool IsGroundPin { get; set; }
    /// <summary>
    /// Gets or sets the voltage value that forms part of the embedded board pin profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The voltage value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public double? Voltage { get; set; }
    /// <summary>
    /// Gets or sets the warning value that forms part of the embedded board pin profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The warning value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public string Warning { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the canvas x value that forms part of the embedded board pin profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas x value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public double CanvasX { get; set; }
    /// <summary>
    /// Gets or sets the canvas y value that forms part of the embedded board pin profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas y value exposed by <see cref="EmbeddedBoardPinProfile"/>.</value>
    public double CanvasY { get; set; }
}

/// <summary>
/// Represents embedded protocol state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class EmbeddedProtocolDescriptor
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this embedded protocol instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the embedded protocol state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the layer value that forms part of the embedded protocol state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The layer value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public string Layer { get; set; } = "Physical";
    /// <summary>
    /// Gets or sets the purpose value that forms part of the embedded protocol state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the required roles collection maintained or exposed by this embedded protocol instance for downstream processing.
    /// </summary>
    /// <value>The required roles value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public List<string> RequiredRoles { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether shared bus applies to the embedded protocol state.
    /// </summary>
    /// <value>The supports shared bus value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public bool SupportsSharedBus { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires external hardware applies to the embedded protocol state.
    /// </summary>
    /// <value>The requires external hardware value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public bool RequiresExternalHardware { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires gateway applies to the embedded protocol state.
    /// </summary>
    /// <value>The requires gateway value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public bool RequiresGateway { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether supported by generated sketch applies to the embedded protocol state.
    /// </summary>
    /// <value>The supported by generated sketch value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public bool SupportedByGeneratedSketch { get; set; }
    /// <summary>
    /// Gets or sets the safety note value that forms part of the embedded protocol state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety note value exposed by <see cref="EmbeddedProtocolDescriptor"/>.</value>
    public string SafetyNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded wiring draft application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedWiringDraft
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this embedded wiring draft instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the embedded wiring draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public string Name { get; set; } = "Embedded wiring";
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded wiring draft instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    /// <summary>
    /// Gets or sets the canvas width value that forms part of the embedded wiring draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas width value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public double CanvasWidth { get; set; } = 1600;
    /// <summary>
    /// Gets or sets the canvas height value that forms part of the embedded wiring draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas height value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public double CanvasHeight { get; set; } = 900;
    /// <summary>
    /// Gets or sets the coordinate system value that forms part of the embedded wiring draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The coordinate system value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public string CoordinateSystem { get; set; } = "PublisherStudioCanvasV1";
    /// <summary>
    /// Gets or sets the nodes collection maintained or exposed by this embedded wiring draft instance for downstream processing.
    /// </summary>
    /// <value>The nodes value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public List<EmbeddedWiringNode> Nodes { get; set; } = [];
    /// <summary>
    /// Gets or sets the connections collection maintained or exposed by this embedded wiring draft instance for downstream processing.
    /// </summary>
    /// <value>The connections value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public List<EmbeddedWiringConnection> Connections { get; set; } = [];
    /// <summary>
    /// Gets or sets the metadata JSON value that forms part of the embedded wiring draft state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The metadata JSON value exposed by <see cref="EmbeddedWiringDraft"/>.</value>
    public string MetadataJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded wiring node application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedWiringNode
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this embedded wiring node instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string Kind { get; set; } = "Sensor";
    /// <summary>
    /// Gets or sets the label value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The label value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable part key used to identify or correlate this embedded wiring node instance with related application state.
    /// </summary>
    /// <value>The part key value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string PartKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable pin key used to identify or correlate this embedded wiring node instance with related application state.
    /// </summary>
    /// <value>The pin key value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded wiring node instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string ProtocolKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the electrical role value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The electrical role value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string ElectricalRole { get; set; } = "Signal";
    /// <summary>
    /// Gets or sets the direction value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string Direction { get; set; } = "Input";
    /// <summary>
    /// Gets or sets the voltage value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The voltage value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public double Voltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets the x value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public double X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public double Y { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public double Width { get; set; } = 120;
    /// <summary>
    /// Gets or sets the height value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public double Height { get; set; } = 80;
    /// <summary>
    /// Gets or sets the stable open OpenSCAD part key used to identify or correlate this embedded wiring node instance with related application state.
    /// </summary>
    /// <value>The open OpenSCAD part key value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string OpenScadPartKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable style key used to identify or correlate this embedded wiring node instance with related application state.
    /// </summary>
    /// <value>The style key value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string StyleKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the properties JSON value that forms part of the embedded wiring node state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The properties JSON value exposed by <see cref="EmbeddedWiringNode"/>.</value>
    public string PropertiesJson { get; set; } = "{}";
}

/// <summary>
/// Represents an embedded wiring connection application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedWiringConnection
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable source node identifier used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The source node identifier value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string SourceNodeId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable target node identifier used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The target node identifier value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string TargetNodeId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable protocol key used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The protocol key value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string ProtocolKey { get; set; } = EmbeddedProtocolKeys.DigitalGpio;
    /// <summary>
    /// Gets or sets the stable bus key used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The bus key value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string BusKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the signal name value that forms part of the embedded wiring connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The signal name value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string SignalName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the direction value that forms part of the embedded wiring connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The direction value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string Direction { get; set; } = "SourceToTarget";
    /// <summary>
    /// Gets or sets the voltage value that forms part of the embedded wiring connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The voltage value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public double Voltage { get; set; } = 3.3;
    /// <summary>
    /// Gets or sets a value indicating whether animated applies to the embedded wiring connection state.
    /// </summary>
    /// <value>The animated value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public bool Animated { get; set; } = true;
    /// <summary>
    /// Gets or sets the stable animation key used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The animation key value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string AnimationKey { get; set; } = "signal-arrow";
    /// <summary>
    /// Gets or sets the stable style key used to identify or correlate this embedded wiring connection instance with related application state.
    /// </summary>
    /// <value>The style key value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string StyleKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the properties JSON value that forms part of the embedded wiring connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The properties JSON value exposed by <see cref="EmbeddedWiringConnection"/>.</value>
    public string PropertiesJson { get; set; } = "{}";
}

/// <summary>
/// Represents the input contract for embedded wiring validation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class EmbeddedWiringValidationRequest
{
    /// <summary>
    /// Gets or sets the draft value that forms part of the embedded wiring validation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The draft value exposed by <see cref="EmbeddedWiringValidationRequest"/>.</value>
    public EmbeddedWiringDraft Draft { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether ground path applies to the embedded wiring validation state.
    /// </summary>
    /// <value>The require ground path value exposed by <see cref="EmbeddedWiringValidationRequest"/>.</value>
    public bool RequireGroundPath { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether board pin profile match applies to the embedded wiring validation state.
    /// </summary>
    /// <value>The require board pin profile match value exposed by <see cref="EmbeddedWiringValidationRequest"/>.</value>
    public bool RequireBoardPinProfileMatch { get; set; } = true;
}

/// <summary>
/// Represents the outcome of embedded wiring validation, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class EmbeddedWiringValidationResult
{
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this embedded wiring validation instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="EmbeddedWiringValidationResult"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the embedded wiring validation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="EmbeddedWiringValidationResult"/>.</value>
    public string Status { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets the findings collection maintained or exposed by this embedded wiring validation instance for downstream processing.
    /// </summary>
    /// <value>The findings value exposed by <see cref="EmbeddedWiringValidationResult"/>.</value>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
    /// <summary>
    /// Gets or sets the used protocols collection maintained or exposed by this embedded wiring validation instance for downstream processing.
    /// </summary>
    /// <value>The used protocols value exposed by <see cref="EmbeddedWiringValidationResult"/>.</value>
    public List<string> UsedProtocols { get; set; } = [];
    /// <summary>
    /// Gets or sets the shared buses collection maintained or exposed by this embedded wiring validation instance for downstream processing.
    /// </summary>
    /// <value>The shared buses value exposed by <see cref="EmbeddedWiringValidationResult"/>.</value>
    public List<string> SharedBuses { get; set; } = [];
    /// <summary>
    /// Gets or sets the council review prompt value that forms part of the embedded wiring validation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council review prompt value exposed by <see cref="EmbeddedWiringValidationResult"/>.</value>
    public string CouncilReviewPrompt { get; set; } = string.Empty;
}

/// <summary>
/// Represents an embedded telemetry reading application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedTelemetryReading
{
    /// <summary>
    /// Gets or sets the stable sensor key used to identify or correlate this embedded telemetry reading instance with related application state.
    /// </summary>
    /// <value>The sensor key value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public string SensorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable pin key used to identify or correlate this embedded telemetry reading instance with related application state.
    /// </summary>
    /// <value>The pin key value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public string PinKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the GPIO value that forms part of the embedded telemetry reading state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The GPIO value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public int? Gpio { get; set; }
    /// <summary>
    /// Gets or sets the metric value that forms part of the embedded telemetry reading state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The metric value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public string Metric { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the embedded telemetry reading state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public double Value { get; set; }
    /// <summary>
    /// Gets or sets the unit value that forms part of the embedded telemetry reading state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The unit value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public string Unit { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the quality value that forms part of the embedded telemetry reading state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The quality value exposed by <see cref="EmbeddedTelemetryReading"/>.</value>
    public string Quality { get; set; } = "raw";
}

/// <summary>
/// Represents the input contract for embedded telemetry bridge, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class EmbeddedTelemetryBridgeRequest
{
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this embedded telemetry bridge instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded telemetry bridge instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable transport key used to identify or correlate this embedded telemetry bridge instance with related application state.
    /// </summary>
    /// <value>The transport key value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public string TransportKey { get; set; } = EmbeddedProtocolKeys.SerialJsonLines;
    /// <summary>
    /// Gets or sets the sequence value that forms part of the embedded telemetry bridge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public long Sequence { get; set; }
    /// <summary>
    /// Gets or sets the device timestamp milliseconds value that forms part of the embedded telemetry bridge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device timestamp milliseconds value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public long DeviceTimestampMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the readings collection maintained or exposed by this embedded telemetry bridge instance for downstream processing.
    /// </summary>
    /// <value>The readings value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public List<EmbeddedTelemetryReading> Readings { get; set; } = [];
    /// <summary>
    /// Gets or sets the stable target peer identifier used to identify or correlate this embedded telemetry bridge instance with related application state.
    /// </summary>
    /// <value>The target peer identifier value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public string TargetPeerId { get; set; } = "localgpt";
    /// <summary>
    /// Gets or sets the metadata JSON value that forms part of the embedded telemetry bridge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The metadata JSON value exposed by <see cref="EmbeddedTelemetryBridgeRequest"/>.</value>
    public string MetadataJson { get; set; } = "{}";
}

/// <summary>
/// Represents the outcome of embedded telemetry bridge, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class EmbeddedTelemetryBridgeResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the embedded telemetry bridge state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="EmbeddedTelemetryBridgeResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the embedded telemetry bridge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="EmbeddedTelemetryBridgeResult"/>.</value>
    public string Status { get; set; } = "Invalid";
    /// <summary>
    /// Gets or sets the edge envelope JSON value that forms part of the embedded telemetry bridge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The edge envelope JSON value exposed by <see cref="EmbeddedTelemetryBridgeResult"/>.</value>
    public string EdgeEnvelopeJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the one wire envelope value that forms part of the embedded telemetry bridge state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The one wire envelope value exposed by <see cref="EmbeddedTelemetryBridgeResult"/>.</value>
    public OneWireEnvelope? OneWireEnvelope { get; set; }
    /// <summary>
    /// Gets or sets the findings collection maintained or exposed by this embedded telemetry bridge instance for downstream processing.
    /// </summary>
    /// <value>The findings value exposed by <see cref="EmbeddedTelemetryBridgeResult"/>.</value>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
}

/// <summary>
/// Represents an embedded publisher workbench contract application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedPublisherWorkbenchContract
{
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this embedded publisher workbench contract instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public string CapabilityKey { get; set; } = "publisher.embedded.wiring.edit.request";
    /// <summary>
    /// Gets or sets the controller value that forms part of the embedded publisher workbench contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public string Controller { get; set; } = "EmbeddedWorkbenchController";
    /// <summary>
    /// Gets or sets the method value that forms part of the embedded publisher workbench contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public string Method { get; set; } = "EditWiring";
    /// <summary>
    /// Gets or sets the route value that forms part of the embedded publisher workbench contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public string Route { get; set; } = "/api/embedded-workbench/wiring/edit";
    /// <summary>
    /// Gets or sets the canvas contract value that forms part of the embedded publisher workbench contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas contract value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public string CanvasContract { get; set; } = "PublisherStudioCanvasV1";
    /// <summary>
    /// Gets or sets the animation contract value that forms part of the embedded publisher workbench contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The animation contract value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public string AnimationContract { get; set; } = "signal-arrow";
    /// <summary>
    /// Gets or sets the supported operations collection maintained or exposed by this embedded publisher workbench contract instance for downstream processing.
    /// </summary>
    /// <value>The supported operations value exposed by <see cref="EmbeddedPublisherWorkbenchContract"/>.</value>
    public List<string> SupportedOperations { get; set; } = ["board.select", "pin.select", "part.place", "wire.connect", "wire.disconnect", "signal.animate", "wiring.validate", "firmware.plan"];
}

/// <summary>
/// Represents the input contract for embedded wiring draft create, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class EmbeddedWiringDraftCreateRequest
{
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded wiring draft create instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedWiringDraftCreateRequest"/>.</value>
    public string BoardProfileKey { get; set; } = "esp32-classic-generic";
    /// <summary>
    /// Gets or sets the name value that forms part of the embedded wiring draft create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="EmbeddedWiringDraftCreateRequest"/>.</value>
    public string Name { get; set; } = "Embedded wiring";
}

/// <summary>
/// Represents the outcome of embedded telemetry ingress, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class EmbeddedTelemetryIngressResult
{
    /// <summary>
    /// Gets or sets a value indicating whether accepted applies to the embedded telemetry ingress state.
    /// </summary>
    /// <value>The accepted value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public bool Accepted { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the embedded telemetry ingress state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public string Status { get; set; } = "Rejected";
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this embedded telemetry ingress instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sequence value that forms part of the embedded telemetry ingress state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public long Sequence { get; set; }
    /// <summary>
    /// Gets or sets the received at UTC associated with this embedded telemetry ingress state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The received at UTC value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the reading count that quantifies the associated embedded telemetry ingress data.
    /// </summary>
    /// <value>The reading count value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public int ReadingCount { get; set; }
    /// <summary>
    /// Gets or sets the findings collection maintained or exposed by this embedded telemetry ingress instance for downstream processing.
    /// </summary>
    /// <value>The findings value exposed by <see cref="EmbeddedTelemetryIngressResult"/>.</value>
    public List<EmbeddedPlanFinding> Findings { get; set; } = [];
}

/// <summary>
/// Represents an embedded telemetry snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmbeddedTelemetrySnapshot
{
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this embedded telemetry snapshot instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="EmbeddedTelemetrySnapshot"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sequence value that forms part of the embedded telemetry snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="EmbeddedTelemetrySnapshot"/>.</value>
    public long Sequence { get; set; }
    /// <summary>
    /// Gets or sets the received at UTC associated with this embedded telemetry snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The received at UTC value exposed by <see cref="EmbeddedTelemetrySnapshot"/>.</value>
    public DateTime ReceivedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded telemetry snapshot instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedTelemetrySnapshot"/>.</value>
    public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable transport key used to identify or correlate this embedded telemetry snapshot instance with related application state.
    /// </summary>
    /// <value>The transport key value exposed by <see cref="EmbeddedTelemetrySnapshot"/>.</value>
    public string TransportKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the readings collection maintained or exposed by this embedded telemetry snapshot instance for downstream processing.
    /// </summary>
    /// <value>The readings value exposed by <see cref="EmbeddedTelemetrySnapshot"/>.</value>
    public List<EmbeddedTelemetryReading> Readings { get; set; } = [];
}
