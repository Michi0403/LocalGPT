using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Defines how LocalGPT assigns AI participants to a Council role.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRoleAiSelectionMode
{
    /// <summary>Assigns every selected model to the role.</summary>
    AllSelected,
    /// <summary>Assigns a bounded random participant count within the role limits.</summary>
    RandomRange,
    /// <summary>Assigns only the exact provider-qualified models saved on the role.</summary>
    AssignedModels,
    /// <summary>Chooses a deterministic-random count from the exact provider-qualified role pool, cycling the pool fairly when the requested count exceeds the number of distinct saved models.</summary>
    AssignedModelsRandomRange
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

/// <summary>Defines which prior Council output a configured workflow step may receive.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilTranscriptVisibilityMode
{
    /// <summary>Shares the complete Council transcript accumulated so far.</summary>
    FullCouncil,
    /// <summary>Shares only prior output produced for the same configured role.</summary>
    SameRole,
    /// <summary>Shares only output from the same logical Council round.</summary>
    CurrentRound,
    /// <summary>Shares only same-role output from the same logical Council round.</summary>
    SameRoleCurrentRound,
    /// <summary>Shares no accumulated transcript. The separately tracked previous step may still be used.</summary>
    None
}

/// <summary>Defines one editable LocalGPT Council team, its roles, workflow and architecture contracts.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class OrganicCouncilTeamDefinition
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this organic council team definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the organic council team definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the organic council team definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the roles collection maintained or exposed by this organic council team definition instance for downstream processing.
    /// </summary>
    /// <value>The roles value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public List<OrganicCouncilRoleDefinition> Roles { get; set; } = [];
    /// <summary>
    /// Gets or sets the preferred capabilities collection maintained or exposed by this organic council team definition instance for downstream processing.
    /// </summary>
    /// <value>The preferred capabilities value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public List<string> PreferredCapabilities { get; set; } = [];
    /// <summary>Gets or sets architecture and safety contracts that every round must preserve.</summary>
    /// <value>The architecture contracts value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public List<string> ArchitectureContracts { get; set; } = [];
    /// <summary>Gets or sets the literal ordered Council workflow.</summary>
    /// <value>The workflow steps value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public List<CouncilWorkflowStepDefinition> WorkflowSteps { get; set; } = [];
    /// <summary>Gets or sets the expert-preparation prompt used by the built-in workflow.</summary>
    /// <value>The expert preparation prompt template value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public string ExpertPreparationPromptTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets the leader-synthesis prompt used by the built-in workflow.</summary>
    /// <value>The leader synthesis prompt template value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public string LeaderSynthesisPromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the main round instruction template value that forms part of the organic council team definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The main round instruction template value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public string MainRoundInstructionTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the team may be selected for new runs.</summary>
    /// <value>The is enabled value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the row originated from maintained seed data.</summary>
    /// <value>The is system seed value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public bool IsSystemSeed { get; set; }
    /// <summary>Gets or sets whether a user edited the seed-owned row.</summary>
    /// <value>The is user modified value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public bool IsUserModified { get; set; }
}

/// <summary>Defines one role and its AI, human, language, runtime-class and pairing policies.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class OrganicCouncilRoleDefinition
{
    /// <summary>
    /// Gets or sets the role value that forms part of the organic council role definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public string Role { get; set; } = string.Empty;
    /// <summary>Gets or sets the expertise expected from participants assigned to the role.</summary>
    /// <value>The expertise value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public string Expertise { get; set; } = string.Empty;
    /// <summary>Gets or sets the responsibility and ownership boundary of the role.</summary>
    /// <value>The responsibility value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public string Responsibility { get; set; } = string.Empty;
    /// <summary>Gets or sets how AI participants are selected for the role.</summary>
    /// <value>The AI selection mode value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public CouncilRoleAiSelectionMode AiSelectionMode { get; set; } = CouncilRoleAiSelectionMode.AllSelected;
    /// <summary>
    /// Gets or sets the lower participant/invocation count used by random role assignment. In provider-bound pool mode this is a number of role invocations, not a limit on how many distinct models may be saved.
    /// </summary>
    /// <value>The inclusive lower count used when resolving participants for one Council run.</value>
    public int MinimumAiParticipants { get; set; } = 1;
    /// <summary>
    /// Gets or sets the upper participant/invocation count used by random role assignment. Provider-bound pool mode may intentionally exceed the number of distinct saved models and then repeats models in shuffled cycles.
    /// </summary>
    /// <value>The inclusive upper count used when resolving participants for one Council run.</value>
    public int MaximumAiParticipants { get; set; } = 1;
    /// <summary>
    /// Gets or sets the human participation mode value that forms part of the organic council role definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human participation mode value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public HumanParticipationMode HumanParticipationMode { get; set; } = HumanParticipationMode.None;
    /// <summary>
    /// Gets or sets the performance mode value that forms part of the organic council role definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The performance mode value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public CouncilRolePerformanceMode PerformanceMode { get; set; } = CouncilRolePerformanceMode.TaskSpecialist;
    /// <summary>
    /// Gets or sets the language mode value that forms part of the organic council role definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The language mode value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public CouncilRoleLanguageMode LanguageMode { get; set; } = CouncilRoleLanguageMode.ModelChoice;
    /// <summary>
    /// Gets or sets the boundary mode value that forms part of the organic council role definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The boundary mode value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public CouncilRoleBoundaryMode BoundaryMode { get; set; } = CouncilRoleBoundaryMode.Bounded;
    /// <summary>Gets or sets the assignment group that requires distinct model identities.</summary>
    /// <value>The distinct AI assignment group value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public string DistinctAiAssignmentGroup { get; set; } = string.Empty;
    /// <summary>Gets or sets another role whose participant count this role mirrors.</summary>
    /// <value>The match AI participant count to role value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public string MatchAiParticipantCountToRole { get; set; } = string.Empty;
    /// <summary>Gets or sets the role paired one-to-one with this role.</summary>
    /// <value>The paired role value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public string PairedRole { get; set; } = string.Empty;
    /// <summary>Gets or sets runtime-class keys exposed to participants in this role.</summary>
    /// <value>The runtime class keys value exposed by <see cref="OrganicCouncilRoleDefinition"/>.</value>
    public List<string> RuntimeClassKeys { get; set; } = [];
    /// <summary>Gets or sets the exact provider-qualified identities eligible for this role. Exact-assignment mode runs the saved set; provider-pool random mode samples only this set and may deliberately reuse members when the configured invocation count exceeds its distinct size.</summary>
    /// <value>The provider, endpoint and model selection keys that form the authoritative role pool.</value>
    public List<string> AssignedModelKeys { get; set; } = [];
}

/// <summary>Stores project and revision metadata used to route organic Council capabilities.</summary>
[DocumentationUpdated("2.1.20")]
public class ProjectOrganicContext
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project organic context instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project organic context instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether installer applies to the project organic context state.
    /// </summary>
    /// <value>The has installer value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public bool? HasInstaller { get; set; }
    /// <summary>
    /// Gets or sets the installer path used by this project organic context instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The installer path value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public string InstallerPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the compilers collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The compilers value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> Compilers { get; set; } = [];
    /// <summary>
    /// Gets or sets the system commands collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The system commands value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> SystemCommands { get; set; } = [];
    /// <summary>
    /// Gets or sets the knowledge references collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The knowledge references value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> KnowledgeReferences { get; set; } = [];
    /// <summary>
    /// Gets or sets the project regex patterns collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The project regex patterns value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> ProjectRegexPatterns { get; set; } = [];
    /// <summary>
    /// Gets or sets the file regex patterns collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The file regex patterns value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> FileRegexPatterns { get; set; } = [];
    /// <summary>
    /// Gets or sets the debug paths used by this project organic context instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The debug paths value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> DebugPaths { get; set; } = [];
    /// <summary>
    /// Gets or sets the build successful value that forms part of the project organic context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The build successful value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public bool? BuildSuccessful { get; set; }
    /// <summary>
    /// Gets or sets the last council activity UTC associated with this project organic context state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last council activity UTC value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public DateTimeOffset? LastCouncilActivityUtc { get; set; }
    /// <summary>
    /// Gets or sets the required organic capabilities collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The required organic capabilities value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> RequiredOrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the external organ plugins collection maintained or exposed by this project organic context instance for downstream processing.
    /// </summary>
    /// <value>The external organ plugins value exposed by <see cref="ProjectOrganicContext"/>.</value>
    public List<string> ExternalOrganPlugins { get; set; } = [];
}

/// <summary>Extends project organic context with the confirmation required for persistence.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class SaveProjectOrganicContextRequest : ProjectOrganicContext
{
    /// <summary>Gets or sets whether the current user confirmed the exact change.</summary>
    /// <value>The user confirmed value exposed by <see cref="SaveProjectOrganicContextRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Defines one ordered Council round and its execution, loop, function and ASCII-frame policies.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class CouncilWorkflowStepDefinition
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this council workflow step definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the sort order value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sort order value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int SortOrder { get; set; }
    /// <summary>Gets or sets the workflow phase label.</summary>
    /// <value>The phase value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string Phase { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the role value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string Role { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt template value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt template value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string PromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the execution mode value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The execution mode value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string ExecutionMode { get; set; } = "AllMembersSequentialOnEachAIHostParallel";
    /// <summary>Gets or sets the exact provider-qualified model used by assigned-model execution.</summary>
    /// <value>The assigned model name value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string AssignedModelName { get; set; } = string.Empty;
    /// <summary>Gets or sets the logical one-based Council round. Zero keeps automatic sequential round numbering.</summary>
    /// <value>The logical round number value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int LogicalRoundNumber { get; set; }
    /// <summary>Gets or sets which prior Council output is visible to this step.</summary>
    /// <value>The transcript visibility value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public CouncilTranscriptVisibilityMode TranscriptVisibility { get; set; } = CouncilTranscriptVisibilityMode.FullCouncil;
    /// <summary>Gets or sets how many times the step is expanded outside a loop group.</summary>
    /// <value>The repeat count value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int RepeatCount { get; set; } = 1;
    /// <summary>Gets or sets whether prior step output is included in the prompt.</summary>
    /// <value>The include prior transcript value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool IncludePriorTranscript { get; set; } = true;
    /// <summary>Gets or sets whether the step produces the visible final answer.</summary>
    /// <value>The produces final answer value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool ProducesFinalAnswer { get; set; }
    /// <summary>Gets or sets whether the built-in orchestration behavior is used.</summary>
    /// <value>The use built in behavior value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool UseBuiltInBehavior { get; set; }
    /// <summary>Gets or sets the loop-group key shared by consecutive steps.</summary>
    /// <value>The loop group value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string LoopGroup { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the maximum loop iterations value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum loop iterations value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int MaximumLoopIterations { get; set; } = 1;
    /// <summary>
    /// Gets or sets the loop completion marker value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The loop completion marker value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string LoopCompletionMarker { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the council workflow step definition state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the step pauses for a human checkpoint.</summary>
    /// <value>The requires human checkpoint value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool RequiresHumanCheckpoint { get; set; }
    /// <summary>Gets or sets whether registered organic/DX functions may be requested.</summary>
    /// <value>The can use organic functions value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool CanUseOrganicFunctions { get; set; } = true;
    /// <summary>Gets or sets whether this step may emit first-class X-Round control requests.</summary>
    /// <value>The x functions enabled value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XFunctionsEnabled { get; set; }
    /// <summary>Gets or sets whether X-Rounds may reconsider or deliberately re-execute another workflow step.</summary>
    /// <value>The x can revisit value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanRevisit { get; set; }
    /// <summary>Gets or sets whether an X-Function may return an explicit text result and finish the parent workflow.</summary>
    /// <value>The x can return text value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanReturnText { get; set; }
    /// <summary>Gets or sets whether an X-Function may run one bounded single-model derived subtask.</summary>
    /// <value>The x can start single model value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanStartSingleModel { get; set; }
    /// <summary>Gets or sets whether an X-Function may start another configured Council as a derived subtask.</summary>
    /// <value>The x can start council value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XCanStartCouncil { get; set; }
    /// <summary>Gets or sets the maximum number of X-Round transitions accepted from this source step in one run.</summary>
    /// <value>The x maximum transitions value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int XMaximumTransitions { get; set; } = 3;
    /// <summary>Gets or sets whether every X-Round request from this step must be approved by a local human before it changes control flow.</summary>
    /// <value>The x requires human approval value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool XRequiresHumanApproval { get; set; }
    /// <summary>Gets or sets the default target workflow-step key for revisit actions.</summary>
    /// <value>The x default target step key value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string XDefaultTargetStepKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the default Council team key used by the start-council X-Function.</summary>
    /// <value>The x child council team key value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string XChildCouncilTeamKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the maximum nested child-Council depth allowed when this step emits start-council X-Functions.</summary>
    /// <value>The x maximum child council depth value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int XMaximumChildCouncilDepth { get; set; } = 1;
    /// <summary>Gets or sets the default provider-qualified model identity used by the start-single-model X-Function.</summary>
    /// <value>The x child model name value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public string XChildModelName { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the step owns a complete ASCII frame.</summary>
    /// <value>The produces ascii frame value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public bool ProducesAsciiFrame { get; set; }
    /// <summary>
    /// Gets or sets the ascii frame width value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ascii frame width value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int AsciiFrameWidth { get; set; } = 80;
    /// <summary>
    /// Gets or sets the ascii frame height value that forms part of the council workflow step definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ascii frame height value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int AsciiFrameHeight { get; set; } = 25;
    /// <summary>Gets or sets the logical world-step scale represented by one frame.</summary>
    /// <value>The world step scale value exposed by <see cref="CouncilWorkflowStepDefinition"/>.</value>
    public int WorldStepScale { get; set; } = 1;
}

/// <summary>Represents the persisted SQLite row for one Council team configuration.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class CouncilTeamConfiguration
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council team instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this council team instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the roles JSON value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The roles JSON value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string RolesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the preferred capabilities JSON value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preferred capabilities JSON value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string PreferredCapabilitiesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the architecture contracts JSON value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The architecture contracts JSON value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string ArchitectureContractsJson { get; set; } = "[]";
    /// <summary>Gets or sets serialized workflow steps.</summary>
    /// <value>The workflow steps JSON value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string WorkflowStepsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the expert preparation prompt template value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expert preparation prompt template value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string ExpertPreparationPromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the leader synthesis prompt template value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The leader synthesis prompt template value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string LeaderSynthesisPromptTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the main round instruction template value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The main round instruction template value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public string MainRoundInstructionTemplate { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the seed version value that forms part of the council team state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed version value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public int SeedVersion { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether system seed applies to the council team state.
    /// </summary>
    /// <value>The is system seed value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public bool IsSystemSeed { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether user modified applies to the council team state.
    /// </summary>
    /// <value>The is user modified value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public bool IsUserModified { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the council team state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the created at UTC associated with this council team state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council team state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilTeamConfiguration"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Carries a reviewed Council team change and its persistence flags.</summary>
[DocumentationUpdated("2.1.20")]
public sealed class SaveCouncilTeamConfigurationRequest
{
    /// <summary>
    /// Gets or sets the team value that forms part of the save council team configuration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The team value exposed by <see cref="SaveCouncilTeamConfigurationRequest"/>.</value>
    public OrganicCouncilTeamDefinition Team { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the save council team configuration state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="SaveCouncilTeamConfigurationRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the current user confirmed the exact change.</summary>
    /// <value>The user confirmed value exposed by <see cref="SaveCouncilTeamConfigurationRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}
