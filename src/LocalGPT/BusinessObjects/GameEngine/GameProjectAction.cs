using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One player action of a game: label, input bindings and the engine action primitive with its parameters.
/// </summary>
public sealed class GameProjectAction
{
    /// <summary>Stable row identifier.</summary>
    /// <value>The row identifier.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Game project profile that owns this row.</summary>
    /// <value>The owning <see cref="LocalGptGameProjectProfile"/> identifier.</value>
    public Guid ProfileId { get; set; }

    /// <summary>Key of the action used by controls, AI controllers and the director.</summary>
    /// <value>The action key, unique within the profile.</value>
    [MaxLength(80)]
    public string ActionKey { get; set; } = string.Empty;

    /// <summary>Label of the action in control help.</summary>
    /// <value>The display name.</value>
    [MaxLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Keyboard binding shown to players.</summary>
    /// <value>The keyboard binding text.</value>
    [MaxLength(80)]
    public string KeyboardKey { get; set; } = string.Empty;

    /// <summary>Gamepad binding shown to players.</summary>
    /// <value>The gamepad binding text.</value>
    [MaxLength(80)]
    public string GamepadButton { get; set; } = string.Empty;

    /// <summary>Engine action primitive that executes the action.</summary>
    /// <value>The action primitive key.</value>
    [MaxLength(80)]
    public string PrimitiveKey { get; set; } = string.Empty;

    /// <summary>JSON object with the primitive parameters.</summary>
    /// <value>The parameters as JSON.</value>
    public string ParametersJson { get; set; } = "{}";

    /// <summary>Whether AI controllers may choose this action.</summary>
    /// <value><see langword="true"/> when AI controllers may choose it.</value>
    public bool AiSelectable { get; set; } = true;

    /// <summary>Order of the action in control lists.</summary>
    /// <value>The sort order.</value>
    public int SortOrder { get; set; }
}
