namespace LocalGPT.BusinessObjects;

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
