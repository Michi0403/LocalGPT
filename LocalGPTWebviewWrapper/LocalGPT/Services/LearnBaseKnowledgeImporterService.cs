using DevExpress.XtraGauges.Core.Model;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;
using static System.Net.WebRequestMethods;

namespace LocalGPT.Services
{
    public sealed partial class LearnBaseKnowledgeImporterService(
        ICouncilKnowledgeService knowledgeService,
        ILogger<LearnBaseKnowledgeImporterService> logger) : ILearnBaseKnowledgeImporterService
    {


        public async Task<LearnBaseImportResult?> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rootPath = string.IsNullOrWhiteSpace(request.RootPath)
              ? @"C:\learnbaseforlocalgpt"
              : request.RootPath.Trim();
                var result = new LearnBaseImportResult
                {
                    RootPath = rootPath,
                    ImportMode = "Compact source-map import; stores architecture fingerprints and documentation corpus summaries, not full file contents.",
                    FilePolicy = CouncilChatStringFunctions.BuildFilePolicySummary(logger),
                    DuplicatePolicy = "Knowledge entries use stable GUIDs derived from source path and corpus section. Re-importing the same folder updates the same row instead of creating duplicate rows."
                };

                if (!Directory.Exists(rootPath))
                {
                    result.Warnings.Add($"Learn-base root was not found: {rootPath}");
                    return result;
                }

                await knowledgeService.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                await ImportKnownDocumentationCorporaAsync(rootPath, request, result, cancellationToken).ConfigureAwait(false);

                var projectDirectories = CouncilChatStringFunctions.BuildImportDirectories(rootPath, Math.Clamp(request.MaxProjects, 1, 120), logger)
                    .ToArray();

                foreach (var projectDirectory in projectDirectories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var summary = CouncilChatStaticsGeneral.BuildProjectSummary(rootPath, projectDirectory, logger);
                        if (request.SaveToKnowledge)
                        {
                            var entry = await knowledgeService.SaveEntryAsync(CouncilChatStaticsGeneral.ToKnowledgeEntry(summary,logger), cancellationToken).ConfigureAwait(false);
                            summary.KnowledgeEntryId = entry.Id;
                            result.SavedKnowledgeCount++;
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ImportAsync request {request?.ToString()}");
                return null;
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
                foreach (var candidate in CouncilChatStaticsGeneral.BuildDocumentationCorpusCandidates(rootPath,logger))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (CouncilChatStaticsGeneral.LooksLikeWindowsDevDocsRoot(candidate, logger))
                        await ImportWindowsDevDocsCorpusAsync(candidate, request, result, cancellationToken).ConfigureAwait(false);

                    if (CouncilChatStaticsGeneral.LooksLikeDotNetDocsRoot(candidate, logger))
                        await ImportDotNetDocsCorpusAsync(candidate, request, result, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ImportKnownDocumentationCorporaAsync rootPath {rootPath?.ToString()} request {request?.ToString()} result {result?.ToString()}");

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
                var markdownFiles = CouncilChatStaticsGeneral.EnumerateUsefulFiles(rootPath,logger)
                .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                .Take(8000)
                .ToArray();
                if (markdownFiles.Length == 0)
                    return;

                foreach (var entry in CouncilChatStaticsGeneral.BuildDotNetDocsEntries(rootPath, markdownFiles, logger))
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
                logger.LogError(ex, $"Error in ImportDotNetDocsCorpusAsync rootPath {rootPath?.ToString()} request {request?.ToString()} result {result?.ToString()}");

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
                var markdownFiles = CouncilChatStaticsGeneral.EnumerateUsefulFiles(rootPath,logger)
                .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                .Take(6000)
                .ToArray();
                if (markdownFiles.Length == 0)
                    return;

                foreach (var entry in CouncilChatStaticsGeneral.BuildWindowsDevDocsEntries(rootPath, markdownFiles, logger))
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
                logger.LogError(ex, $"Error in ImportWindowsDevDocsCorpusAsync rootPath {rootPath?.ToString()} request {request?.ToString()} result {result?.ToString()}");
          
            }
        }
    }
}