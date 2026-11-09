using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine;

/// <summary>
/// The generic game engine. It interprets a session's declarative ruleset (rows of stats, archetypes, actions, rules,
/// levels, texts and aliases) through the registered primitives and contains no game key, name or game-specific branch.
/// </summary>
/// <param name="registry">Looks up primitives.</param>
/// <param name="parameters">Reads ruleset parameters.</param>
/// <param name="texts">Resolves texts.</param>
/// <param name="tokens">Builds session tokens.</param>
/// <param name="mapSupport">Creates the player actor.</param>
/// <param name="messages">Applies control-ownership texts.</param>
/// <param name="logger">Receives engine diagnostics.</param>
public sealed class GameEngineRuntimeService(
    IGamePrimitiveRegistry registry,
    IGameParameterReader parameters,
    IGameTextService texts,
    IGameTokenService tokens,
    IGameMapSupportService mapSupport,
    IGameMessageService messages,
    ILogger<GameEngineRuntimeService> logger) : IGameEngineRuntimeService
{
    /// <summary>
    /// The turn model key of rulesets advanced by Council workflow steps.
    /// </summary>
    private const string CouncilTurnModel = "council";

    /// <inheritdoc />
    public bool IsCouncilDriven(GameRuleset ruleset)
    {
        try
        {
            return string.Equals(ruleset.TurnModelKey, CouncilTurnModel, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the game turn model failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void Initialize(CouncilGameSessionState session)
    {
        try
        {
            session.Actors.Clear();
            session.Markers.Clear();
            session.Variables.Clear();
            session.Timeline.Clear();
            var player = mapSupport.CreatePlayer(session.Ruleset);
            if (player is not null)
                session.Actors.Add(player);
            if (!IsCouncilDriven(session.Ruleset))
                InitializeLevel(session);
            RunRules(session, "start");
            logger.LogInformation("Game engine initialized session {SessionId} ({GameKey}) with view {ViewKey}, turn model {TurnModelKey}, map {MapGeneratorKey}, {ActorCount} actor(s).",
                session.Id, session.GameKey, session.Ruleset.ViewKey, session.Ruleset.TurnModelKey, session.Ruleset.MapGeneratorKey, session.Actors.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Initializing game session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public void InitializeLevel(CouncilGameSessionState session)
    {
        try
        {
            var generator = registry.MapGenerator(session.Ruleset.MapGeneratorKey);
            if (generator is null)
            {
                logger.LogDebug("Game session {SessionId} has no map generator.", session.Id);
                return;
            }
            generator.Generate(new GameEngineContext(session, this));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Generating the level of game session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public string NormalizeAction(GameRuleset ruleset, string? action, double? axisX, double? axisY)
    {
        try
        {
            var value = (action ?? string.Empty).Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            if (string.IsNullOrWhiteSpace(value) && (axisX.HasValue || axisY.HasValue))
            {
                var x = axisX ?? 0;
                var y = axisY ?? 0;
                var direction = Math.Abs(y) >= Math.Abs(x)
                    ? y < 0 ? "up" : "down"
                    : x < 0 ? "left" : "right";
                value = Alias(ruleset, "axis", direction) ?? value;
            }
            return Alias(ruleset, "action", value) ?? value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing game action {Action} failed.", action);
            throw;
        }
    }

    /// <inheritdoc />
    public void ApplyAction(CouncilGameSessionState session, string action, int? aimX, int? aimY)
    {
        try
        {
            var context = new GameEngineContext(session, this);
            RunRules(session, "before-action");
            var row = session.Ruleset.Actions.FirstOrDefault(item => string.Equals(item.ActionKey, action, StringComparison.OrdinalIgnoreCase));
            var primitive = row is null ? null : registry.Action(row.PrimitiveKey);
            if (row is not null && primitive is not null)
                primitive.Apply(context, row, aimX, aimY);
            else
                logger.LogDebug("Game session {SessionId} ignored action {Action}: the ruleset defines no runnable action row.", session.Id, action);
            RunRules(session, "after-action");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying game action {Action} to session {SessionId} failed.", action, session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public void AdvanceWorld(CouncilGameSessionState session)
    {
        try
        {
            if (session.Status != "Running")
                return;
            RunRules(session, "world-step");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Advancing the world of game session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public void RunRules(CouncilGameSessionState session, string trigger)
    {
        try
        {
            var context = new GameEngineContext(session, this);
            foreach (var rule in session.Ruleset.Rules
                .Where(item => item.IsEnabled && string.Equals(item.Trigger, trigger, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.SortOrder))
            {
                var primitive = registry.Rule(rule.PrimitiveKey);
                if (primitive is null)
                    continue;
                primitive.Apply(context, rule);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Running {Trigger} rules of game session {SessionId} failed.", trigger, session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public GameViewFrame Render(CouncilGameSessionState session)
    {
        try
        {
            var view = registry.View(session.Ruleset.ViewKey);
            if (view is null)
            {
                logger.LogWarning("Game session {SessionId} has no runnable view {ViewKey}; an empty frame is shown.", session.Id, session.Ruleset.ViewKey);
                return new GameViewFrame
                {
                    Text = string.Join(Environment.NewLine, Enumerable.Range(0, session.FrameHeight).Select(_ => new string(' ', session.FrameWidth)))
                };
            }
            return view.Render(new GameEngineContext(session, this));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering game session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public string BuildCaption(CouncilGameSessionState session)
    {
        try
        {
            var values = tokens.Build(session);
            values["levelCaption"] = session.Ruleset.Levels.Count > 0
                ? texts.Format(session.Ruleset, "caption.level", values)
                : string.Empty;
            return texts.Format(session.Ruleset, "caption", values);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the caption of game session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public List<string> BuildLegalActions(GameRuleset ruleset, bool authoredScene)
    {
        try
        {
            return PlayableActions(ruleset, authoredScene).Select(action => action.ActionKey).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the legal game actions failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public List<RuntimeInputBindingDefinition> BuildInputBindings(GameRuleset ruleset, bool authoredScene)
    {
        try
        {
            var description = texts.Get(ruleset, "binding.description");
            return PlayableActions(ruleset, authoredScene)
                .Select(action => new RuntimeInputBindingDefinition
                {
                    Action = action.ActionKey,
                    DisplayName = action.DisplayName,
                    KeyboardKey = action.KeyboardKey,
                    GamepadButton = action.GamepadButton,
                    Description = description
                })
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the game input bindings failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public GameSceneControlStep? DescribeSceneControl(GameRuleset ruleset, string action)
    {
        try
        {
            var row = ruleset.Actions.FirstOrDefault(item => string.Equals(item.ActionKey, action, StringComparison.OrdinalIgnoreCase));
            return row is null ? null : registry.Action(row.PrimitiveKey)?.DescribeSceneControl(row);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing scene control {Action} failed.", action);
            throw;
        }
    }

    /// <inheritdoc />
    public string SelectAutoplayAction(CouncilGameSessionSnapshot snapshot)
    {
        try
        {
            var primitive = registry.Autoplay(snapshot.Ruleset.AutoplayKey);
            if (primitive is null)
                return snapshot.LegalActions.FirstOrDefault() ?? string.Empty;
            return primitive.SelectAction(snapshot, parameters.Parse(snapshot.Ruleset.AutoplayParametersJson));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the autoplay action of game session {SessionId} failed.", snapshot.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public List<CouncilAsciiStyleRun> BuildStyleRuns(GameRuleset ruleset, IReadOnlyList<string> lines)
    {
        try
        {
            var runs = new List<CouncilAsciiStyleRun>();
            var styleRules = ruleset.Rules
                .Where(rule => rule.IsEnabled && string.Equals(rule.Trigger, "style", StringComparison.OrdinalIgnoreCase))
                .OrderBy(rule => rule.SortOrder)
                .Select(rule => (Rule: rule, Primitive: registry.Style(rule.PrimitiveKey)))
                .Where(item => item.Primitive is not null)
                .ToList();
            for (var row = 0; row < lines.Count; row++)
            {
                foreach (var (rule, primitive) in styleRules)
                    primitive!.AddRuns(rule, row, lines[row], runs);
            }
            return runs;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building semantic game style runs failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public GameCouncilStepOutcome AdvanceCouncil(CouncilGameSessionState session, GameCouncilAdvanceRequest request)
    {
        try
        {
            var rule = session.Ruleset.Rules
                .Where(item => item.IsEnabled && string.Equals(item.Trigger, "council.progression", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.SortOrder)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"Game '{session.GameKey}' declares no council progression rule.");
            var primitive = registry.CouncilProgression(rule.PrimitiveKey)
                ?? throw new InvalidOperationException($"Game '{session.GameKey}' references the unknown council progression '{rule.PrimitiveKey}'.");
            return primitive.Advance(new GameEngineContext(session, this), rule, request);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Advancing council-driven game session {SessionId} failed.", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public void ApplyControlOwnership(CouncilGameSessionState session, string reasonTextPrefix)
    {
        try
        {
            messages.ApplyControlOwnership(new GameEngineContext(session, this), reasonTextPrefix);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying control ownership {ReasonTextPrefix} to game session {SessionId} failed.", reasonTextPrefix, session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public string Text(CouncilGameSessionState session, string key)
    {
        try
        {
            return texts.Format(session.Ruleset, key, tokens.Build(session));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting game text {TextKey} of session {SessionId} failed.", key, session.Id);
            throw;
        }
    }

    /// <summary>
    /// Lists the actions a session offers, in ruleset order; authored scenes offer only actions with scene behaviour.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="authoredScene">Whether the session runs an authored Game Studio scene.</param>
    /// <returns>The playable action rows.</returns>
    private IEnumerable<GameProjectAction> PlayableActions(GameRuleset ruleset, bool authoredScene)
    {
        try
        {
            if (IsCouncilDriven(ruleset))
                return [];
            return ruleset.Actions
                .OrderBy(action => action.SortOrder)
                .Where(action => registry.Action(action.PrimitiveKey) is { } primitive &&
                    (!authoredScene || primitive.DescribeSceneControl(action) is not null))
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing playable game actions failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves an alias row.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="kind">The alias kind.</param>
    /// <param name="alias">The alias.</param>
    /// <returns>The alias target, or <c>null</c> when no row matches.</returns>
    private string? Alias(GameRuleset ruleset, string kind, string alias)
    {
        try
        {
            return ruleset.Aliases.FirstOrDefault(item =>
                string.Equals(item.AliasKind, kind, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Alias, alias, StringComparison.OrdinalIgnoreCase))?.Target;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving {AliasKind} alias {Alias} failed.", kind, alias);
            throw;
        }
    }
}
