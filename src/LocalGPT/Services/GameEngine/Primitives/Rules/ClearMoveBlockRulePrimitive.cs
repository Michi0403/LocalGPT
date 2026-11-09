using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Rules;

/// <summary>
/// Rule <c>clear-move-block</c>: clears the blocked-move flag before an action is applied.
/// </summary>
/// <param name="logger">Receives rule diagnostics.</param>
public sealed class ClearMoveBlockRulePrimitive(ILogger<ClearMoveBlockRulePrimitive> logger) : IGameRulePrimitive
{
    /// <inheritdoc />
    public string Key => "clear-move-block";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectRule rule)
    {
        try
        {
            context.Session.LastMoveBlocked = false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
