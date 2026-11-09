namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// One storyboard frame to render: the two sides, their poses and health, and the frame texts.
/// </summary>
public sealed class DuelFrameRequest
{
    /// <summary>Gets or sets the headline.</summary>
    /// <value>The frame headline.</value>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Gets or sets the left owner, or <c>null</c>.</summary>
    /// <value>The left owner actor.</value>
    public GameActorState? Left { get; set; }

    /// <summary>Gets or sets the right owner, or <c>null</c>.</summary>
    /// <value>The right owner actor.</value>
    public GameActorState? Right { get; set; }

    /// <summary>Gets or sets the body line.</summary>
    /// <value>The body text.</value>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the footer line.</summary>
    /// <value>The footer text.</value>
    public string Footer { get; set; } = string.Empty;

    /// <summary>Gets or sets the left pose.</summary>
    /// <value>The left pose key.</value>
    public string LeftPose { get; set; } = "idle";

    /// <summary>Gets or sets the right pose.</summary>
    /// <value>The right pose key.</value>
    public string RightPose { get; set; } = "idle";

    /// <summary>Gets or sets the shown left health, or <c>null</c> for the current health.</summary>
    /// <value>The left health override.</value>
    public int? LeftHealth { get; set; }

    /// <summary>Gets or sets the shown right health, or <c>null</c> for the current health.</summary>
    /// <value>The right health override.</value>
    public int? RightHealth { get; set; }

    /// <summary>Gets or sets the centre effect.</summary>
    /// <value>The centre effect text.</value>
    public string CenterEffect { get; set; } = string.Empty;

    /// <summary>Gets or sets the heads-up motif.</summary>
    /// <value>The heads-up text.</value>
    public string HudMotif { get; set; } = string.Empty;

    /// <summary>Gets or sets the left statement; empty shows the owner's greeting.</summary>
    /// <value>The left statement.</value>
    public string LeftStatement { get; set; } = string.Empty;

    /// <summary>Gets or sets the right statement; empty shows the owner's greeting.</summary>
    /// <value>The right statement.</value>
    public string RightStatement { get; set; } = string.Empty;
}
