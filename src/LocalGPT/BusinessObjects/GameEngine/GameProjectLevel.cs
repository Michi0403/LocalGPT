using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One level of a game campaign: name, difficulty, map size and parameters such as room count, hostile density, multipliers and starting stats.
/// </summary>
public sealed class GameProjectLevel
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>Zero-based position of the level in the campaign.</summary>
    /// <value>The level index.</value>
    public int LevelIndex { get; set; }

    /// <summary>Level number shown to players.</summary>
    /// <value>The level number.</value>
    public int LevelNumber { get; set; } = 1;

    /// <summary>Level name.</summary>
    /// <value>The level name.</value>
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Difficulty from 1 to 10.</summary>
    /// <value>The difficulty.</value>
    public int Difficulty { get; set; } = 1;

    /// <summary>Map width in cells.</summary>
    /// <value>The map width.</value>
    public int MapWidth { get; set; } = 28;

    /// <summary>Map height in cells.</summary>
    /// <value>The map height.</value>
    public int MapHeight { get; set; } = 18;

    /// <summary>JSON object with level parameters (room count, hostile density, multipliers, starting stats).</summary>
    /// <value>The parameters as JSON.</value>
    public string ParametersJson { get; set; } = "{}";
}
