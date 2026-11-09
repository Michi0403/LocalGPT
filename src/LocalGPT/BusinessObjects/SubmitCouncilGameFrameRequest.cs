using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for submit council game frame, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SubmitCouncilGameFrameRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this submit council game frame instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the turn value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The turn value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public long Turn { get; set; }
    /// <summary>
    /// Gets or sets the renderer name value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The renderer name value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public string RendererName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the frame text value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame text value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public string FrameText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the caption value that forms part of the submit council game frame state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The caption value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public string Caption { get; set; } = string.Empty;
    /// <summary>Optionally changes the palette contract for this complete frame.</summary>
    /// <value>The ascii color mode value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public CouncilAsciiColorMode? AsciiColorMode { get; set; }
    /// <summary>Optionally changes the default foreground palette index.</summary>
    /// <value>The default foreground color value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public int? DefaultForegroundColor { get; set; }
    /// <summary>Optionally changes the default background palette index.</summary>
    /// <value>The default background color value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public int? DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets bounded style runs transported separately from canonical frame text.</summary>
    /// <value>The style runs value exposed by <see cref="SubmitCouncilGameFrameRequest"/>.</value>
    public IReadOnlyList<CouncilAsciiStyleRun> StyleRuns { get; set; } = [];
}
