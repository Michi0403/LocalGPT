using DevExpress.XtraGauges.Core.Model;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    public sealed partial class LearnBaseKnowledgeImporterService(
        ICouncilKnowledgeService knowledgeService,
        ILogger<LearnBaseKnowledgeImporterService> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        LocalGptCatalogService catalog,
        ILocalGptRuntimePolicyDataService runtimePolicy,
        IRegexPatternService regexPatterns) : ILearnBaseKnowledgeImporterService
    {


        public async Task<LearnBaseImportResult> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rootPath = request.RootPath?.Trim() ?? string.Empty;
                var selection = BuildSelectionPolicy(request);
                var result = new LearnBaseImportResult
                {
                    RootPath = rootPath,
                    ImportMode = "Custom compact source-map import; stores bounded architecture fingerprints and documentation corpus summaries, not complete file contents.",
                    FilePolicy = BuildFilePolicyDescription(selection),
                    DuplicatePolicy = catalog.LearnBaseDuplicatePolicySummary
                };

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    result.Warnings.Add("No learn-base root was selected. Use the local path explorer or supply an explicit host folder.");
                    return result;
                }
                if (!Directory.Exists(rootPath))
                {
                    result.Warnings.Add($"Learn-base root was not found: {rootPath}");
                    return result;
                }

                await knowledgeService.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                if (request.ImportLearningSourceManifests)
                    await ImportLearningSourceManifestsAsync(rootPath, request, selection, result, cancellationToken).ConfigureAwait(false);
                if (request.ImportKnownDocumentationCorpora)
                    await ImportKnownDocumentationCorporaAsync(rootPath, request, selection, result, cancellationToken).ConfigureAwait(false);

                if (!request.ImportProjectArchitecture)
                {
                    result.ProjectCount = result.Projects.Count;
                    return result;
                }

                var configuredProjectLimit = catalog.LearnBaseScanProfiles
                    .Select(profile => profile.MaxProjects)
                    .DefaultIfEmpty(120)
                    .Max();
                var projectDirectories = councilText.BuildImportDirectories(
                    rootPath,
                    Math.Clamp(request.MaxProjects, 1, Math.Max(1, configuredProjectLimit)),
                    logger)
                    .ToArray();

                foreach (var projectDirectory in projectDirectories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var selectedFiles = FindSelectedFiles(projectDirectory, selection, 1600, result);
                        if (selectedFiles.Count == 0)
                        {
                            result.Warnings.Add($"No selected source files matched in {Path.GetFileName(projectDirectory)}.");
                            continue;
                        }
                        var summary = councilRuntime.BuildProjectSummary(rootPath, projectDirectory, selectedFiles, logger);
                        if (summary is null)
                        {
                            result.Warnings.Add($"Could not build an architecture summary for {Path.GetFileName(projectDirectory)}.");
                            continue;
                        }

                        if (request.SaveToKnowledge)
                        {
                            var knowledgeEntry = councilRuntime.ToKnowledgeEntry(summary, logger);
                            if (knowledgeEntry is null)
                            {
                                result.Warnings.Add($"Could not prepare a knowledge entry for {summary.Name}.");
                            }
                            else
                            {
                                var entry = await knowledgeService.SaveEntryAsync(knowledgeEntry, cancellationToken).ConfigureAwait(false);
                                summary.KnowledgeEntryId = entry.Id;
                                result.SavedKnowledgeCount++;
                            }
                        }

                        result.Projects.Add(summary);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        var name = Path.GetFileName(projectDirectory);
                        result.Warnings.Add($"Could not scan {name}: {ex.Message}");
                        logger.LogWarning(ex, "Could not import learn-base project {ProjectDirectory}.", projectDirectory);
                    }
                }

                result.ProjectCount = result.Projects.Count;
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ImportAsync");
                throw;
            }
        }

        private async Task ImportLearningSourceManifestsAsync(
            string rootPath,
            LearnBaseImportRequest request,
            LearnBaseSelectionPolicy selection,
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> manifestPaths;
            try
            {
                manifestPaths = Directory.EnumerateFiles(rootPath, "localgpt-learning-source.json", SearchOption.AllDirectories)
                    .Take(100)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Warnings.Add("Learning-source manifests could not be enumerated completely.");
                logger.LogWarning(ex, "Could not enumerate LocalGPT learning-source manifests; paths omitted from logs.");
                return;
            }

            foreach (var manifestPath in manifestPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
                    var manifest = JsonSerializer.Deserialize<LearningSourceManifest>(manifestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true });
                    if (manifest is null || string.IsNullOrWhiteSpace(manifest.Repository))
                    {
                        result.Warnings.Add($"Ignored invalid learning-source manifest: {Path.GetFileName(Path.GetDirectoryName(manifestPath))}");
                        continue;
                    }
                    var sourceRoot = Path.GetDirectoryName(manifestPath)!;
                    var include = CompileManifestRegex(manifest.IncludeRegex, @"(?i)\.(md|txt|ino|c|h|cpp|hpp|json|ya?ml)$");
                    var exclude = CompileManifestRegex(manifest.ExcludeRegex, @"(?!)");
                    var maximumFiles = Math.Clamp(manifest.MaximumFiles, 1, 20000);
                    var maximumBytes = Math.Min(selection.MaximumFileBytes, Math.Clamp(manifest.MaximumFileBytes, 1024, 8 * 1024 * 1024));
                    var matched = new List<FileInfo>();
                    var pending = new Stack<string>();
                    pending.Push(sourceRoot);
                    while (pending.Count > 0 && matched.Count < maximumFiles)
                    {
                        var current = pending.Pop();
                        IEnumerable<string> directories;
                        IEnumerable<string> files;
                        try
                        {
                            directories = Directory.EnumerateDirectories(current).ToArray();
                            files = Directory.EnumerateFiles(current).ToArray();
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            logger.LogDebug(ex, "Skipped one unreadable learning-source directory; path omitted from logs.");
                            continue;
                        }
                        foreach (var directory in directories)
                        {
                            var relative = Path.GetRelativePath(sourceRoot, directory).Replace('\\', '/') + "/";
                            if (!exclude.IsMatch(relative)) pending.Push(directory);
                        }
                        foreach (var file in files)
                        {
                            var relative = Path.GetRelativePath(sourceRoot, file).Replace('\\', '/');
                            if (relative.Equals("localgpt-learning-source.json", StringComparison.OrdinalIgnoreCase) || exclude.IsMatch(relative) || !include.IsMatch(relative)) continue;
                            var info = new FileInfo(file);
                            if (info.Length <= maximumBytes && selection.Matches(relative, info.Length)) matched.Add(info);
                            if (matched.Count >= maximumFiles) break;
                        }
                    }

                    var extensionCounts = matched
                        .GroupBy(file => string.IsNullOrWhiteSpace(file.Extension) ? "(none)" : file.Extension.ToLowerInvariant())
                        .OrderByDescending(group => group.Count())
                        .ThenBy(group => group.Key, StringComparer.Ordinal)
                        .Take(30)
                        .Select(group => $"{group.Key}: {group.Count()}")
                        .ToArray();
                    var representative = matched
                        .OrderBy(file => RepresentativeRank(file.Extension))
                        .ThenBy(file => file.FullName.Length)
                        .Take(40)
                        .ToArray();
                    var sourceSignatures = new List<string>();
                    foreach (var file in representative.Take(20))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var signature = await ExtractManifestFileSignatureAsync(sourceRoot, file, cancellationToken).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(signature)) sourceSignatures.Add(signature);
                    }
                    var helpfulSources = string.Join("; ", representative.Select(file => Path.GetRelativePath(sourceRoot, file.FullName).Replace('\\', '/')).Take(30));
                    var canonical = manifest.Repository + "|" + manifest.Revision + "|" + string.Join("|", matched.OrderBy(file => file.FullName, StringComparer.Ordinal).Select(file => Path.GetRelativePath(sourceRoot, file.FullName).Replace('\\', '/') + ":" + file.Length));
                    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
                    var content = new StringBuilder()
                        .AppendLine($"Repository: {manifest.Repository}")
                        .AppendLine($"Revision: {manifest.Revision}")
                        .AppendLine($"Matched files: {matched.Count}")
                        .AppendLine($"Manifest include regex: {manifest.IncludeRegex}")
                        .AppendLine($"Manifest exclude regex: {manifest.ExcludeRegex}")
                        .AppendLine("Extension map: " + string.Join(", ", extensionCounts))
                        .AppendLine("Representative parsed source/document signatures:")
                        .AppendLine(sourceSignatures.Count == 0 ? "- No bounded readable signatures were extracted." : string.Join(Environment.NewLine, sourceSignatures.Select(item => "- " + item)))
                        .AppendLine("Use this compact source map as navigation evidence. Read exact current files before making board, compiler, GPIO, or API claims.")
                        .ToString().Trim();
                    Guid? knowledgeEntryId = null;
                    if (request.SaveToKnowledge)
                    {
                        var entry = new CouncilKnowledgeEntry
                        {
                            Id = DeterministicGuid(hash),
                            Topic = $"{manifest.Repository} embedded source map",
                            Scope = "Installer learning source",
                            Content = content,
                            Source = string.IsNullOrWhiteSpace(manifest.SourceUrl) ? manifest.Repository : manifest.SourceUrl,
                            HelpfulSources = helpfulSources,
                            Tags = string.Join(",", (manifest.Topics ?? []).Concat(manifest.RoleKeys ?? []).Append("manifest-import").Distinct(StringComparer.OrdinalIgnoreCase)),
                            Confidence = 70,
                            VerificationStatus = "SourceMapped",
                            ReviewStatus = "NeedsUserReview",
                            SourceHash = hash,
                            SourceDateUtc = DateTime.UtcNow,
                            IsUserApproved = false
                        };
                        var saved = await knowledgeService.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                        knowledgeEntryId = saved.Id;
                        result.SavedKnowledgeCount++;
                    }
                    result.Projects.Add(new LearnBaseProjectSummary
                    {
                        Name = $"{manifest.Repository} manifest corpus",
                        SourcePath = sourceRoot,
                        Architecture = "Installer-provisioned compact source/document corpus parsed through an explicit include/exclude regex manifest.",
                        ProtocolsAndComponents = string.Join(", ", manifest.Topics ?? []),
                        TargetFrameworks = "Documentation and embedded source corpus; exact build targets remain repository-owned.",
                        PackageReferences = "Not expanded by compact manifest import.",
                        ImportantFiles = helpfulSources,
                        SourceFileCount = matched.Count,
                        BinaryFileCount = 0,
                        KnowledgeEntryId = knowledgeEntryId
                    });
                    logger.LogInformation("Parsed LocalGPT learning-source manifest for repository {Repository}: {MatchedFileCount} bounded file(s), saved={Saved}.", manifest.Repository, matched.Count, request.SaveToKnowledge);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
                {
                    result.Warnings.Add($"Could not parse learning-source manifest under {Path.GetFileName(Path.GetDirectoryName(manifestPath))}: {ex.Message}");
                    logger.LogWarning(ex, "Could not parse one LocalGPT learning-source manifest; path and source content omitted from logs.");
                }
            }
        }

        private Guid DeterministicGuid(string hexadecimalHash)
        {
    try
    {
                var bytes = Convert.FromHexString(hexadecimalHash);
                return new Guid(bytes.AsSpan(0, 16));
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(DeterministicGuid)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(DeterministicGuid)} failed.");
        throw;
    }
}

        private Regex CompileManifestRegex(string? pattern, string fallback)
        {
    try
    {
                var value = string.IsNullOrWhiteSpace(pattern) ? fallback : pattern;
                return regexPatterns.Compile(value, "CultureInvariant", runtimePolicy.RegexTimeout);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(CompileManifestRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(CompileManifestRegex)} failed.");
        throw;
    }
}

        private int RepresentativeRank(string extension) {
    try
    {
        return extension.ToLowerInvariant() switch
        {
            ".md" or ".mdx" or ".rst" or ".adoc" => 0,
            ".ino" or ".pde" => 1,
            ".h" or ".hpp" => 2,
            ".c" or ".cc" or ".cpp" or ".cxx" => 3,
            ".json" or ".yml" or ".yaml" or ".toml" or ".ini" => 4,
            _ => 5
        };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(RepresentativeRank)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(RepresentativeRank)} failed.");
        throw;
    }
}

        private async Task<string> ExtractManifestFileSignatureAsync(string root, FileInfo file, CancellationToken cancellationToken)
        {
    try
    {
                const int maximumCharacters = 12000;
                await using var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);
                var buffer = new char[Math.Min(maximumCharacters, (int)Math.Min(file.Length + 1, maximumCharacters))];
                var count = await reader.ReadBlockAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                var text = new string(buffer, 0, count);
                var relative = Path.GetRelativePath(root, file.FullName).Replace('\\', '/');
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(line => line.StartsWith('#') || line.Contains("setup(", StringComparison.Ordinal) || line.Contains("loop(", StringComparison.Ordinal) || line.Contains("#define ", StringComparison.Ordinal) || line.Contains("class ", StringComparison.Ordinal) || line.Contains("struct ", StringComparison.Ordinal) || line.Contains("GPIO", StringComparison.OrdinalIgnoreCase) || line.Contains("pinMode", StringComparison.Ordinal))
                    .Select(line => line.Length <= 220 ? line : line[..220])
                    .Take(6)
                    .ToArray();
                return lines.Length == 0 ? relative : relative + " :: " + string.Join(" | ", lines);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(ExtractManifestFileSignatureAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(ExtractManifestFileSignatureAsync)} failed.");
        throw;
    }
}

        private sealed class LearningSourceManifest
        {
            public int SchemaVersion { get; set; } = 1;
            public string Repository { get; set; } = string.Empty;
            public string SourceUrl { get; set; } = string.Empty;
            public string Revision { get; set; } = string.Empty;
            public string IncludeRegex { get; set; } = string.Empty;
            public string ExcludeRegex { get; set; } = string.Empty;
            public int MaximumFiles { get; set; } = 12000;
            public int MaximumFileBytes { get; set; } = 2 * 1024 * 1024;
            public List<string> Topics { get; set; } = [];
            public List<string> RoleKeys { get; set; } = [];
            public string ImportMode { get; set; } = "CompactManifestCorpus";
        }

        private async Task ImportKnownDocumentationCorporaAsync(
            string rootPath,
            LearnBaseImportRequest request,
            LearnBaseSelectionPolicy selection,
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var candidate in councilRuntime.BuildDocumentationCorpusCandidates(rootPath,logger))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (councilRuntime.LooksLikeWindowsDevDocsRoot(candidate, logger))
                        await ImportWindowsDevDocsCorpusAsync(candidate, request, selection, result, cancellationToken).ConfigureAwait(false);

                    if (councilRuntime.LooksLikeDotNetDocsRoot(candidate, logger))
                        await ImportDotNetDocsCorpusAsync(candidate, request, selection, result, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ImportKnownDocumentationCorporaAsync");

            }
        }
        private async Task ImportDotNetDocsCorpusAsync(
            string rootPath,
            LearnBaseImportRequest request,
            LearnBaseSelectionPolicy selection,
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var markdownFiles = FindSelectedFiles(rootPath, selection, 8000, result)
                    .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (markdownFiles.Length == 0)
                    return;

                foreach (var entry in councilRuntime.BuildDotNetDocsEntries(rootPath, markdownFiles, logger))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Guid? knowledgeEntryId = null;
                    if (request.SaveToKnowledge)
                    {
                        var saved = await knowledgeService.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                        knowledgeEntryId = saved.Id;
                        result.SavedKnowledgeCount++;
                    }

                    result.Projects.Add(new LearnBaseProjectSummary
                    {
                        Name = entry.Topic,
                        SourcePath = rootPath,
                        Architecture = ".NET docs corpus; Microsoft Learn authoring; C# language/compiler; modern .NET architecture; ASP.NET Core/Blazor source map",
                        ProtocolsAndComponents = "DocFX; Microsoft Learn markdown; C# compiler diagnostics; C# language reference; .NET architecture; ASP.NET Core; Blazor; EF/data guidance",
                        TargetFrameworks = "Documentation corpus, not a compiled project",
                        PackageReferences = "none",
                        ImportantFiles = entry.HelpfulSources,
                        SourceFileCount = markdownFiles.Length,
                        BinaryFileCount = 0,
                        KnowledgeEntryId = knowledgeEntryId
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ImportDotNetDocsCorpusAsync");

            }
        }
        private async Task ImportWindowsDevDocsCorpusAsync(
            string rootPath,
            LearnBaseImportRequest request,
            LearnBaseSelectionPolicy selection,
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var markdownFiles = FindSelectedFiles(rootPath, selection, 6000, result)
                    .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (markdownFiles.Length == 0)
                    return;

                foreach (var entry in councilRuntime.BuildWindowsDevDocsEntries(rootPath, markdownFiles, logger))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Guid? knowledgeEntryId = null;
                    if (request.SaveToKnowledge)
                    {
                        var saved = await knowledgeService.SaveEntryAsync(entry, cancellationToken).ConfigureAwait(false);
                        knowledgeEntryId = saved.Id;
                        result.SavedKnowledgeCount++;
                    }

                    result.Projects.Add(new LearnBaseProjectSummary
                    {
                        Name = entry.Topic,
                        SourcePath = rootPath,
                        Architecture = "Windows developer docs corpus; DocFX/Microsoft Learn authoring; Windows app platform; deployment/support/design guidance",
                        ProtocolsAndComponents = "DocFX; Microsoft Learn markdown; Windows App SDK; WinUI; WebView2; MSIX; winget; Terminal; Dev Drive; PowerToys; Arm64; accessibility",
                        TargetFrameworks = "Documentation corpus, not a compiled project",
                        PackageReferences = "none",
                        ImportantFiles = entry.HelpfulSources,
                        SourceFileCount = markdownFiles.Length,
                        BinaryFileCount = 0,
                        KnowledgeEntryId = knowledgeEntryId
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ImportWindowsDevDocsCorpusAsync");
          
            }
        }

        private LearnBaseSelectionPolicy BuildSelectionPolicy(LearnBaseImportRequest request)
        {
    try
    {
                var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var extension in request.FileExtensions ?? new List<string>())
                    AddExtension(selected, extension);
                foreach (var extension in (request.AdditionalFileExtensions ?? string.Empty).Split(
                             [',', ';', '\r', '\n', '\t', ' '],
                             StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    AddExtension(selected, extension);
                if (selected.Count == 0)
                {
                    foreach (var extension in catalog.LearnBaseKnownExtensions)
                        AddExtension(selected, extension);
                }

                return new LearnBaseSelectionPolicy(
                    selected,
                    CompileManifestRegex(request.FileIncludeRegex, @".*"),
                    CompileManifestRegex(request.FileExcludeRegex, @"(?!)"),
                    Math.Clamp(request.MaximumFileBytes, 1024, 16 * 1024 * 1024),
                    catalog.ExcludedDirectoryNames,
                    catalog.BinaryExtensions);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(BuildSelectionPolicy)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(BuildSelectionPolicy)} failed.");
        throw;
    }
}

        private void AddExtension(HashSet<string> extensions, string? value)
        {
    try
    {
                var trimmed = value?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    return;
                if (trimmed.StartsWith("*.", StringComparison.Ordinal))
                    trimmed = trimmed[1..];
                if (!trimmed.StartsWith(".", StringComparison.Ordinal))
                    trimmed = "." + trimmed;
                extensions.Add(trimmed.ToLowerInvariant());
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(AddExtension)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(AddExtension)} failed.");
        throw;
    }
}

        private string BuildFilePolicyDescription(LearnBaseSelectionPolicy selection)
        {
    try
    {
                var ordered = selection.Extensions.OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase).ToArray();
                var visible = string.Join(", ", ordered.Take(36));
                var remainder = ordered.Length > 36 ? $" and {ordered.Length - 36} more" : string.Empty;
                return $"Selected endings: {visible}{remainder}. Include regex: {selection.IncludeRegex}. Exclude regex: {selection.ExcludeRegex}. Maximum file size: {selection.MaximumFileBytes:N0} bytes. Binary containers and excluded build/cache folders remain inactive.";
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(BuildFilePolicyDescription)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LearnBaseKnowledgeImporterService)}.{nameof(BuildFilePolicyDescription)} failed.");
        throw;
    }
}

        private IReadOnlyList<FileInfo> FindSelectedFiles(
            string rootPath,
            LearnBaseSelectionPolicy selection,
            int maximumFiles,
            LearnBaseImportResult result)
        {
            var files = new List<FileInfo>(Math.Min(maximumFiles, 2048));
            var pending = new Stack<string>();
            pending.Push(rootPath);
            while (pending.Count > 0 && files.Count < maximumFiles)
            {
                var current = pending.Pop();
                string[] directories;
                string[] currentFiles;
                try
                {
                    directories = Directory.GetDirectories(current);
                    currentFiles = Directory.GetFiles(current);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogDebug(ex, "Skipped one unreadable LearnBase directory; path omitted from logs.");
                    continue;
                }

                foreach (var directory in directories)
                {
                    if (!selection.ExcludedDirectoryNames.Contains(Path.GetFileName(directory)))
                        pending.Push(directory);
                }

                foreach (var file in currentFiles)
                {
                    var relative = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
                    var info = new FileInfo(file);
                    if (selection.Matches(relative, info.Length))
                        files.Add(info);
                    if (files.Count >= maximumFiles)
                        break;
                }
            }

            if (files.Count >= maximumFiles)
                result.Warnings.Add($"The selected-file scan stopped at the configured limit of {maximumFiles:N0} files under {Path.GetFileName(rootPath)}.");
            return files;
        }

        private sealed class LearnBaseSelectionPolicy(
            IReadOnlySet<string> extensions,
            Regex includeRegex,
            Regex excludeRegex,
            int maximumFileBytes,
            IReadOnlySet<string> excludedDirectoryNames,
            IReadOnlySet<string> binaryExtensions)
        {
            public IReadOnlySet<string> Extensions { get; } = extensions;
            public Regex IncludeRegex { get; } = includeRegex;
            public Regex ExcludeRegex { get; } = excludeRegex;
            public int MaximumFileBytes { get; } = maximumFileBytes;
            public IReadOnlySet<string> ExcludedDirectoryNames { get; } = excludedDirectoryNames;
            public IReadOnlySet<string> BinaryExtensions { get; } = binaryExtensions;

            public bool Matches(string relativePath, long length)
            {
    try
    {
                    if (length <= 0 || length > MaximumFileBytes)
                        return false;
                    if (!IncludeRegex.IsMatch(relativePath) || ExcludeRegex.IsMatch(relativePath))
                        return false;
                    if (BinaryExtensions.Any(extension => relativePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                        return false;
                    return Extensions.Any(extension => relativePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
            
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method LearnBaseSelectionPolicy.Matches failed: {__serviceMethodException}");
        throw;
    }
}
        }
    }
}
