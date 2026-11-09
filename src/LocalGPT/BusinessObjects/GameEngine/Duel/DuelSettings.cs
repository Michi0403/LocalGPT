using System.Text.Json;

namespace LocalGPT.BusinessObjects.GameEngine.Duel;

/// <summary>
/// The parsed parameters of a team duel progression rule row. Every value is game content; defaults are neutral engine values.
/// </summary>
public sealed class DuelSettings
{
    /// <summary>Gets or sets the rule row key, shown in transcript summaries.</summary>
    /// <value>The rule key.</value>
    public string RuleKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the archetype of entrant owners.</summary>
    /// <value>The owner archetype key.</value>
    public string OwnerArchetype { get; set; } = string.Empty;

    /// <summary>Gets or sets the archetype of team members.</summary>
    /// <value>The member archetype key.</value>
    public string MemberArchetype { get; set; } = string.Empty;

    /// <summary>Gets or sets the member health stat.</summary>
    /// <value>The health stat key.</value>
    public string HealthStat { get; set; } = "health";

    /// <summary>Gets or sets the starting and maximum member health.</summary>
    /// <value>The starting health.</value>
    public int StartingHealth { get; set; } = 100;

    /// <summary>Gets or sets the minimum attack damage.</summary>
    /// <value>The minimum damage.</value>
    public int MinimumDamage { get; set; } = 1;

    /// <summary>Gets or sets the maximum attack damage.</summary>
    /// <value>The maximum damage.</value>
    public int MaximumDamage { get; set; } = 10;

    /// <summary>Gets or sets the exchanges after which a match is decided by team health.</summary>
    /// <value>The exchange limit.</value>
    public int MaximumExchangesPerMatch { get; set; } = 10;

    /// <summary>Gets or sets the team size per owner.</summary>
    /// <value>The members per owner.</value>
    public int MembersPerOwner { get; set; } = 1;

    /// <summary>Gets or sets the owner switch budget per match.</summary>
    /// <value>The switch budget.</value>
    public int MaximumSwitchesPerMatch { get; set; }

    /// <summary>Gets or sets the health benched members recover per exchange.</summary>
    /// <value>The bench recovery.</value>
    public int BenchRecoveryPerExchange { get; set; }

    /// <summary>Gets or sets the delay between animation frames.</summary>
    /// <value>The frame delay in milliseconds.</value>
    public int FrameDelayMilliseconds { get; set; } = 650;

    /// <summary>Gets or sets how long subtitles stay visible.</summary>
    /// <value>The subtitle hold in milliseconds.</value>
    public int SubtitleHoldMilliseconds { get; set; } = 1500;

    /// <summary>Gets or sets the maximum number of entrants.</summary>
    /// <value>The entrant limit.</value>
    public int MaximumEntrants { get; set; } = 8;

    /// <summary>Gets or sets the minimum number of entrants.</summary>
    /// <value>The entrant minimum.</value>
    public int MinimumEntrants { get; set; } = 2;

    /// <summary>Gets or sets the number of kept timeline entries.</summary>
    /// <value>The timeline limit.</value>
    public int TimelineLimit { get; set; } = 12;

    /// <summary>Gets or sets the maximum number of Council presentation frames shown at initialization.</summary>
    /// <value>The presentation frame limit.</value>
    public int MaximumPresentationFrames { get; set; } = 6;

    /// <summary>Gets or sets the suffixes of reserve member names by slot.</summary>
    /// <value>The name suffixes; slot 0 keeps the entrant name.</value>
    public List<string> MemberNameSuffixes { get; set; } = [string.Empty];

    /// <summary>Gets or sets the member text fields joined into the rig descriptor.</summary>
    /// <value>The descriptor fields.</value>
    public List<string> RigDescriptorFields { get; set; } = [];

    /// <summary>Gets or sets the move definitions in keyword priority order.</summary>
    /// <value>The moves.</value>
    public List<DuelMoveDefinition> Moves { get; set; } = [];

    /// <summary>Gets or sets the owner action definitions in keyword priority order.</summary>
    /// <value>The owner actions.</value>
    public List<DuelOwnerActionDefinition> OwnerActions { get; set; } = [];

    /// <summary>Gets or sets the member evidence tags naming the move, in priority order.</summary>
    /// <value>The move tags.</value>
    public List<string> MoveTags { get; set; } = [];

    /// <summary>Gets or sets whether untagged member evidence is searched for move keywords.</summary>
    /// <value><c>true</c> to fall back to the whole evidence.</value>
    public bool MoveFallbackToContent { get; set; } = true;

    /// <summary>Gets or sets the owner evidence tags naming the action, in priority order.</summary>
    /// <value>The owner action tags.</value>
    public List<string> OwnerActionTags { get; set; } = [];

    /// <summary>Gets or sets whether untagged owner evidence is searched for action keywords.</summary>
    /// <value><c>true</c> to fall back to the whole evidence.</value>
    public bool OwnerActionFallbackToContent { get; set; }

    /// <summary>Gets or sets the owner evidence tag naming the switch target.</summary>
    /// <value>The switch target tag.</value>
    public string SwitchTargetTag { get; set; } = string.Empty;

    /// <summary>Gets or sets the tagged evidence values exposed to storyboard frames.</summary>
    /// <value>The evidence tags.</value>
    public List<DuelEvidenceTag> EvidenceTags { get; set; } = [];

    /// <summary>Gets or sets the pose of a member that received damage.</summary>
    /// <value>The hit pose.</value>
    public string HitPose { get; set; } = "hit";

    /// <summary>Gets or sets the raw text and sprite parameters (limits, templates, sprite legend).</summary>
    /// <value>The raw rule parameters.</value>
    public JsonElement Parameters { get; set; }
}
