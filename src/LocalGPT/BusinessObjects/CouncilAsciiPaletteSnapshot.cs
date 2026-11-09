using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

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
