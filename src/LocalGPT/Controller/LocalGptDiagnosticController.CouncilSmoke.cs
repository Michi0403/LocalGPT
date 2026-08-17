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
        /// Retrieves council artifact smoke for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="target">Target value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="prompt">Prompt value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="finalAnswer">Final answer value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/council/artifact-smoke")]
        [HumanApprovalRequired("diagnostic.council.artifact.create", "Create council artifact workspace", "Create one deterministic bounded council artifact workspace for diagnostics.", "High", "Artifact reviewer")]
        public async Task<IResult> GetCouncilArtifactSmoke(
            string? target,
            string? prompt,
            string? finalAnswer,
            [FromServices] ICouncilArtifactService artifacts,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "create a deterministic council artifact workspace") is { } denied)
                    return denied;

                var isBlazor = string.IsNullOrWhiteSpace(target) || target.Equals("blazor", StringComparison.OrdinalIgnoreCase);
                var isSolution = target?.Equals("solution", StringComparison.OrdinalIgnoreCase) == true;
                var isAiHostLab = target?.Equals("ai-host", StringComparison.OrdinalIgnoreCase) == true ||
                    target?.Equals("ollama", StringComparison.OrdinalIgnoreCase) == true;
                var isDatapack = target?.Equals("datapack", StringComparison.OrdinalIgnoreCase) == true;
                var isLoaderMatrix = target?.Equals("loader-matrix", StringComparison.OrdinalIgnoreCase) == true ||
                    target?.Equals("skeletons", StringComparison.OrdinalIgnoreCase) == true;
                var smokePrompt = isDatapack
                    ? "implementation-request smoke: generate a downloadable Minecraft Java 26.1 vanilla datapack zip named Benchmark Borough. The zip root must contain pack.mcmeta and data/ directly. Include load/tick tags, singular function folders, storage/scoreboard setup, city/register_banner, and validation notes."
                    : isLoaderMatrix
                    ? "implementation-request smoke: generate a downloadable Minecraft Java project skeleton distinction zip with separate Fabric, Paper, and NeoForge workspaces for Minecraft 26.1. Each loader must use its own metadata and Gradle conventions."
                    : isAiHostLab
                    ? "implementation-request smoke: generate a whole local AI host .NET 10 ASP.NET Core and DevExpress Blazor solution zip. " +
                        "Use only .NET, C#, Razor, and DevExpress Blazor. Include a left navigation shell, model catalog, chat, downloads, " +
                        "running models, API console, settings, logs, and selected provider-compatible API routes such as /api/version, " +
                        "/api/tags, /api/ps, /api/chat, and /api/generate. The generated host should delegate to an approved external " +
                        "Ollama-compatible provider URL by default, then fall back safely when that provider is unavailable. Do not use Go " +
                        "and do not claim native GGML/GPU inference is implemented."
                    : isSolution
                    ? "implementation-request smoke: generate a whole LocalGPT/TacosPortalOpen-style .NET 10 Blazor DevExpress solution zip with .sln, .csproj, real .razor pages, css, service/model code, README, and manifest. The zip must be downloadable through /__artifacts/council/."
                    : isBlazor
                    ? "implementation-request smoke: generate a real .NET 10 Blazor server-interactive DevExpress Razor page for a LocalGPT backend health summary card. Include a service method idea, DxGrid, DxFormLayout, DxButton, DxCheckBox, and safe download guidance."
                    : "implementation-request smoke: generate a LocalGPT backend feature artifact.";
                var requestPrompt = string.IsNullOrWhiteSpace(prompt) ? smokePrompt : prompt;
                var request = new MultiModelCouncilRequest
                {
                    Prompt = requestPrompt,
                    ModelNames = ["artifact-smoke"],
                    GenerateImplementationArtifact = true,
                    UserConfirmedArtifactBuild = userConfirmed,
                    IncludeMemory = false,
                    SaveToMemory = false,
                    Title = "Deterministic council artifact smoke"
                };
                var smokeFinalAnswer = isDatapack
                    ? "Create a validated downloadable Benchmark Borough datapack. It must use Minecraft 26.1 pack_format 101.1, singular function folders, no wrapper zip folder, no .mcfunction.txt placeholders, and a visible register_banner debug line."
                    : isLoaderMatrix
                    ? "Create a loader matrix artifact with distinct Fabric, Paper, and NeoForge skeletons. Do not reuse Fabric metadata for Paper or NeoForge."
                    : isAiHostLab
                    ? "Create a downloadable .NET 10 ASP.NET Core and DevExpress Blazor AI host control-plane lab. Include a left navigation app shell, typed model catalog records, chat/download/running-model/API-console/settings/log pages, selected REST routes, README, manifest, external-provider delegation to an Ollama-compatible URL, and a prominent note that native inference is not implemented without a real backend."
                    : isSolution
                    ? "Create a whole downloadable .NET 10 Blazor/DevExpress solution artifact with project files, routable Razor pages, CSS, service/model code, README, manifest, and safe sandbox guidance. Do not self-integrate generated files into LocalGPT without user approval."
                    : isBlazor
                    ? "Create a real Razor page artifact using @page, @rendermode InteractiveServer, DevExpress controls, and an @code block. Also include compileable support code. Keep it sandboxed until the user approves integration."
                    : "Create a compileable backend support code artifact.";
                var resultAnswer = string.IsNullOrWhiteSpace(finalAnswer) ? smokeFinalAnswer : finalAnswer;
                var result = new MultiModelCouncilResult
                {
                    Prompt = request.Prompt,
                    ModelNames = ["artifact-smoke"],
                    FinalAnswer = resultAnswer,
                    CompletedAtUtc = DateTime.UtcNow
                };

                var generated = await artifacts.CreateImplementationArtifactsAsync(request, result, ct).ConfigureAwait(false);
                return Results.Ok(new
                {
                    Target = isDatapack ? "datapack" : isLoaderMatrix ? "loader-matrix" : isAiHostLab ? "ai-host" : isSolution ? "solution" : isBlazor ? "blazor" : target,
                    artifacts.ArtifactRoot,
                    Count = generated.Count,
                    Artifacts = generated,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council artifact smoke test failed for target {Target}.", target);
                return Results.InternalServerError("Council artifact smoke test failed.");
            }     
        }

        /// <summary>
        /// Returns the post council projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="council">Multi model council service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/council")]
        public async Task<IResult> PostCouncil(
            [FromBody] MultiModelCouncilRequest request,
            [FromServices] IMultiModelCouncilService council,
            CancellationToken ct)
        {
            try
            {
                return Results.Ok(await council.RunAsync(request, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetCouncilArtifactSmoke");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }     
        }
    
    }
}
