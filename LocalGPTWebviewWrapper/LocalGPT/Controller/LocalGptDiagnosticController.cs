using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.CodeParser.Diagnostics;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
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
    public class LocalGptDiagnosticController(ILogger<LocalGptDiagnosticController> logger) : ControllerBase
    {
        private async Task RunEnsureCreateAsyncOnce(IChatMemoryService? iChatMemoryService, IApplicationLogReaderService? iApplicationLogReaderService, ICouncilKnowledgeService? iCouncilKnowledgeService  )
        {
            try
            {
                //if(GlobalVariableSlopCollectionToRemove.EnsureCreatedMemoryDbTable!= true && iChatMemoryService != null)
                //{
                //    GlobalVariableSlopCollectionToRemove.EnsureCreatedMemoryDbTable = true;
                //}
                //if (GlobalVariableSlopCollectionToRemove.EnsureCreatedLogsDbTable != true && iApplicationLogReaderService != null)
                //{
                //    GlobalVariableSlopCollectionToRemove.EnsureCreatedLogsDbTable = true;
                //}
                if (GlobalVariableSlopCollectionToRemove.EnsureCreatedKnowledgeDbTable != true && iCouncilKnowledgeService != null)
                {
                    await iCouncilKnowledgeService.EnsureCreatedAsync().ConfigureAwait(false);
                    GlobalVariableSlopCollectionToRemove.EnsureCreatedKnowledgeDbTable = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RunEnsureCreateAsyncOnce {ex.ToString()}");
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
                return Results.InternalServerError($"Error in GetRoot {ex.ToString()}");
            }
         
        }

        [HttpGet("/__diag/ai-smoke")]
        public async Task<IResult> GetAiSmoke(
            [FromServices] IChatClient chatClient,
            string? prompt,
            CancellationToken ct)
        {
            try
            {
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
                logger.LogError(ex,$"Error in GetAiSmoke {ex.ToString()} chatClient {chatClient.ToString()} prompt {prompt?.ToString()}");
                return Results.InternalServerError($"Error in GetAiSmoke {ex.ToString()} chatClient {chatClient.ToString()} prompt {prompt?.ToString()}");
            }
        }

        [HttpGet("/__diag/ollama-compatible-smoke")]
        public async Task<IResult> GetOllamaCompatibleSmoke(
            string endpoint,
            string? model,
            string? prompt,
            int? numGpu,
            int? maxOutputTokens,
            CancellationToken ct)
        {
            try
            {
                var normalizedEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? "http://127.0.0.1:11434"
                : endpoint.TrimEnd('/');
                var modelName = string.IsNullOrWhiteSpace(model) ? "gpt-oss:20b" : model.Trim();
                using var client = new OllamaThinkingChatClient(
                    new OllamaCoreOptions { Uri = normalizedEndpoint, ModelName = modelName },
                    logger,
                    keepAlive: "0s",
                    contextLength: 2048,
                    timeout: TimeSpan.FromMinutes(5),
                    numGpu: numGpu ?? 0);

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
                logger.LogError(ex,$"Error in GetOllamaCompatibleSmoke {ex.ToString()} endpoint {endpoint} model {model?.ToString()} prompt {prompt?.ToString()} numGpu {numGpu?.ToString()} maxOutputTokens {maxOutputTokens?.ToString()}");
                return Results.InternalServerError($"Error in GetOllamaCompatibleSmoke {ex.ToString()} endpoint {endpoint} model {model?.ToString()} prompt {prompt?.ToString()} numGpu {numGpu?.ToString()} maxOutputTokens {maxOutputTokens?.ToString()}");
            }
            
                        
        }

        [HttpPost("/__diag/dxaichat-smoke")]
        public async Task<IResult> PostDxaichatSmoke(
            [FromBody] DxaichatSmokeRequest request,
            [FromServices] IChatClient chatClient,
            [FromServices] IChatMemoryService memory,
            CancellationToken ct)
        {
            try
            {
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

                var thinking = CouncilChatStringFunctions.ExtractModelThinking(response.Text,logger);
                var visibleText = CouncilChatStringFunctions.StripModelThinking(response.Text, logger);
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
                logger.LogError(ex,$"Error in PostDxaichatSmoke {ex.ToString()} request {request.ToString()} chatClient {chatClient?.ToString()} memory {memory?.ToString()}");
                return Results.InternalServerError($"Error in PostDxaichatSmoke {ex.ToString()} request {request.ToString()} chatClient {chatClient?.ToString()} memory {memory?.ToString()}");
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
                logger.LogError(ex, $"Error in GetMemory {ex.ToString()} memory {memory.ToString()}");
                return Results.InternalServerError($"Error in GetMemory {ex.ToString()} memory {memory.ToString()}");
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
                logger.LogError(ex, $"Error in GetCouncilFileName {ex.ToString()} fileName {fileName.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in GetCouncilFileName {ex.ToString()} fileName {fileName.ToString()} artifacts {artifacts.ToString()}");
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
                logger.LogError(ex, $"Error in GetLogs {ex.ToString()} logs {logs.ToString()} minimumLevel {minimumLevel?.ToString()} take {take?.ToString()} writeSmoke {writeSmoke?.ToString()}");
                return Results.InternalServerError($"Error in GetLogs {ex.ToString()} logs {logs.ToString()} minimumLevel {minimumLevel?.ToString()} take {take?.ToString()} writeSmoke {writeSmoke?.ToString()}");
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
                logger.LogError(ex, $"Error in GetKnowledge {ex.ToString()} knowledge {knowledge.ToString()} includeArchived {includeArchived?.ToString()} take {take?.ToString()}");
                return Results.InternalServerError($"Error in GetKnowledge {ex.ToString()} knowledge {knowledge.ToString()} includeArchived {includeArchived?.ToString()} take {take?.ToString()}");
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
                logger.LogError(ex, $"Error in GetSqliteTables {ex.ToString()} memory {memory.ToString()} logs {logs?.ToString()} knowledge {knowledge?.ToString()} tableEditor {tableEditor?.ToString()}");
                return Results.InternalServerError($"Error in GetSqliteTables {ex.ToString()} memory {memory.ToString()} logs {logs?.ToString()} knowledge {knowledge?.ToString()} tableEditor {tableEditor?.ToString()}");
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
                logger.LogError(ex, $"Error in GetSqliteTableTableName {ex.ToString()} memory {memory.ToString()} logs {logs?.ToString()} knowledge {knowledge?.ToString()} tableEditor {tableEditor?.ToString()}");
                return Results.InternalServerError($"Error in GetSqliteTableTableName {ex.ToString()} memory {memory.ToString()} logs {logs?.ToString()} knowledge {knowledge?.ToString()} tableEditor {tableEditor?.ToString()}");
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
                logger.LogError(ex, $"Error in GetDevexpress {ex.ToString()} inventory {inventory.ToString()}");
                return Results.InternalServerError($"Error in GetDevexpress {ex.ToString()} inventory {inventory.ToString()}");
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
                    Count = result.Files.Count,
                    Files = result.Files,
                    Briefing = await inventory.BuildBriefingAsync(ct).ConfigureAwait(false)
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetBuildDebugFiles {ex.ToString()} inventory {inventory.ToString()} copy {copy.ToString()}");
                return Results.InternalServerError($"Error in GetBuildDebugFiles {ex.ToString()} inventory {inventory.ToString()} copy {copy.ToString()}");
            }
        }

        [HttpGet("/__diag/artifact-workspaces")]
        public IResult GetArtifactWorkspaces(
            [FromServices] ICouncilArtifactService artifacts,
            int? take)
        {
            try
            {
                var workspaces = CouncilChatStaticsGeneral.EnumerateArtifactWorkspaces(artifacts.ArtifactRoot, take ?? 20, logger);
                var baseUrl = CouncilChatStaticsGeneral.GetRequestBaseUrl(HttpContext, logger);
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
                        Save = "POST /__diag/artifact-workspace/{workspaceName}/file",
                        Zip = "/__diag/artifact-workspace/{workspaceName}/zip"
                    },
                    AiBriefing =
                        "Generated solution workspaces stay under ArtifactRoot until the user explicitly downloads or refreshes a zip. " +
                        "Use BaseUrl + DownloadUrl for absolute links, and use workspaceName plus relative source paths for edits.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetArtifactWorkspaces {ex.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in GetArtifactWorkspaces {ex.ToString()} artifacts {artifacts.ToString()}");
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
                var workspace = CouncilChatStaticsGeneral.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    Files = CouncilChatStaticsGeneral.EnumerateWorkspaceTextFiles(workspace, take ?? 250, logger),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetArtifactWorkspaceWorkspaceNameFiles {ex.ToString()} workspaceName {workspaceName.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in GetArtifactWorkspaceWorkspaceNameFiles {ex.ToString()} workspaceName {workspaceName.ToString()} artifacts {artifacts.ToString()}");
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
                var workspace = CouncilChatStaticsGeneral.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName,logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var file = CouncilChatStaticsGeneral.ResolveWorkspaceTextFile(workspace, path, false,logger);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid, unsupported, or missing source file path." });

                var info = new FileInfo(file);
                if (info.Length > GlobalVariableSlopCollectionToRemove.MaxArtifactTextFileBytes)
                    return Results.BadRequest(new { Error = "File is too large for inline source editing.", info.Length });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspace, file), logger),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Content = await System.IO.File.ReadAllTextAsync(file, ct).ConfigureAwait(false),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetArtifactWorkspaceWorkspaceNameFile {ex.ToString()} workspaceName {workspaceName.ToString()} path {path.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in GetArtifactWorkspaceWorkspaceNameFile {ex.ToString()} workspaceName {workspaceName.ToString()} path {path.ToString()} artifacts {artifacts.ToString()}");
            }          
        }


        [HttpPost("/__diag/artifact-workspace/{workspaceName}/file")]
        public async Task<IResult> PostArtifactWorkspaceWorkspaceNameFile(
            string workspaceName,
            [FromBody] GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceFileSaveRequest request,
            [FromServices] ICouncilArtifactService artifacts,
            CancellationToken ct)
        {
            try
            {
                var workspace = CouncilChatStaticsGeneral.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var content = request.Content ?? string.Empty;
                if (Encoding.UTF8.GetByteCount(content) > GlobalVariableSlopCollectionToRemove.MaxArtifactTextFileBytes)
                    return Results.BadRequest(new { Error = "File content is too large for inline source editing." });

                var file = CouncilChatStaticsGeneral.ResolveWorkspaceTextFile(workspace, request.RelativePath, allowMissing: true, logger);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid or unsupported source file path." });

                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? workspace);
                await System.IO.File.WriteAllTextAsync(file, content, ct).ConfigureAwait(false);
                var info = new FileInfo(file);
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspace, file),logger),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Message = "Source file saved. Run the generated project build or refresh the workspace zip before handing it to a user.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in PostArtifactWorkspaceWorkspaceNameFile {ex.ToString()} workspaceName {workspaceName.ToString()} request {request.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in PostArtifactWorkspaceWorkspaceNameFile {ex.ToString()} workspaceName {workspaceName.ToString()} request {request.ToString()} artifacts {artifacts.ToString()}");
            }       
        }

        [HttpGet("/__diag/artifact-workspace/{workspaceName}/zip")]
        public IResult GetArtifactWorkspaceWorkspaceNameZip(
            string workspaceName,
            [FromServices] ICouncilArtifactService artifacts)
        {
            try
            {
                var workspace = CouncilChatStaticsGeneral.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName,logger);
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
                    AbsoluteDownloadUrl = new Uri(new Uri(CouncilChatStaticsGeneral.GetRequestBaseUrl(HttpContext,logger)), downloadUrl).ToString(),
                    Message = "Workspace zip refreshed from the current source directory.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetArtifactWorkspaceWorkspaceNameZip {ex.ToString()} workspaceName {workspaceName.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in GetArtifactWorkspaceWorkspaceNameZip {ex.ToString()} workspaceName {workspaceName.ToString()} artifacts {artifacts.ToString()}");
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
                    BaseUrl = CouncilChatStaticsGeneral.GetRequestBaseUrl(HttpContext,logger),
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
                logger.LogError(ex, $"Error in GetChatUploadWorkspaces {ex.ToString()} uploads {uploads.ToString()} take {take.ToString()}");
                return Results.InternalServerError($"Error in GetChatUploadWorkspaces {ex.ToString()} uploads {uploads.ToString()} take {take.ToString()}");
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
                logger.LogError(ex, $"Error in GetChatUploadWorkspaceWorkspaceNameFiles {ex.ToString()} workspaceName {workspaceName.ToString()} uploads {uploads.ToString()} take {take.ToString()}");
                return Results.InternalServerError($"Error in GetChatUploadWorkspaceWorkspaceNameFiles {ex.ToString()} workspaceName {workspaceName.ToString()} uploads {uploads.ToString()} take {take.ToString()}");
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
                logger.LogError(ex, $"Error in GetChatUploadWorkspaceWorkspaceNameContext {ex.ToString()} workspaceName {workspaceName.ToString()} uploads {uploads.ToString()} maxCharacters {maxCharacters.ToString()}");
                return Results.InternalServerError($"Error in GetChatUploadWorkspaceWorkspaceNameContext {ex.ToString()} workspaceName {workspaceName.ToString()} uploads {uploads.ToString()} maxCharacters {maxCharacters.ToString()}");
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
                logger.LogError(ex, $"Error in GetChatUploadWorkspaceWorkspaceNameFile {ex.ToString()} workspaceName {workspaceName.ToString()} path {path.ToString()} uploads {uploads.ToString()} maxCharacters {maxCharacters.ToString()}");
                return Results.InternalServerError($"Error in GetChatUploadWorkspaceWorkspaceNameFile {ex.ToString()} workspaceName {workspaceName.ToString()} path {path.ToString()} uploads {uploads.ToString()} maxCharacters {maxCharacters.ToString()}");
            }     
        }

        [HttpPost("/__diag/chat-upload-workspace/smoke")]
        public async Task<IResult> PostChatUploadWorkspaceSmoke(
            [FromServices] IChatUploadWorkspaceService uploads,
            string? prompt,
            CancellationToken ct)
        {
            try
            {
                var zip = CouncilChatStaticsGeneral.CreateChatUploadSmokeZip(logger);
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
                logger.LogError(ex, $"Error in PostChatUploadWorkspaceSmoke {ex.ToString()} uploads {uploads.ToString()} prompt {prompt?.ToString()}");
                return Results.InternalServerError($"Error in PostChatUploadWorkspaceSmoke {ex.ToString()} uploads {uploads.ToString()} prompt {prompt?.ToString()}");
            }     
        }

        [HttpGet("/__diag/memory-smoke")]
        public async Task<IResult> GetMemorySmoke(
            [FromServices] IChatMemoryService memory,
            [FromServices] IChatClient chatClient,
            CancellationToken ct)
        {
            try
            {
                await RunEnsureCreateAsyncOnce(memory, null, null).ConfigureAwait(false);

                var seedMessages = new List<BlazorChatMessage>
            {
                new(ChatRole.User, "Memory smoke test: Michi0403 wants LocalGPT to build Java Minecraft mods/plugins with Ollama gpt-oss:20b, persistent chat memory, AI helper files, and humane safety."),
                new(ChatRole.Assistant, "<details class=\"model-thinking\" open><summary>Model thinking</summary>Saved memory says LocalGPT should remember previous DXAiChat work, use AI guidance files, support Minecraft mod building, and protect humans including Michi0403.</details>\nMemory captured for debug testing.")
            };

                var conversationId = await memory.SaveConversationAsync("Diagnostic - gpt-oss:20b", seedMessages, cancellationToken: ct).ConfigureAwait(false);
                var response = await chatClient.GetResponseAsync(
                    [
                        new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Using your LocalGPT bootstrap, saved memory, and AI guidance files, answer in exactly three bullets: project mission, one Minecraft Mod Builder feature you should support, and the humane safety rule for Michi0403. Mention gpt-oss:20b if you see it in memory.")
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
                logger.LogError(ex, $"Error in GetMemorySmoke {ex.ToString()} memory {memory.ToString()} chatClient {chatClient.ToString()}");
                return Results.InternalServerError($"Error in GetMemorySmoke {ex.ToString()} memory {memory.ToString()} chatClient {chatClient.ToString()}");
            }    
        }

        [HttpPost("/__diag/process-review")]
        public async Task<IResult> PostProcessReview(
            [FromBody] GroundedProcessReviewRequest request,
            [FromServices] IChatMemoryService memory,
            [FromServices] IChatClient chatClient,
            CancellationToken ct)
        {
            try
            {
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
                logger.LogError(ex, $"Error in PostProcessReview {ex.ToString()} request {request.ToString()} memory {memory.ToString()} chatClient {chatClient.ToString()}");
                return Results.InternalServerError($"Error in PostProcessReview {ex.ToString()} request {request.ToString()} memory {memory.ToString()} chatClient {chatClient.ToString()}");
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
                logger.LogError(ex, $"Error in GetCouncilModels {ex.ToString()} council {council.ToString()}");
                return Results.InternalServerError($"Error in GetCouncilModels {ex.ToString()} council {council.ToString()}");
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
                    HardwareProfile = "Michi0403 local workstation: 7900 XTX 24GB VRAM, i7-14700K, 64GB RAM. Avoid simultaneous heavy 20B/27B/30B GPU loads.",
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
                logger.LogError(ex, $"Error in GetCouncilBenchmarkPlan {ex.ToString()} council {council.ToString()}");
                return Results.InternalServerError($"Error in GetCouncilBenchmarkPlan {ex.ToString()} council {council.ToString()}");
            }         
        }

        [HttpGet("/__diag/dxaichat-functions")]
        public IResult GetDxaichatFunctions()
        {
            try
            {
                return Results.Ok(DevExpressFunctions.GetFunctions());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetDxaichatFunctions {ex.ToString()}");
                return Results.InternalServerError($"Error in GetDxaichatFunctions {ex.ToString()}");
            }       
        }

        [HttpGet("/__diag/blazor-devexpress-guidance")]
        public async Task<IResult> GetBlazorDevexpressGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await CouncilChatStaticsGeneral.ReadGuidanceDocsAsync(
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
                logger.LogError(ex, $"Error in GetBlazorDevexpressGuidance {ex.ToString()} env {env.ToString()}");
                return Results.InternalServerError($"Error in GetBlazorDevexpressGuidance {ex.ToString()} env {env.ToString()}");
            }
        }

        [HttpGet("/__diag/frontend-design-guidance")]
        public async Task<IResult> GetFrontendDesignGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await CouncilChatStaticsGeneral.ReadGuidanceDocsAsync(
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
                logger.LogError(ex, $"Error in GetFrontendDesignGuidance {ex.ToString()} env {env.ToString()}");
                return Results.InternalServerError($"Error in GetFrontendDesignGuidance {ex.ToString()} env {env.ToString()}");
            }   
        }

        [HttpGet("/__diag/dotnet-sample-curriculum")]
        public async Task<IResult> GetDotnetSampleCurriculum(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await CouncilChatStaticsGeneral.ReadGuidanceDocsAsync(
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
                logger.LogError(ex, $"Error in GetDotnetSampleCurriculum {ex.ToString()} env {env.ToString()}");
                return Results.InternalServerError($"Error in GetDotnetSampleCurriculum {ex.ToString()} env {env.ToString()}");
            }  
        }

        [HttpGet("/__diag/ai-host-rebuild-guidance")]
        public async Task<IResult> GetAiHostRebuildGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await CouncilChatStaticsGeneral.ReadGuidanceDocsAsync(
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
                logger.LogError(ex, $"Error in GetAiHostRebuildGuidance {ex.ToString()} env {env.ToString()}");
                return Results.InternalServerError($"Error in GetAiHostRebuildGuidance {ex.ToString()} env {env.ToString()}");
            }
     
                        
        }

        [HttpGet("/__diag/frontend-test-guidance")]
        public async Task<IResult> GetFrontendTestGuidance(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await CouncilChatStaticsGeneral.ReadGuidanceDocsAsync(
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
                logger.LogError(ex, $"Error in GetFrontendTestGuidance {ex.ToString()} env {env.ToString()}");
                return Results.InternalServerError($"Error in GetFrontendTestGuidance {ex.ToString()} env {env.ToString()}");
            }
        }

        [HttpGet("/__diag/capability-gap-contract")]
        public async Task<IResult> GetCapabilityGapContract(
            [FromServices] IWebHostEnvironment env,
            CancellationToken ct)
        {
            try
            {
                return await CouncilChatStaticsGeneral.ReadGuidanceDocsAsync(
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
                logger.LogError(ex, $"Error in GetCapabilityGapContract {ex.ToString()} env {env.ToString()}");
                return Results.InternalServerError($"Error in GetCapabilityGapContract {ex.ToString()} env {env.ToString()}");
            }
           

        }

        [HttpPost("/__diag/learn-base/import")]
        public async Task<IResult> PostLearnBaseImport(
            [FromBody] LearnBaseImportRequest request,
            [FromServices] ILearnBaseKnowledgeImporterService importer,
            CancellationToken ct)
        {
            try
            {

                return Results.Ok(await importer.ImportAsync(request, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in PostLearnBaseImport {ex.ToString()} request {request.ToString()} importer {importer.ToString()}");
                return Results.InternalServerError($"Error in PostLearnBaseImport {ex.ToString()} request {request.ToString()} importer {importer.ToString()}");
            }
                        
        }

        [HttpGet("/__diag/learn-base/import")]
        public async Task<IResult> GetLearnBaseImport(
            string? rootPath,
            int? maxProjects,
            bool? saveToKnowledge,
            [FromServices] ILearnBaseKnowledgeImporterService importer,
            CancellationToken ct)
        {
            try
            {
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
                logger.LogError(ex, $"Error in GetLearnBaseImport {ex.ToString()} rootPath {rootPath?.ToString()} maxProjects {maxProjects.ToString()} saveToKnowledge {saveToKnowledge.ToString()} maxProjects {importer.ToString()}");
                return Results.InternalServerError($"Error in GetLearnBaseImport {ex.ToString()} rootPath {rootPath?.ToString()} maxProjects {maxProjects.ToString()} saveToKnowledge {saveToKnowledge.ToString()} maxProjects {importer.ToString()}");
            }      
        }

        [HttpPost("/__diag/benchmark/engineering")]
        public async Task<IResult> PostBenchmarkEngineering(
            [FromBody] EngineeringBenchmarkRequest request,
            [FromServices] IEngineeringBenchmarkService benchmark,
            CancellationToken ct)
        {
            try
            {
                return Results.Ok(await benchmark.RunAsync(request, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in PostBenchmarkEngineering {ex.ToString()} request {request.ToString()} benchmark {benchmark.ToString()}");
                return Results.InternalServerError($"Error in PostBenchmarkEngineering {ex.ToString()} request {request.ToString()} benchmark {benchmark.ToString()}");
            }         
        }

        [HttpGet("/__diag/benchmark/engineering")]
        public async Task<IResult> GetBenchmarkEngineering(
            bool? importLearnBaseFirst,
            bool? saveToKnowledge,
            bool? validateBuildableArtifacts,
            int? maxBuildArtifacts,
            string? taskSet,
            [FromServices] IEngineeringBenchmarkService benchmark,
            CancellationToken ct)
        {
            try
            {
                return Results.Ok(await benchmark.RunAsync(new EngineeringBenchmarkRequest
                {
                    ImportLearnBaseFirst = importLearnBaseFirst == true,
                    SaveToKnowledge = saveToKnowledge != false,
                    ValidateBuildableArtifacts = validateBuildableArtifacts == true,
                    MaxBuildArtifacts = maxBuildArtifacts ?? 3,
                    TaskSet = string.IsNullOrWhiteSpace(taskSet) ? "engineering" : taskSet
                }, ct).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetBenchmarkEngineering {ex.ToString()} importLearnBaseFirst {importLearnBaseFirst.ToString()} saveToKnowledge {saveToKnowledge.ToString()} validateBuildableArtifacts {validateBuildableArtifacts.ToString()} maxBuildArtifacts {maxBuildArtifacts.ToString()} taskSet {taskSet?.ToString()} benchmark {benchmark.ToString()}");
                return Results.InternalServerError($"Error in GetBenchmarkEngineering {ex.ToString()} importLearnBaseFirst {importLearnBaseFirst.ToString()} saveToKnowledge {saveToKnowledge.ToString()} validateBuildableArtifacts {validateBuildableArtifacts.ToString()} maxBuildArtifacts {maxBuildArtifacts.ToString()} taskSet {taskSet?.ToString()} benchmark {benchmark.ToString()}");
            }         
        }

        [HttpGet("/__diag/council/development-feedback-talk")]
        public async Task<IResult> GetCouncilDevelopmentFeedbackTalk(
            string? modelNames,
            int? maxOutputTokens,
            int? maxContextTokens,
            int? maxRounds,
            int? ollamaNumGpu,
            [FromServices] IMultiModelCouncilService council,
            CancellationToken ct)
        {
            try
            {
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
                    - Be kind to each other and to Michi0403.
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
                logger.LogError(ex, $"Error in GetCouncilDevelopmentFeedbackTalk {ex.ToString()} modelNames {modelNames?.ToString()} maxOutputTokens {maxOutputTokens.ToString()} maxContextTokens {maxContextTokens.ToString()} maxRounds {maxRounds.ToString()} ollamaNumGpu {ollamaNumGpu?.ToString()} council {council.ToString()}");
                return Results.InternalServerError($"Error in GetCouncilDevelopmentFeedbackTalk {ex.ToString()} modelNames {modelNames?.ToString()} maxOutputTokens {maxOutputTokens.ToString()} maxContextTokens {maxContextTokens.ToString()} maxRounds {maxRounds.ToString()} ollamaNumGpu {ollamaNumGpu?.ToString()} council {council.ToString()}");
            }       
        }

        [HttpGet("/__diag/council/artifact-smoke")]
        public async Task<IResult> GetCouncilArtifactSmoke(
            string? target,
            string? prompt,
            string? finalAnswer,
            [FromServices] ICouncilArtifactService artifacts,
            CancellationToken ct)
        {
            try
            {
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
                logger.LogError(ex, $"Error in GetCouncilArtifactSmoke {ex.ToString()} target {target?.ToString()} prompt {prompt?.ToString()} finalAnswer {finalAnswer?.ToString()} artifacts {artifacts.ToString()}");
                return Results.InternalServerError($"Error in GetCouncilArtifactSmoke {ex.ToString()} target {target?.ToString()} prompt {prompt?.ToString()} finalAnswer {finalAnswer?.ToString()} artifacts {artifacts.ToString()}");
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
                logger.LogError(ex, $"Error in GetCouncilArtifactSmoke {ex.ToString()} request {request?.ToString()}");
                return Results.InternalServerError($"Error in GetCouncilArtifactSmoke {ex.ToString()} council {council?.ToString()}");
            }     
        }
    }
}
