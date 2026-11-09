using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One declarative rule of a game: when it runs (trigger), which engine rule primitive runs and its parameters.
/// </summary>
public sealed class GameProjectRule
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>Key of the rule, unique within the profile.</summary>
    /// <value>The rule key.</value>
    [MaxLength(80)]
    public string RuleKey { get; set; } = string.Empty;

    /// <summary>When the rule runs: start, action, tick, council or end.</summary>
    /// <value>The trigger name.</value>
    [MaxLength(40)]
    public string Trigger { get; set; } = string.Empty;

    /// <summary>Engine rule primitive that runs.</summary>
    /// <value>The rule primitive key.</value>
    [MaxLength(80)]
    public string PrimitiveKey { get; set; } = string.Empty;

    /// <summary>JSON object with the primitive parameters.</summary>
    /// <value>The parameters as JSON.</value>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>Whether the rule runs.</summary>
    /// <value><see langword="true"/> when the rule runs.</value>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Execution order among rules with the same trigger.</summary>
    /// <value>The sort order.</value>
    public int SortOrder { get; set; }
}
