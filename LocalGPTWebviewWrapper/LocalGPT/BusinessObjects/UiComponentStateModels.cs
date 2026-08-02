namespace LocalGPT.BusinessObjects;

/// <summary>
/// Mutable draft state for one human-collaboration response editor.
/// Kept in the business-object domain so Razor components contain behavior and rendering only.
/// </summary>
public sealed class HumanCollaborationRequestEditor(string response, string reason)
{
    public string Response { get; set; } = response;
    public string Reason { get; set; } = reason;
}

/// <summary>
/// Identifies an assistant message that can receive user feedback in Chat.
/// </summary>
public sealed record ChatFeedbackTarget(int SortOrder, string Label);
