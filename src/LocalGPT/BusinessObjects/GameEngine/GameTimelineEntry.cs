namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One recorded event of a game session (for example an exchange of a match).
/// </summary>
public sealed class GameTimelineEntry
{
    /// <summary>Sequence number.</summary>
    /// <value>The sequence number.</value>
    public int Sequence { get; set; }

    /// <summary>Round of the event.</summary>
    /// <value>The round.</value>
    public int Round { get; set; }

    /// <summary>Match of the event.</summary>
    /// <value>The match.</value>
    public int Match { get; set; }

    /// <summary>Exchange of the event.</summary>
    /// <value>The exchange.</value>
    public int Exchange { get; set; }

    /// <summary>Kind of event.</summary>
    /// <value>The event kind.</value>
    public string EventKind { get; set; } = string.Empty;

    /// <summary>State summary of the left side.</summary>
    /// <value>The left state.</value>
    public string LeftState { get; set; } = string.Empty;

    /// <summary>State summary of the right side.</summary>
    /// <value>The right state.</value>
    public string RightState { get; set; } = string.Empty;

    /// <summary>Event detail.</summary>
    /// <value>The detail.</value>
    public string Detail { get; set; } = string.Empty;
}
