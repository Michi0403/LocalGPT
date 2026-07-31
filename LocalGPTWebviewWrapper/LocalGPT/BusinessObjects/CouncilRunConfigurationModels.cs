namespace LocalGPT.BusinessObjects;

public sealed record CouncilRunConfigurationSnapshot(
    Guid RunId,
    long Revision,
    IReadOnlyList<string> Participants,
    IReadOnlyList<OneWireCouncilModelRoute> ModelRoutes,
    int ResourceLoadPercent,
    int RequestedMaxOutputTokens,
    int RequestedMaxContextTokens,
    int? FallbackOllamaNumGpu,
    bool AllowParallelHardwareRoads,
    bool IsRunning);
