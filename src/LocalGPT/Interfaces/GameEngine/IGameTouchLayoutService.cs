using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Builds the on-screen touch controls of a running game from its ruleset: the direction pad follows the game's
/// <c>axis</c> aliases, further buttons are the actions that define a <c>touch.&lt;action&gt;</c> text.
/// </summary>
public interface IGameTouchLayoutService
{
    /// <summary>Builds the direction pad (up, left, down, right) of the session.</summary>
    /// <param name="snapshot">Running session.</param>
    /// <returns>The direction-pad controls whose direction maps to an action.</returns>
    IReadOnlyList<GameTouchButton> BuildDirectionPad(CouncilGameSessionSnapshot snapshot);

    /// <summary>Builds the action buttons of the session in action order.</summary>
    /// <param name="snapshot">Running session.</param>
    /// <returns>The action controls.</returns>
    IReadOnlyList<GameTouchButton> BuildActionButtons(CouncilGameSessionSnapshot snapshot);
}
