using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Collects the editable runtime-facing settings persisted by one LocalGPT Game project before any build is produced.
/// </summary>
public sealed class SaveGameProjectProfileRequest
{
    /// <summary>Defines the stable session/runtime identity that future builds will inherit from the saved profile.</summary>
    /// <value>The desired game key; an empty value lets the service derive it from the project name.</value>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>Defines the player-facing title that future builds will copy into the compiled definition.</summary>
    /// <value>The desired display title; an empty value uses the project name.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Renderer primitive that draws the game (for example first-person-raycast or text-template).</summary>
    /// <value>The view primitive key.</value>
    public string ViewKey { get; set; } = string.Empty;
    /// <summary>JSON parameters of the view primitive.</summary>
    /// <value>The view parameters as JSON.</value>
    public string ViewParametersJson { get; set; } = "{}";
    /// <summary>How turns advance: actions (each control is a turn) or council (council evidence drives the game).</summary>
    /// <value>The turn model key.</value>
    public string TurnModelKey { get; set; } = string.Empty;
    /// <summary>Map generator primitive; empty when the game has no grid map.</summary>
    /// <value>The map generator key.</value>
    public string MapGeneratorKey { get; set; } = string.Empty;
    /// <summary>JSON parameters of the map generator.</summary>
    /// <value>The map parameters as JSON.</value>
    public string MapParametersJson { get; set; } = "{}";
    /// <summary>Autoplay primitive used for AI control; empty for none.</summary>
    /// <value>The autoplay primitive key.</value>
    public string AutoplayKey { get; set; } = string.Empty;
    /// <summary>JSON parameters of the autoplay primitive.</summary>
    /// <value>The autoplay parameters as JSON.</value>
    public string AutoplayParametersJson { get; set; } = "{}";
    /// <summary>Archetype of the directly controlled actor; empty when nobody is controlled directly.</summary>
    /// <value>The player archetype key.</value>
    public string PlayerArchetypeKey { get; set; } = string.Empty;
    /// <summary>Game key of a seeded game whose rules this profile uses while it has no rule rows of its own.</summary>
    /// <value>The ruleset template game key; empty when the profile defines its own rules.</value>
    public string RulesetTemplateKey { get; set; } = string.Empty;
    /// <summary>Defines the Council team associated with future sessions when no launch override is supplied.</summary>
    /// <value>The desired team key; an empty value uses the maintained Game-project playtest team.</value>
    public string DefaultTeamKey { get; set; } = string.Empty;
    /// <summary>Defines the controller ownership applied when a session starts from a later build.</summary>
    /// <value>The Human, Shared, or AI default control mode.</value>
    public CouncilGameControlMode DefaultControlMode { get; set; } = CouncilGameControlMode.Shared;
    /// <summary>Requests the deterministic or Council-model-preferred GameDirector strategy.</summary>
    /// <value>The desired director mode.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>Requests the model name available to the model-preferred GameDirector path.</summary>
    /// <value>The bounded provider model name.</value>
    public string GameDirectorModelName { get; set; } = "qwen3.5:0.8b";
    /// <summary>Defines how many creature subdirectors the runtime may coordinate for this authored game.</summary>
    /// <value>A value clamped to one through eight when the profile is saved.</value>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>Requests the delay between autonomous AI control steps when AI control is active.</summary>
    /// <value>A delay clamped to 250 through 10,000 milliseconds.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>Defines the ASCII/display cell width copied into later compiled definitions.</summary>
    /// <value>A width clamped to 20 through 240 cells.</value>
    public int FrameWidth { get; set; } = 80;
    /// <summary>Defines the ASCII/display cell height copied into later compiled definitions.</summary>
    /// <value>A height clamped to 8 through 100 cells.</value>
    public int FrameHeight { get; set; } = 25;
    /// <summary>Selects terminal-default, ANSI-16, or indexed-256 presentation for later builds.</summary>
    /// <value>The ascii color mode value exposed by <see cref="SaveGameProjectProfileRequest"/>.</value>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Requests the default foreground palette index.</summary>
    /// <value>The default foreground color value exposed by <see cref="SaveGameProjectProfileRequest"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Requests the default background palette index.</summary>
    /// <value>The default background color value exposed by <see cref="SaveGameProjectProfileRequest"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Optionally requests a deterministic map seed for runtime profiles that use one.</summary>
    /// <value>A positive deterministic seed, or <c>null</c> to let the runtime derive one.</value>
    public int? MapSeed { get; set; }
    /// <summary>Supplies the bounded authored scenario/world description that later builds copy into the definition.</summary>
    /// <value>The scenario text normalized and bounded by the Project-owned profile service.</value>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Records the explicit approval required before the Project system persists these editable game-authoring settings.</summary>
    /// <value><see langword="true"/> only for an approved authoring-profile save.</value>
    public bool UserConfirmed { get; set; }
}
