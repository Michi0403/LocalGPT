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
/// <param name="learnBaseImporter">Learn base knowledge importer service dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
/// <param name="knowledge">Council knowledge service dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
/// <param name="regexPatterns">Regex pattern service dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RemoteKnowledgeImportService(
    ILearnBaseKnowledgeImporterService learnBaseImporter,
    ICouncilKnowledgeService knowledge,
    IRegexPatternService regexPatterns,
    LocalGptCatalogService catalog,
    ILogger<RemoteKnowledgeImportService> logger) : IRemoteKnowledgeImportService, IDisposable
{
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

    /// <summary>
    /// Performs download GitHub as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="includeRegex">Include regex value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="maxFiles">Max files value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadGitHubAsync(
        Uri sourceUri,
        RemoteKnowledgeImportRequest request,
        RemoteKnowledgeImportResult result,
        Regex includeRegex,
        int maxFiles,
        CancellationToken cancellationToken)
    {
    try
    {
            var segments = sourceUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                throw new ArgumentException("GitHub URL must include owner/repository.", nameof(request));
            var owner = SafeSegment(segments[0]);
            var repository = SafeSegment(segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? segments[1][..^4] : segments[1]);
            var branch = ResolveBranch(segments, request.Branch);
            var zipPath = Path.Combine(result.CacheRoot, $"{owner}-{repository}-{branch}.zip");
            var extractRoot = Path.Combine(result.CacheRoot, "source");
            Directory.CreateDirectory(extractRoot);

            var branches = new[] { branch, "main", "master" }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            HttpResponseMessage? response = null;
            string resolvedBranch = branch;
            foreach (var candidate in branches)
            {
                var archiveUri = new Uri($"https://codeload.github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/zip/refs/heads/{Uri.EscapeDataString(candidate)}");
                response?.Dispose();
                response = await SendPublicAsync(archiveUri, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    resolvedBranch = candidate;
                    break;
                }
            }
            using (response)
            {
                if (response is null || !response.IsSuccessStatusCode)
                    throw new HttpRequestException($"GitHub archive download failed with HTTP {(int?)response?.StatusCode ?? 0}.");
                await SaveLimitedResponseAsync(response, zipPath, cancellationToken).ConfigureAwait(false);
            }
            result.ResolvedRevision = resolvedBranch;
            ExtractZipSafely(zipPath, extractRoot);
            BuildFileResultList(extractRoot, sourceUri, result, includeRegex, maxFiles);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(DownloadGitHubAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(DownloadGitHubAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs download web as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="includeRegex">Include regex value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="maxFiles">Max files value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadWebAsync(
        Uri sourceUri,
        RemoteKnowledgeImportRequest request,
        RemoteKnowledgeImportResult result,
        Regex includeRegex,
        int maxFiles,
        CancellationToken cancellationToken)
    {
    try
    {
            var pageRoot = Path.Combine(result.CacheRoot, "web");
            Directory.CreateDirectory(pageRoot);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<Uri>();
            pending.Enqueue(sourceUri);
            var maxPages = request.MaxLinkedPages > 0 ? Math.Min(request.MaxLinkedPages, catalog.MaxFiles) : catalog.MaxFiles;
            var index = 0;

            while (pending.Count > 0 && index < maxPages && result.Files.Count < maxFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var uri = pending.Dequeue();
                if (!visited.Add(uri.AbsoluteUri)) continue;
                await EnsurePublicHostAsync(uri, cancellationToken).ConfigureAwait(false);
                using var response = await SendPublicAsync(uri, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    result.Warnings.Add($"Skipped one linked page with HTTP {(int)response.StatusCode}: {uri.Host}");
                    continue;
                }
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var extension = ExtensionFor(uri, mediaType);
                var fileName = $"page-{index:000}{extension}";
                var localPath = Path.Combine(pageRoot, fileName);
                await SaveLimitedResponseAsync(response, localPath, cancellationToken).ConfigureAwait(false);
                var fileInfo = new FileInfo(localPath);
                var matched = includeRegex.IsMatch(fileName);
                result.Files.Add(new RemoteKnowledgeImportFile
                {
                    RelativePath = Path.GetRelativePath(result.CacheRoot, localPath),
                    SourceUrl = uri.AbsoluteUri,
                    Length = fileInfo.Length,
                    MatchesFilePolicy = matched,
                    Status = matched ? "Downloaded; matches file policy" : "Downloaded; excluded by file policy"
                });

                if (mediaType.Contains("html", StringComparison.OrdinalIgnoreCase) && fileInfo.Length <= 8 * 1024 * 1024)
                {
                    var html = await File.ReadAllTextAsync(localPath, cancellationToken).ConfigureAwait(false);
                    var textPath = Path.Combine(pageRoot, $"page-{index:000}.txt");
                    var plainText = HtmlToText(html);
                    await File.WriteAllTextAsync(textPath, plainText, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                    result.Files.Add(new RemoteKnowledgeImportFile
                    {
                        RelativePath = Path.GetRelativePath(result.CacheRoot, textPath),
                        SourceUrl = uri.AbsoluteUri,
                        Length = new FileInfo(textPath).Length,
                        MatchesFilePolicy = includeRegex.IsMatch($"page-{index:000}.txt"),
                        Status = includeRegex.IsMatch($"page-{index:000}.txt")
                            ? "Extracted readable page text; matches file policy"
                            : "Extracted readable page text; excluded by file policy"
                    });

                    foreach (var hrefValue in ExtractHrefValues(html))
                    {
                        if (!Uri.TryCreate(uri, WebUtility.HtmlDecode(hrefValue), out var linked)) continue;
                        if (linked.Scheme is not ("http" or "https") || !string.Equals(linked.Host, sourceUri.Host, StringComparison.OrdinalIgnoreCase)) continue;
                        var candidateName = Path.GetFileName(linked.AbsolutePath);
                        if (string.IsNullOrWhiteSpace(candidateName) || includeRegex.IsMatch(candidateName) || linked.AbsolutePath.EndsWith('/'))
                            pending.Enqueue(linked);
                    }
                }
                index++;
            }
            result.ResolvedRevision = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(DownloadWebAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(DownloadWebAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists limited response as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="response">Response value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveLimitedResponseAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken)
    {
    try
    {
            var maximumDownloadBytes = Math.Max(1L, catalog.MaxTotalFileBytes);
            if (response.Content.Headers.ContentLength is long length && length > maximumDownloadBytes)
                throw new InvalidDataException($"Remote content is larger than the database-backed MaxTotalFileBytes policy ({maximumDownloadBytes:n0} bytes).");
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > maximumDownloadBytes)
                    throw new InvalidDataException($"Remote content exceeded the database-backed MaxTotalFileBytes policy ({maximumDownloadBytes:n0} bytes).");
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SaveLimitedResponseAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SaveLimitedResponseAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs extract ZIP safely as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="zipPath">Zip path value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="targetRoot">Target root value supplied to the remote knowledge import operation and used when producing its result.</param>
    private void ExtractZipSafely(string zipPath, string targetRoot)
    {
    try
    {
            using var archive = ZipFile.OpenRead(zipPath);
            var maximumZipEntries = Math.Max(1, catalog.MaxZipEntries);
            var maximumExtractedBytes = Math.Max(1L, catalog.MaxExtractedBytes);
            var maximumZipEntryBytes = Math.Max(1L, catalog.MaxZipEntryBytes);
            if (archive.Entries.Count > maximumZipEntries)
                throw new InvalidDataException($"Archive has more entries than the database-backed MaxZipEntries policy ({maximumZipEntries:n0}).");
            var normalizedRoot = Path.GetFullPath(targetRoot) + Path.DirectorySeparatorChar;
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.Length > maximumZipEntryBytes)
                    throw new InvalidDataException($"Archive entry exceeds the database-backed MaxZipEntryBytes policy ({maximumZipEntryBytes:n0} bytes).");
                total += Math.Max(0, entry.Length);
                if (total > maximumExtractedBytes)
                    throw new InvalidDataException($"Archive exceeds the database-backed MaxExtractedBytes policy ({maximumExtractedBytes:n0} bytes).");
                var destination = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
                if (!destination.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Archive contains an unsafe traversal path.");
                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destination);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: true);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtractZipSafely)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtractZipSafely)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds file result list as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="includeRegex">Include regex value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="maxFiles">Max files value supplied to the remote knowledge import operation and used when producing its result.</param>
    private void BuildFileResultList(string root, Uri sourceUri, RemoteKnowledgeImportResult result, Regex includeRegex, int maxFiles)
    {
    try
    {
            foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Take(maxFiles))
            {
                var relative = Path.GetRelativePath(result.CacheRoot, path);
                var matched = includeRegex.IsMatch(relative.Replace(Path.DirectorySeparatorChar, '/'));
                result.Files.Add(new RemoteKnowledgeImportFile
                {
                    RelativePath = relative,
                    SourceUrl = sourceUri.AbsoluteUri,
                    Length = new FileInfo(path).Length,
                    MatchesFilePolicy = matched,
                    Status = matched ? "Extracted; matches file policy" : "Extracted; excluded by file policy"
                });
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildFileResultList)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildFileResultList)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs prepare matched import root as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string PrepareMatchedImportRoot(RemoteKnowledgeImportResult result)
    {
    try
    {
            var selectedRoot = Path.Combine(result.CacheRoot, "selected-for-import");
            if (Directory.Exists(selectedRoot)) Directory.Delete(selectedRoot, recursive: true);
            Directory.CreateDirectory(selectedRoot);
            var cacheFull = Path.GetFullPath(result.CacheRoot) + Path.DirectorySeparatorChar;
            var selectedFull = Path.GetFullPath(selectedRoot) + Path.DirectorySeparatorChar;
            foreach (var file in result.Files.Where(item => item.MatchesFilePolicy))
            {
                var source = Path.GetFullPath(Path.Combine(result.CacheRoot, file.RelativePath));
                if (!source.StartsWith(cacheFull, StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(source))
                    continue;
                var relative = Path.GetRelativePath(result.CacheRoot, source);
                var destination = Path.GetFullPath(Path.Combine(selectedRoot, relative));
                if (!destination.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("A selected import path escaped the bounded staging directory.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
            }
            return selectedRoot;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(PrepareMatchedImportRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(PrepareMatchedImportRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies role and topic tags as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ApplyRoleAndTopicTagsAsync(RemoteKnowledgeImportResult result, CancellationToken cancellationToken)
    {
    try
    {
            if (result.AppliedTags.Count == 0 || result.LearnBaseResult is null) return;
            var ids = result.LearnBaseResult.Projects
                .Where(item => item.KnowledgeEntryId.HasValue)
                .Select(item => item.KnowledgeEntryId!.Value)
                .ToHashSet();
            if (ids.Count == 0) return;
            var entries = await knowledge.GetEntriesAsync(includeArchived: true, take: 500, cancellationToken).ConfigureAwait(false);
            foreach (var entry in entries.Where(item => ids.Contains(item.Id)))
            {
                entry.Tags = string.Join("; ", (entry.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Concat(result.AppliedTags)).Distinct(StringComparer.OrdinalIgnoreCase));
                entry.HelpfulSources = string.IsNullOrWhiteSpace(entry.HelpfulSources)
                    ? result.SourceUrl
                    : entry.HelpfulSources + Environment.NewLine + result.SourceUrl;
                entry.Source = result.SourceUrl;
                entry.IsUserApproved = true;
                entry.VerificationStatus = "SourceBacked";
                entry.LastVerifiedAtUtc = DateTime.UtcNow;
                await knowledge.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ApplyRoleAndTopicTagsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ApplyRoleAndTopicTagsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds tags as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> BuildTags(RemoteKnowledgeImportRequest request) {
    try
    {
        return (request.RoleKeys ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => "role:" + item.Trim())
            .Concat((request.Topics ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => "topic:" + item.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(128)
            .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildTags)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildTags)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves kind as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requested">Requested value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveKind(string? requested, Uri sourceUri)
    {
    try
    {
            if (string.Equals(requested, "GitHub", StringComparison.OrdinalIgnoreCase)) return "GitHub";
            if (string.Equals(requested, "Web", StringComparison.OrdinalIgnoreCase) || string.Equals(requested, "Website", StringComparison.OrdinalIgnoreCase)) return "Web";
            return sourceUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ? "GitHub" : "Web";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveKind)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveKind)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves branch as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="segments">Segments value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="requested">Requested value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveBranch(string[] segments, string? requested)
    {
    try
    {
            var treeIndex = Array.FindIndex(segments, item => item.Equals("tree", StringComparison.OrdinalIgnoreCase));
            if (treeIndex >= 0 && treeIndex + 1 < segments.Length) return SafeSegment(segments[treeIndex + 1]);
            return SafeSegment(string.IsNullOrWhiteSpace(requested) ? "main" : requested.Trim());
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveBranch)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ResolveBranch)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds include regex as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    private Regex BuildIncludeRegex(string? pattern)
    {
    try
    {
            var value = string.IsNullOrWhiteSpace(pattern)
                ? @"(?i)\.(cs|razor|csproj|sln|json|xml|md|txt|ps1|cmd|sh|py|js|ts|tsx|css|scss|html|htm|php|c|h|cpp|hpp|java|kt|go|rs|sql|yml|yaml)$"
                : pattern.Trim();
            return regexPatterns.Compile(value, "CultureInvariant");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildIncludeRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildIncludeRegex)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds cache root as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCacheRoot(Uri sourceUri)
    {
    try
    {
            var safeHost = SanitizeSegment(sourceUri.Host, allowLeadingDot: true);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceUri.AbsoluteUri)))[..16].ToLowerInvariant();
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "LearningBase", "RemoteSources", safeHost, hash);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildCacheRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildCacheRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs safe segment as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeSegment(string value)
    {
    try
    {
            var safe = SanitizeSegment(value, allowLeadingDot: false).Trim('-', '.');
            if (string.IsNullOrWhiteSpace(safe)) throw new ArgumentException("A source path segment is empty or invalid.");
            return safe;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SafeSegment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SafeSegment)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs sanitize segment as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="allowLeadingDot">Value indicating whether allow leading dot should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SanitizeSegment(string? value, bool allowLeadingDot)
    {
    try
    {
            var source = value ?? string.Empty;
            var builder = new StringBuilder(source.Length);
            foreach (var character in source)
            {
                var allowed = char.IsAsciiLetterOrDigit(character) || character is '_' or '-' || character == '.';
                builder.Append(allowed ? character : '-');
            }
            var result = builder.ToString();
            return allowLeadingDot ? result : result.TrimStart('.');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SanitizeSegment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SanitizeSegment)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clear directory as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="directory">Directory value supplied to the remote knowledge import operation and used when producing its result.</param>
    private void ClearDirectory(string directory)
    {
    try
    {
            if (!Directory.Exists(directory)) return;
            foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
            foreach (var child in Directory.EnumerateDirectories(directory)) Directory.Delete(child, recursive: true);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ClearDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ClearDirectory)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs extension for as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="uri">Uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="mediaType">Media type value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ExtensionFor(Uri uri, string mediaType)
    {
    try
    {
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extension) && extension.Length <= 12) return extension;
            return mediaType switch
            {
                "text/html" => ".html",
                "application/json" => ".json",
                "text/plain" => ".txt",
                "text/markdown" => ".md",
                "application/xml" or "text/xml" => ".xml",
                _ => ".bin"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtensionFor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtensionFor)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs HTML to text as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string HtmlToText(string html)
    {
    try
    {
            var withoutScripts = RemoveElementBlocks(html, "script");
            withoutScripts = RemoveElementBlocks(withoutScripts, "style");
            withoutScripts = RemoveElementBlocks(withoutScripts, "noscript");
            return CollapseWhitespace(WebUtility.HtmlDecode(RemoveTags(withoutScripts))).Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(HtmlToText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(HtmlToText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs extract href values as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> ExtractHrefValues(string html)
    {
        try
        {
            var values = new List<string>();
            var index = 0;
            while (index < html.Length)
            {
                var hrefIndex = html.IndexOf("href", index, StringComparison.OrdinalIgnoreCase);
                if (hrefIndex < 0)
                    break;
                var cursor = hrefIndex + 4;
                while (cursor < html.Length && char.IsWhiteSpace(html[cursor])) cursor++;
                if (cursor >= html.Length || html[cursor] != '=')
                {
                    index = Math.Max(cursor, hrefIndex + 4);
                    continue;
                }
                cursor++;
                while (cursor < html.Length && char.IsWhiteSpace(html[cursor])) cursor++;
                if (cursor >= html.Length || html[cursor] is not ('\"' or '\''))
                {
                    index = Math.Max(cursor, hrefIndex + 4);
                    continue;
                }
                var quote = html[cursor++];
                var valueEnd = html.IndexOf(quote, cursor);
                if (valueEnd < 0)
                    break;
                var value = html[cursor..valueEnd].Trim();
                if (value.Length > 0 && !value.StartsWith('#'))
                    values.Add(value);
                index = valueEnd + 1;
            }

            logger.LogTrace("Extracted {RemoteHrefCount} same-page href candidate(s); HTML content was omitted.", values.Count);
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote HTML href extraction failed; HTML content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Removes element blocks as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="elementName">Element name value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RemoveElementBlocks(string html, string elementName)
    {
    try
    {
            var output = new StringBuilder(html.Length);
            var index = 0;
            var openToken = "<" + elementName;
            var closeToken = "</" + elementName + ">";
            while (index < html.Length)
            {
                var start = html.IndexOf(openToken, index, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                {
                    output.Append(html, index, html.Length - index);
                    break;
                }
                output.Append(html, index, start - index);
                var end = html.IndexOf(closeToken, start + openToken.Length, StringComparison.OrdinalIgnoreCase);
                if (end < 0)
                    break;
                index = end + closeToken.Length;
            }
            return output.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(RemoveElementBlocks)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(RemoveElementBlocks)} failed.");
        throw;
    }
}

    /// <summary>
    /// Removes tags as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RemoveTags(string html)
    {
    try
    {
            var output = new StringBuilder(html.Length);
            var insideTag = false;
            foreach (var character in html)
            {
                if (character == '<')
                {
                    insideTag = true;
                    output.Append(' ');
                }
                else if (character == '>')
                {
                    insideTag = false;
                    output.Append(' ');
                }
                else if (!insideTag)
                {
                    output.Append(character);
                }
            }
            return output.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(RemoveTags)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(RemoveTags)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs collapse whitespace as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CollapseWhitespace(string value)
    {
    try
    {
            var output = new StringBuilder(value.Length);
            var previousWasWhitespace = false;
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character))
                {
                    if (!previousWasWhitespace) output.Append(' ');
                    previousWasWhitespace = true;
                }
                else
                {
                    output.Append(character);
                    previousWasWhitespace = false;
                }
            }
            return output.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(CollapseWhitespace)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(CollapseWhitespace)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs send public as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="initialUri">Initial uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
    private async Task<HttpResponseMessage> SendPublicAsync(Uri initialUri, CancellationToken cancellationToken)
    {
    try
    {
            var current = initialUri;
            for (var redirect = 0; redirect <= 8; redirect++)
            {
                await EnsurePublicHostAsync(current, cancellationToken).ConfigureAwait(false);
                var response = await http.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode is < 300 or >= 400)
                    return response;

                var location = response.Headers.Location;
                response.Dispose();
                if (location is null)
                    throw new HttpRequestException("Remote source returned a redirect without a Location header.");
                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                if (current.Scheme is not ("http" or "https"))
                    throw new InvalidOperationException("Remote import redirects may only use http or https.");
            }

            throw new HttpRequestException("Remote source exceeded the maximum redirect count.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SendPublicAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SendPublicAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures public host as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="uri">Uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsurePublicHostAsync(Uri uri, CancellationToken cancellationToken)
    {
    try
    {
            if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Remote import does not access loopback/private hosts. Use the local learn-base path importer for local content.");
            var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken).ConfigureAwait(false);
            if (addresses.Length == 0 || addresses.Any(IsPrivateAddress))
                throw new InvalidOperationException("Remote import resolved to a private, local or link-local network address.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(EnsurePublicHostAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(EnsurePublicHostAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether private address as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="address">P address dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsPrivateAddress(IPAddress address)
    {
    try
    {
            if (IPAddress.IsLoopback(address)) return true;
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                return bytes[0] == 10 || bytes[0] == 127 ||
                       bytes[0] == 169 && bytes[1] == 254 ||
                       bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                       bytes[0] == 192 && bytes[1] == 168;
            }
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.Equals(IPAddress.IPv6Loopback);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(IsPrivateAddress)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(IsPrivateAddress)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs throw if disposed as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ThrowIfDisposed()
    {
    try
    {
            if (Volatile.Read(ref disposeState) != 0)
                throw new ObjectDisposedException(nameof(RemoteKnowledgeImportService));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ThrowIfDisposed)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ThrowIfDisposed)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="RemoteKnowledgeImportService"/> and leaves the remote knowledge import workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Interlocked.Exchange(ref disposeState, 1) != 0)
                return;
            http.Dispose();
            logger.LogDebug("Disposed the remote-knowledge HTTP client.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Disposing the remote-knowledge import service failed.");
            throw;
        }
    }

}
