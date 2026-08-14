using DevExpress.CodeParser;
using DevExpress.DataAccess.Native.Sql.MasterDetail;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.IO.Compression;
using System.Security.AccessControl;
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
        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "ChatUploadWorkspaces");

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
                long totalUploadedBytes = 0;

                foreach (var input in fileList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //if (input.SizeBytes > MaxSingleFileBytes)
                    //{
                    //    warnings.Add($"{input.Name} skipped: file is larger than {MaxSingleFileBytes:n0} bytes.");
                    //    continue;
                    //}

                    totalUploadedBytes += input.SizeBytes;
                    //if (totalUploadedBytes > MaxTotalFileBytes)
                    //{
                    //    warnings.Add("Remaining files skipped: upload batch exceeded the LocalGPT prompt-workspace byte cap.");
                    //    break;
                    //}

                    var safeName = councilText.BuildUniqueFileName(originalRoot, input.Name, logger);
                    var originalPath = Path.Combine(originalRoot, safeName);
                    var bytes = input.Data.ToArray();
                    await System.IO.File.WriteAllBytesAsync(originalPath, bytes, cancellationToken).ConfigureAwait(false);

                    var originalRelativePath = councilText.ToForwardSlash(Path.GetRelativePath(root, originalPath), logger);
                    if (councilRuntime.IsZip(input.Name, logger))
                    {
                        var buildSummary = councilRuntime.BuildBinarySummary(originalRelativePath, bytes.Length, "zip", false,
                            "Original zip saved. Extracted safe entries are listed separately.", logger);
                        ArgumentNullException.ThrowIfNull(buildSummary);
                        analyzedFiles.Add(buildSummary);
                        await ExtractZipAsync(root, extractedRoot, safeName, bytes, analyzedFiles, warnings, cancellationToken).ConfigureAwait(false);
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
                                : "Uploaded or extracted workspace file.");
                    })
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .Take(take > 0 ? Math.Min(take, Math.Max(1, catalog.MaxFiles)) : Math.Max(1, catalog.MaxFiles))
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
        /// <param name="maxCharacters">Max characters value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The chat upload workspace file read result produced by the operation.</returns>
        public async Task<ChatUploadWorkspaceFileReadResult?> ReadFileAsync(
            string workspaceName,
            string relativePath,
            int maxCharacters,
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
                if (info.Length > catalog.MaxSingleFileBytes)
                    return new ChatUploadWorkspaceFileReadResult(
                        workspaceName,
                       councilText.ToForwardSlash(Path.GetRelativePath(workspace, file), logger),
                        file,
                        "too-large",
                        info.Length,
                        "File is too large for inline reading. Use the file summary and inspect it manually.");

                var bytes = await System.IO.File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                var analyzed = councilRuntime.AnalyzeBytes(councilText.ToForwardSlash(Path.GetRelativePath(workspace, file), logger), bytes, logger);
                ArgumentNullException.ThrowIfNull(analyzed);
                return new ChatUploadWorkspaceFileReadResult(
                    workspaceName,
                    analyzed.Summary.RelativePath,
                    file,
                    analyzed.Summary.Kind,
                    analyzed.Summary.Length,
                    councilText.TrimForPrompt(analyzed.Excerpt, maxCharacters, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadFileAsync workspaceName: {workspaceName.ToString()} relativePath: {relativePath.ToString()} maxCharacters {maxCharacters.ToString()} ");
                return null;
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
        /// <param name="zipBytes">Zip bytes value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="analyzedFiles">Analyzed files value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="warnings">Warnings value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task ExtractZipAsync(
            string workspaceRoot,
            string extractedRoot,
            string zipFileName,
            byte[] zipBytes,
            List<AnalyzedUploadFile> analyzedFiles,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            try
            {
                using var memory = new MemoryStream(zipBytes);
                using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
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
                    {
                        var entryStream = entry.Open();
                        await using var configuredEntryStreamAsyncDisposal = entryStream.ConfigureAwait(false);
                        var destinationStream = System.IO.File.Create(destination);
                        await using var configuredDestinationStreamAsyncDisposal = destinationStream.ConfigureAwait(false);
                        await entryStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                    }

                    var bytes = await System.IO.File.ReadAllBytesAsync(destination, cancellationToken).ConfigureAwait(false);
                    var analyzedBytes = councilRuntime.AnalyzeBytes(councilText.ToForwardSlash(Path.GetRelativePath(workspaceRoot, destination), logger), bytes, logger);
                    ArgumentNullException.ThrowIfNull(analyzedBytes);
                    analyzedFiles.Add(analyzedBytes);
                }
            }
            catch (InvalidDataException ex)
            {
                warnings.Add($"{zipFileName}: zip could not be opened: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractZipAsync workspaceRoot: {workspaceRoot.ToString()} extractedRoot: {extractedRoot.ToString()} zipFileName: {zipFileName.ToString()} zipBytes: {zipBytes.ToString()} analyzedFiles: {analyzedFiles.ToString()} warnings: {warnings.ToString()}");
            }
        }
    }
}
