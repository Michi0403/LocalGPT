using System.Text;
using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Builds the Markdown transcript summary of a duel step from the <c>summary.*</c> texts of the ruleset.
/// The completion and continuation lines carry the workflow loop markers declared by the game's texts.
/// </summary>
/// <param name="roster">Reads owners, members and health.</param>
/// <param name="bracket">Reads the current pair.</param>
/// <param name="tokens">Builds owner tokens.</param>
/// <param name="parameters">Reads roster markers.</param>
/// <param name="texts">Formats the summary texts.</param>
/// <param name="logger">Receives summary diagnostics.</param>
public sealed class DuelSummaryService(
    IDuelRosterService roster,
    IDuelBracketService bracket,
    IDuelTokenService tokens,
    IGameParameterReader parameters,
    IGameTextService texts,
    ILogger<DuelSummaryService> logger) : IDuelSummaryService
{
    /// <inheritdoc />
    public string Build(GameEngineContext context, DuelSettings settings, string consequence, bool initialized)
    {
        try
        {
            var session = context.Session;
            var ruleset = context.Ruleset;
            var values = tokens.Base(context, settings);
            values["consequence"] = consequence;
            values["round"] = Math.Max(1, session.Progression.Round);
            var markers = parameters.ReadObject(settings.Parameters, "rosterMarkers");
            var active = parameters.ReadString(markers, "active", ">");
            var resting = parameters.ReadString(markers, "resting", "~");

            var builder = new StringBuilder();
            builder.AppendLine(texts.Format(ruleset, "summary.title", values));
            builder.AppendLine(consequence);
            builder.AppendLine();
            builder.AppendLine(texts.Format(ruleset, "summary.rules", values));
            foreach (var owner in roster.Owners(session, settings))
            {
                var entrant = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
                tokens.AddOwner(entrant, "entrant", context, settings, owner);
                entrant["entrant.health"] = Math.Max(0, roster.Health(settings, roster.ActiveMember(session, owner)));
                entrant["entrant.status"] = texts.Get(ruleset, string.Equals(owner.Key, session.Progression.ChampionKey, StringComparison.OrdinalIgnoreCase)
                    ? "summary.status.champion"
                    : owner.IsEliminated ? "summary.status.eliminated" : "summary.status.active");
                entrant["entrant.roster"] = string.Join(", ", roster.Members(session, owner).Select(member =>
                    $"{(member.IsActive ? active : member.Stats.GetValueOrDefault("resting") > 0 ? resting : string.Empty)}{member.Name}:{Math.Max(0, roster.Health(settings, member))}"));
                builder.AppendLine(texts.Format(ruleset, "summary.entrant", entrant));
            }
            if (session.Timeline.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine(texts.Get(ruleset, "summary.timeline"));
                foreach (var entry in session.Timeline.TakeLast(5))
                {
                    builder.AppendLine(texts.Format(ruleset, "summary.timeline.entry", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["sequence"] = entry.Sequence,
                        ["round"] = entry.Round,
                        ["exchange"] = entry.Exchange,
                        ["kind"] = entry.EventKind,
                        ["detail"] = entry.Detail
                    }));
                }
            }
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(session.Progression.ChampionKey))
            {
                tokens.AddOwner(values, "champion", context, settings, roster.Find(session, session.Progression.ChampionKey));
                builder.AppendLine(texts.Format(ruleset, "summary.champion", values));
                builder.AppendLine(texts.Get(ruleset, "summary.complete-marker"));
            }
            else
            {
                var pair = bracket.CurrentPairKeys(session);
                if (pair.Count == 2)
                {
                    tokens.AddOwner(values, "left", context, settings, roster.Find(session, pair[0]));
                    tokens.AddOwner(values, "right", context, settings, roster.Find(session, pair[1]));
                    builder.AppendLine(texts.Format(ruleset, "summary.current-match", values));
                }
                builder.AppendLine(texts.Get(ruleset, initialized ? "summary.first-exchange" : "summary.next-exchange"));
                builder.AppendLine(texts.Get(ruleset, "summary.continue-marker"));
            }
            return builder.ToString().Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the duel summary of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }
}
