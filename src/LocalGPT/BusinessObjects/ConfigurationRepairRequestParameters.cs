namespace LocalGPT.BusinessObjects;

/// <summary>Parameters of <c>localgpt.configuration.repair.request</c>.</summary>
public sealed class ConfigurationRepairRequestParameters
{
    /// <summary>Gets or sets the finding key returned by <c>localgpt.configuration.diagnose</c>.</summary>
    /// <value>Finding key.</value>
    public string FindingKey { get; set; } = string.Empty;
}
