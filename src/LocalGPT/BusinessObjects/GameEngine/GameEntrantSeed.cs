namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One Council-authored entrant handed to a council-driven game: an owner member (for example a trainer or captain)
/// paired with one team member model, plus the identity texts and the sprite the members wrote for it.
/// </summary>
public sealed class GameEntrantSeed
{
    /// <summary>
    /// Gets or sets the Council model that owns the entrant and issues its commands.
    /// </summary>
    /// <value>The owner model selection name.</value>
    public string OwnerModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Council model that plays the entrant's team member.
    /// </summary>
    /// <value>The member model selection name.</value>
    public string MemberModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity texts read from the members' answers, keyed by the field names declared in the game's council rule.
    /// </summary>
    /// <value>The entrant texts such as owner greeting or member name, species and voice.</value>
    public Dictionary<string, string> Texts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the sprite rows drawn for the member.
    /// </summary>
    /// <value>Pixel-legend or ASCII rows; empty when no usable drawing exists.</value>
    public List<string> SpriteRows { get; set; } = [];

    /// <summary>
    /// Gets or sets the sprite kind, <c>pixel</c> or <c>ascii</c>.
    /// </summary>
    /// <value>The sprite kind or empty when no sprite exists.</value>
    public string SpriteKind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the 256-colour palette indexes of the pixel legend.
    /// </summary>
    /// <value>The palette entries in legend order.</value>
    public List<int> SpritePalette { get; set; } = [];

    /// <summary>
    /// Gets or sets the member that drew the sprite when it was not the entrant's own member.
    /// </summary>
    /// <value>The drawing model, or empty when the member drew its own sprite.</value>
    public string SpriteAuthorModelName { get; set; } = string.Empty;
}
