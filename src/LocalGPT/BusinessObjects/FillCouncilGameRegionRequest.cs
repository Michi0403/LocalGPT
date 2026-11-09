namespace LocalGPT.BusinessObjects;

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
