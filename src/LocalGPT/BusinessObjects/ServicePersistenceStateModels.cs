using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a reviewed source artifact application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RelativePath">Relative path value supplied to the reviewed source artifact operation and used when producing its result.</param>
/// <param name="FullPath">Full path value supplied to the reviewed source artifact operation and used when producing its result.</param>
internal sealed record ReviewedSourceArtifact(string RelativePath, string FullPath);

/// <summary>
/// Represents an one wire connection registration application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Sender">Cancellation token that allows the caller to stop the asynchronous operation.</param>
internal sealed record OneWireConnectionRegistration(
    Guid Id,
    Func<OneWireEnvelope, CancellationToken, Task> Sender);

/// <summary>
/// Represents a council text cached pattern application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Pattern">Pattern value supplied to the council text cached pattern operation and used when producing its result.</param>
/// <param name="Flags">Flags value supplied to the council text cached pattern operation and used when producing its result.</param>
/// <param name="TimeoutMilliseconds">Timeout milliseconds value supplied to the council text cached pattern operation and used when producing its result.</param>
/// <param name="Regex">Regex value supplied to the council text cached pattern operation and used when producing its result.</param>
internal sealed record CouncilTextCachedPattern(
    string Pattern,
    string Flags,
    int TimeoutMilliseconds,
    Regex Regex);

/// <summary>
/// Represents a database migration signature application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="ProductVersion">Product version value supplied to the database migration signature operation and used when producing its result.</param>
/// <param name="Requirements">Requirements value supplied to the database migration signature operation and used when producing its result.</param>
internal sealed record DatabaseMigrationSignature(
    string Id,
    string ProductVersion,
    DatabaseSchemaRequirement[] Requirements);

/// <summary>
/// Represents a database schema requirement application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="TableName">Table name value supplied to the database schema requirement operation and used when producing its result.</param>
/// <param name="ColumnName">Column name value supplied to the database schema requirement operation and used when producing its result.</param>
internal sealed record DatabaseSchemaRequirement(string TableName, string? ColumnName);

/// <summary>
/// Defines the supported database migration signature values used to select or describe behavior in the surrounding workflow.
/// </summary>
internal enum DatabaseMigrationSignatureState
{
    /// <summary>
    /// Selects the missing option for <see cref="DatabaseMigrationSignatureState"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Missing,
    /// <summary>
    /// Selects the partial option for <see cref="DatabaseMigrationSignatureState"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Partial,
    /// <summary>
    /// Selects the complete option for <see cref="DatabaseMigrationSignatureState"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Complete
}

/// <summary>
/// Defines the supported database probe values used to select or describe behavior in the surrounding workflow.
/// </summary>
internal enum DatabaseProbeResult
{
    /// <summary>
    /// Selects the healthy option for <see cref="DatabaseProbeResult"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Healthy,
    /// <summary>
    /// Selects the corrupt option for <see cref="DatabaseProbeResult"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Corrupt,
    /// <summary>
    /// Selects the inconclusive option for <see cref="DatabaseProbeResult"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Inconclusive
}

/// <summary>
/// Represents LocalGPT runtime policy state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Values">Values value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
/// <param name="Collections">Collections value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
/// <param name="Patterns">Patterns value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
internal sealed record LocalGptRuntimePolicyState(
    FrozenDictionary<LocalGptRuntimeValue, string> Values,
    FrozenDictionary<LocalGptRuntimeCollection, FrozenSet<string>> Collections,
    FrozenDictionary<LocalGptRuntimePattern, Regex> Patterns);

/// <summary>
/// Represents project source manifest state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="RelativePath">Relative path value supplied to the project source manifest operation and used when producing its result.</param>
/// <param name="ContentHash">Content hash value supplied to the project source manifest operation and used when producing its result.</param>
/// <param name="SizeBytes">Size bytes value supplied to the project source manifest operation and used when producing its result.</param>
internal sealed record ProjectSourceManifestEntry(
    string RelativePath,
    string ContentHash,
    long SizeBytes);

/// <summary>
/// Represents project tracked source state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Hash">Hash value supplied to the project tracked source operation and used when producing its result.</param>
/// <param name="ManifestJson">Manifest json value supplied to the project tracked source operation and used when producing its result.</param>
/// <param name="Entries">Project source manifest entry dependency used by the project tracked source workflow to provide the corresponding application capability.</param>
internal sealed record ProjectTrackedSourceState(
    string Hash,
    string ManifestJson,
    IReadOnlyList<ProjectSourceManifestEntry> Entries);
