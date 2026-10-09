namespace LocalGPT.Interfaces;

/// <summary>Represents one source-backed initial data feed item before it is projected into persisted application data.</summary>
/// <param name="RelativePath">Repository-relative source path.</param>
/// <param name="Content">Text content supplied by the initial data source.</param>
/// <param name="Source">Stable source description used for diagnostics and provenance.</param>
/// <param name="SourceDateUtc">Last-write timestamp for repository files or a deterministic fallback timestamp for embedded content.</param>
public sealed record InitialDataFeedEntry(string RelativePath, string Content, string Source, DateTime SourceDateUtc);

/// <summary>Centralizes file and embedded-resource based initial data feeds so database bootstrap consumers do not own parallel Markdown or SQL loaders.</summary>
public interface IInitialDataFeedService
{
    /// <summary>Loads the configured initial knowledge feed from repository Markdown/SQL files or their approved embedded fallbacks.</summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The configured source-backed initial data feed entries.</returns>
    Task<IReadOnlyList<InitialDataFeedEntry>> LoadKnowledgeFeedAsync(CancellationToken cancellationToken = default);
}
