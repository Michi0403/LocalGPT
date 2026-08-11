using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the embedded hardware catalog service contract.
/// </summary>
public interface IEmbeddedHardwareCatalogService
{
    /// <summary>
    /// Gets catalog async.
    /// </summary>
    Task<EmbeddedBoardCatalog> GetCatalogAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets board profiles async.
    /// </summary>
    Task<IReadOnlyList<EmbeddedBoardProfile>> GetBoardProfilesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets board profile async.
    /// </summary>
    Task<EmbeddedBoardProfile?> GetBoardProfileAsync(string boardProfileKey, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets protocol descriptors async.
    /// </summary>
    Task<IReadOnlyList<EmbeddedProtocolDescriptor>> GetProtocolDescriptorsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets publisher workbench contract.
    /// </summary>
    EmbeddedPublisherWorkbenchContract GetPublisherWorkbenchContract();
}

/// <summary>
/// Defines the embedded wiring service contract.
/// </summary>
public interface IEmbeddedWiringService
{
    /// <summary>
    /// Creates draft async.
    /// </summary>
    Task<EmbeddedWiringDraft> CreateDraftAsync(string boardProfileKey, string name, CancellationToken cancellationToken = default);
    /// <summary>
    /// Validates async.
    /// </summary>
    Task<EmbeddedWiringValidationResult> ValidateAsync(EmbeddedWiringValidationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the embedded firmware planning service contract.
/// </summary>
public interface IEmbeddedFirmwarePlanningService
{
    /// <summary>
    /// Creates plan async.
    /// </summary>
    Task<EmbeddedFirmwarePlan> CreatePlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates artifacts async.
    /// </summary>
    Task<EmbeddedFirmwareArtifactResult> CreateArtifactsAsync(EmbeddedFirmwarePlanRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the embedded telemetry bridge service contract.
/// </summary>
public interface IEmbeddedTelemetryBridgeService
{
    /// <summary>
    /// Runs the preview async operation.
    /// </summary>
    Task<EmbeddedTelemetryBridgeResult> PreviewAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates one wire envelope async.
    /// </summary>
    Task<EmbeddedTelemetryBridgeResult> CreateOneWireEnvelopeAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the embedded telemetry ingress service contract.
/// </summary>
public interface IEmbeddedTelemetryIngressService
{
    /// <summary>
    /// Publishes async.
    /// </summary>
    Task<EmbeddedTelemetryIngressResult> PublishAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets recent.
    /// </summary>
    IReadOnlyList<EmbeddedTelemetrySnapshot> GetRecent(string? deviceId = null, int maximum = 100);
}
