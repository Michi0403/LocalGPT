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

/// <summary>Defines how one role member is selected to consolidate multiple role-member results.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilRoleResultSynthesisMemberMode
{
    /// <summary>Selects one of the role members with a stable run-local pseudo-random choice.</summary>
    DeterministicRandomRoleMember,
    /// <summary>Uses the exact provider-qualified role member selected on the workflow step.</summary>
    AssignedRoleMember
}

/// <summary>Defines how a configured Council workflow step recovers required member work after provider failure, timeout, or unusable output.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilMemberFailureRecoveryMode
{
    /// <summary>Preserves the failed attempt as evidence but does not automatically repeat the required member slot.</summary>
    Disabled = 0,
    /// <summary>Repeats the required member slot only with the same provider-qualified role member.</summary>
    RetrySameMember = 1,
    /// <summary>Retries the same member first, then selects another eligible member from the role's persisted Social Team pool when the execution mode permits substitution.</summary>
    RetrySameThenEligibleRolePool = 2
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

/// <summary>Defines whether a Council team runs an all-members model-readiness preflight before substantive workflow work.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilAllMembersReadinessPreflightMode
{
    /// <summary>Preserves historical behavior: supplied built-in orchestration runs its existing readiness phase while literal custom workflows do not add one.</summary>
    LegacyWorkflowDefault,
    /// <summary>Skips the all-members model-readiness phase.</summary>
    Disabled,
    /// <summary>Runs one compact role-aware readiness probe for every selected Council member before substantive workflow steps.</summary>
    RoleAwareProbe
}

/// <summary>Defines how one workflow step exposes registered automatic/native functions to its assigned model.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilAutomaticFunctionPolicyMode
{
    /// <summary>Loads older saved steps by inferring the explicit policy from the legacy boolean and allow-list fields.</summary>
    Legacy = 0,
    /// <summary>Exposes no automatic/native functions to this workflow step.</summary>
    Disabled = 1,
    /// <summary>Exposes every function that passes LocalGPT's normal registered-function policy.</summary>
    AllPolicyApproved = 2,
    /// <summary>Prefers the team's functions while retaining the policy-approved broker.</summary>
    TeamAllowList = 3,
    /// <summary>Prefers step functions while retaining the policy-approved broker.</summary>
    ExactAllowList = 4,
    /// <summary>For this selected step only, advertises the team's named tools exclusively; function permissions and approvals remain independent.</summary>
    StrictTeamAllowList = 5,
    /// <summary>For this selected step only, advertises the step's named tools exclusively; function permissions and approvals remain independent.</summary>
    StrictStepAllowList = 6
}

/// <summary>Defines how a configured Council step consumes intermediate results returned by the text DXFunction gateway.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilToolResultContinuationMode
{
    /// <summary>Preserves the returned function evidence without starting another model turn.</summary>
    Disabled = 0,
    /// <summary>Returns function evidence to the same assigned model for a bounded number of continuation turns.</summary>
    SameMemberBounded = 1
}

/// <summary>Represents the normalized automatic/native function policy resolved from one persisted Council team and workflow step.</summary>
/// <param name="Enabled">Whether automatic/native provider tools may be attached.</param>
/// <param name="AutomaticFunctionAllowList">Native function names to attach, or <see langword="null"/> for all registered policy-approved automatic functions (the original 5.5.0 contract). When <paramref name="IsStrict"/> is true, this is the exclusive allow-list.</param>
/// <param name="Description">Human-readable policy description used by Council diagnostics and capability briefing.</param>
/// <param name="IsStrict">Whether the function list is exclusive for this step. Per-function authorization and approval are always evaluated separately by the registered tool and skill security system.</param>
public sealed record CouncilAutomaticFunctionPolicyResolution(
    bool Enabled,
    IReadOnlyList<string>? AutomaticFunctionAllowList,
    string Description,
    bool IsStrict = false);

/// <summary>Defines project provisioning for any saved Council team, independent of its display name or stable key.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilProjectProvisioningMode
{
    /// <summary>Create a regular Project only when the run explicitly requests one.</summary>
    OnExplicitRequest = 0,
    /// <summary>Never create a Project implicitly or from a run-level request.</summary>
    Disabled = 1,
    /// <summary>Ensure a Game Project exists for this run; existing selections must be Game Projects.</summary>
    EnsureGameProject = 2,
    /// <summary>Ensure a regular Project exists for this run if none was selected.</summary>
    EnsureProject = 3
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
    /// <summary>Gets or sets team-level automatic/native function guidance; only explicit strict step policy makes it an exclusive allow-list.</summary>
    /// <value>Registered DXFunction names; preferred except where a step explicitly selects StrictTeamAllowList.</value>
    public List<string> AllowedAutomaticFunctions { get; set; } = [];
    /// <summary>Controls project creation and validation from persisted team configuration; independent of team key.</summary>
    public CouncilProjectProvisioningMode ProjectProvisioningMode { get; set; } = CouncilProjectProvisioningMode.OnExplicitRequest;
    /// <summary>DXFunction results required before this team may report the overall work as complete.</summary>
    public List<string> RequiredCompletedFunctions { get; set; } = [];
    /// <summary>Configures evidence-based same-team recovery for missing required function receipts.</summary>
    /// <value>Serialized team-owned function receipt recovery configuration.</value>
    public CouncilCompletionRecoveryPolicy CompletionRecovery { get; set; } = new();
    /// <summary>Configures a subsequent user-selected Council team, independently of this team's key or purpose.</summary>
    /// <value>Saved team routing, verified completion and approval configuration.</value>
    public CouncilTeamFollowOnPolicy FollowOn { get; set; } = new();
    /// <summary>Optional persisted completion notice template; blank disables the additional notice for any team.</summary>
    /// <value>Optional user-authored team completion notification template.</value>
    public string CompletionNoticeTemplate { get; set; } = string.Empty;
    /// <summary>Gets or sets architecture and safety contracts that every round must preserve.</summary>
    /// <value>The architecture contracts value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public List<string> ArchitectureContracts { get; set; } = [];
    /// <summary>Gets or sets the literal ordered Council workflow.</summary>
    /// <value>The workflow steps value exposed by <see cref="OrganicCouncilTeamDefinition"/>.</value>
    public List<CouncilWorkflowStepDefinition> WorkflowSteps { get; set; } = [];
    /// <summary>Gets or sets the optional all-members model-readiness preflight policy used before substantive team workflow work.</summary>
    /// <value>The configured preflight mode. Legacy mode preserves the historical built-in-versus-literal workflow behavior.</value>
    public CouncilAllMembersReadinessPreflightMode AllMembersReadinessPreflightMode { get; set; } = CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault;
    /// <summary>Gets or sets whether explicit all-members preflight member output is allowed into later model workflow context.</summary>
    /// <value><see langword="true"/> to expose the potentially large preflight transcript to later workflow prompts; otherwise it stays visible only as run evidence.</value>
    public bool IncludeAllMembersReadinessPreflightInWorkflowContext { get; set; }
    /// <summary>Gets or sets the maximum output-token budget for each explicit role-aware preflight probe.</summary>
    /// <value>A small positive token budget used only by the optional preflight phase.</value>
    public int AllMembersReadinessPreflightMaxOutputTokens { get; set; } = 192;
    /// <summary>Stores the user-authored role-aware prompt override used only by the optional all-members readiness preflight.</summary>
    /// <value>An optional template supporting {{ModelName}}, {{TeamName}}, {{AssignedRoles}} and {{RoleResponsibilities}}. Blank uses the maintained compact readiness probe.</value>
    public string AllMembersReadinessPreflightPromptTemplate { get; set; } = string.Empty;
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
    /// <summary>Gets or sets whether this configured team was explicitly deleted by the user.</summary>
    /// <value><see langword="true"/> only for configuration-management views; deleted teams are never executable.</value>
    public bool IsDeleted { get; set; }
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
    /// <summary>Gets or sets whether a distinct-assignment role may reuse the available model when no unused member remains. Strict roles keep the default false.</summary>
    /// <value><c>true</c> when the role prefers distinct assignment but may degrade to model reuse instead of blocking the run.</value>
    public bool AllowDistinctAiAssignmentFallback { get; set; }
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
    /// <summary>Gets or sets the persisted team-level automatic/native function allow-list.</summary>
    /// <value>Serialized registered DXFunction names selected by the user for this configured team.</value>
    public string AllowedAutomaticFunctionsJson { get; set; } = "[]";
    /// <summary>Persists the team project-creation contract independently from its name.</summary>
    public CouncilProjectProvisioningMode ProjectProvisioningMode { get; set; } = CouncilProjectProvisioningMode.OnExplicitRequest;
    /// <summary>Persists registered function names whose completed results are required for a truthful team handoff.</summary>
    public string RequiredCompletedFunctionsJson { get; set; } = "[]";
    /// <summary>Persists per-function recovery rules and final evidence review as user-owned JSON.</summary>
    /// <value>Serialized configuration for the team recovery policy.</value>
    public string CompletionRecoveryJson { get; set; } = "{}";
    /// <summary>Persists optional follow-on team routing and approval as user-owned JSON.</summary>
    /// <value>Serialized configuration for selecting the next team.</value>
    public string FollowOnPolicyJson { get; set; } = "{}";
    /// <summary>Persists the team-specific completion message as user-editable seed/template data.</summary>
    /// <value>Optional user-authored team completion notification template.</value>
    public string CompletionNoticeTemplate { get; set; } = string.Empty;
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
    /// <summary>Gets or sets whether this persisted team configuration was explicitly deleted by the user while its optional supplied template remains available for reset.</summary>
    /// <value><see langword="true"/> when the row is a deletion tombstone and must not be offered or silently re-seeded.</value>
    public bool IsDeleted { get; set; }
    /// <summary>Selects whether this persisted Council team performs an all-member readiness preflight before substantive workflow roles begin.</summary>
    /// <value>The user-selected readiness policy stored with the team configuration.</value>
    public CouncilAllMembersReadinessPreflightMode AllMembersReadinessPreflightMode { get; set; } = CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault;
    /// <summary>Gets or sets whether all-members readiness output is included in later workflow context.</summary>
    /// <value><see langword="true"/> to include the preflight transcript in later model prompts.</value>
    public bool IncludeAllMembersReadinessPreflightInWorkflowContext { get; set; }
    /// <summary>Limits how much output one member may produce during this team's optional readiness preflight so large Councils do not inflate later context.</summary>
    /// <value>The maximum output token count for one explicit role-aware preflight probe.</value>
    public int AllMembersReadinessPreflightMaxOutputTokens { get; set; } = 192;
    /// <summary>Gets or sets the user-authored all-members readiness prompt override.</summary>
    /// <value>The optional persisted role-aware preflight template.</value>
    public string AllMembersReadinessPreflightPromptTemplate { get; set; } = string.Empty;
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
