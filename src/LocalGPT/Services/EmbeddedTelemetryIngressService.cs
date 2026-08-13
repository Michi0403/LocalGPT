using System.Collections.Concurrent;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates embedded telemetry ingress behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="bridge">Embedded telemetry bridge service dependency used by the embedded telemetry ingress workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class EmbeddedTelemetryIngressService(
    IEmbeddedTelemetryBridgeService bridge,
    ILogger<EmbeddedTelemetryIngressService> logger) : IEmbeddedTelemetryIngressService
{
    /// <summary>
    /// Defines the maximum snapshots constant used by <see cref="EmbeddedTelemetryIngressService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumSnapshots = 500;
    /// <summary>
    /// Stores the internal snapshots state used by <see cref="EmbeddedTelemetryIngressService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ConcurrentQueue<EmbeddedTelemetrySnapshot> snapshots = new();

    /// <summary>
    /// Performs publish as part of the embedded telemetry ingress service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded telemetry ingress result produced by the operation.</returns>
    public async Task<EmbeddedTelemetryIngressResult> PublishAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            var validation = await bridge.PreviewAsync(request, cancellationToken).ConfigureAwait(false);
            var accepted = validation.Succeeded;
            var deviceId = request.DeviceId?.Trim() ?? string.Empty;
            if (accepted && request.Sequence <= 0)
            {
                validation.Findings.Add(new EmbeddedPlanFinding("Warning", "SEQUENCE_UNTRACKED", "A positive sequence was not supplied. The untrusted edge ingress stores the sample for diagnostics, while replay protection remains the responsibility of an authenticated gateway or protected LocalGPT 1-Wire peer."));
                validation.Status = "Warning";
            }

            var receivedAt = DateTime.UtcNow;
            if (accepted)
            {
                snapshots.Enqueue(new EmbeddedTelemetrySnapshot
                {
                    DeviceId = deviceId,
                    Sequence = Math.Max(0, request.Sequence),
                    ReceivedAtUtc = receivedAt,
                    BoardProfileKey = request.BoardProfileKey?.Trim() ?? string.Empty,
                    TransportKey = request.TransportKey?.Trim() ?? string.Empty,
                    Readings = (request.Readings ?? []).Where(item => item is not null).Select(CloneReading).ToList()
                });
                while (snapshots.Count > MaximumSnapshots && snapshots.TryDequeue(out _)) { }
            }

            logger.LogInformation(
                "Embedded telemetry batch for device {DeviceId} was {Status}; {ReadingCount} reading(s), values omitted from logs.",
                string.IsNullOrWhiteSpace(request.DeviceId) ? "(missing)" : request.DeviceId.Trim(),
                accepted ? "accepted" : "rejected",
                request.Readings?.Count ?? 0);
            return new EmbeddedTelemetryIngressResult
            {
                Accepted = accepted,
                Status = accepted ? validation.Status : "Rejected",
                DeviceId = deviceId,
                Sequence = Math.Max(0, request.Sequence),
                ReceivedAtUtc = receivedAt,
                ReadingCount = request.Readings?.Count ?? 0,
                Findings = validation.Findings
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(PublishAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(PublishAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves recent as part of the embedded telemetry ingress service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="deviceId">Identifier of the device to use for this operation.</param>
    /// <param name="maximum">Maximum value supplied to the embedded telemetry ingress operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<EmbeddedTelemetrySnapshot> GetRecent(string? deviceId = null, int maximum = 100)
    {
    try
    {
            var bounded = Math.Clamp(maximum, 1, 500);
            return snapshots.Reverse()
                .Where(item => string.IsNullOrWhiteSpace(deviceId) || string.Equals(item.DeviceId, deviceId.Trim(), StringComparison.OrdinalIgnoreCase))
                .Take(bounded)
                .Select(CloneSnapshot)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(GetRecent)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(GetRecent)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs clone reading as part of the embedded telemetry ingress service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the embedded telemetry ingress operation and used when producing its result.</param>
    /// <returns>The embedded telemetry reading produced by the operation.</returns>
    private EmbeddedTelemetryReading CloneReading(EmbeddedTelemetryReading source) {
    try
    {
        return new()
    {
        SensorKey = source.SensorKey,
        PinKey = source.PinKey,
        Gpio = source.Gpio,
        Metric = source.Metric,
        Value = source.Value,
        Unit = source.Unit,
        Quality = source.Quality
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(CloneReading)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(CloneReading)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone snapshot as part of the embedded telemetry ingress service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the embedded telemetry ingress operation and used when producing its result.</param>
    /// <returns>The embedded telemetry snapshot produced by the operation.</returns>
    private EmbeddedTelemetrySnapshot CloneSnapshot(EmbeddedTelemetrySnapshot source) {
    try
    {
        return new()
    {
        DeviceId = source.DeviceId,
        Sequence = source.Sequence,
        ReceivedAtUtc = source.ReceivedAtUtc,
        BoardProfileKey = source.BoardProfileKey,
        TransportKey = source.TransportKey,
        Readings = source.Readings.Select(CloneReading).ToList()
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(CloneSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedTelemetryIngressService)}.{nameof(CloneSnapshot)} failed.");
        throw;
    }
}
}
