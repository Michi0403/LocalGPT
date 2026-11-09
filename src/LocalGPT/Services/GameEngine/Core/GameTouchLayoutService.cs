using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Default <see cref="IGameTouchLayoutService"/>. Direction labels are the texts <c>touch.pad.&lt;direction&gt;</c>;
/// an action button's label is its <c>touch.&lt;action&gt;</c> text and its slot the text <c>touch.&lt;action&gt;.slot</c>;
/// only actions that are currently legal get a button.
/// </summary>
/// <param name="texts">Reads the game's texts.</param>
/// <param name="logger">Receives layout diagnostics.</param>
public sealed class GameTouchLayoutService(IGameTextService texts, ILogger<GameTouchLayoutService> logger) : IGameTouchLayoutService
{
    /// <summary>Direction-pad directions in their on-screen order.</summary>
    private readonly IReadOnlyList<string> directions = ["up", "left", "down", "right"];

    /// <inheritdoc />
    public IReadOnlyList<GameTouchButton> BuildDirectionPad(CouncilGameSessionSnapshot snapshot)
    {
        try
        {
            var buttons = new List<GameTouchButton>();
            foreach (var direction in directions)
            {
                var action = snapshot.Ruleset.Aliases.FirstOrDefault(alias =>
                    string.Equals(alias.AliasKind, "axis", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(alias.Alias, direction, StringComparison.OrdinalIgnoreCase))?.Target;
                if (string.IsNullOrWhiteSpace(action))
                    continue;
                buttons.Add(new GameTouchButton
                {
                    Slot = direction,
                    Action = action,
                    Label = texts.Get(snapshot.Ruleset, $"touch.pad.{direction}")
                });
            }
            return buttons;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the touch direction pad of game session {GameSessionId} failed.", snapshot.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<GameTouchButton> BuildActionButtons(CouncilGameSessionSnapshot snapshot)
    {
        try
        {
            return snapshot.Ruleset.Actions
                .OrderBy(action => action.SortOrder)
                .Where(action => snapshot.LegalActions.Contains(action.ActionKey, StringComparer.OrdinalIgnoreCase) &&
                    texts.Has(snapshot.Ruleset, $"touch.{action.ActionKey}"))
                .Select(action => new GameTouchButton
                {
                    Slot = texts.Has(snapshot.Ruleset, $"touch.{action.ActionKey}.slot") ? texts.Get(snapshot.Ruleset, $"touch.{action.ActionKey}.slot") : string.Empty,
                    Action = action.ActionKey,
                    Label = texts.Get(snapshot.Ruleset, $"touch.{action.ActionKey}")
                })
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the touch action buttons of game session {GameSessionId} failed.", snapshot.Id);
            throw;
        }
    }
}
