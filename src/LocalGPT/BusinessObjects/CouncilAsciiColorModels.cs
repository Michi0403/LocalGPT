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
    public CouncilAsciiColorMode? ColorMode { get; set; }
    /// <summary>Gets or sets the foreground palette index; null inherits the active surface/default style.</summary>
    public int? ForegroundColor { get; set; }
    /// <summary>Gets or sets the background palette index; null inherits the active surface/default style.</summary>
    public int? BackgroundColor { get; set; }
    /// <summary>Gets or sets whether the run should use bold terminal emphasis.</summary>
    public bool Bold { get; set; }
    /// <summary>Gets or sets whether the run should use dim terminal emphasis.</summary>
    public bool Dim { get; set; }
    /// <summary>Gets or sets whether foreground/background presentation should be inverted.</summary>
    public bool Invert { get; set; }
}

/// <summary>Styles one bounded horizontal run without changing canonical ASCII frame text.</summary>
public sealed class CouncilAsciiStyleRun
{
    /// <summary>Gets or sets the zero-based frame row.</summary>
    public int Y { get; set; }
    /// <summary>Gets or sets the zero-based first frame column.</summary>
    public int X { get; set; }
    /// <summary>Gets or sets the number of cells styled from <see cref="X"/>.</summary>
    public int Length { get; set; } = 1;
    /// <summary>Gets or sets the presentation metadata applied to the run.</summary>
    public CouncilAsciiTextStyle Style { get; set; } = new();
}

/// <summary>Describes the active frame palette and style metadata independently of frame text.</summary>
public sealed class CouncilAsciiFramePresentation
{
    /// <summary>Gets or sets the active palette contract.</summary>
    public CouncilAsciiColorMode ColorMode { get; set; } = CouncilAsciiColorMode.TerminalDefault;
    /// <summary>Gets or sets the default foreground index used by explicit palette modes.</summary>
    public int DefaultForegroundColor { get; set; } = 46;
    /// <summary>Gets or sets the default background index used by explicit palette modes.</summary>
    public int DefaultBackgroundColor { get; set; }
    /// <summary>Gets or sets bounded horizontal style runs for this frame.</summary>
    public IReadOnlyList<CouncilAsciiStyleRun> StyleRuns { get; set; } = [];
}

/// <summary>Compact palette information exposed to local models so they do not need terminal-color prior knowledge.</summary>
public sealed class CouncilAsciiPaletteSnapshot
{
    /// <summary>Gets or sets the supported presentation modes.</summary>
    public IReadOnlyList<CouncilAsciiPaletteModeSnapshot> Modes { get; set; } = [];
    /// <summary>Gets or sets named ANSI-16 colors with stable indexes.</summary>
    public IReadOnlyList<CouncilAsciiNamedColor> Ansi16 { get; set; } = [];
    /// <summary>Gets or sets the minimum indexed-256 value.</summary>
    public int Indexed256Minimum { get; set; }
    /// <summary>Gets or sets the maximum indexed-256 value.</summary>
    public int Indexed256Maximum { get; set; } = 255;
    /// <summary>Gets or sets compact model-facing authoring rules.</summary>
    public IReadOnlyList<string> AuthoringRules { get; set; } = [];
}

/// <summary>Describes one ASCII palette mode and its LocalGPT defaults.</summary>
public sealed class CouncilAsciiPaletteModeSnapshot
{
    public CouncilAsciiColorMode Mode { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DefaultForegroundColor { get; set; }
    public int DefaultBackgroundColor { get; set; }
}

/// <summary>Names one stable ANSI-16 palette entry.</summary>
public sealed class CouncilAsciiNamedColor
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
}
