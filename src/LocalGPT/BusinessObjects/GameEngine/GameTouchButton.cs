namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>One on-screen touch control of a running game.</summary>
public sealed class GameTouchButton
{
    /// <summary>Gets or sets the layout slot (CSS class) of the control, such as <c>up</c>, <c>left</c> or <c>primary</c>.</summary>
    /// <value>The slot name; empty for a plain button.</value>
    public string Slot { get; set; } = string.Empty;

    /// <summary>Gets or sets the semantic action submitted by the control.</summary>
    /// <value>The action key.</value>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the visible label of the control.</summary>
    /// <value>The label from the game's <c>touch.*</c> texts.</value>
    public string Label { get; set; } = string.Empty;
}
