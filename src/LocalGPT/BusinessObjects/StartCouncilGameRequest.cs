using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for start council game, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class StartCouncilGameRequest
{
    /// <summary>
    /// Gets or sets the key or alias of the game project to start; blank starts the game named by the runtime policy value
    /// <c>GameDefaultGameKey</c>. Ignored when <see cref="Definition"/> is supplied.
    /// </summary>
    /// <value>The game key or alias.</value>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the Council team the session belongs to; blank uses the game's default team.
    /// </summary>
    /// <value>The team key.</value>
    public string TeamKey { get; set; } = string.Empty;
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
    /// <summary>
    /// Gets or sets the frame width value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame width value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int FrameWidth { get; set; } = 80;
    /// <summary>
    /// Gets or sets the frame height value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame height value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int FrameHeight { get; set; } = 25;
    /// <summary>Gets or sets the requested ASCII color contract. Project-built definitions override this value.</summary>
    /// <value>The ascii color mode value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>
    /// Gets or sets the default foreground color value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default foreground color value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>
    /// Gets or sets the default background color value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default background color value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets an optional deterministic map seed. Zero or null lets LocalGPT derive a fresh seed.</summary>
    /// <value>The map seed value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int? MapSeed { get; set; }
    /// <summary>Gets or sets an optional bounded scenario description used to name and deterministically vary a fresh ASCII corridor map.</summary>
    /// <value>The scenario prompt value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional 1-based campaign level override. Null uses the runtime-class default.</summary>
    /// <value>The starting level value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public int? StartingLevel { get; set; }
    /// <summary>Gets or sets an optional automatic level-advance override. Null uses the runtime-class default.</summary>
    /// <value>The auto advance levels value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public bool? AutoAdvanceLevels { get; set; }
    /// <summary>
    /// Gets or sets the started by value that forms part of the start council game state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The started by value exposed by <see cref="StartCouncilGameRequest"/>.</value>
    public string StartedBy { get; set; } = "Human User";
}
