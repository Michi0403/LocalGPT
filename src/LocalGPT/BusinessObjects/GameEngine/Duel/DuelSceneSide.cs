namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// One side of a rendered duel scene.
/// </summary>
public sealed class DuelSceneSide
{
    /// <summary>Gets or sets the short owner label.</summary>
    /// <value>The owner label.</value>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the shown owner action.</summary>
    /// <value>The owner action label.</value>
    public string OwnerAction { get; set; } = string.Empty;

    /// <summary>Gets or sets the active member name.</summary>
    /// <value>The member name.</value>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>Gets or sets the short member species.</summary>
    /// <value>The species label.</value>
    public string Species { get; set; } = string.Empty;

    /// <summary>Gets or sets the shown member health.</summary>
    /// <value>The health.</value>
    public int Health { get; set; }

    /// <summary>Gets or sets the spoken statement.</summary>
    /// <value>The statement.</value>
    public string Statement { get; set; } = string.Empty;

    /// <summary>Gets or sets the switches used in the match.</summary>
    /// <value>The used switches.</value>
    public int SwitchesUsed { get; set; }

    /// <summary>Gets or sets the team health and active flags in slot order.</summary>
    /// <value>The roster entries.</value>
    public List<(int Health, bool Active)> Roster { get; set; } = [];

    /// <summary>Gets or sets the active member sprite rows.</summary>
    /// <value>The sprite rows.</value>
    public List<string> SpriteRows { get; set; } = [];

    /// <summary>Gets or sets the sprite kind.</summary>
    /// <value><c>pixel</c>, <c>ascii</c> or empty.</value>
    public string SpriteKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the sprite palette.</summary>
    /// <value>The palette entries.</value>
    public List<int> SpritePalette { get; set; } = [];

    /// <summary>Gets or sets the short label of the sprite author.</summary>
    /// <value>The sprite author label.</value>
    public string SpriteAuthor { get; set; } = string.Empty;

    /// <summary>Gets or sets the pose.</summary>
    /// <value>The pose key.</value>
    public string Pose { get; set; } = "idle";
}
