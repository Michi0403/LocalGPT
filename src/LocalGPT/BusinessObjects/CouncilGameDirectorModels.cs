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
    /// <summary>
    /// Gets or sets session.
    /// </summary>
    public required CouncilGameSessionSnapshot Session { get; init; }
    /// <summary>
    /// Gets or sets proposal.
    /// </summary>
    public required CouncilGameControlRequest Proposal { get; init; }
    /// <summary>
    /// Gets or sets normalized action.
    /// </summary>
    public required string NormalizedAction { get; init; }
    /// <summary>
    /// Gets or sets director mode.
    /// </summary>
    public CouncilGameDirectorMode DirectorMode { get; init; }
    /// <summary>
    /// Gets or sets director model name.
    /// </summary>
    public string DirectorModelName { get; init; } = string.Empty;
}


/// <summary>Describes one factory-created actor instance and its Council assignment slot.</summary>
public sealed class CouncilGameActorRuntimeDescriptor
{
    /// <summary>
    /// Gets or sets instance key.
    /// </summary>
    public string InstanceKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor kind.
    /// </summary>
    public CouncilGameActorKind ActorKind { get; set; }
    /// <summary>
    /// Gets or sets runtime class key.
    /// </summary>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets archetype.
    /// </summary>
    public string Archetype { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council role.
    /// </summary>
    public string CouncilRole { get; set; } = "World Actor";
    /// <summary>
    /// Gets or sets council assignment group.
    /// </summary>
    public string CouncilAssignmentGroup { get; set; } = "ascii-doom-actors";
    /// <summary>
    /// Gets or sets council assignment slot.
    /// </summary>
    public string CouncilAssignmentSlot { get; set; } = string.Empty;
}

/// <summary>Represents one bounded prediction from a creature or reactive-object subdirector.</summary>
public sealed class CouncilGameSubdirectorPrediction
{
    /// <summary>
    /// Gets or sets director key.
    /// </summary>
    public string DirectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor kind.
    /// </summary>
    public CouncilGameActorKind ActorKind { get; set; }
    /// <summary>
    /// Gets or sets runtime class key.
    /// </summary>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets prediction.
    /// </summary>
    public string Prediction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets confidence percent.
    /// </summary>
    public int ConfidencePercent { get; set; }
    /// <summary>
    /// Gets or sets actor instances.
    /// </summary>
    public IReadOnlyList<CouncilGameActorRuntimeDescriptor> ActorInstances { get; set; } = [];
}

/// <summary>Records the GameDirector decision that authorizes or rejects one state transition.</summary>
public sealed class CouncilGameDirectorDecision
{
    /// <summary>
    /// Gets or sets decision identifier.
    /// </summary>
    public Guid DecisionId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets expected turn.
    /// </summary>
    public long ExpectedTurn { get; set; }
    /// <summary>
    /// Gets or sets approved.
    /// </summary>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets normalized action.
    /// </summary>
    public string NormalizedAction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets director name.
    /// </summary>
    public string DirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets director mode.
    /// </summary>
    public CouncilGameDirectorMode DirectorMode { get; set; }
    /// <summary>
    /// Gets or sets director model name.
    /// </summary>
    public string DirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets predictions.
    /// </summary>
    public IReadOnlyList<CouncilGameSubdirectorPrediction> Predictions { get; set; } = [];
    /// <summary>
    /// Gets or sets decided at UTC.
    /// </summary>
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
}
