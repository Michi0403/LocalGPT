namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Runtime state of one actor in a game session: position, facing, stats, descriptive texts, rig and sprite. Actors are created from archetypes by the engine.
/// </summary>
public sealed class GameActorState
{
    /// <summary>Unique key of the actor within the session.</summary>
    /// <value>The actor key.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Archetype the actor was created from.</summary>
    /// <value>The archetype key.</value>
    public string ArchetypeKey { get; set; } = string.Empty;

    /// <summary>Role copied from the archetype (player, hostile, fighter, creature, trainer, object).</summary>
    /// <value>The actor role.</value>
    public string Role { get; set; } = string.Empty;

    /// <summary>Display name of the actor.</summary>
    /// <value>The display name.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>Glyph drawn for the actor.</summary>
    /// <value>The glyph.</value>
    public string Glyph { get; set; } = string.Empty;

    /// <summary>Key of the actor that owns this one (for example the trainer of a creature).</summary>
    /// <value>The owner actor key.</value>
    public string OwnerKey { get; set; } = string.Empty;

    /// <summary>Map column of the actor.</summary>
    /// <value>The column.</value>
    public int X { get; set; }

    /// <summary>Map row of the actor.</summary>
    /// <value>The row.</value>
    public int Y { get; set; }

    /// <summary>Facing direction in radians (0 = east, clockwise).</summary>
    /// <value>The facing angle.</value>
    public double FacingRadians { get; set; }

    /// <summary>Whether the actor is the active one of its owner (for example the creature in the fight).</summary>
    /// <value><see langword="true"/> when active.</value>
    public bool IsActive { get; set; }

    /// <summary>Whether the actor left the game (defeated or knocked out of a bracket).</summary>
    /// <value><see langword="true"/> when eliminated.</value>
    public bool IsEliminated { get; set; }

    /// <summary>Numeric stats keyed by stat key.</summary>
    /// <value>The stat values.</value>
    public Dictionary<string, int> Stats { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Descriptive texts keyed by name (species, style, voice, last action, ...).</summary>
    /// <value>The descriptive texts.</value>
    public Dictionary<string, string> Texts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Articulated ASCII rig of the actor.</summary>
    /// <value>The rig.</value>
    public CouncilAsciiActorRig Rig { get; set; } = new();

    /// <summary>Sprite rows drawn for the actor (empty for glyph-only actors).</summary>
    /// <value>The sprite rows.</value>
    public List<string> SpriteRows { get; set; } = [];

    /// <summary>Sprite encoding (ascii or pixel).</summary>
    /// <value>The sprite kind.</value>
    public string SpriteKind { get; set; } = string.Empty;

    /// <summary>Palette of a pixel sprite.</summary>
    /// <value>The palette indexes.</value>
    public List<int> SpritePalette { get; set; } = [];
}
