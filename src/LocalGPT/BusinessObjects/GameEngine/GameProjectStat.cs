using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One numeric stat of a game (for example health or ammunition) with its bounds, start value and HUD label. Part of the declarative game rules owned by a game project profile.
/// </summary>
public sealed class GameProjectStat
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>Key the rules, texts and renderers use for this stat.</summary>
    /// <value>The stat key, unique within the profile.</value>
    [MaxLength(80)]
    public string StatKey { get; set; } = string.Empty;

    /// <summary>Label shown in HUDs and editors.</summary>
    /// <value>The display name.</value>
    [MaxLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Lowest value the engine allows.</summary>
    /// <value>The minimum value.</value>
    public int MinimumValue { get; set; }

    /// <summary>Highest value the engine allows.</summary>
    /// <value>The maximum value.</value>
    public int MaximumValue { get; set; } = 999;

    /// <summary>Value at the start of a session or level unless a level overrides it.</summary>
    /// <value>The initial value.</value>
    public int InitialValue { get; set; }

    /// <summary>Display order in HUDs and editors.</summary>
    /// <value>The sort order.</value>
    public int SortOrder { get; set; }
}
