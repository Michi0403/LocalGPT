using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;






public sealed class HumanCollaborationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CouncilRunId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string OperationKey { get; set; } = string.Empty;
    public string ParameterFingerprint { get; set; } = string.Empty;
    public string RequestKind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Medium";
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public string SuggestedResponsesText { get; set; } = string.Empty;
    public string ResponsePrompt { get; set; } = string.Empty;
    public string PrefillText { get; set; } = string.Empty;
    public string UserResponse { get; set; } = string.Empty;
    public string DecisionReason { get; set; } = string.Empty;
    public string DecisionBy { get; set; } = string.Empty;
    public Guid? DecisionByProfileId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int EarliestCouncilRound { get; set; }
    public bool RequiredBeforeCompletion { get; set; }
    public bool IsSensitive { get; set; }
    public bool AllowFreeText { get; set; } = true;

    [NotMapped]
    public IReadOnlyList<string> SuggestedResponses => SuggestedResponsesText
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(8)
        .ToList();
}

public sealed class HumanCouncilParticipantProfile
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = "Human User";
    public string RoleName { get; set; } = "Human collaborator";
    public string Expertise { get; set; } = string.Empty;
    public string WorkingStyle { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int ProfileVersion { get; set; } = 1;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "Human User";
}

public sealed class HumanCouncilContribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CouncilRunId { get; set; }
    public string HumanDisplayName { get; set; } = "Human User";
    public string HumanRole { get; set; } = "Human collaborator";
    public string Content { get; set; } = string.Empty;
    public int EarliestCouncilRound { get; set; } = 1;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? InjectedAtUtc { get; set; }
    public DateTime? EvaluatedAtUtc { get; set; }
    public string Evaluation { get; set; } = string.Empty;
    public string EvaluationVerdict { get; set; } = string.Empty;
    public int EvaluatedAfterRound { get; set; }
}

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
    string ParameterFingerprint = "");

public sealed record HumanApprovalGateResult(
    bool IsAuthorized,
    bool IsDeclined,
    Guid? RequestId,
    string Status,
    string Message,
    string DecisionReason = "",
    string CorrelationId = "",
    string UserResponse = "");

public sealed record HumanDecisionSubmission(
    bool? Approved,
    string Response,
    string Reason);

public sealed record HumanCouncilRunSnapshot(
    Guid RunId,
    DateTime StartedAtUtc,
    int CurrentRound,
    string Phase,
    IReadOnlyList<string> CouncilMembers,
    bool IsWaitingForFinalHumanInput);

public sealed record HumanCollaborationSnapshot(
    HumanCouncilParticipantProfile Profile,
    IReadOnlyList<HumanCollaborationRequest> Requests,
    IReadOnlyList<HumanCouncilRunSnapshot> ActiveRuns,
    IReadOnlyList<HumanCouncilContribution> Contributions);


public sealed class DeferredDxAiInvocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApprovalRequestId { get; set; }
    public Guid? CouncilRunId { get; set; }
    public Guid OperationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public string ConfirmationSummaryHash { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectVersionId { get; set; }
    public string ApplicationVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResultStatus { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed record DeferredDxAiExecutionOutcome(
    Guid DeferredInvocationId,
    Guid ApprovalRequestId,
    string FunctionName,
    string Status,
    string ResultStatus,
    string ResultSummary);
