namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Progress of a session through levels or a bracket.
/// </summary>
public sealed class GameProgressionState
{
    /// <summary>Current level index.</summary>
    /// <value>The level index.</value>
    public int LevelIndex { get; set; }

    /// <summary>Whether the next level starts when a level is completed.</summary>
    /// <value><see langword="true"/> when levels advance automatically.</value>
    public bool AutoAdvanceLevels { get; set; } = true;

    /// <summary>Current bracket round (1-based once started).</summary>
    /// <value>The round.</value>
    public int Round { get; set; }

    /// <summary>Current match within the round (0-based).</summary>
    /// <value>The match index.</value>
    public int Match { get; set; }

    /// <summary>Current exchange within the match.</summary>
    /// <value>The exchange.</value>
    public int Exchange { get; set; }

    /// <summary>Whether the bracket or campaign has been initialized.</summary>
    /// <value><see langword="true"/> when initialized.</value>
    public bool Initialized { get; set; }

    /// <summary>Actor key of the champion once decided.</summary>
    /// <value>The champion actor key.</value>
    public string ChampionKey { get; set; } = string.Empty;

    /// <summary>Actor keys entered in the current round.</summary>
    /// <value>The current round entrants.</value>
    public List<string> RoundEntrantKeys { get; set; } = [];

    /// <summary>Actor keys that advanced to the next round.</summary>
    /// <value>The next round entrants.</value>
    public List<string> NextRoundEntrantKeys { get; set; } = [];

    /// <summary>Sequence number of the last timeline entry.</summary>
    /// <value>The timeline sequence.</value>
    public int TimelineSequence { get; set; }
}
