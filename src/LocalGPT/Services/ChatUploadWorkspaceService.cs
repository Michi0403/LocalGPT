using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates chat upload workspace behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
    /// <param name="councilText">Council text service dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
    /// <param name="catalog">Local gpt catalog service dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
    public sealed class ChatUploadWorkspaceService(
        ILogger<ChatUploadWorkspaceService> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        LocalGptCatalogService catalog) : IChatUploadWorkspaceService
    {
        /// <summary>
        /// Gets the workspace root value that forms part of the chat upload workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The workspace root value exposed by <see cref="ChatUploadWorkspaceService"/>.</value>
        public string WorkspaceRoot { get; } = LocalGptApplicationDataPaths.ResolveUserPath("ChatUploadWorkspaces");

        /// <summary>
        /// Creates workspace as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="files">Chat upload workspace input file dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The chat upload workspace result produced by the operation.</returns>
        public async Task<ChatUploadWorkspaceResult> CreateWorkspaceAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceInputFile> files,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var fileList = files
                    .Where(file => !string.IsNullOrWhiteSpace(file.Name))
                    .Take(catalog.MaxFiles)
                    .ToList();
                var workspaceName = councilRuntime.BuildWorkspaceName(prompt, fileList, logger);
                var root = Path.Combine(WorkspaceRoot, workspaceName);
                var originalRoot = Path.Combine(root, "original");
                var extractedRoot = Path.Combine(root, "extracted");
                Directory.CreateDirectory(originalRoot);
                Directory.CreateDirectory(extractedRoot);

                var warnings = new List<string>();
                var analyzedFiles = new List<AnalyzedUploadFile>();
                var readOnlyArchiveExtractionReady = false;
                long totalUploadedBytes = 0;

                foreach (var input in fileList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (input.SizeBytes > catalog.MaxSingleFileBytes)
                    {
                        warnings.Add($"{input.Name} skipped: file is larger than {catalog.MaxSingleFileBytes:n0} bytes.");
                        continue;
                    }

                    totalUploadedBytes += input.SizeBytes;
                    if (totalUploadedBytes > catalog.MaxTotalFileBytes)
                    {
                        warnings.Add("Remaining files skipped: upload batch exceeded the LocalGPT prompt-workspace byte cap.");
                        break;
                    }

                    var safeName = councilText.BuildUniqueFileName(originalRoot, input.Name, logger);
                    var originalPath = Path.Combine(originalRoot, safeName);
                    var bytes = input.Data.ToArray();
                    await System.IO.File.WriteAllBytesAsync(originalPath, bytes, cancellationToken).ConfigureAwait(false);

                    var originalRelativePath = councilText.ToForwardSlash(Path.GetRelativePath(root, originalPath), logger);
                    if (councilRuntime.IsZip(input.Name, logger))
                    {
                        var buildSummary = councilRuntime.BuildBinarySummary(
                            originalRelativePath,
                            bytes.Length,
                            "zip",
                            false,
                            "Original zip remains quarantined. Safe read-only extraction starts immediately so uploaded source can be inspected without waiting for promotion; execution and promotion remain gated.",
                            logger);
                        ArgumentNullException.ThrowIfNull(buildSummary);
                        analyzedFiles.Add(buildSummary);
                        readOnlyArchiveExtractionReady |= await ExtractZipAsync(
                            root,
                            extractedRoot,
                            safeName,
                            originalPath,
                            analyzedFiles,
                            warnings,
                            cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var analyzedFilesToAdd = councilRuntime.AnalyzeBytes(originalRelativePath, bytes, logger);
                        ArgumentNullException.ThrowIfNull(analyzedFilesToAdd);
                        analyzedFiles.Add(analyzedFilesToAdd);
                    }
                }

                if (!fileList.Any())
                    warnings.Add("No files were supplied for this prompt workspace.");

                var context = councilRuntime.BuildContextMarkdown(workspaceName, root, prompt, analyzedFiles, warnings, logger);
                var contextPath = Path.Combine(root, "context.md");
                await System.IO.File.WriteAllTextAsync(contextPath, context, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

                var manifestPath = Path.Combine(root, "manifest.json");
                await System.IO.File.WriteAllTextAsync(
                    manifestPath,
                    JsonSerializer.Serialize(new
                    {
                        WorkspaceName = workspaceName,
                        RootPath = root,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        Prompt = prompt,
                        Limits = new
                        {
                            catalog.MaxFiles,
                            catalog.MaxSingleFileBytes,
                            catalog.MaxTotalFileBytes,
                            catalog.MaxZipEntries,
                            catalog.MaxZipEntryBytes,
                            catalog.MaxExtractedBytes,
                            catalog.MaxContextCharacters,
                            catalog.MaxExcerptCharactersPerFile
                        },
                        GateStatus = readOnlyArchiveExtractionReady ? "QuarantinedReadOnlyExtractionReady" : "Quarantined",
                        ReadOnlyArchiveExtractionReady = readOnlyArchiveExtractionReady,
                        Warnings = warnings,
                        Files = analyzedFiles.Select(file => file.Summary)
                    }, catalog.JsonOptions),
                    Encoding.UTF8,
                    cancellationToken).ConfigureAwait(false);

                var filesSummary = analyzedFiles
                    .Select(file => file.Summary)
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                logger.LogInformation(
                    "Created chat upload workspace {WorkspaceName} with {FileCount} analyzed files.",
                    workspaceName,
                    filesSummary.Count);

                return new ChatUploadWorkspaceResult(
                    workspaceName,
                    root,
                    manifestPath,
                    contextPath,
                    DateTimeOffset.UtcNow,
                    filesSummary,
                    warnings,
                    context);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the chat upload workspace.");
                throw;
            }

        }

        /// <summary>Creates a quarantine workspace by streaming selected files directly to disk before analysis.</summary>
        /// <param name="prompt">Optional user goal or upload context.</param>
        /// <param name="files">Stream-backed input files supplied by an interactive upload surface.</param>
        /// <param name="cancellationToken">Cancellation token that stops the quarantine copy.</param>
        /// <returns>The created quarantine workspace.</returns>
        public async Task<ChatUploadWorkspaceResult> CreateWorkspaceFromStreamsAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceStreamInput> files,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);
                var fileList = files
                    .Where(file => !string.IsNullOrWhiteSpace(file.Name) && file.OpenReadStream is not null)
                    .Take(catalog.MaxFiles)
                    .ToList();
                var nameInputs = fileList.Select(file => new ChatUploadWorkspaceInputFile(file.Name, file.ContentType, file.SizeBytes, ReadOnlyMemory<byte>.Empty)).ToList();
                var workspaceName = councilRuntime.BuildWorkspaceName(prompt, nameInputs, logger);
                var root = Path.Combine(WorkspaceRoot, workspaceName);
                var originalRoot = Path.Combine(root, "original");
                var extractedRoot = Path.Combine(root, "extracted");
                Directory.CreateDirectory(originalRoot);
                Directory.CreateDirectory(extractedRoot);

                var warnings = new List<string>();
                var analyzedFiles = new List<AnalyzedUploadFile>();
                var readOnlyArchiveExtractionReady = false;
                long totalUploadedBytes = 0;
                foreach (var input in fileList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (input.SizeBytes < 0 || input.SizeBytes > catalog.MaxSingleFileBytes)
                    {
                        warnings.Add($"{input.Name} skipped: file is larger than {catalog.MaxSingleFileBytes:n0} bytes.");
                        continue;
                    }
                    if (totalUploadedBytes + input.SizeBytes > catalog.MaxTotalFileBytes)
                    {
                        warnings.Add("Remaining files skipped: upload batch exceeded the LocalGPT prompt-workspace byte cap.");
                        break;
                    }

                    var safeName = councilText.BuildUniqueFileName(originalRoot, input.Name, logger);
                    var originalPath = Path.Combine(originalRoot, safeName);
                    long copiedBytes;
                    using (var source = input.OpenReadStream())
                    {
                        var destination = new FileStream(originalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        await using var configuredDestination = destination.ConfigureAwait(false);
                        copiedBytes = await CopyBoundedStreamAsync(source, destination, catalog.MaxSingleFileBytes, cancellationToken).ConfigureAwait(false);
                    }
                    totalUploadedBytes += copiedBytes;
                    if (totalUploadedBytes > catalog.MaxTotalFileBytes)
                    {
                        File.Delete(originalPath);
                        warnings.Add("Remaining files skipped: upload batch exceeded the LocalGPT prompt-workspace byte cap.");
                        break;
                    }

                    var originalRelativePath = councilText.ToForwardSlash(Path.GetRelativePath(root, originalPath), logger);
                    if (councilRuntime.IsZip(input.Name, logger))
                    {
                        var summary = councilRuntime.BuildBinarySummary(
                            originalRelativePath,
                            copiedBytes,
                            "zip",
                            false,
                            "Original zip remains quarantined. Safe read-only extraction starts immediately so uploaded source can be inspected without waiting for promotion; execution and promotion remain gated.",
                            logger);
                        ArgumentNullException.ThrowIfNull(summary);
                        analyzedFiles.Add(summary);
                        readOnlyArchiveExtractionReady |= await ExtractZipAsync(
                            root,
                            extractedRoot,
                            safeName,
                            originalPath,
                            analyzedFiles,
                            warnings,
                            cancellationToken).ConfigureAwait(false);
                    }
                    else if (copiedBytes <= Math.Min(catalog.MaxSingleFileBytes, 8L * 1024 * 1024))
                    {
                        var bytes = await File.ReadAllBytesAsync(originalPath, cancellationToken).ConfigureAwait(false);
                        var analyzed = councilRuntime.AnalyzeBytes(originalRelativePath, bytes, logger);
                        ArgumentNullException.ThrowIfNull(analyzed);
                        analyzedFiles.Add(analyzed);
                    }
                    else
                    {
                        var summary = councilRuntime.BuildBinarySummary(
                            originalRelativePath,
                            copiedBytes,
                            councilRuntime.DetermineFileKind(originalPath, logger),
                            false,
                            "Large streamed upload is retained in quarantine; bounded inspection and recommendation use metadata, reviewed regex rules and approved knowledge before any promotion.",
                            logger);
                        ArgumentNullException.ThrowIfNull(summary);
                        analyzedFiles.Add(summary);
                    }
                }

                if (fileList.Count == 0)
                    warnings.Add("No files were supplied for this prompt workspace.");

                var context = councilRuntime.BuildContextMarkdown(workspaceName, root, prompt, analyzedFiles, warnings, logger);
                var contextPath = Path.Combine(root, "context.md");
                await File.WriteAllTextAsync(contextPath, context, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                var manifestPath = Path.Combine(root, "manifest.json");
                await File.WriteAllTextAsync(
                    manifestPath,
                    JsonSerializer.Serialize(new
                    {
                        WorkspaceName = workspaceName,
                        RootPath = root,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        Prompt = prompt,
                        Limits = new
                        {
                            catalog.MaxFiles,
                            catalog.MaxSingleFileBytes,
                            catalog.MaxTotalFileBytes,
                            catalog.MaxZipEntries,
                            catalog.MaxZipEntryBytes,
                            catalog.MaxExtractedBytes,
                            catalog.MaxContextCharacters,
                            catalog.MaxExcerptCharactersPerFile
                        },
                        GateStatus = readOnlyArchiveExtractionReady ? "QuarantinedReadOnlyExtractionReady" : "Quarantined",
                        ReadOnlyArchiveExtractionReady = readOnlyArchiveExtractionReady,
                        Warnings = warnings,
                        Files = analyzedFiles.Select(file => file.Summary)
                    }, catalog.JsonOptions),
                    Encoding.UTF8,
                    cancellationToken).ConfigureAwait(false);

                var summaries = analyzedFiles.Select(file => file.Summary)
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                logger.LogInformation("Created streamed chat upload workspace {WorkspaceName} with {FileCount} analyzed files.", workspaceName, summaries.Count);
                return new ChatUploadWorkspaceResult(workspaceName, root, manifestPath, contextPath, DateTimeOffset.UtcNow, summaries, warnings, context);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the streamed chat upload workspace.");
                throw;
            }
        }

        /// <summary>Copies a stream while enforcing the configured quarantine byte cap.</summary>
        private async Task<long> CopyBoundedStreamAsync(Stream source, Stream destination, long maximumBytes, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[128 * 1024];
                long total = 0;
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;
                    total += read;
                    if (total > maximumBytes)
                        throw new InvalidDataException($"Upload exceeded the configured {maximumBytes:n0}-byte single-file cap while streaming.");
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
                return total;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bounded streamed upload copy failed; file content was omitted from logs.");
                throw;
            }
        }

        /// <summary>
        /// Lists workspaces as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="take">Take value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IReadOnlyList<ChatUploadWorkspaceSummary> ListWorkspaces(int take = 20)
        {
            try
            {
                if (!Directory.Exists(WorkspaceRoot))
                    return [];

                return Directory
                    .EnumerateDirectories(WorkspaceRoot)
                    .Select(BuildWorkspaceSummary)
                    .Where(summary => summary is not null)
                    .Cast<ChatUploadWorkspaceSummary>()
                    .OrderByDescending(summary => summary.LastWriteTimeUtc)
                    .Take(take > 0 ? Math.Min(take, Math.Max(1, catalog.MaxFiles)) : Math.Max(1, catalog.MaxFiles))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListWorkspaces take:{take.ToString()}");
                return new List<ChatUploadWorkspaceSummary>();
            }
        }

        /// <summary>
        /// Retrieves latest workspace as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="maxAge">Max age value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The chat upload workspace summary produced by the operation.</returns>
        public ChatUploadWorkspaceSummary? GetLatestWorkspace(TimeSpan? maxAge = null)
        {
            try
            {
                var latest = ListWorkspaces(1).FirstOrDefault();
                if (latest is null || maxAge is null)
                    return latest;

                var age = DateTimeOffset.UtcNow - latest.CreatedAtUtc;
                return age <= maxAge.Value ? latest : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetLatestWorkspace maxAge:{maxAge?.ToString()}");
                return null;
            }

        }

        /// <summary>
        /// Retrieves latest context markdown as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="maxCharacters">Max characters value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="maxAge">Max age value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GetLatestContextMarkdown(int maxCharacters, TimeSpan? maxAge = null)
        {
            try
            {
                try
                {
                    var latest = GetLatestWorkspace(maxAge);
                    if (latest is null)
                        return string.Empty;
                    if (!System.IO.File.Exists(latest.ContextPath))
                        return string.Empty;

                    return councilText.TrimForPrompt(System.IO.File.ReadAllText(latest.ContextPath), maxCharacters, logger);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not read latest chat upload workspace context.");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetLatestContextMarkdown maxCharacters:{maxCharacters.ToString()} maxAge:{maxAge?.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Reads context markdown as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="maxCharacters">Max characters value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        public async Task<string> ReadContextMarkdownAsync(
            string workspaceName,
            int maxCharacters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = ResolveWorkspacePath(workspaceName);
                if (workspace is null)
                    return string.Empty;

                var contextPath = Path.Combine(workspace, "context.md");
                if (!System.IO.File.Exists(contextPath))
                    return string.Empty;

                var context = await System.IO.File.ReadAllTextAsync(contextPath, cancellationToken).ConfigureAwait(false);
                return councilText.TrimForPrompt(context, maxCharacters, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadContextMarkdownAsync workspaceName:{workspaceName.ToString()} maxCharacters:{maxCharacters.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Lists files as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IReadOnlyList<ChatUploadWorkspaceFileSummary> ListFiles(string workspaceName, int take = 250)
        {
            try
            {
                var workspace = ResolveWorkspacePath(workspaceName);
                if (workspace is null)
                    return [];

                return Directory
                    .EnumerateFiles(workspace, "*", SearchOption.AllDirectories)
                    .Select(path =>
                    {
                        var info = new FileInfo(path);
                        return new ChatUploadWorkspaceFileSummary(
                            councilText.ToForwardSlash(Path.GetRelativePath(workspace, path), logger),
                            councilRuntime.DetermineFileKind(path, logger),
                            info.Length,
                            info.LastWriteTimeUtc,
                            councilRuntime.IsTextLike(path, logger) || catalog.BinaryDiagnosticExtensions.Contains(Path.GetExtension(path)),
                            path.EndsWith("context.md", StringComparison.OrdinalIgnoreCase)
                                ? "AI prompt context generated by LocalGPT."
                                : councilText.ToForwardSlash(Path.GetRelativePath(workspace, path), logger).StartsWith("extracted/", StringComparison.OrdinalIgnoreCase)
                                    ? "Safely extracted source-backed read-only evidence; execution and promotion remain separately gated."
                                    : "Original user-uploaded quarantine evidence.");
                    })
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .Take(take > 0 ? Math.Clamp(take, 1, 20_000) : 5_000)
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListFiles workspaceName:{workspaceName.ToString()} take:{take.ToString()}");
                return new List<ChatUploadWorkspaceFileSummary>();
            }
        }

        /// <summary>
        /// Reads file as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="relativePath">Relative path value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="maxCharacters">Maximum decoded characters returned by one progressive read segment.</param>
        /// <param name="characterOffset">Decoded character offset at which this progressive read starts.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The chat upload workspace file read result produced by the operation, including continuation metadata when more content remains.</returns>
        public async Task<ChatUploadWorkspaceFileReadResult?> ReadFileAsync(
            string workspaceName,
            string relativePath,
            int maxCharacters,
            long characterOffset = 0,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = ResolveWorkspacePath(workspaceName);
                if (workspace is null)
                    return null;

                var file = councilRuntime.ResolveWorkspaceFile(workspace, relativePath, logger);
                if (file is null || !System.IO.File.Exists(file))
                    return null;

                var info = new FileInfo(file);
                var normalizedRelativePath = councilText.ToForwardSlash(Path.GetRelativePath(workspace, file), logger);
                if (info.Length > catalog.MaxSingleFileBytes)
                {
                    return new ChatUploadWorkspaceFileReadResult(
                        workspaceName,
                        normalizedRelativePath,
                        file,
                        "too-large",
                        info.Length,
                        "File exceeds the configured workspace read limit.")
                    {
                        CharacterOffset = Math.Max(0, characterOffset)
                    };
                }

                var effectiveOffset = Math.Max(0, characterOffset);
                var effectiveMaximum = Math.Max(1, maxCharacters);
                if (councilRuntime.IsTextLike(file, logger))
                {
                    var segment = await ReadTextSegmentAsync(file, effectiveOffset, effectiveMaximum, cancellationToken).ConfigureAwait(false);
                    return new ChatUploadWorkspaceFileReadResult(
                        workspaceName,
                        normalizedRelativePath,
                        file,
                        "text",
                        info.Length,
                        councilRuntime.SanitizeForPrompt(segment.Content, logger))
                    {
                        CharacterOffset = effectiveOffset,
                        CharactersReturned = segment.CharactersReturned,
                        HasMore = segment.HasMore,
                        NextOffsetCharacters = segment.HasMore
                            ? effectiveOffset + segment.CharactersReturned
                            : null
                    };
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                var analyzed = councilRuntime.AnalyzeBytes(normalizedRelativePath, bytes, logger);
                ArgumentNullException.ThrowIfNull(analyzed);
                var analyzedOffset = (int)Math.Min(effectiveOffset, analyzed.Excerpt.Length);
                var available = analyzed.Excerpt.Length - analyzedOffset;
                var charactersReturned = Math.Min(effectiveMaximum, Math.Max(0, available));
                var content = charactersReturned > 0
                    ? analyzed.Excerpt.Substring(analyzedOffset, charactersReturned)
                    : string.Empty;
                var hasMore = analyzedOffset + charactersReturned < analyzed.Excerpt.Length;
                return new ChatUploadWorkspaceFileReadResult(
                    workspaceName,
                    analyzed.Summary.RelativePath,
                    file,
                    analyzed.Summary.Kind,
                    analyzed.Summary.Length,
                    content)
                {
                    CharacterOffset = effectiveOffset,
                    CharactersReturned = charactersReturned,
                    HasMore = hasMore,
                    NextOffsetCharacters = hasMore
                        ? effectiveOffset + charactersReturned
                        : null
                };
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error in ReadFileAsync workspaceName: {WorkspaceName} relativePath: {RelativePath} maxCharacters: {MaxCharacters} characterOffset: {CharacterOffset}.",
                    workspaceName,
                    relativePath,
                    maxCharacters,
                    characterOffset);
                return null;
            }
        }

        /// <summary>Reads one progressive character segment without loading a large text upload completely into managed memory.</summary>
        /// <param name="filePath">Absolute workspace file path that has already passed workspace-root validation.</param>
        /// <param name="characterOffset">Decoded character offset at which reading starts.</param>
        /// <param name="maxCharacters">Maximum decoded characters returned in this segment.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The progressive segment content, returned character count, and whether unread content remains.</returns>
        private async Task<(string Content, int CharactersReturned, bool HasMore)> ReadTextSegmentAsync(
            string filePath,
            long characterOffset,
            int maxCharacters,
            CancellationToken cancellationToken)
        {
            try
            {
                var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var configuredStream = stream.ConfigureAwait(false);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 64 * 1024, leaveOpen: true);

                var buffer = new char[64 * 1024];
                var charactersToSkip = Math.Max(0, characterOffset);
                while (charactersToSkip > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var requested = (int)Math.Min(buffer.Length, charactersToSkip);
                    var read = await reader.ReadAsync(buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        return (string.Empty, 0, false);
                    charactersToSkip -= read;
                }

                var targetCharacters = Math.Clamp(maxCharacters, 1, 1_000_000);
                var builder = new StringBuilder(Math.Min(targetCharacters + 1, 1_000_001));
                while (builder.Length <= targetCharacters)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var remaining = targetCharacters + 1 - builder.Length;
                    var requested = Math.Min(buffer.Length, remaining);
                    var read = await reader.ReadAsync(buffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;
                    builder.Append(buffer, 0, read);
                }

                var hasMore = builder.Length > targetCharacters;
                var returnedCharacters = Math.Min(builder.Length, targetCharacters);
                return (
                    returnedCharacters == 0 ? string.Empty : builder.ToString(0, returnedCharacters),
                    returnedCharacters,
                    hasMore);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reading a progressive upload-workspace text segment failed; file path was omitted.");
                throw;
            }
        }

        /// <summary>
        /// Resolves workspace path as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? ResolveWorkspacePath(string workspaceName)
        {
            try
            {
                var safeName = Path.GetFileName(workspaceName);
                if (string.IsNullOrWhiteSpace(safeName) ||
                    !string.Equals(workspaceName, safeName, StringComparison.Ordinal))
                {
                    return null;
                }

                var root = Path.GetFullPath(WorkspaceRoot);
                var candidate = Path.GetFullPath(Path.Combine(root, safeName));
                if (!councilRuntime.IsInsideRoot(root, candidate, logger) || !Directory.Exists(candidate))
                    return null;

                return candidate;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ResolveWorkspacePath workspaceName: {workspaceName.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// Builds workspace summary as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="path">Path value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The chat upload workspace summary produced by the operation.</returns>
        private ChatUploadWorkspaceSummary? BuildWorkspaceSummary(string path)
        {
            try
            {
                var directory = new DirectoryInfo(path);
                var contextPath = Path.Combine(path, "context.md");
                var totalBytes = Directory
                    .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
                var fileCount = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count();
                var createdAtUtc = directory.CreationTimeUtc == DateTime.MinValue
                    ? directory.LastWriteTimeUtc
                    : directory.CreationTimeUtc;

                return new ChatUploadWorkspaceSummary(
                    directory.Name,
                    directory.FullName,
                    new DateTimeOffset(createdAtUtc, TimeSpan.Zero),
                    directory.LastWriteTimeUtc,
                    fileCount,
                    totalBytes,
                    contextPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not summarize chat upload workspace {path}.");
                return null;
            }
        }
        /// <summary>
        /// Performs extract ZIP as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceRoot">Workspace root value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="extractedRoot">Extracted root value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="zipFileName">Zip file name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="zipPath">Quarantined archive path that is opened read-only for bounded safe extraction.</param>
        /// <param name="analyzedFiles">Analyzed files value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="warnings">Warnings value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns><see langword="true"/> when the archive opened and its bounded safe extraction completed; otherwise <see langword="false"/>.</returns>
        private async Task<bool> ExtractZipAsync(
            string workspaceRoot,
            string extractedRoot,
            string zipFileName,
            string zipPath,
            List<AnalyzedUploadFile> analyzedFiles,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                var zipName = Path.GetFileNameWithoutExtension(zipFileName);
                var zipExtractRoot = Path.Combine(extractedRoot, councilText.SanitizeFileName(zipName, logger));
                Directory.CreateDirectory(zipExtractRoot);

                var entryCount = 0;
                long extractedBytes = 0;

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(entry.Name))
                        continue;

                    entryCount++;
                    if (entryCount > catalog.MaxZipEntries)
                    {
                        warnings.Add($"{zipFileName}: remaining entries skipped after {catalog.MaxZipEntries:n0} entries.");
                        break;
                    }

                    if (entry.Length > catalog.MaxZipEntryBytes)
                    {
                        warnings.Add($"{zipFileName}: {entry.FullName} skipped because the entry is too large.");
                        continue;
                    }

                    extractedBytes += entry.Length;
                    if (extractedBytes > catalog.MaxExtractedBytes)
                    {
                        warnings.Add($"{zipFileName}: remaining entries skipped after extracted byte cap.");
                        break;
                    }

                    var safeRelativePath = councilText.BuildSafeZipRelativePath(entry.FullName, logger);
                    if (safeRelativePath is null)
                    {
                        warnings.Add($"{zipFileName}: unsafe zip path skipped: {entry.FullName}");
                        continue;
                    }

                    var destination = Path.GetFullPath(Path.Combine(zipExtractRoot, safeRelativePath));
                    if (!councilRuntime.IsInsideRoot(zipExtractRoot, destination, logger))
                    {
                        warnings.Add($"{zipFileName}: path traversal entry skipped: {entry.FullName}");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    if (System.IO.File.Exists(destination))
                    {
                        warnings.Add($"{zipFileName}: duplicate archive destination skipped: {entry.FullName}");
                        continue;
                    }

                    {
                        var entryStream = entry.Open();
                        await using var configuredEntryStreamAsyncDisposal = entryStream.ConfigureAwait(false);
                        var destinationStream = new FileStream(
                            destination,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            64 * 1024,
                            FileOptions.Asynchronous | FileOptions.SequentialScan);
                        await using var configuredDestinationStreamAsyncDisposal = destinationStream.ConfigureAwait(false);
                        await entryStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                    }

                    var relativePath = councilText.ToForwardSlash(Path.GetRelativePath(workspaceRoot, destination), logger);
                    if (entry.Length <= Math.Min(catalog.MaxSingleFileBytes, 8L * 1024 * 1024))
                    {
                        var bytes = await System.IO.File.ReadAllBytesAsync(destination, cancellationToken).ConfigureAwait(false);
                        var analyzedBytes = councilRuntime.AnalyzeBytes(relativePath, bytes, logger);
                        ArgumentNullException.ThrowIfNull(analyzedBytes);
                        analyzedFiles.Add(analyzedBytes with
                        {
                            Summary = analyzedBytes.Summary with
                            {
                                Note = "Safely extracted source-backed read-only evidence; execution and promotion remain separately gated."
                            }
                        });
                    }
                    else
                    {
                        var summary = councilRuntime.BuildBinarySummary(
                            relativePath,
                            entry.Length,
                            councilRuntime.DetermineFileKind(destination, logger),
                            false,
                            "Large safely extracted entry is available for progressive direct reading but is not duplicated into generated prompt context.",
                            logger);
                        ArgumentNullException.ThrowIfNull(summary);
                        analyzedFiles.Add(summary);
                    }
                }

                logger.LogInformation(
                    "Safely extracted {EntryCount} read-only archive entr{EntrySuffix} from {ZipFileName}; extracted bytes {ExtractedBytes}. Promotion remains separately gated.",
                    entryCount,
                    entryCount == 1 ? "y" : "ies",
                    zipFileName,
                    extractedBytes);
                return true;
            }
            catch (InvalidDataException ex)
            {
                warnings.Add($"{zipFileName}: zip could not be opened: {ex.Message}");
                logger.LogWarning(ex, "Safe read-only extraction rejected invalid archive {ZipFileName}.", zipFileName);
                return false;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error in ExtractZipAsync for {ZipFileName}; workspace paths and archive content were omitted.",
                    zipFileName);
                warnings.Add($"{zipFileName}: safe read-only extraction failed; original quarantine file remains available.");
                return false;
            }
        }

    }
}
