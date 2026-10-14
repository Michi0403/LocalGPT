using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Selects the palette contract used by the shared LocalGPT ASCII game surface.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilAsciiColorMode
{
    /// <summary>Preserves the existing LocalGPT terminal theme for unstyled cells.</summary>
    TerminalDefault = 0,
    /// <summary>Uses the standard sixteen ANSI terminal colors.</summary>
    Ansi16 = 1,
    /// <summary>Uses xterm-compatible indexed colors in the range 0 through 255.</summary>
    Indexed256 = 2
}

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

/// <summary>Compact palette information exposed to local models so they do not need terminal-color prior knowledge.</summary>
public sealed class CouncilAsciiPaletteSnapshot
{
    /// <summary>
    /// Gets or sets the modes collection maintained or exposed by this council ascii palette snapshot instance for downstream processing.
    /// </summary>
    /// <value>The modes value exposed by <see cref="CouncilAsciiPaletteSnapshot"/>.</value>
    public IReadOnlyList<CouncilAsciiPaletteModeSnapshot> Modes { get; set; } = [];
    /// <summary>
    /// Gets or sets the ansi16 collection maintained or exposed by this council ascii palette snapshot instance for downstream processing.
    /// </summary>
    /// <value>The ansi16 value exposed by <see cref="CouncilAsciiPaletteSnapshot"/>.</value>
    public IReadOnlyList<CouncilAsciiNamedColor> Ansi16 { get; set; } = [];
    /// <summary>
    /// Gets or sets the indexed256 minimum value that forms part of the council ascii palette snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The indexed256 minimum value exposed by <see cref="CouncilAsciiPaletteSnapshot"/>.</value>
    public int Indexed256Minimum { get; set; }
    /// <summary>
    /// Gets or sets the indexed256 maximum value that forms part of the council ascii palette snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The indexed256 maximum value exposed by <see cref="CouncilAsciiPaletteSnapshot"/>.</value>
    public int Indexed256Maximum { get; set; } = 255;
    /// <summary>
    /// Gets or sets the authoring rules collection maintained or exposed by this council ascii palette snapshot instance for downstream processing.
    /// </summary>
    /// <value>The authoring rules value exposed by <see cref="CouncilAsciiPaletteSnapshot"/>.</value>
    public IReadOnlyList<string> AuthoringRules { get; set; } = [];
}

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

/// <summary>Names one stable ANSI-16 palette entry.</summary>
public sealed class CouncilAsciiNamedColor
{
    /// <summary>
    /// Gets or sets the index value that forms part of the council ascii named color state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The index value exposed by <see cref="CouncilAsciiNamedColor"/>.</value>
    public int Index { get; set; }
    /// <summary>
    /// Gets or sets the name value that forms part of the council ascii named color state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="CouncilAsciiNamedColor"/>.</value>
    public string Name { get; set; } = string.Empty;
}
