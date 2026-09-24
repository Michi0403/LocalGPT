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
/// Represents one deterministic ASCII DOOM campaign level profile. The runtime class system owns the
/// configurable defaults; the game service only clamps and applies the supplied values.
/// </summary>
public sealed class CouncilGameLevelProfile
{
    /// <summary>Gets or sets the 1-based level number.</summary>
    public int Level { get; set; } = 1;
    /// <summary>Gets or sets the human-readable level name.</summary>
    public string Name { get; set; } = "Corridor 01";
    /// <summary>Gets or sets the bounded difficulty rating from 1 (easy) through 10 (hard).</summary>
    public int Difficulty { get; set; } = 1;
    /// <summary>Gets or sets the deterministic map width in world cells.</summary>
    public int MapWidth { get; set; } = 28;
    /// <summary>Gets or sets the deterministic map height in world cells.</summary>
    public int MapHeight { get; set; } = 18;
    /// <summary>Gets or sets the requested total carved-room count including the starting room.</summary>
    public int RoomCount { get; set; } = 5;
    /// <summary>Gets or sets the target hostile density expressed as enemies per carved room.</summary>
    public double EnemyDensity { get; set; } = .6d;
    /// <summary>Gets or sets the hostile-health multiplier applied to deterministic archetype health.</summary>
    public double EnemyHealthMultiplier { get; set; } = .8d;
    /// <summary>Gets or sets the hostile contact-damage multiplier applied to deterministic archetype damage.</summary>
    public double EnemyDamageMultiplier { get; set; } = .75d;
    /// <summary>Gets or sets the player health restored when this level starts.</summary>
    public int StartingHealth { get; set; } = 100;
    /// <summary>Gets or sets the ammunition restored when this level starts.</summary>
    public int StartingAmmo { get; set; } = 30;
}

/// <summary>Normalized copyable rules consumed by the deterministic Kernel Creature Tournament engine.</summary>
public sealed class CouncilKernelTournamentRules
{
    public string RuntimeClassKey { get; set; } = "games.ascii.kernel-tournament.rules";
    public int StartingHealth { get; set; } = 100;
    public int MinimumDamage { get; set; } = 7;
    public int MaximumDamage { get; set; } = 18;
    public int GuardReduction { get; set; } = 7;
    public int RecoveryAmount { get; set; } = 6;
    public int MaximumExchangesPerMatch { get; set; } = 12;
    /// <summary>Gets or sets the default number of creature slots owned by each trainer for one fight.</summary>
    public int CreaturesPerTrainer { get; set; } = 3;
    /// <summary>Gets or sets the maximum trainer-initiated creature switches allowed during one fight.</summary>
    public int MaximumCreatureSwitchesPerFight { get; set; } = 3;
    /// <summary>Gets or sets the bounded passive health recovery applied to benched creatures after each exchange.</summary>
    public int RestRecoveryPerExchange { get; set; } = 3;
    /// <summary>Gets or sets the bounded attack bonus applied by the deterministic FOCUS trainer action.</summary>
    public int TrainerFocusBonus { get; set; } = 2;
    /// <summary>Gets or sets the bounded incoming-damage reduction applied by the deterministic BRACE trainer action.</summary>
    public int TrainerBraceReduction { get; set; } = 2;
    public int AnimationFrameDelayMilliseconds { get; set; } = 320;
    public int SubtitleHoldMilliseconds { get; set; } = 1500;
}

/// <summary>Stores one named ASCII actor joint so persistent trainer/creature rigs can be rendered consistently across poses.</summary>
public sealed class CouncilAsciiActorJoint
{
    /// <summary>Gets or sets the stable joint name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the zero-based horizontal anchor coordinate inside the actor rig.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the zero-based vertical anchor coordinate inside the actor rig.</summary>
    public int Y { get; set; }
}

/// <summary>Stores a small persistent ASCII actor rig that keeps model joints connected while presentation poses change.</summary>
public sealed class CouncilAsciiActorRig
{
    /// <summary>Gets or sets the stable rig key.</summary>
    public string RigKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the rig width in terminal cells.</summary>
    public int Width { get; set; } = 13;
    /// <summary>Gets or sets the rig height in terminal cells.</summary>
    public int Height { get; set; } = 6;
    /// <summary>Gets or sets the identity glyph rendered at the rig head anchor.</summary>
    public char HeadGlyph { get; set; } = '@';
    /// <summary>Gets or sets the identity glyph rendered at the rig torso anchor.</summary>
    public char TorsoGlyph { get; set; } = '#';
    /// <summary>Gets or sets the stable base-pose joint anchors.</summary>
    public List<CouncilAsciiActorJoint> Joints { get; set; } = [];
}

/// <summary>Stores one deterministic creature slot owned by a tournament trainer.</summary>
public sealed class CouncilKernelTournamentCreatureState
{
    /// <summary>Gets or sets the stable creature slot identifier.</summary>
    public string CreatureId { get; set; } = string.Empty;
    /// <summary>Gets or sets the human-readable creature name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded species descriptor.</summary>
    public string Species { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded visual style descriptor.</summary>
    public string Style { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded body/form descriptor.</summary>
    public string Form { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded tactical trait descriptor.</summary>
    public string Trait { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded in-character voice descriptor.</summary>
    public string Voice { get; set; } = string.Empty;
    /// <summary>Gets or sets the authoritative current health for this creature slot.</summary>
    public int Health { get; set; } = 100;
    /// <summary>Gets or sets whether this creature currently occupies the active arena slot.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets whether this creature is currently resting on the trainer bench.</summary>
    public bool IsResting { get; set; }
    /// <summary>Gets or sets the number of completed exchanges this creature has spent resting.</summary>
    public int RestedExchanges { get; set; }
    /// <summary>Gets or sets the persistent ASCII actor rig for this creature.</summary>
    public CouncilAsciiActorRig Rig { get; set; } = new();
}

/// <summary>Stores one compact engine event shown as the trainer/creature team timeline between exchanges.</summary>
public sealed class CouncilKernelTournamentTimelineEntry
{
    /// <summary>Gets or sets the monotonically increasing timeline sequence.</summary>
    public int Sequence { get; set; }
    /// <summary>Gets or sets the tournament round that produced the entry.</summary>
    public int Round { get; set; }
    /// <summary>Gets or sets the match number within the current round.</summary>
    public int Match { get; set; }
    /// <summary>Gets or sets the exchange number within the current match.</summary>
    public int Exchange { get; set; }
    /// <summary>Gets or sets a compact event-kind label such as COMMAND, SWITCH, IMPACT or REST.</summary>
    public string EventKind { get; set; } = string.Empty;
    /// <summary>Gets or sets the left trainer/creature state summary.</summary>
    public string LeftState { get; set; } = string.Empty;
    /// <summary>Gets or sets the right trainer/creature state summary.</summary>
    public string RightState { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded event detail.</summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>Pairs one trainer model with the distinct creature model it owns for the tournament bracket.</summary>
public sealed class CouncilKernelTournamentContestantSeed
{
    public string TrainerModelName { get; set; } = string.Empty;
    public string CreatureModelName { get; set; } = string.Empty;
    public string CreatureName { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded species descriptor used to keep this tournament creature visually recognizable.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureSpecies { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded visual style descriptor used by ASCII creature presentation.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureStyle { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded body/form descriptor used by ASCII creature presentation.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureForm { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded tactical personality trait attached to this tournament creature.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureTrait { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded in-character voice line associated with this tournament creature.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureVoice { get; set; } = string.Empty;
    /// <summary>Gets or sets the trainer's compact opening greeting authored during team selection.</summary>
    public string TrainerGreeting { get; set; } = string.Empty;
}

/// <summary>Captures the latest bounded AI-authored trainer or creature evidence for one tournament exchange.</summary>
public sealed class CouncilKernelTournamentRoleEvidence
{
    public string Role { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>Requests initialization or exactly one authoritative tournament exchange.</summary>
public sealed class CouncilKernelTournamentAdvanceRequest
{
    public Guid SessionId { get; set; }
    public bool InitializeOnly { get; set; }
    public IReadOnlyList<CouncilKernelTournamentContestantSeed> Contestants { get; set; } = [];
    /// <summary>Gets or sets bounded presentation-only frames prepared by the tournament ASCII artist before deterministic bracket initialization.</summary>
    /// <value>Complete terminal-safe frames that may precede the engine-owned lineup frames without changing authoritative tournament state.</value>
    public IReadOnlyList<string> PresentationFrames { get; set; } = [];
    public IReadOnlyList<CouncilKernelTournamentRoleEvidence> Evidence { get; set; } = [];
}

/// <summary>One authoritative fighter entry owned by the deterministic tournament engine.</summary>
public sealed class CouncilKernelTournamentFighterState
{
    public string FighterId { get; set; } = string.Empty;
    public string TrainerModelName { get; set; } = string.Empty;
    public string CreatureModelName { get; set; } = string.Empty;
    public string CreatureName { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded species descriptor used to keep this tournament creature visually recognizable.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureSpecies { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded visual style descriptor used by ASCII creature presentation.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureStyle { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded body/form descriptor used by ASCII creature presentation.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureForm { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded tactical personality trait attached to this tournament creature.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureTrait { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded in-character voice line associated with this tournament creature.</summary>
    /// <value>The persisted bounded creature descriptor.</value>
    public string CreatureVoice { get; set; } = string.Empty;
    /// <summary>Gets or sets the trainer's compact opening greeting retained for arena presentation.</summary>
    public string TrainerGreeting { get; set; } = string.Empty;
    /// <summary>Gets or sets the trainer's persistent ASCII actor rig.</summary>
    public CouncilAsciiActorRig TrainerRig { get; set; } = new();
    /// <summary>Gets or sets the complete creature roster owned by this trainer for the current fight.</summary>
    public List<CouncilKernelTournamentCreatureState> CreatureRoster { get; set; } = [];
    /// <summary>Gets or sets the active creature slot identifier mirrored by the compatibility creature fields above.</summary>
    public string ActiveCreatureId { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of trainer-initiated creature switches used in the current fight.</summary>
    public int SwitchesUsedInCurrentFight { get; set; }
    /// <summary>Gets or sets the last bounded trainer action applied by the deterministic engine.</summary>
    public string LastTrainerAction { get; set; } = "NONE";
    public int Health { get; set; } = 100;
    public int Wins { get; set; }
    public bool Eliminated { get; set; }
}

/// <summary>Returns the engine-owned transcript consequence and the updated game-session snapshot.</summary>
public sealed class CouncilKernelTournamentResolution
{
    public CouncilGameSessionSnapshot Session { get; set; } = new();
    public string SummaryMarkdown { get; set; } = string.Empty;
    public bool Completed { get; set; }
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
    /// <summary>Gets or sets the Council run that owns this game when it was started from an AI Council workflow.</summary>
    /// <value>The owning Council run identifier, or <c>null</c> for a standalone game.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>Gets or sets the LocalGPT project that produced the supplied built game definition, when this session is project-owned.</summary>
    /// <value>The owning project identifier, or <c>null</c> for legacy/built-in sessions.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>Gets or sets the project-built runtime definition consumed by GameDirector.</summary>
    /// <value>The compiled game definition, or <c>null</c> when a legacy built-in game key is used.</value>
    public ProjectGameDefinition? Definition { get; set; }
    /// <summary>
    /// Gets or sets the control mode value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control mode value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Shared;
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
    /// <summary>Gets or sets the requested terminal-cell display width.</summary>
    public int FrameWidth { get; set; } = 80;
    /// <summary>Gets or sets the requested terminal-cell display height.</summary>
    public int FrameHeight { get; set; } = 25;
    /// <summary>Gets or sets the requested ASCII color contract. Project-built definitions override this value.</summary>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Gets or sets the requested default foreground palette index.</summary>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Gets or sets the requested default background palette index.</summary>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets an optional deterministic map seed. Zero or null lets LocalGPT derive a fresh seed.</summary>
    public int? MapSeed { get; set; }
    /// <summary>Gets or sets an optional bounded scenario description used to name and deterministically vary a fresh ASCII corridor map.</summary>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional database-backed campaign runtime-class override. Blank resolves the campaign class assigned to the selected Council team before falling back to the maintained starter.</summary>
    public string CampaignRuntimeClassKey { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional Kernel Creature Tournament rules runtime-class override.</summary>
    public string TournamentRuntimeClassKey { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional 1-based campaign level override. Null uses the runtime-class default.</summary>
    public int? StartingLevel { get; set; }
    /// <summary>Gets or sets an optional automatic level-advance override. Null uses the runtime-class default.</summary>
    public bool? AutoAdvanceLevels { get; set; }
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
    /// <summary>Optionally changes the palette contract for this complete frame.</summary>
    public CouncilAsciiColorMode? AsciiColorMode { get; set; }
    /// <summary>Optionally changes the default foreground palette index.</summary>
    public int? DefaultForegroundColor { get; set; }
    /// <summary>Optionally changes the default background palette index.</summary>
    public int? DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets bounded style runs transported separately from canonical frame text.</summary>
    public IReadOnlyList<CouncilAsciiStyleRun> StyleRuns { get; set; } = [];
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
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Shared;
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

/// <summary>Represents one hostile actor in the deterministic ASCII corridor world.</summary>
public sealed class CouncilGameEnemySnapshot
{
    /// <summary>Gets or sets the stable enemy key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the player-facing enemy name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the single-cell radar glyph.</summary>
    public string Glyph { get; set; } = "M";
    /// <summary>Gets or sets the map x coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the map y coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets current health.</summary>
    public int Health { get; set; }
    /// <summary>Gets or sets maximum health.</summary>
    public int MaximumHealth { get; set; }
    /// <summary>Gets or sets contact damage.</summary>
    public int ContactDamage { get; set; }
    /// <summary>Gets whether this enemy is still active.</summary>
    public bool IsAlive => Health > 0;
}

/// <summary>Internal authoritative hostile state owned by the deterministic Council game service.</summary>
public sealed class CouncilGameEnemyState
{
    /// <summary>Gets or sets the stable enemy key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the player-facing enemy name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the single-cell radar glyph.</summary>
    public string Glyph { get; set; } = "M";
    /// <summary>Gets or sets the map x coordinate.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the map y coordinate.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets current health.</summary>
    public int Health { get; set; }
    /// <summary>Gets or sets maximum health.</summary>
    public int MaximumHealth { get; set; }
    /// <summary>Gets or sets contact damage.</summary>
    public int ContactDamage { get; set; }
    /// <summary>Gets whether this enemy is still active.</summary>
    public bool IsAlive => Health > 0;
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
    /// <summary>Gets or sets the Council run that owns this game, when applicable.</summary>
    /// <value>The owning Council run identifier, or <c>null</c> for a standalone game.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>Gets or sets the project that produced this game runtime, when applicable.</summary>
    /// <value>The project identifier, or <c>null</c> for a legacy built-in runtime.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>Gets or sets the project version captured by the built game definition.</summary>
    /// <value>The source project version; empty for a legacy built-in runtime.</value>
    public string ProjectVersion { get; set; } = string.Empty;
    /// <summary>Gets or sets the runtime profile used to interpret this session independently of its game key.</summary>
    /// <value>The data-driven runtime profile.</value>
    public CouncilGameRuntimeProfile RuntimeProfile { get; set; } = CouncilGameRuntimeProfile.Corridor;
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
    /// <summary>Gets or sets the active ASCII color contract.</summary>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Gets or sets the active default foreground palette index.</summary>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Gets or sets the active default background palette index.</summary>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets presentation-only style runs for <see cref="FrameText"/>.</summary>
    public IReadOnlyList<CouncilAsciiStyleRun> FrameStyleRuns { get; set; } = [];
    /// <summary>Gets or sets pregenerated frames played locally without replacing transcript history.</summary>
    public IReadOnlyList<string> AnimationFrames { get; set; } = [];
    /// <summary>Gets or sets local animation frame delay in milliseconds.</summary>
    public int AnimationDelayMilliseconds { get; set; } = 650;
    /// <summary>Gets or sets the tournament animation subtitle shown over the one-shot movie.</summary>
    public string AnimationSubtitle { get; set; } = string.Empty;
    /// <summary>Gets or sets how long the final tournament subtitle remains visible after the movie completes.</summary>
    public int AnimationSubtitleHoldMilliseconds { get; set; } = 1500;
    /// <summary>Gets or sets per-frame style runs for local animation playback.</summary>
    public IReadOnlyList<IReadOnlyList<CouncilAsciiStyleRun>> AnimationFrameStyleRuns { get; set; } = [];
    /// <summary>Gets or sets optional presentation metadata for the held animation subtitle.</summary>
    public CouncilAsciiTextStyle? AnimationSubtitleStyle { get; set; }
    /// <summary>Gets or sets the runtime class that supplied deterministic tournament rules.</summary>
    public string TournamentRuntimeClassKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the engine-owned tournament bracket fighters.</summary>
    public IReadOnlyList<CouncilKernelTournamentFighterState> TournamentFighters { get; set; } = [];
    /// <summary>Gets or sets the recent bounded trainer/creature team timeline shown by the ASCII game surface.</summary>
    public IReadOnlyList<CouncilKernelTournamentTimelineEntry> TournamentTimeline { get; set; } = [];
    /// <summary>Gets the two engine-owned fighter identifiers that are allowed to act in the current tournament match.</summary>
    public IReadOnlyList<string> TournamentCurrentFighterIds { get; set; } = [];
    /// <summary>Gets or sets the current 1-based tournament bracket round.</summary>
    public int TournamentRound { get; set; }
    /// <summary>Gets or sets the current 1-based exchange within the active match.</summary>
    public int TournamentExchange { get; set; }
    /// <summary>Gets or sets the winning creature name when the bracket is complete.</summary>
    public string TournamentChampion { get; set; } = string.Empty;
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
    /// <summary>Gets or sets the runtime-class key that supplied the current campaign configuration.</summary>
    public string CampaignRuntimeClassKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the current 1-based campaign level.</summary>
    public int CurrentLevel { get; set; } = 1;
    /// <summary>Gets or sets the total configured campaign level count.</summary>
    public int TotalLevels { get; set; } = 1;
    /// <summary>Gets or sets whether successful extraction automatically starts the next configured level.</summary>
    public bool AutoAdvanceLevels { get; set; } = true;
    /// <summary>Gets or sets the current deterministic level profile.</summary>
    public CouncilGameLevelProfile? CurrentLevelProfile { get; set; }
    /// <summary>Gets or sets the deterministic seed shared by the configured campaign.</summary>
    public int CampaignSeed { get; set; }
    /// <summary>Gets or sets the authoritative connected ASCII corridor map.</summary>
    public IReadOnlyList<string> WorldMap { get; set; } = [];
    /// <summary>Gets or sets the deterministic map seed.</summary>
    public int MapSeed { get; set; }
    /// <summary>Gets or sets the bounded scenario description associated with this game.</summary>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the extraction x coordinate.</summary>
    public int ExtractionX { get; set; }
    /// <summary>Gets or sets the extraction y coordinate.</summary>
    public int ExtractionY { get; set; }
    /// <summary>Gets or sets the current hostile actors.</summary>
    public IReadOnlyList<CouncilGameEnemySnapshot> Enemies { get; set; } = [];
    /// <summary>Gets or sets the latest deterministic combat/navigation message.</summary>
    public string CombatMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets the count of consecutive blocked movement attempts.</summary>
    public int BlockedMoveStreak { get; set; }
    /// <summary>Gets whether the latest movement proposal was blocked.</summary>
    public bool LastMoveBlocked { get; set; }
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
    /// <summary>Gets or sets the Council run that owns this authoritative game state, when applicable.</summary>
    /// <value>The owning Council run identifier, or <c>null</c> for a standalone game.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>Gets or sets the LocalGPT project that produced this runtime, when the session came from a game project.</summary>
    /// <value>The owning project identifier, or <c>null</c> for legacy built-in sessions.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>Identifies the authored project version whose compiled definition produced this runtime session.</summary>
    /// <value>The built project version or an empty value for legacy sessions.</value>
    public string ProjectVersion { get; set; } = string.Empty;
    /// <summary>Gets or sets the runtime profile that owns game rules/rendering for this session.</summary>
    /// <value>The selected game runtime profile.</value>
    public CouncilGameRuntimeProfile RuntimeProfile { get; set; } = CouncilGameRuntimeProfile.Corridor;
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
    /// <summary>Gets or sets the active ASCII color contract.</summary>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Gets or sets the active default foreground palette index.</summary>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Gets or sets the active default background palette index.</summary>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets presentation-only style runs for the current frame.</summary>
    public List<CouncilAsciiStyleRun> FrameStyleRuns { get; set; } = [];
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
    /// <summary>Gets or sets pregenerated frames for local presentation playback.</summary>
    public List<string> AnimationFrames { get; set; } = [];
    /// <summary>Gets or sets local animation frame delay in milliseconds.</summary>
    public int AnimationDelayMilliseconds { get; set; } = 650;
    /// <summary>Gets or sets the current one-shot animation subtitle.</summary>
    public string AnimationSubtitle { get; set; } = string.Empty;
    /// <summary>Gets or sets how long the subtitle remains after the last animation frame.</summary>
    public int AnimationSubtitleHoldMilliseconds { get; set; } = 1500;
    /// <summary>Gets or sets per-frame style runs for local animation playback.</summary>
    public List<List<CouncilAsciiStyleRun>> AnimationFrameStyleRuns { get; set; } = [];
    /// <summary>Gets or sets optional presentation metadata for the held animation subtitle.</summary>
    public CouncilAsciiTextStyle? AnimationSubtitleStyle { get; set; }
    /// <summary>Gets or sets the rules runtime class selected for the Kernel Creature Tournament.</summary>
    public string TournamentRuntimeClassKey { get; set; } = string.Empty;
    /// <summary>Gets or sets normalized deterministic tournament rules.</summary>
    public CouncilKernelTournamentRules TournamentRules { get; set; } = new();
    /// <summary>Gets or sets engine-owned fighter state.</summary>
    public List<CouncilKernelTournamentFighterState> TournamentFighters { get; set; } = [];
    /// <summary>Gets or sets the recent bounded trainer/creature team timeline.</summary>
    public List<CouncilKernelTournamentTimelineEntry> TournamentTimeline { get; set; } = [];
    /// <summary>Gets or sets the next monotonic tournament timeline sequence.</summary>
    public int TournamentTimelineSequence { get; set; }
    /// <summary>Gets or sets the fighter ids still participating in the current bracket round.</summary>
    public List<string> TournamentRoundFighterIds { get; set; } = [];
    /// <summary>Gets or sets winners/byes accumulated for the next bracket round.</summary>
    public List<string> TournamentNextRoundFighterIds { get; set; } = [];
    /// <summary>Gets or sets the zero-based position of the active match in the current round list.</summary>
    public int TournamentMatchIndex { get; set; }
    /// <summary>Gets or sets the current 1-based bracket round.</summary>
    public int TournamentRound { get; set; }
    /// <summary>Gets or sets the current 1-based exchange inside the active match.</summary>
    public int TournamentExchange { get; set; }
    /// <summary>Gets or sets the winning fighter id after completion.</summary>
    public string TournamentChampionFighterId { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the bracket has been initialized.</summary>
    public bool TournamentInitialized { get; set; }
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
    /// <summary>Gets or sets the runtime-class key that supplied the current campaign configuration.</summary>
    public string CampaignRuntimeClassKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the configured campaign levels retained for automatic progression.</summary>
    public List<CouncilGameLevelProfile> LevelProfiles { get; set; } = [];
    /// <summary>Gets or sets the zero-based index of the active campaign level.</summary>
    public int CurrentLevelIndex { get; set; }
    /// <summary>Gets or sets whether successful extraction automatically starts the next configured level.</summary>
    public bool AutoAdvanceLevels { get; set; } = true;
    /// <summary>Gets or sets the deterministic seed shared by the campaign before per-level derivation.</summary>
    public int CampaignSeed { get; set; }
    /// <summary>Gets or sets the authoritative connected ASCII corridor map.</summary>
    public List<string> WorldMap { get; set; } = [];
    /// <summary>Gets or sets the deterministic map seed.</summary>
    public int MapSeed { get; set; }
    /// <summary>Gets or sets the bounded scenario description associated with this game.</summary>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the extraction x coordinate.</summary>
    public int ExtractionX { get; set; }
    /// <summary>Gets or sets the extraction y coordinate.</summary>
    public int ExtractionY { get; set; }
    /// <summary>Gets or sets the active hostile actors.</summary>
    public List<CouncilGameEnemyState> Enemies { get; set; } = [];
    /// <summary>Gets or sets the latest deterministic combat/navigation message.</summary>
    public string CombatMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets the count of consecutive blocked movement attempts.</summary>
    public int BlockedMoveStreak { get; set; }
    /// <summary>Gets whether the latest movement proposal was blocked.</summary>
    public bool LastMoveBlocked { get; set; }
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

