using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Defines how LocalGPT assigns AI participants to a Council role.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRoleAiSelectionMode
{
    /// <summary>Assigns every selected model to the role.</summary>
    AllSelected,
    /// <summary>Assigns a bounded random participant count within the role limits.</summary>
    RandomRange
}

/// <summary>Defines how a human participant may join a Council role.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HumanParticipationMode
{
    /// <summary>Disables human assignment to the role.</summary>
    None,
    /// <summary>Allows but does not require a human participant.</summary>
    Optional,
    /// <summary>Requires a human participant before dependent rounds continue.</summary>
    Required,
    /// <summary>Restricts the role to a human participant.</summary>
    HumanOnly
}

/// <summary>Defines whether a role behaves as a task specialist or an improvisation player.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRolePerformanceMode
{
    /// <summary>Executes bounded project or analysis work.</summary>
    TaskSpecialist,
    /// <summary>Participates as a fictional or game runtime actor.</summary>
    ImprovisationPlayer
}

/// <summary>Defines the language policy applied to a Council role.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRoleLanguageMode
{
    /// <summary>Lets the assigned model choose a language.</summary>
    ModelChoice,
    /// <summary>Uses the current sender's language.</summary>
    SenderLanguage,
    /// <summary>Forces English output.</summary>
    English
}

/// <summary>Defines how strictly a Council role must remain inside its assigned responsibility.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRoleBoundaryMode
{
    /// <summary>Uses normal bounded project-role limits.</summary>
    Bounded,
    /// <summary>Allows cooperative overlap with adjacent roles.</summary>
    Collaborative,
    /// <summary>Requires strict role separation.</summary>
    Strict
}

/// <summary>Defines one editable LocalGPT Council team, its roles, workflow and architecture contracts.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class OrganicCouncilTeamDefinition
{
    /// <summary>Gets or sets the stable lowercase team key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the human-readable team name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded purpose of the team.</summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>Gets or sets the role definitions used by the team.</summary>
    public List<OrganicCouncilRoleDefinition> Roles { get; set; } = [];
    /// <summary>Gets or sets preferred registered DXFunction and organic capability keys.</summary>
    public List<string> PreferredCapabilities { get; set; } = [];
    /// <summary>Gets or sets architecture and safety contracts that every round must preserve.</summary>
    public List<string> ArchitectureContracts { get; set; } = [];
    /// <summary>Gets or sets the literal ordered Council workflow.</summary>
    public List<CouncilWorkflowStepDefinition> WorkflowSteps { get; set; } = [];
    /// <summary>Gets or sets the expert-preparation prompt used by the built-in workflow.</summary>
    public string ExpertPreparationPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the leader-synthesis prompt used by the built-in workflow.</summary>
    public string LeaderSynthesisPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the common main-round instruction.</summary>
    public string MainRoundInstructionTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the team may be selected for new runs.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the row originated from maintained seed data.</summary>
    public bool IsSystemSeed { get; set; }
    /// <summary>Gets or sets whether a user edited the seed-owned row.</summary>
    public bool IsUserModified { get; set; }
}

/// <summary>Defines one role and its AI, human, language, runtime-class and pairing policies.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class OrganicCouncilRoleDefinition
{
    /// <summary>Gets or sets the unique role name within the team.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>Gets or sets the expertise expected from participants assigned to the role.</summary>
    public string Expertise { get; set; } = string.Empty;
    /// <summary>Gets or sets the responsibility and ownership boundary of the role.</summary>
    public string Responsibility { get; set; } = string.Empty;
    /// <summary>Gets or sets how AI participants are selected for the role.</summary>
    public CouncilRoleAiSelectionMode AiSelectionMode { get; set; } = CouncilRoleAiSelectionMode.AllSelected;
    /// <summary>Gets or sets the minimum AI participant count.</summary>
    public int MinimumAiParticipants { get; set; } = 1;
    /// <summary>Gets or sets the maximum AI participant count.</summary>
    public int MaximumAiParticipants { get; set; } = 1;
    /// <summary>Gets or sets the human participation policy.</summary>
    public HumanParticipationMode HumanParticipationMode { get; set; } = HumanParticipationMode.None;
    /// <summary>Gets or sets the task-specialist or improvisation behavior.</summary>
    public CouncilRolePerformanceMode PerformanceMode { get; set; } = CouncilRolePerformanceMode.TaskSpecialist;
    /// <summary>Gets or sets the role language policy.</summary>
    public CouncilRoleLanguageMode LanguageMode { get; set; } = CouncilRoleLanguageMode.ModelChoice;
    /// <summary>Gets or sets the role-boundary strictness.</summary>
    public CouncilRoleBoundaryMode BoundaryMode { get; set; } = CouncilRoleBoundaryMode.Bounded;
    /// <summary>Gets or sets the assignment group that requires distinct model identities.</summary>
    public string DistinctAiAssignmentGroup { get; set; } = string.Empty;
    /// <summary>Gets or sets another role whose participant count this role mirrors.</summary>
    public string MatchAiParticipantCountToRole { get; set; } = string.Empty;
    /// <summary>Gets or sets the role paired one-to-one with this role.</summary>
    public string PairedRole { get; set; } = string.Empty;
    /// <summary>Gets or sets runtime-class keys exposed to participants in this role.</summary>
    public List<string> RuntimeClassKeys { get; set; } = [];
}

/// <summary>Stores project and revision metadata used to route organic Council capabilities.</summary>
[DocumentationUpdated("2.1.20")]
public class ProjectOrganicContext
{
    /// <summary>Gets or sets the owning project identifier.</summary>
    public Guid ProjectId { get; set; }
    /// <summary>Gets or sets the optional project revision identifier.</summary>
    public Guid? RevisionId { get; set; }
    /// <summary>Gets or sets whether an installer is known to exist.</summary>
    public bool? HasInstaller { get; set; }
    /// <summary>Gets or sets the recorded installer path.</summary>
    public string InstallerPath { get; set; } = string.Empty;
    /// <summary>Gets or sets known compiler or tool names.</summary>
    public List<string> Compilers { get; set; } = [];
    /// <summary>Gets or sets bounded system-command descriptions.</summary>
    public List<string> SystemCommands { get; set; } = [];
    /// <summary>Gets or sets knowledge references relevant to the project.</summary>
    public List<string> KnowledgeReferences { get; set; } = [];
    /// <summary>Gets or sets project-level regex patterns.</summary>
    public List<string> ProjectRegexPatterns { get; set; } = [];
    /// <summary>Gets or sets file-level regex patterns.</summary>
    public List<string> FileRegexPatterns { get; set; } = [];
    /// <summary>Gets or sets approved debug and diagnostic paths.</summary>
    public List<string> DebugPaths { get; set; } = [];
    /// <summary>Gets or sets the latest known build result.</summary>
    public bool? BuildSuccessful { get; set; }
    /// <summary>Gets or sets the most recent Council activity time.</summary>
    public DateTimeOffset? LastCouncilActivityUtc { get; set; }
    /// <summary>Gets or sets required organic capability keys.</summary>
    public List<string> RequiredOrganicCapabilities { get; set; } = [];
    /// <summary>Gets or sets external organic plugin identifiers.</summary>
    public List<string> ExternalOrganPlugins { get; set; } = [];
}

/// <summary>Extends project organic context with the confirmation required for persistence.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class SaveProjectOrganicContextRequest : ProjectOrganicContext
{
    /// <summary>Gets or sets whether the current user confirmed the exact change.</summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>Defines one ordered Council round and its execution, loop, function and ASCII-frame policies.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class CouncilWorkflowStepDefinition
{
    /// <summary>Gets or sets the stable step key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the visible step name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the primary ordering value.</summary>
    public int SortOrder { get; set; }
    /// <summary>Gets or sets the workflow phase label.</summary>
    public string Phase { get; set; } = string.Empty;
    /// <summary>Gets or sets the role assigned to the step.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>Gets or sets the literal prompt template.</summary>
    public string PromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the supported execution-mode name.</summary>
    public string ExecutionMode { get; set; } = "AllMembersParallel";
    /// <summary>Gets or sets the explicit model used by assigned-model execution.</summary>
    public string AssignedModelName { get; set; } = string.Empty;
    /// <summary>Gets or sets how many times the step is expanded outside a loop group.</summary>
    public int RepeatCount { get; set; } = 1;
    /// <summary>Gets or sets whether prior step output is included in the prompt.</summary>
    public bool IncludePriorTranscript { get; set; } = true;
    /// <summary>Gets or sets whether the step produces the visible final answer.</summary>
    public bool ProducesFinalAnswer { get; set; }
    /// <summary>Gets or sets whether the built-in orchestration behavior is used.</summary>
    public bool UseBuiltInBehavior { get; set; }
    /// <summary>Gets or sets the loop-group key shared by consecutive steps.</summary>
    public string LoopGroup { get; set; } = string.Empty;
    /// <summary>Gets or sets the maximum loop iterations.</summary>
    public int MaximumLoopIterations { get; set; } = 1;
    /// <summary>Gets or sets the transcript marker that completes the loop.</summary>
    public string LoopCompletionMarker { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the step is active.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the step pauses for a human checkpoint.</summary>
    public bool RequiresHumanCheckpoint { get; set; }
    /// <summary>Gets or sets whether registered organic/DX functions may be requested.</summary>
    public bool CanUseOrganicFunctions { get; set; } = true;
    /// <summary>Gets or sets whether the step owns a complete ASCII frame.</summary>
    public bool ProducesAsciiFrame { get; set; }
    /// <summary>Gets or sets the requested ASCII frame width.</summary>
    public int AsciiFrameWidth { get; set; } = 80;
    /// <summary>Gets or sets the requested ASCII frame height.</summary>
    public int AsciiFrameHeight { get; set; } = 25;
    /// <summary>Gets or sets the logical world-step scale represented by one frame.</summary>
    public int WorldStepScale { get; set; } = 1;
}

/// <summary>Represents the persisted SQLite row for one Council team configuration.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class CouncilTeamConfiguration
{
    /// <summary>Gets or sets the row identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the stable team key.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the team display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the team purpose.</summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>Gets or sets serialized role definitions.</summary>
    public string RolesJson { get; set; } = "[]";
    /// <summary>Gets or sets serialized preferred capabilities.</summary>
    public string PreferredCapabilitiesJson { get; set; } = "[]";
    /// <summary>Gets or sets serialized architecture contracts.</summary>
    public string ArchitectureContractsJson { get; set; } = "[]";
    /// <summary>Gets or sets serialized workflow steps.</summary>
    public string WorkflowStepsJson { get; set; } = "[]";
    /// <summary>Gets or sets the expert-preparation prompt.</summary>
    public string ExpertPreparationPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the leader-synthesis prompt.</summary>
    public string LeaderSynthesisPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the common main-round instruction.</summary>
    public string MainRoundInstructionTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the maintained seed schema version.</summary>
    public int SeedVersion { get; set; } = 1;
    /// <summary>Gets or sets whether the row is maintained seed data.</summary>
    public bool IsSystemSeed { get; set; } = true;
    /// <summary>Gets or sets whether a user changed the row.</summary>
    public bool IsUserModified { get; set; }
    /// <summary>Gets or sets whether the team can be selected.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets the UTC creation time.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC update time.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Carries a reviewed Council team change and its persistence flags.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class SaveCouncilTeamConfigurationRequest
{
    /// <summary>Gets or sets the team definition to save.</summary>
    public OrganicCouncilTeamDefinition Team { get; set; } = new();
    /// <summary>Gets or sets whether the saved team remains enabled.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the current user confirmed the exact change.</summary>
    public bool UserConfirmed { get; set; }
}
