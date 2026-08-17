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
        /// Retrieves council models for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="council">Multi model council service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/council/models")]
        public async Task<IResult> GetCouncilModels(
            [FromServices] IMultiModelCouncilService council,
            CancellationToken ct)
        {
            try
            {
                return Results.Ok(await council.GetCandidatesAsync(ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetCouncilModels");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves council benchmark plan for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="council">Multi model council service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/council/benchmark-plan")]
        public async Task<IResult> GetCouncilBenchmarkPlan(
            [FromServices] IMultiModelCouncilService council,
            CancellationToken ct)
        {
            try
            {
                var candidates = await council.GetCandidatesAsync(ct).ConfigureAwait(false);
                var available = candidates
                    .Where(candidate => candidate.IsInstalled || candidate.IsConfigured)
                    .Select(candidate => new
                    {
                        candidate.ModelName,
                        candidate.Provider,
                        candidate.Endpoint,
                        candidate.IsInstalled,
                        candidate.IsConfigured,
                        candidate.IsLoaded,
                        candidate.Details
                    })
                    .Take(16)
                    .ToArray();

                var preferredGptOss = candidates.FirstOrDefault(candidate =>
                    councilText.ContainsText(candidate.ModelName, "gpt-oss"));
                var preferredDeepseek = candidates.FirstOrDefault(candidate =>
                    councilText.ContainsText(candidate.ModelName, "deepseek"));
                var preferredQwen = candidates.FirstOrDefault(candidate =>
                    councilText.ContainsText(candidate.ModelName, "qwen") ||
                    councilText.ContainsText(candidate.ModelName, "gwen"));

                return Results.Ok(new
                {
                    HardwareProfile = "Configured local workstation: 7900 XTX 24GB VRAM, i7-14700K, 64GB RAM. Avoid simultaneous heavy 20B/27B/30B GPU loads.",
                    AvailableModels = available,
                    RecommendedMatrix = new[]
                    {
                    new
                    {
                        Name = "Baseline single-model generation",
                        Members = preferredGptOss is null ? Array.Empty<string>() : new[] { preferredGptOss.ModelName },
                        MaxParallelModels = 1,
                        OllamaNumGpu = (int?)null,
                        MaxContextTokens = 32768,
                        MaxOutputTokens = 8192,
                        Purpose = "Verify Harmony formatting, streaming, artifact links, and normal DXAiChat usability with a compact but realistic local context."
                    },
                    new
                    {
                        Name = "CPU-stable reviewer",
                        Members = preferredDeepseek is null ? Array.Empty<string>() : new[] { preferredDeepseek.ModelName },
                        MaxParallelModels = 1,
                        OllamaNumGpu = (int?)0,
                        MaxContextTokens = 32768,
                        MaxOutputTokens = 4096,
                        Purpose = "Slow but GPU-safe review of generated .NET/DevExpress or Minecraft datapack output."
                    },
                    new
                    {
                        Name = "Two-member safe council",
                        Members = new[] { preferredGptOss?.ModelName, preferredDeepseek?.ModelName }
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Cast<string>()
                            .ToArray(),
                        MaxParallelModels = 1,
                        OllamaNumGpu = (int?)0,
                        MaxContextTokens = 32768,
                        MaxOutputTokens = 8192,
                        Purpose = "Best default cross-check without concurrent VRAM pressure; use keep_alive=0s."
                    },
                    new
                    {
                        Name = "Heavy coder solo trial",
                        Members = preferredQwen is null ? Array.Empty<string>() : new[] { preferredQwen.ModelName },
                        MaxParallelModels = 1,
                        OllamaNumGpu = (int?)12,
                        MaxContextTokens = 65536,
                        MaxOutputTokens = 32768,
                        Purpose = "Optional qwen/gwen solo code-generation check after Ollama/GPU stability is confirmed. Do not combine with other heavy models."
                    }
                },
                    BenchmarkPrompt = "DXAiChat benchmark: generate a downloadable .NET 10 DevExpress Blazor solution zip with an Index page, navigation, one API route, one EF/SQLite-backed service, and a README. Then summarize which files were produced and what still needs verification.",
                    Acceptance = new[]
                    {
                    "The answer streams or shows visible status before first token.",
                    "The final answer includes /__artifacts/council/ download links, not zip text.",
                    "Generated Razor files are real .razor components, not string-builder fake pages.",
                    "A poll appears only when a material choice is genuinely missing and generation pauses for the next user turn."
                },
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetCouncilBenchmarkPlan");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }         
        }

        /// <summary>
        /// Retrieves dxaichat functions for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/dxaichat-functions")]
        public IResult GetDxaichatFunctions()
        {
            try
            {
                return Results.Ok(devExpressChat.GetFunctions());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetDxaichatFunctions");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }       
        }


        /// <summary>
        /// Invokes DevExpress function for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="functionName">Function name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/dxaichat-functions/{functionName}/invoke")]
        public async Task<IResult> InvokeDxFunction(
            string functionName,
            [FromBody] DxAiFunctionInvocationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await dxAiFunctionRegistry
                .InvokeAsync(functionName, request, cancellationToken)
                .ConfigureAwait(false);
            var statusCode = result.Status switch
            {
                "NotFound" => StatusCodes.Status404NotFound,
                "HumanConfirmationRequired" => StatusCodes.Status409Conflict,
                "InvalidParameters" => StatusCodes.Status400BadRequest,
                "DiscoveryOnly" => StatusCodes.Status405MethodNotAllowed,
                "Failed" => StatusCodes.Status500InternalServerError,
                _ => result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
            };
            return Results.Json(result, statusCode: statusCode);
        }


        /// <summary>
        /// Retrieves blazor devexpress guidance for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/blazor-devexpress-guidance")]
        public async Task<IResult> GetBlazorDevexpressGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await councilRuntime.ReadGuidanceDocsAsync(
      env,
      [
          Path.Combine("docs", "architecture", "frontend-and-themes.md"),
          Path.Combine("docs", "architecture", "system-overview.md")
      ],
      """
                Generate real .razor files for Blazor UI requests. Use @page, @rendermode InteractiveServer,
                @code, dependency injection, Bootstrap v5 layout utilities, and known DevExpress Blazor controls.
                Generate line and solid SVG navigation icon variants when nav icons are requested. Check
                /__diag/devexpress for package inventory and mark unknown APIs as Needs verification.
                """,
      ct,logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetBlazorDevexpressGuidance");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves frontend design guidance for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/frontend-design-guidance")]
        public async Task<IResult> GetFrontendDesignGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await councilRuntime.ReadGuidanceDocsAsync(
            env,
            [
                Path.Combine("docs", "architecture", "frontend-and-themes.md"),
                Path.Combine("docs", "guide", "getting-started.md")
            ],
            """
                Use LocalGPT's compiled frontend design pattern library directly.
                Classify the app archetype, primary task, information architecture, Windows/Fluent design
                principles, Bootstrap layout, DevExpress/custom Razor components, injected services,
                accessibility states, and safe downloadable artifact path before generating frontend code.
                """,
            ct,logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetFrontendDesignGuidance");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }   
        }

        /// <summary>
        /// Retrieves dotnet sample curriculum for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/dotnet-sample-curriculum")]
        public async Task<IResult> GetDotnetSampleCurriculum(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await councilRuntime.ReadGuidanceDocsAsync(
               env,
               [
                   Path.Combine("docs", "architecture", "frontend-and-themes.md"),
                   Path.Combine("docs", "architecture", "system-overview.md")
               ],
               """
                Use official Microsoft/dotnet samples and Microsoft Learn training as the baseline for .NET
                generation. Prefer focused samples, real .NET project structure, C# fundamentals, ASP.NET Core
                services, Blazor pages, EF/SQLite persistence, CI/build/test/publish evidence, and explicit
                architecture boundaries. Mark unknown package or template details as Needs verification.
                """,
               ct, logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetDotnetSampleCurriculum");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }  
        }

        /// <summary>
        /// Retrieves AI host rebuild guidance for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/ai-host-rebuild-guidance")]
        public async Task<IResult> GetAiHostRebuildGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await councilRuntime.ReadGuidanceDocsAsync(
         env,
         [
             Path.Combine("docs", "architecture", "ai-host.md"),
             Path.Combine("docs", "architecture", "system-overview.md"),
             Path.Combine("docs", "architecture", "frontend-and-themes.md")
         ],
         """
                Generate a local AI host .NET/ASP.NET Core/DevExpress Blazor control-plane app with a
                recognizable left navigation shell, model catalog, chat, downloads, running models, API console,
                templates, hardware, logs, diagnostics, settings, representative provider-compatible API routes,
                DI/IoC registrations, provider adapters, plugin/native-runner interfaces, Python.NET/PowerShell
                boundaries when useful, and an honest native-inference capability gap until a real runner exists.
                """,
         ct,logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetAiHostRebuildGuidance");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
     
                        
        }

        /// <summary>
        /// Retrieves frontend test guidance for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/frontend-test-guidance")]
        public async Task<IResult> GetFrontendTestGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await councilRuntime.ReadGuidanceDocsAsync(
              env,
              [
                  Path.Combine("docs", "engineering", "build-validation.md"),
                  Path.Combine("docs", "architecture", "frontend-and-themes.md")
              ],
              """
                Prefer LocalGPT Test Lab and deterministic local HTTP diagnostic routes before loading heavy
                models. For the real WinUI/WebView2 shell, use Microsoft Edge WebDriver with Selenium and either
                launch the WebView2 app or attach to a running WebView2 instance through a remote debugging port.
                Optional Python/browser automation belongs behind explicit user permission gates and should be
                learned as source fingerprints rather than pasted as huge prompt context.
                """,
              ct,logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetFrontendTestGuidance");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves capability gap contract for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/capability-gap-contract")]
        public async Task<IResult> GetCapabilityGapContract(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await councilRuntime.ReadGuidanceDocsAsync(
               env,
               [
                   Path.Combine("docs", "reference", "capability-map.md"),
                   Path.Combine("docs", "reference", "design-evolution.md")
               ],
               """
                If LocalGPT, DXAiChat, or the AI Council lacks a function, source, version map, or domain
                knowledge needed for a user request, emit a structured capability gap instead of refusing.
                Classify requested language/framework/version/domain knowledge, local sources, external
                official sources, missing LocalGPT functions, safe workflow, and downloadable artifact plan.
                """,
               ct,logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetCapabilityGapContract");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
           

        }

    }
}
