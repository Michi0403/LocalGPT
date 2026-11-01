namespace LocalGPT.BusinessObjects;

/// <summary>Untrusted, bounded diagnostic data for one attempted action.</summary>
public sealed class AiHelpFailureEvidence
{
    /// <summary>Short machine-readable failure code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Redacted description, never passwords or complete prompts.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Claimed evidence origin; not a proof of its authority.</summary>
    public string Source { get; set; } = "agent";
}

/// <summary>Scoped report of one attempt against a stable task/goal identifier.</summary>
public sealed class AiHelpAttemptReport
{
    /// <summary>Stable task identifier reused across retries and reloads.</summary>
    public string TaskId { get; set; } = string.Empty;
    /// <summary>Optional idempotency key for one actual attempt; reuse it on transport replay.</summary>
    public Guid? AttemptId { get; set; }
    /// <summary>Concise description of the active objective.</summary>
    public string Task { get; set; } = string.Empty;
    /// <summary>Action actually attempted, not the next proposed action.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Exact registered function name, when a DXAiFunction call was attempted.</summary>
    public string? FunctionName { get; set; }
    /// <summary>Provider-qualified failed model selection, when known by the owner.</summary>
    public string? FailedModelSelectionKey { get; set; }
    /// <summary>Observed completion category.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Optional observed error, treated as untrusted evidence until corroborated.</summary>
    public AiHelpFailureEvidence? Error { get; set; }
    /// <summary>Optional question that the human could answer if the issue is confirmed.</summary>
    public string? UserQuestion { get; set; }
    /// <summary>Optional original Council run; does not grant authority to that run.</summary>
    public Guid? CouncilRunId { get; set; }
}

/// <summary>Instruction to the calling agent, without executing its next operation.</summary>
public sealed class AiHelpRecoveryDecision
{
    /// <summary>One of completed, try_alternative, retry_with_guidance, await_user, halt.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>One actionable, bounded recovery instruction.</summary>
    public string Instruction { get; set; } = string.Empty;
    /// <summary>Human question, when escalation was actually persisted.</summary>
    public string Question { get; set; } = string.Empty;
    /// <summary>Safe, bounded explanation.</summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>Link to the existing human-collaboration inbox item, if any.</summary>
    public Guid? HumanRequestId { get; set; }
    /// <summary>Stable task identity for status and explicit continuation.</summary>
    public string TaskId { get; set; } = string.Empty;
}

/// <summary>Persisted execution state; only the owning help service may mutate it.</summary>
public sealed class AiHelpTaskState
{
    /// <summary>Stable task identity.</summary>
    public string TaskId { get; set; } = string.Empty;
    /// <summary>Last completed attempt's idempotency key, when supplied.</summary>
    public Guid? LastAttemptId { get; set; }
    /// <summary>Recent distinct attempt IDs, preventing out-of-order transport replays.</summary>
    public List<Guid> RecentAttemptIds { get; set; } = [];
    /// <summary>Number of observed failed attempts.</summary>
    public int Failures { get; set; }
    /// <summary>Number of repeats of the last same failing action.</summary>
    public int Repeated { get; set; }
    /// <summary>Hash of the last action and failure code, without storing error text.</summary>
    public string LastFingerprint { get; set; } = string.Empty;
    /// <summary>Whether the one permitted curator consultation has been consumed.</summary>
    public bool CuratorUsed { get; set; }
    /// <summary>The exact last decision; await_user must remain stable across rejoin.</summary>
    public AiHelpRecoveryDecision LastDecision { get; set; } = new();
    /// <summary>UTC timestamp of last state change.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Limited curator output; never represents a verified tool execution.</summary>
public sealed class AiHelpCuratorAdvice
{
    /// <summary>retry, ask_user, or cannot_resolve.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>One materially distinct correction.</summary>
    public string Instruction { get; set; } = string.Empty;
    /// <summary>Question for the existing human inbox, if needed.</summary>
    public string Question { get; set; } = string.Empty;
    /// <summary>Explanation when the curator cannot safely proceed.</summary>
    public string Reason { get; set; } = string.Empty;
}
