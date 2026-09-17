using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Selects the lower game-runtime rules/rendering contract consumed by a project build without making a project name or game key an engine switch.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameRuntimeProfile
{
    /// <summary>Uses deterministic map movement, combat, extraction, ray-cast preview, and shared controller semantics.</summary>
    Corridor,
    /// <summary>Uses the bounded story/choice simulation while retaining the shared controller, session, and display contracts.</summary>
    Story
}

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
    /// <summary>Selects the lower runtime profile targeted by the project build.</summary>
    /// <value>The corridor or story runtime profile.</value>
    public CouncilGameRuntimeProfile RuntimeProfile { get; set; } = CouncilGameRuntimeProfile.Corridor;
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
    /// <summary>Stores a deterministic map seed when project authoring requests one.</summary>
    /// <value>The positive map seed or zero for runtime derivation.</value>
    public int MapSeed { get; set; }
    /// <summary>Stores the bounded scenario/world prompt authored for this game project.</summary>
    /// <value>The project-owned scenario text.</value>
    [MaxLength(240)] public string ScenarioPrompt { get; set; } = string.Empty;
}

/// <summary>
/// Captures the serializable runtime definition produced by the Project system after a Game project is explicitly built.
/// GameDirector consumes this build output but does not own the project authoring data that produced it.
/// </summary>
public sealed class ProjectGameDefinition
{
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
    /// <summary>Selects the lower simulation/rendering contract that interprets this build.</summary>
    /// <value>The compiled corridor or story runtime profile.</value>
    public CouncilGameRuntimeProfile RuntimeProfile { get; set; } = CouncilGameRuntimeProfile.Corridor;
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
    /// <summary>Selects the lower runtime profile targeted when the saved design is later compiled.</summary>
    /// <value>The requested corridor or story profile.</value>
    public CouncilGameRuntimeProfile RuntimeProfile { get; set; } = CouncilGameRuntimeProfile.Corridor;
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

/// <summary>
/// Requests compilation of the already-persisted Game-project design baseline into a runtime definition artifact.
/// Build requests deliberately carry no editable game design fields so compiling cannot silently mutate project authoring state.
/// </summary>
public sealed class BuildProjectGameRequest
{
    /// <summary>Records the explicit approval required before the Project system creates or replaces the durable GameBuild artifact.</summary>
    /// <value><see langword="true"/> only for an approved build invocation.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Reports the durable project artifact and runtime definition created by a successful Game-project build.
/// </summary>
public sealed class ProjectGameBuildResult
{
    /// <summary>Identifies the project artifact that persists the compiled runtime definition.</summary>
    /// <value>The durable `GameBuild / Runtime Definition` artifact identifier.</value>
    public Guid ArtifactId { get; set; }
    /// <summary>Identifies the current project revision associated with the build when one was available.</summary>
    /// <value>The revision identifier, or <c>null</c> for a build without a current revision.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>Provides the compiled definition that GameDirector can consume.</summary>
    /// <value>The validated project-owned runtime definition.</value>
    public required ProjectGameDefinition Definition { get; set; }
}

/// <summary>
/// Supplies chat/Council ownership metadata when an already-built Game project is handed to the runtime.
/// </summary>
public sealed class LaunchProjectGameRequest
{
    /// <summary>Associates the new runtime session with a saved/current chat when available.</summary>
    /// <value>The owning conversation identifier, or <c>null</c> for an unbound launch.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>Associates the new runtime session with the active Council run when applicable.</summary>
    /// <value>The owning Council run identifier, or <c>null</c> for a standalone launch.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>Provides the bounded human-readable actor label recorded as the launch initiator.</summary>
    /// <value>The launch initiator label.</value>
    public string StartedBy { get; set; } = "Human User";
}

/// <summary>Wraps the project identifier used by the read-only project.game.profile.get DX function.</summary>
public sealed class ProjectGameProfileGetParameters
{
    /// <summary>Identifies the Game project whose editable authoring profile should be read.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
}

/// <summary>Wraps the project identifier and approved authoring settings for the project.game.profile.save DX function.</summary>
public sealed class ProjectGameProfileSaveParameters
{
    /// <summary>Identifies the Game project whose authoring profile should be saved.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Provides the authoring settings submitted through the approval-gated AI function.</summary>
    /// <value>The Game-project authoring request.</value>
    public SaveGameProjectProfileRequest Request { get; set; } = new();
}

/// <summary>Wraps the project identifier and approved build request for the project.game.build DX function.</summary>
public sealed class ProjectGameBuildParameters
{
    /// <summary>Identifies the Game project to compile.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Provides the build settings submitted through the approval-gated AI function.</summary>
    /// <value>The Game-project build request.</value>
    public BuildProjectGameRequest Request { get; set; } = new();
}

/// <summary>Wraps the project identifier used by the read-only project.game.get DX function.</summary>
public sealed class ProjectGameGetParameters
{
    /// <summary>Identifies the Game project whose latest approved build should be read.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
}

/// <summary>Wraps the project identifier used by the coordination-only project.game.start DX function.</summary>
public sealed class ProjectGameStartParameters
{
    /// <summary>Identifies the Game project whose latest approved build should be launched.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
}
