using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;

namespace LocalGPT.Services.Persistence;

/// <summary>Owns repository and embedded-resource reads used to feed initial database content.</summary>
/// <param name="environment">Web host environment used to resolve the source repository root.</param>
/// <param name="runtimePolicySeed">Runtime seed catalog that defines which initial knowledge files are authoritative.</param>
/// <param name="platform">Platform runtime service used for safe cross-platform path containment.</param>
/// <param name="logger">Logger used to record diagnostics produced while initial feed sources are read.</param>
public sealed class InitialDataFeedService(
    IWebHostEnvironment environment,
    ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
    IPlatformRuntimeService platform,
    ILogger<InitialDataFeedService> logger) : IInitialDataFeedService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InitialDataFeedEntry>> LoadKnowledgeFeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var root = ResolveRepositoryRoot(environment.ContentRootPath);
            var configuredFiles = runtimePolicySeed.GetSeed().Collections
                .Single(item => item.Key == LocalGptRuntimeCollection.KnowledgeFiles)
                .Values;
            var entries = new List<InitialDataFeedEntry>(configuredFiles.Count);

            foreach (var relativePath in configuredFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsSupportedFeedPath(relativePath))
                {
                    logger.LogWarning("Configured initial knowledge feed path {RelativePath} is not a supported Markdown/SQL/text feed and was skipped.", relativePath);
                    continue;
                }

                var normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
                var path = Path.GetFullPath(Path.Combine(root, normalizedRelative));
                if (!platform.IsSameOrDescendantPath(root, path))
                {
                    logger.LogWarning("Configured initial knowledge feed path {RelativePath} resolved outside the repository root and was skipped.", relativePath);
                    continue;
                }

                string? content;
                string source;
                DateTime sourceDateUtc;
                if (File.Exists(path))
                {
                    content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                    source = $"repository:{relativePath.Replace('\\', '/')}";
                    sourceDateUtc = File.GetLastWriteTimeUtc(path);
                }
                else
                {
                    content = await TryReadEmbeddedAsync(relativePath, cancellationToken).ConfigureAwait(false);
                    source = $"embedded:{relativePath.Replace('\\', '/')}";
                    sourceDateUtc = DateTime.UnixEpoch;
                }

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                entries.Add(new InitialDataFeedEntry(relativePath.Replace('\\', '/'), content, source, sourceDateUtc));
            }

            return entries;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the configured initial knowledge data feed failed.");
            throw;
        }
    }

    /// <summary>Determines whether a configured source belongs to the text-based initial feed formats maintained by the data catalog.</summary>
    /// <param name="relativePath">Repository-relative configured source path.</param>
    /// <returns><see langword="true"/> when the path is a supported initial feed source.</returns>
    private bool IsSupportedFeedPath(string relativePath)
    {
        try
        {
            var extension = Path.GetExtension(relativePath);
            return extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".sql", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"Initial data feed path classification failed: {exception}");
            throw;
        }
    }

    /// <summary>Resolves the repository root that owns configured initial data feed files.</summary>
    /// <param name="contentRoot">Current host content root.</param>
    /// <returns>The resolved repository root.</returns>
    private string ResolveRepositoryRoot(string contentRoot)
    {
        try
        {
            var current = new DirectoryInfo(contentRoot);
            for (var depth = 0; current is not null && depth < 6; depth++, current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")) &&
                    Directory.Exists(Path.Combine(current.FullName, "docs")))
                    return current.FullName;
            }
            return contentRoot;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"Initial data feed repository-root resolution failed: {exception}");
            throw;
        }
    }

    /// <summary>Reads an approved embedded fallback for initial data that may not be loose in a publish layout.</summary>
    /// <param name="relativePath">Repository-relative configured source path.</param>
    /// <param name="cancellationToken">Cancellation token for the bounded read.</param>
    /// <returns>Embedded content or <see langword="null"/> when no fallback exists.</returns>
    private async Task<string?> TryReadEmbeddedAsync(string relativePath, CancellationToken cancellationToken)
    {
        try
        {
            var resourceName = relativePath.Replace('\\', '/').ToLowerInvariant() switch
            {
                "docs/reference/ai-provider-installation.md" => "LocalGPT.Knowledge.ai-provider-installation.md",
                "docs/reference/ascii-game-authoring.md" => "LocalGPT.Knowledge.ascii-game-authoring.md",
                "docs/reference/toolchain-discovery.md" => "LocalGPT.Knowledge.toolchain-discovery.md",
                "docs/reference/runtime-path-layout.md" => "LocalGPT.Knowledge.runtime-path-layout.md",
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;

            using var stream = typeof(InitialDataFeedService).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                logger.LogWarning("Embedded fallback initial data resource {ResourceName} is unavailable.", resourceName);
                return null;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            cancellationToken.ThrowIfCancellationRequested();
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read embedded fallback initial data for {RelativePath}.", relativePath);
            return null;
        }
    }
}
