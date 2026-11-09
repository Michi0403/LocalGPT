using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Presentation-only style metadata for one or more ASCII cells.</summary>
public sealed class CouncilAsciiTextStyle
{
    /// <summary>Optionally overrides the session palette for this style run.</summary>
    /// <value>The color mode value exposed by <see cref="CouncilAsciiTextStyle"/>.</value>
    public CouncilAsciiColorMode? ColorMode { get; set; }
    /// <summary>Gets or sets the foreground palette index; null inherits the active surface/default style.</summary>
    /// <value>The foreground color value exposed by <see cref="CouncilAsciiTextStyle"/>.</value>
    public int? ForegroundColor { get; set; }
    /// <summary>Gets or sets the background palette index; null inherits the active surface/default style.</summary>
    /// <value>The background color value exposed by <see cref="CouncilAsciiTextStyle"/>.</value>
    public int? BackgroundColor { get; set; }
    /// <summary>Gets or sets whether the run should use bold terminal emphasis.</summary>
    /// <value>The bold value exposed by <see cref="CouncilAsciiTextStyle"/>.</value>
    public bool Bold { get; set; }
    /// <summary>Gets or sets whether the run should use dim terminal emphasis.</summary>
    /// <value>The dim value exposed by <see cref="CouncilAsciiTextStyle"/>.</value>
    public bool Dim { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether invert applies to the council ascii text style state.
    /// </summary>
    /// <value>The invert value exposed by <see cref="CouncilAsciiTextStyle"/>.</value>
    public bool Invert { get; set; }
}
