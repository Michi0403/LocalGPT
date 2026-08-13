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
    /// Gets or sets the session value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The session value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public required CouncilGameSessionSnapshot Session { get; init; }
    /// <summary>
    /// Gets or sets the proposal value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public required CouncilGameControlRequest Proposal { get; init; }
    /// <summary>
    /// Gets or sets the normalized action value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The normalized action value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public required string NormalizedAction { get; init; }
    /// <summary>
    /// Gets or sets the director mode value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; init; }
    /// <summary>
    /// Gets or sets the director model name value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director model name value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public string DirectorModelName { get; init; } = string.Empty;
}


/// <summary>Describes one factory-created actor instance and its Council assignment slot.</summary>
public sealed class CouncilGameActorRuntimeDescriptor
{
    /// <summary>
    /// Gets or sets the stable instance key used to identify or correlate this council game actor runtime instance with related application state.
    /// </summary>
    /// <value>The instance key value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string InstanceKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor kind value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public CouncilGameActorKind ActorKind { get; set; }
    /// <summary>
    /// Gets or sets the stable runtime class key used to identify or correlate this council game actor runtime instance with related application state.
    /// </summary>
    /// <value>The runtime class key value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the archetype value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The archetype value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string Archetype { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council role value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council role value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string CouncilRole { get; set; } = "World Actor";
    /// <summary>
    /// Gets or sets the council assignment group value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council assignment group value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string CouncilAssignmentGroup { get; set; } = "ascii-doom-actors";
    /// <summary>
    /// Gets or sets the council assignment slot value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council assignment slot value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string CouncilAssignmentSlot { get; set; } = string.Empty;
}

/// <summary>Represents one bounded prediction from a creature or reactive-object subdirector.</summary>
public sealed class CouncilGameSubdirectorPrediction
{
    /// <summary>
    /// Gets or sets the stable director key used to identify or correlate this council game subdirector prediction instance with related application state.
    /// </summary>
    /// <value>The director key value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public string DirectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor kind value that forms part of the council game subdirector prediction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public CouncilGameActorKind ActorKind { get; set; }
    /// <summary>
    /// Gets or sets the stable runtime class key used to identify or correlate this council game subdirector prediction instance with related application state.
    /// </summary>
    /// <value>The runtime class key value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prediction value that forms part of the council game subdirector prediction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prediction value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public string Prediction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the confidence percent value that forms part of the council game subdirector prediction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confidence percent value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public int ConfidencePercent { get; set; }
    /// <summary>
    /// Gets or sets the actor instances collection maintained or exposed by this council game subdirector prediction instance for downstream processing.
    /// </summary>
    /// <value>The actor instances value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public IReadOnlyList<CouncilGameActorRuntimeDescriptor> ActorInstances { get; set; } = [];
}

/// <summary>Records the GameDirector decision that authorizes or rejects one state transition.</summary>
public sealed class CouncilGameDirectorDecision
{
    /// <summary>
    /// Gets or sets the stable decision identifier used to identify or correlate this council game director decision instance with related application state.
    /// </summary>
    /// <value>The decision identifier value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public Guid DecisionId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this council game director decision instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the expected turn value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected turn value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public long ExpectedTurn { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the council game director decision state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the normalized action value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The normalized action value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string NormalizedAction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the director name value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director name value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string DirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets the director mode value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; }
    /// <summary>
    /// Gets or sets the director model name value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director model name value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string DirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reason value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the predictions collection maintained or exposed by this council game director decision instance for downstream processing.
    /// </summary>
    /// <value>The predictions value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public IReadOnlyList<CouncilGameSubdirectorPrediction> Predictions { get; set; } = [];
    /// <summary>
    /// Gets or sets the decided at UTC associated with this council game director decision state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The decided at UTC value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
}
