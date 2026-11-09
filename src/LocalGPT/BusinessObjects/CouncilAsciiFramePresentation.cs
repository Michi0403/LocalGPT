using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Describes the active frame palette and style metadata independently of frame text.</summary>
public sealed class CouncilAsciiFramePresentation
{
    /// <summary>
    /// Gets or sets the color mode value that forms part of the council ascii frame presentation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color mode value exposed by <see cref="CouncilAsciiFramePresentation"/>.</value>
    public CouncilAsciiColorMode ColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Gets or sets the default foreground index used by explicit palette modes.</summary>
    /// <value>The default foreground color value exposed by <see cref="CouncilAsciiFramePresentation"/>.</value>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Gets or sets the default background index used by explicit palette modes.</summary>
    /// <value>The default background color value exposed by <see cref="CouncilAsciiFramePresentation"/>.</value>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>
    /// Gets or sets the style runs collection maintained or exposed by this council ascii frame presentation instance for downstream processing.
    /// </summary>
    /// <value>The style runs value exposed by <see cref="CouncilAsciiFramePresentation"/>.</value>
    public IReadOnlyList<CouncilAsciiStyleRun> StyleRuns { get; set; } = [];
}
