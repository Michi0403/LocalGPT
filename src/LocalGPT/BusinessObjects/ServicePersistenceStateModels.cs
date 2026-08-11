using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a reviewed source artifact.
/// </summary>
internal sealed record ReviewedSourceArtifact(string RelativePath, string FullPath);

/// <summary>
/// Represents an one wire connection registration.
/// </summary>
internal sealed record OneWireConnectionRegistration(
    Guid Id,
    Func<OneWireEnvelope, CancellationToken, Task> Sender);

/// <summary>
/// Represents a council text cached pattern.
/// </summary>
internal sealed record CouncilTextCachedPattern(
    string Pattern,
    string Flags,
    int TimeoutMilliseconds,
    Regex Regex);

/// <summary>
/// Represents a database migration signature.
/// </summary>
internal sealed record DatabaseMigrationSignature(
    string Id,
    string ProductVersion,
    DatabaseSchemaRequirement[] Requirements);

/// <summary>
/// Represents a database schema requirement.
/// </summary>
internal sealed record DatabaseSchemaRequirement(string TableName, string? ColumnName);

/// <summary>
/// Lists supported database migration signature state values.
/// </summary>
internal enum DatabaseMigrationSignatureState
{
    Missing,
    Partial,
    Complete
}

/// <summary>
/// Lists supported database probe result values.
/// </summary>
internal enum DatabaseProbeResult
{
    Healthy,
    Corrupt,
    Inconclusive
}

/// <summary>
/// Represents a local gpt runtime policy state.
/// </summary>
internal sealed record LocalGptRuntimePolicyState(
    FrozenDictionary<LocalGptRuntimeValue, string> Values,
    FrozenDictionary<LocalGptRuntimeCollection, FrozenSet<string>> Collections,
    FrozenDictionary<LocalGptRuntimePattern, Regex> Patterns);

/// <summary>
/// Represents a project source manifest entry.
/// </summary>
internal sealed record ProjectSourceManifestEntry(
    string RelativePath,
    string ContentHash,
    long SizeBytes);

/// <summary>
/// Represents a project tracked source state.
/// </summary>
internal sealed record ProjectTrackedSourceState(
    string Hash,
    string ManifestJson,
    IReadOnlyList<ProjectSourceManifestEntry> Entries);
