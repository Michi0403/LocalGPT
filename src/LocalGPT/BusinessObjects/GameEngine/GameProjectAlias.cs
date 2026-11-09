using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// An alternative name that resolves to a game key or an action key (for example "doom" or "fire").
/// </summary>
public sealed class GameProjectAlias
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>What the alias names: game or action.</summary>
    /// <value>The alias kind.</value>
    [MaxLength(20)]
    public string AliasKind { get; set; } = string.Empty;

    /// <summary>Alternative name as typed by users or models.</summary>
    /// <value>The alias.</value>
    [MaxLength(120)]
    public string Alias { get; set; } = string.Empty;

    /// <summary>Game key or action key the alias resolves to.</summary>
    /// <value>The target key.</value>
    [MaxLength(160)]
    public string Target { get; set; } = string.Empty;
}
