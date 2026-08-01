namespace LocalGPT.BusinessObjects;

public sealed record CouncilPreparationConfiguration(
    IReadOnlyList<string> ModelNames,
    IReadOnlyList<OneWireCouncilModelRoute> ModelRoutes,
    int ResourceLoadPercent,
    int MaxOutputTokens,
    int MaxContextTokens,
    int? OllamaNumGpu,
    bool AllowParallelHardwareRoads,
    int MaxParallelModels,
    int CritiqueRounds,
    bool IncludeMemory,
    bool CreateProjectPerRun,
    string CouncilTeamKey);

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
    int CurrentRound,
    string CurrentPhase,
    bool IsRoundSkipRequested,
    bool IsRunning);
