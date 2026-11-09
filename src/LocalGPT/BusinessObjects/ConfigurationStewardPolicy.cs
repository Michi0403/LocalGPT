namespace LocalGPT.BusinessObjects;

/// <summary>Database-backed thresholds of the configuration steward (runtime value <c>ConfigurationStewardPolicyJson</c>).</summary>
public sealed class ConfigurationStewardPolicy
{
    /// <summary>Gets or sets the default Council power below which a finding is raised.</summary>
    /// <value>Minimum recommended load percent.</value>
    public int MinimumRecommendedCouncilLoadPercent { get; set; } = 60;

    /// <summary>Gets or sets the recommended default Council power.</summary>
    /// <value>Recommended load percent.</value>
    public int RecommendedCouncilLoadPercent { get; set; } = 100;

    /// <summary>Gets or sets the alternative, more conservative default Council power.</summary>
    /// <value>Alternative load percent.</value>
    public int AlternativeCouncilLoadPercent { get; set; } = 60;

    /// <summary>Gets or sets the automatic road's minimum output tokens used to explain a load value.</summary>
    /// <value>Reference minimum output tokens.</value>
    public int ReferenceAutoRoadMinimumOutputTokens { get; set; } = 256;

    /// <summary>Gets or sets the automatic road's maximum output tokens used to explain a load value.</summary>
    /// <value>Reference maximum output tokens.</value>
    public int ReferenceAutoRoadMaximumOutputTokens { get; set; } = 4096;

    /// <summary>Gets or sets the automatic road's minimum context tokens used to explain a load value.</summary>
    /// <value>Reference minimum context tokens.</value>
    public int ReferenceAutoRoadMinimumContextTokens { get; set; } = 2048;

    /// <summary>Gets or sets the automatic road's maximum context tokens used to explain a load value.</summary>
    /// <value>Reference maximum context tokens.</value>
    public int ReferenceAutoRoadMaximumContextTokens { get; set; } = 32768;

    /// <summary>Gets or sets the number of native tools above which attaching all tools to unlisted steps is reported.</summary>
    /// <value>Native tool threshold.</value>
    public int MaximumNativeToolsWithoutPreference { get; set; } = 64;

    /// <summary>Gets or sets the maximum findings returned by one diagnosis.</summary>
    /// <value>Finding limit.</value>
    public int MaximumFindings { get; set; } = 25;

    /// <summary>Gets or sets the risk level shown on repair approval requests.</summary>
    /// <value>Approval risk level.</value>
    public string ApprovalRiskLevel { get; set; } = "High";
}
