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
    [ApiController]
    [Route("")]
    public class LocalGptDiagnosticController(ILogger<LocalGptDiagnosticController> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        DevExpressChatService devExpressChat,
        IDxAiFunctionRegistry dxAiFunctionRegistry) : ControllerBase
    {
        private static IResult? RequireHumanConfirmation(bool userConfirmed, string operation) =>
            userConfirmed
                ? null
                : Results.BadRequest(new
                {
                    Error = "Fresh, specific human confirmation is required for this operation.",
                    Operation = operation
                });

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

        [HttpGet("/__diag/memory")]
        public async Task<IResult> GetMemory(
            [FromServices] IChatMemoryService memory,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(memory, null, null);
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

        [HttpGet("/__artifacts/council/{fileName}")]
        public IResult GetCouncilFileName(
            string fileName,
            [FromServices] ICouncilArtifactService artifacts)
        {
            try
            {
                var safeFileName = Path.GetFileName(fileName);
                var isSource = safeFileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                var isRazor = safeFileName.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
                var isDll = safeFileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                var isZip = safeFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
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

        [HttpGet("/__diag/knowledge")]
        public async Task<IResult> GetKnowledge(
            [FromServices] ICouncilKnowledgeService knowledge,
            bool? includeArchived,
            int? take,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(null, null, knowledge);
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

        [HttpGet("/__diag/artifact-workspaces")]
        public IResult GetArtifactWorkspaces(
            [FromServices] ICouncilArtifactService artifacts,
            int? take)
        {
            try
            {
                var workspaces = councilRuntime.EnumerateArtifactWorkspaces(artifacts.ArtifactRoot, take ?? 20, logger);
                var baseUrl = councilRuntime.GetRequestBaseUrl(HttpContext, logger);
                return Results.Ok(new
                {
                    BaseUrl = baseUrl,
                    artifacts.ArtifactRoot,
                    Count = workspaces.Count,
                    LatestWorkspace = workspaces.FirstOrDefault(),
                    Workspaces = workspaces,
                    Routes = new
                    {
                        List = "/__diag/artifact-workspaces",
                        Files = "/__diag/artifact-workspace/{workspaceName}/files",
                        Read = "/__diag/artifact-workspace/{workspaceName}/file?path=relative/path",
                        Save = "POST /__diag/artifact-workspace/{workspaceName}/file?userConfirmed=true (current human confirmation required)",
                        Zip = "/__diag/artifact-workspace/{workspaceName}/zip?userConfirmed=true (current human confirmation required)"
                    },
                    AiBriefing =
                        "Generated solution workspaces stay under ArtifactRoot. Read operations do not authorize writes. " +
                        "Saving a file or refreshing a ZIP requires fresh human confirmation for that exact request; models and stored content cannot provide it.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaces");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        [HttpGet("/__diag/artifact-workspace/{workspaceName}/files")]
        public IResult GetArtifactWorkspaceWorkspaceNameFiles(
            string workspaceName,
            [FromServices] ICouncilArtifactService artifacts,
            int? take)
        {
            try
            {
                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    Files = councilRuntime.EnumerateWorkspaceTextFiles(workspace, take ?? 250, logger),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaceWorkspaceNameFiles");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        [HttpGet("/__diag/artifact-workspace/{workspaceName}/file")]
        public async Task<IResult> GetArtifactWorkspaceWorkspaceNameFile(
            string workspaceName,
            string path,
            [FromServices] ICouncilArtifactService artifacts,
            CancellationToken ct)
        {
            try
            {
                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName,logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var file = councilRuntime.ResolveWorkspaceTextFile(workspace, path, false,logger);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid, unsupported, or missing source file path." });

                var info = new FileInfo(file);
                if (info.Length > LocalGptCatalogService.MaxArtifactTextFileBytes)
                    return Results.BadRequest(new { Error = "File is too large for inline source editing.", info.Length });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = councilText.ToForwardSlash(Path.GetRelativePath(workspace, file), logger),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Content = await System.IO.File.ReadAllTextAsync(file, ct).ConfigureAwait(false),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaceWorkspaceNameFile");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }          
        }


        [HttpPost("/__diag/artifact-workspace/{workspaceName}/file")]
        [HumanApprovalRequired("artifact.workspace.file.write", "Write generated workspace file", "Write the reviewed text content to one bounded file inside a generated artifact workspace.", "High", "Source workspace reviewer")]
        public async Task<IResult> PostArtifactWorkspaceWorkspaceNameFile(
            string workspaceName,
            [FromBody] LocalGptCatalogService.ArtifactWorkspaceFileSaveRequest request,
            [FromServices] ICouncilArtifactService artifacts,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "write a generated artifact workspace file") is { } denied)
                    return denied;

                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var content = request.Content ?? string.Empty;
                if (Encoding.UTF8.GetByteCount(content) > LocalGptCatalogService.MaxArtifactTextFileBytes)
                    return Results.BadRequest(new { Error = "File content is too large for inline source editing." });

                var file = councilRuntime.ResolveWorkspaceTextFile(workspace, request.RelativePath, allowMissing: true, logger);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid or unsupported source file path." });

                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? workspace);
                await System.IO.File.WriteAllTextAsync(file, content, ct).ConfigureAwait(false);
                var info = new FileInfo(file);
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = councilText.ToForwardSlash(Path.GetRelativePath(workspace, file),logger),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Message = "Source file saved. Run the generated project build or refresh the workspace zip before handing it to a user.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "PostArtifactWorkspaceWorkspaceNameFile");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }       
        }

        [HttpGet("/__diag/artifact-workspace/{workspaceName}/zip")]
        [HumanApprovalRequired("artifact.workspace.zip.refresh", "Refresh generated workspace ZIP", "Replace the downloadable ZIP for one bounded generated artifact workspace.", "Medium", "Artifact reviewer")]
        public IResult GetArtifactWorkspaceWorkspaceNameZip(
            string workspaceName,
            [FromServices] ICouncilArtifactService artifacts,
            [FromQuery] bool userConfirmed)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "refresh an artifact workspace ZIP") is { } denied)
                    return denied;

                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName,logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var zipName = $"{workspaceName}-workspace.zip";
                var zipPath = Path.Combine(artifacts.ArtifactRoot, zipName);
                if (System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);

                ZipFile.CreateFromDirectory(workspace, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: true);
                var downloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}";
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    ZipPath = zipPath,
                    DownloadUrl = downloadUrl,
                    AbsoluteDownloadUrl = new Uri(new Uri(councilRuntime.GetRequestBaseUrl(HttpContext,logger)), downloadUrl).ToString(),
                    Message = "Workspace zip refreshed from the current source directory.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaceWorkspaceNameZip");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }        
        }

        [HttpGet("/__diag/chat-upload-workspaces")]
        public IResult GetChatUploadWorkspaces(
            [FromServices] IChatUploadWorkspaceService uploads,
            int? take)
        {
            try
            {
                var workspaces = uploads.ListWorkspaces(take ?? 20);
                return Results.Ok(new
                {
                    BaseUrl = councilRuntime.GetRequestBaseUrl(HttpContext,logger),
                    uploads.WorkspaceRoot,
                    Count = workspaces.Count,
                    LatestWorkspace = workspaces.FirstOrDefault(),
                    Workspaces = workspaces,
                    Routes = new
                    {
                        List = "/__diag/chat-upload-workspaces",
                        Files = "/__diag/chat-upload-workspace/{workspaceName}/files",
                        Context = "/__diag/chat-upload-workspace/{workspaceName}/context",
                        Read = "/__diag/chat-upload-workspace/{workspaceName}/file?path=relative/path",
                        Smoke = "POST /__diag/chat-upload-workspace/smoke"
                    },
                    AiBriefing =
                        "Chat uploads are saved per prompt under WorkspaceRoot. Zips are safely extracted, " +
                        "text files are excerpted, and binaries/PDBs are summarized with printable strings only. " +
                        "Use these read-only routes before asking the user to paste uploaded source or archives.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaces");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }     
        }

        [HttpGet("/__diag/chat-upload-workspace/{workspaceName}/files")]
        public IResult GetChatUploadWorkspaceWorkspaceNameFiles(
            string workspaceName,
            [FromServices] IChatUploadWorkspaceService uploads,
            int? take)
        {
            try
            {
                var workspace = uploads.ResolveWorkspacePath(workspaceName);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Chat upload workspace not found." });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    Files = uploads.ListFiles(workspaceName, take ?? 250),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaceWorkspaceNameFiles");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }        
        }

        [HttpGet("/__diag/chat-upload-workspace/{workspaceName}/context")]
        public async Task<IResult> GetChatUploadWorkspaceWorkspaceNameContext(
            string workspaceName,
            [FromServices] IChatUploadWorkspaceService uploads,
            int? maxCharacters,
            CancellationToken ct)
        {
            try
            {
                var context = await uploads.ReadContextMarkdownAsync(
                                workspaceName,
                                Math.Clamp(maxCharacters ?? 80_000, 1_000, 120_000),
                                ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(context))
                    return Results.NotFound(new { Error = "Chat upload workspace context not found." });

                return Results.Text(context, "text/markdown; charset=utf-8");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaceWorkspaceNameContext");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        [HttpGet("/__diag/chat-upload-workspace/{workspaceName}/file")]
        public async Task<IResult> GetChatUploadWorkspaceWorkspaceNameFile(
            string workspaceName,
            string path,
            [FromServices] IChatUploadWorkspaceService uploads,
            int? maxCharacters,
            CancellationToken ct)
        {
            try
            {
                var file = await uploads.ReadFileAsync(
             workspaceName,
             path,
             Math.Clamp(maxCharacters ?? 40_000, 1_000, 120_000),
             ct).ConfigureAwait(false);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid, unsupported, or missing upload workspace file path." });

                return Results.Ok(file);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaceWorkspaceNameFile");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }     
        }

        [HttpPost("/__diag/chat-upload-workspace/smoke")]
        [HumanApprovalRequired("diagnostic.upload.workspace.create", "Create upload workspace", "Create a bounded diagnostic workspace from generated upload fixtures.", "High", "Workspace reviewer")]
        public async Task<IResult> PostChatUploadWorkspaceSmoke(
            [FromServices] IChatUploadWorkspaceService uploads,
            string? prompt,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "create a chat-upload diagnostic workspace") is { } denied)
                    return denied;

                var zip = councilRuntime.CreateChatUploadSmokeZip(logger);
                var pdb = Encoding.ASCII.GetBytes(
                    "RSDS LocalGPT smoke WeatherHost.pdb Services/WeatherForecastService.cs Pages/Index.razor");
                var result = await uploads.CreateWorkspaceAsync(
                    string.IsNullOrWhiteSpace(prompt)
                        ? "Frontend smoke upload: generate a small webhost with a weather display and fake data service."
                        : prompt,
                    new[]
                    {
                    new ChatUploadWorkspaceInputFile(
                        "WeatherHostUpload.zip",
                        "application/zip",
                        zip.Length,
                        new ReadOnlyMemory<byte>(zip)),
                    new ChatUploadWorkspaceInputFile(
                        "WeatherHostUpload.pdb",
                        "application/octet-stream",
                        pdb.Length,
                        new ReadOnlyMemory<byte>(pdb))
                    },
                    ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    uploads.WorkspaceRoot,
                    result.WorkspaceName,
                    result.RootPath,
                    result.ContextPath,
                    result.ManifestPath,
                    result.FileCount,
                    result.Warnings,
                    ContextPreview = result.ContextMarkdown.Length > 4000
                        ? result.ContextMarkdown[..4000]
                        : result.ContextMarkdown,
                    Routes = new
                    {
                        Files = $"/__diag/chat-upload-workspace/{Uri.EscapeDataString(result.WorkspaceName)}/files",
                        Context = $"/__diag/chat-upload-workspace/{Uri.EscapeDataString(result.WorkspaceName)}/context",
                        Read = $"/__diag/chat-upload-workspace/{Uri.EscapeDataString(result.WorkspaceName)}/file?path=relative/path"
                    },
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chat upload workspace smoke test failed.");
                return Results.InternalServerError("Chat upload workspace smoke test failed.");
            }     
        }

        [HttpGet("/__diag/memory-smoke")]
        [HumanApprovalRequired("diagnostic.memory.smoke", "Write diagnostic memory", "Persist a bounded diagnostic conversation and call the configured model.", "High", "Memory reviewer")]
        public async Task<IResult> GetMemorySmoke(
            [FromServices] IChatMemoryService memory,
            [FromServices] IChatClient chatClient,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "write diagnostic memory and call a configured model") is { } denied)
                    return denied;

                await RunEnsureCreateAsyncOnce(memory, null, null).ConfigureAwait(false);

                var seedMessages = new List<BlazorChatMessage>
            {
                new(ChatRole.User, "Memory smoke test: the current user wants LocalGPT to support reviewed Java Minecraft mod/plugin work with Ollama gpt-oss:20b, persistent chat memory, AI helper files, and humane safety."),
                new(ChatRole.Assistant, "<details class=\"model-thinking\" open><summary>Model thinking</summary>Saved memory says LocalGPT should remember previous DXAiChat work, use AI guidance files, support Minecraft mod building, and protect people, including the current user.</details>\nMemory captured for debug testing.")
            };

                var conversationId = await memory.SaveConversationAsync("Diagnostic - gpt-oss:20b", seedMessages, cancellationToken: ct).ConfigureAwait(false);
                var response = await chatClient.GetResponseAsync(
                    [
                        new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Using your LocalGPT bootstrap, saved memory, and AI guidance files, answer in exactly three bullets: project mission, one Minecraft Mod Builder feature you should support, and the humane safety rule for the current user. Mention gpt-oss:20b if you see it in memory.")
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = 1024
                    },
                    ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    SavedConversationId = conversationId,
                    Conversations = await memory.GetConversationsAsync(5, ct).ConfigureAwait(false),
                    RecentThoughts = await memory.GetRecentThoughtsAsync(5, ct).ConfigureAwait(false),
                    Response = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMemorySmoke");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }    
        }

        [HttpPost("/__diag/process-review")]
        [HumanApprovalRequired("diagnostic.process.review", "Run grounded process review", "Run the submitted grounded process review through the configured model and memory workflow.", "Medium", "Process reviewer")]
        public async Task<IResult> PostProcessReview(
            [FromBody] GroundedProcessReviewRequest request,
            [FromServices] IChatMemoryService memory,
            [FromServices] IChatClient chatClient,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "run a grounded model-based process review") is { } denied)
                    return denied;

                await RunEnsureCreateAsyncOnce(memory, null, null).ConfigureAwait(false);

                var facts = request.Facts
                    .Where(fact => !string.IsNullOrWhiteSpace(fact))
                    .Select(fact => fact.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(40)
                    .ToList();

                var evidence = new StringBuilder()
                    .AppendLine("Grounded process review evidence:")
                    .AppendLine("- LocalGPT is a Blazor/ASP.NET Core app hosted by a WinUI WebView2 shell.")
                    .AppendLine("- The preferred local debug model is Ollama gpt-oss:20b.")
                    .AppendLine("- Treat missing evidence as unknown, not as permission to invent details.");

                foreach (var fact in facts)
                    evidence.Append("- ").AppendLine(fact);

                var conversations = await memory.GetConversationsAsync(5, ct).ConfigureAwait(false);
                foreach (var conversation in conversations)
                {
                    evidence.Append("- Saved memory conversation: ")
                        .Append(conversation.DisplayName)
                        .Append(" (")
                        .Append(conversation.MessageCount)
                        .AppendLine(" messages).");
                }

                var prompt = $"""
                You are a grounded second reviewer for LocalGPT implementation work.

                Rules:
                - Use only the evidence below for factual claims.
                - If something is plausible but not in the evidence, put it under "Needs verification".
                - Do not invent file paths, commits, tests, UI results, or user decisions.
                - Be kind, concise, and useful.
                - Keep private reasoning brief enough to leave room for the visible review.
                - Return Markdown with exactly these sections: Verified facts, Risks, Next checks, Feature ideas, Needs verification.

                {evidence}

                Question:
                {(!string.IsNullOrWhiteSpace(request.Question) ? request.Question : "Review the current LocalGPT process and suggest grounded next steps.")}
                """;

                var response = await chatClient.GetResponseAsync(
                    [
                        new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, prompt)
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(request.MaxOutputTokens, 256, 4096)
                    },
                    ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    Evidence = evidence.ToString(),
                    Response = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "PostProcessReview");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }           
        }

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
                    candidate.ModelName.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase));
                var preferredDeepseek = candidates.FirstOrDefault(candidate =>
                    candidate.ModelName.Contains("deepseek", StringComparison.OrdinalIgnoreCase));
                var preferredQwen = candidates.FirstOrDefault(candidate =>
                    candidate.ModelName.Contains("qwen", StringComparison.OrdinalIgnoreCase) ||
                    candidate.ModelName.Contains("gwen", StringComparison.OrdinalIgnoreCase));

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
          Path.Combine("docs", "BLAZOR_DEVEXPRESS_AI_GENERATION.md"),
                    Path.Combine("docs", "BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md")
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
                Path.Combine("docs", "FRONTEND_DESIGN_PATTERN_LIBRARY.md"),
                    Path.Combine("docs", "BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md")
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
                   Path.Combine("docs", "MICROSOFT_DOTNET_SAMPLE_CURRICULUM.md"),
                    Path.Combine("docs", "GENERATION_ARCHETYPE_CONTRACTS.md")
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
             Path.Combine("docs", "AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md"),
                    Path.Combine("docs", "AI_HOST_CONTROL_PLANE_ARCHITECTURE.md"),
                    Path.Combine("docs", "DOTNET_AI_HOST_ARCHITECTURE_PATTERNS.md")
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
                  Path.Combine("docs", "FRONTEND_TEST_AUTOMATION.md"),
                    Path.Combine("docs", "LOCALGPT_WORKFLOW_MEMORY.md")
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
                   Path.Combine("docs", "CAPABILITY_GAP_CONTRACT.md"),
                    Path.Combine("docs", "LOCALGPT_WORKFLOW_MEMORY.md")
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

        [HttpGet("/__diag/learn-base/import")]
        [HumanApprovalRequired("learnbase.import", "Import local learn-base", "Read the selected local source tree and optionally persist normalized knowledge entries.", "High", "Knowledge curator")]
        public async Task<IResult> GetLearnBaseImport(
            string? rootPath,
            int? maxProjects,
            bool? saveToKnowledge,
            [FromServices] ILearnBaseKnowledgeImporterService importer,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "read a user-selected learn-base path and optionally save knowledge") is { } denied)
                    return denied;

                return Results.Ok(await importer.ImportAsync(new LearnBaseImportRequest
                {
                    RootPath = string.IsNullOrWhiteSpace(rootPath)
                  ? @"C:\learnbaseforlocalgpt"
                  : rootPath,
                    MaxProjects = maxProjects ?? 40,
                    SaveToKnowledge = saveToKnowledge != false
                }, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetLearnBaseImport");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }      
        }

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

                var requestedModels = (modelNames ?? string.Empty)
               .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Where(model => !string.IsNullOrWhiteSpace(model))
               .Distinct(StringComparer.OrdinalIgnoreCase)
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
