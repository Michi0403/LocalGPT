using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IEmbeddedHardwareCatalogService
{
    Task<EmbeddedBoardCatalog> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmbeddedBoardProfile>> GetBoardProfilesAsync(CancellationToken cancellationToken = default);
    Task<EmbeddedBoardProfile?> GetBoardProfileAsync(string boardProfileKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmbeddedProtocolDescriptor>> GetProtocolDescriptorsAsync(CancellationToken cancellationToken = default);
    EmbeddedPublisherWorkbenchContract GetPublisherWorkbenchContract();
}

public interface IEmbeddedWiringService
{
    Task<EmbeddedWiringDraft> CreateDraftAsync(string boardProfileKey, string name, CancellationToken cancellationToken = default);
    Task<EmbeddedWiringValidationResult> ValidateAsync(EmbeddedWiringValidationRequest request, CancellationToken cancellationToken = default);
}

public interface IEmbeddedFirmwarePlanningService
{
    Task<EmbeddedFirmwarePlan> CreatePlanAsync(EmbeddedFirmwarePlanRequest request, CancellationToken cancellationToken = default);
    Task<EmbeddedFirmwareArtifactResult> CreateArtifactsAsync(EmbeddedFirmwarePlanRequest request, bool userConfirmed, CancellationToken cancellationToken = default);
}

public interface IEmbeddedTelemetryBridgeService
{
    Task<EmbeddedTelemetryBridgeResult> PreviewAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
    Task<EmbeddedTelemetryBridgeResult> CreateOneWireEnvelopeAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
}

public interface IEmbeddedTelemetryIngressService
{
    Task<EmbeddedTelemetryIngressResult> PublishAsync(EmbeddedTelemetryBridgeRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<EmbeddedTelemetrySnapshot> GetRecent(string? deviceId = null, int maximum = 100);
}
