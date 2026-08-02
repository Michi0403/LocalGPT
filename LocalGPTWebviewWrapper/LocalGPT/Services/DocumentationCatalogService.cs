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
[DocumentationUpdated("2.1.19")]
public sealed class DocumentationCatalogService(
    IWebHostEnvironment environment,
    ICustomVersion version,
    IDocumentationTranslationAdapter translation,
    ILogger<DocumentationCatalogService> logger) : IDocumentationCatalogService
{
    private readonly object commentSync = new();
    private readonly string documentationRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "help-docs"));
    private readonly string xmlDocumentationPath = File.Exists(Path.Combine(AppContext.BaseDirectory, "LocalGPT.xml"))
        ? Path.Combine(AppContext.BaseDirectory, "LocalGPT.xml")
        : Path.Combine(environment.ContentRootPath, "LocalGPT.xml");
    private IReadOnlyList<LocalGptDocumentationComment>? commentCache;
    private DateTime commentCacheWriteUtc;

    /// <inheritdoc />
    public LocalGptDocumentationStatus GetStatus()
    {
        try
        {
            var pdfFileName = $"LocalGPT-{version.Version}.pdf";
            var manifest = ReadBuildManifest();
            var comments = GetCommentCatalog();
            return new LocalGptDocumentationStatus
            {
                Version = version.Version,
                InspectedAtUtc = DateTime.UtcNow,
                GeneratedAtUtc = manifest?.GeneratedAtUtc,
                HtmlAvailable = File.Exists(Path.Combine(documentationRoot, "index.html")),
                PdfAvailable = File.Exists(Path.Combine(documentationRoot, pdfFileName)),
                XmlCommentsAvailable = File.Exists(xmlDocumentationPath),
                CommentCount = comments.Count,
                HtmlUrl = "/help-docs/index.html",
                PdfUrl = "/api/documentation/pdf",
                CommentsUrl = "/api/documentation/comments",
                PdfFileName = pdfFileName
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
            var candidate = Path.GetFullPath(Path.Combine(documentationRoot, $"LocalGPT-{version.Version}.pdf"));
            if (!candidate.StartsWith(documentationRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return null;
            return File.Exists(candidate) ? candidate : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the generated LocalGPT PDF path failed.");
            throw;
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
        if (!File.Exists(xmlDocumentationPath)) return [];
        var writeUtc = File.GetLastWriteTimeUtc(xmlDocumentationPath);
        lock (commentSync)
        {
            if (commentCache is not null && commentCacheWriteUtc == writeUtc)
                return commentCache;

            commentCache = LoadCommentCatalog();
            commentCacheWriteUtc = writeUtc;
            return commentCache;
        }
    }

    private IReadOnlyList<LocalGptDocumentationComment> LoadCommentCatalog()
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

    private DocumentationBuildManifest? ReadBuildManifest()
    {
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
        public DateTime? GeneratedAtUtc { get; set; }
    }
}
