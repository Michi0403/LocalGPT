namespace LocalGPT.BusinessObjects;

/// <summary>Requests a pregenerated local ASCII animation for one Council display turn.</summary>
public sealed class SubmitCouncilGameAnimationRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this submit council game animation instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the submit council game animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets the renderer name value that forms part of the submit council game animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The renderer name value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets two through twelve complete frames generated before playback.</summary>
    /// <value>The frames value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public IReadOnlyList<string> Frames { get; set; } = [];
    /// <summary>
    /// Gets or sets the delay milliseconds value that forms part of the submit council game animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delay milliseconds value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public int DelayMilliseconds { get; set; } = 650;
    /// <summary>
    /// Gets or sets the caption value that forms part of the submit council game animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The caption value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public string Caption { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional movie subtitle rendered over the game wall.</summary>
    /// <value>The subtitle value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public string Subtitle { get; set; } = string.Empty;
    /// <summary>Gets or sets how long the subtitle remains visible after the final frame.</summary>
    /// <value>The subtitle hold milliseconds value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public int SubtitleHoldMilliseconds { get; set; } = 1500;
    /// <summary>Optionally changes the palette contract for this animation and its first/stable frame.</summary>
    /// <value>The ascii color mode value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public CouncilAsciiColorMode? AsciiColorMode { get; set; }
    /// <summary>Optionally changes the default foreground palette index.</summary>
    /// <value>The default foreground color value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public int? DefaultForegroundColor { get; set; }
    /// <summary>Optionally changes the default background palette index.</summary>
    /// <value>The default background color value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public int? DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets bounded style runs for each submitted frame, by frame index.</summary>
    /// <value>The frame style runs value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public IReadOnlyList<IReadOnlyList<CouncilAsciiStyleRun>> FrameStyleRuns { get; set; } = [];
    /// <summary>
    /// Gets or sets the subtitle style value that forms part of the submit council game animation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The subtitle style value exposed by <see cref="SubmitCouncilGameAnimationRequest"/>.</value>
    public CouncilAsciiTextStyle? SubtitleStyle { get; set; }
}
