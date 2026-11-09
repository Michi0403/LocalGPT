namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// Physical inputs of one game action for the browser controller: keyboard codes (<c>KeyboardEvent.code</c>) and
/// standard-gamepad controls (<c>button:&lt;index&gt;</c> or <c>axis:&lt;index&gt;:-</c>/<c>axis:&lt;index&gt;:+</c>),
/// parsed from the action row's keyboard and gamepad labels.
/// </summary>
public sealed class GameInputMapping
{
    /// <summary>Gets or sets the semantic action submitted when one of the inputs is pressed.</summary>
    /// <value>The action key.</value>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the keyboard codes of the action.</summary>
    /// <value>Codes such as <c>KeyW</c>, <c>ArrowUp</c>, <c>Space</c> or <c>Digit1</c>.</value>
    public List<string> Keys { get; set; } = [];

    /// <summary>Gets or sets the gamepad controls of the action.</summary>
    /// <value>Controls such as <c>button:0</c> or <c>axis:1:-</c>.</value>
    public List<string> Controls { get; set; } = [];
}
