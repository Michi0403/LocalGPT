namespace LocalGPT.BusinessObjects;

/// <summary>Requests a full or cropped readback of the authoritative Council ASCII display.</summary>
public sealed class ReadCouncilGameDisplayRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this read council game display instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="ReadCouncilGameDisplayRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the x value that forms part of the read council game display state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="ReadCouncilGameDisplayRequest"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the read council game display state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="ReadCouncilGameDisplayRequest"/>.</value>
    public int Y { get; set; }
    /// <summary>Gets or sets the requested width; zero means through the right edge.</summary>
    /// <value>The width value exposed by <see cref="ReadCouncilGameDisplayRequest"/>.</value>
    public int Width { get; set; }
    /// <summary>Gets or sets the requested height; zero means through the bottom edge.</summary>
    /// <value>The height value exposed by <see cref="ReadCouncilGameDisplayRequest"/>.</value>
    public int Height { get; set; }
}

/// <summary>Requests bounded text output on the authoritative Council ASCII display.</summary>
public sealed class WriteCouncilGameTextRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this write council game text instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the write council game text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    /// <value>The renderer name value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the x value that forms part of the write council game text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the write council game text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public int Y { get; set; }
    /// <summary>Gets or sets text to write; line breaks advance display rows.</summary>
    /// <value>The text value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the style value that forms part of the write council game text state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The style value exposed by <see cref="WriteCouncilGameTextRequest"/>.</value>
    public CouncilAsciiTextStyle? Style { get; set; }
}

/// <summary>Requests one display-cell mutation.</summary>
public sealed class SetCouncilGameCellRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this set council game cell instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the set council game cell state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    /// <value>The renderer name value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the x value that forms part of the set council game cell state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the set council game cell state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public int Y { get; set; }
    /// <summary>Gets or sets the glyph; the first Unicode character is used.</summary>
    /// <value>The glyph value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public string Glyph { get; set; } = " ";
    /// <summary>
    /// Gets or sets the style value that forms part of the set council game cell state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The style value exposed by <see cref="SetCouncilGameCellRequest"/>.</value>
    public CouncilAsciiTextStyle? Style { get; set; }
}

/// <summary>Requests a rectangular display region to be filled with one glyph.</summary>
public sealed class FillCouncilGameRegionRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this fill council game region instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the fill council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    /// <value>The renderer name value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the x value that forms part of the fill council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the fill council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public int Y { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the fill council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height value that forms part of the fill council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the glyph value that forms part of the fill council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The glyph value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public string Glyph { get; set; } = " ";
    /// <summary>Gets or sets optional presentation metadata applied to every filled cell.</summary>
    /// <value>The style value exposed by <see cref="FillCouncilGameRegionRequest"/>.</value>
    public CouncilAsciiTextStyle? Style { get; set; }
}

/// <summary>Requests a multiline ASCII block to be blitted into a display region.</summary>
public sealed class BlitCouncilGameRegionRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this blit council game region instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the blit council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>Gets or sets the renderer that owns display mutations for this turn.</summary>
    /// <value>The renderer name value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the x value that forms part of the blit council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the blit council game region state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public int Y { get; set; }
    /// <summary>Gets or sets multiline ASCII content. Content is clipped to the display.</summary>
    /// <value>The text value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>Gets or sets optional presentation metadata applied to each non-empty blitted row segment.</summary>
    /// <value>The style value exposed by <see cref="BlitCouncilGameRegionRequest"/>.</value>
    public CouncilAsciiTextStyle? Style { get; set; }
}

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

/// <summary>Returns a full or cropped read-only snapshot of the Council ASCII display.</summary>
public sealed class CouncilGameDisplaySnapshot
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this council game display snapshot instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets the frame width value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame width value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int FrameWidth { get; set; }
    /// <summary>
    /// Gets or sets the frame height value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame height value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int FrameHeight { get; set; }
    /// <summary>
    /// Gets or sets the x value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the y value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int Y { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the text value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ascii color mode value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ascii color mode value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public CouncilAsciiColorMode AsciiColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>
    /// Gets or sets the default foreground color value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default foreground color value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>
    /// Gets or sets the default background color value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default background color value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets clipped style runs rebased to this returned display region.</summary>
    /// <value>The style runs value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public IReadOnlyList<CouncilAsciiStyleRun> StyleRuns { get; set; } = [];
    /// <summary>
    /// Gets or sets the renderer name value that forms part of the council game display snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The renderer name value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>Gets or sets supported read/write display functions for AI discovery.</summary>
    /// <value>The supported methods value exposed by <see cref="CouncilGameDisplaySnapshot"/>.</value>
    public IReadOnlyList<string> SupportedMethods { get; set; } = [];
}
