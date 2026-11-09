namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// A named map position such as an extraction point.
/// </summary>
public sealed class GameMarkerState
{
    /// <summary>Marker key referenced by rules.</summary>
    /// <value>The marker key.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Glyph drawn for the marker.</summary>
    /// <value>The glyph.</value>
    public string Glyph { get; set; } = string.Empty;

    /// <summary>Map column.</summary>
    /// <value>The column.</value>
    public int X { get; set; }

    /// <summary>Map row.</summary>
    /// <value>The row.</value>
    public int Y { get; set; }
}
