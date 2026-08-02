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

/// <summary>Identifies the runtime actor that proposed an action.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameActorKind
{
    Player,
    Creature,
    ReactiveObject,
    Director
}

/// <summary>Provides the immutable input used by the authoritative GameDirector for one proposed action.</summary>
public sealed class CouncilGameDirectorContext
{
    public required CouncilGameSessionSnapshot Session { get; init; }
    public required CouncilGameControlRequest Proposal { get; init; }
    public required string NormalizedAction { get; init; }
    public CouncilGameDirectorMode DirectorMode { get; init; }
    public string DirectorModelName { get; init; } = string.Empty;
}


/// <summary>Describes one factory-created actor instance and its Council assignment slot.</summary>
public sealed class CouncilGameActorRuntimeDescriptor
{
    public string InstanceKey { get; set; } = string.Empty;
    public CouncilGameActorKind ActorKind { get; set; }
    public string RuntimeClassKey { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string CouncilRole { get; set; } = "World Actor";
    public string CouncilAssignmentGroup { get; set; } = "ascii-doom-actors";
    public string CouncilAssignmentSlot { get; set; } = string.Empty;
}

/// <summary>Represents one bounded prediction from a creature or reactive-object subdirector.</summary>
public sealed class CouncilGameSubdirectorPrediction
{
    public string DirectorKey { get; set; } = string.Empty;
    public CouncilGameActorKind ActorKind { get; set; }
    public string RuntimeClassKey { get; set; } = string.Empty;
    public string Prediction { get; set; } = string.Empty;
    public int ConfidencePercent { get; set; }
    public IReadOnlyList<CouncilGameActorRuntimeDescriptor> ActorInstances { get; set; } = [];
}

/// <summary>Records the GameDirector decision that authorizes or rejects one state transition.</summary>
public sealed class CouncilGameDirectorDecision
{
    public Guid DecisionId { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public long ExpectedTurn { get; set; }
    public bool Approved { get; set; }
    public string NormalizedAction { get; set; } = string.Empty;
    public string DirectorName { get; set; } = "LocalGPT GameDirector";
    public CouncilGameDirectorMode DirectorMode { get; set; }
    public string DirectorModelName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<CouncilGameSubdirectorPrediction> Predictions { get; set; } = [];
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
}
