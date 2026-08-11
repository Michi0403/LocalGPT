using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Council;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LocalGPT.Services;

/// <summary>
/// Gives AI chat and Council members an explicit clock and a compact, current runtime-state snapshot.
/// The function is read-only and intentionally returns only the three newest bounded log/council rows.
/// </summary>
public sealed class GetTimeAndStateNowFunction(
    IApplicationLogReaderService applicationLogs,
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    ICouncilSpoolerService councilSpooler,
    IHardwareInventoryService hardwareInventory,
    ILogger<GetTimeAndStateNowFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets or sets descriptor.
    /// </summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.time_state.now",
        "POST",
        "/api/dxai/functions/localgpt.time_state.now/invoke",
        "Returns LocalGPT's current UTC/local time, process state, the three newest operational logs, the three newest Council spool entries, hardware inventory and linked 1-Wire peers.",
        "No parameters are required.",
        "Read-only, bounded and safe for automatic Council preflight use. Log messages and exceptions are truncated; no chat prompts, generated source, secrets or whole databases are returned.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "TimeAndStateDxAiFunction",
        ParameterSchemaJson: "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");

    /// <summary>
    /// Runs the invoke async operation.
    /// </summary>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var utcNow = DateTimeOffset.UtcNow;
            var localNow = utcNow.ToLocalTime();
            using var process = Process.GetCurrentProcess();
            var processStartUtc = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
            var recentLogs = await applicationLogs.GetRecentAsync(LogLevel.Trace, 3, cancellationToken).ConfigureAwait(false);
            var hardware = await hardwareInventory.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            var peerRows = peers.GetPeers().Take(32).Select(peer => new
            {
                peer.PeerId,
                peer.DisplayName,
                peer.Application,
                peer.ApplicationVersion,
                peer.IsConnected,
                TransportConnected = connections.IsConnected(peer.PeerId),
                peer.SeenUtc,
                OnlineCapabilities = peer.Capabilities.Count(capability => capability.IsEnabled && capability.IsOnline),
                OnlineSkills = peer.Skills.Count(skill => skill.IsEnabled && skill.IsOnline)
            }).ToList();
            var councilRows = councilSpooler.GetSnapshots(includeCompleted: true, take: 3).Select(item => new
            {
                item.RunId,
                item.Status,
                TeamKey = item.CouncilTeamKey,
                PromptPreview = Limit(item.Prompt, 600),
                item.CurrentRound,
                CurrentPhase = item.Phase,
                CreatedUtc = item.StartedAtUtc,
                item.UpdatedAtUtc,
                item.CompletedAtUtc,
                StepCount = item.Steps.Count,
                Warnings = item.Warnings.Take(8).Select(warning => Limit(warning, 400)).ToList()
            }).ToList();

            var value = new
            {
                UtcNow = utcNow,
                LocalNow = localNow,
                TimeZone = TimeZoneInfo.Local.Id,
                Environment.TickCount64,
                Process = new
                {
                    process.Id,
                    process.ProcessName,
                    StartTimeUtc = processStartUtc,
                    Uptime = utcNow - processStartUtc,
                    WorkingSetBytes = process.WorkingSet64,
                    ManagedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false),
                    Environment.MachineName,
                    Environment.ProcessorCount,
                    Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription
                },
                RecentLogs = recentLogs.Select(entry => new
                {
                    entry.Id,
                    entry.TimestampUtc,
                    entry.Level,
                    entry.Category,
                    entry.EventId,
                    entry.EventName,
                    Message = Limit(entry.Message, 1600),
                    Exception = Limit(entry.Exception ?? string.Empty, 2000)
                }).ToList(),
                RecentCouncilRuns = councilRows,
                OneWirePeers = peerRows,
                HardwareInventory = hardware.Take(32).Select(item => new
                {
                    Kind = item.Kind.ToString(),
                    item.Index,
                    item.Name,
                    item.Vendor,
                    item.DedicatedMemoryBytes,
                    item.LogicalProcessorCount,
                    item.IsOnline,
                    item.LaneKey
                }).ToList()
            };

            logger.LogInformation(
                "DXAIFunction returned current time/state with {LogCount} log(s), {CouncilCount} council run(s), {PeerCount} peer(s) and {HardwareCount} hardware row(s).",
                recentLogs.Count, councilRows.Count, peerRows.Count, hardware.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = value };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetTimeAndStateNowFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetTimeAndStateNowFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the limit operation.
    /// </summary>
    private string Limit(string value, int maximum) {
    try
    {
        return value.Length <= maximum ? value : value[..maximum] + "…";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetTimeAndStateNowFunction)}.{nameof(Limit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetTimeAndStateNowFunction)}.{nameof(Limit)} failed.");
        throw;
    }
}
}
