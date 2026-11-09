using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Builds the generic template tokens of a running session.
/// </summary>
/// <param name="actors">Reads actors and markers.</param>
/// <param name="angles">Formats headings.</param>
/// <param name="texts">Resolves flag labels.</param>
/// <param name="logger">Receives token diagnostics.</param>
public sealed class GameTokenService(
    IGameActorQueryService actors,
    IGameAngleService angles,
    IGameTextService texts,
    ILogger<GameTokenService> logger) : IGameTokenService
{
    /// <inheritdoc />
    public Dictionary<string, object?> Build(CouncilGameSessionState session)
    {
        try
        {
            var ruleset = session.Ruleset;
            var tokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["turn"] = session.Turn,
                ["turnOwner"] = session.CurrentTurnOwner,
                ["displayName"] = session.DisplayName,
                ["gameKey"] = session.GameKey,
                ["renderer"] = session.FrameRenderer,
                ["message"] = session.Message,
                ["lastAction"] = session.LastAction,
                ["lastActionBy"] = session.LastActionBy,
                ["scenario"] = session.ScenarioPrompt,
                ["status"] = session.Status,
                ["round"] = session.Progression.Round,
                ["exchange"] = session.Progression.Exchange
            };

            var levels = ruleset.Levels.OrderBy(level => level.LevelIndex).ToList();
            var level = levels.Count == 0 ? null : levels[Math.Clamp(session.Progression.LevelIndex, 0, levels.Count - 1)];
            tokens["hasLevels"] = levels.Count > 0;
            tokens["levelCount"] = Math.Max(1, levels.Count);
            tokens["level"] = level?.LevelNumber ?? 1;
            tokens["levelName"] = level?.Name ?? string.Empty;
            tokens["difficulty"] = level?.Difficulty ?? 1;

            var player = actors.Player(session.Actors, ruleset);
            if (player is not null)
            {
                tokens["player.x"] = player.X;
                tokens["player.y"] = player.Y;
                tokens["player.name"] = player.Name;
                tokens["player.facing"] = Math.Round(angles.Normalize(player.FacingRadians) * 180d / Math.PI, 1);
                tokens["compass"] = angles.Compass(ruleset, player.FacingRadians);
                foreach (var stat in player.Stats)
                    tokens[$"stat.{stat.Key}"] = stat.Value;
            }

            foreach (var variable in session.Variables)
                tokens[$"var.{variable.Key}"] = variable.Value;

            foreach (var text in ruleset.Texts)
            {
                if (!text.TextKey.StartsWith("flag.", StringComparison.OrdinalIgnoreCase) ||
                    !text.TextKey.EndsWith(".on", StringComparison.OrdinalIgnoreCase))
                    continue;
                var name = text.TextKey[5..^3];
                var set = session.Variables.TryGetValue(name, out var value) &&
                    (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || (int.TryParse(value, out var number) && number != 0));
                tokens[$"flag.{name}"] = texts.Get(ruleset, $"flag.{name}.{(set ? "on" : "off")}");
            }

            foreach (var group in session.Actors.GroupBy(actor => actor.Role, StringComparer.OrdinalIgnoreCase))
            {
                tokens[$"alive.{group.Key}"] = group.Count(actor => !actor.IsEliminated);
                tokens[$"count.{group.Key}"] = group.Count();
            }

            foreach (var marker in session.Markers)
            {
                tokens[$"marker.{marker.Key}.x"] = marker.X;
                tokens[$"marker.{marker.Key}.y"] = marker.Y;
            }
            return tokens;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building game session tokens failed for session {SessionId}.", session.Id);
            throw;
        }
    }
}
