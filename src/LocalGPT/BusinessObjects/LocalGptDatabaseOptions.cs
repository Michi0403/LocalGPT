namespace LocalGPT.BusinessObjects;

/// <summary>
/// Immutable runtime settings for the local SQLite store.
/// </summary>
public sealed record LocalGptDatabaseOptions(
    string DatabasePath,
    int ProbeCommandTimeoutSeconds = 5)
{
    /// <summary>
    /// Stores section name.
    /// </summary>
    public const string SectionName = "LocalGptDatabase";
}
