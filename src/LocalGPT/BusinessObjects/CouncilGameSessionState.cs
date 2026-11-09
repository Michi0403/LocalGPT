using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Internal authoritative state for one in-chat Council game session. It is deliberately
/// data-only so the session service owns orchestration, rendering and synchronization policy.
/// </summary>
public sealed class CouncilGameSessionState
{
    /// <summary>Immutable authored Game Studio scene selected from the approved project revision.</summary>
    /// <value>The scene value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public GameSceneDocument? Scene { get; set; }
    /// <summary>Authoritative fixed-tick state; separate from user-authored scene data.</summary>
    /// <value>The scene clock value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public GameSceneClock? SceneClock { get; set; }
    /// <summary>Exact source revision for play and reload identity.</summary>
    /// <value>The source revision identifier value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public Guid? SourceRevisionId { get; set; }
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
    /// Gets or sets the ascii color mode value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ascii color mode value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>
    /// Gets or sets the default foreground color value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default foreground color value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>
    /// Gets or sets the default background color value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default background color value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets presentation-only style runs for the current frame.</summary>
    /// <value>The frame style runs value exposed by <see cref="CouncilGameSessionState"/>.</value>
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
    /// <summary>
    /// Gets or sets the animation frames collection maintained or exposed by this council game session instance for downstream processing.
    /// </summary>
    /// <value>The animation frames value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public List<string> AnimationFrames { get; set; } = [];
    /// <summary>
    /// Gets or sets the animation delay milliseconds value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The animation delay milliseconds value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int AnimationDelayMilliseconds { get; set; } = 650;
    /// <summary>
    /// Gets or sets the animation subtitle value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The animation subtitle value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string AnimationSubtitle { get; set; } = string.Empty;
    /// <summary>Gets or sets how long the subtitle remains after the last animation frame.</summary>
    /// <value>The animation subtitle hold milliseconds value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int AnimationSubtitleHoldMilliseconds { get; set; } = 1500;
    /// <summary>Gets or sets per-frame style runs for local animation playback.</summary>
    /// <value>The animation frame style runs value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public List<List<CouncilAsciiStyleRun>> AnimationFrameStyleRuns { get; set; } = [];
    /// <summary>Gets or sets optional presentation metadata for the held animation subtitle.</summary>
    /// <value>The animation subtitle style value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public CouncilAsciiTextStyle? AnimationSubtitleStyle { get; set; }
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
    /// <summary>Gets or sets the deterministic seed shared by the campaign before per-level derivation.</summary>
    /// <value>The campaign seed value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int CampaignSeed { get; set; }
    /// <summary>
    /// Gets or sets the world map collection maintained or exposed by this council game session instance for downstream processing.
    /// </summary>
    /// <value>The world map value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public List<string> WorldMap { get; set; } = [];
    /// <summary>
    /// Gets or sets the map seed value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map seed value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int MapSeed { get; set; }
    /// <summary>Gets or sets the bounded scenario description associated with this game.</summary>
    /// <value>The scenario prompt value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the blocked move streak value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The blocked move streak value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public int BlockedMoveStreak { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether last move blocked applies to the council game session state.
    /// </summary>
    /// <value>The last move blocked value exposed by <see cref="CouncilGameSessionState"/>.</value>
    public bool LastMoveBlocked { get; set; }
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

    /// <summary>
    /// Gets or sets the declarative ruleset the session runs: view, turn model, map generator, autoplay and the
    /// game's stats, archetypes, actions, rules, levels, texts and aliases. The engine reads it; it never owns it.
    /// </summary>
    /// <value>The ruleset copied from the launched game definition.</value>
    public LocalGPT.BusinessObjects.GameEngine.GameRuleset Ruleset { get; set; } = new();

    /// <summary>
    /// Gets or sets every actor of the session: the controlled player, opponents, entrants, team members and objects.
    /// </summary>
    /// <value>The actors in creation order; their meaning comes from the archetype and role rows of the ruleset.</value>
    public List<LocalGPT.BusinessObjects.GameEngine.GameActorState> Actors { get; set; } = [];

    /// <summary>
    /// Gets or sets the named map markers such as goals, exits or switches placed by the map generator.
    /// </summary>
    /// <value>The markers of the current map.</value>
    public List<LocalGPT.BusinessObjects.GameEngine.GameMarkerState> Markers { get; set; } = [];

    /// <summary>
    /// Gets or sets the named session variables written by rule and action primitives (flags, counters, story text).
    /// </summary>
    /// <value>The variable values keyed case-insensitively by their ruleset-defined name.</value>
    public Dictionary<string, string> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the level, round, match and exchange progression of the session.
    /// </summary>
    /// <value>The progression state owned by the progression primitives.</value>
    public LocalGPT.BusinessObjects.GameEngine.GameProgressionState Progression { get; set; } = new();

    /// <summary>
    /// Gets or sets the bounded engine timeline of the most recent authoritative events.
    /// </summary>
    /// <value>The recent timeline entries, oldest first.</value>
    public List<LocalGPT.BusinessObjects.GameEngine.GameTimelineEntry> Timeline { get; set; } = [];

    /// <summary>
    /// Gets or sets the latest engine message shown in the heads-up display.
    /// </summary>
    /// <value>The message text produced from the ruleset texts.</value>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the heading used by authored Game Studio scenes for relative movement.
    /// </summary>
    /// <value>The scene heading in radians.</value>
    public double SceneHeadingRadians { get; set; }
}
