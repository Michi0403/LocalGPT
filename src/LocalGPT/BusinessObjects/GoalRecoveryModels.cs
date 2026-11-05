namespace LocalGPT.BusinessObjects;

/// <summary>Classifies why a Council member slot has no usable answer when the goal recovery helper is asked to keep the run moving.</summary>
public enum GoalMemberFailureKind
{
    /// <summary>No Council evidence step exists for the member slot.</summary>
    NoStep = 0,
    /// <summary>The member finished without visible answer text.</summary>
    EmptyAnswer = 1,
    /// <summary>The provider request failed or timed out after the member's own bounded retry.</summary>
    ProviderFailure = 2,
    /// <summary>The repetition watchdog stopped a looping stream; no same-member retry has happened yet.</summary>
    RepetitionWatchdog = 3,
    /// <summary>The member's AI host did not answer the outage wait and the single reachability check.</summary>
    HostUnreachable = 4
}

/// <summary>Defines what the goal recovery helper decided for one failed Council member slot.</summary>
public enum GoalMemberRecoveryAction
{
    /// <summary>Run the failed member again for the same slot.</summary>
    RetrySameMember = 0,
    /// <summary>Run another member of the same Council run for the slot.</summary>
    UseOtherMember = 1,
    /// <summary>Leave the slot without an answer for now; the failure stays visible and the run continues.</summary>
    SkipSlot = 2
}

/// <summary>Describes whether a member may start its provider request on its AI host in the current Council run.</summary>
public enum GoalHostAdmissionKind
{
    /// <summary>The host is not known to be down in this run, or it answered the reachability check again.</summary>
    Allowed = 0,
    /// <summary>The host is marked down in this run and did not answer the single reachability check before this member.</summary>
    HostUnreachable = 1,
    /// <summary>The local user approved excluding the host for this run.</summary>
    ExcludedForRun = 2
}

/// <summary>Run-scoped evidence about one Council member that the goal recovery helper used for a decision.</summary>
public sealed class GoalMemberEvidence
{
    /// <summary>Gets the provider-qualified member identity.</summary>
    /// <value>The provider-qualified model selection key.</value>
    public string ModelName { get; init; } = string.Empty;
    /// <summary>Gets the AI host identity of the member.</summary>
    /// <value>The runtime authority key of the member's provider endpoint.</value>
    public string HostKey { get; init; } = string.Empty;
    /// <summary>Gets whether the member belongs to the configured pool of the failed role.</summary>
    /// <value><see langword="true"/> for a configured member of the role; <see langword="false"/> for another member of the run.</value>
    public bool InRolePool { get; init; }
    /// <summary>Gets whether the member's host is usable in this run.</summary>
    /// <value><see langword="false"/> when the host is marked down or excluded for the run.</value>
    public bool HostAvailable { get; init; } = true;
    /// <summary>Gets the number of usable answers of this member in the same role during this run.</summary>
    /// <value>Count of steps without error and with visible text.</value>
    public int RunSuccesses { get; init; }
    /// <summary>Gets the number of failed attempts of this member in the same role during this run.</summary>
    /// <value>Count of steps with an error.</value>
    public int RunFailures { get; init; }
    /// <summary>Gets the number of usable answers of this member in the same role in recent retained Council runs.</summary>
    /// <value>Count of retained steps without error and with visible text.</value>
    public int HistorySuccesses { get; init; }
    /// <summary>Gets the number of failed attempts of this member in the same role in recent retained Council runs.</summary>
    /// <value>Count of retained steps with an error.</value>
    public int HistoryFailures { get; init; }
}

/// <summary>Everything the goal recovery helper needs to decide how one failed Council member slot continues.</summary>
/// <remarks>The caller supplies only members of the current run; the helper never adds a member or host that is not already part of the run.</remarks>
public sealed class GoalMemberRecoveryContext
{
    /// <summary>Gets the Council run identifier.</summary>
    /// <value>The run whose slot failed.</value>
    public Guid RunId { get; init; }
    /// <summary>Gets the saved team key.</summary>
    /// <value>The team key used for diagnostics.</value>
    public string TeamKey { get; init; } = string.Empty;
    /// <summary>Gets the configured workflow step key.</summary>
    /// <value>The workflow step key used for diagnostics.</value>
    public string WorkflowStepKey { get; init; } = string.Empty;
    /// <summary>Gets the role of the failed slot.</summary>
    /// <value>The configured role name.</value>
    public string RoleName { get; init; } = string.Empty;
    /// <summary>Gets the logical Council round.</summary>
    /// <value>The round of the failed slot.</value>
    public int Round { get; init; }
    /// <summary>Gets the member originally assigned to the slot.</summary>
    /// <value>The provider-qualified failed member.</value>
    public string FailedModel { get; init; } = string.Empty;
    /// <summary>Gets why the slot has no usable answer.</summary>
    /// <value>The observed failure kind.</value>
    public GoalMemberFailureKind FailureKind { get; init; }
    /// <summary>Gets the step's configured member recovery policy.</summary>
    /// <value>The persisted recovery mode.</value>
    public CouncilMemberFailureRecoveryMode RecoveryMode { get; init; }
    /// <summary>Gets whether only the same member may answer (same-member policy or a single assigned model).</summary>
    /// <value><see langword="true"/> when substitution is not permitted by the configuration.</value>
    public bool SameMemberOnly { get; init; }
    /// <summary>Gets the 1-based recovery attempt for this slot.</summary>
    /// <value>The attempt number.</value>
    public int AttemptNumber { get; init; }
    /// <summary>Gets the active members of the run. This is the security boundary for every recovery choice.</summary>
    /// <value>Provider-qualified members of the current run.</value>
    public IReadOnlyList<string> RunParticipants { get; init; } = [];
    /// <summary>Gets the configured members of the failed role in this run, ordered by observed health.</summary>
    /// <value>Preferred recovery candidates.</value>
    public IReadOnlyList<string> RolePool { get; init; } = [];
    /// <summary>Gets members reserved by other roles of the same distinct-assignment group that the configuration does not allow to be reused.</summary>
    /// <value>Members that must not answer for this role.</value>
    public IReadOnlyCollection<string> ReservedByOtherRoles { get; init; } = [];
    /// <summary>Gets members already tried for this slot, including the failed member when its retry already happened.</summary>
    /// <value>Members that are not chosen again for this slot.</value>
    public IReadOnlyCollection<string> AttemptedModels { get; init; } = [];
    /// <summary>Gets the AI host identity of every run member and of the failed member.</summary>
    /// <value>Map from provider-qualified member to host key.</value>
    public IReadOnlyDictionary<string, string> HostKeys { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Gets whether the failed role is paired one-to-one with another role.</summary>
    /// <value><see langword="true"/> for a paired role.</value>
    public bool IsPairedRole { get; init; }
    /// <summary>Gets the paired role name of the failed slot.</summary>
    /// <value>The paired role name or an empty string.</value>
    public string PairedRoleName { get; init; } = string.Empty;
    /// <summary>Gets the paired partner of the failed member.</summary>
    /// <value>The partner member or an empty string.</value>
    public string PairedParticipant { get; init; } = string.Empty;
    /// <summary>Gets the Council steps of this run, used as evidence.</summary>
    /// <value>Current run steps.</value>
    public IReadOnlyList<MultiModelCouncilStep> RunSteps { get; init; } = [];
}

/// <summary>Decision of the goal recovery helper for one failed Council member slot.</summary>
public sealed class GoalMemberRecoveryDecision
{
    /// <summary>Gets the chosen action.</summary>
    /// <value>Retry, use another run member, or skip the slot.</value>
    public GoalMemberRecoveryAction Action { get; init; }
    /// <summary>Gets the member that answers for the slot.</summary>
    /// <value>The provider-qualified member; empty for <see cref="GoalMemberRecoveryAction.SkipSlot"/>.</value>
    public string ModelName { get; init; } = string.Empty;
    /// <summary>Gets the readable reason, rendered from the configurable helper texts.</summary>
    /// <value>Reason shown in progress messages, warnings and logs.</value>
    public string Reason { get; init; } = string.Empty;
    /// <summary>Gets whether another member answers for a paired slot and must inherit the failed member's pairing.</summary>
    /// <value><see langword="true"/> after an approved paired substitution.</value>
    public bool SubstitutesPairedSlot { get; init; }
    /// <summary>Gets whether the chosen member is not configured for the failed role.</summary>
    /// <value><see langword="true"/> when a member of another role answers.</value>
    public bool FromOtherRole { get; init; }
    /// <summary>Gets the approval request that governs a paired substitution, when one exists.</summary>
    /// <value>The Human Collaboration request identifier.</value>
    public Guid? ApprovalRequestId { get; init; }
    /// <summary>Gets the evidence of the considered members.</summary>
    /// <value>Evidence rows in decision order.</value>
    public IReadOnlyList<GoalMemberEvidence> Evidence { get; init; } = [];
}

/// <summary>Result of the run-scoped AI host admission check before a member starts its provider request.</summary>
public sealed class GoalHostAdmission
{
    /// <summary>Gets the admission result.</summary>
    /// <value>Allowed, unreachable, or excluded for the run.</value>
    public GoalHostAdmissionKind Kind { get; init; }
    /// <summary>Gets whether the member may start its request.</summary>
    /// <value><see langword="true"/> for <see cref="GoalHostAdmissionKind.Allowed"/>.</value>
    public bool IsAllowed => Kind == GoalHostAdmissionKind.Allowed;
    /// <summary>Gets the user-visible explanation for a refused start.</summary>
    /// <value>Rendered helper text; empty when allowed.</value>
    public string Message { get; init; } = string.Empty;
}

/// <summary>Describes a provider failure that may be caused by an unavailable AI host.</summary>
public sealed class GoalHostFailureReport
{
    /// <summary>Gets the Council run identifier.</summary>
    /// <value>The run in which the member failed.</value>
    public Guid RunId { get; init; }
    /// <summary>Gets the AI host identity.</summary>
    /// <value>The runtime authority key of the member's endpoint.</value>
    public string HostKey { get; init; } = string.Empty;
    /// <summary>Gets the provider-qualified member.</summary>
    /// <value>The failed member.</value>
    public string ModelName { get; init; } = string.Empty;
    /// <summary>Gets the resolved provider model of the member.</summary>
    /// <value>The exact provider model used for availability checks.</value>
    public ProviderModelReference Model { get; init; } = new();
    /// <summary>Gets the Council phase of the failed request.</summary>
    /// <value>The phase used for diagnostics.</value>
    public string Phase { get; init; } = string.Empty;
    /// <summary>Gets an optional callback that shows a notice in the member's stream before a bounded wait starts.</summary>
    /// <value>Stream notice callback, or <see langword="null"/>.</value>
    public Action<string>? Progress { get; init; }
}

/// <summary>Decision of the goal recovery helper after a provider failure on a possibly unavailable AI host.</summary>
public sealed class GoalHostFailureDecision
{
    /// <summary>Gets whether the member's bounded same-member retry may run now.</summary>
    /// <value><see langword="true"/> when the exact model is reachable.</value>
    public bool RetryNow { get; init; }
    /// <summary>Gets whether the host is marked down or excluded for this run.</summary>
    /// <value><see langword="true"/> when the failure is attributed to the host.</value>
    public bool HostUnavailable { get; init; }
    /// <summary>Gets a user-visible notice for the member's stream; empty when nothing needs to be shown.</summary>
    /// <value>Rendered helper text.</value>
    public string Notice { get; init; } = string.Empty;
    /// <summary>Gets the exclusion offer created or found for the host.</summary>
    /// <value>The Human Collaboration request identifier, when one exists.</value>
    public Guid? ExclusionRequestId { get; init; }
}

/// <summary>Run-scoped availability state of one AI host as tracked by the goal recovery helper.</summary>
public sealed class GoalHostRunStatus
{
    /// <summary>Gets the AI host identity.</summary>
    /// <value>The runtime authority key.</value>
    public string HostKey { get; init; } = string.Empty;
    /// <summary>Gets whether the host is marked down in this run.</summary>
    /// <value><see langword="true"/> after the outage wait and the reachability check failed.</value>
    public bool IsDown { get; init; }
    /// <summary>Gets whether the local user excluded the host for this run.</summary>
    /// <value><see langword="true"/> after an approved exclusion.</value>
    public bool IsExcluded { get; init; }
    /// <summary>Gets the time of the first observed outage in this run.</summary>
    /// <value>UTC time or <see langword="null"/>.</value>
    public DateTime? FirstOutageAtUtc { get; init; }
    /// <summary>Gets the exclusion offer for the host.</summary>
    /// <value>The Human Collaboration request identifier, when one exists.</value>
    public Guid? ExclusionRequestId { get; init; }
}

/// <summary>
/// User-editable texts of the goal recovery helper (runtime policy <c>GoalRecoveryHelperTextsJson</c>).
/// Placeholders in braces, for example <c>{Model}</c> or <c>{Host}</c>, are replaced with run values.
/// </summary>
public sealed class GoalRecoveryHelperTexts
{
    /// <summary>Gets or sets the stream notice shown while the first outage wait of a host runs.</summary>
    /// <value>Template with {Model}, {Host}, {Seconds}.</value>
    public string HostWaitingNotice { get; set; } = string.Empty;
    /// <summary>Gets or sets the notice shown when a host is marked down for the run.</summary>
    /// <value>Template with {Model}, {Host}, {Seconds}.</value>
    public string HostDownNotice { get; set; } = string.Empty;
    /// <summary>Gets or sets the notice shown when a host that was down answers again.</summary>
    /// <value>Template with {Host}.</value>
    public string HostBackNotice { get; set; } = string.Empty;
    /// <summary>Gets or sets the failed-step text of a member that was not started because its host is unreachable.</summary>
    /// <value>Template with {Model}, {Host}.</value>
    public string HostUnreachableStepText { get; set; } = string.Empty;
    /// <summary>Gets or sets the failed-step text of a member that was not started because its host is excluded for the run.</summary>
    /// <value>Template with {Model}, {Host}.</value>
    public string HostExcludedStepText { get; set; } = string.Empty;
    /// <summary>Gets or sets the run warning added once per unreachable host.</summary>
    /// <value>Template with {Host}.</value>
    public string HostDownWarning { get; set; } = string.Empty;
    /// <summary>Gets or sets the approval title for excluding a host for one run.</summary>
    /// <value>Template with {Host}.</value>
    public string HostExclusionTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the approval description for excluding a host for one run.</summary>
    /// <value>Template with {Host}, {Model}, {Seconds}.</value>
    public string HostExclusionDescription { get; set; } = string.Empty;
    /// <summary>Gets or sets the suggested answers of the host exclusion request, one per line.</summary>
    /// <value>Newline-separated answers.</value>
    public string HostExclusionSuggestedResponses { get; set; } = string.Empty;
    /// <summary>Gets or sets the response prompt of the host exclusion request.</summary>
    /// <value>Plain text.</value>
    public string HostExclusionResponsePrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the approval title for letting other members answer for a failed paired member.</summary>
    /// <value>Template with {Role}.</value>
    public string PairedSubstitutionTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the approval description for a paired substitution.</summary>
    /// <value>Template with {Model}, {Role}, {PairedRole}, {Partner}, {Candidate}.</value>
    public string PairedSubstitutionDescription { get; set; } = string.Empty;
    /// <summary>Gets or sets the suggested answers of the paired substitution request, one per line.</summary>
    /// <value>Newline-separated answers.</value>
    public string PairedSubstitutionSuggestedResponses { get; set; } = string.Empty;
    /// <summary>Gets or sets the response prompt of the paired substitution request.</summary>
    /// <value>Plain text.</value>
    public string PairedSubstitutionResponsePrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when the configuration allows only the same member.</summary>
    /// <value>Template with {Model}.</value>
    public string ReasonSameMemberPolicy { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when the same member is retried first.</summary>
    /// <value>Template with {Model}.</value>
    public string ReasonSameMemberFirst { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when the same member retries again because no other member is available.</summary>
    /// <value>Template with {Model}.</value>
    public string ReasonSameMemberAgain { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when another member of the role pool answers.</summary>
    /// <value>Template with {Model}, {Candidate}, {Evidence}.</value>
    public string ReasonRolePoolMember { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when a member of another role answers.</summary>
    /// <value>Template with {Model}, {Candidate}, {Evidence}, {Role}.</value>
    public string ReasonOtherRoleMember { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when a paired slot waits for the user's decision.</summary>
    /// <value>Template with {Model}, {Role}.</value>
    public string ReasonPairedPending { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when the user declined paired substitutes.</summary>
    /// <value>Template with {Model}, {Role}.</value>
    public string ReasonPairedDeclined { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when no member of the run can answer.</summary>
    /// <value>Template with {Model}, {Role}.</value>
    public string ReasonNoCandidate { get; set; } = string.Empty;
    /// <summary>Gets or sets the reason used when the same-member policy meets an unavailable host.</summary>
    /// <value>Template with {Model}, {Host}.</value>
    public string ReasonSameMemberHostDown { get; set; } = string.Empty;
    /// <summary>Gets or sets the requester name shown on the helper's approval requests.</summary>
    /// <value>Plain text.</value>
    public string ApprovalRequestedBy { get; set; } = string.Empty;
    /// <summary>Gets or sets the requester role shown on the helper's approval requests.</summary>
    /// <value>Plain text.</value>
    public string ApprovalRequestedRole { get; set; } = string.Empty;
    /// <summary>Gets or sets the evidence summary of one member.</summary>
    /// <value>Template with {RunSuccesses}, {RunFailures}, {HistorySuccesses}, {HistoryFailures}.</value>
    public string EvidenceSummary { get; set; } = string.Empty;
    /// <summary>Gets or sets the evidence text when no member has earlier results in the role.</summary>
    /// <value>Plain text.</value>
    public string EvidenceNone { get; set; } = string.Empty;
}
