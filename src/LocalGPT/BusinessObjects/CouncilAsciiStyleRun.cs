using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Styles one bounded horizontal run without changing canonical ASCII frame text.</summary>
public sealed class CouncilAsciiStyleRun
{
    /// <summary>
    /// Gets or sets the y value that forms part of the council ascii style run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The y value exposed by <see cref="CouncilAsciiStyleRun"/>.</value>
    public int Y { get; set; }
    /// <summary>
    /// Gets or sets the x value that forms part of the council ascii style run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The x value exposed by <see cref="CouncilAsciiStyleRun"/>.</value>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the length that quantifies the associated council ascii style run data.
    /// </summary>
    /// <value>The length value exposed by <see cref="CouncilAsciiStyleRun"/>.</value>
    public int Length { get; set; } = 1;
    /// <summary>
    /// Gets or sets the style value that forms part of the council ascii style run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The style value exposed by <see cref="CouncilAsciiStyleRun"/>.</value>
    public CouncilAsciiTextStyle Style { get; set; } = new();
}
