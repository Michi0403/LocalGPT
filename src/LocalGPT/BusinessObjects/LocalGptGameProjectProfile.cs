using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Persists the editable game-authoring profile owned by one LocalGPT Game project. Runtime services never own this row;
/// they receive only a compiled <see cref="ProjectGameDefinition"/> after the Project system builds it.
/// </summary>
public sealed class LocalGptGameProjectProfile
{
    /// <summary>Identifies this durable authoring-profile row.</summary>
    /// <value>The stable profile identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Identifies the LocalGPT project that owns this game-authoring profile.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Navigates to the project that owns the authoring profile.</summary>
    /// <value>The owning LocalGPT project when loaded.</value>
    public LocalGptProject? Project { get; set; }
    /// <summary>Records when the authoring profile was first created.</summary>
    /// <value>The UTC creation timestamp.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Records when the authoring profile was most recently changed.</summary>
    /// <value>The UTC update timestamp.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Stores the stable project-owned runtime identity requested for built sessions.</summary>
    /// <value>The normalized game key.</value>
    [MaxLength(160)] public string GameKey { get; set; } = string.Empty;
    /// <summary>Stores the player-facing title authored for this project game.</summary>
    /// <value>The bounded game title.</value>
    [MaxLength(240)] public string DisplayName { get; set; } = string.Empty;
    /// <summary>Renderer primitive that draws the game (for example first-person-raycast or text-template).</summary>
    /// <value>The view primitive key.</value>
    [MaxLength(80)] public string ViewKey { get; set; } = "first-person-raycast";
    /// <summary>JSON parameters of the view primitive.</summary>
    /// <value>The view parameters as JSON.</value>
    public string ViewParametersJson { get; set; } = "{}";
    /// <summary>How turns advance: actions (each control is a turn) or council (council evidence drives the game).</summary>
    /// <value>The turn model key.</value>
    [MaxLength(40)] public string TurnModelKey { get; set; } = "actions";
    /// <summary>Map generator primitive; empty when the game has no grid map.</summary>
    /// <value>The map generator key.</value>
    [MaxLength(80)] public string MapGeneratorKey { get; set; } = string.Empty;
    /// <summary>JSON parameters of the map generator.</summary>
    /// <value>The map parameters as JSON.</value>
    public string MapParametersJson { get; set; } = "{}";
    /// <summary>Autoplay primitive used for AI control; empty for none.</summary>
    /// <value>The autoplay primitive key.</value>
    [MaxLength(80)] public string AutoplayKey { get; set; } = string.Empty;
    /// <summary>JSON parameters of the autoplay primitive.</summary>
    /// <value>The autoplay parameters as JSON.</value>
    public string AutoplayParametersJson { get; set; } = "{}";
    /// <summary>Archetype of the directly controlled actor; empty when nobody is controlled directly.</summary>
    /// <value>The player archetype key.</value>
    [MaxLength(80)] public string PlayerArchetypeKey { get; set; } = string.Empty;
    /// <summary>Game key of a seeded game whose rules this profile uses while it has no rule rows of its own.</summary>
    /// <value>The ruleset template game key; empty when the profile defines its own rules.</value>
    [MaxLength(160)] public string RulesetTemplateKey { get; set; } = string.Empty;
    /// <summary>Stores the default Council team used when a launch does not override team ownership.</summary>
    /// <value>The default Council team key.</value>
    [MaxLength(160)] public string DefaultTeamKey { get; set; } = string.Empty;
    /// <summary>Stores the default human/AI control ownership selected by project authoring.</summary>
    /// <value>The default game control mode.</value>
    public CouncilGameControlMode DefaultControlMode { get; set; } = CouncilGameControlMode.Shared;
    /// <summary>Determines whether sessions compiled from this project remain deterministic or may delegate director decisions to a configured Council model.</summary>
    /// <value>The deterministic or Council-model-preferred director mode.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>Stores the optional provider model name preferred by the GameDirector.</summary>
    /// <value>The bounded provider model name.</value>
    [MaxLength(240)] public string GameDirectorModelName { get; set; } = string.Empty;
    /// <summary>Stores the bounded creature-subdirector count requested by the game project.</summary>
    /// <value>A value from one through eight.</value>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>Stores the default autonomous-control pacing requested by the project.</summary>
    /// <value>The autoplay delay in milliseconds.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>Stores the authored terminal/display width for the game project.</summary>
    /// <value>The frame width in character cells.</value>
    public int FrameWidth { get; set; } = 80;
    /// <summary>Stores the authored terminal/display height for the game project.</summary>
    /// <value>The frame height in character cells.</value>
    public int FrameHeight { get; set; } = 25;
    /// <summary>Selects the shared ASCII presentation palette used by built sessions.</summary>
    /// <value>The ascii color mode value exposed by <see cref="LocalGptGameProjectProfile"/>.</value>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Stores the default foreground palette index for explicit palette modes.</summary>
    /// <value>The default foreground color value exposed by <see cref="LocalGptGameProjectProfile"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Stores the default background palette index for explicit palette modes.</summary>
    /// <value>The default background color value exposed by <see cref="LocalGptGameProjectProfile"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Stores a deterministic map seed when project authoring requests one.</summary>
    /// <value>The positive map seed or zero for runtime derivation.</value>
    public int MapSeed { get; set; }
    /// <summary>Stores the bounded scenario/world prompt authored for this game project.</summary>
    /// <value>The project-owned scenario text.</value>
    [MaxLength(240)] public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Whether the profile was created by the LocalGPT seed (a shipped game) rather than by a user.</summary>
    /// <value><see langword="true"/> for seeded game projects.</value>
    public bool IsSystemSeed { get; set; }
    /// <summary>Whether a user changed a seeded profile; the seed then no longer overwrites it.</summary>
    /// <value><see langword="true"/> after a user edit of a seeded profile.</value>
    public bool IsUserModified { get; set; }
    /// <summary>Seed version that last wrote the profile and its rule rows.</summary>
    /// <value>The seed version; 0 for user-created profiles.</value>
    public int SeedVersion { get; set; }
}
