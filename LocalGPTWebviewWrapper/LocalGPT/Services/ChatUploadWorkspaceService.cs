using DevExpress.CodeParser;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    public sealed class ChatUploadWorkspaceService(
        ILogger<ChatUploadWorkspaceService> logger) : IChatUploadWorkspaceService
    {
        private const int MaxFiles = 12;
        private const long MaxSingleFileBytes = 32 * 1024 * 1024;
        private const long MaxTotalFileBytes = 96 * 1024 * 1024;
        private const int MaxZipEntries = 400;
        private const long MaxZipEntryBytes = 8 * 1024 * 1024;
        private const long MaxExtractedBytes = 64 * 1024 * 1024;
        private const int MaxContextCharacters = 80_000;
        private const int MaxExcerptCharactersPerFile = 6_000;
        private const int MaxBinaryStringCharacters = 8_000;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt",
            ".md",
            ".json",
            ".xml",
            ".csv",
            ".cs",
            ".razor",
            ".cshtml",
            ".css",
            ".scss",
            ".js",
            ".ts",
            ".tsx",
            ".html",
            ".htm",
            ".xaml",
            ".sln",
            ".csproj",
            ".vbproj",
            ".fsproj",
            ".props",
            ".targets",
            ".config",
            ".editorconfig",
            ".yml",
            ".yaml",
            ".toml",
            ".sql",
            ".ps1",
            ".cmd",
            ".bat",
            ".sh",
            ".java",
            ".kt",
            ".gradle",
            ".mcfunction",
            ".mcmeta",
            ".properties"
        };

        private static readonly HashSet<string> BinaryDiagnosticExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll",
            ".exe",
            ".pdb",
            ".appxsym",
            ".nupkg",
            ".wasm"
        };

        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "ChatUploadWorkspaces");

        public async Task<ChatUploadWorkspaceResult> CreateWorkspaceAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceInputFile> files,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(WorkspaceRoot);

            var fileList = files
                .Where(file => !string.IsNullOrWhiteSpace(file.Name))
                .Take(MaxFiles)
                .ToList();
            var workspaceName = BuildWorkspaceName(prompt, fileList);
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

                if (input.SizeBytes > MaxSingleFileBytes)
                {
                    warnings.Add($"{input.Name} skipped: file is larger than {MaxSingleFileBytes:n0} bytes.");
                    continue;
                }

                totalUploadedBytes += input.SizeBytes;
                if (totalUploadedBytes > MaxTotalFileBytes)
                {
                    warnings.Add("Remaining files skipped: upload batch exceeded the LocalGPT prompt-workspace byte cap.");
                    break;
                }

                var safeName = BuildUniqueFileName(originalRoot, input.Name);
                var originalPath = Path.Combine(originalRoot, safeName);
                var bytes = input.Data.ToArray();
                await File.WriteAllBytesAsync(originalPath, bytes, cancellationToken);

                var originalRelativePath = ToForwardSlash(Path.GetRelativePath(root, originalPath));
                if (IsZip(input.Name))
                {
                    analyzedFiles.Add(BuildBinarySummary(originalRelativePath, bytes.Length, "zip", false,
                        "Original zip saved. Extracted safe entries are listed separately."));
                    await ExtractZipAsync(root, extractedRoot, safeName, bytes, analyzedFiles, warnings, cancellationToken);
                }
                else
                {
                    analyzedFiles.Add(AnalyzeBytes(originalRelativePath, bytes));
                }
            }

            if (!fileList.Any())
                warnings.Add("No files were supplied for this prompt workspace.");

            var context = BuildContextMarkdown(workspaceName, root, prompt, analyzedFiles, warnings);
            var contextPath = Path.Combine(root, "context.md");
            await File.WriteAllTextAsync(contextPath, context, Encoding.UTF8, cancellationToken);

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
                }, JsonOptions),
                Encoding.UTF8,
                cancellationToken);

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

        public IReadOnlyList<ChatUploadWorkspaceSummary> ListWorkspaces(int take = 20)
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

        public ChatUploadWorkspaceSummary? GetLatestWorkspace(TimeSpan? maxAge = null)
        {
            var latest = ListWorkspaces(1).FirstOrDefault();
            if (latest is null || maxAge is null)
                return latest;

            var age = DateTimeOffset.UtcNow - latest.CreatedAtUtc;
            return age <= maxAge.Value ? latest : null;
        }

        public string GetLatestContextMarkdown(int maxCharacters, TimeSpan? maxAge = null)
        {
            var latest = GetLatestWorkspace(maxAge);
            if (latest is null)
                return string.Empty;

            try
            {
                if (!File.Exists(latest.ContextPath))
                    return string.Empty;

                return CouncilChatStringFunctions.TrimForPrompt(File.ReadAllText(latest.ContextPath), maxCharacters);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read latest chat upload workspace context.");
                return string.Empty;
            }
        }

        public async Task<string> ReadContextMarkdownAsync(
            string workspaceName,
            int maxCharacters,
            CancellationToken cancellationToken = default)
        {
            var workspace = ResolveWorkspacePath(workspaceName);
            if (workspace is null)
                return string.Empty;

            var contextPath = Path.Combine(workspace, "context.md");
            if (!File.Exists(contextPath))
                return string.Empty;

            var context = await File.ReadAllTextAsync(contextPath, cancellationToken);
            return CouncilChatStringFunctions.TrimForPrompt(context, maxCharacters);
        }

        public IReadOnlyList<ChatUploadWorkspaceFileSummary> ListFiles(string workspaceName, int take = 250)
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
                        ToForwardSlash(Path.GetRelativePath(workspace, path)),
                        DetermineFileKind(path),
                        info.Length,
                        info.LastWriteTimeUtc,
                        IsTextLike(path) || BinaryDiagnosticExtensions.Contains(Path.GetExtension(path)),
                        path.EndsWith("context.md", StringComparison.OrdinalIgnoreCase)
                            ? "AI prompt context generated by LocalGPT."
                            : "Uploaded or extracted workspace file.");
                })
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(take, 1, 1000))
                .ToList();
        }

        public async Task<ChatUploadWorkspaceFileReadResult?> ReadFileAsync(
            string workspaceName,
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default)
        {
            var workspace = ResolveWorkspacePath(workspaceName);
            if (workspace is null)
                return null;

            var file = ResolveWorkspaceFile(workspace, relativePath);
            if (file is null || !File.Exists(file))
                return null;

            var info = new FileInfo(file);
            if (info.Length > MaxSingleFileBytes)
                return new ChatUploadWorkspaceFileReadResult(
                    workspaceName,
                    ToForwardSlash(Path.GetRelativePath(workspace, file)),
                    file,
                    "too-large",
                    info.Length,
                    "File is too large for inline reading. Use the file summary and inspect it manually.");

            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            var analyzed = AnalyzeBytes(ToForwardSlash(Path.GetRelativePath(workspace, file)), bytes);
            return new ChatUploadWorkspaceFileReadResult(
                workspaceName,
                analyzed.Summary.RelativePath,
                file,
                analyzed.Summary.Kind,
                analyzed.Summary.Length,
                CouncilChatStringFunctions.TrimForPrompt(analyzed.Excerpt, maxCharacters));
        }

        public string? ResolveWorkspacePath(string workspaceName)
        {
            var safeName = Path.GetFileName(workspaceName);
            if (string.IsNullOrWhiteSpace(safeName) ||
                !string.Equals(workspaceName, safeName, StringComparison.Ordinal))
            {
                return null;
            }

            var root = Path.GetFullPath(WorkspaceRoot);
            var candidate = Path.GetFullPath(Path.Combine(root, safeName));
            if (!IsInsideRoot(root, candidate) || !Directory.Exists(candidate))
                return null;

            return candidate;
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
                var zipExtractRoot = Path.Combine(extractedRoot, SanitizeFileName(zipName));
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

                    var safeRelativePath = BuildSafeZipRelativePath(entry.FullName);
                    if (safeRelativePath is null)
                    {
                        warnings.Add($"{zipFileName}: unsafe zip path skipped: {entry.FullName}");
                        continue;
                    }

                    var destination = Path.GetFullPath(Path.Combine(zipExtractRoot, safeRelativePath));
                    if (!IsInsideRoot(zipExtractRoot, destination))
                    {
                        warnings.Add($"{zipFileName}: path traversal entry skipped: {entry.FullName}");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    await using (var entryStream = entry.Open())
                    await using (var destinationStream = File.Create(destination))
                    {
                        await entryStream.CopyToAsync(destinationStream, cancellationToken);
                    }

                    var bytes = await File.ReadAllBytesAsync(destination, cancellationToken);
                    analyzedFiles.Add(AnalyzeBytes(ToForwardSlash(Path.GetRelativePath(workspaceRoot, destination)), bytes));
                }
            }
            catch (InvalidDataException ex)
            {
                warnings.Add($"{zipFileName}: zip could not be opened: {ex.Message}");
            }
        }

        private static AnalyzedUploadFile AnalyzeBytes(string relativePath, byte[] bytes)
        {
            if (IsZip(relativePath))
            {
                return BuildBinarySummary(
                    relativePath,
                    bytes.Length,
                    "zip",
                    false,
                    "Zip file saved as uploaded. Extracted safe entries are represented separately.");
            }

            var isText = IsTextLike(relativePath) || LooksLikeText(bytes);
            if (isText)
            {
                var text = DecodeText(bytes);
                return BuildSummary(relativePath, bytes.Length, "text", true, "Text excerpt included.", text);
            }

            var extension = Path.GetExtension(relativePath);
            if (BinaryDiagnosticExtensions.Contains(extension))
            {
                var strings = ExtractPrintableStrings(bytes, MaxBinaryStringCharacters);
                var note = extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase)
                    ? "PDB/debug file summarized with printable strings only."
                    : "Binary file summarized with printable strings only.";
                return BuildSummary(relativePath, bytes.Length, "binary-strings", true, note, strings);
            }

            return BuildBinarySummary(
                relativePath,
                bytes.Length,
                "binary",
                false,
                "Binary file saved but not included in prompt context.");
        }

        private static string BuildContextMarkdown(
            string workspaceName,
            string root,
            string prompt,
            IReadOnlyList<AnalyzedUploadFile> analyzedFiles,
            IReadOnlyList<string> warnings, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
              .AppendLine("# LocalGPT Chat Upload Workspace")
              .AppendLine()
              .AppendLine($"Workspace: `{workspaceName}`")
              .AppendLine($"Root path: `{root}`")
              .AppendLine($"Created UTC: {DateTimeOffset.UtcNow:O}")
              .AppendLine()
              .AppendLine("## Prompt")
              .AppendLine(CouncilChatStringFunctions.TrimForPrompt(prompt, 4_000,logger))
              .AppendLine()
              .AppendLine("## AI workflow instructions")
              .AppendLine("- Use this workspace as uploaded user evidence for the current DXAiChat prompt.")
              .AppendLine("- Read files through chat.upload_workspace_* DXAiFunctions instead of asking for huge pasted context.")
              .AppendLine("- Zips are extracted safely; skipped entries are listed as warnings.")
              .AppendLine("- PDB, DLL, EXE, WASM, and other binaries are never executed; only bounded printable strings are shown.")
              .AppendLine("- Generated or edited code belongs in a council artifact workspace, then a refreshed zip download.")
              .AppendLine();

                if (warnings.Count > 0)
                {
                    builder.AppendLine("## Warnings");
                    foreach (var warning in warnings)
                        builder.AppendLine($"- {warning}");
                    builder.AppendLine();
                }

                builder.AppendLine("## Files");
                foreach (var file in analyzedFiles.Select(file => file.Summary))
                {
                    builder
                        .Append("- ")
                        .Append(file.RelativePath)
                        .Append(" (")
                        .Append(file.Kind)
                        .Append(", ")
                        .Append(file.Length)
                        .Append(" bytes): ")
                        .AppendLine(file.Note);
                }

                builder.AppendLine();
                builder.AppendLine("## Extracted context");

                var remainingCharacters = MaxContextCharacters - builder.Length;
                foreach (var file in analyzedFiles.Where(file => file.Summary.IncludedInPrompt))
                {
                    if (remainingCharacters <= 0)
                        break;

                    var excerpt = CouncilChatStringFunctions.TrimForPrompt(file.Excerpt, Math.Min(MaxExcerptCharactersPerFile, remainingCharacters), logger);
                    if (string.IsNullOrWhiteSpace(excerpt))
                        continue;

                    var section = new StringBuilder()
                        .AppendLine()
                        .AppendLine($"### {file.Summary.RelativePath}")
                        .AppendLine($"Kind: {file.Summary.Kind}. {file.Summary.Note}")
                        .AppendLine()
                        .AppendLine("```text")
                        .AppendLine(excerpt)
                        .AppendLine("```")
                        .ToString();

                    if (section.Length > remainingCharacters)
                        section = CouncilChatStringFunctions.TrimForPrompt(section, remainingCharacters, logger);

                    builder.Append(section);
                    remainingCharacters -= section.Length;
                }

                return CouncilChatStringFunctions.TrimForPrompt(builder.ToString(), MaxContextCharacters, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Error inBuildContextMarkdown workspaceName {workspaceName} root {root} prompt {prompt} analyzedFiles {analyzedFiles.ToString()} warnings {warnings.ToString()}");
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
                logger.LogWarning(ex, "Could not summarize chat upload workspace {Path}.", path);
                return null;
            }
        }

        private static string? ResolveWorkspaceFile(string workspace, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalized))
                return null;

            var root = Path.GetFullPath(workspace);
            var file = Path.GetFullPath(Path.Combine(root, normalized));
            return IsInsideRoot(root, file) ? file : null;
        }

        private static bool IsInsideRoot(string root, string path)
        {
            var normalizedRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildWorkspaceName(
            string prompt,
            IReadOnlyList<ChatUploadWorkspaceInputFile> files)
        {
            var source = files.FirstOrDefault()?.Name;
            if (string.IsNullOrWhiteSpace(source))
                source = prompt;

            var slug = Regex.Replace(source ?? "prompt", "[^A-Za-z0-9]+", "-")
                .Trim('-')
                .ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
                slug = "prompt";
            if (slug.Length > 24)
                slug = slug[..24].Trim('-');

            var suffix = Guid.NewGuid().ToString("N")[..8];
            return $"chat-upload-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{slug}-{suffix}";
        }

        private static string BuildUniqueFileName(string directory, string fileName)
        {
            var safe = SanitizeFileName(fileName);
            var candidate = Path.Combine(directory, safe);
            if (!File.Exists(candidate))
                return safe;

            var name = Path.GetFileNameWithoutExtension(safe);
            var extension = Path.GetExtension(safe);
            for (var i = 1; i < 1000; i++)
            {
                candidate = Path.Combine(directory, $"{name}-{i}{extension}");
                if (!File.Exists(candidate))
                    return Path.GetFileName(candidate);
            }

            return $"{name}-{Guid.NewGuid():N}{extension}";
        }

        private static string SanitizeFileName(string fileName)
        {
            var safe = Path.GetFileName(fileName);
            foreach (var invalid in Path.GetInvalidFileNameChars())
                safe = safe.Replace(invalid, '_');

            return string.IsNullOrWhiteSpace(safe) ? "upload.bin" : safe;
        }

        private static string? BuildSafeZipRelativePath(string fullName)
        {
            var parts = fullName
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => part is not "." and not "..")
                .Select(SanitizeFileName)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            return parts.Length == 0 ? null : Path.Combine(parts);
        }

        private static bool IsZip(string path) =>
            Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);

        private static bool IsTextLike(string path) =>
            TextExtensions.Contains(Path.GetExtension(path));

        private static string DetermineFileKind(string path)
        {
            if (IsZip(path))
                return "zip";
            if (IsTextLike(path))
                return "text";
            return BinaryDiagnosticExtensions.Contains(Path.GetExtension(path))
                ? "binary-diagnostic"
                : "binary";
        }

        private static bool LooksLikeText(byte[] bytes)
        {
            if (bytes.Length == 0)
                return true;

            var sampleLength = Math.Min(bytes.Length, 8192);
            var controlCount = 0;
            for (var i = 0; i < sampleLength; i++)
            {
                var value = bytes[i];
                if (value == 0)
                    return false;

                if (value < 9 || (value > 13 && value < 32))
                    controlCount++;
            }

            return controlCount <= sampleLength / 20;
        }

        private static string DecodeText(byte[] bytes)
        {
            try
            {
                return SanitizeForPrompt(Encoding.UTF8.GetString(bytes));
            }
            catch
            {
                return SanitizeForPrompt(Encoding.Latin1.GetString(bytes));
            }
        }

        private static string ExtractPrintableStrings(byte[] bytes, int maxCharacters)
        {
            var builder = new StringBuilder();
            var current = new StringBuilder();

            foreach (var value in bytes)
            {
                var printable = value is >= 32 and <= 126 || value is 9;
                if (printable)
                {
                    current.Append((char)value);
                    continue;
                }

                FlushCurrentString(builder, current, maxCharacters);
                if (builder.Length >= maxCharacters)
                    break;
            }

            FlushCurrentString(builder, current, maxCharacters);
            return SanitizeForPrompt(builder.ToString());
        }

        private static void FlushCurrentString(StringBuilder builder, StringBuilder current, int maxCharacters)
        {
            if (current.Length >= 4 && builder.Length < maxCharacters)
            {
                builder.AppendLine(current.ToString());
            }

            current.Clear();
        }

        private static AnalyzedUploadFile? BuildSummary(
            string relativePath,
            long length,
            string kind,
            bool includedInPrompt,
            string note,
            string excerpt, ILogger logger)
        {
            try
            {
                return new AnalyzedUploadFile(
                new ChatUploadWorkspaceFileSummary(
                    relativePath,
                    kind,
                    length,
                    DateTime.UtcNow,
                    includedInPrompt,
                    note),
                CouncilChatStringFunctions.TrimForPrompt(excerpt, MaxExcerptCharactersPerFile, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildSummary relativePath {relativePath} length {length} kind {kind} includedInPrompt {includedInPrompt} note {note} excerpt {excerpt}");
                return null;
            }
        }

        public static AnalyzedUploadFile? BuildBinarySummary(
            string relativePath,
            long length,
            string kind,
            bool includedInPrompt,
            string note, ILogger logger) 
        {
            try
            {
                return BuildSummary(relativePath, length, kind, includedInPrompt, note, string.Empty, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildBinarySummary relativePath {relativePath} length {length} kind {kind} includedInPrompt {includedInPrompt} note {note}");
                return null;
            }
        }


        public static string SanitizeForPrompt(string text, ILogger logger)
        {
            try
            {
                var userName = Environment.UserName;
                if (!string.IsNullOrWhiteSpace(userName))
                    text = text.Replace(userName, "%USER%", StringComparison.OrdinalIgnoreCase);

                return text.Replace("\0", string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SanitizeForPrompt text {text}");
                return string.Empty;
            }
        }



        public static string ToForwardSlash(string path, ILogger logger) 
        {
            try
            {
                return path.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToForwardSlash path {path}");
                return string.Empty;
            }
        }
            

        public sealed record AnalyzedUploadFile(
            ChatUploadWorkspaceFileSummary Summary,
            string Excerpt);
    }
}
