namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// One owner (team lead) action of a team duel, recognised from Council evidence by keywords.
/// </summary>
public sealed class DuelOwnerActionDefinition
{
    /// <summary>Gets or sets the action key.</summary>
    /// <value>The action key.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the keywords that select the action; earlier actions win.</summary>
    /// <value>The keywords.</value>
    public List<string> Words { get; set; } = [];

    /// <summary>Gets or sets whether the action is chosen when no keyword matches.</summary>
    /// <value><c>true</c> for the default action.</value>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether the action switches the active member (counting against the switch budget).</summary>
    /// <value><c>true</c> for switch actions.</value>
    public bool Switches { get; set; }

    /// <summary>Gets or sets the damage added to the own member's attack.</summary>
    /// <value>The attack bonus.</value>
    public int AttackBonus { get; set; }

    /// <summary>Gets or sets the damage removed from the opponent's attack.</summary>
    /// <value>The incoming damage reduction.</value>
    public int IncomingReduction { get; set; }

    /// <summary>Gets or sets the health restored to the own active member.</summary>
    /// <value>The heal amount.</value>
    public int Heal { get; set; }
}
