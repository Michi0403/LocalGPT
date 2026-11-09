using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Captures the serializable runtime definition produced by the Project system after a Game project is explicitly built.
/// GameDirector consumes this build output but does not own the project authoring data that produced it.
/// </summary>
public sealed class ProjectGameDefinition
{
    /// <summary>Curated scene from the last explicitly completed project revision, if that revision contains a Game Studio world.</summary>
    /// <value>The scene value exposed by <see cref="ProjectGameDefinition"/>.</value>
    public GameSceneDocument? Scene { get; set; }
    /// <summary>Identifies the durable LocalGPT project that owns this compiled game definition.</summary>
    /// <value>The non-empty project identifier captured by the build.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Captures the project's semantic/version label at the moment the definition was built.</summary>
    /// <value>The project version associated with this game build.</value>
    public string ProjectVersion { get; set; } = string.Empty;
    /// <summary>Identifies the persisted authoring profile compiled by this build.</summary>
    /// <value>The source game-project profile identifier.</value>
    public Guid AuthoringProfileId { get; set; }
    /// <summary>Identifies the current project revision captured by the build when one exists.</summary>
    /// <value>The source revision identifier, or <c>null</c> when no current revision exists.</value>
    public Guid? SourceRevisionId { get; set; }
    /// <summary>Lists the durable project requirements that formed the build's requirement baseline.</summary>
    /// <value>The requirement identifiers visible to the project build at compile time.</value>
    public List<Guid> SourceRequirementIds { get; set; } = [];
    /// <summary>Provides the stable runtime identity used in session snapshots, logs, and game-tool correlation.</summary>
    /// <value>A normalized project game key independent of the selected runtime profile.</value>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>Provides the player-facing title rendered by the game surface and runtime status views.</summary>
    /// <value>The bounded display title compiled from project settings.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>The declarative rules compiled from the profile: engine primitives with parameters, stats, archetypes, actions, rules, levels, texts and aliases.</summary>
    /// <value>The compiled ruleset the generic engine runs.</value>
    public LocalGPT.BusinessObjects.GameEngine.GameRuleset Ruleset { get; set; } = new();
    /// <summary>Associates the game build with the default Council team used when no launch-time team override exists.</summary>
    /// <value>The bounded Council team key stored in the build.</value>
    public string DefaultTeamKey { get; set; } = string.Empty;
    /// <summary>Defines who owns direct controls when a runtime session is created from this build.</summary>
    /// <value>The default Human, Shared, or AI control mode.</value>
    public CouncilGameControlMode DefaultControlMode { get; set; } = CouncilGameControlMode.Shared;
    /// <summary>Selects whether GameDirector remains deterministic or may prefer a configured Council model.</summary>
    /// <value>The director strategy compiled into the project build.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; } = CouncilGameDirectorMode.Deterministic;
    /// <summary>Names the optional model requested when the build uses the model-preferred director strategy.</summary>
    /// <value>The bounded provider model name, or an empty string when no model is requested.</value>
    public string GameDirectorModelName { get; set; } = string.Empty;
    /// <summary>Defines the bounded number of creature subdirectors requested for runtime coordination.</summary>
    /// <value>A value from one through eight.</value>
    public int CreatureDirectorCount { get; set; } = 2;
    /// <summary>Defines the default delay between autonomous AI control proposals when autoplay is enabled.</summary>
    /// <value>The bounded delay in milliseconds.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
    /// <summary>Defines the terminal-cell width requested by this project build.</summary>
    /// <value>A bounded display width from 20 through 240 cells.</value>
    public int FrameWidth { get; set; } = 80;
    /// <summary>Defines the terminal-cell height requested by this project build.</summary>
    /// <value>A bounded display height from 8 through 100 cells.</value>
    public int FrameHeight { get; set; } = 25;
    /// <summary>Selects the shared ASCII presentation palette compiled into this project build.</summary>
    /// <value>The ascii color mode value exposed by <see cref="ProjectGameDefinition"/>.</value>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Defines the default foreground palette index for explicit palette modes.</summary>
    /// <value>The default foreground color value exposed by <see cref="ProjectGameDefinition"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Defines the default background palette index for explicit palette modes.</summary>
    /// <value>The default background color value exposed by <see cref="ProjectGameDefinition"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Provides a deterministic map seed for compatible runtime profiles, with zero requesting runtime derivation.</summary>
    /// <value>The positive project-selected seed or zero.</value>
    public int MapSeed { get; set; }
    /// <summary>Supplies the bounded world/scenario description compiled from project authoring data.</summary>
    /// <value>At most 240 normalized characters of scenario context.</value>
    public string ScenarioPrompt { get; set; } = string.Empty;
    /// <summary>Records when the Project system produced this specific compiled definition.</summary>
    /// <value>The UTC build timestamp.</value>
    public DateTime BuiltAtUtc { get; set; } = DateTime.UtcNow;
}
