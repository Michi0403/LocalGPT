using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Resolves generated DocFX artifacts and compiler-generated XML comments without exposing arbitrary filesystem paths.
/// </summary>
/// <param name="environment">Web host environment dependency used by the documentation catalog workflow to provide the corresponding application capability.</param>
/// <param name="version">Custom version dependency used by the documentation catalog workflow to provide the corresponding application capability.</param>
/// <param name="translation">Documentation translation adapter dependency used by the documentation catalog workflow to provide the corresponding application capability.</param>
/// <param name="platform">Platform runtime service used to compare and normalize documentation filesystem paths safely.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[DocumentationUpdated("2.2.8")]
public sealed class DocumentationCatalogService(
    IWebHostEnvironment environment,
    ICustomVersion version,
    IDocumentationTranslationAdapter translation,
    IPlatformRuntimeService platform,
    ILogger<DocumentationCatalogService> logger) : IDocumentationCatalogService
{
    /// <summary>
    /// Stores the internal comment sync state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object commentSync = new();
    /// <summary>
    /// Stores the internal documentation root sync state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object documentationRootSync = new();
    /// <summary>
    /// Stores the internal application root state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string applicationRoot = Path.GetFullPath(AppContext.BaseDirectory);
    /// <summary>
    /// Stores the in-memory comment cache collection maintained internally by <see cref="DocumentationCatalogService"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<LocalGptDocumentationComment>? commentCache;
    /// <summary>
    /// Stores the internal comment cache path state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private string? commentCachePath;
    /// <summary>
    /// Stores the internal comment cache write UTC state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private DateTime commentCacheWriteUtc;
    /// <summary>
    /// Stores the internal documentation root cache state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private string? documentationRootCache;
    /// <summary>
    /// Stores the internal documentation root cache expires UTC state used by <see cref="DocumentationCatalogService"/> while executing its surrounding workflow.
    /// </summary>
    private DateTime documentationRootCacheExpiresUtc;

    /// <summary>
    /// Retrieves status as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalGptDocumentationStatus GetStatus()
    {
        try
        {
            var documentationRoot = ResolveDocumentationRoot();
            var manifest = ReadBuildManifest(documentationRoot);
            var pdfPath = ResolveInstalledPdfPath(documentationRoot, manifest?.Version);
            var xmlDocumentationPath = ResolveXmlDocumentationPath(documentationRoot);
            var comments = GetCommentCatalog();
            return new LocalGptDocumentationStatus
            {
                Version = string.IsNullOrWhiteSpace(manifest?.Version) ? version.Version : manifest.Version,
                InspectedAtUtc = DateTime.UtcNow,
                GeneratedAtUtc = manifest?.GeneratedAtUtc,
                HtmlAvailable = documentationRoot is not null && File.Exists(Path.Combine(documentationRoot, "index.html")),
                PdfAvailable = pdfPath is not null,
                XmlCommentsAvailable = xmlDocumentationPath is not null,
                CommentCount = comments.Count,
                HtmlUrl = "/help-docs/index.html",
                PdfUrl = "/api/documentation/pdf",
                CommentsUrl = "/api/documentation/comments",
                PdfFileName = pdfPath is null ? $"LocalGPT-{version.Version}.pdf" : Path.GetFileName(pdfPath)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Inspecting generated LocalGPT documentation failed.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves PDF path as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string? GetPdfPath()
    {
        try
        {
            var documentationRoot = ResolveDocumentationRoot();
            var manifest = ReadBuildManifest(documentationRoot);
            return ResolveInstalledPdfPath(documentationRoot, manifest?.Version);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the generated LocalGPT PDF path failed.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves HTML file path as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string? GetHtmlFilePath(string? relativePath)
    {
        try
        {
            var documentationRoot = ResolveDocumentationRoot();
            if (documentationRoot is null) return null;

            var normalized = string.IsNullOrWhiteSpace(relativePath)
                ? "index.html"
                : relativePath.Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(documentationRoot, normalized));
            if (!IsWithinRoot(documentationRoot, candidate)) return null;
            if (Directory.Exists(candidate)) candidate = Path.Combine(candidate, "index.html");
            return File.Exists(candidate) ? candidate : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            logger.LogWarning(exception, "Resolving a generated documentation HTML asset failed.");
            return null;
        }
    }

    /// <summary>
    /// Retrieves comment as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalGptDocumentationComment? GetComment(string memberId, string? culture = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(memberId)) return null;
            var comment = GetCommentCatalog().FirstOrDefault(item =>
                string.Equals(item.MemberId, memberId.Trim(), StringComparison.Ordinal));
            return comment is null ? null : translation.Adapt(comment, culture);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading XML documentation member {DocumentationMemberId} failed.", memberId);
            throw;
        }
    }

    /// <summary>
    /// Searches comments as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<LocalGptDocumentationComment> SearchComments(string? query, int limit = 100, string? culture = null)
    {
        try
        {
            var normalizedQuery = query?.Trim() ?? string.Empty;
            var boundedLimit = Math.Clamp(limit, 1, 500);
            return GetCommentCatalog()
                .Where(item => normalizedQuery.Length == 0 ||
                    item.MemberId.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                    item.Summary.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                    item.Remarks.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.MemberId, StringComparer.Ordinal)
                .Take(boundedLimit)
                .Select(item => translation.Adapt(item, culture))
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Searching the LocalGPT XML documentation catalog failed; query content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves comment catalog as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<LocalGptDocumentationComment> GetCommentCatalog()
    {
    try
    {
            var xmlDocumentationPath = ResolveXmlDocumentationPath(ResolveDocumentationRoot());
            if (xmlDocumentationPath is null) return [];
            var writeUtc = File.GetLastWriteTimeUtc(xmlDocumentationPath);
            lock (commentSync)
            {
                if (commentCache is not null &&
                    commentCacheWriteUtc == writeUtc &&
                    string.Equals(commentCachePath, xmlDocumentationPath, StringComparison.OrdinalIgnoreCase))
                    return commentCache;

                commentCache = LoadCommentCatalog(xmlDocumentationPath);
                commentCachePath = xmlDocumentationPath;
                commentCacheWriteUtc = writeUtc;
                return commentCache;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(GetCommentCatalog)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(GetCommentCatalog)} failed.");
        throw;
    }
}

    /// <summary>
    /// Loads comment catalog as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="xmlDocumentationPath">Xml documentation path value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<LocalGptDocumentationComment> LoadCommentCatalog(string xmlDocumentationPath)
    {
    try
    {
            var document = XDocument.Load(xmlDocumentationPath, LoadOptions.PreserveWhitespace);
            var assembly = typeof(DocumentationCatalogService).Assembly;
            return document
                .Descendants("member")
                .Select(member => BuildComment(member, assembly))
                .Where(comment => comment is not null)
                .Cast<LocalGptDocumentationComment>()
                .OrderBy(comment => comment.MemberId, StringComparer.Ordinal)
                .ToArray();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(LoadCommentCatalog)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(LoadCommentCatalog)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds comment as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="member">Member value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="assembly">Assembly value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The LocalGPT documentation comment produced by the operation.</returns>
    private LocalGptDocumentationComment? BuildComment(XElement member, Assembly assembly)
    {
    try
    {
            var memberId = member.Attribute("name")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(memberId)) return null;

            var summary = NormalizeCommentText(member.Element("summary")?.Value);
            var remarks = NormalizeCommentText(member.Element("remarks")?.Value);
            if (summary.Length == 0 && remarks.Length == 0) return null;

            var lastUpdatedVersion = ResolveDocumentationVersion(assembly, memberId);
            return new LocalGptDocumentationComment
            {
                MemberId = memberId,
                DisplayName = BuildDisplayName(memberId),
                Summary = summary,
                Remarks = remarks,
                Culture = "en-US",
                LastUpdatedVersion = lastUpdatedVersion,
                CurrentVersion = version.Version,
                IsCurrent = string.Equals(lastUpdatedVersion, version.Version, StringComparison.OrdinalIgnoreCase)
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(BuildComment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(BuildComment)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves documentation version as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="assembly">Assembly value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="memberId">Identifier of the member to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveDocumentationVersion(Assembly assembly, string memberId)
    {
    try
    {
            var declaringType = ResolveDeclaringType(assembly, memberId);
            return declaringType?.GetCustomAttribute<DocumentationUpdatedAttribute>(inherit: true)?.Version
                ?? "unversioned";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveDocumentationVersion)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveDocumentationVersion)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves declaring type as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="assembly">Assembly value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="memberId">Identifier of the member to use for this operation.</param>
    /// <returns>The type produced by the operation.</returns>
    private Type? ResolveDeclaringType(Assembly assembly, string memberId)
    {
    try
    {
            if (memberId.Length < 3 || memberId[1] != ':') return null;
            var identifier = memberId[2..];
            var parameterStart = identifier.IndexOf('(');
            if (parameterStart >= 0) identifier = identifier[..parameterStart];
            if (memberId[0] == 'T') return ResolveType(assembly, identifier);

            var separator = identifier.LastIndexOf('.');
            while (separator > 0)
            {
                var candidate = identifier[..separator];
                var type = ResolveType(assembly, candidate);
                if (type is not null) return type;
                separator = identifier.LastIndexOf('.', separator - 1);
            }
            return null;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveDeclaringType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveDeclaringType)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves type as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="assembly">Assembly value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="identifier">Identifier value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The type produced by the operation.</returns>
    private Type? ResolveType(Assembly assembly, string identifier)
    {
    try
    {
            var normalized = identifier.Replace('#', '.');
            var type = assembly.GetType(normalized, throwOnError: false, ignoreCase: false);
            if (type is not null) return type;

            var nestedCandidate = normalized;
            var separator = nestedCandidate.LastIndexOf('.');
            while (separator > 0)
            {
                nestedCandidate = nestedCandidate[..separator] + "+" + nestedCandidate[(separator + 1)..];
                type = assembly.GetType(nestedCandidate, throwOnError: false, ignoreCase: false);
                if (type is not null) return type;
                separator = nestedCandidate.LastIndexOf('.', separator - 1);
            }
            return null;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveType)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads build manifest as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The documentation build manifest produced by the operation.</returns>
    private DocumentationBuildManifest? ReadBuildManifest(string? documentationRoot)
    {
        if (documentationRoot is null) return null;
        var path = Path.Combine(documentationRoot, "documentation-status.json");
        if (!File.Exists(path)) return null;
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<DocumentationBuildManifest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(exception, "The generated LocalGPT documentation manifest could not be read.");
            return null;
        }
    }

    /// <summary>
    /// Resolves documentation root as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string? ResolveDocumentationRoot()
    {
    try
    {
            lock (documentationRootSync)
            {
                if (DateTime.UtcNow < documentationRootCacheExpiresUtc &&
                    (documentationRootCache is null || Directory.Exists(documentationRootCache)))
                    return documentationRootCache;

                var previous = documentationRootCache;
                var selected = EnumerateDocumentationRoots()
                    .Select(InspectDocumentationRoot)
                    .Where(candidate => candidate is not null)
                    .Cast<DocumentationRootCandidate>()
                    .OrderByDescending(candidate => candidate.IsCurrentVersion)
                    .ThenByDescending(candidate => candidate.ParsedVersion ?? new System.Version(0, 0))
                    .ThenByDescending(candidate => candidate.GeneratedAtUtc)
                    .ThenByDescending(candidate => candidate.LastWriteUtc)
                    .FirstOrDefault();

                documentationRootCache = selected?.Path;
                documentationRootCacheExpiresUtc = DateTime.UtcNow.AddSeconds(20);
                if (!string.Equals(previous, documentationRootCache, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation(
                        "Resolved LocalGPT documentation root to canonical shipped path {DocumentationRoot}.",
                        documentationRootCache ?? "not found");
                }
                return documentationRootCache;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveDocumentationRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveDocumentationRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs enumerate documentation roots as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<string> EnumerateDocumentationRoots()
    {
    try
    {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddDocumentationRoot(results, Path.Combine(environment.WebRootPath ?? string.Empty, "help-docs"));
            AddDocumentationRoot(results, Path.Combine(applicationRoot, "wwwroot", "help-docs"));
            AddDocumentationRoot(results, Path.Combine(environment.ContentRootPath, "wwwroot", "help-docs"));
            return results;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(EnumerateDocumentationRoots)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(EnumerateDocumentationRoots)} failed.");
        throw;
    }
}

    /// <summary>
    /// Adds documentation root as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="roots">String dependency used by the documentation catalog workflow to provide the corresponding application capability.</param>
    /// <param name="path">Path value supplied to the documentation catalog operation and used when producing its result.</param>
    private void AddDocumentationRoot(ISet<string> roots, string path)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath)) roots.Add(fullPath);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                _ = exception;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(AddDocumentationRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(AddDocumentationRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs inspect documentation root as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The documentation root candidate produced by the operation.</returns>
    private DocumentationRootCandidate? InspectDocumentationRoot(string path)
    {
        try
        {
            var indexPath = Path.Combine(path, "index.html");
            var manifestPath = Path.Combine(path, "documentation-status.json");
            var pdfFiles = EnumeratePdfFiles(path);
            if (!File.Exists(indexPath) && !File.Exists(manifestPath) && pdfFiles.Count == 0) return null;

            var manifest = ReadBuildManifest(path);
            var candidateVersion = manifest?.Version;
            if (string.IsNullOrWhiteSpace(candidateVersion))
            {
                candidateVersion = pdfFiles
                    .Select(file => Path.GetFileNameWithoutExtension(file))
                    .Where(name => name.StartsWith("LocalGPT-", StringComparison.OrdinalIgnoreCase))
                    .Select(name => name[9..])
                    .OrderByDescending(ParseVersionOrZero)
                    .FirstOrDefault();
            }

            var parsedVersion = ParseVersion(candidateVersion);
            var generatedAtUtc = manifest?.GeneratedAtUtc ?? DateTime.MinValue;
            var lastWriteUtc = new[] { indexPath, manifestPath }.Concat(pdfFiles)
                .Where(File.Exists)
                .Select(File.GetLastWriteTimeUtc)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
            return new DocumentationRootCandidate(
                path,
                parsedVersion,
                string.Equals(candidateVersion, version.Version, StringComparison.OrdinalIgnoreCase),
                generatedAtUtc,
                lastWriteUtc);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Skipping a generated documentation root that is temporarily unavailable.");
            return null;
        }
    }

    /// <summary>
    /// Performs enumerate PDF files as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> EnumeratePdfFiles(string documentationRoot)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<(string Path, int Depth)>();
        pending.Enqueue((documentationRoot, 0));
        var inspected = 0;
        while (pending.Count > 0 && inspected < 256)
        {
            var current = pending.Dequeue();
            inspected++;
            try
            {
                foreach (var file in Directory.GetFiles(current.Path, "LocalGPT-*.pdf", SearchOption.TopDirectoryOnly))
                {
                    var fullPath = Path.GetFullPath(file);
                    if (IsWithinRoot(documentationRoot, fullPath)) result.Add(fullPath);
                }
                if (current.Depth >= 4) continue;
                foreach (var child in Directory.GetDirectories(current.Path))
                {
                    var attributes = File.GetAttributes(child);
                    if ((attributes & FileAttributes.ReparsePoint) == 0)
                        pending.Enqueue((child, current.Depth + 1));
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(exception, "Skipping a documentation PDF search directory that is temporarily unavailable.");
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// Resolves installed PDF path as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="manifestVersion">Manifest version value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? ResolveInstalledPdfPath(string? documentationRoot, string? manifestVersion)
    {
        try
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (documentationRoot is not null)
                AddPdfFiles(files, documentationRoot);
            foreach (var root in EnumerateDocumentationRoots())
                AddPdfFiles(files, root);

            foreach (var requestedVersion in new[] { version.Version, manifestVersion }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var expectedName = $"LocalGPT-{requestedVersion}.pdf";
                var exact = files
                    .Where(file => string.Equals(Path.GetFileName(file), expectedName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
                if (exact is not null) return exact;
            }

            return files
                .Where(file => Path.GetFileNameWithoutExtension(file).StartsWith("LocalGPT-", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => ParseVersionOrZero(Path.GetFileNameWithoutExtension(file)[9..]))
                .ThenByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "The installed application roots could not be inspected for PDF files.");
            return null;
        }
    }

    /// <summary>
    /// Adds PDF files as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="files">String dependency used by the documentation catalog workflow to provide the corresponding application capability.</param>
    /// <param name="root">Root value supplied to the documentation catalog operation and used when producing its result.</param>
    private void AddPdfFiles(ISet<string> files, string root)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
            foreach (var file in EnumeratePdfFiles(root))
                files.Add(file);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(AddPdfFiles)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(AddPdfFiles)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves XML documentation path as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentationRoot">Documentation root value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? ResolveXmlDocumentationPath(string? documentationRoot)
    {
    try
    {
            var candidates = new[]
            {
                documentationRoot is null ? null : Path.Combine(documentationRoot, "LocalGPT.xml"),
                Path.Combine(applicationRoot, "LocalGPT.xml"),
                Path.Combine(environment.ContentRootPath, "LocalGPT.xml")
            };
            return candidates.Where(path => !string.IsNullOrWhiteSpace(path)).FirstOrDefault(path => File.Exists(path));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveXmlDocumentationPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ResolveXmlDocumentationPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether within root as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="candidate">Candidate value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsWithinRoot(string root, string candidate)
    {
    try
    {
            return platform.IsSameOrDescendantPath(root, candidate);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(IsWithinRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(IsWithinRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Parses version as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The system version produced by the operation.</returns>
    private System.Version? ParseVersion(string? value)
        {
    try
    {
        return System.Version.TryParse(value, out var parsed) ? parsed : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ParseVersion)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ParseVersion)} failed.");
        throw;
    }
}

    /// <summary>
    /// Parses version or zero as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The system version produced by the operation.</returns>
    private System.Version ParseVersionOrZero(string? value)
        {
    try
    {
        return ParseVersion(value) ?? new System.Version(0, 0);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ParseVersionOrZero)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(ParseVersionOrZero)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds display name as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="memberId">Identifier of the member to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildDisplayName(string memberId)
    {
    try
    {
            var value = memberId.Length > 2 ? memberId[2..] : memberId;
            return value.Replace('#', '.');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(BuildDisplayName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(BuildDisplayName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes comment text as part of the documentation catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeCommentText(string? value) {
    try
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Regex.Replace(value, @"\s+", " ").Trim();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(NormalizeCommentText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationCatalogService)}.{nameof(NormalizeCommentText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Represents a documentation build manifest helper type nested within <see cref="DocumentationCatalogService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class DocumentationBuildManifest
    {
        /// <summary>
        /// Gets or sets the version value that forms part of the documentation build manifest state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The version value exposed by <see cref="DocumentationBuildManifest"/>.</value>
        public string Version { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the generated at UTC associated with this documentation build manifest state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The generated at UTC value exposed by <see cref="DocumentationBuildManifest"/>.</value>
        public DateTime? GeneratedAtUtc { get; set; }
    }

    /// <summary>
    /// Represents a documentation root candidate helper type nested within <see cref="DocumentationCatalogService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Path">Path value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="ParsedVersion">Parsed version value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="IsCurrentVersion">Value indicating whether current version should apply to this operation.</param>
    /// <param name="GeneratedAtUtc">Generated at utc value supplied to the documentation catalog operation and used when producing its result.</param>
    /// <param name="LastWriteUtc">Last write utc value supplied to the documentation catalog operation and used when producing its result.</param>
    private sealed record DocumentationRootCandidate(
        string Path,
        System.Version? ParsedVersion,
        bool IsCurrentVersion,
        DateTime? GeneratedAtUtc,
        DateTime LastWriteUtc);
}
