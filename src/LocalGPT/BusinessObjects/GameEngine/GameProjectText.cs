using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One user-visible or prompt text of a game. Texts may contain {token} placeholders the engine fills in.
/// </summary>
public sealed class GameProjectText
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>Key the engine uses to look up the text.</summary>
    /// <value>The text key, unique within the profile.</value>
    [MaxLength(120)]
    public string TextKey { get; set; } = string.Empty;

    /// <summary>Text template.</summary>
    /// <value>The text template.</value>
    public string Text { get; set; } = string.Empty;
}
