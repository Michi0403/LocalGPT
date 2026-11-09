namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Replaces the declarative rule rows of one game project profile. Rows are game content; the engine only interprets them.
/// </summary>
public sealed class SaveGameRulesetRequest
{
    /// <summary>
    /// Gets or sets whether the user explicitly confirmed replacing the rule rows.
    /// </summary>
    /// <value><c>true</c> when the replacement was confirmed.</value>
    public bool UserConfirmed { get; set; }

    /// <summary>
    /// Gets or sets the stat rows.
    /// </summary>
    /// <value>The stats to persist.</value>
    public List<GameProjectStat> Stats { get; set; } = [];

    /// <summary>
    /// Gets or sets the archetype rows.
    /// </summary>
    /// <value>The archetypes to persist.</value>
    public List<GameProjectArchetype> Archetypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the action rows.
    /// </summary>
    /// <value>The actions to persist.</value>
    public List<GameProjectAction> Actions { get; set; } = [];

    /// <summary>
    /// Gets or sets the rule rows.
    /// </summary>
    /// <value>The rules to persist.</value>
    public List<GameProjectRule> Rules { get; set; } = [];

    /// <summary>
    /// Gets or sets the level rows.
    /// </summary>
    /// <value>The levels to persist.</value>
    public List<GameProjectLevel> Levels { get; set; } = [];

    /// <summary>
    /// Gets or sets the text rows.
    /// </summary>
    /// <value>The texts to persist.</value>
    public List<GameProjectText> Texts { get; set; } = [];

    /// <summary>
    /// Gets or sets the alias rows.
    /// </summary>
    /// <value>The aliases to persist.</value>
    public List<GameProjectAlias> Aliases { get; set; } = [];
}
