namespace LocalGPT.BusinessObjects;

/// <summary>
/// Immutable runtime settings for the local SQLite store.
/// </summary>
public sealed record LocalGptDatabaseOptions(
    string DatabasePath,
    int ProbeCommandTimeoutSeconds = 5)
{
    public const string SectionName = "LocalGptDatabase";
}
