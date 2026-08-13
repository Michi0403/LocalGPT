using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported human collaboration boundary values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum HumanCollaborationBoundary
{
    Phase,
    Round,
    Completion
}

/// <summary>
/// Defines the supported human approval reuse scope values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum HumanApprovalReuseScope
{
    ExactRequestOnce,
    CurrentApplicationSession,
    PersistentUntilChanged
}

/// <summary>
/// Represents the input contract for human collaboration, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class HumanCollaborationRequest
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this human collaboration instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this human collaboration instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this human collaboration instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable operation key used to identify or correlate this human collaboration instance with related application state.
    /// </summary>
    /// <value>The operation key value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string OperationKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter fingerprint value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter fingerprint value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string ParameterFingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the request kind value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request kind value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string RequestKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the title value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the risk level value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The risk level value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string RiskLevel { get; set; } = "Medium";
    /// <summary>
    /// Gets or sets the status value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requested by value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested by value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string RequestedBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requested role value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested role value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string RequestedRole { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the question scope value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The question scope value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string QuestionScope { get; set; } = "Member";
    /// <summary>
    /// Gets or sets the gate mode value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The gate mode value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string GateMode { get; set; } = "None";
    /// <summary>
    /// Gets or sets the target members text value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target members text value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string TargetMembersText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requested council round value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested council round value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public int RequestedCouncilRound { get; set; }
    /// <summary>
    /// Gets or sets the requested council phase value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested council phase value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string RequestedCouncilPhase { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the suggested responses text value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suggested responses text value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string SuggestedResponsesText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the response prompt value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response prompt value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string ResponsePrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prefill text value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prefill text value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string PrefillText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the user response value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The user response value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string UserResponse { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the decision reason value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision reason value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string DecisionReason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the decision by value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision by value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public string DecisionBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable decision by profile identifier used to identify or correlate this human collaboration instance with related application state.
    /// </summary>
    /// <value>The decision by profile identifier value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public Guid? DecisionByProfileId { get; set; }
    /// <summary>
    /// Gets or sets the requested at UTC associated with this human collaboration state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The requested at UTC value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this human collaboration state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the decided at UTC associated with this human collaboration state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The decided at UTC value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the consumed at UTC associated with this human collaboration state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The consumed at UTC value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public DateTime? ConsumedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the approval reuse scope value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The approval reuse scope value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public HumanApprovalReuseScope ApprovalReuseScope { get; set; } = HumanApprovalReuseScope.ExactRequestOnce;
    /// <summary>
    /// Gets or sets a value indicating whether consume approval applies to the human collaboration state.
    /// </summary>
    /// <value>The consume approval value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public bool ConsumeApproval { get; set; } = true;
    /// <summary>
    /// Gets or sets the stable approval session identifier used to identify or correlate this human collaboration instance with related application state.
    /// </summary>
    /// <value>The approval session identifier value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public Guid? ApprovalSessionId { get; set; }
    /// <summary>
    /// Gets or sets the decision version value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision version value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public int DecisionVersion { get; set; }
    /// <summary>
    /// Gets or sets the earliest council round value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The earliest council round value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public int EarliestCouncilRound { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether required before completion applies to the human collaboration state.
    /// </summary>
    /// <value>The required before completion value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public bool RequiredBeforeCompletion { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether sensitive applies to the human collaboration state.
    /// </summary>
    /// <value>The is sensitive value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether free text applies to the human collaboration state.
    /// </summary>
    /// <value>The allow free text value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    public bool AllowFreeText { get; set; } = true;

    /// <summary>
    /// Gets the suggested responses collection maintained or exposed by this human collaboration instance for downstream processing.
    /// </summary>
    /// <value>The suggested responses value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    [NotMapped]
    public IReadOnlyList<string> SuggestedResponses => SuggestedResponsesText
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(8)
        .ToList();

    /// <summary>
    /// Gets the target members display value that forms part of the human collaboration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target members display value exposed by <see cref="HumanCollaborationRequest"/>.</value>
    [NotMapped]
    public string TargetMembersDisplay => string.Join(", ", TargetMembersText
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase));
}

/// <summary>
/// Represents a human council participant profile application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class HumanCouncilParticipantProfile
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this human council participant profile instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the display name value that forms part of the human council participant profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public string DisplayName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets the role name value that forms part of the human council participant profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role name value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public string RoleName { get; set; } = "Human collaborator";
    /// <summary>
    /// Gets or sets the expertise value that forms part of the human council participant profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expertise value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public string Expertise { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the working style value that forms part of the human council participant profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The working style value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public string WorkingStyle { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the human council participant profile state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the profile version value that forms part of the human council participant profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The profile version value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public int ProfileVersion { get; set; } = 1;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this human council participant profile state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated by value that forms part of the human council participant profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The updated by value exposed by <see cref="HumanCouncilParticipantProfile"/>.</value>
    public string UpdatedBy { get; set; } = "Human User";
}

/// <summary>
/// Represents a human council contribution application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class HumanCouncilContribution
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this human council contribution instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this human council contribution instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public Guid CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the human display name value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human display name value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public string HumanDisplayName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets the human role value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human role value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public string HumanRole { get; set; } = "Human collaborator";
    /// <summary>
    /// Gets or sets the content value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the earliest council round value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The earliest council round value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public int EarliestCouncilRound { get; set; } = 1;
    /// <summary>
    /// Gets or sets the status value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the submitted at UTC associated with this human council contribution state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The submitted at UTC value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the injected at UTC associated with this human council contribution state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The injected at UTC value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public DateTime? InjectedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the evaluated at UTC associated with this human council contribution state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The evaluated at UTC value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public DateTime? EvaluatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the evaluation value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evaluation value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public string Evaluation { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the evaluation verdict value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evaluation verdict value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public string EvaluationVerdict { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the evaluated after round value that forms part of the human council contribution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evaluated after round value exposed by <see cref="HumanCouncilContribution"/>.</value>
    public int EvaluatedAfterRound { get; set; }
}

/// <summary>
/// Represents a human approval request spec application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="CorrelationId">Identifier of the correlation to use for this operation.</param>
/// <param name="OperationKey">Operation key value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="Title">Title value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="RiskLevel">Risk level value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="Source">Source value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="RequestedBy">Requested by value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="RequestedRole">Requested role value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="CouncilRunId">Identifier of the council run to use for this operation.</param>
/// <param name="EarliestCouncilRound">Earliest council round value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="RequiredBeforeCompletion">Value indicating whether required before completion should apply to this operation.</param>
/// <param name="IsSensitive">Value indicating whether sensitive should apply to this operation.</param>
/// <param name="RequestKind">Request kind value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="SuggestedResponsesText">Suggested responses text value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="ResponsePrompt">Response prompt value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="PrefillText">Prefill text value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="AllowFreeText">Value indicating whether free text should apply to this operation.</param>
/// <param name="ParameterFingerprint">Parameter fingerprint value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="QuestionScope">Question scope value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="GateMode">Gate mode value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="TargetMembersText">Target members text value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="RequestedCouncilRound">Requested council round value supplied to the human approval request spec operation and used when producing its result.</param>
/// <param name="RequestedCouncilPhase">Requested council phase value supplied to the human approval request spec operation and used when producing its result.</param>
public sealed record HumanApprovalRequestSpec(
    string CorrelationId,
    string OperationKey,
    string Title,
    string Description,
    string RiskLevel,
    string Source,
    string RequestedBy,
    string RequestedRole,
    Guid? CouncilRunId = null,
    int EarliestCouncilRound = 0,
    bool RequiredBeforeCompletion = false,
    bool IsSensitive = true,
    string RequestKind = "",
    string SuggestedResponsesText = "",
    string ResponsePrompt = "",
    string PrefillText = "",
    bool AllowFreeText = true,
    string ParameterFingerprint = "",
    string QuestionScope = "Member",
    string GateMode = "None",
    string TargetMembersText = "",
    int RequestedCouncilRound = 0,
    string RequestedCouncilPhase = "");

/// <summary>
/// Represents a human collaboration gate status application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="IsBlocked">Value indicating whether blocked should apply to this operation.</param>
/// <param name="Boundary">Boundary value supplied to the human collaboration gate status operation and used when producing its result.</param>
/// <param name="UpcomingRound">Upcoming round value supplied to the human collaboration gate status operation and used when producing its result.</param>
/// <param name="UpcomingPhase">Upcoming phase value supplied to the human collaboration gate status operation and used when producing its result.</param>
/// <param name="BlockingRequests">Human collaboration request dependency used by the human collaboration gate status workflow to provide the corresponding application capability.</param>
public sealed record HumanCollaborationGateStatus(
    bool IsBlocked,
    HumanCollaborationBoundary Boundary,
    int UpcomingRound,
    string UpcomingPhase,
    IReadOnlyList<HumanCollaborationRequest> BlockingRequests);

/// <summary>
/// Represents the outcome of human approval gate, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="IsAuthorized">Value indicating whether authorized should apply to this operation.</param>
/// <param name="IsDeclined">Value indicating whether declined should apply to this operation.</param>
/// <param name="RequestId">Identifier of the request to use for this operation.</param>
/// <param name="Status">Status value supplied to the human approval gate operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the human approval gate operation and used when producing its result.</param>
/// <param name="DecisionReason">Decision reason value supplied to the human approval gate operation and used when producing its result.</param>
/// <param name="CorrelationId">Identifier of the correlation to use for this operation.</param>
/// <param name="UserResponse">User response value supplied to the human approval gate operation and used when producing its result.</param>
public sealed record HumanApprovalGateResult(
    bool IsAuthorized,
    bool IsDeclined,
    Guid? RequestId,
    string Status,
    string Message,
    string DecisionReason = "",
    string CorrelationId = "",
    string UserResponse = "");

/// <summary>
/// Represents a human decision submission application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Approved">Approved value supplied to the human decision submission operation and used when producing its result.</param>
/// <param name="Response">Response value supplied to the human decision submission operation and used when producing its result.</param>
/// <param name="Reason">Reason value supplied to the human decision submission operation and used when producing its result.</param>
/// <param name="ReuseScope">Reuse scope value supplied to the human decision submission operation and used when producing its result.</param>
/// <param name="ConsumeApproval">Value indicating whether consume approval should apply to this operation.</param>
public sealed record HumanDecisionSubmission(
    bool? Approved,
    string Response,
    string Reason,
    HumanApprovalReuseScope ReuseScope = HumanApprovalReuseScope.ExactRequestOnce,
    bool ConsumeApproval = true);

/// <summary>
/// Represents a human council run snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RunId">Identifier of the run to use for this operation.</param>
/// <param name="StartedAtUtc">Started at utc value supplied to the human council run snapshot operation and used when producing its result.</param>
/// <param name="CurrentRound">Current round value supplied to the human council run snapshot operation and used when producing its result.</param>
/// <param name="Phase">Phase value supplied to the human council run snapshot operation and used when producing its result.</param>
/// <param name="CouncilMembers">String dependency used by the human council run snapshot workflow to provide the corresponding application capability.</param>
/// <param name="IsWaitingForFinalHumanInput">Value indicating whether waiting for final human input should apply to this operation.</param>
public sealed record HumanCouncilRunSnapshot(
    Guid RunId,
    DateTime StartedAtUtc,
    int CurrentRound,
    string Phase,
    IReadOnlyList<string> CouncilMembers,
    bool IsWaitingForFinalHumanInput);

/// <summary>
/// Represents a human collaboration snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Profile">Profile value supplied to the human collaboration snapshot operation and used when producing its result.</param>
/// <param name="Requests">Human collaboration request dependency used by the human collaboration snapshot workflow to provide the corresponding application capability.</param>
/// <param name="ActiveRuns">Human council run snapshot dependency used by the human collaboration snapshot workflow to provide the corresponding application capability.</param>
/// <param name="Contributions">Human council contribution dependency used by the human collaboration snapshot workflow to provide the corresponding application capability.</param>
public sealed record HumanCollaborationSnapshot(
    HumanCouncilParticipantProfile Profile,
    IReadOnlyList<HumanCollaborationRequest> Requests,
    IReadOnlyList<HumanCouncilRunSnapshot> ActiveRuns,
    IReadOnlyList<HumanCouncilContribution> Contributions);


/// <summary>
/// Represents a deferred DevExpress AI invocation application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class DeferredDxAiInvocation
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable approval request identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The approval request identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid ApprovalRequestId { get; set; }
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The operation identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid OperationId { get; set; }
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the function name value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters JSON value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters JSON value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string ParametersJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the confirmation summary hash value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confirmation summary hash value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string ConfirmationSummaryHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requested by value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested by value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string RequestedBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project version identifier used to identify or correlate this deferred DevExpress AI invocation instance with related application state.
    /// </summary>
    /// <value>The project version identifier value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public Guid? ProjectVersionId { get; set; }
    /// <summary>
    /// Gets or sets the application version value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application version value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string ApplicationVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the result status value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The result status value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string ResultStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the result summary value that forms part of the deferred DevExpress AI invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The result summary value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public string ResultSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the attempt count that quantifies the associated deferred DevExpress AI invocation data.
    /// </summary>
    /// <value>The attempt count value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public int AttemptCount { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this deferred DevExpress AI invocation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this deferred DevExpress AI invocation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the last attempt at UTC associated with this deferred DevExpress AI invocation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last attempt at UTC value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public DateTime? LastAttemptAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the completed at UTC associated with this deferred DevExpress AI invocation state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="DeferredDxAiInvocation"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Represents a deferred DevExpress AI execution outcome application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="DeferredInvocationId">Identifier of the deferred invocation to use for this operation.</param>
/// <param name="ApprovalRequestId">Identifier of the approval request to use for this operation.</param>
/// <param name="FunctionName">Function name value supplied to the deferred DevExpress AI execution outcome operation and used when producing its result.</param>
/// <param name="Status">Status value supplied to the deferred DevExpress AI execution outcome operation and used when producing its result.</param>
/// <param name="ResultStatus">Result status value supplied to the deferred DevExpress AI execution outcome operation and used when producing its result.</param>
/// <param name="ResultSummary">Result summary value supplied to the deferred DevExpress AI execution outcome operation and used when producing its result.</param>
public sealed record DeferredDxAiExecutionOutcome(
    Guid DeferredInvocationId,
    Guid ApprovalRequestId,
    string FunctionName,
    string Status,
    string ResultStatus,
    string ResultSummary);
