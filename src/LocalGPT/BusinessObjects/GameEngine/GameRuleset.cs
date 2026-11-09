namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// The declarative rules of one game as built from its game project profile: engine primitive keys and parameters plus stats, archetypes, actions, rules, levels, texts and aliases. The generic engine runs a game only from this data.
/// </summary>
public sealed class GameRuleset
{
    /// <summary>Renderer primitive that draws the game (for example first-person-raycast or text-template).</summary>
    /// <value>The view primitive key.</value>
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>JSON parameters of the view primitive.</summary>
    /// <value>The view parameters as JSON.</value>
    public string ViewParametersJson { get; set; } = "{}";

    /// <summary>How turns advance: actions (each control is a turn) or council (council evidence drives the game).</summary>
    /// <value>The turn model key.</value>
    public string TurnModelKey { get; set; } = string.Empty;

    /// <summary>Map generator primitive (empty when the game has no grid map).</summary>
    /// <value>The map generator key.</value>
    public string MapGeneratorKey { get; set; } = string.Empty;

    /// <summary>JSON parameters of the map generator.</summary>
    /// <value>The map parameters as JSON.</value>
    public string MapParametersJson { get; set; } = "{}";

    /// <summary>Autoplay primitive used for AI control (empty for none).</summary>
    /// <value>The autoplay primitive key.</value>
    public string AutoplayKey { get; set; } = string.Empty;

    /// <summary>JSON parameters of the autoplay primitive.</summary>
    /// <value>The autoplay parameters as JSON.</value>
    public string AutoplayParametersJson { get; set; } = "{}";

    /// <summary>Archetype of the controlled actor (empty when nobody is controlled directly).</summary>
    /// <value>The player archetype key.</value>
    public string PlayerArchetypeKey { get; set; } = string.Empty;

    /// <summary>Numeric stats of the game.</summary>
    /// <value>The stat definitions.</value>
    public List<GameProjectStat> Stats { get; set; } = [];

    /// <summary>Actor archetypes of the game.</summary>
    /// <value>The archetype definitions.</value>
    public List<GameProjectArchetype> Archetypes { get; set; } = [];

    /// <summary>Player actions of the game.</summary>
    /// <value>The action definitions.</value>
    public List<GameProjectAction> Actions { get; set; } = [];

    /// <summary>Rules of the game in execution order.</summary>
    /// <value>The rule definitions.</value>
    public List<GameProjectRule> Rules { get; set; } = [];

    /// <summary>Campaign levels in order.</summary>
    /// <value>The level definitions.</value>
    public List<GameProjectLevel> Levels { get; set; } = [];

    /// <summary>User-visible and prompt texts.</summary>
    /// <value>The text definitions.</value>
    public List<GameProjectText> Texts { get; set; } = [];

    /// <summary>Alternative game and action names.</summary>
    /// <value>The alias definitions.</value>
    public List<GameProjectAlias> Aliases { get; set; } = [];
}
