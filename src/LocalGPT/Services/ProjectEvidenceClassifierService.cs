using System.IO.Compression;
using System.Xml.Linq;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Classifies bounded repository evidence from reviewed data rules and authoritative repository markers without executing uploaded content.</summary>
public sealed class ProjectEvidenceClassifierService(
    IRegexCuratorService regexCurator,
    IRegexPatternService regexPatterns,
    CouncilTextService councilText,
    LocalGptCatalogService catalog,
    ILogger<ProjectEvidenceClassifierService> logger) : IProjectEvidenceClassifierService
{
    private const string EvidenceRulePrefix = "project.evidence::";
    private const int MaximumProjectMarkerBytes = 256_000;

    /// <inheritdoc />
    public async Task<ProjectEvidenceClassification> ClassifyAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new ProjectEvidenceClassification();
            if (!Directory.Exists(rootPath))
                return result;

            var relativePaths = CollectEvidencePaths(rootPath);
            if (relativePaths.Count == 0)
                return result;

            var approved = await regexCurator.ListApprovedPatternsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var pattern in approved.Where(item => item.Name.StartsWith(EvidenceRulePrefix, StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var metadata = ParseRuleName(pattern.Name);
                if (metadata is null)
                    continue;
                var compiled = regexPatterns.Compile(pattern.Pattern, pattern.Flags, TimeSpan.FromSeconds(2));
                if (!relativePaths.Any(compiled.IsMatch))
                    continue;

                AddUnique(result.Domains, metadata.Value.Domain);
                AddUnique(result.ProjectKinds, metadata.Value.ProjectKind);
                AddUnique(result.Toolchains, metadata.Value.Toolchain);
                AddUnique(result.MatchedRuleNames, pattern.Name);
            }

            foreach (var repository in DetectRepositoryIdentities(rootPath, cancellationToken))
            {
                result.Repositories.Add(repository);
                AddUnique(result.Domains, "SourceCode");
                AddUnique(result.ProjectKinds, repository.Kind);
                if (repository.Name.Equals("LocalGPT", StringComparison.OrdinalIgnoreCase))
                    AddUnique(result.ProjectKinds, "LocalGPTRepository");
                else if (repository.Name.Equals("PublisherStudio", StringComparison.OrdinalIgnoreCase))
                    AddUnique(result.ProjectKinds, "PublisherStudioRepository");
                if (repository.GitMetadataDetected)
                    AddUnique(result.Toolchains, "Git");
                if (repository.Markers.Any(marker => marker.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
                    AddUnique(result.Toolchains, "DotNet");
            }

            logger.LogInformation(
                "Classified repository evidence from {RuleCount} reviewed data rule(s) and {RepositoryCount} bounded repository identity marker set(s): {DomainCount} domain(s), {ProjectKindCount} project kind(s), {ToolchainCount} toolchain(s).",
                result.MatchedRuleNames.Count,
                result.Repositories.Count,
                result.Domains.Count,
                result.ProjectKinds.Count,
                result.Toolchains.Count);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classifying repository evidence failed; source content was not executed.");
            throw;
        }
    }

    private List<string> CollectEvidencePaths(string rootPath)
    {
        try
        {
            var values = new List<string>();
            foreach (var path in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                         .Where(path => !IsExcluded(path, rootPath))
                         .Take(Math.Max(1, catalog.MaxFiles)))
            {
                var relative = councilText.ToForwardSlash(Path.GetRelativePath(rootPath, path), logger);
                AddUnique(values, relative);
                if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    using var archive = ZipFile.OpenRead(path);
                    foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrWhiteSpace(entry.Name)).Take(Math.Max(1, catalog.MaxZipEntries)))
                    {
                        var safeEntry = councilText.BuildSafeZipRelativePath(entry.FullName, logger);
                        if (!string.IsNullOrWhiteSpace(safeEntry))
                            AddUnique(values, $"{relative}!/{councilText.ToForwardSlash(safeEntry, logger)}");
                    }
                }
                catch (InvalidDataException ex)
                {
                    logger.LogDebug(ex, "Project-evidence classifier could not inspect one ZIP entry list; the ingestion gate reports archive validity separately.");
                }
            }
            return values;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Collecting bounded repository evidence paths failed.");
            throw;
        }
    }

    private IReadOnlyList<RepositoryEvidenceIdentity> DetectRepositoryIdentities(string rootPath, CancellationToken cancellationToken)
    {
        try
        {
            var repositories = new List<RepositoryEvidenceIdentity>();
            var directFiles = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Take(Math.Max(1, catalog.MaxFiles))
                .ToList();

            var directRelative = directFiles.ToDictionary(
                path => councilText.ToForwardSlash(Path.GetRelativePath(rootPath, path), logger),
                path => path,
                StringComparer.OrdinalIgnoreCase);
            var directIdentity = BuildIdentity(
                directRelative.Keys,
                marker => directRelative.TryGetValue(marker, out var path) ? ReadProjectVersion(path) : string.Empty,
                "workspace");
            if (directIdentity is not null)
                repositories.Add(directIdentity);

            foreach (var zipPath in directFiles.Where(path => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var archive = ZipFile.OpenRead(zipPath);
                    var entries = archive.Entries
                        .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                        .Take(Math.Max(1, catalog.MaxZipEntries))
                        .ToList();
                    var normalized = entries
                        .Select(entry => (Entry: entry, Path: NormalizeArchivePath(entry.FullName)))
                        .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                        .ToList();
                    var entryMap = normalized
                        .GroupBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First().Entry, StringComparer.OrdinalIgnoreCase);
                    var relativeZip = councilText.ToForwardSlash(Path.GetRelativePath(rootPath, zipPath), logger);
                    var identity = BuildIdentity(
                        entryMap.Keys,
                        marker => entryMap.TryGetValue(marker, out var entry) ? ReadProjectVersion(entry) : string.Empty,
                        relativeZip);
                    if (identity is not null)
                        repositories.Add(identity);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogDebug(ex, "Repository identity inspection skipped an invalid ZIP; quarantine validation owns the rejection.");
                }
            }

            return repositories
                .GroupBy(item => $"{item.RootHint}|{item.Name}|{item.Version}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(40)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Detecting bounded repository identity markers failed.");
            throw;
        }
    }

    private RepositoryEvidenceIdentity? BuildIdentity(IEnumerable<string> paths, Func<string, string> readVersion, string sourceHint)
    {
        try
        {
            var values = paths.Select(NormalizeArchivePath).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (values.Count == 0)
                return null;

            var localGptMarker = FindMarker(values, "src/LocalGPT/LocalGPT.csproj");
            var publisherMarker = FindMarker(values, "src/PublisherStudio.Web/PublisherStudio.Web.csproj")
                ?? FindMarker(values, "src/PublisherStudio/PublisherStudio.csproj");
            var gitConfig = values.FirstOrDefault(value => value.Equals(".git/config", StringComparison.OrdinalIgnoreCase) || value.EndsWith("/.git/config", StringComparison.OrdinalIgnoreCase));
            var projectMarker = values.FirstOrDefault(value => value.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                                                               || value.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                                                               || value.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                                               || value.EndsWith("/package.json", StringComparison.OrdinalIgnoreCase)
                                                               || value.Equals("package.json", StringComparison.OrdinalIgnoreCase)
                                                               || value.EndsWith("/pyproject.toml", StringComparison.OrdinalIgnoreCase)
                                                               || value.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase)
                                                               || value.EndsWith("/Cargo.toml", StringComparison.OrdinalIgnoreCase)
                                                               || value.Equals("Cargo.toml", StringComparison.OrdinalIgnoreCase)
                                                               || value.EndsWith("/go.mod", StringComparison.OrdinalIgnoreCase)
                                                               || value.Equals("go.mod", StringComparison.OrdinalIgnoreCase));

            var preferred = localGptMarker ?? publisherMarker ?? projectMarker ?? gitConfig;
            if (preferred is null)
                return null;

            var name = localGptMarker is not null ? "LocalGPT"
                : publisherMarker is not null ? "PublisherStudio"
                : projectMarker is not null ? RepositoryNameFromMarker(projectMarker)
                : "Git repository";
            var kind = localGptMarker is not null ? "LocalGPTRepository"
                : publisherMarker is not null ? "PublisherStudioRepository"
                : gitConfig is not null ? "GitRepository"
                : "SourceRepository";
            var markers = new List<string>();
            if (localGptMarker is not null) markers.Add(localGptMarker);
            if (publisherMarker is not null) markers.Add(publisherMarker);
            if (projectMarker is not null && !markers.Contains(projectMarker, StringComparer.OrdinalIgnoreCase)) markers.Add(projectMarker);
            if (gitConfig is not null) markers.Add(gitConfig);

            var versionMarker = localGptMarker ?? publisherMarker;
            var version = versionMarker is null ? string.Empty : readVersion(versionMarker);
            return new RepositoryEvidenceIdentity
            {
                Kind = kind,
                Name = name,
                Version = version,
                RootHint = BuildRootHint(sourceHint, preferred),
                GitMetadataDetected = gitConfig is not null,
                Markers = markers.Take(8).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building one bounded repository identity failed.");
            throw;
        }
    }

    private string? FindMarker(IReadOnlyList<string> paths, string suffix)
    {
        try
        {
            return paths.FirstOrDefault(value => value.Equals(suffix, StringComparison.OrdinalIgnoreCase) || value.EndsWith("/" + suffix, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Finding a bounded repository marker failed.");
            throw;
        }
    }

    private string RepositoryNameFromMarker(string marker)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(marker);
            return string.IsNullOrWhiteSpace(fileName) || fileName.Equals("package", StringComparison.OrdinalIgnoreCase) || fileName.Equals("pyproject", StringComparison.OrdinalIgnoreCase)
                ? "Source repository"
                : fileName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resolving repository display name from a marker failed.");
            throw;
        }
    }

    private string BuildRootHint(string sourceHint, string marker)
    {
        try
        {
            var markerRoot = marker;
            var knownSuffixes = new[] { "src/LocalGPT/LocalGPT.csproj", "src/PublisherStudio.Web/PublisherStudio.Web.csproj", "src/PublisherStudio/PublisherStudio.csproj", ".git/config" };
            var strippedKnownSuffix = false;
            foreach (var suffix in knownSuffixes)
            {
                if (!marker.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    continue;
                markerRoot = marker[..Math.Max(0, marker.Length - suffix.Length)].TrimEnd('/');
                strippedKnownSuffix = true;
                break;
            }
            if (!strippedKnownSuffix)
            {
                var separator = marker.LastIndexOf('/');
                markerRoot = separator <= 0 ? string.Empty : marker[..separator].TrimEnd('/');
            }
            return string.IsNullOrWhiteSpace(markerRoot) ? sourceHint : $"{sourceHint}!/{markerRoot}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building repository root hint failed.");
            throw;
        }
    }

    private string ReadProjectVersion(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumProjectMarkerBytes)
                return string.Empty;
            using var stream = File.OpenRead(path);
            return ReadVersionXml(stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            logger.LogDebug(ex, "Could not read a bounded project version marker.");
            return string.Empty;
        }
    }

    private string ReadProjectVersion(ZipArchiveEntry entry)
    {
        try
        {
            if (entry.Length <= 0 || entry.Length > MaximumProjectMarkerBytes)
                return string.Empty;
            using var stream = entry.Open();
            return ReadVersionXml(stream);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Xml.XmlException)
        {
            logger.LogDebug(ex, "Could not read a bounded project version marker from an archive.");
            return string.Empty;
        }
    }

    private string ReadVersionXml(Stream stream)
    {
        try
        {
            var document = XDocument.Load(stream, LoadOptions.None);
            return document.Descendants().FirstOrDefault(element => element.Name.LocalName.Equals("Version", StringComparison.OrdinalIgnoreCase))?.Value.Trim() ?? string.Empty;
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException)
        {
            logger.LogDebug(ex, "Project version XML could not be parsed.");
            return string.Empty;
        }
    }

    private string NormalizeArchivePath(string value)
    {
        try
        {
            return councilText.ToForwardSlash(value, logger).TrimStart('/');
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Normalizing repository marker path failed.");
            throw;
        }
    }

    private (string Domain, string ProjectKind, string Toolchain)? ParseRuleName(string name)
    {
        try
        {
            var parts = name.Split("::", StringSplitOptions.TrimEntries);
            if (parts.Length != 4 || !parts[0].Equals("project.evidence", StringComparison.OrdinalIgnoreCase))
                return null;
            return (parts[1], parts[2], parts[3]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parsing project evidence rule metadata failed.");
            throw;
        }
    }

    private bool IsExcluded(string path, string root)
    {
        try
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            return relative.Split('/').Any(segment => segment is ".git" or ".vs" or "bin" or "obj" or "node_modules" or "target" or ".gradle");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Checking project-classifier excluded path failed.");
            throw;
        }
    }

    private void AddUnique(List<string> values, string value)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, StringComparer.OrdinalIgnoreCase))
                values.Add(value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Adding unique project-evidence classification value failed.");
            throw;
        }
    }
}
