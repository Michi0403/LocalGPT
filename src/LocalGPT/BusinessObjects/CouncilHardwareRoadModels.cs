namespace LocalGPT.BusinessObjects;

/// <summary>Immutable execution limits selected for one model on one CPU/GPU road.</summary>
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
