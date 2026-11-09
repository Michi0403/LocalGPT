namespace LocalGPT.BusinessObjects;

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
