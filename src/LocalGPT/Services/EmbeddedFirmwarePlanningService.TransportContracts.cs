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
    /// Builds transport contracts as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="plan">Plan value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<List<EmbeddedTransportContract>> BuildTransportContractsAsync(EmbeddedFirmwarePlan plan, CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildTransportContractsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildTransportContractsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds one wire contract as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="contracts">Embedded transport contract dependency used by the embedded firmware planning workflow to provide the corresponding application capability.</param>
    /// <returns>The embedded one wire contract produced by the operation.</returns>
    private EmbeddedOneWireContract BuildOneWireContract(IReadOnlyList<EmbeddedTransportContract> contracts)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildOneWireContract)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildOneWireContract)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds wiring steps as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="plan">Plan value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <param name="profile">Profile value supplied to the embedded firmware planning operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> BuildWiringSteps(EmbeddedFirmwarePlan plan, EmbeddedBoardProfile? profile)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildWiringSteps)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(BuildWiringSteps)} failed.");
        throw;
    }
}

    }
}
