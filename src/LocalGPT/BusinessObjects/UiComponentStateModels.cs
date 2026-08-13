namespace LocalGPT.BusinessObjects;

/// <summary>
/// Mutable draft state for one human-collaboration response editor.
/// Kept in the business-object domain so Razor components contain behavior and rendering only.
/// </summary>
/// <param name="response">Response value supplied to the human collaboration request editor operation and used when producing its result.</param>
/// <param name="reason">Reason value supplied to the human collaboration request editor operation and used when producing its result.</param>
/// <param name="reuseScope">Reuse scope value supplied to the human collaboration request editor operation and used when producing its result.</param>
/// <param name="consumeApproval">Value indicating whether consume approval should apply to this operation.</param>
public sealed class HumanCollaborationRequestEditor(
    string response,
    string reason,
    HumanApprovalReuseScope reuseScope = HumanApprovalReuseScope.ExactRequestOnce,
    bool consumeApproval = true)
{
    /// <summary>
    /// Gets or sets the response value that forms part of the human collaboration request editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response value exposed by <see cref="HumanCollaborationRequestEditor"/>.</value>
    public string Response { get; set; } = response;
    /// <summary>
    /// Gets or sets the reason value that forms part of the human collaboration request editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="HumanCollaborationRequestEditor"/>.</value>
    public string Reason { get; set; } = reason;
    /// <summary>
    /// Gets or sets the reuse scope value that forms part of the human collaboration request editor state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reuse scope value exposed by <see cref="HumanCollaborationRequestEditor"/>.</value>
    public HumanApprovalReuseScope ReuseScope { get; set; } = reuseScope;
    /// <summary>
    /// Gets or sets a value indicating whether consume approval applies to the human collaboration request editor state.
    /// </summary>
    /// <value>The consume approval value exposed by <see cref="HumanCollaborationRequestEditor"/>.</value>
    public bool ConsumeApproval { get; set; } = consumeApproval;
}

/// <summary>
/// Identifies an assistant message that can receive user feedback in Chat.
/// </summary>
/// <param name="SortOrder">Sort order value supplied to the chat feedback target operation and used when producing its result.</param>
/// <param name="Label">Label value supplied to the chat feedback target operation and used when producing its result.</param>
public sealed record ChatFeedbackTarget(int SortOrder, string Label);
