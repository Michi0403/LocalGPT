using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates remote knowledge import behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class RemoteKnowledgeImportService
    {
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
            var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredSourceAsyncDisposal = source.ConfigureAwait(false);
            var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await using var configuredDestinationAsyncDisposal = destination.ConfigureAwait(false);
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

    }
}
