using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council game session behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilGameSessionService
    {
    /// <summary>
    /// Normalizes game key as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeGameKey(string? gameKey)
    {
    try
    {
            var normalized = (gameKey ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            return normalized switch
            {
                "doom" or "ascii-doom" or "ascii-doom-council-adventure" => "ascii-doom",
                "dragon" or "green-dragon" or "lotgd" or "green-dragon-runtime-story" => "green-dragon",
                _ => throw new ArgumentException($"Unsupported game key '{gameKey}'. Use ascii-doom or green-dragon.", nameof(gameKey))
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeGameKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeGameKey)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs default team for as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DefaultTeamFor(string gameKey) {
    try
    {
        return gameKey == "green-dragon" ? "green-dragon-runtime-story" : "ascii-doom-council-adventure";
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(DefaultTeamFor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(DefaultTeamFor)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds legal actions as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> BuildLegalActions(string gameKey) {
    try
    {
        return gameKey == "green-dragon"
        ? ["move-forward", "move-backward", "turn-left", "turn-right", "use", "choice-1", "choice-2", "choice-3"]
        : ["move-forward", "move-backward", "strafe-left", "strafe-right", "turn-left", "turn-right", "shoot", "duck", "use"];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildLegalActions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildLegalActions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds input bindings as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gameKey">Game key value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<RuntimeInputBindingDefinition> BuildInputBindings(string gameKey) {
    try
    {
        return gameKey == "green-dragon"
        ?
        [
            Binding("move-forward", "Move / choice up", "W / ArrowUp", "D-pad Up"),
            Binding("move-backward", "Move / choice down", "S / ArrowDown", "D-pad Down"),
            Binding("turn-left", "Previous choice", "A / ArrowLeft", "D-pad Left"),
            Binding("turn-right", "Next choice", "D / ArrowRight", "D-pad Right"),
            Binding("use", "Confirm / interact", "E / Enter", "A"),
            Binding("choice-1", "Choice 1", "1", "X"),
            Binding("choice-2", "Choice 2", "2", "Y"),
            Binding("choice-3", "Choice 3", "3", "B")
        ]
        :
        [
            Binding("move-forward", "Move forward", "W / ArrowUp", "Left stick up"),
            Binding("move-backward", "Move backward", "S / ArrowDown", "Left stick down"),
            Binding("strafe-left", "Strafe left", "A", "Left stick left"),
            Binding("strafe-right", "Strafe right", "D", "Left stick right"),
            Binding("turn-left", "Turn / aim left", "Q / ArrowLeft", "Right stick left"),
            Binding("turn-right", "Turn / aim right", "R / ArrowRight", "Right stick right"),
            Binding("shoot", "Shoot along the current x/y facing ray", "Space", "Right trigger"),
            Binding("duck", "Duck / stand", "Ctrl / C", "B"),
            Binding("use", "Use door / switch", "E / Enter", "A")
        ];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildInputBindings)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(BuildInputBindings)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs binding as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="action">Action value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="display">Display value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="keyboard">Keyboard value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="gamepad">Gamepad value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The runtime input binding definition produced by the operation.</returns>
    private RuntimeInputBindingDefinition Binding(string action, string display, string keyboard, string gamepad) {
    try
    {
        return new()
    {
        Action = action,
        DisplayName = display,
        KeyboardKey = keyboard,
        GamepadButton = gamepad,
        Description = "The human UI and AI Player Controller call the same localgpt.game.control action."
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Binding)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(Binding)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes action as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="action">Action value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="axisX">Axis x value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="axisY">Axis y value supplied to the council game session operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeAction(string? action, double? axisX, double? axisY)
    {
    try
    {
            var value = (action ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            if (string.IsNullOrWhiteSpace(value) && (axisX.HasValue || axisY.HasValue))
            {
                var x = axisX ?? 0;
                var y = axisY ?? 0;
                if (Math.Abs(y) >= Math.Abs(x)) value = y < 0 ? "move-forward" : "move-backward";
                else value = x < 0 ? "strafe-left" : "strafe-right";
            }
            return value switch
            {
                "forward" or "up" or "w" => "move-forward",
                "back" or "backward" or "down" or "s" => "move-backward",
                "left" or "a" => "strafe-left",
                "right" or "d" => "strafe-right",
                "look-left" or "aim-left" or "rotate-left" or "q" => "turn-left",
                "look-right" or "aim-right" or "rotate-right" or "r" => "turn-right",
                "fire" or "attack" or "space" => "shoot",
                "crouch" or "ctrl" => "duck",
                "interact" or "enter" or "e" => "use",
                "1" => "choice-1",
                "2" => "choice-2",
                "3" => "choice-3",
                _ => value
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeAction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(NormalizeAction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies action as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="action">Action value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="aimX">Aim x value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="aimY">Aim y value supplied to the council game session operation and used when producing its result.</param>
    private void ApplyAction(CouncilGameSessionState session, string action, int? aimX, int? aimY)
    {
    try
    {
            if (session.GameKey == "green-dragon")
            {
                switch (action)
                {
                    case "move-forward": session.PlayerY = Math.Max(1, session.PlayerY - 1); break;
                    case "move-backward": session.PlayerY = Math.Min(8, session.PlayerY + 1); break;
                    case "turn-left": session.FacingRadians -= Math.PI / 8d; break;
                    case "turn-right": session.FacingRadians += Math.PI / 8d; break;
                    case "use": session.StoryLine = "The innkeeper nods and points toward the moonlit forest path."; break;
                    case "choice-1": session.StoryLine = "You accept the lantern and prepare for the forest path."; break;
                    case "choice-2": session.StoryLine = "You ask the villagers what they heard beyond the old gate."; break;
                    case "choice-3": session.StoryLine = "You rest by the hearth while the Story Director advances the world."; break;
                }
                return;
            }

            switch (action)
            {
                case "turn-left":
                    session.FacingRadians -= Math.PI / 12d;
                    break;
                case "turn-right":
                    session.FacingRadians += Math.PI / 12d;
                    break;
                case "duck":
                    session.IsDucking = !session.IsDucking;
                    break;
                case "shoot":
                    if (session.Ammo > 0)
                    {
                        session.Ammo--;
                        session.MuzzleFlash = 2;
                        if (aimX.HasValue && aimY.HasValue)
                            session.FacingRadians = Math.Atan2(aimY.Value - session.PlayerY, aimX.Value - session.PlayerX);
                    }
                    break;
                case "use":
                    session.UsePulse = 2;
                    break;
                case "move-forward":
                    TryMove(session, Math.Cos(session.FacingRadians), Math.Sin(session.FacingRadians));
                    break;
                case "move-backward":
                    TryMove(session, -Math.Cos(session.FacingRadians), -Math.Sin(session.FacingRadians));
                    break;
                case "strafe-left":
                    TryMove(session, Math.Cos(session.FacingRadians - Math.PI / 2d), Math.Sin(session.FacingRadians - Math.PI / 2d));
                    break;
                case "strafe-right":
                    TryMove(session, Math.Cos(session.FacingRadians + Math.PI / 2d), Math.Sin(session.FacingRadians + Math.PI / 2d));
                    break;
            }
            session.FacingRadians = NormalizeRadians(session.FacingRadians);
            session.MuzzleFlash = Math.Max(0, session.MuzzleFlash - 1);
            session.UsePulse = Math.Max(0, session.UsePulse - 1);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ApplyAction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(ApplyAction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to move as part of the council game session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="dx">Devexpress value supplied to the council game session operation and used when producing its result.</param>
    /// <param name="dy">Dy value supplied to the council game session operation and used when producing its result.</param>
    private void TryMove(CouncilGameSessionState session, double dx, double dy)
    {
    try
    {
            var map = doomMap;
            var nextX = session.PlayerX + (int)Math.Round(dx);
            var nextY = session.PlayerY + (int)Math.Round(dy);
            if (nextY < 0 || nextY >= map.Length || nextX < 0 || nextX >= map[nextY].Length) return;
            if (map[nextY][nextX] == '#') return;
            session.PlayerX = nextX;
            session.PlayerY = nextY;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(TryMove)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilGameSessionService)}.{nameof(TryMove)} failed.");
        throw;
    }
}

    }
}
