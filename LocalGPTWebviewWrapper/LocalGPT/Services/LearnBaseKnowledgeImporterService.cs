using DevExpress.XtraGauges.Core.Model;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    public sealed partial class LearnBaseKnowledgeImporterService(
        ICouncilKnowledgeService knowledgeService,
        ILogger<LearnBaseKnowledgeImporterService> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        LocalGptCatalogService catalog) : ILearnBaseKnowledgeImporterService
    {


        public async Task<LearnBaseImportResult> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var defaultPreset = catalog.LearnBasePresets.FirstOrDefault();
                var rootPath = string.IsNullOrWhiteSpace(request.RootPath)
                    ? defaultPreset?.RootPath ?? @"C:\learnbaseforlocalgpt"
                    : request.RootPath.Trim();
                var result = new LearnBaseImportResult
                {
                    RootPath = rootPath,
                    ImportMode = "Compact source-map import; stores architecture fingerprints and documentation corpus summaries, not full file contents.",
                    FilePolicy = catalog.LearnBaseFilePolicySummary,
                    DuplicatePolicy = catalog.LearnBaseDuplicatePolicySummary
                };

                if (!Directory.Exists(rootPath))
                {
                    result.Warnings.Add($"Learn-base root was not found: {rootPath}");
                    return result;
                }

                await knowledgeService.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                await ImportKnownDocumentationCorporaAsync(rootPath, request, result, cancellationToken).ConfigureAwait(false);

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
                        var summary = councilRuntime.BuildProjectSummary(rootPath, projectDirectory, logger);
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

        private async Task ImportKnownDocumentationCorporaAsync(
            string rootPath,
            LearnBaseImportRequest request,
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var candidate in councilRuntime.BuildDocumentationCorpusCandidates(rootPath,logger))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (councilRuntime.LooksLikeWindowsDevDocsRoot(candidate, logger))
                        await ImportWindowsDevDocsCorpusAsync(candidate, request, result, cancellationToken).ConfigureAwait(false);

                    if (councilRuntime.LooksLikeDotNetDocsRoot(candidate, logger))
                        await ImportDotNetDocsCorpusAsync(candidate, request, result, cancellationToken).ConfigureAwait(false);
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
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var markdownFiles = councilRuntime.EnumerateUsefulFiles(rootPath,logger)
                .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                .Take(8000)
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
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var markdownFiles = councilRuntime.EnumerateUsefulFiles(rootPath,logger)
                .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                .Take(6000)
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
    }
}
