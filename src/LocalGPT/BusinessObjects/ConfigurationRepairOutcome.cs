namespace LocalGPT.BusinessObjects;

/// <summary>Result of a configuration repair request: waiting for the human, declined, kept, applied or refused.</summary>
public sealed class ConfigurationRepairOutcome
{
    /// <summary>Repair is waiting for the local human decision.</summary>
    public const string AwaitingUserDecision = "AwaitingUserDecision";
    /// <summary>The local human declined; nothing changed.</summary>
    public const string DeclinedByUser = "DeclinedByUser";
    /// <summary>The local human chose to keep the current state.</summary>
    public const string KeptByUser = "KeptByUser";
    /// <summary>The approved option was applied and verified.</summary>
    public const string Applied = "Applied";
    /// <summary>The stored state changed after the finding; the option was rolled back.</summary>
    public const string StateChanged = "StateChanged";
    /// <summary>The finding no longer exists in the current data.</summary>
    public const string FindingNotPresent = "FindingNotPresent";
    /// <summary>The finding is report-only.</summary>
    public const string NoRepairAvailable = "NoRepairAvailable";
    /// <summary>The approval carried no recognizable option.</summary>
    public const string NoOptionSelected = "NoOptionSelected";
    /// <summary>The approval system refused to queue another request.</summary>
    public const string RequestRejected = "RequestRejected";

    /// <summary>Gets or sets the finding the outcome belongs to.</summary>
    /// <value>Finding key.</value>
    public string FindingKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the outcome status (one of the constants of this type).</summary>
    /// <value>Outcome status.</value>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable explanation.</summary>
    /// <value>Message.</value>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the Human Collaboration request carrying the decision.</summary>
    /// <value>Approval request identifier, when one exists.</value>
    public Guid? ApprovalRequestId { get; set; }

    /// <summary>Gets or sets the correlation identifier of the approval request.</summary>
    /// <value>Correlation identifier.</value>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the option the human selected.</summary>
    /// <value>Selected option key.</value>
    public string SelectedOptionKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the applied changes as "target: old → new" lines.</summary>
    /// <value>Applied change descriptions.</value>
    public List<string> AppliedChanges { get; set; } = [];
}
