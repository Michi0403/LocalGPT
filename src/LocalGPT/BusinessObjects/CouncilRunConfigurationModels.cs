namespace LocalGPT.BusinessObjects;

/// <summary>
/// Carries the configurable council preparation settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
/// <param name="ModelNames">String dependency used by the council preparation workflow to provide the corresponding application capability.</param>
/// <param name="ModelRoutes">One wire council model route dependency used by the council preparation workflow to provide the corresponding application capability.</param>
/// <param name="ResourceLoadPercent">Resource load percent value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="MaxOutputTokens">Max output tokens value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="MaxContextTokens">Max context tokens value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="OllamaNumGpu">Ollama num gpu value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="AllowParallelHardwareRoads">Value indicating whether parallel hardware roads should apply to this operation.</param>
/// <param name="MaxParallelModels">Max parallel models value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="ModelTimeoutSeconds">Model timeout seconds value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="CritiqueRounds">Critique rounds value supplied to the council preparation operation and used when producing its result.</param>
/// <param name="IncludeMemory">Value indicating whether memory should apply to this operation.</param>
/// <param name="CreateProjectPerRun">Value indicating whether create project per run should apply to this operation.</param>
/// <param name="CouncilTeamKey">Council team key value supplied to the council preparation operation and used when producing its result.</param>
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
/// Represents a council run configuration snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RunId">Identifier of the run to use for this operation.</param>
/// <param name="Revision">Revision value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="Participants">String dependency used by the council run configuration snapshot workflow to provide the corresponding application capability.</param>
/// <param name="ModelRoutes">One wire council model route dependency used by the council run configuration snapshot workflow to provide the corresponding application capability.</param>
/// <param name="ResourceLoadPercent">Resource load percent value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="RequestedMaxOutputTokens">Requested max output tokens value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="RequestedMaxContextTokens">Requested max context tokens value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="FallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="AllowParallelHardwareRoads">Value indicating whether parallel hardware roads should apply to this operation.</param>
/// <param name="MaxParallelModels">Max parallel models value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="ModelTimeoutSeconds">Model timeout seconds value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="CurrentRound">Current round value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="CurrentPhase">Current phase value supplied to the council run configuration snapshot operation and used when producing its result.</param>
/// <param name="IsRoundSkipRequested">Value indicating whether round skip requested should apply to this operation.</param>
/// <param name="IsRunning">Value indicating whether running should apply to this operation.</param>
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
