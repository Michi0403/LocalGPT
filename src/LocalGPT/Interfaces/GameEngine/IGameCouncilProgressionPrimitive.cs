using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// A council progression primitive: a game whose turns are resolved by Council workflow steps instead of direct controls.
/// </summary>
public interface IGameCouncilProgressionPrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by the council progression rule row.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Initializes or advances the progression from Council evidence.
    /// </summary>
    /// <param name="context">The engine context of the session.</param>
    /// <param name="rule">The progression rule row.</param>
    /// <param name="request">The Council entrants, presentation frames and evidence.</param>
    /// <returns>The frames and transcript summary of the step.</returns>
    GameCouncilStepOutcome Advance(GameEngineContext context, GameProjectRule rule, GameCouncilAdvanceRequest request);
}
