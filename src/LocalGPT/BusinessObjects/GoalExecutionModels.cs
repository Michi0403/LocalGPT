namespace LocalGPT.BusinessObjects;

/// <summary>Current progress of a single goal executed through the Council.</summary>
public enum GoalExecutionState
{
    /// <summary>The goal cannot be executed using currently available capabilities or models.</summary>
    Blocked,
    /// <summary>The Council ran, but its external effects still require independent verification.</summary>
    NeedsVerification,
    /// <summary>The Council completed with no reported step failures and a nonempty response.</summary>
    Completed,
    /// <summary>One or more Council steps failed or the Council returned no answer.</summary>
    Failed,
    /// <summary>Goal execution is currently active.</summary>
    Running,
    /// <summary>Execution was interrupted; a safe resume decision is required.</summary>
    Interrupted,
    /// <summary>Uncertain external effects must be reviewed before retry.</summary>
    NeedsApproval
}

/// <summary>Provenance for an observed Council step, artifact or verified workspace read.</summary>
public sealed class GoalExecutionEvidence
{
    /// <summary>Identifies the kind of observed evidence, not a model assertion.</summary>
    /// <value>The source kind.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Identifies the Council step, artifact or read that supplied the observation.</summary>
    /// <value>The observed source identity.</value>
    public string Source { get; set; } = string.Empty;
    /// <summary>Indicates whether the observation came from a successful technical action.</summary>
    /// <value>Whether its service-produced status is successful.</value>
    public bool Succeeded { get; set; }
    /// <summary>Stable correlation ID for the Council execution.</summary>
    /// <value>The originating Council run.</value>
    public Guid CouncilRunId { get; set; }
}

/// <summary>Typed user goal and requested constraints, with the existing Council request as the execution payload.</summary>
public sealed class GoalExecutionRequest
{
    /// <summary>Exact user intent to accomplish without adding hidden task instructions.</summary>
    /// <value>The original goal text.</value>
    public string GoalText { get; set; } = string.Empty;
    /// <summary>Configured Council parameters, provider routes, permissions and callbacks.</summary>
    /// <value>The caller-owned Council execution request.</value>
    public MultiModelCouncilRequest CouncilRequest { get; set; } = new();
    /// <summary>Optional concrete organic capabilities required for completion.</summary>
    /// <value>Capability keys resolved against the live directory.</value>
    public List<string> RequiredCapabilities { get; set; } = [];
    /// <summary>Optional concrete DXFunction keys required for completion.</summary>
    /// <value>Function names resolved against executable DI handlers.</value>
    public List<string> RequiredFunctions { get; set; } = [];
    /// <summary>Prevents automatic team substitution when the caller explicitly selects a team.</summary>
    /// <value>Whether the saved team selection is authoritative.</value>
    public bool PreserveSelectedTeam { get; set; }
}

/// <summary>Inspectable goal plan, routing and observed result; does not confuse model prose with verified effects.</summary>
public sealed class GoalExecutionResult
{
    /// <summary>Identity of the caller's Council execution.</summary>
    /// <value>The stable run ID.</value>
    public Guid GoalId { get; set; }
    /// <summary>Original user goal.</summary>
    /// <value>The submitted intent.</value>
    public string GoalText { get; set; } = string.Empty;
    /// <summary>Selected persisted Council team, if routing succeeded.</summary>
    /// <value>The database team key.</value>
    public string SelectedTeamKey { get; set; } = string.Empty;
    /// <summary>Provider-qualified participant identities selected for the run.</summary>
    /// <value>The model selection keys.</value>
    public List<string> SelectedModels { get; set; } = [];
    /// <summary>Provider-qualified model routes discovered if the caller did not select participants.</summary>
    /// <value>Selected installed and configured model references.</value>
    public List<ProviderModelReference> PlannedModelSelections { get; set; } = [];
    /// <summary>Available executable DXFunction names relevant to the goal.</summary>
    /// <value>Names found in the live executable directory.</value>
    public List<string> AvailableFunctions { get; set; } = [];
    /// <summary>Available online organic capability keys relevant to the goal.</summary>
    /// <value>Names discovered from live organic skills.</value>
    public List<string> AvailableCapabilities { get; set; } = [];
    /// <summary>Explicitly requested requirements not provided by the current runtime directory.</summary>
    /// <value>Missing executable functions or online capability keys.</value>
    public List<string> MissingRequirements { get; set; } = [];
    /// <summary>Observations obtained from the actual Council run rather than generated claims.</summary>
    /// <value>Step, workspace and artifact evidence.</value>
    public List<GoalExecutionEvidence> Evidence { get; set; } = [];
    /// <summary>Current outcome classification.</summary>
    /// <value>Result state backed by observed execution.</value>
    public GoalExecutionState State { get; set; } = GoalExecutionState.Blocked;
    /// <summary>Underlying completed Council result for existing chat and 1-Wire clients.</summary>
    /// <value>The Council result, or null when execution was blocked.</value>
    public MultiModelCouncilResult? CouncilResult { get; set; }
    /// <summary>Whether routing was decided by a verified model response or the catalog fallback.</summary>
    /// <value>Planner provenance.</value>
    public string PlannerSource { get; set; } = string.Empty;
    /// <summary>Ordered goal substeps proposed by the planner and checked against persisted teams.</summary>
    /// <value>Read-only plan projection.</value>
    public List<GoalExecutionPlanStep> PlanSteps { get; set; } = [];
    /// <summary>Current persisted recovery state for this goal.</summary>
    /// <value>The checkpoint state after the most recent durable write.</value>
    public GoalExecutionCheckpoint? Checkpoint { get; set; }
}

/// <summary>Non-recursive goal routing summary attached to an existing Council result for chat and 1-Wire consumers.</summary>
public sealed class GoalExecutionSummary
{
    /// <summary>Stable original goal identifier across Council subruns and application restarts.</summary><value>Goal identifier.</value>
    public Guid GoalId { get; set; }
    /// <summary>Provenance of the chosen plan.</summary><value>Planner kind.</value>
    public string PlannerSource { get; set; } = string.Empty;
    /// <summary>Number of safe semantic replan attempts.</summary><value>Replanning count.</value>
    public int ReplanCount { get; set; }
    /// <summary>Number of durably committed subgoals.</summary><value>Committed steps.</value>
    public int CompletedSubgoals { get; set; }
    /// <summary>Number of planned subgoals.</summary><value>Step count.</value>
    public int TotalSubgoals { get; set; }
    /// <summary>Outcome inferred from Council operation results rather than conversational assertions.</summary>
    /// <value>The observed execution state.</value>
    public GoalExecutionState State { get; set; }
    /// <summary>Resolved persisted team key.</summary>
    /// <value>The team that ran.</value>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>Directory requirements that could not be resolved at planning time.</summary>
    /// <value>Missing keys.</value>
    public List<string> MissingRequirements { get; set; } = [];
    /// <summary>Source-attributed observations from Council execution.</summary>
    /// <value>Technical execution evidence.</value>
    public List<GoalExecutionEvidence> Evidence { get; set; } = [];
}

/// <summary>A proposed subgoal executed by one configured Council team; keys are scoped to one goal.</summary>
public sealed class GoalExecutionPlanStep
{
    /// <summary>Stable plan-local step identity.</summary><value>The step identifier.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>Concrete objective derived from the original user goal, not an approval instruction.</summary><value>The step objective.</value>
    public string Objective { get; set; } = string.Empty;
    /// <summary>Existing configured Council team key; never created from a planner response.</summary><value>The executable team key.</value>
    public string TeamKey { get; set; } = string.Empty;
}

/// <summary>Observed outcome for one Council invocation within a goal; saved before the next invocation.</summary>
public sealed class GoalExecutionStepReceipt
{
    /// <summary>Plan-local step identifier.</summary><value>The step key.</value>
    public string StepKey { get; set; } = string.Empty;
    /// <summary>Actual Council run identity for this attempt.</summary><value>The Council run ID.</value>
    public Guid CouncilRunId { get; set; }
    /// <summary>Status of the step at the last durable checkpoint.</summary><value>The observed execution state.</value>
    public GoalExecutionState State { get; set; }
    /// <summary>Successful Council answer retained for dependent steps; never inferred to verify external effects.</summary><value>Response from the Council.</value>
    public string Answer { get; set; } = string.Empty;
    /// <summary>Observed Council failure reason, not a model's unverified explanation.</summary><value>Failure metadata.</value>
    public string FailureSummary { get; set; } = string.Empty;
    /// <summary>Whether any completed external effects or artifacts make automatic replay unsafe.</summary><value>Replay safety marker.</value>
    public bool ExternalEffectsPossible { get; set; }
    /// <summary>Service-attributed technical observations.</summary><value>The observations list.</value>
    public List<GoalExecutionEvidence> Evidence { get; set; } = [];
    /// <summary>UTC update time.</summary><value>Time of the last write.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Durable goal-level journal snapshot. Sensitive intent stays in the LocalGPT per-user data directory.</summary>
public sealed class GoalExecutionCheckpoint
{
    /// <summary>Stable goal identifier across all Council attempts.</summary><value>The goal identifier.</value>
    public Guid GoalId { get; set; }
    /// <summary>User's original goal.</summary><value>Original goal text.</value>
    public string GoalText { get; set; } = string.Empty;
    /// <summary>User-supplied Council prompt, retained to reconstruct the next safe step.</summary><value>Original prompt.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>Caller-selected team when selection was explicitly authoritative.</summary><value>The explicit team key.</value>
    public string RequestedTeamKey { get; set; } = string.Empty;
    /// <summary>Original explicit-team policy.</summary><value>Whether team selection is locked.</value>
    public bool PreserveSelectedTeam { get; set; }
    /// <summary>Provider-qualified selection keys; endpoints/secrets are resolved live, never stored.</summary><value>Model selection keys.</value>
    public List<string> ModelSelectionKeys { get; set; } = [];
    /// <summary>Required DXFunction keys.</summary><value>Required function list.</value>
    public List<string> RequiredFunctions { get; set; } = [];
    /// <summary>Required organic capabilities.</summary><value>Required capability list.</value>
    public List<string> RequiredCapabilities { get; set; } = [];
    /// <summary>Caller-configured maximum Council rounds.</summary><value>Round budget.</value>
    public int MaxRounds { get; set; } = 1;
    /// <summary>Caller-configured output token limit.</summary><value>Output limit.</value>
    public int MaxOutputTokens { get; set; } = 1024;
    /// <summary>Caller-configured context token limit.</summary><value>Context limit.</value>
    public int MaxContextTokens { get; set; } = 4096;
    /// <summary>Caller-configured concurrent model limit.</summary><value>Concurrency limit.</value>
    public int MaxParallelModels { get; set; } = 1;
    /// <summary>Caller-configured model timeout seconds.</summary><value>Timeout setting.</value>
    public int ModelTimeoutSeconds { get; set; } = 180;
    /// <summary>Caller-configured resource load percentage.</summary><value>Resource use setting.</value>
    public int ResourceLoadPercent { get; set; } = 30;
    /// <summary>Caller-configured hardware concurrency policy.</summary><value>Whether hardware roads can run in parallel.</value>
    public bool AllowParallelHardwareRoads { get; set; } = true;
    /// <summary>Caller-configured Ollama keepalive; no credentials are included.</summary><value>Keepalive setting.</value>
    public string? OllamaKeepAlive { get; set; }
    /// <summary>Caller-configured Ollama GPU layer count.</summary><value>GPU layer setting.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>Whether existing chat memory should be read.</summary><value>Original memory policy.</value>
    public bool IncludeMemory { get; set; } = true;
    /// <summary>Whether original user settings allow saving chat memory.</summary><value>Original save policy.</value>
    public bool SaveToMemory { get; set; } = true;
    /// <summary>Whether original user settings enable change-review workflow.</summary><value>Original review policy.</value>
    public bool UseChangeReviewWorkflow { get; set; } = true;
    /// <summary>Existing project topic identity.</summary><value>Topic ID.</value>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>Existing project revision identity.</summary><value>Revision ID.</value>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>Correlation value from a trusted 1-Wire request.</summary><value>Correlation key.</value>
    public string OneWireCorrelationId { get; set; } = string.Empty;
    /// <summary>Original Council organic-workflow preference.</summary><value>Organic workflow flag.</value>
    public bool UseOrganicCouncilWorkflow { get; set; }
    /// <summary>Existing attached workspace identities, without copying source contents.</summary><value>Workspace names.</value>
    public List<string> AttachedWorkspaceNames { get; set; } = [];
    /// <summary>Existing project identity.</summary><value>Project ID.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>Original serialized project context supplied to Council.</summary><value>Project context.</value>
    public string ExternalProjectContextJson { get; set; } = string.Empty;
    /// <summary>Ordered, validated execution plan.</summary><value>The plan.</value>
    public List<GoalExecutionPlanStep> Steps { get; set; } = [];
    /// <summary>Persisted outcome for each attempted or completed subgoal.</summary><value>Step receipts.</value>
    public List<GoalExecutionStepReceipt> Receipts { get; set; } = [];
    /// <summary>Current overall goal state.</summary><value>State of this checkpoint.</value>
    public GoalExecutionState State { get; set; } = GoalExecutionState.Blocked;
    /// <summary>Number of semantic replanning operations performed.</summary><value>Replanning count.</value>
    public int ReplanCount { get; set; }
    /// <summary>Whether the last planner used model inference or a deterministic fallback.</summary><value>Planner provenance.</value>
    public string PlannerSource { get; set; } = string.Empty;
    /// <summary>Last updated time; a running checkpoint found after restart is considered interrupted.</summary><value>UTC update time.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Untrusted semantic planner JSON proposal prior to server-side capability validation.</summary>
public sealed class GoalSemanticProposal
{
    /// <summary>Existing Council team identifier.</summary><value>Chosen team.</value>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>Planner-proposed subgoals, validated before use.</summary><value>Step proposals.</value>
    public List<GoalExecutionPlanStep> Steps { get; set; } = [];
    /// <summary>Function keys selected from the executable directory.</summary><value>Function selections.</value>
    public List<string> Functions { get; set; } = [];
    /// <summary>Skill keys selected from online organic capabilities.</summary><value>Skill selections.</value>
    public List<string> Capabilities { get; set; } = [];
}
