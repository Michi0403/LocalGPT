namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Presentation and transcript output of one council-driven engine step, applied to the session by the session service.
/// </summary>
public sealed class GameCouncilStepOutcome
{
    /// <summary>
    /// Gets or sets the one-shot animation frames of the step.
    /// </summary>
    /// <value>The frames in playback order.</value>
    public List<GameViewFrame> Frames { get; set; } = [];

    /// <summary>
    /// Gets or sets the frame shown when the step produced no animation frames.
    /// </summary>
    /// <value>The resting frame.</value>
    public GameViewFrame RestingFrame { get; set; } = new();

    /// <summary>
    /// Gets or sets the subtitle shown with the animation.
    /// </summary>
    /// <value>The subtitle text.</value>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subtitle style.
    /// </summary>
    /// <value>The subtitle style, or <c>null</c> for the session default.</value>
    public CouncilAsciiTextStyle? SubtitleStyle { get; set; }

    /// <summary>
    /// Gets or sets the delay between animation frames.
    /// </summary>
    /// <value>The frame delay in milliseconds.</value>
    public int FrameDelayMilliseconds { get; set; } = 650;

    /// <summary>
    /// Gets or sets how long the subtitle stays visible.
    /// </summary>
    /// <value>The subtitle hold in milliseconds.</value>
    public int SubtitleHoldMilliseconds { get; set; } = 1500;

    /// <summary>
    /// Gets or sets the frame caption.
    /// </summary>
    /// <value>The caption text.</value>
    public string Caption { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the renderer name recorded as frame owner.
    /// </summary>
    /// <value>The renderer name.</value>
    public string Renderer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current turn owner after the step.
    /// </summary>
    /// <value>The turn owner label.</value>
    public string TurnOwner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action recorded for the step.
    /// </summary>
    /// <value>The last action key.</value>
    public string LastAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Markdown summary for the Council transcript.
    /// </summary>
    /// <value>The transcript summary.</value>
    public string SummaryMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the progression finished.
    /// </summary>
    /// <value><c>true</c> when the game is complete.</value>
    public bool Completed { get; set; }
}
