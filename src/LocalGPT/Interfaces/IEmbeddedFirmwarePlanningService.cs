using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for embedded hardware catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IEmbeddedHardwareCatalogService
{
    /// <summary>
    /// Retrieves catalog as part of the embedded hardware catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded board catalog produced by the operation.</returns>
    Task<EmbeddedBoardCatalog> GetCatalogAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves board profiles as part of the embedded hardware catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<EmbeddedBoardProfile>> GetBoardProfilesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves board profile as part of the embedded hardware catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="boardProfileKey">Board profile key value supplied to the embedded hardware catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded board profile produced by the operation.</returns>
    Task<EmbeddedBoardProfile?> GetBoardProfileAsync(string boardProfileKey, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves protocol descriptors as part of the embedded hardware catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<EmbeddedProtocolDescriptor>> GetProtocolDescriptorsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves publisher workbench contract as part of the embedded hardware catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The embedded publisher workbench contract produced by the operation.</returns>
    EmbeddedPublisherWorkbenchContract GetPublisherWorkbenchContract();
}

/// <summary>
/// Defines the contract for embedded wiring behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IEmbeddedWiringService
{
    /// <summary>
    /// Creates draft as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="boardProfileKey">Board profile key value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the embedded wiring operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded wiring draft produced by the operation.</returns>
    Task<EmbeddedWiringDraft> CreateDraftAsync(string boardProfileKey, string name, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs validate as part of the embedded wiring service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded wiring validation result produced by the operation.</returns>
    Task<EmbeddedWiringValidationResult> ValidateAsync(EmbeddedWiringValidationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for embedded firmware planning behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IEmbeddedFirmwarePlanningService
{
    /// <summary>
    /// Creates plan as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded firmware plan produced by the operation.</returns>
    Task<EmbeddedFirmwarePlan> CreatePlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates artifacts as part of the embedded firmware planning service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded firmware artifact result produced by the operation.</returns>
    Task<EmbeddedFirmwareArtifactResult> CreateArtifactsAsync(EmbeddedFirmwarePlanRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for embedded telemetry bridge behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IEmbeddedTelemetryBridgeService
{
    /// <summary>
    /// Performs preview as part of the embedded telemetry bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded telemetry bridge result produced by the operation.</returns>
    Task<EmbeddedTelemetryBridgeResult> PreviewAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates one wire envelope as part of the embedded telemetry bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded telemetry bridge result produced by the operation.</returns>
    Task<EmbeddedTelemetryBridgeResult> CreateOneWireEnvelopeAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for embedded telemetry ingress behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IEmbeddedTelemetryIngressService
{
    /// <summary>
    /// Performs publish as part of the embedded telemetry ingress service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The embedded telemetry ingress result produced by the operation.</returns>
    Task<EmbeddedTelemetryIngressResult> PublishAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves recent as part of the embedded telemetry ingress service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="deviceId">Identifier of the device to use for this operation.</param>
    /// <param name="maximum">Maximum value supplied to the embedded telemetry ingress operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<EmbeddedTelemetrySnapshot> GetRecent(string? deviceId = null, int maximum = 100);
}
