using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Renders the classic ASCII arena of a duel frame: boxed headline and heads-up rows, posed owner and member rigs,
/// health bars, team rosters, switch budgets, statements and the recent timeline. Labels come from <c>classic.*</c> texts.
/// </summary>
/// <param name="parameters">Reads roster markers.</param>
/// <param name="roster">Reads members and health.</param>
/// <param name="timeline">Formats the recent timeline lines.</param>
/// <param name="rigs">Poses rigs.</param>
/// <param name="hashes">Picks the stable border glyph.</param>
/// <param name="texts">Formats labels.</param>
/// <param name="textUtility">Compacts, fits and shortens texts.</param>
/// <param name="logger">Receives renderer diagnostics.</param>
public sealed class DuelClassicRenderer(
    IGameParameterReader parameters,
    IDuelRosterService roster,
    IDuelTimelineService timeline,
    IGameRigService rigs,
    IGameStableHashService hashes,
    IGameTextService texts,
    IGameTextUtilityService textUtility,
    ILogger<DuelClassicRenderer> logger) : IDuelClassicRenderer
{
    /// <inheritdoc />
    public string Render(GameEngineContext context, DuelSettings settings, DuelFrameRequest request)
    {
        try
        {
            var session = context.Session;
            var ruleset = context.Ruleset;
            var width = Math.Min(180, Math.Max(112, session.FrameWidth));
            var height = Math.Min(52, Math.Max(34, session.FrameHeight));
            var rows = Enumerable.Repeat(string.Empty, height).ToArray();
            var interior = width - 2;
            var sideWidth = Math.Max(34, (interior - 7) / 2);
            var motif = textUtility.Compact(request.HudMotif, 42);
            var borders = parameters.ReadString(parameters.Parse(ruleset.ViewParametersJson), "classicBorderGlyphs", "-=~");
            var horizontal = borders[hashes.StableRange($"{session.Id:N}|{session.Progression.Round}|{session.Progression.Exchange}|{request.Headline}|{motif}", 0, borders.Length - 1)];
            var rule = "+" + new string(horizontal, width - 2) + "+";

            rows[0] = rule;
            rows[1] = Center(textUtility.Compact(request.Headline, interior), interior);
            rows[2] = Center(string.IsNullOrWhiteSpace(motif)
                ? texts.Get(ruleset, "classic.arena")
                : texts.Format(ruleset, "classic.hud", Tokens(("motif", motif))), interior);
            rows[3] = rule;
            var left = request.Left;
            var right = request.Right;
            if (left is not null && right is not null)
            {
                var ownerArchetype = Archetype(ruleset, settings.OwnerArchetype);
                var memberArchetype = Archetype(ruleset, settings.MemberArchetype);
                var leftMember = roster.ActiveMember(session, left);
                var rightMember = roster.ActiveMember(session, right);
                rows[4] = Sides(OwnerLabel(context, left), OwnerLabel(context, right), request.CenterEffect, interior, sideWidth);
                var leftOwnerRig = rigs.Render(ownerArchetype, left.Rig, request.LeftPose, true);
                var rightOwnerRig = rigs.Render(ownerArchetype, right.Rig, request.RightPose, false);
                for (var line = 0; line < 7; line++)
                    rows[5 + line] = Sides(leftOwnerRig.ElementAtOrDefault(line) ?? string.Empty, rightOwnerRig.ElementAtOrDefault(line) ?? string.Empty, line == 3 ? request.CenterEffect : string.Empty, interior, sideWidth);

                rows[12] = Sides(MemberLabel(context, leftMember), MemberLabel(context, rightMember), texts.Get(ruleset, "classic.members"), interior, sideWidth);
                var leftMemberRig = rigs.Render(memberArchetype, leftMember?.Rig ?? rigs.Build(memberArchetype, left.Key, string.Empty), request.LeftPose, true);
                var rightMemberRig = rigs.Render(memberArchetype, rightMember?.Rig ?? rigs.Build(memberArchetype, right.Key, string.Empty), request.RightPose, false);
                for (var line = 0; line < 7; line++)
                    rows[13 + line] = Sides(leftMemberRig.ElementAtOrDefault(line) ?? string.Empty, rightMemberRig.ElementAtOrDefault(line) ?? string.Empty, line == 3 ? request.CenterEffect : string.Empty, interior, sideWidth);

                rows[20] = Sides(MiniHealth(request.LeftHealth ?? roster.Health(settings, leftMember), settings.StartingHealth),
                    MiniHealth(request.RightHealth ?? roster.Health(settings, rightMember), settings.StartingHealth), texts.Get(ruleset, "classic.health"), interior, sideWidth);
                rows[21] = Sides(RosterLine(context, settings, left), RosterLine(context, settings, right), texts.Get(ruleset, "classic.team"), interior, sideWidth);
                rows[22] = Sides(Switches(context, settings, left), Switches(context, settings, right), texts.Get(ruleset, "classic.owner"), interior, sideWidth);
                rows[23] = Sides(
                    string.IsNullOrWhiteSpace(request.LeftStatement) ? left.Texts.GetValueOrDefault("greeting") ?? string.Empty : request.LeftStatement,
                    string.IsNullOrWhiteSpace(request.RightStatement) ? right.Texts.GetValueOrDefault("greeting") ?? string.Empty : request.RightStatement,
                    texts.Get(ruleset, "classic.voice"), interior, sideWidth);
            }
            rows[24] = Box(textUtility.Compact(request.Body, interior), interior);
            rows[25] = Box(textUtility.Compact(request.Footer, interior), interior);
            rows[26] = Box(textUtility.Compact(timeline.Line(session, 0), interior), interior);
            rows[27] = Box(textUtility.Compact(timeline.Line(session, 1), interior), interior);
            rows[28] = Box(textUtility.Compact(timeline.Line(session, 2), interior), interior);
            for (var row = 29; row < height - 1; row++)
                rows[row] = Box(string.Empty, interior);
            rows[height - 1] = rule;

            var normalized = new List<string>(session.FrameHeight);
            for (var row = 0; row < session.FrameHeight; row++)
                normalized.Add(textUtility.Fit(row < rows.Length ? rows[row] : string.Empty, session.FrameWidth));
            return string.Join(Environment.NewLine, normalized);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rendering the classic duel arena failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds a token dictionary from pairs.
    /// </summary>
    /// <param name="pairs">The token pairs.</param>
    /// <returns>The tokens.</returns>
    private Dictionary<string, object?> Tokens(params (string Name, object? Value)[] pairs)
    {
        try
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in pairs)
                values[name] = value;
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building classic duel tokens failed.");
            throw;
        }
    }

    /// <summary>
    /// Finds an archetype row.
    /// </summary>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="key">The archetype key.</param>
    /// <returns>The archetype.</returns>
    private GameProjectArchetype Archetype(GameRuleset ruleset, string key)
    {
        try
        {
            return ruleset.Archetypes.FirstOrDefault(item => string.Equals(item.ArchetypeKey, key, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"The duel ruleset has no archetype '{key}'.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finding classic duel archetype {ArchetypeKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Formats an owner label with its last action.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The label.</returns>
    private string OwnerLabel(GameEngineContext context, GameActorState owner)
    {
        try
        {
            return texts.Format(context.Ruleset, "classic.owner-label", Tokens(
                ("owner", textUtility.ShortModel(owner.Name)),
                ("action", owner.Texts.GetValueOrDefault("lastAction") ?? string.Empty)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a classic owner label failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats a member label with its species.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="member">The member.</param>
    /// <returns>The label.</returns>
    private string MemberLabel(GameEngineContext context, GameActorState? member)
    {
        try
        {
            return texts.Format(context.Ruleset, "classic.member-label", Tokens(
                ("member", member?.Name ?? string.Empty),
                ("species", textUtility.Compact(member?.Texts.GetValueOrDefault("species"), 24))));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a classic member label failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats an owner's switch budget.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The switch label.</returns>
    private string Switches(GameEngineContext context, DuelSettings settings, GameActorState owner)
    {
        try
        {
            return texts.Format(context.Ruleset, "classic.switches", Tokens(
                ("used", owner.Stats.GetValueOrDefault("switches")),
                ("max", settings.MaximumSwitchesPerMatch)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a classic switch label failed.");
            throw;
        }
    }

    /// <summary>
    /// Formats an owner's team roster with active and resting markers.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>The roster line.</returns>
    private string RosterLine(GameEngineContext context, DuelSettings settings, GameActorState owner)
    {
        try
        {
            var markers = parameters.ReadObject(settings.Parameters, "rosterMarkers");
            var active = parameters.ReadString(markers, "active", ">");
            var resting = parameters.ReadString(markers, "resting", "~");
            var names = roster.Members(context.Session, owner).Select(member =>
                $"{(member.IsActive ? active : member.Stats.GetValueOrDefault("resting") > 0 ? resting : " ")}{textUtility.Compact(member.Name, 18)}:{Math.Max(0, roster.Health(settings, member))}");
            return textUtility.Compact(string.Join(" ", names), 70);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting a classic roster line failed.");
            throw;
        }
    }

    /// <summary>
    /// Draws a fourteen-cell health bar.
    /// </summary>
    /// <param name="health">The health.</param>
    /// <param name="startingHealth">The starting health.</param>
    /// <returns>The bar.</returns>
    private string MiniHealth(int health, int startingHealth)
    {
        try
        {
            const int cells = 14;
            var filled = Math.Clamp((int)Math.Round(cells * Math.Max(0, health) / (double)Math.Max(1, startingHealth)), 0, cells);
            return "[" + new string('#', filled) + new string('-', cells - filled) + $"] {Math.Max(0, health),3}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Drawing a classic health bar failed.");
            throw;
        }
    }

    /// <summary>
    /// Cuts text to a maximum length without padding.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="maximum">The maximum length.</param>
    /// <returns>The cut text.</returns>
    private string FitRaw(string? value, int maximum)
    {
        try
        {
            var raw = value ?? string.Empty;
            return raw.Length <= maximum ? raw : raw[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cutting classic duel text failed.");
            throw;
        }
    }

    /// <summary>
    /// Boxes a row between vertical borders.
    /// </summary>
    /// <param name="value">The row text.</param>
    /// <param name="interior">The interior width.</param>
    /// <returns>The boxed row.</returns>
    private string Box(string value, int interior)
    {
        try
        {
            return "|" + FitRaw(value, interior).PadRight(interior) + "|";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Boxing a classic duel row failed.");
            throw;
        }
    }

    /// <summary>
    /// Centres a boxed row.
    /// </summary>
    /// <param name="value">The row text.</param>
    /// <param name="interior">The interior width.</param>
    /// <returns>The centred boxed row.</returns>
    private string Center(string value, int interior)
    {
        try
        {
            var fitted = FitRaw(value, interior);
            return Box(new string(' ', Math.Max(0, (interior - fitted.Length) / 2)) + fitted, interior);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Centring a classic duel row failed.");
            throw;
        }
    }

    /// <summary>
    /// Lays out a boxed row with a left text, a right text and a centred middle label.
    /// </summary>
    /// <param name="leftText">The left text.</param>
    /// <param name="rightText">The right text.</param>
    /// <param name="center">The middle label.</param>
    /// <param name="interior">The interior width.</param>
    /// <param name="sideWidth">The width of each side.</param>
    /// <returns>The boxed row.</returns>
    private string Sides(string leftText, string rightText, string center, int interior, int sideWidth)
    {
        try
        {
            var lhs = FitRaw(leftText, sideWidth).PadRight(sideWidth);
            var rhs = FitRaw(rightText, sideWidth).PadLeft(sideWidth);
            var gap = Math.Max(1, interior - lhs.Length - rhs.Length);
            var centered = FitRaw(center, gap);
            var leftPad = Math.Max(0, (gap - centered.Length) / 2);
            var rightPad = Math.Max(0, gap - centered.Length - leftPad);
            return Box(FitRaw(lhs + new string(' ', leftPad) + centered + new string(' ', rightPad) + rhs, interior), interior);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Laying out a classic duel row failed.");
            throw;
        }
    }
}
