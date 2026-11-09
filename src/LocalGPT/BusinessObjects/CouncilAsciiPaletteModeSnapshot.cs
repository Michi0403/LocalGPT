using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Describes one ASCII palette mode and its LocalGPT defaults.</summary>
public sealed class CouncilAsciiPaletteModeSnapshot
{
    /// <summary>
    /// Gets or sets the mode value that forms part of the council ascii palette mode snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mode value exposed by <see cref="CouncilAsciiPaletteModeSnapshot"/>.</value>
    public CouncilAsciiColorMode Mode { get; set; }
    /// <summary>
    /// Gets or sets the description value that forms part of the council ascii palette mode snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="CouncilAsciiPaletteModeSnapshot"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the default foreground color value that forms part of the council ascii palette mode snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default foreground color value exposed by <see cref="CouncilAsciiPaletteModeSnapshot"/>.</value>
    public int DefaultForegroundColor { get; set; }
    /// <summary>
    /// Gets or sets the default background color value that forms part of the council ascii palette mode snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default background color value exposed by <see cref="CouncilAsciiPaletteModeSnapshot"/>.</value>
    public int DefaultBackgroundColor { get; set; }
}
