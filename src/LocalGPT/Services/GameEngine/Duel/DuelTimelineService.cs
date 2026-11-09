using System.Globalization;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// The bounded authoritative event timeline of a duel progression.
/// </summary>
/// <param name="texts">Formats event kinds and details.</param>
/// <param name="textUtility">Compacts timeline fields and shortens model labels.</param>
/// <param name="roster">Reads the active member of each side.</param>
/// <param name="logger">Receives timeline diagnostics.</param>
public sealed class DuelTimelineService(
    IGameTextService texts,
    IGameTextUtilityService textUtility,
    IDuelRosterService roster,
    ILogger<DuelTimelineService> logger) : IDuelTimelineService
{
    /// <inheritdoc />
    public void Add(GameEngineContext context, DuelSettings settings, string eventKey, GameActorState? left, GameActorState? right, IReadOnlyDictionary<string, object?> tokens)
    {
        try
        {
            var session = context.Session;
            var progression = session.Progression;
            progression.TimelineSequence++;
            session.Timeline.Add(new GameTimelineEntry
            {
                Sequence = progression.TimelineSequence,
                Round = Math.Max(1, progression.Round),
                Match = Math.Max(1, progression.Match / 2 + 1),
                Exchange = Math.Max(0, progression.Exchange),
                EventKind = textUtility.Compact(texts.Get(context.Ruleset, $"timeline.{eventKey}.kind"), 18),
                LeftState = SideState(context, settings, left),
                RightState = SideState(context, settings, right),
                Detail = textUtility.Compact(texts.Format(context.Ruleset, $"timeline.{eventKey}", tokens), 140)
            });
            if (session.Timeline.Count > settings.TimelineLimit)
                session.Timeline.RemoveRange(0, session.Timeline.Count - settings.TimelineLimit);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Adding duel timeline event {EventKey} failed.", eventKey);
            throw;
        }
    }

    /// <inheritdoc />
    public string Line(CouncilGameSessionState session, int offsetFromEnd)
    {
        try
        {
            var index = session.Timeline.Count - 1 - offsetFromEnd;
            if (index < 0)
                return string.Empty;
            var entry = session.Timeline[index];
            return texts.Format(session.Ruleset, "timeline.line", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["sequence"] = entry.Sequence,
                ["round"] = entry.Round,
                ["exchange"] = entry.Exchange,
                ["kind"] = entry.EventKind,
                ["detail"] = entry.Detail
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a duel timeline line failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats the side state of a timeline entry.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner, or <c>null</c>.</param>
    /// <returns>The side state.</returns>
    private string SideState(GameEngineContext context, DuelSettings settings, GameActorState? owner)
    {
        try
        {
            if (owner is null)
                return string.Empty;
            var member = roster.ActiveMember(context.Session, owner);
            return textUtility.Compact(texts.Format(context.Ruleset, "timeline.side", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["owner"] = textUtility.ShortModel(owner.Name),
                ["member"] = member?.Name ?? string.Empty,
                ["health"] = Math.Max(0, roster.Health(settings, member)).ToString(CultureInfo.InvariantCulture)
            }), 54);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a duel timeline side failed.");
            throw;
        }
    }
}
