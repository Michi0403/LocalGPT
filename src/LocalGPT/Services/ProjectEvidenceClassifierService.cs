using System.IO.Compression;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Classifies bounded repository evidence from curator-approved database regex rules; source code never needs a file-type switch.</summary>
public sealed class ProjectEvidenceClassifierService(
    IRegexCuratorService regexCurator,
    IRegexPatternService regexPatterns,
    CouncilTextService councilText,
    LocalGptCatalogService catalog,
    ILogger<ProjectEvidenceClassifierService> logger) : IProjectEvidenceClassifierService
{
    private const string EvidenceRulePrefix = "project.evidence::";

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

            logger.LogInformation(
                "Classified repository evidence from {RuleCount} curator-approved data rule(s): {DomainCount} domain(s), {ProjectKindCount} project kind(s), {ToolchainCount} toolchain(s).",
                result.MatchedRuleNames.Count,
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
            logger.LogError(ex, "Classifying repository evidence from curator-approved data rules failed; source content was not executed.");
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
                    logger.LogDebug(ex, "Project-evidence classifier could not inspect one ZIP entry list; the ingestion gate will report archive validity separately.");
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
