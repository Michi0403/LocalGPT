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
[DocumentationUpdated("2.2.8")]
public sealed class DocumentationCatalogService(
    IWebHostEnvironment environment,
    ICustomVersion version,
    IDocumentationTranslationAdapter translation,
    ILogger<DocumentationCatalogService> logger) : IDocumentationCatalogService
{
    private readonly object commentSync = new();
    private readonly object documentationRootSync = new();
    private readonly string applicationRoot = Path.GetFullPath(AppContext.BaseDirectory);
    private IReadOnlyList<LocalGptDocumentationComment>? commentCache;
    private string? commentCachePath;
    private DateTime commentCacheWriteUtc;
    private string? documentationRootCache;
    private DateTime documentationRootCacheExpiresUtc;

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

    private IReadOnlyList<LocalGptDocumentationComment> GetCommentCatalog()
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

    private IReadOnlyList<LocalGptDocumentationComment> LoadCommentCatalog(string xmlDocumentationPath)
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

    private LocalGptDocumentationComment? BuildComment(XElement member, Assembly assembly)
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

    private string ResolveDocumentationVersion(Assembly assembly, string memberId)
    {
        var declaringType = ResolveDeclaringType(assembly, memberId);
        return declaringType?.GetCustomAttribute<DocumentationUpdatedAttribute>(inherit: true)?.Version
            ?? "unversioned";
    }

    private Type? ResolveDeclaringType(Assembly assembly, string memberId)
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

    private Type? ResolveType(Assembly assembly, string identifier)
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

    private string? ResolveDocumentationRoot()
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
                    "Resolved LocalGPT documentation root to {DocumentationRoot}; recursive application search inspected generated versions below {ApplicationRoot}.",
                    documentationRootCache ?? "not found",
                    applicationRoot);
            }
            return documentationRootCache;
        }
    }

    private IEnumerable<string> EnumerateDocumentationRoots()
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddDocumentationRoot(results, Path.Combine(environment.WebRootPath ?? string.Empty, "help-docs"));
        AddDocumentationRoot(results, Path.Combine(applicationRoot, "wwwroot", "help-docs"));
        AddDocumentationRoot(results, Path.Combine(environment.ContentRootPath, "wwwroot", "help-docs"));

        foreach (var searchRoot in new[] { applicationRoot, environment.ContentRootPath }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(searchRoot)) continue;
            var pending = new Queue<(string Path, int Depth)>();
            pending.Enqueue((searchRoot, 0));
            var inspected = 0;
            while (pending.Count > 0 && inspected < 4096)
            {
                var current = pending.Dequeue();
                inspected++;
                if (current.Depth >= 8) continue;

                string[] children;
                try { children = Directory.GetDirectories(current.Path); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    logger.LogDebug(exception, "Skipping an inaccessible documentation search directory.");
                    continue;
                }

                foreach (var child in children)
                {
                    try
                    {
                        var attributes = File.GetAttributes(child);
                        if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        logger.LogDebug(exception, "Skipping an inaccessible documentation search candidate.");
                        continue;
                    }

                    if (string.Equals(Path.GetFileName(child), "help-docs", StringComparison.OrdinalIgnoreCase))
                    {
                        AddDocumentationRoot(results, child);
                        continue;
                    }
                    pending.Enqueue((child, current.Depth + 1));
                }
            }
        }

        return results;
    }

    private void AddDocumentationRoot(ISet<string> roots, string path)
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

    private string? ResolveInstalledPdfPath(string? documentationRoot, string? manifestVersion)
    {
        try
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (documentationRoot is not null)
                AddPdfFiles(files, documentationRoot);
            foreach (var root in EnumerateDocumentationRoots())
                AddPdfFiles(files, root);

            // Installed desktop layouts may place the runtime in a version/RID directory while the
            // generated help payload is below a sibling directory. Search both trusted application
            // roots as a final bounded fallback instead of tying PDF availability to one HTML root.
            AddPdfFiles(files, applicationRoot);
            AddPdfFiles(files, environment.ContentRootPath);

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

    private void AddPdfFiles(ISet<string> files, string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
        foreach (var file in EnumeratePdfFiles(root))
            files.Add(file);
    }

    private string? ResolveXmlDocumentationPath(string? documentationRoot)
    {
        var candidates = new[]
        {
            documentationRoot is null ? null : Path.Combine(documentationRoot, "LocalGPT.xml"),
            Path.Combine(applicationRoot, "LocalGPT.xml"),
            Path.Combine(environment.ContentRootPath, "LocalGPT.xml")
        };
        return candidates.Where(path => !string.IsNullOrWhiteSpace(path)).FirstOrDefault(path => File.Exists(path));
    }

    private bool IsWithinRoot(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedCandidate = Path.GetFullPath(candidate);
        return string.Equals(normalizedRoot, normalizedCandidate, StringComparison.OrdinalIgnoreCase) ||
            normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private System.Version? ParseVersion(string? value)
        => System.Version.TryParse(value, out var parsed) ? parsed : null;

    private System.Version ParseVersionOrZero(string? value)
        => ParseVersion(value) ?? new System.Version(0, 0);

    private string BuildDisplayName(string memberId)
    {
        var value = memberId.Length > 2 ? memberId[2..] : memberId;
        return value.Replace('#', '.');
    }

    private string NormalizeCommentText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Regex.Replace(value, @"\s+", " ").Trim();

    private sealed class DocumentationBuildManifest
    {
        public string Version { get; set; } = string.Empty;
        public DateTime? GeneratedAtUtc { get; set; }
    }

    private sealed record DocumentationRootCandidate(
        string Path,
        System.Version? ParsedVersion,
        bool IsCurrentVersion,
        DateTime? GeneratedAtUtc,
        DateTime LastWriteUtc);
}
