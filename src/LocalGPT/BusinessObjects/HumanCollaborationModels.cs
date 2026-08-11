using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Lists supported human collaboration boundary values.
/// </summary>
public enum HumanCollaborationBoundary
{
    Phase,
    Round,
    Completion
}

/// <summary>
/// Lists supported human approval reuse scope values.
/// </summary>
public enum HumanApprovalReuseScope
{
    ExactRequestOnce,
    CurrentApplicationSession,
    PersistentUntilChanged
}

/// <summary>
/// Represents a human collaboration request.
/// </summary>
public sealed class HumanCollaborationRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets council run identifier.
    /// </summary>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets correlation identifier.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets operation key.
    /// </summary>
    public string OperationKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameter fingerprint.
    /// </summary>
    public string ParameterFingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets request kind.
    /// </summary>
    public string RequestKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets risk level.
    /// </summary>
    public string RiskLevel { get; set; } = "Medium";
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requested by.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requested role.
    /// </summary>
    public string RequestedRole { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets question scope.
    /// </summary>
    public string QuestionScope { get; set; } = "Member";
    /// <summary>
    /// Gets or sets gate mode.
    /// </summary>
    public string GateMode { get; set; } = "None";
    /// <summary>
    /// Gets or sets target members text.
    /// </summary>
    public string TargetMembersText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requested council round.
    /// </summary>
    public int RequestedCouncilRound { get; set; }
    /// <summary>
    /// Gets or sets requested council phase.
    /// </summary>
    public string RequestedCouncilPhase { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets suggested responses text.
    /// </summary>
    public string SuggestedResponsesText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets response prompt.
    /// </summary>
    public string ResponsePrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets prefill text.
    /// </summary>
    public string PrefillText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user response.
    /// </summary>
    public string UserResponse { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets decision reason.
    /// </summary>
    public string DecisionReason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets decision by.
    /// </summary>
    public string DecisionBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets decision by profile identifier.
    /// </summary>
    public Guid? DecisionByProfileId { get; set; }
    /// <summary>
    /// Gets or sets requested at UTC.
    /// </summary>
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets decided at UTC.
    /// </summary>
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets consumed at UTC.
    /// </summary>
    public DateTime? ConsumedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets approval reuse scope.
    /// </summary>
    public HumanApprovalReuseScope ApprovalReuseScope { get; set; } = HumanApprovalReuseScope.ExactRequestOnce;
    /// <summary>
    /// Gets or sets consume approval.
    /// </summary>
    public bool ConsumeApproval { get; set; } = true;
    /// <summary>
    /// Gets or sets approval session identifier.
    /// </summary>
    public Guid? ApprovalSessionId { get; set; }
    /// <summary>
    /// Gets or sets decision version.
    /// </summary>
    public int DecisionVersion { get; set; }
    /// <summary>
    /// Gets or sets earliest council round.
    /// </summary>
    public int EarliestCouncilRound { get; set; }
    /// <summary>
    /// Gets or sets required before completion.
    /// </summary>
    public bool RequiredBeforeCompletion { get; set; }
    /// <summary>
    /// Gets or sets is sensitive.
    /// </summary>
    public bool IsSensitive { get; set; }
    /// <summary>
    /// Gets or sets allow free text.
    /// </summary>
    public bool AllowFreeText { get; set; } = true;

    /// <summary>
    /// Gets or sets suggested responses.
    /// </summary>
    [NotMapped]
    public IReadOnlyList<string> SuggestedResponses => SuggestedResponsesText
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(8)
        .ToList();

    /// <summary>
    /// Gets or sets target members display.
    /// </summary>
    [NotMapped]
    public string TargetMembersDisplay => string.Join(", ", TargetMembersText
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase));
}

/// <summary>
/// Represents a human council participant profile.
/// </summary>
public sealed class HumanCouncilParticipantProfile
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets role name.
    /// </summary>
    public string RoleName { get; set; } = "Human collaborator";
    /// <summary>
    /// Gets or sets expertise.
    /// </summary>
    public string Expertise { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets working style.
    /// </summary>
    public string WorkingStyle { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets profile version.
    /// </summary>
    public int ProfileVersion { get; set; } = 1;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated by.
    /// </summary>
    public string UpdatedBy { get; set; } = "Human User";
}

/// <summary>
/// Represents a human council contribution.
/// </summary>
public sealed class HumanCouncilContribution
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets council run identifier.
    /// </summary>
    public Guid CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets human display name.
    /// </summary>
    public string HumanDisplayName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets human role.
    /// </summary>
    public string HumanRole { get; set; } = "Human collaborator";
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets earliest council round.
    /// </summary>
    public int EarliestCouncilRound { get; set; } = 1;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets submitted at UTC.
    /// </summary>
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets injected at UTC.
    /// </summary>
    public DateTime? InjectedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets evaluated at UTC.
    /// </summary>
    public DateTime? EvaluatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets evaluation.
    /// </summary>
    public string Evaluation { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets evaluation verdict.
    /// </summary>
    public string EvaluationVerdict { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets evaluated after round.
    /// </summary>
    public int EvaluatedAfterRound { get; set; }
}

/// <summary>
/// Represents a human approval request spec.
/// </summary>
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
/// Represents a human collaboration gate status.
/// </summary>
public sealed record HumanCollaborationGateStatus(
    bool IsBlocked,
    HumanCollaborationBoundary Boundary,
    int UpcomingRound,
    string UpcomingPhase,
    IReadOnlyList<HumanCollaborationRequest> BlockingRequests);

/// <summary>
/// Represents a human approval gate result.
/// </summary>
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
/// Represents a human decision submission.
/// </summary>
public sealed record HumanDecisionSubmission(
    bool? Approved,
    string Response,
    string Reason,
    HumanApprovalReuseScope ReuseScope = HumanApprovalReuseScope.ExactRequestOnce,
    bool ConsumeApproval = true);

/// <summary>
/// Represents a human council run snapshot.
/// </summary>
public sealed record HumanCouncilRunSnapshot(
    Guid RunId,
    DateTime StartedAtUtc,
    int CurrentRound,
    string Phase,
    IReadOnlyList<string> CouncilMembers,
    bool IsWaitingForFinalHumanInput);

/// <summary>
/// Represents a human collaboration snapshot.
/// </summary>
public sealed record HumanCollaborationSnapshot(
    HumanCouncilParticipantProfile Profile,
    IReadOnlyList<HumanCollaborationRequest> Requests,
    IReadOnlyList<HumanCouncilRunSnapshot> ActiveRuns,
    IReadOnlyList<HumanCouncilContribution> Contributions);


/// <summary>
/// Represents a deferred DevExpress ai invocation.
/// </summary>
public sealed class DeferredDxAiInvocation
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets approval request identifier.
    /// </summary>
    public Guid ApprovalRequestId { get; set; }
    /// <summary>
    /// Gets or sets council run identifier.
    /// </summary>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets operation identifier.
    /// </summary>
    public Guid OperationId { get; set; }
    /// <summary>
    /// Gets or sets correlation identifier.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameters JSON.
    /// </summary>
    public string ParametersJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets confirmation summary hash.
    /// </summary>
    public string ConfirmationSummaryHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requested by.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project version identifier.
    /// </summary>
    public Guid? ProjectVersionId { get; set; }
    /// <summary>
    /// Gets or sets application version.
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets result status.
    /// </summary>
    public string ResultStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets result summary.
    /// </summary>
    public string ResultSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets attempt count.
    /// </summary>
    public int AttemptCount { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets last attempt at UTC.
    /// </summary>
    public DateTime? LastAttemptAtUtc { get; set; }
    /// <summary>
    /// Gets or sets completed at UTC.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Represents a deferred DevExpress ai execution outcome.
/// </summary>
public sealed record DeferredDxAiExecutionOutcome(
    Guid DeferredInvocationId,
    Guid ApprovalRequestId,
    string FunctionName,
    string Status,
    string ResultStatus,
    string ResultSummary);
