using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// A kind of actor in a game (player, hostile, fighter, creature, trainer or object): glyph, behaviour primitive and parameters such as stats, damage, rig or sprite.
/// </summary>
public sealed class GameProjectArchetype
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>Key rules and maps use to spawn this kind of actor.</summary>
    /// <value>The archetype key, unique within the profile.</value>
    [MaxLength(80)]
    public string ArchetypeKey { get; set; } = string.Empty;

    /// <summary>Name template of spawned actors; may contain {index} tokens.</summary>
    /// <value>The display name template.</value>
    [MaxLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Role of the actor (player, hostile, fighter, creature, trainer, object).</summary>
    /// <value>The actor role.</value>
    [MaxLength(40)]
    public string Role { get; set; } = string.Empty;

    /// <summary>Character drawn for the actor on maps and in sprites.</summary>
    /// <value>The glyph.</value>
    [MaxLength(8)]
    public string Glyph { get; set; } = string.Empty;

    /// <summary>Engine behaviour primitive that moves the actor (empty for none).</summary>
    /// <value>The behaviour primitive key.</value>
    [MaxLength(80)]
    public string BehaviourKey { get; set; } = string.Empty;

    /// <summary>JSON object with the archetype parameters (stats, damage, placement, rig, sprite).</summary>
    /// <value>The parameters as JSON.</value>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>Order used when several archetypes compete for a placement.</summary>
    /// <value>The sort order.</value>
    public int SortOrder { get; set; }
}
