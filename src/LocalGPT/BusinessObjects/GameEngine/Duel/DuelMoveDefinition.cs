namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// One member move of a team duel, recognised from Council evidence by keywords.
/// </summary>
public sealed class DuelMoveDefinition
{
    /// <summary>Gets or sets the move key shown in frames and the transcript.</summary>
    /// <value>The move key.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the keywords that select the move; earlier moves win.</summary>
    /// <value>The keywords.</value>
    public List<string> Words { get; set; } = [];

    /// <summary>Gets or sets whether the move is chosen when no keyword matches.</summary>
    /// <value><c>true</c> for the default move.</value>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether the move deals stable random damage.</summary>
    /// <value><c>true</c> for attacking moves.</value>
    public bool Attacks { get; set; }

    /// <summary>Gets or sets the health the move restores to the acting member.</summary>
    /// <value>The heal amount.</value>
    public int Heal { get; set; }

    /// <summary>Gets or sets the damage the move removes from the opponent's attack.</summary>
    /// <value>The incoming damage reduction.</value>
    public int IncomingReduction { get; set; }

    /// <summary>Gets or sets the rig pose of the move.</summary>
    /// <value>The pose key.</value>
    public string Pose { get; set; } = "idle";
}
