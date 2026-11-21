namespace LocalGPT.BusinessObjects;

/// <summary>Compact server-origin state for a Council draft or its immutable submitted review.</summary>
/// <param name="ReviewId">Durable draft or review identifier.</param>
/// <param name="ProjectId">Bound database project identifier.</param>
/// <param name="ProjectRevisionId">Base project revision identifier.</param>
/// <param name="Status">Actual persisted review status.</param>
/// <param name="ReviewHash">Immutable submitted review hash, if available.</param>
/// <param name="BuildStatus">Persisted build status, not a model assertion.</param>
/// <param name="WorkspaceName">Approved generated workspace identity, if available.</param>
/// <param name="ZipFileName">Actual recorded archive name, if available.</param>
/// <param name="MissingPaths">Manifest paths without a committed file receipt.</param>
/// <param name="CompletedFiles">Accepted paths and content hashes, without source payloads.</param>
/// <param name="WorkspaceMode">Single authoritative workspace route.</param>
/// <param name="GuestLanguageKey">Selected isolated generation language for the workspace.</param>
/// <param name="IsDraft">Whether the source draft can still be authored.</param>
public sealed record CodeGenerationDraftProgress(
    Guid ReviewId, Guid? ProjectId, Guid? ProjectRevisionId, string Status, string ReviewHash,
    string BuildStatus, string WorkspaceName, string ZipFileName, IReadOnlyList<string> MissingPaths,
    IReadOnlyDictionary<string, string> CompletedFiles, bool IsDraft, string WorkspaceMode, string GuestLanguageKey)
{
    /// <summary>Persisted number of pre-approval source amendment commits, bounded by the current Council policy.</summary>
    public int AmendmentCount { get; init; }
    /// <summary>Exact hash of the persisted source manifest; the coordinator uses it to fence additive replans.</summary>
    public string ManifestSha256 { get; init; } = string.Empty;
    /// <summary>Current manifest paths and short work orders; not accepted file bytes.</summary>
    public IReadOnlyDictionary<string, string> ManifestWorkOrders { get; init; } = new Dictionary<string, string>();
    /// <summary>Read-only file progress projected from the persisted manifest, assignments and inspections.</summary>
    public IReadOnlyList<CodeGenerationDraftPathProgress> Paths { get; init; } = [];
    /// <summary>Hierarchical view of the same persisted path receipts, prepared only for the detailed progress request.</summary>
    public IReadOnlyList<CodeGenerationDraftTreeNode> Tree { get; init; } = [];
}
