namespace LocalGPT.BusinessObjects;

/// <summary>Configures optional recovery of verified function receipts after the team's ordinary workflow.</summary>
public sealed class CouncilCompletionRecoveryPolicy
{
    /// <summary>Configures individual function-specific retries; an empty list disables automatic completion repair.</summary>
    /// <value>Saved function-specific recovery rules; empty disables automatic completion repair.</value>
    public List<CouncilRequiredFunctionRecoveryRule> Rules { get; set; } = [];
    /// <summary>When enabled, re-runs the final-answer workflow step after function-recovery attempts.</summary>
    /// <value>Whether to re-run the final-answer step after attempted function repairs.</value>
    public bool RepeatFinalAnswerAfterRecovery { get; set; }
    /// <summary>User-owned instruction for the post-recovery final-answer step.</summary>
    /// <value>User-provided final review instruction.</value>
    public string FinalReviewPromptTemplate { get; set; } = string.Empty;
}

/// <summary>Defines one configurable same-team recovery action, using registered tool receipts rather than prose claims.</summary>
public sealed class CouncilRequiredFunctionRecoveryRule
{
    /// <summary>Registered function whose verified successful result is required.</summary>
    /// <value>Exact DXFunction name whose verified receipt is missing.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Exact enabled workflow-step key authorized to attempt the function.</summary>
    /// <value>Exact configured workflow-step key authorized to attempt the function.</value>
    public string WorkflowStepKey { get; set; } = string.Empty;
    /// <summary>Maximum extra attempts after the ordinary workflow; zero disables retries.</summary>
    /// <value>Configured additional attempts, capped by the Council runtime policy.</value>
    public int MaximumAttempts { get; set; }
    /// <summary>Successful function receipts required before this rule may run.</summary>
    /// <value>Function receipts required before this recovery rule executes.</value>
    public List<string> PrerequisiteFunctions { get; set; } = [];
    /// <summary>User-owned corrective instruction used when this required function receipt is missing.</summary>
    /// <value>User-owned corrective instruction for the selected retry.</value>
    public string RecoveryPromptTemplate { get; set; } = string.Empty;
}

/// <summary>Routes the output of one configured Council team to another without choosing teams by hard-coded keys.</summary>
public sealed class CouncilTeamFollowOnPolicy
{
    /// <summary>Exact saved team key to run afterward; empty disables automatic handoff.</summary>
    /// <value>Configured follow-on team key; empty disables the transition.</value>
    public string TeamKey { get; set; } = string.Empty;
    /// <summary>User-owned prompt passed to the selected follow-on team.</summary>
    /// <value>User-provided downstream task prompt with parent-result placeholders.</value>
    public string PromptTemplate { get; set; } = string.Empty;
    /// <summary>Only start the follow-on team when all mandatory function receipts are verified.</summary>
    /// <value>Whether mandatory receipt evidence must be complete before starting the next team.</value>
    public bool RequireVerifiedCompletion { get; set; } = true;
    /// <summary>Require approval for the team transition, in addition to consequential child tool approvals.</summary>
    /// <value>Whether the owner must approve the transition.</value>
    public bool RequiresHumanApproval { get; set; } = true;
    /// <summary>Maximum chain nesting depth, capped by the Council's shared runtime policy.</summary>
    /// <value>Maximum descendant council depth permitted by this policy and the global limit.</value>
    public int MaximumDepth { get; set; } = 1;
}
