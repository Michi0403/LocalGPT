namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// The authoritative result of one duel exchange.
/// </summary>
public sealed class DuelExchangeOutcome
{
    /// <summary>Gets or sets the left side.</summary>
    /// <value>The left side outcome.</value>
    public DuelSideOutcome Left { get; set; } = new();

    /// <summary>Gets or sets the right side.</summary>
    /// <value>The right side outcome.</value>
    public DuelSideOutcome Right { get; set; } = new();

    /// <summary>Gets or sets whether this was the opening exchange of the match.</summary>
    /// <value><c>true</c> for the first exchange.</value>
    public bool Opening { get; set; }

    /// <summary>Gets or sets the match winner, when the match was decided.</summary>
    /// <value>The winning owner, or <c>null</c>.</value>
    public GameActorState? Winner { get; set; }

    /// <summary>Gets or sets the match loser, when the match was decided.</summary>
    /// <value>The losing owner, or <c>null</c>.</value>
    public GameActorState? Loser { get; set; }
}
