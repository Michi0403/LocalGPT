using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// An action primitive: applies one ruleset action row (movement, turning, hitscan, toggles, text choices, marker use).
/// </summary>
public interface IGameActionPrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by <see cref="GameProjectAction.PrimitiveKey"/>.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Applies the action.
    /// </summary>
    /// <param name="context">The engine context of the session.</param>
    /// <param name="action">The action row with its parameters.</param>
    /// <param name="aimX">Optional aim column.</param>
    /// <param name="aimY">Optional aim row.</param>
    void Apply(GameEngineContext context, GameProjectAction action, int? aimX, int? aimY);

    /// <summary>
    /// Describes the action for authored Game Studio scenes.
    /// </summary>
    /// <param name="action">The action row with its parameters.</param>
    /// <returns>The scene control step, or <c>null</c> when the action has no scene behaviour.</returns>
    GameSceneControlStep? DescribeSceneControl(GameProjectAction action);
}
