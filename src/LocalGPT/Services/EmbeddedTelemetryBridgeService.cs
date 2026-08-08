using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class EmbeddedTelemetryBridgeService(
    IEmbeddedHardwareCatalogService catalog,
    ILogger<EmbeddedTelemetryBridgeService> logger) : IEmbeddedTelemetryBridgeService
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<EmbeddedTelemetryBridgeResult> PreviewAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var result = await BuildAsync(request, includeOneWireEnvelope: false, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Prepared embedded telemetry edge preview for device {DeviceId} with status {Status}; readings were omitted from logs.", SafeDeviceForLog(request?.DeviceId), result.Status);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(PreviewAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(PreviewAsync)} failed.");
        throw;
    }
}

    public async Task<EmbeddedTelemetryBridgeResult> CreateOneWireEnvelopeAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var result = await BuildAsync(request, includeOneWireEnvelope: true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Prepared embedded telemetry logical 1-Wire envelope for device {DeviceId} with status {Status}; readings were omitted from logs.", SafeDeviceForLog(request?.DeviceId), result.Status);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(CreateOneWireEnvelopeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(CreateOneWireEnvelopeAsync)} failed.");
        throw;
    }
}

    private async Task<EmbeddedTelemetryBridgeResult> BuildAsync(EmbeddedTelemetryBridgeRequest request, bool includeOneWireEnvelope, CancellationToken cancellationToken)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            var findings = new List<EmbeddedPlanFinding>();
            var deviceId = NormalizeToken(request.DeviceId, 120);
            if (string.IsNullOrWhiteSpace(deviceId))
                findings.Add(new("Danger", "DEVICE_ID_REQUIRED", "A stable device id is required."));
            if ((request.Readings?.Count ?? 0) == 0)
                findings.Add(new("Warning", "READINGS_EMPTY", "No sensor readings were supplied."));
            if ((request.Readings?.Count ?? 0) > 128)
                findings.Add(new("Danger", "READINGS_LIMIT", "One edge packet may contain at most 128 readings."));

            var transportKey = string.IsNullOrWhiteSpace(request.TransportKey) ? EmbeddedProtocolKeys.SerialJsonLines : request.TransportKey.Trim().ToLowerInvariant();
            var protocols = await catalog.GetProtocolDescriptorsAsync(cancellationToken).ConfigureAwait(false);
            var transport = protocols.FirstOrDefault(item => string.Equals(item.Key, transportKey, StringComparison.OrdinalIgnoreCase));
            if (transport is null)
                findings.Add(new("Warning", "TRANSPORT_CUSTOM", $"Transport '{transportKey}' is not in the current embedded protocol catalog and requires a custom gateway contract."));
            else if (!(transport.Layer ?? string.Empty).Contains("transport", StringComparison.OrdinalIgnoreCase))
                findings.Add(new("Warning", "TRANSPORT_LAYER", $"Protocol '{transportKey}' is not an edge transport. A separate transport binding is still required."));

            if (!string.IsNullOrWhiteSpace(request.BoardProfileKey) && await catalog.GetBoardProfileAsync(request.BoardProfileKey, cancellationToken).ConfigureAwait(false) is null)
                findings.Add(new("Warning", "BOARD_PROFILE_UNKNOWN", $"Board profile '{request.BoardProfileKey}' is not installed."));

            try
            {
                using var _ = JsonDocument.Parse(string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson);
            }
            catch (JsonException)
            {
                findings.Add(new("Danger", "METADATA_JSON", "Telemetry metadataJson is not valid JSON."));
            }

            var normalizedReadings = new List<object>();
            foreach (var reading in (request.Readings ?? []).Where(reading => reading is not null).Take(128))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sensorKey = NormalizeToken(reading.SensorKey, 120);
                var metric = NormalizeToken(reading.Metric, 80);
                if (string.IsNullOrWhiteSpace(sensorKey))
                    findings.Add(new("Danger", "SENSOR_KEY_REQUIRED", "Every reading requires a stable sensor key.", reading.Gpio, reading.PinKey));
                if (string.IsNullOrWhiteSpace(metric))
                    findings.Add(new("Danger", "METRIC_REQUIRED", $"Reading '{sensorKey}' requires a metric.", reading.Gpio, reading.PinKey));
                if (double.IsNaN(reading.Value) || double.IsInfinity(reading.Value))
                    findings.Add(new("Danger", "VALUE_NOT_FINITE", $"Reading '{sensorKey}' is not finite.", reading.Gpio, reading.PinKey));
                normalizedReadings.Add(new
                {
                    sensorKey,
                    pinKey = NormalizeToken(reading.PinKey, 80),
                    reading.Gpio,
                    metric,
                    value = reading.Value,
                    unit = NormalizeToken(reading.Unit, 80),
                    quality = NormalizeToken(reading.Quality, 40)
                });
            }

            var edgePacket = new
            {
                schema = "localgpt.embedded.telemetry.v1",
                deviceId,
                boardProfileKey = NormalizeToken(request.BoardProfileKey, 160),
                transportKey,
                sequence = Math.Max(0, request.Sequence),
                deviceTimestampMilliseconds = Math.Max(0, request.DeviceTimestampMilliseconds),
                readings = normalizedReadings,
                metadata = ParseMetadata(request.MetadataJson)
            };
            var edgeJson = JsonSerializer.Serialize(edgePacket, jsonOptions);
            if (System.Text.Encoding.UTF8.GetByteCount(edgeJson) > OneWireProtocol.MaximumMessageBytes)
                findings.Add(new("Danger", "PACKET_SIZE", "The normalized edge packet exceeds the LocalGPT logical 1-Wire maximum message size."));

            var status = SeverityStatus(findings);
            OneWireEnvelope? envelope = null;
            if (includeOneWireEnvelope && !status.Equals("Danger", StringComparison.OrdinalIgnoreCase))
            {
                envelope = new OneWireEnvelope
                {
                    MessageType = OneWireMessageType.Invoke,
                    SourcePeerId = deviceId,
                    TargetPeerId = string.IsNullOrWhiteSpace(request.TargetPeerId) ? "localgpt" : NormalizeToken(request.TargetPeerId, 120),
                    Controller = "DxAiFunctions",
                    Method = "POST",
                    Route = "/api/dxai/functions/embedded.sensor.telemetry.publish/invoke",
                    CapabilityKey = "embedded.sensor.telemetry.publish",
                    ExecutionMode = OneWireExecutionMode.SequentialSpool,
                    WorkOrderKey = $"embedded:{deviceId}:{Math.Max(0, request.Sequence)}",
                    UserConfirmed = false,
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["Parameters"] = JsonSerializer.SerializeToElement(new EmbeddedTelemetryBridgeRequest
                        {
                            DeviceId = deviceId,
                            BoardProfileKey = NormalizeToken(request.BoardProfileKey, 160),
                            TransportKey = transportKey,
                            Sequence = Math.Max(0, request.Sequence),
                            DeviceTimestampMilliseconds = Math.Max(0, request.DeviceTimestampMilliseconds),
                            Readings = (request.Readings ?? []).Where(reading => reading is not null).Take(128).ToList(),
                            TargetPeerId = string.IsNullOrWhiteSpace(request.TargetPeerId) ? "localgpt" : NormalizeToken(request.TargetPeerId, 120),
                            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson
                        }),
                        ["TransportKey"] = JsonSerializer.SerializeToElement(transportKey),
                        ["BoardProfileKey"] = JsonSerializer.SerializeToElement(NormalizeToken(request.BoardProfileKey, 160))
                    }
                };
            }

            return new EmbeddedTelemetryBridgeResult
            {
                Succeeded = !status.Equals("Danger", StringComparison.OrdinalIgnoreCase),
                Status = status,
                EdgeEnvelopeJson = edgeJson,
                OneWireEnvelope = envelope,
                Findings = findings
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(BuildAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(BuildAsync)} failed.");
        throw;
    }
}

    private JsonElement ParseMetadata(string? value)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(value))
                return JsonSerializer.SerializeToElement(new { });
            try
            {
                using var document = JsonDocument.Parse(value);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return JsonSerializer.SerializeToElement(new { });
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(ParseMetadata)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(ParseMetadata)} failed.");
        throw;
    }
}

    private string NormalizeToken(string? value, int maximum)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var normalized = new string(value.Trim().Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':' or '/' ? ch : '-').ToArray());
            return normalized.Length <= maximum ? normalized : normalized[..maximum];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(NormalizeToken)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(NormalizeToken)} failed.");
        throw;
    }
}

    private string SafeDeviceForLog(string? value) {
    try
    {
        return string.IsNullOrWhiteSpace(value) ? "(missing)" : NormalizeToken(value, 80);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(SafeDeviceForLog)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(SafeDeviceForLog)} failed.");
        throw;
    }
}
    private string SeverityStatus(IEnumerable<EmbeddedPlanFinding> findings) {
    try
    {
        return findings.Any(item => string.Equals(item.Severity, "Danger", StringComparison.OrdinalIgnoreCase)) ? "Danger" : findings.Any(item => string.Equals(item.Severity, "Warning", StringComparison.OrdinalIgnoreCase)) ? "Warning" : "Approved";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(SeverityStatus)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryBridgeService)}.{nameof(SeverityStatus)} failed.");
        throw;
    }
}
}
