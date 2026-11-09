using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Rules;

/// <summary>
/// Rule <c>normalize-facing</c>: keeps the player heading inside [0, 2π).
/// </summary>
/// <param name="actors">Finds the player.</param>
/// <param name="angles">Normalizes the heading.</param>
/// <param name="logger">Receives rule diagnostics.</param>
public sealed class NormalizeFacingRulePrimitive(
    IGameActorQueryService actors,
    IGameAngleService angles,
    ILogger<NormalizeFacingRulePrimitive> logger) : IGameRulePrimitive
{
    /// <inheritdoc />
    public string Key => "normalize-facing";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectRule rule)
    {
        try
        {
            var player = actors.Player(context.Session.Actors, context.Ruleset);
            if (player is not null)
                player.FacingRadians = angles.Normalize(player.FacingRadians);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
