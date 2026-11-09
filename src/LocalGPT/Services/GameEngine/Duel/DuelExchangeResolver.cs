using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Resolves one authoritative duel exchange: owner actions and switches, member moves, stable damage with bonuses and
/// reductions, recovery, knock-out switches, bench rest, timeline events and the match decision.
/// </summary>
/// <param name="roster">Owns members, switching and team health.</param>
/// <param name="bracket">Advances decided matches.</param>
/// <param name="timeline">Records authoritative events.</param>
/// <param name="reader">Reads tagged evidence values.</param>
/// <param name="hashes">Picks stable damage and tie decisions.</param>
/// <param name="texts">Formats switch labels.</param>
/// <param name="logger">Receives exchange diagnostics.</param>
public sealed class DuelExchangeResolver(
    IDuelRosterService roster,
    IDuelBracketService bracket,
    IDuelTimelineService timeline,
    IGameCouncilTextReader reader,
    IGameStableHashService hashes,
    IGameTextService texts,
    ILogger<DuelExchangeResolver> logger) : IDuelExchangeResolver
{
    /// <inheritdoc />
    public DuelExchangeOutcome Resolve(GameEngineContext context, DuelSettings settings, GameActorState left, GameActorState right, IReadOnlyList<GameCouncilEvidence> evidence)
    {
        try
        {
            var session = context.Session;
            var progression = session.Progression;
            var defaultAction = settings.OwnerActions.First(action => action.IsDefault);
            var outcome = new DuelExchangeOutcome { Opening = progression.Exchange == 0 };
            if (outcome.Opening)
            {
                left.Stats["switches"] = 0;
                right.Stats["switches"] = 0;
                left.Texts["lastAction"] = defaultAction.Key;
                right.Texts["lastAction"] = defaultAction.Key;
            }

            progression.Exchange++;
            outcome.Left = Side(left, evidence, settings);
            outcome.Right = Side(right, evidence, settings);
            left.Texts["lastAction"] = outcome.Left.Action.Key;
            right.Texts["lastAction"] = outcome.Right.Action.Key;

            outcome.Left.Switched = outcome.Left.Action.Switches &&
                roster.Switch(session, settings, left, reader.ReadTaggedValue(outcome.Left.OwnerEvidence, [settings.SwitchTargetTag]), countAgainstLimit: true);
            outcome.Right.Switched = outcome.Right.Action.Switches &&
                roster.Switch(session, settings, right, reader.ReadTaggedValue(outcome.Right.OwnerEvidence, [settings.SwitchTargetTag]), countAgainstLimit: true);
            if (outcome.Left.Switched || outcome.Right.Switched)
                timeline.Add(context, settings, "switch", left, right, SwitchTokens(context, settings, outcome.Left.Switched, outcome.Right.Switched, left, right));

            outcome.Left.Move = ResolveMove(settings, outcome.Left.MemberEvidence + "\n" + outcome.Left.OwnerEvidence);
            outcome.Right.Move = ResolveMove(settings, outcome.Right.MemberEvidence + "\n" + outcome.Right.OwnerEvidence);
            outcome.Left.HealthBefore = roster.Health(settings, roster.ActiveMember(session, left));
            outcome.Right.HealthBefore = roster.Health(settings, roster.ActiveMember(session, right));

            Heal(context, settings, left, outcome.Left.Action.Heal);
            Heal(context, settings, right, outcome.Right.Action.Heal);

            var leftDamage = outcome.Left.Move.Attacks ? Damage(context, settings, "L", outcome.Left) : 0;
            var rightDamage = outcome.Right.Move.Attacks ? Damage(context, settings, "R", outcome.Right) : 0;
            if (outcome.Left.Action.AttackBonus != 0 && leftDamage > 0) leftDamage += outcome.Left.Action.AttackBonus;
            if (outcome.Right.Action.AttackBonus != 0 && rightDamage > 0) rightDamage += outcome.Right.Action.AttackBonus;
            leftDamage = Math.Max(0, leftDamage - outcome.Right.Move.IncomingReduction);
            rightDamage = Math.Max(0, rightDamage - outcome.Left.Move.IncomingReduction);
            leftDamage = Math.Max(0, leftDamage - outcome.Right.Action.IncomingReduction);
            rightDamage = Math.Max(0, rightDamage - outcome.Left.Action.IncomingReduction);

            var leftActive = roster.ActiveMember(session, left);
            var rightActive = roster.ActiveMember(session, right);
            Heal(context, settings, left, outcome.Left.Move.Heal);
            Heal(context, settings, right, outcome.Right.Move.Heal);
            if (rightActive is not null)
                rightActive.Stats[settings.HealthStat] = Math.Max(0, roster.Health(settings, rightActive) - leftDamage);
            if (leftActive is not null)
                leftActive.Stats[settings.HealthStat] = Math.Max(0, roster.Health(settings, leftActive) - rightDamage);
            outcome.Left.DamageDealt = leftDamage;
            outcome.Right.DamageDealt = rightDamage;
            roster.Sync(session, left);
            roster.Sync(session, right);

            outcome.Left.AutoSwitched = roster.Health(settings, roster.ActiveMember(session, left)) <= 0 &&
                roster.Switch(session, settings, left, string.Empty, countAgainstLimit: false);
            outcome.Right.AutoSwitched = roster.Health(settings, roster.ActiveMember(session, right)) <= 0 &&
                roster.Switch(session, settings, right, string.Empty, countAgainstLimit: false);
            if (outcome.Left.AutoSwitched || outcome.Right.AutoSwitched)
                timeline.Add(context, settings, "ko-switch", left, right, SwitchTokens(context, settings, outcome.Left.AutoSwitched, outcome.Right.AutoSwitched, left, right));

            roster.RestBench(session, settings, left);
            roster.RestBench(session, settings, right);
            roster.Sync(session, left);
            roster.Sync(session, right);

            var eventTokens = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["left.action"] = outcome.Left.Action.Key,
                ["left.move"] = outcome.Left.Move.Key,
                ["right.action"] = outcome.Right.Action.Key,
                ["right.move"] = outcome.Right.Move.Key,
                ["left.damage"] = leftDamage,
                ["right.damage"] = rightDamage,
                ["left.team"] = roster.TeamHealth(session, settings, left),
                ["right.team"] = roster.TeamHealth(session, settings, right)
            };
            timeline.Add(context, settings, "command", left, right, eventTokens);
            timeline.Add(context, settings, "impact", left, right, eventTokens);

            var forcedDecision = progression.Exchange >= settings.MaximumExchangesPerMatch;
            if (!roster.HasHealthyMember(session, settings, left) || !roster.HasHealthyMember(session, settings, right) || forcedDecision)
            {
                var leftTeam = roster.TeamHealth(session, settings, left);
                var rightTeam = roster.TeamHealth(session, settings, right);
                if (leftTeam == rightTeam)
                {
                    var chooseLeft = hashes.StableRange($"{session.Id:N}|{progression.Round}|{progression.Match}|decision", 0, 1) == 0;
                    outcome.Winner = chooseLeft ? left : right;
                    outcome.Loser = chooseLeft ? right : left;
                }
                else
                {
                    outcome.Winner = leftTeam > rightTeam ? left : right;
                    outcome.Loser = ReferenceEquals(outcome.Winner, left) ? right : left;
                }
                outcome.Loser.IsEliminated = true;
                outcome.Winner.Stats["wins"] = outcome.Winner.Stats.GetValueOrDefault("wins") + 1;
                outcome.Winner.Stats["switches"] = 0;
                bracket.AdvanceWinner(session, outcome.Winner);
                logger.LogInformation("Game session {SessionId} decided match {Match} of round {Round}: {WinnerKey} beats {LoserKey}.",
                    session.Id, progression.Match / 2, progression.Round, outcome.Winner.Key, outcome.Loser.Key);
            }

            foreach (var tag in settings.EvidenceTags)
            {
                outcome.Left.Tagged[tag.Token] = reader.ReadTaggedValue(EvidenceText(outcome.Left, tag.Side), [tag.Tag]);
                outcome.Right.Tagged[tag.Token] = reader.ReadTaggedValue(EvidenceText(outcome.Right, tag.Side), [tag.Tag]);
            }
            return outcome;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the duel exchange of session {SessionId} failed.", context.Session.Id);
            throw;
        }
    }

    /// <summary>
    /// Collects one side's evidence and owner action.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="evidence">The Council evidence.</param>
    /// <param name="settings">The duel settings.</param>
    /// <returns>The side outcome before resolution.</returns>
    private DuelSideOutcome Side(GameActorState owner, IReadOnlyList<GameCouncilEvidence> evidence, DuelSettings settings)
    {
        try
        {
            var ownerEvidence = EvidenceFor(evidence, owner.Texts.GetValueOrDefault("model") ?? owner.Name, "owner");
            var memberEvidence = EvidenceFor(evidence, owner.Texts.GetValueOrDefault("memberModel") ?? string.Empty, "member");
            return new DuelSideOutcome
            {
                Owner = owner,
                OwnerEvidence = ownerEvidence,
                MemberEvidence = memberEvidence,
                Action = ResolveOwnerAction(settings, ownerEvidence)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Collecting duel evidence of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <summary>
    /// Gets the last evidence of a model on a side.
    /// </summary>
    /// <param name="evidence">The Council evidence.</param>
    /// <param name="modelName">The model.</param>
    /// <param name="side">The side.</param>
    /// <returns>The evidence text, or an empty string.</returns>
    private string EvidenceFor(IReadOnlyList<GameCouncilEvidence> evidence, string modelName, string side)
    {
        try
        {
            return evidence
                .Where(item => string.Equals(item.ModelName, modelName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Side, side, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Content ?? string.Empty)
                .LastOrDefault() ?? string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting duel evidence failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves the member move from tagged or untagged evidence keywords.
    /// </summary>
    /// <param name="settings">The duel settings.</param>
    /// <param name="content">The combined member and owner evidence.</param>
    /// <returns>The move.</returns>
    private DuelMoveDefinition ResolveMove(DuelSettings settings, string content)
    {
        try
        {
            var tagged = reader.ReadTaggedValue(content, settings.MoveTags);
            var source = string.IsNullOrWhiteSpace(tagged) ? settings.MoveFallbackToContent ? content ?? string.Empty : string.Empty : tagged;
            return settings.Moves.FirstOrDefault(move => !move.IsDefault && move.Words.Any(word => source.Contains(word, StringComparison.OrdinalIgnoreCase)))
                ?? settings.Moves.First(move => move.IsDefault);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the duel move failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves the owner action from tagged (or, when configured, untagged) evidence keywords.
    /// </summary>
    /// <param name="settings">The duel settings.</param>
    /// <param name="content">The owner evidence.</param>
    /// <returns>The owner action.</returns>
    private DuelOwnerActionDefinition ResolveOwnerAction(DuelSettings settings, string content)
    {
        try
        {
            var tagged = reader.ReadTaggedValue(content, settings.OwnerActionTags);
            var source = string.IsNullOrWhiteSpace(tagged) ? settings.OwnerActionFallbackToContent ? content ?? string.Empty : string.Empty : tagged;
            return settings.OwnerActions.FirstOrDefault(action => !action.IsDefault && action.Words.Any(word => source.Contains(word, StringComparison.OrdinalIgnoreCase)))
                ?? settings.OwnerActions.First(action => action.IsDefault);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the duel owner action failed.");
            throw;
        }
    }

    /// <summary>
    /// Heals an owner's active member, capped at the starting health.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="amount">The heal amount; zero does nothing.</param>
    private void Heal(GameEngineContext context, DuelSettings settings, GameActorState owner, int amount)
    {
        try
        {
            if (amount == 0)
                return;
            var active = roster.ActiveMember(context.Session, owner);
            if (active is null)
                return;
            active.Stats[settings.HealthStat] = Math.Min(settings.StartingHealth, roster.Health(settings, active) + amount);
            roster.Sync(context.Session, owner);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Healing the active member of {OwnerKey} failed.", owner.Key);
            throw;
        }
    }

    /// <summary>
    /// Picks the stable attack damage of one side.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="sideLabel">The side label mixed into the seed.</param>
    /// <param name="side">The side outcome.</param>
    /// <returns>The damage.</returns>
    private int Damage(GameEngineContext context, DuelSettings settings, string sideLabel, DuelSideOutcome side)
    {
        try
        {
            var progression = context.Session.Progression;
            return hashes.StableRange(
                $"{context.Session.Id:N}|{progression.Round}|{progression.Match}|{progression.Exchange}|{sideLabel}|{side.MemberEvidence}|{side.OwnerEvidence}",
                settings.MinimumDamage,
                settings.MaximumDamage);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Picking duel damage failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds the tokens of a switch timeline event.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="leftSwitched">Whether the left side switched.</param>
    /// <param name="rightSwitched">Whether the right side switched.</param>
    /// <param name="left">The left owner.</param>
    /// <param name="right">The right owner.</param>
    /// <returns>The tokens.</returns>
    private Dictionary<string, object?> SwitchTokens(GameEngineContext context, DuelSettings settings, bool leftSwitched, bool rightSwitched, GameActorState left, GameActorState right)
    {
        try
        {
            var none = texts.Get(context.Ruleset, "timeline.none");
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["left"] = leftSwitched ? roster.ActiveMember(context.Session, left)?.Name ?? none : none,
                ["right"] = rightSwitched ? roster.ActiveMember(context.Session, right)?.Name ?? none : none
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel switch tokens failed.");
            throw;
        }
    }

    /// <summary>
    /// Gets a side's evidence text.
    /// </summary>
    /// <param name="side">The side outcome.</param>
    /// <param name="evidenceSide">The evidence side, <c>owner</c> or <c>member</c>.</param>
    /// <returns>The evidence text.</returns>
    private string EvidenceText(DuelSideOutcome side, string evidenceSide)
    {
        try
        {
            return string.Equals(evidenceSide, "owner", StringComparison.OrdinalIgnoreCase) ? side.OwnerEvidence : side.MemberEvidence;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting duel side evidence failed.");
            throw;
        }
    }
}
