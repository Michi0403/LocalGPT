using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Endpoints
{
    public static class LocalGptDiagnosticEndpointExtensions
    {
        public static IEndpointRouteBuilder MapLocalGptDiagnosticEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/__diag", (IWebHostEnvironment env) => new
            {
                env.EnvironmentName,
                env.ContentRootPath,
                env.WebRootPath,
                AppAssembly = typeof(Program).Assembly.Location
            });

            app.MapGet("/__diag/ai-smoke", async (IChatClient chatClient, string? prompt, CancellationToken ct) =>
            {
                var response = await chatClient.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.User, string.IsNullOrWhiteSpace(prompt)
                            ? "Reply with exactly: LocalGPT DXAiChat backend test passed."
                            : prompt)
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = 2048
                    },
                    ct);

                return Results.Ok(new
                {
                    Text = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/ollama-compatible-smoke", async (
                string endpoint,
                string? model,
                string? prompt,
                int? numGpu,
                int? maxOutputTokens,
                CancellationToken ct) =>
            {
                var normalizedEndpoint = string.IsNullOrWhiteSpace(endpoint)
                    ? "http://127.0.0.1:11434"
                    : endpoint.TrimEnd('/');
                var modelName = string.IsNullOrWhiteSpace(model) ? "gpt-oss:20b" : model.Trim();
                using var client = new OllamaThinkingChatClient(
                    new OllamaCoreOptions { Uri = normalizedEndpoint, ModelName = modelName },
                    keepAlive: "0s",
                    contextLength: 2048,
                    timeout: TimeSpan.FromMinutes(5),
                    numGpu: numGpu ?? 0);

                var response = await client.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.User, string.IsNullOrWhiteSpace(prompt)
                            ? "Reply with exactly: LocalGPT Ollama-compatible endpoint smoke passed."
                            : prompt)
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(maxOutputTokens ?? 128, 64, 4096),
                        Temperature = 0.1f
                    },
                    ct);

                return Results.Ok(new
                {
                    Source = "LocalGPT OllamaThinkingChatClient",
                    Endpoint = normalizedEndpoint,
                    Model = modelName,
                    NumGpu = numGpu ?? 0,
                    Text = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapPost("/__diag/dxaichat-smoke", async (
                [FromBody] DxaichatSmokeRequest request,
                IChatClient chatClient,
                IChatMemoryService memory,
                CancellationToken ct) =>
            {
                var prompt = string.IsNullOrWhiteSpace(request.Prompt)
                    ? "Reply with exactly: LocalGPT DXAiChat configured-client smoke test passed."
                    : request.Prompt.Trim();

                var messages = new List<ChatMessage>();
                if (request.IncludeDiagnosticSystemPrompt)
                {
                    messages.Add(new ChatMessage(ChatRole.System, """
                        You are being called through LocalGPT's configured IChatClient, the same backend service used by the DXAiChat page.
                        This is a diagnostic smoke test, not direct Ollama access.
                        Keep the visible answer concise, mark uncertain claims as "Needs verification", and do not claim UI behavior was tested unless the prompt says it was.
                        """));
                }

                messages.Add(new ChatMessage(ChatRole.User, prompt));

                var response = await chatClient.GetResponseAsync(
                    messages,
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(request.MaxOutputTokens, 256, 4096),
                        Temperature = 0.2f
                    },
                    ct);

                await memory.EnsureCreatedAsync(ct);

                Guid? savedConversationId = null;
                if (request.SaveToMemory)
                {
                    savedConversationId = await memory.SaveConversationAsync(
                        string.IsNullOrWhiteSpace(request.Title) ? "Diagnostic - DXAiChat configured client" : request.Title.Trim(),
                        [
                            new BlazorChatMessage(ChatRole.User, prompt, new List<AIChatUploadFileInfo>()),
                            new BlazorChatMessage(ChatRole.Assistant, response.Text, new List<AIChatUploadFileInfo>())
                        ],
                        cancellationToken: ct);
                }

                var thinking = ExtractModelThinking(response.Text);
                var visibleText = StripModelThinking(response.Text);
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
            });

            app.MapGet("/__diag/memory", async (IChatMemoryService memory, CancellationToken ct) =>
            {
                await memory.EnsureCreatedAsync(ct);
                var conversations = await memory.GetConversationsAsync(20, ct);
                var thoughts = await memory.GetRecentThoughtsAsync(5, ct);

                return Results.Ok(new
                {
                    memory.DatabasePath,
                    ConversationCount = conversations.Count,
                    RecentThoughtCount = thoughts.Count,
                    Conversations = conversations,
                    RecentThoughts = thoughts
                });
            });

            app.MapGet("/__artifacts/council/{fileName}", (
                string fileName,
                ICouncilArtifactService artifacts,
                HttpContext httpContext) =>
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
                if (!File.Exists(path))
                    return Results.NotFound();

                httpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{safeFileName}\"";
                httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
                var contentType = isZip
                    ? "application/zip"
                    : isDll ? "application/octet-stream" : "text/plain; charset=utf-8";
                return Results.File(path, contentType, safeFileName);
            });

            app.MapGet("/__diag/logs", async (
                IApplicationLogReaderService logs,
                ILoggerFactory loggerFactory,
                string? minimumLevel,
                int? take,
                bool? writeSmoke,
                CancellationToken ct) =>
            {
                await logs.EnsureCreatedAsync(ct);
                var parsedLevel = Enum.TryParse<LogLevel>(minimumLevel, ignoreCase: true, out var level)
                    ? level
                    : LogLevel.Warning;

                if (writeSmoke == true)
                {
                    loggerFactory
                        .CreateLogger("LocalGPT.Diagnostics.DatabaseLoggerSmoke")
                        .LogWarning("SQLite database logger smoke test warning. This entry verifies async application log persistence.");
                    await Task.Delay(TimeSpan.FromSeconds(4), ct);
                }

                var recent = await logs.GetRecentAsync(parsedLevel, take ?? 30, ct);
                var briefing = await logs.BuildAiLogBriefingAsync(parsedLevel, Math.Min(take ?? 8, 20), ct);
                return Results.Ok(new
                {
                    logs.DatabasePath,
                    MinimumLevel = parsedLevel.ToString(),
                    Count = recent.Count,
                    Recent = recent,
                    AiBriefing = briefing,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/knowledge", async (
                ICouncilKnowledgeService knowledge,
                bool? includeArchived,
                int? take,
                CancellationToken ct) =>
            {
                await knowledge.EnsureCreatedAsync(ct);
                var entries = await knowledge.GetEntriesAsync(includeArchived == true, take ?? 50, ct);
                return Results.Ok(new
                {
                    knowledge.DatabasePath,
                    Count = entries.Count,
                    Entries = entries,
                    Briefing = await knowledge.BuildKnowledgeBriefingAsync(Math.Min(take ?? 8, 20), ct),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/sqlite/tables", async (
                IChatMemoryService memory,
                IApplicationLogReaderService logs,
                ICouncilKnowledgeService knowledge,
                ISqliteTableEditorService tableEditor,
                CancellationToken ct) =>
            {
                await memory.EnsureCreatedAsync(ct);
                await logs.EnsureCreatedAsync(ct);
                await knowledge.EnsureCreatedAsync(ct);
                var tables = await tableEditor.GetTablesAsync(ct);
                return Results.Ok(new
                {
                    tableEditor.DatabasePath,
                    Count = tables.Count,
                    Tables = tables,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/sqlite/table/{tableName}", async (
                string tableName,
                int? take,
                IChatMemoryService memory,
                IApplicationLogReaderService logs,
                ICouncilKnowledgeService knowledge,
                ISqliteTableEditorService tableEditor,
                CancellationToken ct) =>
            {
                await memory.EnsureCreatedAsync(ct);
                await logs.EnsureCreatedAsync(ct);
                await knowledge.EnsureCreatedAsync(ct);
                return Results.Ok(await tableEditor.GetTableAsync(tableName, take ?? 100, ct));
            });

            app.MapGet("/__diag/devexpress", async (IProjectLibraryInventoryService inventory, CancellationToken ct) =>
            {
                return Results.Ok(new
                {
                    Briefing = await inventory.BuildDevExpressBriefingAsync(ct),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/build-debug-files", async (IBuildDebugInventoryService inventory, bool? copy, CancellationToken ct) =>
            {
                var result = await inventory.CaptureAsync(copy == true, ct);
                return Results.Ok(new
                {
                    result.ArtifactRoot,
                    result.CopiedFiles,
                    result.CapturedAtUtc,
                    Count = result.Files.Count,
                    Files = result.Files,
                    Briefing = await inventory.BuildBriefingAsync(ct)
                });
            });

            app.MapGet("/__diag/artifact-workspaces", (
                ICouncilArtifactService artifacts,
                HttpContext httpContext,
                int? take) =>
            {
                var workspaces = EnumerateArtifactWorkspaces(artifacts.ArtifactRoot, take ?? 20);
                var baseUrl = GetRequestBaseUrl(httpContext);
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
            });

            app.MapGet("/__diag/artifact-workspace/{workspaceName}/files", (
                string workspaceName,
                ICouncilArtifactService artifacts,
                int? take) =>
            {
                var workspace = ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    Files = EnumerateWorkspaceTextFiles(workspace, take ?? 250),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/artifact-workspace/{workspaceName}/file", async (
                string workspaceName,
                string path,
                ICouncilArtifactService artifacts,
                CancellationToken ct) =>
            {
                var workspace = ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var file = ResolveWorkspaceTextFile(workspace, path, allowMissing: false);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid, unsupported, or missing source file path." });

                var info = new FileInfo(file);
                if (info.Length > MaxArtifactTextFileBytes)
                    return Results.BadRequest(new { Error = "File is too large for inline source editing.", info.Length });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = ToForwardSlash(Path.GetRelativePath(workspace, file)),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Content = await File.ReadAllTextAsync(file, ct),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapPost("/__diag/artifact-workspace/{workspaceName}/file", async (
                string workspaceName,
                [FromBody] ArtifactWorkspaceFileSaveRequest request,
                ICouncilArtifactService artifacts,
                CancellationToken ct) =>
            {
                var workspace = ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var content = request.Content ?? string.Empty;
                if (Encoding.UTF8.GetByteCount(content) > MaxArtifactTextFileBytes)
                    return Results.BadRequest(new { Error = "File content is too large for inline source editing." });

                var file = ResolveWorkspaceTextFile(workspace, request.RelativePath, allowMissing: true);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid or unsupported source file path." });

                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? workspace);
                await File.WriteAllTextAsync(file, content, ct);
                var info = new FileInfo(file);
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = ToForwardSlash(Path.GetRelativePath(workspace, file)),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Message = "Source file saved. Run the generated project build or refresh the workspace zip before handing it to a user.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/artifact-workspace/{workspaceName}/zip", (
                string workspaceName,
                ICouncilArtifactService artifacts,
                HttpContext httpContext) =>
            {
                var workspace = ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var zipName = $"{workspaceName}-workspace.zip";
                var zipPath = Path.Combine(artifacts.ArtifactRoot, zipName);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(workspace, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: true);
                var downloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}";
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    ZipPath = zipPath,
                    DownloadUrl = downloadUrl,
                    AbsoluteDownloadUrl = new Uri(new Uri(GetRequestBaseUrl(httpContext)), downloadUrl).ToString(),
                    Message = "Workspace zip refreshed from the current source directory.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/memory-smoke", async (IChatMemoryService memory, IChatClient chatClient, CancellationToken ct) =>
            {
                await memory.EnsureCreatedAsync(ct);

                var seedMessages = new List<BlazorChatMessage>
                {
                    new(ChatRole.User, "Memory smoke test: Michi0403 wants LocalGPT to build Java Minecraft mods/plugins with Ollama gpt-oss:20b, persistent chat memory, AI helper files, and humane safety."),
                    new(ChatRole.Assistant, "<details class=\"model-thinking\" open><summary>Model thinking</summary>Saved memory says LocalGPT should remember previous DXAiChat work, use AI guidance files, support Minecraft mod building, and protect humans including Michi0403.</details>\nMemory captured for debug testing.")
                };

                var conversationId = await memory.SaveConversationAsync("Diagnostic - gpt-oss:20b", seedMessages, cancellationToken: ct);
                var response = await chatClient.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.User, "Using your LocalGPT bootstrap, saved memory, and AI guidance files, answer in exactly three bullets: project mission, one Minecraft Mod Builder feature you should support, and the humane safety rule for Michi0403. Mention gpt-oss:20b if you see it in memory.")
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = 1024
                    },
                    ct);

                return Results.Ok(new
                {
                    SavedConversationId = conversationId,
                    Conversations = await memory.GetConversationsAsync(5, ct),
                    RecentThoughts = await memory.GetRecentThoughtsAsync(5, ct),
                    Response = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapPost("/__diag/process-review", async (
                [FromBody] GroundedProcessReviewRequest request,
                IChatMemoryService memory,
                IChatClient chatClient,
                CancellationToken ct) =>
            {
                await memory.EnsureCreatedAsync(ct);

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

                var conversations = await memory.GetConversationsAsync(5, ct);
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
                        new ChatMessage(ChatRole.User, prompt)
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(request.MaxOutputTokens, 256, 4096)
                    },
                    ct);

                return Results.Ok(new
                {
                    Evidence = evidence.ToString(),
                    Response = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapGet("/__diag/council/models", async (IMultiModelCouncilService council, CancellationToken ct) =>
            {
                return Results.Ok(await council.GetCandidatesAsync(ct));
            });

            app.MapGet("/__diag/council/benchmark-plan", async (IMultiModelCouncilService council, CancellationToken ct) =>
            {
                var candidates = await council.GetCandidatesAsync(ct);
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
            });

            app.MapGet("/__diag/dxaichat-functions", () =>
            {
                return Results.Ok(DxaichatFunctionCatalog.GetFunctions());
            });

            app.MapGet("/__diag/blazor-devexpress-guidance", async (IWebHostEnvironment env, CancellationToken ct) =>
            {
                return await ReadGuidanceDocsAsync(
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
                    ct);
            });

            app.MapGet("/__diag/frontend-design-guidance", async (IWebHostEnvironment env, CancellationToken ct) =>
            {
                return await ReadGuidanceDocsAsync(
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
                    ct);
            });

            app.MapGet("/__diag/dotnet-sample-curriculum", async (IWebHostEnvironment env, CancellationToken ct) =>
            {
                return await ReadGuidanceDocsAsync(
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
                    ct);
            });

            app.MapGet("/__diag/ai-host-rebuild-guidance", async (IWebHostEnvironment env, CancellationToken ct) =>
            {
                return await ReadGuidanceDocsAsync(
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
                    ct);
            });

            app.MapGet("/__diag/frontend-test-guidance", async (IWebHostEnvironment env, CancellationToken ct) =>
            {
                return await ReadGuidanceDocsAsync(
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
                    ct);
            });

            app.MapGet("/__diag/capability-gap-contract", async (IWebHostEnvironment env, CancellationToken ct) =>
            {
                return await ReadGuidanceDocsAsync(
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
                    ct);
            });

            app.MapPost("/__diag/learn-base/import", async (
                [FromBody] LearnBaseImportRequest request,
                ILearnBaseKnowledgeImporterService importer,
                CancellationToken ct) =>
            {
                return Results.Ok(await importer.ImportAsync(request, ct));
            });

            app.MapGet("/__diag/learn-base/import", async (
                string? rootPath,
                int? maxProjects,
                bool? saveToKnowledge,
                ILearnBaseKnowledgeImporterService importer,
                CancellationToken ct) =>
            {
                return Results.Ok(await importer.ImportAsync(new LearnBaseImportRequest
                {
                    RootPath = string.IsNullOrWhiteSpace(rootPath)
                        ? @"C:\tmpselectedcodexlearnbaseforlocalgpt"
                        : rootPath,
                    MaxProjects = maxProjects ?? 40,
                    SaveToKnowledge = saveToKnowledge != false
                }, ct));
            });

            app.MapPost("/__diag/benchmark/engineering", async (
                [FromBody] EngineeringBenchmarkRequest request,
                IEngineeringBenchmarkService benchmark,
                CancellationToken ct) =>
            {
                return Results.Ok(await benchmark.RunAsync(request, ct));
            });

            app.MapGet("/__diag/benchmark/engineering", async (
                bool? importLearnBaseFirst,
                bool? saveToKnowledge,
                bool? validateBuildableArtifacts,
                int? maxBuildArtifacts,
                string? taskSet,
                IEngineeringBenchmarkService benchmark,
                CancellationToken ct) =>
            {
                return Results.Ok(await benchmark.RunAsync(new EngineeringBenchmarkRequest
                {
                    ImportLearnBaseFirst = importLearnBaseFirst == true,
                    SaveToKnowledge = saveToKnowledge != false,
                    ValidateBuildableArtifacts = validateBuildableArtifacts == true,
                    MaxBuildArtifacts = maxBuildArtifacts ?? 3,
                    TaskSet = string.IsNullOrWhiteSpace(taskSet) ? "engineering" : taskSet
                }, ct));
            });

            app.MapGet("/__diag/council/development-feedback-talk", async (
                string? modelNames,
                int? maxOutputTokens,
                int? maxContextTokens,
                int? maxRounds,
                int? ollamaNumGpu,
                IMultiModelCouncilService council,
                CancellationToken ct) =>
            {
                var requestedModels = (modelNames ?? string.Empty)
                    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(model => !string.IsNullOrWhiteSpace(model))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(4)
                    .ToList();

                if (requestedModels.Count < 2)
                {
                    var candidates = await council.GetCandidatesAsync(ct);
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

                return Results.Ok(await council.RunAsync(request, ct));
            });

            app.MapGet("/__diag/council/artifact-smoke", async (
                string? target,
                string? prompt,
                string? finalAnswer,
                ICouncilArtifactService artifacts,
                CancellationToken ct) =>
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

                var generated = await artifacts.CreateImplementationArtifactsAsync(request, result, ct);
                return Results.Ok(new
                {
                    Target = isDatapack ? "datapack" : isLoaderMatrix ? "loader-matrix" : isAiHostLab ? "ai-host" : isSolution ? "solution" : isBlazor ? "blazor" : target,
                    artifacts.ArtifactRoot,
                    Count = generated.Count,
                    Artifacts = generated,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            });

            app.MapPost("/__diag/council", async (
                [FromBody] MultiModelCouncilRequest request,
                IMultiModelCouncilService council,
                CancellationToken ct) =>
            {
                return Results.Ok(await council.RunAsync(request, ct));
            });

            return app;
        }

        private const long MaxArtifactTextFileBytes = 2 * 1024 * 1024;

        private static readonly HashSet<string> ArtifactTextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".razor",
            ".cshtml",
            ".csproj",
            ".sln",
            ".props",
            ".targets",
            ".md",
            ".txt",
            ".json",
            ".xml",
            ".css",
            ".scss",
            ".js",
            ".ts",
            ".yml",
            ".yaml",
            ".ps1",
            ".sql",
            ".html",
            ".htm",
            ".mcfunction",
            ".mcmeta",
            ".toml",
            ".properties",
            ".java"
        };

        private static string GetRequestBaseUrl(HttpContext httpContext)
        {
            var request = httpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }

        private static IReadOnlyList<ArtifactWorkspaceSummary> EnumerateArtifactWorkspaces(string artifactRoot, int take)
        {
            if (!Directory.Exists(artifactRoot))
                return [];

            return Directory
                .EnumerateDirectories(artifactRoot)
                .Select(path => BuildArtifactWorkspaceSummary(artifactRoot, path))
                .Where(summary => summary is not null)
                .Cast<ArtifactWorkspaceSummary>()
                .OrderByDescending(summary => summary.LastWriteTimeUtc)
                .Take(Math.Clamp(take, 1, 100))
                .ToList();
        }

        private static ArtifactWorkspaceSummary? BuildArtifactWorkspaceSummary(string artifactRoot, string workspacePath)
        {
            try
            {
                var directory = new DirectoryInfo(workspacePath);
                var files = EnumerateWorkspaceTextFiles(workspacePath, 500);
                var zipNames = Directory
                    .EnumerateFiles(artifactRoot, "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Where(name => name!.StartsWith(directory.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name!)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new ArtifactWorkspaceSummary(
                    directory.Name,
                    directory.FullName,
                    directory.LastWriteTimeUtc,
                    files.Count,
                    files.Count(file => file.RelativePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)),
                    files.Count(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)),
                    zipNames);
            }
            catch
            {
                return null;
            }
        }

        private static List<ArtifactWorkspaceFileSummary> EnumerateWorkspaceTextFiles(string workspaceRoot, int take)
        {
            if (!Directory.Exists(workspaceRoot))
                return [];

            return Directory
                .EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
                .Where(IsSupportedArtifactTextFile)
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new ArtifactWorkspaceFileSummary(
                        ToForwardSlash(Path.GetRelativePath(workspaceRoot, path)),
                        info.Length,
                        info.LastWriteTimeUtc);
                })
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(take, 1, 1000))
                .ToList();
        }

        private static string? ResolveArtifactWorkspace(string artifactRoot, string workspaceName)
        {
            var safeName = Path.GetFileName(workspaceName);
            if (!string.Equals(workspaceName, safeName, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(safeName))
            {
                return null;
            }

            var root = Path.GetFullPath(artifactRoot);
            var path = Path.GetFullPath(Path.Combine(root, safeName));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(path))
            {
                return null;
            }

            return path;
        }

        private static string? ResolveWorkspaceTextFile(string workspaceRoot, string relativePath, bool allowMissing)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalizedRelativePath))
                return null;

            var root = Path.GetFullPath(workspaceRoot);
            var path = Path.GetFullPath(Path.Combine(root, normalizedRelativePath));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                !IsSupportedArtifactTextFile(path))
            {
                return null;
            }

            return allowMissing || File.Exists(path) ? path : null;
        }

        private static bool IsSupportedArtifactTextFile(string path)
        {
            var extension = Path.GetExtension(path);
            return ArtifactTextExtensions.Contains(extension);
        }

        private static string ToForwardSlash(string path) =>
            path.Replace('\\', '/');

        private sealed record ArtifactWorkspaceSummary(
            string WorkspaceName,
            string RootPath,
            DateTime LastWriteTimeUtc,
            int SourceFileCount,
            int RazorFileCount,
            int CSharpFileCount,
            IReadOnlyList<string> ZipNames);

        private sealed record ArtifactWorkspaceFileSummary(
            string RelativePath,
            long Length,
            DateTime LastWriteTimeUtc);

        private sealed record ArtifactWorkspaceFileSaveRequest(
            string RelativePath,
            string? Content);

        private static async Task<IResult> ReadGuidanceDocsAsync(
            IWebHostEnvironment env,
            IReadOnlyList<string> relativePaths,
            string fallbackBriefing,
            CancellationToken cancellationToken)
        {
            var foundFiles = new List<object>();
            var briefing = new StringBuilder();

            foreach (var relativePath in relativePaths)
            {
                var candidatePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, relativePath),
                    Path.Combine(env.ContentRootPath, relativePath),
                    Path.Combine(Directory.GetCurrentDirectory(), relativePath)
                }.Distinct(StringComparer.OrdinalIgnoreCase);

                var path = candidatePaths.FirstOrDefault(File.Exists);
                if (path is null)
                    continue;

                var text = await File.ReadAllTextAsync(path, cancellationToken);
                foundFiles.Add(new
                {
                    RelativePath = relativePath.Replace('\\', '/'),
                    SourcePath = path
                });

                briefing
                    .Append("# ")
                    .AppendLine(Path.GetFileName(relativePath))
                    .AppendLine()
                    .AppendLine(text.Trim())
                    .AppendLine();
            }

            return Results.Ok(new
            {
                GuidanceFiles = foundFiles,
                Briefing = foundFiles.Count > 0
                    ? briefing.ToString().Trim()
                    : fallbackBriefing.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private static string ExtractModelThinking(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var match = Regex.Match(
                content,
                "<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

            return match.Success
                ? WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim()
                : string.Empty;
        }

        private static string StripModelThinking(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            return Regex.Replace(
                    content,
                    "<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>",
                    string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)
                .Trim();
        }
    }
}
