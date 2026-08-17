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
    /// Exposes the LocalGPT diagnostic application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
    /// </summary>
    [ApiController]
    [Route("")]
    public partial class LocalGptDiagnosticController : ControllerBase
    {
        /// <summary>
        /// Stores the logger used by <see cref="LocalGptDiagnosticController"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<LocalGptDiagnosticController> logger;
        /// <summary>
        /// Stores the council runtime service dependency used by <see cref="LocalGptDiagnosticController"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilRuntimeService councilRuntime;
        /// <summary>
        /// Stores the council text service dependency used by <see cref="LocalGptDiagnosticController"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilTextService councilText;
        /// <summary>
        /// Stores the dev express chat service dependency used by <see cref="LocalGptDiagnosticController"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly DevExpressChatService devExpressChat;
        /// <summary>
        /// Stores the DevExpress AI function registry dependency used by <see cref="LocalGptDiagnosticController"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDxAiFunctionRegistry dxAiFunctionRegistry;
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="LocalGptDiagnosticController"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService catalog;

        /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
        /// <param name="logger">Injected dependency used by the LocalGptDiagnosticController.</param>
        /// <param name="councilRuntime">Injected dependency used by the LocalGptDiagnosticController.</param>
        /// <param name="councilText">Injected dependency used by the LocalGptDiagnosticController.</param>
        /// <param name="devExpressChat">Injected dependency used by the LocalGptDiagnosticController.</param>
        /// <param name="dxAiFunctionRegistry">Injected dependency used by the LocalGptDiagnosticController.</param>
        /// <param name="catalog">Injected dependency used by the LocalGptDiagnosticController.</param>
        public LocalGptDiagnosticController(
            ILogger<LocalGptDiagnosticController> logger,
            CouncilRuntimeService councilRuntime,
            CouncilTextService councilText,
            DevExpressChatService devExpressChat,
            IDxAiFunctionRegistry dxAiFunctionRegistry,
            LocalGptCatalogService catalog)
        {
            this.logger = logger;
            this.councilRuntime = councilRuntime;
            this.councilText = councilText;
            this.devExpressChat = devExpressChat;
            this.dxAiFunctionRegistry = dxAiFunctionRegistry;
            this.catalog = catalog;
        }

        /// <summary>
        /// Returns the require human confirmation projection for the local GPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="operation">Operation value supplied to the local GPT diagnostic operation and used when producing its result.</param>
        /// <returns>The i result produced by the operation.</returns>
        private IResult? RequireHumanConfirmation(bool userConfirmed, string operation) =>
            userConfirmed
                ? null
                : Results.BadRequest(new
                {
                    Error = "Fresh, specific human confirmation is required for this operation.",
                    Operation = operation
                });

        /// <summary>
        /// Returns the run ensure create async once projection for the local GPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="iChatMemoryService">Chat memory service dependency used by the local GPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="iApplicationLogReaderService">Application log reader service dependency used by the local GPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="iCouncilKnowledgeService">Council knowledge service dependency used by the local GPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task RunEnsureCreateAsyncOnce(IChatMemoryService? iChatMemoryService, IApplicationLogReaderService? iApplicationLogReaderService, ICouncilKnowledgeService? iCouncilKnowledgeService  )
        {
            try
            {
                if (iCouncilKnowledgeService is not null)
                    await iCouncilKnowledgeService.EnsureCreatedAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "RunEnsureCreateAsyncOnce");
            }

        }
        /// <summary>
        /// Retrieves root for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="env">Web host environment dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag")]
        public IResult GetRoot(
            [FromServices] IWebHostEnvironment env)
        {
            try
            {
                return Results.Ok(new
                {
                    env.EnvironmentName,
                    env.ContentRootPath,
                    env.WebRootPath,
                    AppAssembly = typeof(Program).Assembly.Location
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex,$"Error in GetRoot {ex.ToString()}");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
         
        }

        /// <summary>
        /// Retrieves component activity for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="componentActivity">Component activity service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/component-activity")]
        public IResult GetComponentActivity(
            [FromServices] IComponentActivityService componentActivity,
            int? take)
        {
            try
            {
                var safeTake = Math.Clamp(take ?? 20, 1, 128);
                var entries = componentActivity.GetRecent(safeTake);
                return Results.Ok(new
                {
                    Capacity = 128,
                    Count = entries.Count,
                    Entries = entries,
                    Briefing = componentActivity.BuildBriefing(Math.Min(safeTake, 20)),
                    Privacy = "Operational summaries only. Prompts, responses, uploads, generated source, secrets, and full exception details are excluded."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetComponentActivity");
                return Results.InternalServerError("Component activity diagnostics failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves AI smoke for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="chatClient">Chat client dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="prompt">Prompt value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/ai-smoke")]
        [HumanApprovalRequired("diagnostic.ai.smoke", "Call configured AI client", "Send one exact diagnostic prompt to the configured AI client.", "Medium", "AI connectivity reviewer")]
        public async Task<IResult> GetAiSmoke(
            [FromServices] IChatClient chatClient,
            string? prompt,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "call the configured AI client") is { } denied)
                    return denied;

                var response = await chatClient.GetResponseAsync(
               [
                   new Microsoft.Extensions.AI. ChatMessage(ChatRole.User, string.IsNullOrWhiteSpace(prompt)
                        ? "Reply with exactly: LocalGPT DXAiChat backend test passed."
                        : prompt)
               ],
               new ChatOptions
               {
                   MaxOutputTokens = 2048
               },
               ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    Text = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI smoke test failed.");
                return Results.InternalServerError("AI smoke test failed. Review the server log for the correlation context.");
            }
        }

        /// <summary>
        /// Retrieves Ollama compatible smoke for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="endpoint">Endpoint value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="model">Model value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="prompt">Prompt value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="numGpu">Num gpu value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="maxOutputTokens">Max output tokens value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="promptConfigService">Prompt config service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="formatterFactory">Chat response formatter factory dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="protocolResolver">Chat protocol resolver dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/ollama-compatible-smoke")]
        [HumanApprovalRequired("diagnostic.ollama.smoke", "Call Ollama-compatible endpoint", "Send one exact diagnostic request to the selected Ollama-compatible endpoint.", "Medium", "AI connectivity reviewer")]
        public async Task<IResult> GetOllamaCompatibleSmoke(
            string endpoint,
            string? model,
            string? prompt,
            int? numGpu,
            int? maxOutputTokens,
            [FromServices] IPromptConfigService promptConfigService,
            [FromServices] IChatResponseFormatterFactory formatterFactory,
            [FromServices] IChatProtocolResolver protocolResolver,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "call an Ollama-compatible endpoint") is { } denied)
                    return denied;

                var normalizedEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? "http://127.0.0.1:11434"
                : endpoint.TrimEnd('/');
                var modelName = string.IsNullOrWhiteSpace(model) ? "gpt-oss:20b" : model.Trim();
                using var client = new OllamaThinkingChatClient(
                    new OllamaCoreOptions { Uri = normalizedEndpoint, ModelName = modelName },
                    logger,
                    councilRuntime,
                    keepAlive: "0s",
                    contextLength: 2048,
                    timeout: TimeSpan.FromMinutes(5),
                    numGpu: numGpu ?? 0,
                    formatterFactory: formatterFactory,
                    protocolResolver: protocolResolver,
                    promptConfigService: promptConfigService);

                var response = await client.GetResponseAsync(
                    [
                        new Microsoft.Extensions.AI. ChatMessage(ChatRole.User, string.IsNullOrWhiteSpace(prompt)
                        ? "Reply with exactly: LocalGPT Ollama-compatible endpoint smoke passed."
                        : prompt)
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(maxOutputTokens ?? 128, 64, 4096),
                        Temperature = 0.1f
                    },
                    ct).ConfigureAwait(false);
                ArgumentNullException.ThrowIfNull(response);
                return Results.Ok(new
                {
                    Source = "LocalGPT OllamaThinkingChatClient",
                    Endpoint = normalizedEndpoint,
                    Model = modelName,
                    NumGpu = numGpu ?? 0,
                    Text = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ollama-compatible smoke test failed for model {Model}; GPU layers {NumGpu}; max output {MaxOutputTokens}.", model, numGpu, maxOutputTokens);
                return Results.InternalServerError("Ollama-compatible smoke test failed. Review the server log for non-sensitive diagnostics.");
            }
            
                        
        }

        /// <summary>
        /// Returns the post dxaichat smoke projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="chatClient">Chat client dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="memory">Chat memory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/dxaichat-smoke")]
        [HumanApprovalRequired("diagnostic.dxaichat.smoke", "Run DXAiChat diagnostic", "Call the configured chat client and optionally persist the exact diagnostic exchange.", "Medium", "Chat workflow reviewer")]
        public async Task<IResult> PostDxaichatSmoke(
            [FromBody] DxaichatSmokeRequest request,
            [FromServices] IChatClient chatClient,
            [FromServices] IChatMemoryService memory,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "run the configured DXAiChat smoke test") is { } denied)
                    return denied;

                var prompt = string.IsNullOrWhiteSpace(request.Prompt)
                ? "Reply with exactly: LocalGPT DXAiChat configured-client smoke test passed."
                : request.Prompt.Trim();

                var messages = new List<Microsoft.Extensions.AI. ChatMessage>();
                if (request.IncludeDiagnosticSystemPrompt)
                {
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, """
                    You are being called through LocalGPT's configured IChatClient, the same backend service used by the DXAiChat page.
                    This is a diagnostic smoke test, not direct Ollama access.
                    Keep the visible answer concise, mark uncertain claims as "Needs verification", and do not claim UI behavior was tested unless the prompt says it was.
                    """));
                }

                messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, prompt));

                var response = await chatClient.GetResponseAsync(
                    messages,
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(request.MaxOutputTokens, 256, 4096),
                        Temperature = 0.2f
                    },
                    ct).ConfigureAwait(false);
                await RunEnsureCreateAsyncOnce(memory,null,null).ConfigureAwait(false);
              
                Guid? savedConversationId = null;
                if (request.SaveToMemory)
                {
                    savedConversationId = await memory.SaveConversationAsync(
                        string.IsNullOrWhiteSpace(request.Title) ? "Diagnostic - DXAiChat configured client" : request.Title.Trim(),
                        [
                            new BlazorChatMessage(ChatRole.User, prompt, new List<AIChatUploadFileInfo>()),
                        new BlazorChatMessage(ChatRole.Assistant, response.Text, new List<AIChatUploadFileInfo>())
                        ],
                        cancellationToken: ct).ConfigureAwait(false);
                }

                var thinking = councilText.ExtractModelThinking(response.Text,logger);
                var visibleText = councilText.StripModelThinking(response.Text, logger);
                if (string.IsNullOrWhiteSpace(visibleText) && !string.IsNullOrWhiteSpace(thinking))
                    visibleText = "The model returned thinking but no final visible answer. Increase MaxOutputTokens or ask for a shorter final answer.";

                return Results.Ok(new
                {
                    Source = "LocalGPT configured IChatClient used by DXAiChat",
                    Prompt = prompt,
                    RawText = response.Text,
                    VisibleText = visibleText,
                    Thinking = thinking,
                    HasThinking = !string.IsNullOrWhiteSpace(thinking),
                    SavedConversationId = savedConversationId,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "PostDxaichatSmoke");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }           
        }

        /// <summary>
        /// Retrieves memory for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="memory">Chat memory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/memory")]
        public async Task<IResult> GetMemory(
            [FromServices] IChatMemoryService memory,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(memory, null, null).ConfigureAwait(false);
                var conversations = await memory.GetConversationsAsync(20, ct).ConfigureAwait(false);
                var thoughts = await memory.GetRecentThoughtsAsync(5, ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    memory.DatabasePath,
                    ConversationCount = conversations.Count,
                    RecentThoughtCount = thoughts.Count,
                    Conversations = conversations,
                    RecentThoughts = thoughts
                });

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMemory");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
           
        }

        /// <summary>
        /// Retrieves council file name for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="fileName">File name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__artifacts/council/{fileName}")]
        public IResult GetCouncilFileName(
            string fileName,
            [FromServices] ICouncilArtifactService artifacts)
        {
            try
            {
                var safeFileName = Path.GetFileName(fileName);
                var isSource = councilText.EndsWithText(safeFileName, ".cs");
                var isRazor = councilText.EndsWithText(safeFileName, ".razor");
                var isDll = councilText.EndsWithText(safeFileName, ".dll");
                var isZip = councilText.EndsWithText(safeFileName, ".zip");
                if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal) ||
                    (!isSource && !isRazor && !isDll && !isZip))
                    return Results.BadRequest("Invalid artifact file name.");

                var path = Path.Combine(artifacts.ArtifactRoot, safeFileName);
                if (!System.IO.File.Exists(path))
                    return Results.NotFound();

                HttpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{safeFileName}\"";
                HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
                var contentType = isZip
                    ? "application/zip"
                    : isDll ? "application/octet-stream" : "text/plain; charset=utf-8";
                return Results.File(path, contentType, safeFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetCouncilFileName");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }    
        }

        /// <summary>
        /// Retrieves logs for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="logs">Application log reader service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="loggerFactory">Logger factory dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="minimumLevel">Minimum level value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="writeSmoke">Write smoke value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/logs")]
        public async Task<IResult> GetLogs(
            [FromServices] IApplicationLogReaderService logs,
            [FromServices] ILoggerFactory loggerFactory,
            string? minimumLevel,
            int? take,
            bool? writeSmoke,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(null, logs, null).ConfigureAwait(false);
                var parsedLevel = Enum.TryParse<LogLevel>(minimumLevel, ignoreCase: true, out var level)
                    ? level
                    : LogLevel.Warning;

                if (writeSmoke == true)
                {
                    loggerFactory
                        .CreateLogger("LocalGPT.Diagnostics.DatabaseLoggerSmoke")
                        .LogWarning("SQLite database logger smoke test warning. This entry verifies async application log persistence.");
                    await Task.Delay(TimeSpan.FromSeconds(4), ct).ConfigureAwait(false);
                }

                var recent = await logs.GetRecentAsync(parsedLevel, take ?? 30, ct).ConfigureAwait(false);
                var briefing = await logs.BuildAiLogBriefingAsync(parsedLevel, Math.Min(take ?? 8, 20), ct).ConfigureAwait(false);
                return Results.Ok(new
                {
                    logs.DatabasePath,
                    MinimumLevel = parsedLevel.ToString(),
                    Count = recent.Count,
                    Recent = recent,
                    AiBriefing = briefing,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetLogs");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }  
        }

        /// <summary>
        /// Retrieves knowledge for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="knowledge">Council knowledge service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="includeArchived">Include archived value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/knowledge")]
        public async Task<IResult> GetKnowledge(
            [FromServices] ICouncilKnowledgeService knowledge,
            bool? includeArchived,
            int? take,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(null, null, knowledge).ConfigureAwait(false);
                var entries = await knowledge.GetEntriesAsync(includeArchived == true, take ?? 50, ct).ConfigureAwait(false);
                return Results.Ok(new
                {
                    knowledge.DatabasePath,
                    Count = entries.Count,
                    Entries = entries,
                    Briefing = await knowledge.BuildKnowledgeBriefingAsync(Math.Min(take ?? 8, 20), ct).ConfigureAwait(false),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetKnowledge");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }       
        }

        /// <summary>
        /// Retrieves sqlite tables for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="memory">Chat memory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="logs">Application log reader service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="knowledge">Council knowledge service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="tableEditor">Sqlite table editor service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/sqlite/tables")]
        public async Task<IResult> GetSqliteTables(
            [FromServices] IChatMemoryService memory,
            [FromServices] IApplicationLogReaderService logs,
            [FromServices] ICouncilKnowledgeService knowledge,
            [FromServices] ISqliteTableEditorService tableEditor,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(memory, logs, knowledge).ConfigureAwait(false);
                var tables = await tableEditor.GetTablesAsync(ct).ConfigureAwait(false);
                return Results.Ok(new
                {
                    tableEditor.DatabasePath,
                    Count = tables.Count,
                    Tables = tables,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetSqliteTables");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }         
        }

        /// <summary>
        /// Retrieves sqlite table table name for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="tableName">Table name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="memory">Chat memory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="logs">Application log reader service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="knowledge">Council knowledge service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="tableEditor">Sqlite table editor service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/sqlite/table/{tableName}")]
        public async Task<IResult> GetSqliteTableTableName(
            string tableName,
            int? take,
            [FromServices] IChatMemoryService memory,
            [FromServices] IApplicationLogReaderService logs,
            [FromServices] ICouncilKnowledgeService knowledge,
            [FromServices] ISqliteTableEditorService tableEditor,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(memory, logs, knowledge).ConfigureAwait(false);
                return Results.Ok(await tableEditor.GetTableAsync(tableName, take ?? 100, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetSqliteTableTableName");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves devexpress for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="inventory">Project library inventory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/devexpress")]
        public async Task<IResult> GetDevexpress(
            [FromServices] IProjectLibraryInventoryService inventory,
            CancellationToken ct)
        {
            try
            {
                return Results.Ok(new
                {
                    Briefing = await inventory.BuildDevExpressBriefingAsync(ct).ConfigureAwait(false),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetDevexpress");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves build debug files for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="inventory">Build debug inventory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="copy">Copy value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/build-debug-files")]
        public async Task<IResult> GetBuildDebugFiles(
            [FromServices] IBuildDebugInventoryService inventory,
            bool? copy,
            CancellationToken ct)
        {
            try
            {
                var result = await inventory.CaptureAsync(copy == true, ct).ConfigureAwait(false);
                return Results.Ok(new
                {
                    result.ArtifactRoot,
                    result.CopiedFiles,
                    result.CapturedAtUtc,
                    result.Succeeded,
                    result.Warnings,
                    Count = result.Files.Count,
                    Files = result.Files,
                    Briefing = await inventory.BuildBriefingAsync(ct).ConfigureAwait(false)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetBuildDebugFiles");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }
}
}
