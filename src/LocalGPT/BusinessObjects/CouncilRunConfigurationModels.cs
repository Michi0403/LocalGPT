namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council preparation configuration.
/// </summary>
public sealed record CouncilPreparationConfiguration(
    IReadOnlyList<string> ModelNames,
    IReadOnlyList<OneWireCouncilModelRoute> ModelRoutes,
    int ResourceLoadPercent,
    int MaxOutputTokens,
    int MaxContextTokens,
    int? OllamaNumGpu,
    bool AllowParallelHardwareRoads,
    int MaxParallelModels,
    int ModelTimeoutSeconds,
    int CritiqueRounds,
    bool IncludeMemory,
    bool CreateProjectPerRun,
    string CouncilTeamKey);

/// <summary>
/// Represents a council run configuration snapshot.
/// </summary>
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
    int MaxParallelModels,
    int ModelTimeoutSeconds,
    int CurrentRound,
    string CurrentPhase,
    bool IsRoundSkipRequested,
    bool IsRunning);
