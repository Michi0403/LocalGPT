using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameControlMode
{
    Human,
    Ai,
    Shared
}

public sealed class StartCouncilGameRequest
{
    public string GameKey { get; set; } = "ascii-doom";
    public string TeamKey { get; set; } = "ascii-doom-council-adventure";
    public Guid? ConversationId { get; set; }
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Human;
    public bool AutoplayEnabled { get; set; }
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    public string StartedBy { get; set; } = "Human User";
}

public sealed class CouncilGameControlRequest
{
    public Guid SessionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public double? AxisX { get; set; }
    public double? AxisY { get; set; }
    public int? AimX { get; set; }
    public int? AimY { get; set; }
    public string Source { get; set; } = "Human";
    public string ActorName { get; set; } = "Human User";
    public long? ExpectedTurn { get; set; }
}

public sealed class SubmitCouncilGameFrameRequest
{
    public Guid SessionId { get; set; }
    public long Turn { get; set; }
    public string RendererName { get; set; } = string.Empty;
    public string FrameText { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public sealed class SetCouncilGameControlModeRequest
{
    public Guid SessionId { get; set; }
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Human;
    public bool AutoplayEnabled { get; set; }
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
}

public sealed class SetCouncilGameInputGateRequest
{
    public Guid SessionId { get; set; }
    public bool HumanInputRequired { get; set; }
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
}

public sealed class CouncilGameSessionSnapshot
{
    public Guid Id { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string TeamKey { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "Running";
    public CouncilGameControlMode ControlMode { get; set; }
    public bool AutoplayEnabled { get; set; }
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    public bool HumanInputRequired { get; set; }
    public string InputReason { get; set; } = string.Empty;
    public string CurrentTurnOwner { get; set; } = string.Empty;
    public long Turn { get; set; }
    public int FrameWidth { get; set; } = 80;
    public int FrameHeight { get; set; } = 25;
    public string FrameText { get; set; } = string.Empty;
    public string FrameCaption { get; set; } = string.Empty;
    public string FrameRenderer { get; set; } = string.Empty;
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    public IReadOnlyList<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    public string LastAction { get; set; } = string.Empty;
    public string LastActionBy { get; set; } = string.Empty;
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public double FacingDegrees { get; set; }
    public bool IsDucking { get; set; }
    public int Health { get; set; } = 100;
    public int Ammo { get; set; } = 24;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Internal authoritative state for one in-chat Council game session. It is deliberately
/// data-only so the session service owns orchestration, rendering and synchronization policy.
/// </summary>
public sealed class CouncilGameSessionState
{
    public object SyncRoot { get; } = new();
    public Guid Id { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string TeamKey { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "Running";
    public CouncilGameControlMode ControlMode { get; set; }
    public bool AutoplayEnabled { get; set; }
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    public bool HumanInputRequired { get; set; }
    public string InputReason { get; set; } = string.Empty;
    public string CurrentTurnOwner { get; set; } = string.Empty;
    public long Turn { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public string FrameText { get; set; } = string.Empty;
    public string FrameCaption { get; set; } = string.Empty;
    public string FrameRenderer { get; set; } = string.Empty;
    public string FrameOwner { get; set; } = string.Empty;
    public long FrameOwnerTurn { get; set; } = -1;
    public List<string> LegalActions { get; set; } = [];
    public List<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    public string LastAction { get; set; } = "start";
    public string LastActionBy { get; set; } = string.Empty;
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public double FacingRadians { get; set; }
    public bool IsDucking { get; set; }
    public int Health { get; set; } = 100;
    public int Ammo { get; set; } = 24;
    public int MuzzleFlash { get; set; }
    public int UsePulse { get; set; }
    public string StoryLine { get; set; } = "A bell rings once. The village waits for your choice.";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

