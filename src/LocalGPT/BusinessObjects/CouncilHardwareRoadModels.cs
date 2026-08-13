namespace LocalGPT.BusinessObjects;

/// <summary>Immutable execution limits selected for one model on one CPU/GPU road.</summary>
/// <param name="ModelName">Model name value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="HardwareKind">Hardware kind value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="HardwareIndex">Hardware index value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="HardwareName">Hardware name value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="LaneKey">Lane key value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="EffectiveLoadPercent">Effective load percent value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="EffectiveMaxOutputTokens">Effective max output tokens value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="EffectiveMaxContextTokens">Effective max context tokens value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="OllamaNumGpu">Ollama num gpu value supplied to the council hardware road plan operation and used when producing its result.</param>
/// <param name="MaxConcurrentModelsOnLane">Max concurrent models on lane value supplied to the council hardware road plan operation and used when producing its result.</param>
public sealed record CouncilHardwareRoadPlan(
    string ModelName,
    OneWireHardwareKind HardwareKind,
    int HardwareIndex,
    string HardwareName,
    string LaneKey,
    int EffectiveLoadPercent,
    int EffectiveMaxOutputTokens,
    int EffectiveMaxContextTokens,
    int? OllamaNumGpu,
    int MaxConcurrentModelsOnLane);
