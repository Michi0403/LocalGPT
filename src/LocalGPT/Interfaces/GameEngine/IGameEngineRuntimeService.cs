using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// The one generic game engine: interprets a session's declarative ruleset through the registered primitives.
/// It contains no game key, game name or game-specific branch.
/// </summary>
public interface IGameEngineRuntimeService
{
    /// <summary>
    /// Gets whether the ruleset is advanced by Council workflow steps instead of direct controls.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <returns><c>true</c> for council turn models.</returns>
    bool IsCouncilDriven(GameRuleset ruleset);

    /// <summary>
    /// Initializes the session: player actor and stats, start variables, first level and start rules.
    /// </summary>
    /// <param name="session">The new session.</param>
    void Initialize(CouncilGameSessionState session);

    /// <summary>
    /// Generates the current level with the ruleset's map generator.
    /// </summary>
    /// <param name="session">The running session.</param>
    void InitializeLevel(CouncilGameSessionState session);

    /// <summary>
    /// Normalizes a requested action through the ruleset's action and axis aliases.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="action">The requested action.</param>
    /// <param name="axisX">Optional horizontal axis.</param>
    /// <param name="axisY">Optional vertical axis.</param>
    /// <returns>The canonical action key.</returns>
    string NormalizeAction(GameRuleset ruleset, string? action, double? axisX, double? axisY);

    /// <summary>
    /// Applies an action with its before-action and after-action rules.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <param name="action">The canonical action key.</param>
    /// <param name="aimX">Optional aim column.</param>
    /// <param name="aimY">Optional aim row.</param>
    void ApplyAction(CouncilGameSessionState session, string action, int? aimX, int? aimY);

    /// <summary>
    /// Runs the world-step rules after an action.
    /// </summary>
    /// <param name="session">The running session.</param>
    void AdvanceWorld(CouncilGameSessionState session);

    /// <summary>
    /// Runs the enabled rules of one trigger in ruleset order.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <param name="trigger">The trigger name.</param>
    void RunRules(CouncilGameSessionState session, string trigger);

    /// <summary>
    /// Renders the session with the ruleset's view primitive.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <returns>The frame.</returns>
    GameViewFrame Render(CouncilGameSessionState session);

    /// <summary>
    /// Builds the frame caption.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <returns>The caption.</returns>
    string BuildCaption(CouncilGameSessionState session);

    /// <summary>
    /// Lists the legal actions in ruleset order.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="authoredScene">Whether the session runs an authored Game Studio scene.</param>
    /// <returns>The legal action keys.</returns>
    List<string> BuildLegalActions(GameRuleset ruleset, bool authoredScene);

    /// <summary>
    /// Builds the keyboard and gamepad bindings of the legal actions.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="authoredScene">Whether the session runs an authored Game Studio scene.</param>
    /// <returns>The input bindings.</returns>
    List<RuntimeInputBindingDefinition> BuildInputBindings(GameRuleset ruleset, bool authoredScene);

    /// <summary>
    /// Describes an action for authored Game Studio scenes.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="action">The canonical action key.</param>
    /// <returns>The scene control step, or <c>null</c> when the action has no scene behaviour.</returns>
    GameSceneControlStep? DescribeSceneControl(GameRuleset ruleset, string action);

    /// <summary>
    /// Chooses the next AI action with the ruleset's autoplay primitive.
    /// </summary>
    /// <param name="snapshot">The session snapshot including its ruleset.</param>
    /// <returns>A legal action key.</returns>
    string SelectAutoplayAction(CouncilGameSessionSnapshot snapshot);

    /// <summary>
    /// Builds the semantic colour runs of a frame from the ruleset's style rules.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="lines">The frame rows.</param>
    /// <returns>The raw style runs before normalization.</returns>
    List<CouncilAsciiStyleRun> BuildStyleRuns(GameRuleset ruleset, IReadOnlyList<string> lines);

    /// <summary>
    /// Initializes or advances a council-driven session.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <param name="request">The Council entrants, presentation frames and evidence.</param>
    /// <returns>The frames and transcript summary of the step.</returns>
    GameCouncilStepOutcome AdvanceCouncil(CouncilGameSessionState session, GameCouncilAdvanceRequest request);

    /// <summary>
    /// Reads the text of the ruleset with the session tokens.
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <param name="key">The text key.</param>
    /// <returns>The formatted text.</returns>
    string Text(CouncilGameSessionState session, string key);

    /// <summary>
    /// Sets the turn owner, input reason and human-input flag of the session from the control mode and the texts
    /// <c>control.owner.{mode}</c> and <c>{reasonTextPrefix}.{mode}</c> (mode = ai, shared or human).
    /// </summary>
    /// <param name="session">The running session.</param>
    /// <param name="reasonTextPrefix">The text-key prefix of the input reason.</param>
    void ApplyControlOwnership(CouncilGameSessionState session, string reasonTextPrefix);
}
