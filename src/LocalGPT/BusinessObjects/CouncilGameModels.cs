using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Lists supported council game control mode values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameControlMode
{
    Human,
    Ai,
    Shared
}

/// <summary>
/// Represents a start council game request.
/// </summary>
public sealed class StartCouncilGameRequest
{
    /// <summary>
    /// Gets or sets game key.
    /// </summary>
    public string GameKey { get; set; } = "ascii-doom";
    /// <summary>
    /// Gets or sets team key.
    /// </summary>
    public string TeamKey { get; set; } = "ascii-doom-council-adventure";
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets control mode.
    /// </summary>
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Human;
    /// <summary>
    /// Gets or sets autoplay enabled.
    /// </summary>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets autoplay delay milliseconds.
    /// </summary>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>
    /// Gets or sets director mode.
    /// </summary>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>
    /// Gets or sets game director model name.
    /// </summary>
    public string GameDirectorModelName { get; set; } = "qwen3.5:0.8b";
    /// <summary>
    /// Gets or sets creature director count.
    /// </summary>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets started by.
    /// </summary>
    public string StartedBy { get; set; } = "Human User";
}

/// <summary>
/// Represents a council game control request.
/// </summary>
public sealed class CouncilGameControlRequest
{
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets action.
    /// </summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets axis x.
    /// </summary>
    public double? AxisX { get; set; }
    /// <summary>
    /// Gets or sets axis y.
    /// </summary>
    public double? AxisY { get; set; }
    /// <summary>
    /// Gets or sets aim x.
    /// </summary>
    public int? AimX { get; set; }
    /// <summary>
    /// Gets or sets aim y.
    /// </summary>
    public int? AimY { get; set; }
    /// <summary>
    /// Gets or sets source.
    /// </summary>
    public string Source { get; set; } = "Human";
    /// <summary>
    /// Gets or sets actor name.
    /// </summary>
    public string ActorName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets actor kind.
    /// </summary>
    public CouncilGameActorKind ActorKind { get; set; } = CouncilGameActorKind.Player;
    /// <summary>
    /// Gets or sets runtime class key.
    /// </summary>
    public string RuntimeClassKey { get; set; } = "games.ascii.doom.player";
    /// <summary>
    /// Gets or sets expected turn.
    /// </summary>
    public long? ExpectedTurn { get; set; }
}

/// <summary>
/// Represents a submit council game frame request.
/// </summary>
public sealed class SubmitCouncilGameFrameRequest
{
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets turn.
    /// </summary>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets renderer name.
    /// </summary>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame text.
    /// </summary>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets caption.
    /// </summary>
    public string Caption { get; set; } = string.Empty;
}

/// <summary>
/// Represents a set council game control mode request.
/// </summary>
public sealed class SetCouncilGameControlModeRequest
{
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets control mode.
    /// </summary>
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Human;
    /// <summary>
    /// Gets or sets autoplay enabled.
    /// </summary>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets autoplay delay milliseconds.
    /// </summary>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
}

/// <summary>
/// Represents a set council game input gate request.
/// </summary>
public sealed class SetCouncilGameInputGateRequest
{
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets human input required.
    /// </summary>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets legal actions.
    /// </summary>
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a council game session snapshot.
/// </summary>
public sealed class CouncilGameSessionSnapshot
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets game key.
    /// </summary>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets team key.
    /// </summary>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Running";
    /// <summary>
    /// Gets or sets control mode.
    /// </summary>
    public CouncilGameControlMode ControlMode { get; set; }
    /// <summary>
    /// Gets or sets autoplay enabled.
    /// </summary>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets autoplay delay milliseconds.
    /// </summary>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>
    /// Gets or sets human input required.
    /// </summary>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets input reason.
    /// </summary>
    public string InputReason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current turn owner.
    /// </summary>
    public string CurrentTurnOwner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets director mode.
    /// </summary>
    public CouncilGameDirectorMode DirectorMode { get; set; }
    /// <summary>
    /// Gets or sets game director name.
    /// </summary>
    public string GameDirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets game director model name.
    /// </summary>
    public string GameDirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets creature director count.
    /// </summary>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets last director decision.
    /// </summary>
    public string LastDirectorDecision { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last director predictions.
    /// </summary>
    public IReadOnlyList<CouncilGameSubdirectorPrediction> LastDirectorPredictions { get; set; } = [];
    /// <summary>
    /// Gets or sets turn.
    /// </summary>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets frame width.
    /// </summary>
    public int FrameWidth { get; set; } = 80;
    /// <summary>
    /// Gets or sets frame height.
    /// </summary>
    public int FrameHeight { get; set; } = 25;
    /// <summary>
    /// Gets or sets frame text.
    /// </summary>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame caption.
    /// </summary>
    public string FrameCaption { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame renderer.
    /// </summary>
    public string FrameRenderer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets legal actions.
    /// </summary>
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets input bindings.
    /// </summary>
    public IReadOnlyList<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets last action.
    /// </summary>
    public string LastAction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last action by.
    /// </summary>
    public string LastActionBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets player x.
    /// </summary>
    public int PlayerX { get; set; }
    /// <summary>
    /// Gets or sets player y.
    /// </summary>
    public int PlayerY { get; set; }
    /// <summary>
    /// Gets or sets facing degrees.
    /// </summary>
    public double FacingDegrees { get; set; }
    /// <summary>
    /// Gets or sets is ducking.
    /// </summary>
    public bool IsDucking { get; set; }
    /// <summary>
    /// Gets or sets health.
    /// </summary>
    public int Health { get; set; } = 100;
    /// <summary>
    /// Gets or sets ammo.
    /// </summary>
    public int Ammo { get; set; } = 24;
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Internal authoritative state for one in-chat Council game session. It is deliberately
/// data-only so the session service owns orchestration, rendering and synchronization policy.
/// </summary>
public sealed class CouncilGameSessionState
{
    /// <summary>
    /// Gets or sets sync root.
    /// </summary>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets game key.
    /// </summary>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets team key.
    /// </summary>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Running";
    /// <summary>
    /// Gets or sets control mode.
    /// </summary>
    public CouncilGameControlMode ControlMode { get; set; }
    /// <summary>
    /// Gets or sets autoplay enabled.
    /// </summary>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets autoplay delay milliseconds.
    /// </summary>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>
    /// Gets or sets human input required.
    /// </summary>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets input reason.
    /// </summary>
    public string InputReason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current turn owner.
    /// </summary>
    public string CurrentTurnOwner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets director mode.
    /// </summary>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>
    /// Gets or sets game director name.
    /// </summary>
    public string GameDirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets game director model name.
    /// </summary>
    public string GameDirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets creature director count.
    /// </summary>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets last director decision.
    /// </summary>
    public string LastDirectorDecision { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last director predictions.
    /// </summary>
    public List<CouncilGameSubdirectorPrediction> LastDirectorPredictions { get; set; } = [];
    /// <summary>
    /// Gets or sets turn.
    /// </summary>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets frame width.
    /// </summary>
    public int FrameWidth { get; set; }
    /// <summary>
    /// Gets or sets frame height.
    /// </summary>
    public int FrameHeight { get; set; }
    /// <summary>
    /// Gets or sets frame text.
    /// </summary>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame caption.
    /// </summary>
    public string FrameCaption { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame renderer.
    /// </summary>
    public string FrameRenderer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame owner.
    /// </summary>
    public string FrameOwner { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets frame owner turn.
    /// </summary>
    public long FrameOwnerTurn { get; set; } = -1;
    /// <summary>
    /// Gets or sets legal actions.
    /// </summary>
    public List<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets input bindings.
    /// </summary>
    public List<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets last action.
    /// </summary>
    public string LastAction { get; set; } = "start";
    /// <summary>
    /// Gets or sets last action by.
    /// </summary>
    public string LastActionBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets player x.
    /// </summary>
    public int PlayerX { get; set; }
    /// <summary>
    /// Gets or sets player y.
    /// </summary>
    public int PlayerY { get; set; }
    /// <summary>
    /// Gets or sets facing radians.
    /// </summary>
    public double FacingRadians { get; set; }
    /// <summary>
    /// Gets or sets is ducking.
    /// </summary>
    public bool IsDucking { get; set; }
    /// <summary>
    /// Gets or sets health.
    /// </summary>
    public int Health { get; set; } = 100;
    /// <summary>
    /// Gets or sets ammo.
    /// </summary>
    public int Ammo { get; set; } = 24;
    /// <summary>
    /// Gets or sets muzzle flash.
    /// </summary>
    public int MuzzleFlash { get; set; }
    /// <summary>
    /// Gets or sets use pulse.
    /// </summary>
    public int UsePulse { get; set; }
    /// <summary>
    /// Gets or sets story line.
    /// </summary>
    public string StoryLine { get; set; } = "A bell rings once. The village waits for your choice.";
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

