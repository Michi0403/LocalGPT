using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates embedded firmware planning behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class EmbeddedFirmwarePlanningService : IEmbeddedFirmwarePlanningService
{
    /// <summary>
    /// Stores the embedded hardware catalog service dependency used by <see cref="EmbeddedFirmwarePlanningService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IEmbeddedHardwareCatalogService catalog;
    /// <summary>
    /// Stores the embedded wiring service dependency used by <see cref="EmbeddedFirmwarePlanningService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IEmbeddedWiringService wiring;
    /// <summary>
    /// Stores the embedded telemetry bridge service dependency used by <see cref="EmbeddedFirmwarePlanningService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IEmbeddedTelemetryBridgeService telemetryBridge;
    /// <summary>
    /// Stores the logger used by <see cref="EmbeddedFirmwarePlanningService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<EmbeddedFirmwarePlanningService> logger;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="catalog">Injected dependency used by the EmbeddedFirmwarePlanningService.</param>
    /// <param name="wiring">Injected dependency used by the EmbeddedFirmwarePlanningService.</param>
    /// <param name="telemetryBridge">Injected dependency used by the EmbeddedFirmwarePlanningService.</param>
    /// <param name="logger">Injected dependency used by the EmbeddedFirmwarePlanningService.</param>
    public EmbeddedFirmwarePlanningService(
        IEmbeddedHardwareCatalogService catalog,
        IEmbeddedWiringService wiring,
        IEmbeddedTelemetryBridgeService telemetryBridge,
        ILogger<EmbeddedFirmwarePlanningService> logger)
    {
        this.catalog = catalog;
        this.wiring = wiring;
        this.telemetryBridge = telemetryBridge;
        this.logger = logger;
    }

    /// <summary>
    /// Stores the internal artifact JSON options state used by <see cref="EmbeddedFirmwarePlanningService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions artifactJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Creates plan as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded firmware plan produced by the operation.</returns>
    public async Task<EmbeddedFirmwarePlan> CreatePlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken = default)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(CreatePlanAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(CreatePlanAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates artifacts as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded firmware artifact result produced by the operation.</returns>
    public async Task<EmbeddedFirmwareArtifactResult> CreateArtifactsAsync(EmbeddedFirmwarePlanRequest request, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(CreateArtifactsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EmbeddedFirmwarePlanningService)}.{nameof(CreateArtifactsAsync)} failed.");
        throw;
    }
}
}
