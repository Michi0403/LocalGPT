namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// A sprite read from a Council answer.
/// </summary>
public sealed class GameSpriteReading
{
    /// <summary>
    /// Gets or sets the sprite rows.
    /// </summary>
    /// <value>Pixel-legend or ASCII rows; empty when the answer had no usable sprite.</value>
    public List<string> Rows { get; set; } = [];

    /// <summary>
    /// Gets or sets the sprite kind.
    /// </summary>
    /// <value><c>pixel</c>, <c>ascii</c> or empty.</value>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the palette entries in legend order.
    /// </summary>
    /// <value>256-colour indexes; empty when the answer declared none.</value>
    public List<int> Palette { get; set; } = [];
}
