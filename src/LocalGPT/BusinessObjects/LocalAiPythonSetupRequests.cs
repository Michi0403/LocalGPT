namespace LocalGPT.BusinessObjects;

/// <summary>Selects one discovered Python executable for LocalGPT's managed local-AI runtime binding.</summary>
public sealed class LocalAiPythonConfigureRequest
{
    /// <summary>Gets or sets the discovered Python executable selected for the managed runtime binding.</summary>
    /// <value>An absolute executable path that must match a currently discovered Python candidate.</value>
    public string ExecutablePath { get; set; } = string.Empty;
}

/// <summary>Selects one configured LocalGPT Python package profile for installation.</summary>
public sealed class LocalAiPythonPackageInstallRequest
{
    /// <summary>Gets or sets the configured package-profile key selected for installation.</summary>
    /// <value>A package-profile key from the Local AI runtime configuration.</value>
    public string ProfileKey { get; set; } = string.Empty;
}
