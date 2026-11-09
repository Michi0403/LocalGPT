using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Chooses how a game action is reviewed before the authoritative session state changes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameDirectorMode
{
    /// <summary>Uses the deterministic LocalGPT director and bounded subdirector rules.</summary>
    Deterministic,

    /// <summary>Marks the decision for a configured Council model review while retaining deterministic final authority.</summary>
    CouncilModelPreferred
}
