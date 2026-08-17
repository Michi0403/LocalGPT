using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.CodeParser.Diagnostics;
using DevExpress.Xpo.Logger;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Controller
{
    /// <summary>
    /// Exposes the local GPT diagnostic application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
    /// </summary>
    public partial class LocalGptDiagnosticController
    {
        /// <summary>
        /// Returns the post learn base import projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="importer">Learn base knowledge importer service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/learn-base/import")]
        [HumanApprovalRequired("learnbase.import", "Import local learn-base", "Read the selected local source tree and optionally persist normalized knowledge entries.", "High", "Knowledge curator")]
        public async Task<IResult> PostLearnBaseImport(
            [FromBody] LearnBaseImportRequest request,
            [FromServices] ILearnBaseKnowledgeImporterService importer,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "read a user-selected learn-base path and optionally save knowledge") is { } denied)
                    return denied;

                return Results.Ok(await importer.ImportAsync(request, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "PostLearnBaseImport");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
                        
        }

        /// <summary>
        /// Retrieves learn base import for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="rootPath">Root path value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maxProjects">Max projects value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="saveToKnowledge">Save to knowledge value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="fileExtensions">File extensions value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="includeRegex">Include regex value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="excludeRegex">Exclude regex value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maximumFileBytes">Maximum file bytes value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="importLearningSourceManifests">Import learning source manifests value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="importKnownDocumentationCorpora">Import known documentation corpora value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="importProjectArchitecture">Import project architecture value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="importer">Learn base knowledge importer service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/learn-base/import")]
        [HumanApprovalRequired("learnbase.import", "Import local learn-base", "Read the selected local source tree and optionally persist normalized knowledge entries.", "High", "Knowledge curator")]
        public async Task<IResult> GetLearnBaseImport(
            string? rootPath,
            int? maxProjects,
            bool? saveToKnowledge,
            string? fileExtensions,
            string? includeRegex,
            string? excludeRegex,
            int? maximumFileBytes,
            bool? importLearningSourceManifests,
            bool? importKnownDocumentationCorpora,
            bool? importProjectArchitecture,
            [FromServices] ILearnBaseKnowledgeImporterService importer,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "read a user-selected learn-base path and optionally save knowledge") is { } denied)
                    return denied;

                if (string.IsNullOrWhiteSpace(rootPath))
                    return Results.BadRequest("Select an explicit local learn-base folder. LocalGPT no longer invents a machine-specific default path.");

                return Results.Ok(await importer.ImportAsync(new LearnBaseImportRequest
                {
                    RootPath = rootPath.Trim(),
                    MaxProjects = maxProjects ?? 40,
                    SaveToKnowledge = saveToKnowledge != false,
                    AdditionalFileExtensions = fileExtensions ?? string.Empty,
                    FileIncludeRegex = includeRegex ?? string.Empty,
                    FileExcludeRegex = excludeRegex ?? string.Empty,
                    MaximumFileBytes = maximumFileBytes ?? 1_048_576,
                    ImportLearningSourceManifests = importLearningSourceManifests != false,
                    ImportKnownDocumentationCorpora = importKnownDocumentationCorpora != false,
                    ImportProjectArchitecture = importProjectArchitecture != false
                }, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetLearnBaseImport");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }      
        }

        /// <summary>
        /// Returns the post benchmark engineering projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="benchmark">Engineering benchmark service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/benchmark/engineering")]
        [HumanApprovalRequired("diagnostic.engineering.benchmark", "Run engineering benchmark", "Run the bounded engineering benchmark and persist its reviewed diagnostic result.", "High", "Engineering benchmark reviewer")]
        public async Task<IResult> PostBenchmarkEngineering(
            [FromBody] EngineeringBenchmarkRequest request,
            [FromServices] IEngineeringBenchmarkService benchmark,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "run the engineering benchmark") is { } denied)
                    return denied;

                request.UserConfirmedArtifactActions = request.UserConfirmedArtifactActions && userConfirmed;
                return Results.Ok(await benchmark.RunAsync(request, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "PostBenchmarkEngineering");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }         
        }

        /// <summary>
        /// Retrieves benchmark engineering for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="importLearnBaseFirst">Import learn base first value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="saveToKnowledge">Save to knowledge value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="validateBuildableArtifacts">Validate buildable artifacts value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maxBuildArtifacts">Max build artifacts value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="taskSet">Task set value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="benchmark">Engineering benchmark service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/benchmark/engineering")]
        [HumanApprovalRequired("diagnostic.engineering.benchmark", "Run engineering benchmark", "Run the bounded engineering benchmark and persist its reviewed diagnostic result.", "High", "Engineering benchmark reviewer")]
        public async Task<IResult> GetBenchmarkEngineering(
            bool? importLearnBaseFirst,
            bool? saveToKnowledge,
            bool? validateBuildableArtifacts,
            int? maxBuildArtifacts,
            string? taskSet,
            [FromServices] IEngineeringBenchmarkService benchmark,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "run the engineering benchmark") is { } denied)
                    return denied;

                return Results.Ok(await benchmark.RunAsync(new EngineeringBenchmarkRequest
                {
                    ImportLearnBaseFirst = importLearnBaseFirst == true,
                    SaveToKnowledge = saveToKnowledge != false,
                    ValidateBuildableArtifacts = validateBuildableArtifacts == true,
                    MaxBuildArtifacts = maxBuildArtifacts ?? 3,
                    UserConfirmedArtifactActions = userConfirmed && validateBuildableArtifacts == true,
                    TaskSet = string.IsNullOrWhiteSpace(taskSet) ? "engineering" : taskSet
                }, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetBenchmarkEngineering");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }         
        }

        /// <summary>
        /// Retrieves council development feedback talk for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="modelNames">Model names value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maxOutputTokens">Max output tokens value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maxContextTokens">Max context tokens value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maxRounds">Max rounds value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="ollamaNumGpu">Ollama num gpu value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="council">Multi model council service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/council/development-feedback-talk")]
        [HumanApprovalRequired("diagnostic.council.feedback", "Run council development feedback", "Start the requested local council feedback session and persist its bounded result.", "Medium", "Council facilitator")]
        public async Task<IResult> GetCouncilDevelopmentFeedbackTalk(
            string? modelNames,
            int? maxOutputTokens,
            int? maxContextTokens,
            int? maxRounds,
            int? ollamaNumGpu,
            [FromServices] IMultiModelCouncilService council,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "run a council development feedback session") is { } denied)
                    return denied;

                var requestedModels = councilText.ParseUserEditableNameList(modelNames)
               .Take(4)
               .ToList();

                if (requestedModels.Count < 2)
                {
                    var candidates = await council.GetCandidatesAsync(ct).ConfigureAwait(false);
                    requestedModels = candidates
                        .Where(candidate => candidate.IsInstalled || candidate.IsConfigured)
                        .Select(candidate => candidate.ModelName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(2)
                        .ToList();
                }

                if (requestedModels.Count < 2)
                    requestedModels = ["gpt-oss:20b", "deepseek-r1:8b"];

                var request = new MultiModelCouncilRequest
                {
                    Title = "LocalGPT development feedback talk",
                    Prompt = """
                    LocalGPT Council development feedback talk.

                    Speak as at least two cooperative council members reviewing our development process.
                    Discuss what LocalGPT still needs to generate fully working LocalGPT-style, TacosPortalOpen-style,
                    provider-compatible AI-host, and simple bot-backend replacement solutions faster and with fewer
                    missing features.

                    Requirements:
                    - Be kind to all participants and the current user.
                    - Do not refuse because the task is large; propose buildable milestones.
                    - Report missing LocalGPT functions, knowledge, routes, UI controls, benchmark evidence, or sources.
                    - Include a concise Capability gap report when anything is missing.
                    - Mention whether the replacement benchmark should run with build validation.
                    - Keep the answer compact enough for DXAiChat/Test Lab.
                """,
                    ModelNames = requestedModels,
                    MaxOutputTokens = Math.Clamp(maxOutputTokens ?? 2048, 128, 262144),
                    MaxContextTokens = Math.Clamp(maxContextTokens ?? 32768, 2048, 262144),
                    MaxRounds = Math.Clamp(maxRounds ?? 0, 0, 1),
                    MaxParallelModels = 1,
                    OllamaKeepAlive = "0s",
                    OllamaNumGpu = ollamaNumGpu,
                    IncludeMemory = true,
                    SaveToMemory = true,
                    GenerateImplementationArtifact = false
                };

                if (request.ModelNames.Count < 2)
                    return Results.BadRequest(new { Error = "Development feedback talk requires at least two council members.", request.ModelNames });

                return Results.Ok(await council.RunAsync(request, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetCouncilDevelopmentFeedbackTalk");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }       
        }

    }
}
