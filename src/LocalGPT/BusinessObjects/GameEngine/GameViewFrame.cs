namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One frame produced by a view primitive: the frame text and, when the view styles its own cells, the style runs.
/// </summary>
public sealed class GameViewFrame
{
    /// <summary>
    /// Gets or sets the frame text with one line per terminal row.
    /// </summary>
    /// <value>The rendered frame text.</value>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explicit style runs of the frame; <c>null</c> lets the ruleset's style rules colour it.
    /// </summary>
    /// <value>The view-owned style runs, or <c>null</c>.</value>
    public List<CouncilAsciiStyleRun>? StyleRuns { get; set; }
}
