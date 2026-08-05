using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

internal sealed record ReviewedSourceArtifact(string RelativePath, string FullPath);

internal sealed record OneWireConnectionRegistration(
    Guid Id,
    Func<OneWireEnvelope, CancellationToken, Task> Sender);

internal sealed record CouncilTextCachedPattern(
    string Pattern,
    string Flags,
    int TimeoutMilliseconds,
    Regex Regex);

internal sealed record DatabaseMigrationSignature(
    string Id,
    string ProductVersion,
    DatabaseSchemaRequirement[] Requirements);

internal sealed record DatabaseSchemaRequirement(string TableName, string? ColumnName);

internal enum DatabaseMigrationSignatureState
{
    Missing,
    Partial,
    Complete
}

internal enum DatabaseProbeResult
{
    Healthy,
    Corrupt,
    Inconclusive
}

internal sealed record LocalGptRuntimePolicyState(
    FrozenDictionary<LocalGptRuntimeValue, string> Values,
    FrozenDictionary<LocalGptRuntimeCollection, FrozenSet<string>> Collections,
    FrozenDictionary<LocalGptRuntimePattern, Regex> Patterns);

internal sealed record ProjectSourceManifestEntry(
    string RelativePath,
    string ContentHash,
    long SizeBytes);

internal sealed record ProjectTrackedSourceState(
    string Hash,
    string ManifestJson,
    IReadOnlyList<ProjectSourceManifestEntry> Entries);
