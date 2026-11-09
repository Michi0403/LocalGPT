using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// A rule primitive: runs one ruleset rule row when its trigger fires (world step, goal check, before/after action, start).
/// </summary>
public interface IGameRulePrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by <see cref="GameProjectRule.PrimitiveKey"/>.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Applies the rule.
    /// </summary>
    /// <param name="context">The engine context of the session.</param>
    /// <param name="rule">The rule row with its parameters.</param>
    void Apply(GameEngineContext context, GameProjectRule rule);
}
