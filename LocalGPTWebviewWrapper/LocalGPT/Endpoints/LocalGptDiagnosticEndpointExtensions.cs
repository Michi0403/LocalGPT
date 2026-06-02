using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
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
                            MaxContextTokens = 8192,
                            MaxOutputTokens = 4096,
                            Purpose = "Verify Harmony formatting, streaming, artifact links, and normal DXAiChat usability."
                        },
                        new
                        {
                            Name = "CPU-stable reviewer",
                            Members = preferredDeepseek is null ? Array.Empty<string>() : new[] { preferredDeepseek.ModelName },
                            MaxParallelModels = 1,
                            OllamaNumGpu = (int?)0,
                            MaxContextTokens = 4096,
                            MaxOutputTokens = 2048,
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
                            MaxContextTokens = 8192,
                            MaxOutputTokens = 4096,
                            Purpose = "Best default cross-check without concurrent VRAM pressure; use keep_alive=0s."
                        },
                        new
                        {
                            Name = "Heavy coder solo trial",
                            Members = preferredQwen is null ? Array.Empty<string>() : new[] { preferredQwen.ModelName },
                            MaxParallelModels = 1,
                            OllamaNumGpu = (int?)12,
                            MaxContextTokens = 8192,
                            MaxOutputTokens = 4096,
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
                        Path.Combine("docs", "AI_HOST_CONTROL_PLANE_ARCHITECTURE.md")
                    ],
                    """
                    Generate a local AI host .NET/ASP.NET Core/DevExpress Blazor control-plane app with a
                    recognizable left navigation shell, model catalog, chat, downloads, running models, API console,
                    templates, hardware, logs, diagnostics, settings, and representative provider-compatible API routes.
                    Generate a buildable milestone instead of refusing as too large.
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
                IEngineeringBenchmarkService benchmark,
                CancellationToken ct) =>
            {
                return Results.Ok(await benchmark.RunAsync(new EngineeringBenchmarkRequest
                {
                    ImportLearnBaseFirst = importLearnBaseFirst == true,
                    SaveToKnowledge = saveToKnowledge != false
                }, ct));
            });

            app.MapGet("/__diag/council/artifact-smoke", async (
                string? target,
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
                var request = new MultiModelCouncilRequest
                {
                    Prompt = isDatapack
                        ? "implementation-request smoke: generate a downloadable Minecraft Java 26.1 vanilla datapack zip named Benchmark Borough. The zip root must contain pack.mcmeta and data/ directly. Include load/tick tags, singular function folders, storage/scoreboard setup, city/register_banner, and validation notes."
                        : isLoaderMatrix
                        ? "implementation-request smoke: generate a downloadable Minecraft Java project skeleton distinction zip with separate Fabric, Paper, and NeoForge workspaces for Minecraft 26.1. Each loader must use its own metadata and Gradle conventions."
                        : isAiHostLab
                        ? "implementation-request smoke: generate a whole local AI host .NET 10 ASP.NET Core and DevExpress Blazor solution zip. Use only .NET, C#, Razor, and DevExpress Blazor. Include a left navigation shell, model catalog, chat, downloads, running models, API console, settings, logs, and selected provider-compatible API routes such as /api/version, /api/tags, /api/ps, /api/chat, and a safe non-inference /api/generate stub. Do not use Go and do not claim native GGML/GPU inference is implemented."
                        : isSolution
                        ? "implementation-request smoke: generate a whole LocalGPT/TacosPortalOpen-style .NET 10 Blazor DevExpress solution zip with .sln, .csproj, real .razor pages, css, service/model code, README, and manifest. The zip must be downloadable through /__artifacts/council/."
                        : isBlazor
                        ? "implementation-request smoke: generate a real .NET 10 Blazor server-interactive DevExpress Razor page for a LocalGPT backend health summary card. Include a service method idea, DxGrid, DxFormLayout, DxButton, DxCheckBox, and safe download guidance."
                        : "implementation-request smoke: generate a LocalGPT backend feature artifact.",
                    ModelNames = ["artifact-smoke"],
                    GenerateImplementationArtifact = true,
                    IncludeMemory = false,
                    SaveToMemory = false,
                    Title = "Deterministic council artifact smoke"
                };
                var result = new MultiModelCouncilResult
                {
                    Prompt = request.Prompt,
                    ModelNames = ["artifact-smoke"],
                    FinalAnswer = isDatapack
                        ? "Create a validated downloadable Benchmark Borough datapack. It must use Minecraft 26.1 pack_format 101.1, singular function folders, no wrapper zip folder, no .mcfunction.txt placeholders, and a visible register_banner debug line."
                        : isLoaderMatrix
                        ? "Create a loader matrix artifact with distinct Fabric, Paper, and NeoForge skeletons. Do not reuse Fabric metadata for Paper or NeoForge."
                        : isAiHostLab
                        ? "Create a downloadable .NET 10 ASP.NET Core and DevExpress Blazor AI host control-plane lab. Include a left navigation app shell, typed model catalog records, chat/download/running-model/API-console/settings/log pages, selected REST route stubs, README, manifest, and a prominent note that native inference is not implemented without a real backend."
                        : isSolution
                        ? "Create a whole downloadable .NET 10 Blazor/DevExpress solution artifact with project files, routable Razor pages, CSS, service/model code, README, manifest, and safe sandbox guidance. Do not self-integrate generated files into LocalGPT without user approval."
                        : isBlazor
                        ? "Create a real Razor page artifact using @page, @rendermode InteractiveServer, DevExpress controls, and an @code block. Also include compileable support code. Keep it sandboxed until the user approves integration."
                        : "Create a compileable backend support code artifact.",
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
