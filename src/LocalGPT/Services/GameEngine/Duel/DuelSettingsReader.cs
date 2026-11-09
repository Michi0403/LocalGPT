using LocalGPT.BusinessObjects.GameEngine;
using LocalGPT.BusinessObjects.GameEngine.Duel;
using LocalGPT.Interfaces.GameEngine;
using LocalGPT.Interfaces.GameEngine.Duel;

namespace LocalGPT.Services.GameEngine.Duel;

/// <summary>
/// Parses the parameters of a team duel progression rule row.
/// </summary>
/// <param name="parameters">Reads the rule parameters.</param>
/// <param name="logger">Receives settings diagnostics.</param>
public sealed class DuelSettingsReader(IGameParameterReader parameters, ILogger<DuelSettingsReader> logger) : IDuelSettingsReader
{
    /// <inheritdoc />
    public DuelSettings Read(GameProjectRule rule)
    {
        try
        {
            var values = parameters.Parse(rule.ParametersJson);
            var settings = new DuelSettings
            {
                RuleKey = rule.RuleKey,
                OwnerArchetype = parameters.ReadString(values, "ownerArchetype", string.Empty),
                MemberArchetype = parameters.ReadString(values, "memberArchetype", string.Empty),
                HealthStat = parameters.ReadString(values, "healthStat", "health"),
                StartingHealth = parameters.ReadInt(values, "startingHealth", 100),
                MinimumDamage = parameters.ReadInt(values, "minimumDamage", 1),
                MaximumDamage = parameters.ReadInt(values, "maximumDamage", 10),
                MaximumExchangesPerMatch = parameters.ReadInt(values, "maximumExchangesPerMatch", 10),
                MembersPerOwner = parameters.ReadInt(values, "membersPerOwner", 1),
                MaximumSwitchesPerMatch = parameters.ReadInt(values, "maximumSwitchesPerMatch", 0),
                BenchRecoveryPerExchange = parameters.ReadInt(values, "benchRecoveryPerExchange", 0),
                FrameDelayMilliseconds = parameters.ReadInt(values, "frameDelayMilliseconds", 650),
                SubtitleHoldMilliseconds = parameters.ReadInt(values, "subtitleHoldMilliseconds", 1500),
                MaximumEntrants = parameters.ReadInt(values, "maximumEntrants", 8),
                MinimumEntrants = parameters.ReadInt(values, "minimumEntrants", 2),
                TimelineLimit = parameters.ReadInt(values, "timelineLimit", 12),
                MaximumPresentationFrames = parameters.ReadInt(values, "maximumPresentationFrames", 6),
                MemberNameSuffixes = parameters.ReadStrings(values, "memberNameSuffixes").ToList(),
                RigDescriptorFields = parameters.ReadStrings(values, "rigDescriptorFields").ToList(),
                MoveTags = parameters.ReadStrings(values, "moveTags").ToList(),
                MoveFallbackToContent = parameters.ReadBool(values, "moveFallbackToContent", true),
                OwnerActionTags = parameters.ReadStrings(values, "ownerActionTags").ToList(),
                OwnerActionFallbackToContent = parameters.ReadBool(values, "ownerActionFallbackToContent", false),
                SwitchTargetTag = parameters.ReadString(values, "switchTargetTag", string.Empty),
                HitPose = parameters.ReadString(values, "hitPose", "hit"),
                Parameters = values
            };
            if (settings.MemberNameSuffixes.Count == 0)
                settings.MemberNameSuffixes.Add(string.Empty);
            settings.Moves = parameters.ReadObjects(values, "moves").Select(move => new DuelMoveDefinition
            {
                Key = parameters.ReadString(move, "key", string.Empty),
                Words = parameters.ReadStrings(move, "words").ToList(),
                IsDefault = parameters.ReadBool(move, "default", false),
                Attacks = parameters.ReadBool(move, "attacks", false),
                Heal = parameters.ReadInt(move, "heal", 0),
                IncomingReduction = parameters.ReadInt(move, "incomingReduction", 0),
                Pose = parameters.ReadString(move, "pose", "idle")
            }).ToList();
            settings.OwnerActions = parameters.ReadObjects(values, "ownerActions").Select(action => new DuelOwnerActionDefinition
            {
                Key = parameters.ReadString(action, "key", string.Empty),
                Words = parameters.ReadStrings(action, "words").ToList(),
                IsDefault = parameters.ReadBool(action, "default", false),
                Switches = parameters.ReadBool(action, "switches", false),
                AttackBonus = parameters.ReadInt(action, "attackBonus", 0),
                IncomingReduction = parameters.ReadInt(action, "incomingReduction", 0),
                Heal = parameters.ReadInt(action, "heal", 0)
            }).ToList();
            settings.EvidenceTags = parameters.ReadObjects(values, "evidenceTags").Select(tag => new DuelEvidenceTag
            {
                Token = parameters.ReadString(tag, "token", string.Empty),
                Side = parameters.ReadString(tag, "side", string.Empty),
                Tag = parameters.ReadString(tag, "tag", string.Empty)
            }).ToList();
            if (settings.Moves.Count == 0 || settings.OwnerActions.Count == 0)
                throw new InvalidOperationException($"Duel rule '{rule.RuleKey}' declares no moves or no owner actions.");
            return settings;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading duel settings of rule {RuleKey} failed.", rule.RuleKey);
            throw;
        }
    }
}
