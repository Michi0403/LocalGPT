using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class EmbeddedFirmwarePlanningService(
    IEmbeddedHardwareCatalogService catalog,
    IEmbeddedWiringService wiring,
    IEmbeddedTelemetryBridgeService telemetryBridge,
    ILogger<EmbeddedFirmwarePlanningService> logger) : IEmbeddedFirmwarePlanningService
{
    private readonly JsonSerializerOptions artifactJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<EmbeddedFirmwarePlan> CreatePlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var plan = await BuildPlanAsync(request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Created embedded firmware plan {PlanId} for board profile {BoardProfileKey} with {PinCount} pin assignment(s), {ProtocolCount} protocol binding(s), and status {Status}.",
            plan.PlanId,
            plan.BoardProfileKey,
            plan.PinAssignments.Count,
            plan.ProtocolBindings.Count,
            plan.OverallStatus);
        return plan;
    }

    public async Task<EmbeddedFirmwareArtifactResult> CreateArtifactsAsync(EmbeddedFirmwarePlanRequest request, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        if (!userConfirmed)
            throw new InvalidOperationException("Fresh user confirmation is required before firmware planning artifacts are written.");
        var plan = await CreatePlanAsync(request, cancellationToken).ConfigureAwait(false);
        if (plan.Findings.Any(item => string.Equals(item.Severity, "Danger", StringComparison.OrdinalIgnoreCase)) ||
            plan.WiringValidation?.Findings.Any(item => string.Equals(item.Severity, "Danger", StringComparison.OrdinalIgnoreCase)) == true)
        {
            throw new InvalidOperationException("Artifacts were not written because the deterministic board/GPIO/wiring review contains danger findings. Correct the board profile or wiring first.");
        }

        var safeDevice = SafeFileName(plan.DeviceName);
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "Artifacts", "EmbeddedFirmware", $"{safeDevice}-{plan.PlanId:N}");
        Directory.CreateDirectory(root);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["src/main.cpp"] = plan.ArduinoSketch,
            ["platformio.ini"] = plan.PlatformIoConfiguration,
            ["WIRING.md"] = plan.WiringMarkdown,
            ["localgpt-plan.json"] = JsonSerializer.Serialize(plan, artifactJsonOptions),
            ["localgpt-transport-contracts.json"] = JsonSerializer.Serialize(plan.TransportContracts, artifactJsonOptions)
        };
        if (plan.WiringDraft is not null)
            files["wiring-draft.json"] = JsonSerializer.Serialize(plan.WiringDraft, artifactJsonOptions);

        foreach (var pair in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(root, pair.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, pair.Value, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        }
        var zipPath = root + ".zip";
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(root, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        logger.LogInformation("Created approved embedded firmware planning artifact {PlanId} with {FileCount} file(s); local paths were omitted from logs.", plan.PlanId, files.Count);
        return new EmbeddedFirmwareArtifactResult
        {
            PlanId = plan.PlanId,
            ArtifactDirectory = root,
            ZipPath = zipPath,
            Files = files.Keys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private async Task<EmbeddedFirmwarePlan> BuildPlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken)
    {
        var boardProfileKey = Text(request.BoardProfileKey, "esp32-classic-generic", 160).ToLowerInvariant();
        var profile = await catalog.GetBoardProfileAsync(boardProfileKey, cancellationToken).ConfigureAwait(false);
        var boardFamily = Text(request.BoardFamily, profile?.Family ?? "ESP32", 120);
        var boardName = Text(request.BoardName, profile?.DisplayName ?? "ESP32 Dev Module", 160);
        var framework = Text(request.Framework, profile?.Framework ?? "Arduino", 80);
        var deviceName = Text(request.DeviceName, "localgpt-embedded-node", 120);
        var transport = NormalizeTransport(request.TelemetryTransport);
        var interval = Math.Clamp(request.TelemetryIntervalMilliseconds, 250, 3_600_000);
        var baud = request.BaudRate is >= 1200 and <= 4_000_000 ? request.BaudRate : 115200;
        var protocols = await catalog.GetProtocolDescriptorsAsync(cancellationToken).ConfigureAwait(false);
        var assignments = NormalizeAssignments(request, profile);
        var bindings = NormalizeBindings(request, assignments, transport);
        var findings = ReviewAssignments(profile, boardFamily, transport, assignments, bindings, protocols);

        EmbeddedWiringValidationResult? wiringValidation = null;
        if (request.WiringDraft is not null)
        {
            wiringValidation = await wiring.ValidateAsync(new EmbeddedWiringValidationRequest
            {
                Draft = request.WiringDraft,
                RequireBoardPinProfileMatch = true,
                RequireGroundPath = true
            }, cancellationToken).ConfigureAwait(false);
            findings.AddRange(wiringValidation.Findings.Select(item => item with { Code = $"WIRING_{item.Code}" }));
        }

        var status = SeverityStatus(findings);
        foreach (var assignment in assignments)
        {
            assignment.Status = findings.Any(item => MatchesPin(item, assignment) && item.Severity.Equals("Danger", StringComparison.OrdinalIgnoreCase))
                ? "Danger"
                : findings.Any(item => MatchesPin(item, assignment) && item.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                    ? "Warning"
                    : "Approved";
        }

        var plan = new EmbeddedFirmwarePlan
        {
            DeviceName = deviceName,
            BoardFamily = boardFamily,
            BoardName = boardName,
            BoardProfileKey = boardProfileKey,
            Framework = framework,
            TelemetryTransport = transport,
            BaudRate = baud,
            TelemetryIntervalMilliseconds = interval,
            OverallStatus = status,
            PinAssignments = assignments,
            ProtocolBindings = bindings,
            Findings = findings,
            WiringValidation = wiringValidation,
            WiringDraft = request.WiringDraft,
            LearningRoundAdvice = "After wiring, capture one dry-run telemetry packet without enabling automation. Feed the learning round the exact board revision, current board-profile source, measured wet/dry or min/max calibration ranges, physical bus addresses, observed packets, compiler versions, and boot/reset anomalies. The Council should maintain the project/workspace regex and knowledge records before any compile, serial-device or flash approval."
        };
        plan.TransportContracts = await BuildTransportContractsAsync(plan, cancellationToken).ConfigureAwait(false);
        plan.OneWireContract = BuildOneWireContract(plan.TransportContracts);
        plan.WiringSteps = BuildWiringSteps(plan, profile);
        plan.ArduinoSketch = BuildArduinoSketch(plan);
        plan.PlatformIoConfiguration = BuildPlatformIoConfiguration(plan, profile);
        plan.WiringMarkdown = BuildWiringMarkdown(plan, profile, request.AdditionalRequirements);
        return plan;
    }

    private List<EmbeddedPinAssignment> NormalizeAssignments(EmbeddedFirmwarePlanRequest request, EmbeddedBoardProfile? profile)
    {
        var assignments = (request.Pins ?? []).Where(pin => pin is not null).Select((pin, index) =>
        {
            var pinKey = Text(pin.PinKey, ResolvePinKey(profile, pin.Gpio), 80);
            var protocolKey = NormalizeProtocol(pin.ProtocolKey, pin.Mode);
            return new EmbeddedPinAssignment
            {
                PinKey = pinKey,
                Gpio = pin.Gpio,
                Function = Text(pin.Function, "sensor input", 120),
                Mode = NormalizeMode(pin.Mode, protocolKey),
                ProtocolKey = protocolKey,
                BusKey = Text(pin.BusKey, protocolKey == EmbeddedProtocolKeys.PhysicalOneWire ? $"onewire-{pinKey}" : string.Empty, 120),
                SensorKey = Text(pin.SensorKey, $"sensor-{SanitizeIdentifier(pinKey, index).ToLowerInvariant()}", 120),
                Metric = Text(pin.Metric, "reading", 80),
                Unit = Text(pin.Unit, "raw", 80),
                SupplyVoltage = pin.SupplyVoltage <= 0 ? profile?.LogicVoltage ?? 3.3 : pin.SupplyVoltage,
                Notes = Text(pin.Notes, string.Empty, 1000)
            };
        }).ToList();

        foreach (var sensor in (request.Sensors ?? []).Where(sensor => sensor is not null))
        {
            var gpio = sensor.PreferredGpio ?? profile?.Pins.FirstOrDefault(item => string.Equals(item.PinKey, sensor.PreferredPinKey, StringComparison.OrdinalIgnoreCase))?.Gpio;
            if (gpio is null) continue;
            var pinKey = Text(sensor.PreferredPinKey, ResolvePinKey(profile, gpio.Value), 80);
            var protocolKey = NormalizeProtocol(sensor.ProtocolKey, sensor.Interface);
            if (assignments.Any(item => string.Equals(item.PinKey, pinKey, StringComparison.OrdinalIgnoreCase) && string.Equals(item.SensorKey, sensor.Key, StringComparison.OrdinalIgnoreCase)))
                continue;
            assignments.Add(new EmbeddedPinAssignment
            {
                PinKey = pinKey,
                Gpio = gpio.Value,
                Function = Text(sensor.SensorType, "sensor input", 120),
                Mode = NormalizeMode(sensor.Interface, protocolKey),
                ProtocolKey = protocolKey,
                BusKey = Text(sensor.BusKey, protocolKey == EmbeddedProtocolKeys.PhysicalOneWire ? $"onewire-{pinKey}" : string.Empty, 120),
                SensorKey = Text(sensor.Key, $"sensor-{pinKey.ToLowerInvariant()}", 120),
                Metric = Text(sensor.Metric, InferMetric(sensor.SensorType), 80),
                Unit = Text(sensor.Unit, DefaultUnit(sensor.SensorType), 80),
                SupplyVoltage = sensor.SupplyVoltage <= 0 ? profile?.LogicVoltage ?? 3.3 : sensor.SupplyVoltage,
                Notes = Text(string.Join(" ", new[] { sensor.DriverKey, sensor.Notes }.Where(item => !string.IsNullOrWhiteSpace(item))), string.Empty, 1000)
            });
        }
        return assignments.OrderBy(item => item.Gpio).ThenBy(item => item.SensorKey, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private List<EmbeddedProtocolBinding> NormalizeBindings(EmbeddedFirmwarePlanRequest request, IReadOnlyList<EmbeddedPinAssignment> assignments, string transport)
    {
        var bindings = (request.ProtocolBindings ?? []).Where(binding => binding is not null).Select((binding, index) => new EmbeddedProtocolBinding
        {
            Key = Text(binding.Key, $"protocol-{index + 1}", 120),
            ProtocolKey = NormalizeProtocol(binding.ProtocolKey, string.Empty),
            Role = Text(binding.Role, "Sensor", 120),
            Direction = Text(binding.Direction, "Input", 120),
            PinKeys = (binding.PinKeys ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => Text(item, string.Empty, 80)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            TargetController = Text(binding.TargetController, string.Empty, 160),
            TargetMethod = Text(binding.TargetMethod, string.Empty, 160),
            CapabilityKey = Text(binding.CapabilityKey, string.Empty, 200),
            SettingsJson = NormalizeJson(binding.SettingsJson)
        }).ToList();
        foreach (var group in assignments.Where(item => !string.IsNullOrWhiteSpace(item.BusKey)).GroupBy(item => item.BusKey, StringComparer.OrdinalIgnoreCase))
        {
            if (bindings.Any(item => string.Equals(item.Key, group.Key, StringComparison.OrdinalIgnoreCase))) continue;
            bindings.Add(new EmbeddedProtocolBinding
            {
                Key = group.Key,
                ProtocolKey = group.First().ProtocolKey,
                Role = "SensorBus",
                Direction = "Input",
                PinKeys = group.Select(item => item.PinKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            });
        }
        if (!bindings.Any(item => string.Equals(item.ProtocolKey, transport, StringComparison.OrdinalIgnoreCase)))
        {
            bindings.Add(new EmbeddedProtocolBinding
            {
                Key = "localgpt-telemetry",
                ProtocolKey = transport,
                Role = "TelemetryTransport",
                Direction = "DeviceToLocalGpt",
                TargetController = "EmbeddedTelemetry",
                TargetMethod = "PublishSensorBatch",
                CapabilityKey = "embedded.sensor.telemetry.publish",
                SettingsJson = JsonSerializer.Serialize(new { baudRate = request.BaudRate })
            });
        }
        return bindings;
    }

    private List<EmbeddedPlanFinding> ReviewAssignments(
        EmbeddedBoardProfile? profile,
        string boardFamily,
        string transport,
        IReadOnlyList<EmbeddedPinAssignment> assignments,
        IReadOnlyList<EmbeddedProtocolBinding> bindings,
        IReadOnlyList<EmbeddedProtocolDescriptor> protocols)
    {
        var findings = new List<EmbeddedPlanFinding>();
        if (profile is null)
            findings.Add(new("Danger", "BOARD_PROFILE_MISSING", "The selected board profile is not installed. Import or create the exact board profile before artifacts are approved."));
        else if (profile.Status.Contains("Danger", StringComparison.OrdinalIgnoreCase))
            findings.Add(new("Danger", "BOARD_PROFILE_PLACEHOLDER", "The selected profile is only a family placeholder. Select an exact source-controlled board profile."));
        else if (!profile.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            findings.Add(new("Warning", "BOARD_PROFILE_REVIEW", "The selected source-controlled board profile still requires confirmation against the exact board schematic/data sheet."));
        if (assignments.Count == 0)
            findings.Add(new("Warning", "NO_PINS", "No GPIO assignments were supplied. The Council must infer or request a bounded pin layout before firmware artifacts are created."));

        foreach (var duplicate in assignments.GroupBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            var sameSharedBus = duplicate.All(item => item.ProtocolKey == EmbeddedProtocolKeys.PhysicalOneWire) && duplicate.Select(item => item.BusKey).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
            if (!sameSharedBus)
                findings.Add(new("Danger", "DUPLICATE_PIN", $"Pin {duplicate.Key} is assigned more than once without one compatible shared bus.", duplicate.First().Gpio, duplicate.Key));
            else
                findings.Add(new("Warning", "ONEWIRE_SHARED_PIN", $"Pin {duplicate.Key} hosts multiple physical 1-Wire sensors. Store and verify their ROM addresses during the learning round.", duplicate.First().Gpio, duplicate.Key));
        }

        foreach (var pin in assignments)
        {
            var pinProfile = profile?.Pins.FirstOrDefault(item =>
                string.Equals(item.PinKey, pin.PinKey, StringComparison.OrdinalIgnoreCase) || item.Gpio == pin.Gpio);
            if (pinProfile is null && profile?.Pins.Count > 0)
                findings.Add(new("Danger", "PIN_NOT_IN_PROFILE", $"Pin {pin.PinKey} / GPIO {pin.Gpio} is not present in board profile '{profile.Key}'.", pin.Gpio, pin.PinKey));
            if (pinProfile?.IsReserved == true)
                findings.Add(new("Danger", "PIN_RESERVED", $"Pin {pin.PinKey} is reserved. {pinProfile.Warning}", pin.Gpio, pin.PinKey));
            if (pinProfile?.IsInputOnly == true && IsOutputMode(pin.Mode))
                findings.Add(new("Danger", "INPUT_ONLY", $"Pin {pin.PinKey} is input-only and cannot drive an output.", pin.Gpio, pin.PinKey));
            if (pinProfile?.IsBootStrap == true)
                findings.Add(new("Warning", "BOOT_STRAP", $"Pin {pin.PinKey} is a boot-strapping pin. {pinProfile.Warning}", pin.Gpio, pin.PinKey));
            if (profile is not null && pin.SupplyVoltage - profile.LogicVoltage > 0.25)
                findings.Add(new("Danger", "VOLTAGE", $"Pin {pin.PinKey} is planned at {pin.SupplyVoltage:0.##} V but the board profile logic voltage is {profile.LogicVoltage:0.##} V. Add an approved level/voltage interface.", pin.Gpio, pin.PinKey));
            if (pinProfile is not null && pinProfile.Capabilities.Count > 0 && !pinProfile.Capabilities.Contains(pin.ProtocolKey, StringComparer.OrdinalIgnoreCase))
                findings.Add(new("Warning", "PIN_PROTOCOL", $"Pin {pin.PinKey} does not advertise protocol '{pin.ProtocolKey}' in the selected board profile.", pin.Gpio, pin.PinKey));
            if (boardFamily.Contains("ESP32", StringComparison.OrdinalIgnoreCase) && IsEsp32Adc2Pin(pin.Gpio) && pin.ProtocolKey == EmbeddedProtocolKeys.AnalogAdc)
                findings.Add(new("Warning", "ADC2_WIFI", $"Pin {pin.PinKey} is commonly on ADC2 for classic ESP32 targets; analog reads may conflict with Wi-Fi.", pin.Gpio, pin.PinKey));
            if (pin.ProtocolKey == EmbeddedProtocolKeys.PhysicalOneWire)
                findings.Add(new("Warning", "PHYSICAL_ONEWIRE_PULLUP", $"Pin {pin.PinKey} physical 1-Wire data normally needs a suitable pull-up, common ground and verified bus topology. This is separate from LocalGPT logical 1-Wire messaging.", pin.Gpio, pin.PinKey));
            var protocol = protocols.FirstOrDefault(item => string.Equals(item.Key, pin.ProtocolKey, StringComparison.OrdinalIgnoreCase));
            if (protocol is null)
                findings.Add(new("Warning", "PROTOCOL_CUSTOM", $"Protocol '{pin.ProtocolKey}' needs an explicit custom driver and validation contract.", pin.Gpio, pin.PinKey));
            else if (!protocol.SupportedByGeneratedSketch)
                findings.Add(new("Warning", "DRIVER_REQUIRED", $"Protocol '{protocol.DisplayName}' is represented in the plan but the minimal generated sketch contains only a safe placeholder until a reviewed driver is selected.", pin.Gpio, pin.PinKey));
        }

        foreach (var binding in bindings)
        {
            try { using var _ = JsonDocument.Parse(string.IsNullOrWhiteSpace(binding.SettingsJson) ? "{}" : binding.SettingsJson); }
            catch (JsonException) { findings.Add(new("Danger", "PROTOCOL_SETTINGS_JSON", $"Protocol binding '{binding.Key}' has invalid settings JSON.")); }
        }
        if (!transport.Equals(EmbeddedProtocolKeys.SerialJsonLines, StringComparison.OrdinalIgnoreCase))
            findings.Add(new("Warning", "TRANSPORT_ADAPTER", $"Transport '{transport}' requires an explicitly approved gateway/driver. The minimal generated sketch emits serial JSON lines unless its transport section is reviewed and replaced."));
        return findings;
    }

    private async Task<List<EmbeddedTransportContract>> BuildTransportContractsAsync(EmbeddedFirmwarePlan plan, CancellationToken cancellationToken)
    {
        var first = plan.PinAssignments.FirstOrDefault();
        var telemetryRequest = new EmbeddedTelemetryBridgeRequest
        {
            DeviceId = plan.DeviceName,
            BoardProfileKey = plan.BoardProfileKey,
            TransportKey = plan.TelemetryTransport,
            Sequence = 1,
            DeviceTimestampMilliseconds = 123456,
            Readings =
            [
                new EmbeddedTelemetryReading
                {
                    SensorKey = first?.SensorKey ?? "soil-01",
                    PinKey = first?.PinKey ?? "GPIO34",
                    Gpio = first?.Gpio ?? 34,
                    Metric = first?.Metric ?? "moisture",
                    Value = 624,
                    Unit = first?.Unit ?? "raw_adc",
                    Quality = "dry-run"
                }
            ]
        };
        var bridge = await telemetryBridge.PreviewAsync(telemetryRequest, cancellationToken).ConfigureAwait(false);
        var oneWirePreview = await telemetryBridge.CreateOneWireEnvelopeAsync(telemetryRequest, cancellationToken).ConfigureAwait(false);
        return
        [
            new EmbeddedTransportContract
            {
                ProtocolKey = plan.TelemetryTransport,
                DisplayName = "Embedded edge telemetry packet",
                Direction = "DeviceToLocalGptGateway",
                Boundary = "The device emits a compact packet over the selected local transport. No LocalGPT trust secret is embedded in generated firmware.",
                Controller = "DxAiFunctions",
                Method = "POST",
                CapabilityKey = "embedded.sensor.telemetry.publish",
                RequiresGateway = true,
                RequiresOneWireSecurity = false,
                ExampleEnvelopeJson = bridge.EdgeEnvelopeJson
            },
            new EmbeddedTransportContract
            {
                ProtocolKey = EmbeddedProtocolKeys.LocalGptOneWire,
                DisplayName = "Protected LocalGPT logical 1-Wire invocation",
                Direction = "TrustedGatewayToLocalGpt",
                Boundary = "A trusted local gateway validates bounds, source identity, replay/timestamp policy and capability routing, then protects the LocalGPT envelope.",
                Controller = "DxAiFunctions",
                Method = "POST",
                CapabilityKey = "embedded.sensor.telemetry.publish",
                RequiresGateway = true,
                RequiresOneWireSecurity = true,
                ExampleEnvelopeJson = oneWirePreview.OneWireEnvelope is null
                    ? JsonSerializer.Serialize(new { status = oneWirePreview.Status, findings = oneWirePreview.Findings }, artifactJsonOptions)
                    : JsonSerializer.Serialize(oneWirePreview.OneWireEnvelope, artifactJsonOptions)
            }
        ];
    }

    private EmbeddedOneWireContract BuildOneWireContract(IReadOnlyList<EmbeddedTransportContract> contracts)
    {
        var logical = contracts.FirstOrDefault(item => item.ProtocolKey == EmbeddedProtocolKeys.LocalGptOneWire);
        return new EmbeddedOneWireContract
        {
            ProtocolVersion = OneWireProtocol.Version,
            Controller = logical?.Controller ?? "DxAiFunctions",
            Method = logical?.Method ?? "POST",
            CapabilityKey = logical?.CapabilityKey ?? "embedded.sensor.telemetry.publish",
            Direction = logical?.Direction ?? "TrustedGatewayToLocalGpt",
            TransportBoundary = logical?.Boundary ?? string.Empty,
            ExampleEnvelopeJson = logical?.ExampleEnvelopeJson ?? string.Empty
        };
    }

    private List<string> BuildWiringSteps(EmbeddedFirmwarePlan plan, EmbeddedBoardProfile? profile)
    {
        var steps = new List<string>
        {
            "Disconnect power before changing wiring.",
            "Confirm the exact board profile, carrier schematic and logic voltage; a family name is not enough for flash approval.",
            "Use one common ground between the board, sensors and any approved local transport gateway unless an intentional isolated interface is documented.",
            "Verify every sensor output stays inside the selected board pin voltage range; use level shifting, dividers, transceivers or isolation where required.",
            $"For serial JSON lines, verify TX/RX voltage levels, cross TX to RX, share ground and use {plan.BaudRate} baud."
        };
        if (profile is not null)
            steps.AddRange(profile.Notes.Select(note => $"Board profile note: {note}"));
        steps.AddRange(plan.PinAssignments.Select(item => $"{item.PinKey} / GPIO {item.Gpio}: {item.Function}; mode {item.Mode}; protocol {item.ProtocolKey}; sensor '{item.SensorKey}'; metric '{item.Metric}' ({item.Unit}); planned supply {item.SupplyVoltage:0.##} V."));
        steps.Add("Capture one edge telemetry packet and let LocalGPT validate it before converting it into a protected logical 1-Wire capability invocation.");
        return steps;
    }

    private string BuildArduinoSketch(EmbeddedFirmwarePlan plan)
    {
        var physicalOneWireGroups = plan.PinAssignments
            .Where(item => item.ProtocolKey == EmbeddedProtocolKeys.PhysicalOneWire)
            .GroupBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var includes = physicalOneWireGroups.Count > 0 ? "#include <OneWire.h>\n#include <DallasTemperature.h>" : string.Empty;
        var declarations = string.Join(Environment.NewLine, plan.PinAssignments
            .GroupBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase)
            .Select((group, index) => $"constexpr int PIN_{SanitizeIdentifier(group.Key, index)} = {group.First().Gpio};"));
        var oneWireDeclarations = string.Join(Environment.NewLine, physicalOneWireGroups.Select((group, index) =>
            $"OneWire oneWireBus{index}(PIN_{SanitizeIdentifier(group.Key, index)});\nDallasTemperature oneWireSensors{index}(&oneWireBus{index});"));
        var setupPins = string.Join(Environment.NewLine, plan.PinAssignments
            .GroupBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase)
            .Select((group, index) => $"  pinMode(PIN_{SanitizeIdentifier(group.Key, index)}, {ArduinoPinMode(group.First().Mode)});"));
        var oneWireSetup = string.Join(Environment.NewLine, physicalOneWireGroups.Select((_, index) => $"  oneWireSensors{index}.begin();"));
        var emitCalls = new StringBuilder();
        var pinGroups = plan.PinAssignments.GroupBy(item => item.PinKey, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var assignment in plan.PinAssignments)
        {
            var pinIndex = pinGroups.FindIndex(group => string.Equals(group.Key, assignment.PinKey, StringComparison.OrdinalIgnoreCase));
            var constant = $"PIN_{SanitizeIdentifier(assignment.PinKey, pinIndex)}";
            if (assignment.ProtocolKey == EmbeddedProtocolKeys.PhysicalOneWire)
            {
                var busIndex = physicalOneWireGroups.FindIndex(group => string.Equals(group.Key, assignment.PinKey, StringComparison.OrdinalIgnoreCase));
                var sensorIndex = physicalOneWireGroups[busIndex].ToList().FindIndex(item => ReferenceEquals(item, assignment));
                emitCalls.AppendLine($"  oneWireSensors{busIndex}.requestTemperatures();");
                emitCalls.AppendLine($"  publishReading(\"{EscapeCpp(assignment.SensorKey)}\", \"{EscapeCpp(assignment.PinKey)}\", {assignment.Gpio}, \"{EscapeCpp(assignment.Metric)}\", oneWireSensors{busIndex}.getTempCByIndex({Math.Max(0, sensorIndex)}), \"{EscapeCpp(assignment.Unit)}\");");
            }
            else if (assignment.ProtocolKey == EmbeddedProtocolKeys.AnalogAdc)
            {
                emitCalls.AppendLine($"  publishReading(\"{EscapeCpp(assignment.SensorKey)}\", \"{EscapeCpp(assignment.PinKey)}\", {assignment.Gpio}, \"{EscapeCpp(assignment.Metric)}\", analogRead({constant}), \"{EscapeCpp(assignment.Unit)}\");");
            }
            else if (assignment.ProtocolKey == EmbeddedProtocolKeys.DigitalGpio)
            {
                emitCalls.AppendLine($"  publishReading(\"{EscapeCpp(assignment.SensorKey)}\", \"{EscapeCpp(assignment.PinKey)}\", {assignment.Gpio}, \"{EscapeCpp(assignment.Metric)}\", digitalRead({constant}), \"{EscapeCpp(assignment.Unit)}\");");
            }
            else
            {
                emitCalls.AppendLine($"  // TODO reviewed driver for {EscapeCpp(assignment.ProtocolKey)} on {EscapeCpp(assignment.PinKey)}; placeholder is intentionally not transmitted.");
            }
        }

        var sketch = new StringBuilder();
        sketch.AppendLine("// Generated by LocalGPT as a reviewable planning artifact.");
        sketch.AppendLine("// Verify the exact board, electrical limits, drivers and physical wiring before compile or flash.");
        sketch.AppendLine("#include <Arduino.h>");
        if (!string.IsNullOrWhiteSpace(includes))
            sketch.AppendLine(includes);
        sketch.AppendLine();
        sketch.AppendLine($"constexpr unsigned long TELEMETRY_INTERVAL_MS = {plan.TelemetryIntervalMilliseconds}UL;");
        if (!string.IsNullOrWhiteSpace(declarations))
            sketch.AppendLine(declarations);
        if (!string.IsNullOrWhiteSpace(oneWireDeclarations))
            sketch.AppendLine(oneWireDeclarations);
        sketch.AppendLine("unsigned long lastTelemetry = 0;");
        sketch.AppendLine("unsigned long sequenceNumber = 0;");
        sketch.AppendLine();
        sketch.AppendLine("void publishReading(const char* sensorKey, const char* pinKey, int gpio, const char* metric, double value, const char* unit) {");
        sketch.AppendLine($"  Serial.print(\"{{\\\"schema\\\":\\\"localgpt.embedded.telemetry.v1\\\",\\\"deviceId\\\":\\\"{EscapeCpp(plan.DeviceName)}\\\",\\\"boardProfileKey\\\":\\\"{EscapeCpp(plan.BoardProfileKey)}\\\",\\\"transportKey\\\":\\\"{EscapeCpp(EmbeddedProtocolKeys.SerialJsonLines)}\\\",\\\"sequence\\\":\");");
        sketch.AppendLine("  Serial.print(++sequenceNumber);");
        sketch.AppendLine("  Serial.print(\",\\\"deviceTimestampMilliseconds\\\":\"); Serial.print(millis());");
        sketch.AppendLine("  Serial.print(\",\\\"readings\\\":[{\\\"sensorKey\\\":\\\"\"); Serial.print(sensorKey);");
        sketch.AppendLine("  Serial.print(\"\\\",\\\"pinKey\\\":\\\"\"); Serial.print(pinKey);");
        sketch.AppendLine("  Serial.print(\"\\\",\\\"gpio\\\":\"); Serial.print(gpio);");
        sketch.AppendLine("  Serial.print(\",\\\"metric\\\":\\\"\"); Serial.print(metric);");
        sketch.AppendLine("  Serial.print(\"\\\",\\\"value\\\":\"); Serial.print(value, 4);");
        sketch.AppendLine("  Serial.print(\",\\\"unit\\\":\\\"\"); Serial.print(unit);");
        sketch.AppendLine("  Serial.println(\"\\\",\\\"quality\\\":\\\"raw\\\"}],\\\"metadataJson\\\":\\\"{}\\\"}\");");
        sketch.AppendLine("}");
        sketch.AppendLine();
        sketch.AppendLine("void setup() {");
        sketch.AppendLine($"  Serial.begin({plan.BaudRate});");
        if (!string.IsNullOrWhiteSpace(setupPins))
            sketch.AppendLine(setupPins);
        if (!string.IsNullOrWhiteSpace(oneWireSetup))
            sketch.AppendLine(oneWireSetup);
        sketch.AppendLine("}");
        sketch.AppendLine();
        sketch.AppendLine("void loop() {");
        sketch.AppendLine("  const unsigned long now = millis();");
        sketch.AppendLine("  if (now - lastTelemetry < TELEMETRY_INTERVAL_MS) return;");
        sketch.AppendLine("  lastTelemetry = now;");
        var emitted = emitCalls.ToString().TrimEnd();
        if (!string.IsNullOrWhiteSpace(emitted))
            sketch.AppendLine(emitted);
        sketch.AppendLine("}");
        return sketch.ToString();
    }

    private string BuildPlatformIoConfiguration(EmbeddedFirmwarePlan plan, EmbeddedBoardProfile? profile)
    {
        var board = string.IsNullOrWhiteSpace(profile?.PlatformIoBoard) ? "esp32dev" : profile.PlatformIoBoard;
        var platform = (profile?.Family ?? plan.BoardFamily).Contains("AVR", StringComparison.OrdinalIgnoreCase) || board.Equals("uno", StringComparison.OrdinalIgnoreCase)
            ? "atmelavr"
            : "espressif32";
        var libraryDependencies = plan.PinAssignments.Any(item => item.ProtocolKey == EmbeddedProtocolKeys.PhysicalOneWire)
            ? "lib_deps =\n  paulstoffregen/OneWire\n  milesburton/DallasTemperature"
            : string.Empty;
        var configuration = new StringBuilder();
        configuration.AppendLine("[platformio]");
        configuration.AppendLine("default_envs = localgpt-embedded");
        configuration.AppendLine();
        configuration.AppendLine("[env:localgpt-embedded]");
        configuration.AppendLine($"platform = {platform}");
        configuration.AppendLine($"board = {board}");
        configuration.AppendLine("framework = arduino");
        configuration.AppendLine($"monitor_speed = {plan.BaudRate}");
        if (!string.IsNullOrWhiteSpace(libraryDependencies))
            configuration.AppendLine(libraryDependencies);
        configuration.AppendLine("build_flags =");
        configuration.AppendLine($"  -DLOCALGPT_DEVICE_NAME=\\\"{EscapeIni(plan.DeviceName)}\\\"");
        configuration.AppendLine($"  -DLOCALGPT_BOARD_PROFILE=\\\"{EscapeIni(plan.BoardProfileKey)}\\\"");
        return configuration.ToString();
    }

    private string BuildWiringMarkdown(EmbeddedFirmwarePlan plan, EmbeddedBoardProfile? profile, string additionalRequirements)
    {
        var rows = plan.PinAssignments.Count == 0
            ? "| _none_ | _none_ | _Council input required_ | | | |\n"
            : string.Join(Environment.NewLine, plan.PinAssignments.Select(pin => $"| {pin.PinKey} | {pin.Gpio} | {pin.Function} | {pin.ProtocolKey} | {pin.SensorKey} | {pin.Status} |"));
        var findings = plan.Findings.Count == 0
            ? "- Approved: no deterministic conflicts were detected; exact board and electrical review is still required."
            : string.Join(Environment.NewLine, plan.Findings.Select(item => $"- **{item.Severity} / {item.Code}:** {item.Message}"));
        var steps = string.Join(Environment.NewLine, plan.WiringSteps.Select((step, index) => $"{index + 1}. {step}"));
        var contracts = string.Join(Environment.NewLine + Environment.NewLine, plan.TransportContracts.Select(contract => $"### {contract.DisplayName}\n\nProtocol: `{contract.ProtocolKey}`  \nBoundary: {contract.Boundary}\n\n```json\n{contract.ExampleEnvelopeJson}\n```"));
        var profileNotes = profile is null ? "- Board profile missing." : string.Join(Environment.NewLine, profile.Notes.Select(note => $"- {note}"));
        var requirements = string.IsNullOrWhiteSpace(additionalRequirements) ? "None supplied." : additionalRequirements.Trim();
        var markdown = new StringBuilder();
        markdown.AppendLine($"# {plan.DeviceName} wiring and LocalGPT telemetry plan");
        markdown.AppendLine();
        markdown.AppendLine($"Status: **{plan.OverallStatus}**");
        markdown.AppendLine();
        markdown.AppendLine($"Board: {plan.BoardFamily} / {plan.BoardName}  ");
        markdown.AppendLine($"Board profile: `{plan.BoardProfileKey}`  ");
        markdown.AppendLine($"Framework: {plan.Framework}  ");
        markdown.AppendLine($"Transport: `{plan.TelemetryTransport}` at {plan.BaudRate} baud");
        markdown.AppendLine();
        markdown.AppendLine("## Board-profile notes");
        markdown.AppendLine();
        markdown.AppendLine(profileNotes);
        markdown.AppendLine();
        markdown.AppendLine("## Pin assignment");
        markdown.AppendLine();
        markdown.AppendLine("| Pin | GPIO | Function | Protocol | Sensor key | Review |");
        markdown.AppendLine("|---|---:|---|---|---|---|");
        markdown.AppendLine(rows);
        markdown.AppendLine();
        markdown.AppendLine("## Deterministic review");
        markdown.AppendLine();
        markdown.AppendLine(findings);
        markdown.AppendLine();
        markdown.AppendLine("## Wiring order");
        markdown.AppendLine();
        markdown.AppendLine(steps);
        markdown.AppendLine();
        markdown.AppendLine("## Transport contracts");
        markdown.AppendLine();
        markdown.AppendLine(contracts);
        markdown.AppendLine();
        markdown.AppendLine("The embedded device packet is not itself a trusted LocalGPT command. A local gateway validates source identity, bounds, replay/timestamp policy and capability routing before creating a protected logical 1-Wire envelope. Physical 1-Wire, I²C, SPI, UART, CAN, RS-485 or custom sensor buses remain independent choices.");
        markdown.AppendLine();
        markdown.AppendLine("## Additional user requirements");
        markdown.AppendLine();
        markdown.AppendLine(requirements);
        markdown.AppendLine();
        markdown.AppendLine("## Learning round");
        markdown.AppendLine();
        markdown.AppendLine(plan.LearningRoundAdvice);
        return markdown.ToString();
    }

    private string NormalizeMode(string? mode, string protocolKey)
    {
        var value = Text(mode, "Input", 80);
        if (protocolKey == EmbeddedProtocolKeys.AnalogAdc || value.Contains("analog", StringComparison.OrdinalIgnoreCase)) return "AnalogInput";
        if (protocolKey == EmbeddedProtocolKeys.PhysicalOneWire || value.Contains("onewire", StringComparison.OrdinalIgnoreCase) || value.Contains("1-wire", StringComparison.OrdinalIgnoreCase)) return "PhysicalOneWire";
        if (value.Contains("output", StringComparison.OrdinalIgnoreCase) || value.Contains("drive", StringComparison.OrdinalIgnoreCase)) return "Output";
        if (value.Contains("pullup", StringComparison.OrdinalIgnoreCase)) return "InputPullup";
        return "Input";
    }

    private string NormalizeProtocol(string? protocolKey, string? hint)
    {
        if (!string.IsNullOrWhiteSpace(protocolKey))
        {
            var value = protocolKey.Trim().ToLowerInvariant();
            if (value is not "analog" and not "digital" and not "onewire" and not "1-wire") return value;
        }
        var text = $"{protocolKey} {hint}";
        if ((text.Contains("physical", StringComparison.OrdinalIgnoreCase) && text.Contains("wire", StringComparison.OrdinalIgnoreCase)) || text.Contains("ds18", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.PhysicalOneWire;
        if (text.Contains("onewire", StringComparison.OrdinalIgnoreCase) || text.Contains("1-wire", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.PhysicalOneWire;
        if (text.Contains("analog", StringComparison.OrdinalIgnoreCase) || text.Contains("adc", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.AnalogAdc;
        if (text.Contains("pwm", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.Pwm;
        if (text.Contains("i2c", StringComparison.OrdinalIgnoreCase) || text.Contains("i²c", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.I2c;
        if (text.Contains("spi", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.Spi;
        if (text.Contains("uart", StringComparison.OrdinalIgnoreCase) || text.Contains("serial", StringComparison.OrdinalIgnoreCase)) return EmbeddedProtocolKeys.Uart;
        return EmbeddedProtocolKeys.DigitalGpio;
    }

    private string NormalizeTransport(string? value)
    {
        var transport = Text(value, EmbeddedProtocolKeys.SerialJsonLines, 120).ToLowerInvariant();
        return transport switch
        {
            "serialjsonlines" or "serial-json-lines" or "serial" => EmbeddedProtocolKeys.SerialJsonLines,
            "httpjson" or "http-json" => EmbeddedProtocolKeys.HttpJson,
            "mqtt" => EmbeddedProtocolKeys.Mqtt,
            "onewire" or "localgpt-onewire" => EmbeddedProtocolKeys.LocalGptOneWire,
            _ => transport
        };
    }

    private string ResolvePinKey(EmbeddedBoardProfile? profile, int gpio) => profile?.Pins.FirstOrDefault(item => item.Gpio == gpio)?.PinKey ?? $"GPIO{gpio}";
    private string InferMetric(string? sensorType) => sensorType?.Contains("moist", StringComparison.OrdinalIgnoreCase) == true ? "moisture" : sensorType?.Contains("temp", StringComparison.OrdinalIgnoreCase) == true ? "temperature" : "reading";
    private string DefaultUnit(string? sensorType) => sensorType?.Contains("temp", StringComparison.OrdinalIgnoreCase) == true ? "celsius" : sensorType?.Contains("moist", StringComparison.OrdinalIgnoreCase) == true ? "raw_adc" : "raw";
    private bool IsEsp32Adc2Pin(int gpio) => gpio is 0 or 2 or 4 or 12 or 13 or 14 or 15 or 25 or 26 or 27;
    private bool IsOutputMode(string? mode) => (mode ?? string.Empty).Contains("Output", StringComparison.OrdinalIgnoreCase);
    private string ArduinoPinMode(string mode) => mode == "Output" ? "OUTPUT" : mode == "InputPullup" || mode == "PhysicalOneWire" ? "INPUT_PULLUP" : "INPUT";
    private bool MatchesPin(EmbeddedPlanFinding finding, EmbeddedPinAssignment assignment) => finding.Gpio == assignment.Gpio || (!string.IsNullOrWhiteSpace(finding.PinKey) && string.Equals(finding.PinKey, assignment.PinKey, StringComparison.OrdinalIgnoreCase));
    private string SeverityStatus(IEnumerable<EmbeddedPlanFinding> findings) => findings.Any(item => string.Equals(item.Severity, "Danger", StringComparison.OrdinalIgnoreCase)) ? "Danger" : findings.Any(item => string.Equals(item.Severity, "Warning", StringComparison.OrdinalIgnoreCase)) ? "Warning" : "Approved";
    private string SanitizeIdentifier(string value, int index)
    {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_').ToArray();
        var result = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(result) ? $"PIN_{index}" : result;
    }
    private string EscapeCpp(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    private string EscapeIni(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    private string NormalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "{}";
        try { using var document = JsonDocument.Parse(value); return document.RootElement.GetRawText(); }
        catch (JsonException) { return value.Trim(); }
    }
    private string Text(string? value, string fallback, int maximum)
    {
        var result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return result.Length <= maximum ? result : result[..maximum];
    }
    private string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "embedded-node" : cleaned;
    }
}
