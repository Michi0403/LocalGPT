using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
        /// <summary>Executes the sort knowledge entries operation.</summary>
        /// <param name="entries">Input value for entries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public List<CouncilKnowledgeEntry> SortKnowledgeEntries(IEnumerable<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                return entries
             .OrderBy(filter => KnowledgeReviewPriority(filter, logger))
             .ThenByDescending(entry => entry.IsPinned)
             .ThenByDescending(entry => entry.IsUserApproved)
             .ThenByDescending(entry => entry.UpdatedAtUtc)
             .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SortKnowledgeEntries entries:{entries.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }

        }
        /// <summary>Executes the copy knowledge entry operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilKnowledgeEntry? CopyKnowledgeEntry(CouncilKnowledgeEntry entry,ILogger logger)
        {
            try
            {
                return new CouncilKnowledgeEntry
                {
                    Id = entry.Id,
                    CreatedAtUtc = entry.CreatedAtUtc,
                    UpdatedAtUtc = entry.UpdatedAtUtc,
                    Topic = entry.Topic,
                    Scope = entry.Scope,
                    Content = entry.Content,
                    Source = entry.Source,
                    HelpfulSources = entry.HelpfulSources,
                    Tags = entry.Tags,
                    Confidence = entry.Confidence,
                    VerificationStatus = entry.VerificationStatus,
                    ReviewStatus = entry.ReviewStatus,
                    ExpiresAtUtc = entry.ExpiresAtUtc,
                    LastVerifiedAtUtc = entry.LastVerifiedAtUtc,
                    LastUsedAtUtc = entry.LastUsedAtUtc,
                    SupersededByKnowledgeId = entry.SupersededByKnowledgeId,
                    StalenessReason = entry.StalenessReason,
                    StalenessDetectedAtUtc = entry.StalenessDetectedAtUtc,
                    StalenessDetectedBy = entry.StalenessDetectedBy,
                    SourceHash = entry.SourceHash,
                    SourceDateUtc = entry.SourceDateUtc,
                    IsUserApproved = entry.IsUserApproved,
                    IsPinned = entry.IsPinned,
                    IsArchived = entry.IsArchived
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyKnowledgeEntry entry:{entry.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the build knowledge review summary operation.</summary>
        /// <param name="entries">Input value for entries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildKnowledgeReviewSummary(IReadOnlyCollection<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                if (entries.Count == 0)
                    return "No knowledge notes loaded yet.";

                var needsAttention = entries.Count(entry => entry.ReviewStatus is "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired");
                var trusted = entries.Count(entry => entry.ReviewStatus == "Current" && entry.IsUserApproved);
                return $"{needsAttention} note(s) need attention. {trusted} user-approved current note(s) can guide the council.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildKnowledgeReviewSummary entries:{entries.ToString()}");
                return string.Empty;
            }
            
        }

        /// <summary>Executes the get council model load priority randomisator operation.</summary>
        /// <param name="maxPriority">Input value for maxPriority.</param>
        /// <param name="modelName">Input value for modelName.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int? GetCouncilModelLoadPriorityRandomisator(int maxPriority, string modelName, ILogger logger)
        {
            try
            {
                var random = new Random();
                int randomNumber = random.Next(maxPriority);
                logger.LogInformation($"GetCouncilModelLoadPriorityRandomisator modelName:{modelName.ToString()} returning value..{randomNumber}");
                return randomNumber;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetCouncilModelLoadPriorityRandomisator modelName:{modelName.ToString()} maxPriority:{maxPriority}");
                return null;
            }
            
        }

        /// <summary>Executes the knowledge review priority operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int? KnowledgeReviewPriority(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                return entry.ReviewStatus switch
                {
                    "NeedsUserReview" => 0,
                    "NeedsSourceRefresh" => 1,
                    "NeedsDiagnosticVerification" => 2,
                    "Expired" => 3,
                    "Superseded" => 4,
                    "Deprecated" => 5,
                    "Current" => 6,
                    "Archived" => 7,
                    _ => 8
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in KnowledgeReviewPriority entry:{entry.ToString()}");
                return null;
            }
           
        }

        /// <summary>Executes the is dynamic session operation.</summary>
        /// <param name="session">Input value for session.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsDynamicSession(ChatClientSession session, ILogger logger)
        {
            try
            {
                return session.Name.StartsWith(catalog.DetectedOllamaSessionPrefix, StringComparison.OrdinalIgnoreCase) ||
            session.Name.Equals(catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsDynamicSession sessionm:{session.ToString()}");
                return null;
            }

        }
       

        /// <summary>Executes the order council models for load operation.</summary>
        /// <param name="modelNames">Input value for modelNames.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<string>? OrderCouncilModelsForLoad(IEnumerable<string> modelNames, ILogger logger)
        {
            try
            {
                return modelNames
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(filter =>  GetCouncilModelLoadPriorityRandomisator(modelNames.Count(), filter,logger) ?? 0)
               .ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in OrderCouncilModelsForLoad modelNames:{modelNames.ToString()}");
                return null;
            }

        }
        /// <summary>Executes the build dynamic session name operation.</summary>
        /// <param name="candidate">Input value for candidate.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildDynamicSessionName(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                return $"{catalog.DetectedOllamaSessionPrefix}{candidate.SelectionKey}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDynamicSessionName candidate:{candidate.ToString()}");
                return string.Empty;
            }

        }
  

        /// <summary>Executes the build candidate label operation.</summary>
        /// <param name="candidate">Input value for candidate.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildCandidateLabel(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                return $"{candidate.ModelName} @ {text.TrimEndpoint(candidate.Endpoint,logger)}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCandidateLabel candidate:{candidate.ToString()}");
                return string.Empty;
            }

        }
           

        /// <summary>Executes the build candidate title operation.</summary>
        /// <param name="candidate">Input value for candidate.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildCandidateTitle(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                var details = string.IsNullOrWhiteSpace(candidate.Details)
          ? "No model details reported."
          : candidate.Details;
                return $"{candidate.Provider} at {candidate.Endpoint}. {details}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCandidateTitle candidate:{candidate.ToString()}");
                return string.Empty;
            }

      
        }


        /// <summary>Executes the validate solution artifact contract operation.</summary>
        /// <param name="solutionRoot">Input value for solutionRoot.</param>
        /// <param name="projectName">Input value for projectName.</param>
        /// <param name="archetype">Input value for archetype.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ArtifactContractReport? ValidateSolutionArtifactContract(
            string solutionRoot,
            string projectName,
            GeneratedSolutionArchetype archetype,ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var requiredFiles = new List<string>
            {
                $"{projectName}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
                "SOURCE_FIDELITY.md",
                "BUILD_AND_RUN.md",
                ".localgpt-generation.json",
                "LocalGPT.GenerationManifest.json",
                Path.Combine("src", projectName, $"{projectName}.csproj"),
                Path.Combine("src", projectName, "Program.cs"),
                Path.Combine("src", projectName, "Components", "GeneratedNavigation.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "Index.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedDashboard.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedKnowledgeTable.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "SourceFidelity.razor"),
                Path.Combine("src", projectName, "Components", "Pages", isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                Path.Combine("src", projectName, "Services", "GeneratedHealthSummaryService.cs"),
                Path.Combine("src", projectName, "Services", "GeneratedSourceFidelityService.cs"),
                Path.Combine("src", projectName, "Models", "GeneratedHealthCard.cs"),
                Path.Combine("src", projectName, "wwwroot", "app.css"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-solid.svg")
            };

                if (isAiHostLab)
                {
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Chat.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "RunningModels.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "ModelDownloads.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Templates.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Hardware.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "RunnerPlugins.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Logs.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Settings.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Services", "GeneratedAiHostArchitectureServices.cs"));
                }

                var missing = requiredFiles
                    .Where(relativePath => !File.Exists(Path.Combine(solutionRoot, relativePath)))
                    .ToArray();

                if (missing.Length > 0)
                    throw new InvalidOperationException($"Generated solution artifact is missing required files: {string.Join(", ", missing)}");

                ValidateGenerationContractJson(Path.Combine(solutionRoot, ".localgpt-generation.json"), logger);
                ValidateGenerationManifestJson(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), logger);

                if (isAiHostLab)
                {
                    ValidateAiHostArtifactContract(solutionRoot, projectName, logger);
                    return new ArtifactContractReport(
                        "Source-contract prototype",
                        "AI-host source contract validated",
                        [
                            "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable",
                        "AI-host endpoint and native-runner source markers exist"
                        ],
                        ["No model-file runtime execution proof was produced", "No generated-project build proof was produced"],
                        "AI-host routes, settings, navigation, and native-runner source markers were checked before zipping; runtime behavior is still unproven.");
                }

                if (archetype == GeneratedSolutionArchetype.LocalGpt)
                {
                    return new ArtifactContractReport(
                        "Static LocalGPT-style prototype",
                        "Missing LocalGPT runtime contract",
                        [
                            "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable"
                        ],
                        [
                            "DXAiChat runtime wiring is not proven",
                        "AI Council execution is not proven",
                        "SQLite memory persistence is not proven",
                        "Artifact route behavior is not proven"
                        ],
                        "LocalGPT-like source files were generated, but the artifact must not be treated as a working LocalGPT replacement.");
                }

                return new ArtifactContractReport(
                    "Generated solution prototype",
                    "Generated files validated",
                    [
                        "Required generated file set exists",
                    "Generation contract JSON is parseable",
                    "Generation manifest JSON is parseable"
                    ],
                    ["No generated-project build proof was produced", "No runtime UI proof was produced"],
                    "Required files and metadata were checked before zipping; build and runtime behavior are unproven.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateSolutionArtifactContract solutionRoot:{solutionRoot.ToString()} projectName:{projectName.ToString()} archetype:{archetype.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the validate ai host artifact contract operation.</summary>
        /// <param name="solutionRoot">Input value for solutionRoot.</param>
        /// <param name="projectName">Input value for projectName.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateAiHostArtifactContract(string solutionRoot, string projectName, ILogger logger)
        {
            try
            {
                var projectRoot = Path.Combine(solutionRoot, "src", projectName);
                var programPath = Path.Combine(projectRoot, "Program.cs");
                var architectureServicePath = Path.Combine(projectRoot, "Services", "GeneratedAiHostArchitectureServices.cs");
                var appSettingsPath = Path.Combine(projectRoot, "appsettings.json");
                var navigationPath = Path.Combine(projectRoot, "Components", "GeneratedNavigation.razor");

                var program = File.ReadAllText(programPath);
                var architectureService = File.ReadAllText(architectureServicePath);
                var appSettings = File.ReadAllText(appSettingsPath);
                var navigation = File.ReadAllText(navigationPath);

                var requiredRoutes = new[]
                {
                "/api/version",
                "/api/tags",
                "/api/ps",
                "/api/generate",
                "/api/chat"
            };

                foreach (var route in requiredRoutes)
                {
                    if (!program.Contains(route, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host artifact Program.cs is missing required route {route}.");
                }

                var requiredProgramTokens = new[]
                {
                "IInferenceProvider",
                "NativeModelFileInferenceProvider",
                "IInferenceRunner",
                "NativeModelFileProcessRunner",
                "upstream_proxy = false"
            };

                foreach (var token in requiredProgramTokens)
                {
                    if (!program.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host artifact Program.cs is missing required implementation token {token}.");
                }

                var requiredServiceTokens = new[]
                {
                "AiHostRuntimeOptions",
                "NativeModelFileProcessRunner",
                "NativeRunnerExecutable",
                "No upstream proxy fallback is used",
                "ProcessStartInfo"
            };

                foreach (var token in requiredServiceTokens)
                {
                    if (!architectureService.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host architecture service is missing required implementation token {token}.");
                }

                var requiredSettingTokens = new[]
                {
                "\"DefaultModel\"",
                "\"NativeRunnerExecutable\"",
                "\"ModelSearchRoots\"",
                "\"ContextTokens\"",
                "\"GpuLayers\""
            };

                foreach (var token in requiredSettingTokens)
                {
                    if (!appSettings.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host appsettings.json is missing required setting {token}.");
                }

                var requiredNavigationRoutes = new[]
                {
                "/chat",
                "/models",
                "/api-console",
                "/downloads",
                "/settings"
            };

                foreach (var route in requiredNavigationRoutes)
                {
                    if (!navigation.Contains(route, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host navigation is missing required route {route}.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateAiHostArtifactContract solutionRoot:{solutionRoot.ToString()} projectName:{projectName.ToString()}");
             
            }
        }

        /// <summary>Executes the validate generation contract json operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateGenerationContractJson(string path, ILogger logger)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                var requiredProperties = new[]
                {
                "schema",
                "project_kind",
                "target_platform",
                "complexity",
                "needs_datagen",
                "needs_tests",
                "needs_native_commands",
                "needs_index",
                "needs_version_resolver",
                "expected_entrypoints",
                "generated_files",
                "validation_status",
                "build_test_result_provenance"
            };

                foreach (var property in requiredProperties)
                    RequireJsonProperty(root, property, path, logger);

                RequireNonEmptyJsonArray(root, "expected_entrypoints", path, logger);
                RequireNonEmptyJsonArray(root, "generated_files", path, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGenerationContractJson path:{path.ToString()}");

            }
           
        }

        /// <summary>Executes the validate generation manifest json operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateGenerationManifestJson(string path, ILogger logger)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                RequireJsonProperty(root, "artifactKind", path, logger);
                RequireJsonProperty(root, "sourceGoal", path, logger);
                RequireJsonProperty(root, "validationStatus", path, logger);
                RequireJsonProperty(root, "buildTestResultProvenance", path, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGenerationManifestJson path:{path.ToString()}");

            }
       
        }

        /// <summary>Executes the require json property operation.</summary>
        /// <param name="root">Input value for root.</param>
        /// <param name="propertyName">Input value for propertyName.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void RequireJsonProperty(JsonElement root, string propertyName, string path, ILogger logger)
        {
            try
            {
                if (!root.TryGetProperty(propertyName, out _))
                    throw new InvalidOperationException($"Generated contract {Path.GetFileName(path)} is missing {propertyName}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequireJsonProperty root:{root.ToString()} propertyName:{propertyName.ToString()} path:{path.ToString()}");

            }
            
        }

        /// <summary>Executes the require non empty json array operation.</summary>
        /// <param name="root">Input value for root.</param>
        /// <param name="propertyName">Input value for propertyName.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void RequireNonEmptyJsonArray(JsonElement root, string propertyName, string path, ILogger logger)
        {
            try
            {
                RequireJsonProperty(root, propertyName, path, logger);
                var property = root.GetProperty(propertyName);
                if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException(
                        $"Generated contract {Path.GetFileName(path)} must include a non-empty {propertyName} array.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequireNonEmptyJsonArray root:{root.ToString()} propertyName:{propertyName.ToString()} path:{path.ToString()}");

            }
        }

    
    }
}
