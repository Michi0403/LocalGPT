using DevExpress.CodeParser;
using DevExpress.DataAccess.Native.Sql.MasterDetail;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;
using static System.Net.WebRequestMethods;

namespace LocalGPT.Services
{
    public sealed class ChatUploadWorkspaceService(
        ILogger<ChatUploadWorkspaceService> logger) : IChatUploadWorkspaceService
    {
        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "ChatUploadWorkspaces");

        public async Task<ChatUploadWorkspaceResult?> CreateWorkspaceAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceInputFile> files,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var fileList = files
                    .Where(file => !string.IsNullOrWhiteSpace(file.Name))
                    .Take(MaxFiles)
                    .ToList();
                var workspaceName = CouncilChatStaticsGeneral.BuildWorkspaceName(prompt, fileList, logger);
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

                    var safeName = CouncilChatStringFunctions.BuildUniqueFileName(originalRoot, input.Name, logger);
                    var originalPath = Path.Combine(originalRoot, safeName);
                    var bytes = input.Data.ToArray();
                    await System.IO.File.WriteAllBytesAsync(originalPath, bytes, cancellationToken).ConfigureAwait(false);

                    var originalRelativePath = CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(root, originalPath), logger);
                    if (CouncilChatStaticsGeneral.IsZip(input.Name, logger))
                    {
                        var buildSummary = CouncilChatStaticsGeneral.BuildBinarySummary(originalRelativePath, bytes.Length, "zip", false,
                            "Original zip saved. Extracted safe entries are listed separately.", logger);
                        ArgumentNullException.ThrowIfNull(buildSummary);
                        analyzedFiles.Add(buildSummary);
                        await ExtractZipAsync(root, extractedRoot, safeName, bytes, analyzedFiles, warnings, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var analyzedFilesToAdd = CouncilChatStaticsGeneral.AnalyzeBytes(originalRelativePath, bytes, logger);
                        ArgumentNullException.ThrowIfNull(analyzedFilesToAdd);
                        analyzedFiles.Add(analyzedFilesToAdd);
                    }
                }

                if (!fileList.Any())
                    warnings.Add("No files were supplied for this prompt workspace.");

                var context = CouncilChatStaticsGeneral.BuildContextMarkdown(workspaceName, root, prompt, analyzedFiles, warnings, logger);
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
                            MaxFiles,
                            MaxSingleFileBytes,
                            MaxTotalFileBytes,
                            MaxZipEntries,
                            MaxZipEntryBytes,
                            MaxExtractedBytes,
                            MaxContextCharacters,
                            MaxExcerptCharactersPerFile
                        },
                        Warnings = warnings,
                        Files = analyzedFiles.Select(file => file.Summary)
                    }, GlobalVariableSlopCollectionToRemove.JsonOptions),
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateWorkspaceAsync prompt:{prompt.ToString()} files:{files.ToString()}");
                return null;
            }

        }

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
                    .Take(Math.Clamp(take, 1, 100))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListWorkspaces take:{take.ToString()}");
                return new List<ChatUploadWorkspaceSummary>();
            }
        }

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

                    return CouncilChatStringFunctions.TrimForPrompt(System.IO.File.ReadAllText(latest.ContextPath), maxCharacters, logger);
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
                return CouncilChatStringFunctions.TrimForPrompt(context, maxCharacters, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadContextMarkdownAsync workspaceName:{workspaceName.ToString()} maxCharacters:{maxCharacters.ToString()}");
                return string.Empty;
            }
        }

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
                            CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspace, path), logger),
                            CouncilChatStaticsGeneral.DetermineFileKind(path, logger),
                            info.Length,
                            info.LastWriteTimeUtc,
                            CouncilChatStaticsGeneral.IsTextLike(path, logger) || BinaryDiagnosticExtensions.Contains(Path.GetExtension(path)),
                            path.EndsWith("context.md", StringComparison.OrdinalIgnoreCase)
                                ? "AI prompt context generated by LocalGPT."
                                : "Uploaded or extracted workspace file.");
                    })
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .Take(Math.Clamp(take, 1, 1000))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListFiles workspaceName:{workspaceName.ToString()} take:{take.ToString()}");
                return new List<ChatUploadWorkspaceFileSummary>();
            }
        }

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

                var file = CouncilChatStaticsGeneral.ResolveWorkspaceFile(workspace, relativePath, logger);
                if (file is null || !System.IO.File.Exists(file))
                    return null;

                var info = new FileInfo(file);
                if (info.Length > MaxSingleFileBytes)
                    return new ChatUploadWorkspaceFileReadResult(
                        workspaceName,
                       CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspace, file), logger),
                        file,
                        "too-large",
                        info.Length,
                        "File is too large for inline reading. Use the file summary and inspect it manually.");

                var bytes = await System.IO.File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                var analyzed = CouncilChatStaticsGeneral.AnalyzeBytes(CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspace, file), logger), bytes, logger);
                ArgumentNullException.ThrowIfNull(analyzed);
                return new ChatUploadWorkspaceFileReadResult(
                    workspaceName,
                    analyzed.Summary.RelativePath,
                    file,
                    analyzed.Summary.Kind,
                    analyzed.Summary.Length,
                    CouncilChatStringFunctions.TrimForPrompt(analyzed.Excerpt, maxCharacters, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadFileAsync workspaceName: {workspaceName.ToString()} relativePath: {relativePath.ToString()} maxCharacters {maxCharacters.ToString()} ");
                return null;
            }
        }

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
                if (!CouncilChatStaticsGeneral.IsInsideRoot(root, candidate, logger) || !Directory.Exists(candidate))
                    return null;

                return candidate;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ResolveWorkspacePath workspaceName: {workspaceName.ToString()}");
                return null;
            }
        }

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
                var zipExtractRoot = Path.Combine(extractedRoot, CouncilChatStringFunctions.SanitizeFileName(zipName, logger));
                Directory.CreateDirectory(zipExtractRoot);

                var entryCount = 0;
                long extractedBytes = 0;

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(entry.Name))
                        continue;

                    entryCount++;
                    if (entryCount > MaxZipEntries)
                    {
                        warnings.Add($"{zipFileName}: remaining entries skipped after {MaxZipEntries:n0} entries.");
                        break;
                    }

                    if (entry.Length > MaxZipEntryBytes)
                    {
                        warnings.Add($"{zipFileName}: {entry.FullName} skipped because the entry is too large.");
                        continue;
                    }

                    extractedBytes += entry.Length;
                    if (extractedBytes > MaxExtractedBytes)
                    {
                        warnings.Add($"{zipFileName}: remaining entries skipped after extracted byte cap.");
                        break;
                    }

                    var safeRelativePath = CouncilChatStringFunctions.BuildSafeZipRelativePath(entry.FullName, logger);
                    if (safeRelativePath is null)
                    {
                        warnings.Add($"{zipFileName}: unsafe zip path skipped: {entry.FullName}");
                        continue;
                    }

                    var destination = Path.GetFullPath(Path.Combine(zipExtractRoot, safeRelativePath));
                    if (!CouncilChatStaticsGeneral.IsInsideRoot(zipExtractRoot, destination, logger))
                    {
                        warnings.Add($"{zipFileName}: path traversal entry skipped: {entry.FullName}");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    await using (var entryStream = entry.Open())
                    await using (var destinationStream = System.IO.File.Create(destination))
                    {
                        await entryStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                    }

                    var bytes = await System.IO.File.ReadAllBytesAsync(destination, cancellationToken).ConfigureAwait(false);
                    var analyzedBytes = CouncilChatStaticsGeneral.AnalyzeBytes(CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspaceRoot, destination), logger), bytes, logger);
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