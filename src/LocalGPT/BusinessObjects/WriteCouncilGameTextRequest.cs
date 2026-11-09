namespace LocalGPT.BusinessObjects;

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
