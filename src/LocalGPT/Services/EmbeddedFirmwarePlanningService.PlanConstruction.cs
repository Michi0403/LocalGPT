using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates embedded firmware planning behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class EmbeddedFirmwarePlanningService
    {
    /// <summary>
    /// Builds plan as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded firmware plan produced by the operation.</returns>
    private async Task<EmbeddedFirmwarePlan> BuildPlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildPlanAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildPlanAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes assignments as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="profile">Profile value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<EmbeddedPinAssignment> NormalizeAssignments(EmbeddedFirmwarePlanRequest request, EmbeddedBoardProfile? profile)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeAssignments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeAssignments)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes bindings as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="assignments">Embedded pin assignment dependency used by the embedded firmware planning workflow to provide the corresponding application capability.</param>
    /// <param name="transport">Transport value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<EmbeddedProtocolBinding> NormalizeBindings(EmbeddedFirmwarePlanRequest request, IReadOnlyList<EmbeddedPinAssignment> assignments, string transport)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeBindings)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(NormalizeBindings)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs review assignments as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="boardFamily">Board family value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="transport">Transport value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="assignments">Embedded pin assignment dependency used by the embedded firmware planning workflow to provide the corresponding application capability.</param>
    /// <param name="bindings">Embedded protocol binding dependency used by the embedded firmware planning workflow to provide the corresponding application capability.</param>
    /// <param name="protocols">Embedded protocol descriptor dependency used by the embedded firmware planning workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<EmbeddedPlanFinding> ReviewAssignments(
        EmbeddedBoardProfile? profile,
        string boardFamily,
        string transport,
        IReadOnlyList<EmbeddedPinAssignment> assignments,
        IReadOnlyList<EmbeddedProtocolBinding> bindings,
        IReadOnlyList<EmbeddedProtocolDescriptor> protocols)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(ReviewAssignments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(ReviewAssignments)} failed.");
        throw;
    }
}

    }
}
