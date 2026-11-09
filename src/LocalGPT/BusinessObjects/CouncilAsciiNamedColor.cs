using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

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
