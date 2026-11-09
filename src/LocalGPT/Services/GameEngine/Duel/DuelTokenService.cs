using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Builds the storyboard tokens of a duel step: session and settings values, per-side identity and exchange values,
/// the winner and the finisher. Prefixes are <c>left</c>, <c>right</c>, <c>champion</c>, <c>winner</c>, <c>loser</c> and <c>finisher</c>.
/// </summary>
/// <param name="sessionTokens">Builds the generic session tokens.</param>
/// <param name="roster">Reads members and team health.</param>
/// <param name="parameters">Reads the pairing separator.</param>
/// <param name="textUtility">Shortens model labels and compacts species.</param>
/// <param name="logger">Receives token diagnostics.</param>
public sealed class DuelTokenService(
    IGameTokenService sessionTokens,
    IDuelRosterService roster,
    IGameParameterReader parameters,
    IGameTextUtilityService textUtility,
    ILogger<DuelTokenService> logger) : IDuelTokenService
{
    /// <inheritdoc />
    public Dictionary<string, object?> Base(GameEngineContext context, DuelSettings settings)
    {
        try
        {
            var session = context.Session;
            var values = sessionTokens.Build(session);
            values["round"] = session.Progression.Round;
            values["exchange"] = session.Progression.Exchange;
            values["maxSwitches"] = settings.MaximumSwitchesPerMatch;
            values["membersPerOwner"] = settings.MembersPerOwner;
            values["startingHealth"] = settings.StartingHealth;
            values["ruleKey"] = settings.RuleKey;
            values["champion.decided"] = !string.IsNullOrWhiteSpace(session.Progression.ChampionKey);
            values["pairing"] = string.Join(
                parameters.ReadString(settings.Parameters, "pairingSeparator", " | "),
                roster.Owners(session, settings).Select(owner => roster.ActiveMember(session, owner)?.Name ?? string.Empty));
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel base tokens failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void AddOwner(Dictionary<string, object?> tokens, string prefix, GameEngineContext context, DuelSettings settings, GameActorState? owner)
    {
        try
        {
            if (owner is null)
            {
                foreach (var name in new[] { "owner", "ownerModel", "summaryOwner", "memberModel", "member", "species", "identityVoice", "greeting", "spriteAuthor" })
                    tokens[$"{prefix}.{name}"] = string.Empty;
                foreach (var name in new[] { "health", "team", "switches", "wins" })
                    tokens[$"{prefix}.{name}"] = 0;
                tokens[$"{prefix}.fainted"] = false;
                return;
            }
            var session = context.Session;
            var member = roster.ActiveMember(session, owner);
            var health = roster.Health(settings, member);
            tokens[$"{prefix}.owner"] = textUtility.ShortModel(owner.Name);
            tokens[$"{prefix}.ownerModel"] = owner.Name;
            tokens[$"{prefix}.summaryOwner"] = textUtility.ShortSummaryModel(owner.Name);
            tokens[$"{prefix}.memberModel"] = owner.Texts.GetValueOrDefault("memberModel") ?? string.Empty;
            tokens[$"{prefix}.member"] = member?.Name ?? string.Empty;
            tokens[$"{prefix}.species"] = member?.Texts.GetValueOrDefault("species") ?? string.Empty;
            tokens[$"{prefix}.identityVoice"] = member?.Texts.GetValueOrDefault("voice") ?? string.Empty;
            tokens[$"{prefix}.greeting"] = owner.Texts.GetValueOrDefault("greeting") ?? string.Empty;
            tokens[$"{prefix}.health"] = health;
            tokens[$"{prefix}.team"] = roster.TeamHealth(session, settings, owner);
            tokens[$"{prefix}.switches"] = owner.Stats.GetValueOrDefault("switches");
            tokens[$"{prefix}.wins"] = owner.Stats.GetValueOrDefault("wins");
            tokens[$"{prefix}.fainted"] = health <= 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel owner tokens for {Prefix} failed.", prefix);
            throw;
        }
    }

    /// <inheritdoc />
    public void AddExchange(Dictionary<string, object?> tokens, GameEngineContext context, DuelSettings settings, DuelExchangeOutcome outcome)
    {
        try
        {
            AddSide(tokens, "left", settings, outcome.Left, outcome.Right);
            AddSide(tokens, "right", settings, outcome.Right, outcome.Left);
            tokens["opening"] = outcome.Opening;
            var winnerIsLeft = outcome.Winner is not null && ReferenceEquals(outcome.Winner, outcome.Left.Owner);
            tokens["winner"] = outcome.Winner is not null;
            tokens["winner.side"] = outcome.Winner is null ? string.Empty : winnerIsLeft ? "left" : "right";
            AddOwner(tokens, "winner", context, settings, outcome.Winner);
            AddOwner(tokens, "loser", context, settings, outcome.Loser);
            var finisher = outcome.Winner is null ? null : winnerIsLeft ? outcome.Left : outcome.Right;
            foreach (var tag in settings.EvidenceTags)
                tokens[$"finisher.{tag.Token}"] = finisher?.Tagged.GetValueOrDefault(tag.Token) ?? string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel exchange tokens failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds the exchange tokens of one side.
    /// </summary>
    /// <param name="tokens">The tokens to extend.</param>
    /// <param name="prefix">The side prefix.</param>
    /// <param name="settings">The duel settings.</param>
    /// <param name="side">The side outcome.</param>
    /// <param name="opponent">The opposing side outcome.</param>
    private void AddSide(Dictionary<string, object?> tokens, string prefix, DuelSettings settings, DuelSideOutcome side, DuelSideOutcome opponent)
    {
        try
        {
            tokens[$"{prefix}.action"] = side.Action.Key;
            tokens[$"{prefix}.move"] = side.Move.Key;
            tokens[$"{prefix}.damage"] = side.DamageDealt;
            tokens[$"{prefix}.before"] = side.HealthBefore;
            tokens[$"{prefix}.pose"] = side.Move.Pose;
            tokens[$"{prefix}.impactPose"] = opponent.DamageDealt > 0 ? settings.HitPose : side.Move.Pose;
            tokens[$"{prefix}.switched"] = side.Switched;
            tokens[$"{prefix}.autoSwitched"] = side.AutoSwitched;
            foreach (var tag in settings.EvidenceTags)
                tokens[$"{prefix}.{tag.Token}"] = side.Tagged.GetValueOrDefault(tag.Token) ?? string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building duel side tokens for {Prefix} failed.", prefix);
            throw;
        }
    }
}
