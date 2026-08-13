namespace LocalGPT.BusinessObjects;

/// <summary>
/// Immutable runtime settings for the local SQLite store.
/// </summary>
/// <param name="DatabasePath">Database path value supplied to the LocalGPT database operation and used when producing its result.</param>
/// <param name="ProbeCommandTimeoutSeconds">Probe command timeout seconds value supplied to the LocalGPT database operation and used when producing its result.</param>
public sealed record LocalGptDatabaseOptions(
    string DatabasePath,
    int ProbeCommandTimeoutSeconds = 5)
{
    /// <summary>
    /// Defines the section name constant used by <see cref="LocalGptDatabaseOptions"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SectionName = "LocalGptDatabase";
}
