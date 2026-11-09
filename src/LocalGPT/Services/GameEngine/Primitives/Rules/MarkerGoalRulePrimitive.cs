using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Rules;

/// <summary>
/// Rule <c>marker-goal</c>: when no living <c>clearRole</c> actor remains and the player stands on <c>marker</c>, the next
/// level row is generated (when levels auto-advance) or the game completes.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="actors">Finds the player, living actors and the marker.</param>
/// <param name="messages">Writes level and outcome texts.</param>
/// <param name="logger">Receives rule diagnostics.</param>
public sealed class MarkerGoalRulePrimitive(
    IGameParameterReader parameters,
    IGameActorQueryService actors,
    IGameMessageService messages,
    ILogger<MarkerGoalRulePrimitive> logger) : IGameRulePrimitive
{
    /// <inheritdoc />
    public string Key => "marker-goal";

    /// <inheritdoc />
    public void Apply(GameEngineContext context, GameProjectRule rule)
    {
        try
        {
            var session = context.Session;
            var ruleParameters = parameters.Parse(rule.ParametersJson);
            var clearRole = parameters.ReadString(ruleParameters, "clearRole", string.Empty);
            if (clearRole.Length > 0 && actors.Living(session.Actors, clearRole).Count > 0)
                return;
            var player = actors.Player(session.Actors, context.Ruleset);
            var marker = actors.Marker(session.Markers, parameters.ReadString(ruleParameters, "marker", "goal"));
            if (player is null || marker is null || player.X != marker.X || player.Y != marker.Y)
                return;

            var levelCount = context.Ruleset.Levels.Count;
            var levelsUsed = parameters.ReadBool(ruleParameters, "usesLevels", true) && levelCount > 0;
            if (!levelsUsed)
            {
                messages.Complete(context, "goal.complete.owner", "goal.complete.single.reason", "goal.complete.single.message");
                return;
            }

            var completedLevel = context.Ruleset.Levels.OrderBy(level => level.LevelIndex).ElementAt(Math.Clamp(session.Progression.LevelIndex, 0, levelCount - 1)).LevelNumber;
            if (session.Progression.AutoAdvanceLevels && session.Progression.LevelIndex + 1 < levelCount)
            {
                session.Progression.LevelIndex++;
                context.Engine.InitializeLevel(session);
                session.Status = "Running";
                var extra = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["completedLevel"] = completedLevel };
                messages.ApplyControlOwnership(context, "level.started.reason", extra);
                messages.Say(context, "level.advanced", extra);
                logger.LogInformation("Game session {SessionId} advanced from level {CompletedLevel} to level index {LevelIndex}.", session.Id, completedLevel, session.Progression.LevelIndex);
                return;
            }

            if (levelCount > 1)
                messages.Complete(context, "goal.complete.owner", "goal.complete.campaign.reason", "goal.complete.campaign.message");
            else
                messages.Complete(context, "goal.complete.owner", "goal.complete.single.reason", "goal.complete.single.message");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
