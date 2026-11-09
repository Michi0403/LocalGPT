namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Result of one council-driven engine step: the authoritative session, the transcript summary and completion.
/// </summary>
public sealed class GameCouncilResolution
{
    /// <summary>
    /// Gets or sets the session snapshot after the step.
    /// </summary>
    /// <value>The updated session snapshot.</value>
    public CouncilGameSessionSnapshot Session { get; set; } = new();

    /// <summary>
    /// Gets or sets the Markdown summary the Council transcript receives for this step.
    /// </summary>
    /// <value>The engine summary built from the ruleset texts.</value>
    public string SummaryMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the progression reached its final result.
    /// </summary>
    /// <value><c>true</c> when the game is complete.</value>
    public bool Completed { get; set; }
}
