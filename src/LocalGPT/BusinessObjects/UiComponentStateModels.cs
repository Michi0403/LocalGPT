namespace LocalGPT.BusinessObjects;

/// <summary>
/// Mutable draft state for one human-collaboration response editor.
/// Kept in the business-object domain so Razor components contain behavior and rendering only.
/// </summary>
public sealed class HumanCollaborationRequestEditor(
    string response,
    string reason,
    HumanApprovalReuseScope reuseScope = HumanApprovalReuseScope.ExactRequestOnce,
    bool consumeApproval = true)
{
    /// <summary>
    /// Gets or sets response.
    /// </summary>
    public string Response { get; set; } = response;
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = reason;
    /// <summary>
    /// Gets or sets reuse scope.
    /// </summary>
    public HumanApprovalReuseScope ReuseScope { get; set; } = reuseScope;
    /// <summary>
    /// Gets or sets consume approval.
    /// </summary>
    public bool ConsumeApproval { get; set; } = consumeApproval;
}

/// <summary>
/// Identifies an assistant message that can receive user feedback in Chat.
/// </summary>
public sealed record ChatFeedbackTarget(int SortOrder, string Label);
