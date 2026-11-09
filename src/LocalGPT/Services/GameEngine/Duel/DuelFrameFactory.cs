using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Renders one duel frame: the battle view when the terminal is large enough (view parameters <c>battleMinimumWidth</c>
/// and <c>battleMinimumHeight</c>), otherwise the classic arena whose colours come from the ruleset's style rules.
/// </summary>
/// <param name="parameters">Reads the view thresholds.</param>
/// <param name="roster">Reads the active members and teams.</param>
/// <param name="timeline">Formats the recent timeline lines.</param>
/// <param name="texts">Reads the ready label.</param>
/// <param name="textUtility">Compacts scene texts and shortens model labels.</param>
/// <param name="battle">Renders battle scenes.</param>
/// <param name="classic">Renders classic arenas.</param>
/// <param name="logger">Receives frame diagnostics.</param>
public sealed class DuelFrameFactory(
    IGameParameterReader parameters,
    IDuelRosterService roster,
    IDuelTimelineService timeline,
    IGameTextService texts,
    IGameTextUtilityService textUtility,
    IDuelBattleRenderer battle,
    IDuelClassicRenderer classic,
    ILogger<DuelFrameFactory> logger) : IDuelFrameFactory
{
    /// <inheritdoc />
    public GameViewFrame Make(GameEngineContext context, DuelSettings settings, DuelFrameRequest request)
    {
        try
        {
            var session = context.Session;
            var view = parameters.Parse(context.Ruleset.ViewParametersJson);
            if (session.FrameWidth < parameters.ReadInt(view, "battleMinimumWidth", 72) || session.FrameHeight < parameters.ReadInt(view, "battleMinimumHeight", 26))
                return new GameViewFrame { Text = classic.Render(context, settings, request) };

            var scene = new DuelScene
            {
                Headline = textUtility.Compact(request.Headline, 60),
                Body = request.Body ?? string.Empty,
                Footer = request.Footer ?? string.Empty,
                CenterEffect = textUtility.Compact(request.CenterEffect, 16),
                HudMotif = textUtility.Compact(request.HudMotif, 42),
                Round = Math.Max(1, session.Progression.Round),
                Exchange = Math.Max(0, session.Progression.Exchange),
                StartingHealth = settings.StartingHealth,
                MaximumSwitches = settings.MaximumSwitchesPerMatch,
                TimelineRecent = textUtility.Compact(timeline.Line(session, 0), 180),
                TimelinePrevious = textUtility.Compact(timeline.Line(session, 1), 180),
                Left = Side(context, settings, request.Left, request.LeftPose, request.LeftHealth, request.LeftStatement),
                Right = Side(context, settings, request.Right, request.RightPose, request.RightHealth, request.RightStatement),
                SameEntrant = request.Left is not null && ReferenceEquals(request.Left, request.Right)
            };
            return battle.Render(context, scene);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering duel frame {Headline} failed.", request.Headline);
            throw;
        }
    }

    /// <summary>
    /// Builds one scene side from an owner.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner, or <c>null</c>.</param>
    /// <param name="pose">The pose.</param>
    /// <param name="health">The shown health override.</param>
    /// <param name="statement">The statement; empty shows the greeting.</param>
    /// <returns>The scene side, or <c>null</c>.</returns>
    private DuelSceneSide? Side(GameEngineContext context, DuelSettings settings, GameActorState? owner, string pose, int? health, string statement)
    {
        try
        {
            if (owner is null)
                return null;
            var session = context.Session;
            var active = roster.ActiveMember(session, owner);
            var lastAction = owner.Texts.GetValueOrDefault("lastAction") ?? string.Empty;
            var defaultAction = settings.OwnerActions.FirstOrDefault(action => action.IsDefault)?.Key ?? string.Empty;
            var author = active?.Texts.GetValueOrDefault("spriteAuthor") ?? string.Empty;
            return new DuelSceneSide
            {
                OwnerName = textUtility.ShortModel(owner.Name),
                OwnerAction = string.IsNullOrWhiteSpace(lastAction) || string.Equals(lastAction, defaultAction, StringComparison.Ordinal)
                    ? texts.Get(context.Ruleset, "view.ready")
                    : lastAction,
                MemberName = active?.Name ?? string.Empty,
                Species = textUtility.Compact(active?.Texts.GetValueOrDefault("species"), 22),
                Health = health ?? roster.Health(settings, active),
                Statement = textUtility.Compact(string.IsNullOrWhiteSpace(statement) ? owner.Texts.GetValueOrDefault("greeting") : statement, 160),
                SwitchesUsed = owner.Stats.GetValueOrDefault("switches"),
                Roster = roster.Members(session, owner).Select(member => (Math.Max(0, roster.Health(settings, member)), member.IsActive)).ToList(),
                SpriteRows = active?.SpriteRows ?? [],
                SpriteKind = active?.SpriteKind ?? string.Empty,
                SpritePalette = active?.SpritePalette ?? [],
                SpriteAuthor = author.Length > 0 ? textUtility.ShortModel(author) : textUtility.ShortModel(owner.Texts.GetValueOrDefault("memberModel")),
                Pose = pose
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a duel scene side failed.");
            throw;
        }
    }
}
