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
