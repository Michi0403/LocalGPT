using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Council progression <c>single-elimination-team-duel</c>: Council members enter as owner/member pairs with teams,
/// a single-elimination bracket pairs them, and each Council round resolves one authoritative exchange. Mechanics,
/// storyboard frames, texts and the phase specifications (<c>phases.initialize|exchange|complete</c> with <c>hud</c>,
/// <c>consequence</c>, <c>subtitle</c> and <c>lastAction</c>) are declared by the game's rule rows and texts.
/// </summary>
/// <param name="settingsReader">Parses the rule parameters.</param>
/// <param name="roster">Owns entrants and teams.</param>
/// <param name="bracket">Owns the bracket.</param>
/// <param name="exchanges">Resolves exchanges.</param>
/// <param name="timeline">Records events.</param>
/// <param name="duelTokens">Builds storyboard tokens.</param>
/// <param name="storyboard">Renders storyboard frames.</param>
/// <param name="summaries">Builds transcript summaries.</param>
/// <param name="parameters">Reads phase specifications.</param>
/// <param name="evaluator">Evaluates phase specifications.</param>
/// <param name="styles">Reads the subtitle style.</param>
/// <param name="texts">Formats captions and owner labels.</param>
/// <param name="logger">Receives progression diagnostics.</param>
public sealed class SingleEliminationTeamDuelProgression(
    IDuelSettingsReader settingsReader,
    IDuelRosterService roster,
    IDuelBracketService bracket,
    IDuelExchangeResolver exchanges,
    IDuelTimelineService timeline,
    IDuelTokenService duelTokens,
    IDuelStoryboardService storyboard,
    IDuelSummaryService summaries,
    IGameParameterReader parameters,
    IGameValueSpecEvaluator evaluator,
    IGameStyleReader styles,
    IGameTextService texts,
    ILogger<SingleEliminationTeamDuelProgression> logger) : IGameCouncilProgressionPrimitive
{
    /// <inheritdoc />
    public string Key => "single-elimination-team-duel";

    /// <inheritdoc />
    public GameCouncilStepOutcome Advance(GameEngineContext context, GameProjectRule rule, GameCouncilAdvanceRequest request)
    {
        try
        {
            var session = context.Session;
            var settings = settingsReader.Read(rule);
            var phases = parameters.ReadObject(settings.Parameters, "phases");
            if (!session.Progression.Initialized)
                roster.Initialize(context, settings, request.Entrants);
            bracket.Prepare(session);
            var pair = bracket.CurrentPairKeys(session);
            var left = pair.Count == 2 ? roster.Find(session, pair[0]) : null;
            var right = pair.Count == 2 ? roster.Find(session, pair[1]) : null;

            var frames = new List<GameViewFrame>();
            Dictionary<string, object?> values;
            JsonElement phase;
            string consequence;
            if (request.InitializeOnly)
            {
                phase = parameters.ReadObject(phases, "initialize");
                var presentation = request.PresentationFrames
                    .Where(frame => !string.IsNullOrWhiteSpace(frame))
                    .Take(settings.MaximumPresentationFrames)
                    .ToList();
                frames.AddRange(presentation.Select(frame => new GameViewFrame { Text = frame }));
                values = Tokens(context, settings, left, right, phase);
                values["presentation"] = presentation.Count > 0;
                timeline.Add(context, settings, "ready", left, right, values);
                frames.AddRange(storyboard.Build(context, settings, "initialize", left, right, values));
                consequence = Evaluate(phase, "consequence", values);
            }
            else if (left is null || right is null)
            {
                phase = parameters.ReadObject(phases, "complete");
                var champion = roster.Find(session, session.Progression.ChampionKey);
                values = Tokens(context, settings, champion, champion, phase);
                duelTokens.AddOwner(values, "champion", context, settings, champion);
                values["hud"] = Evaluate(phase, "hud", values);
                timeline.Add(context, settings, "complete", champion, champion, values);
                frames.AddRange(storyboard.Build(context, settings, "complete", champion, champion, values));
                consequence = Evaluate(phase, "consequence", values);
            }
            else
            {
                phase = parameters.ReadObject(phases, "exchange");
                var outcome = exchanges.Resolve(context, settings, left, right, request.Evidence);
                values = duelTokens.Base(context, settings);
                duelTokens.AddOwner(values, "left", context, settings, left);
                duelTokens.AddOwner(values, "right", context, settings, right);
                duelTokens.AddExchange(values, context, settings, outcome);
                values["hud"] = Evaluate(phase, "hud", values);
                consequence = Evaluate(phase, "consequence", values);
                values["consequence"] = consequence;
                frames.AddRange(storyboard.Build(context, settings, "exchange", left, right, values));
                if (outcome.Winner is not null)
                {
                    session.Progression.Exchange = 0;
                    timeline.Add(context, settings, "match", outcome.Winner, outcome.Loser, values);
                    bracket.Prepare(session);
                    values["champion.decided"] = !string.IsNullOrWhiteSpace(session.Progression.ChampionKey);
                    values["round"] = session.Progression.Round;
                    values["exchange"] = session.Progression.Exchange;
                }
                frames.AddRange(storyboard.Build(context, settings, "exchange-resolved", left, right, values));
            }

            values["consequence"] = consequence;
            var completed = !string.IsNullOrWhiteSpace(session.Progression.ChampionKey);
            var subtitleStyle = parameters.ReadObject(settings.Parameters, "subtitleStyle");
            var outcomeResult = new GameCouncilStepOutcome
            {
                Frames = frames,
                Subtitle = Evaluate(phase, "subtitle", values),
                SubtitleStyle = parameters.TryRead(settings.Parameters, "subtitleStyle", out _) ? styles.Read(subtitleStyle) : null,
                FrameDelayMilliseconds = settings.FrameDelayMilliseconds,
                SubtitleHoldMilliseconds = settings.SubtitleHoldMilliseconds,
                Caption = texts.Format(context.Ruleset, completed ? "council.caption.complete" : "council.caption", values),
                Renderer = texts.Get(context.Ruleset, "council.renderer"),
                TurnOwner = texts.Get(context.Ruleset, "council.turn-owner"),
                LastAction = parameters.ReadString(phase, "lastAction", string.Empty),
                SummaryMarkdown = summaries.Build(context, settings, consequence, request.InitializeOnly),
                Completed = completed
            };
            logger.LogInformation("Duel progression of session {SessionId} produced {FrameCount} frame(s); round {Round}, exchange {Exchange}, completed={Completed}.",
                session.Id, frames.Count, session.Progression.Round, session.Progression.Exchange, completed);
            return outcomeResult;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Advancing the duel progression of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }

    /// <summary>
    /// Builds the tokens of the initialization or completion phase.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="left">The left owner, or <c>null</c>.</param>
    /// <param name="right">The right owner, or <c>null</c>.</param>
    /// <param name="phase">The phase specification.</param>
    /// <returns>The tokens including the phase heads-up motif.</returns>
    private Dictionary<string, object?> Tokens(GameEngineContext context, DuelSettings settings, GameActorState? left, GameActorState? right, JsonElement phase)
    {
        try
        {
            var values = duelTokens.Base(context, settings);
            duelTokens.AddOwner(values, "left", context, settings, left);
            duelTokens.AddOwner(values, "right", context, settings, right);
            values["hud"] = Evaluate(phase, "hud", values);
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel phase tokens failed.");
            throw;
        }
    }

    /// <summary>
    /// Evaluates a phase specification field.
    /// </summary>
    /// <param name="phase">The phase specification.</param>
    /// <param name="name">The field name.</param>
    /// <param name="values">The tokens.</param>
    /// <returns>The evaluated text, or an empty string.</returns>
    private string Evaluate(JsonElement phase, string name, IReadOnlyDictionary<string, object?> values)
    {
        try
        {
            return parameters.TryRead(phase, name, out var specification) ? evaluator.Evaluate(specification, values) : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating duel phase field {FieldName} failed.", name);
            throw;
        }
    }
}
