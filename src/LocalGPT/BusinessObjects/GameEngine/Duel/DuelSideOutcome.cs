namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// What one entrant side did and suffered in a resolved exchange.
/// </summary>
public sealed class DuelSideOutcome
{
    /// <summary>Gets or sets the owner actor of the side.</summary>
    /// <value>The owner actor.</value>
    public GameActorState Owner { get; set; } = new();

    /// <summary>Gets or sets the owner evidence of the exchange.</summary>
    /// <value>The owner answer text.</value>
    public string OwnerEvidence { get; set; } = string.Empty;

    /// <summary>Gets or sets the member evidence of the exchange.</summary>
    /// <value>The member answer text.</value>
    public string MemberEvidence { get; set; } = string.Empty;

    /// <summary>Gets or sets the resolved owner action.</summary>
    /// <value>The owner action definition.</value>
    public DuelOwnerActionDefinition Action { get; set; } = new();

    /// <summary>Gets or sets the resolved member move.</summary>
    /// <value>The move definition.</value>
    public DuelMoveDefinition Move { get; set; } = new();

    /// <summary>Gets or sets the active member health before the exchange.</summary>
    /// <value>The health before damage.</value>
    public int HealthBefore { get; set; }

    /// <summary>Gets or sets the damage this side dealt.</summary>
    /// <value>The dealt damage.</value>
    public int DamageDealt { get; set; }

    /// <summary>Gets or sets whether the owner switched the active member by command.</summary>
    /// <value><c>true</c> after a commanded switch.</value>
    public bool Switched { get; set; }

    /// <summary>Gets or sets whether the engine swapped in a healthy reserve after a knock-out.</summary>
    /// <value><c>true</c> after a forced switch.</value>
    public bool AutoSwitched { get; set; }

    /// <summary>Gets or sets the tagged evidence values by token name.</summary>
    /// <value>The tagged values.</value>
    public Dictionary<string, string> Tagged { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
