namespace LocalGPT.BusinessObjects;

/// <summary>Parameters of <c>localgpt.configuration.diagnose</c>.</summary>
public sealed class ConfigurationDiagnoseParameters
{
    /// <summary>Gets or sets an optional category filter (council, runtime-values, teams, tools).</summary>
    /// <value>Category, or empty for all checks.</value>
    public string Category { get; set; } = string.Empty;
}
