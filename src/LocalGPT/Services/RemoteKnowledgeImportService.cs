using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Downloads a user-selected public GitHub repository or webpage into the same local learn-base style
/// cache used by LocalGPT, lists every returned file, applies a regex file policy, and then delegates
/// database extraction to the existing learn-base importer.
/// </summary>
public sealed partial class RemoteKnowledgeImportService : IRemoteKnowledgeImportService, IDisposable
{
    /// <summary>
    /// Stores the learn base knowledge importer service dependency used by <see cref="RemoteKnowledgeImportService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ILearnBaseKnowledgeImporterService learnBaseImporter;
    /// <summary>
    /// Stores the council knowledge service dependency used by <see cref="RemoteKnowledgeImportService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly ICouncilKnowledgeService knowledge;
    /// <summary>
    /// Stores the regex pattern service dependency used by <see cref="RemoteKnowledgeImportService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IRegexPatternService regexPatterns;
    /// <summary>
    /// Stores the local GPT catalog service dependency used by <see cref="RemoteKnowledgeImportService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly LocalGptCatalogService catalog;
    /// <summary>Stores host filesystem semantics behind the injected platform boundary.</summary>
    private readonly IPlatformRuntimeService platform;
    /// <summary>
    /// Stores the logger used by <see cref="RemoteKnowledgeImportService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<RemoteKnowledgeImportService> logger;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="learnBaseImporter">Injected dependency used by the RemoteKnowledgeImportService.</param>
    /// <param name="knowledge">Injected dependency used by the RemoteKnowledgeImportService.</param>
    /// <param name="regexPatterns">Injected dependency used by the RemoteKnowledgeImportService.</param>
    /// <param name="catalog">Injected dependency used by the RemoteKnowledgeImportService.</param>
    /// <param name="platform">Injected host filesystem/platform semantics.</param>
    /// <param name="logger">Injected dependency used by the RemoteKnowledgeImportService.</param>
    public RemoteKnowledgeImportService(
        ILearnBaseKnowledgeImporterService learnBaseImporter,
        ICouncilKnowledgeService knowledge,
        IRegexPatternService regexPatterns,
        LocalGptCatalogService catalog,
        IPlatformRuntimeService platform,
        ILogger<RemoteKnowledgeImportService> logger)
    {
        this.learnBaseImporter = learnBaseImporter;
        this.knowledge = knowledge;
        this.regexPatterns = regexPatterns;
        this.catalog = catalog;
        this.platform = platform;
        this.logger = logger;
    }

    /// <summary>
    /// Stores the internal dispose state state used by <see cref="RemoteKnowledgeImportService"/> while executing its surrounding workflow.
    /// </summary>
    private int disposeState;
    /// <summary>
    /// Stores the HTTP client dependency used by <see cref="RemoteKnowledgeImportService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly HttpClient http = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        UseCookies = false
    })
    {
        Timeout = TimeSpan.FromMinutes(10)
    };


    /// <summary>
    /// Parses labels as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">Values value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public List<string> ParseLabels(params string?[] values)
    {
        try
        {
            ThrowIfDisposed();
            var labels = new List<string>();
            foreach (var value in values ?? [])
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                labels.AddRange(value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            var result = labels
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(64)
                .ToList();
            logger.LogDebug("Normalized {RemoteKnowledgeLabelCount} remote-knowledge role/topic label(s).", result.Count);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote-knowledge role/topic label normalization failed; label content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs import as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote knowledge import result produced by the operation.</returns>
    public async Task<RemoteKnowledgeImportResult> ImportAsync(
        RemoteKnowledgeImportRequest request,
        CancellationToken cancellationToken = default)
    {
        Uri? sourceUri = null;
        try
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(request);
            if (!Uri.TryCreate(request.SourceUrl?.Trim(), UriKind.Absolute, out var parsedSourceUri) ||
                parsedSourceUri.Scheme is not ("http" or "https"))
                throw new ArgumentException("A public absolute http/https SourceUrl is required.", nameof(request));
            sourceUri = parsedSourceUri;
            if (!request.PreviewOnly && request.SaveToKnowledge && !request.UserConfirmed)
                throw new InvalidOperationException("Fresh user confirmation is required before remote content is saved to Council knowledge.");

            await EnsurePublicHostAsync(sourceUri, cancellationToken).ConfigureAwait(false);
            var includeRegex = BuildIncludeRegex(request.FileIncludeRegex);
            var maxFiles = request.MaxFiles > 0 ? Math.Min(request.MaxFiles, catalog.MaxFiles) : catalog.MaxFiles;
            var sourceKind = ResolveKind(request.SourceKind, sourceUri);
            var cacheRoot = BuildCacheRoot(sourceUri);
            Directory.CreateDirectory(cacheRoot);
            ClearDirectory(cacheRoot);

            var result = new RemoteKnowledgeImportResult
            {
                SourceUrl = sourceUri.AbsoluteUri,
                SourceKind = sourceKind,
                CacheRoot = cacheRoot,
                AppliedTags = BuildTags(request)
            };

            if (sourceKind == "GitHub")
                await DownloadGitHubAsync(sourceUri, request, result, includeRegex, maxFiles, cancellationToken).ConfigureAwait(false);
            else
                await DownloadWebAsync(sourceUri, request, result, includeRegex, maxFiles, cancellationToken).ConfigureAwait(false);

            result.DownloadedFileCount = result.Files.Count;
            result.MatchedFileCount = result.Files.Count(item => item.MatchesFilePolicy);
            if (result.MatchedFileCount == 0)
            {
                result.Warnings.Add("The source downloaded successfully, but no returned file matched the configured regex/file-ending policy.");
                return result;
            }

            if (!request.PreviewOnly)
            {
                var selectedRoot = PrepareMatchedImportRoot(result);
                result.LearnBaseResult = await learnBaseImporter.ImportAsync(new LearnBaseImportRequest
                {
                    RootPath = selectedRoot,
                    MaxProjects = Math.Clamp(Math.Min(120, Math.Max(1, result.MatchedFileCount)), 1, 120),
                    SaveToKnowledge = request.SaveToKnowledge
                }, cancellationToken).ConfigureAwait(false);
                result.ImportedKnowledgeCount = result.LearnBaseResult.SavedKnowledgeCount;
                await ApplyRoleAndTopicTagsAsync(result, cancellationToken).ConfigureAwait(false);
                foreach (var file in result.Files.Where(item => item.MatchesFilePolicy))
                {
                    file.Imported = request.SaveToKnowledge && result.ImportedKnowledgeCount > 0;
                    file.Status = request.SaveToKnowledge ? "Passed to learn-base extractor" : "Downloaded and inspected";
                }
            }

            logger.LogInformation(
                "Remote knowledge import completed for host {SourceHost}: {DownloadedFileCount} file(s), {MatchedFileCount} matched, {KnowledgeCount} knowledge entry/entries; URL paths and content were omitted.",
                sourceUri.Host,
                result.DownloadedFileCount,
                result.MatchedFileCount,
                result.ImportedKnowledgeCount);
            return result;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                exception,
                "Remote knowledge import for host {SourceHost} was cancelled by the caller.",
                sourceUri?.Host ?? "unresolved");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Remote knowledge import failed for host {SourceHost}; URL paths and downloaded content were omitted.",
                sourceUri?.Host ?? "unresolved");
            throw;
        }
    }
}
