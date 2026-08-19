using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported council game control mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameControlMode
{
    /// <summary>
    /// Selects the human option for <see cref="CouncilGameControlMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Human,
    /// <summary>
    /// Selects the AI option for <see cref="CouncilGameControlMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Ai,
    /// <summary>
    /// Selects the shared option for <see cref="CouncilGameControlMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Shared
}

/// <summary>
/// Represents the input contract for start council game, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class StartCouncilGameRequest
{
    /// <summary>
    /// Gets or sets the stable game key used to identify or correlate this start council game instance with related application state.
    /// </summary>
    /// <value>The game key value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public string GameKey { get; set; } = "ascii-doom";
    /// <summary>
    /// Gets or sets the stable team key used to identify or correlate this start council game instance with related application state.
    /// </summary>
    /// <value>The team key value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public string TeamKey { get; set; } = "ascii-doom-council-adventure";
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this start council game instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the control mode value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control mode value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Human;
    /// <summary>
    /// Gets or sets a value indicating whether autoplay enabled applies to the start council game state.
    /// </summary>
    /// <value>The autoplay enabled value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets the autoplay delay milliseconds value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The autoplay delay milliseconds value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>
    /// Gets or sets the director mode value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>
    /// Gets or sets the game director model name value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The game director model name value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public string GameDirectorModelName { get; set; } = "qwen3.5:0.8b";
    /// <summary>
    /// Gets or sets the creature director count that quantifies the associated start council game data.
    /// </summary>
    /// <value>The creature director count value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets the started by value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The started by value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public string StartedBy { get; set; } = "Human User";
}

/// <summary>
/// Represents the input contract for council game control, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class CouncilGameControlRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this council game control instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the action value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the axis x value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The axis x value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public double? AxisX { get; set; }
    /// <summary>
    /// Gets or sets the axis y value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The axis y value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public double? AxisY { get; set; }
    /// <summary>
    /// Gets or sets the aim x value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The aim x value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public int? AimX { get; set; }
    /// <summary>
    /// Gets or sets the aim y value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The aim y value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public int? AimY { get; set; }
    /// <summary>
    /// Gets or sets the source value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string Source { get; set; } = "Human";
    /// <summary>
    /// Gets or sets the actor name value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor name value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string ActorName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets the actor kind value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public CouncilGameActorKind ActorKind { get; set; } = CouncilGameActorKind.Player;
    /// <summary>
    /// Gets or sets the stable runtime class key used to identify or correlate this council game control instance with related application state.
    /// </summary>
    /// <value>The runtime class key value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string RuntimeClassKey { get; set; } = "games.ascii.doom.player";
    /// <summary>
    /// Gets or sets the expected turn value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected turn value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public long? ExpectedTurn { get; set; }
}

/// <summary>
/// Represents the input contract for submit council game frame, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SubmitCouncilGameFrameRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this submit council game frame instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets the renderer name value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The renderer name value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame text value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame text value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the caption value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The caption value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public string Caption { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for set council game control mode, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SetCouncilGameControlModeRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this set council game control mode instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the control mode value that forms part of the set council game control mode state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control mode value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Human;
    /// <summary>
    /// Gets or sets a value indicating whether autoplay enabled applies to the set council game control mode state.
    /// </summary>
    /// <value>The autoplay enabled value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets the autoplay delay milliseconds value that forms part of the set council game control mode state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The autoplay delay milliseconds value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
}

/// <summary>
/// Represents the input contract for set council game input gate, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SetCouncilGameInputGateRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this set council game input gate instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether human input required applies to the set council game input gate state.
    /// </summary>
    /// <value>The human input required value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets the legal actions collection maintained or exposed by this set council game input gate instance for downstream processing.
    /// </summary>
    /// <value>The legal actions value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets the reason value that forms part of the set council game input gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a council game session snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilGameSessionSnapshot
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council game session snapshot instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the stable game key used to identify or correlate this council game session snapshot instance with related application state.
    /// </summary>
    /// <value>The game key value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable team key used to identify or correlate this council game session snapshot instance with related application state.
    /// </summary>
    /// <value>The team key value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this council game session snapshot instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the display name value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string Status { get; set; } = "Running";
    /// <summary>
    /// Gets or sets the control mode value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control mode value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public CouncilGameControlMode ControlMode { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether autoplay enabled applies to the council game session snapshot state.
    /// </summary>
    /// <value>The autoplay enabled value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets the autoplay delay milliseconds value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The autoplay delay milliseconds value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>
    /// Gets or sets a value indicating whether human input required applies to the council game session snapshot state.
    /// </summary>
    /// <value>The human input required value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets the input reason value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input reason value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string InputReason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the current turn owner value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current turn owner value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string CurrentTurnOwner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the director mode value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; }
    /// <summary>
    /// Gets or sets the game director name value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The game director name value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string GameDirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets the game director model name value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The game director model name value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string GameDirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the creature director count that quantifies the associated council game session snapshot data.
    /// </summary>
    /// <value>The creature director count value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets the last director decision value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last director decision value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string LastDirectorDecision { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last director predictions collection maintained or exposed by this council game session snapshot instance for downstream processing.
    /// </summary>
    /// <value>The last director predictions value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public IReadOnlyList<CouncilGameSubdirectorPrediction> LastDirectorPredictions { get; set; } = [];
    /// <summary>
    /// Gets or sets the turn value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets the frame width value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame width value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int FrameWidth { get; set; } = 80;
    /// <summary>
    /// Gets or sets the frame height value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame height value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int FrameHeight { get; set; } = 25;
    /// <summary>
    /// Gets or sets the frame text value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame text value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame caption value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame caption value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string FrameCaption { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame renderer value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame renderer value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string FrameRenderer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the legal actions collection maintained or exposed by this council game session snapshot instance for downstream processing.
    /// </summary>
    /// <value>The legal actions value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets the input bindings collection maintained or exposed by this council game session snapshot instance for downstream processing.
    /// </summary>
    /// <value>The input bindings value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public IReadOnlyList<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets the last action value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last action value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string LastAction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last action by value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last action by value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public string LastActionBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the player x value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The player x value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int PlayerX { get; set; }
    /// <summary>
    /// Gets or sets the player y value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The player y value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int PlayerY { get; set; }
    /// <summary>
    /// Gets or sets the facing degrees value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The facing degrees value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public double FacingDegrees { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether ducking applies to the council game session snapshot state.
    /// </summary>
    /// <value>The is ducking value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public bool IsDucking { get; set; }
    /// <summary>
    /// Gets or sets the health value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The health value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int Health { get; set; } = 100;
    /// <summary>
    /// Gets or sets the ammo value that forms part of the council game session snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ammo value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public int Ammo { get; set; } = 24;
    /// <summary>
    /// Gets or sets the created at UTC associated with this council game session snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council game session snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilGameSessionSnapshot"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Internal authoritative state for one in-chat Council game session. It is deliberately
/// data-only so the session service owns orchestration, rendering and synchronization policy.
/// </summary>
public sealed class CouncilGameSessionState
{
    /// <summary>
    /// Gets the sync root value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the stable game key used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The game key value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable team key used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The team key value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the display name value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string Status { get; set; } = "Running";
    /// <summary>
    /// Gets or sets the control mode value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control mode value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public CouncilGameControlMode ControlMode { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether autoplay enabled applies to the council game session state.
    /// </summary>
    /// <value>The autoplay enabled value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets the autoplay delay milliseconds value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The autoplay delay milliseconds value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>
    /// Gets or sets a value indicating whether human input required applies to the council game session state.
    /// </summary>
    /// <value>The human input required value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets the input reason value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input reason value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string InputReason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the current turn owner value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current turn owner value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string CurrentTurnOwner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the director mode value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>
    /// Gets or sets the game director name value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The game director name value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string GameDirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets the game director model name value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The game director model name value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string GameDirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the creature director count that quantifies the associated council game session data.
    /// </summary>
    /// <value>The creature director count value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets the last director decision value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last director decision value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string LastDirectorDecision { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last director predictions collection maintained or exposed by this council game session instance for downstream processing.
    /// </summary>
    /// <value>The last director predictions value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public List<CouncilGameSubdirectorPrediction> LastDirectorPredictions { get; set; } = [];
    /// <summary>
    /// Gets or sets the turn value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets the frame width value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame width value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int FrameWidth { get; set; }
    /// <summary>
    /// Gets or sets the frame height value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame height value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int FrameHeight { get; set; }
    /// <summary>
    /// Gets or sets the frame text value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame text value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame caption value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame caption value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string FrameCaption { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame renderer value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame renderer value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string FrameRenderer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame owner value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame owner value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string FrameOwner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame owner turn value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame owner turn value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public long FrameOwnerTurn { get; set; } = -1;
    /// <summary>
    /// Gets or sets the legal actions collection maintained or exposed by this council game session instance for downstream processing.
    /// </summary>
    /// <value>The legal actions value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public List<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets the input bindings collection maintained or exposed by this council game session instance for downstream processing.
    /// </summary>
    /// <value>The input bindings value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public List<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets the last action value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last action value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string LastAction { get; set; } = "start";
    /// <summary>
    /// Gets or sets the last action by value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last action by value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string LastActionBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the player x value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The player x value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int PlayerX { get; set; }
    /// <summary>
    /// Gets or sets the player y value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The player y value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int PlayerY { get; set; }
    /// <summary>
    /// Gets or sets the facing radians value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The facing radians value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public double FacingRadians { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether ducking applies to the council game session state.
    /// </summary>
    /// <value>The is ducking value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public bool IsDucking { get; set; }
    /// <summary>
    /// Gets or sets the health value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The health value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int Health { get; set; } = 100;
    /// <summary>
    /// Gets or sets the ammo value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ammo value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int Ammo { get; set; } = 24;
    /// <summary>
    /// Gets or sets the muzzle flash value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The muzzle flash value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int MuzzleFlash { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether pulse applies to the council game session state.
    /// </summary>
    /// <value>The use pulse value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int UsePulse { get; set; }
    /// <summary>
    /// Gets or sets the story line value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The story line value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string StoryLine { get; set; } = "A bell rings once. The village waits for your choice.";
    /// <summary>
    /// Gets or sets the created at UTC associated with this council game session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council game session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

