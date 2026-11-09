namespace LocalGPT.BusinessObjects;

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
