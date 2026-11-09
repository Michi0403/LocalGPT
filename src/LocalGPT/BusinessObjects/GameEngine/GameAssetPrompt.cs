namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>The prompt and labels of one asset drawing turn, built from the game's <c>asset.*</c> texts.</summary>
public sealed class GameAssetPrompt
{
    /// <summary>Gets or sets the phase label of the drawing turn (text <c>asset.phase</c>).</summary>
    /// <value>The phase label.</value>
    public string PhaseLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the entrant the asset is drawn for.</summary>
    /// <value>The entrant name.</value>
    public string EntrantName { get; set; } = string.Empty;

    /// <summary>Gets or sets the complete prompt template of the drawing turn.</summary>
    /// <value>The prompt with entrant, drawer and other-asset placeholders filled.</value>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Gets or sets the progress line shown while the drawing turn runs (text <c>asset.progress</c>).</summary>
    /// <value>The progress message.</value>
    public string ProgressMessage { get; set; } = string.Empty;
}
