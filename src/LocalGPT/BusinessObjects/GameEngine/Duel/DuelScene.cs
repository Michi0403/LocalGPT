namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// A duel scene handed to the battle renderer.
/// </summary>
public sealed class DuelScene
{
    /// <summary>Gets or sets the headline.</summary>
    /// <value>The headline.</value>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Gets or sets the body.</summary>
    /// <value>The body.</value>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the footer.</summary>
    /// <value>The footer.</value>
    public string Footer { get; set; } = string.Empty;

    /// <summary>Gets or sets the centre effect.</summary>
    /// <value>The centre effect.</value>
    public string CenterEffect { get; set; } = string.Empty;

    /// <summary>Gets or sets the heads-up motif.</summary>
    /// <value>The heads-up motif.</value>
    public string HudMotif { get; set; } = string.Empty;

    /// <summary>Gets or sets the round.</summary>
    /// <value>The round number.</value>
    public int Round { get; set; }

    /// <summary>Gets or sets the exchange.</summary>
    /// <value>The exchange number.</value>
    public int Exchange { get; set; }

    /// <summary>Gets or sets the starting health.</summary>
    /// <value>The starting health.</value>
    public int StartingHealth { get; set; } = 100;

    /// <summary>Gets or sets the switch budget.</summary>
    /// <value>The switch budget.</value>
    public int MaximumSwitches { get; set; }

    /// <summary>Gets or sets the most recent timeline line.</summary>
    /// <value>The recent timeline line.</value>
    public string TimelineRecent { get; set; } = string.Empty;

    /// <summary>Gets or sets the previous timeline line.</summary>
    /// <value>The previous timeline line.</value>
    public string TimelinePrevious { get; set; } = string.Empty;

    /// <summary>Gets or sets the left side.</summary>
    /// <value>The left side, or <c>null</c>.</value>
    public DuelSceneSide? Left { get; set; }

    /// <summary>Gets or sets the right side.</summary>
    /// <value>The right side, or <c>null</c>.</value>
    public DuelSceneSide? Right { get; set; }

    /// <summary>Gets or sets whether both sides are the same entrant (champion scenes).</summary>
    /// <value><c>true</c> for single-entrant scenes.</value>
    public bool SameEntrant { get; set; }
}
