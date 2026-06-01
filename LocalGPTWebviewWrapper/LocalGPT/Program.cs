using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.Components;
using LocalGPT.Data;
using LocalGPT.Helper;
using LocalGPT.Hubs;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using TacosPortal.Services;
using static System.Net.Mime.MediaTypeNames;
namespace LocalGPT
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var app = BuildWebApp(args);

            app.Run();
        }
        public static int Port { get; private set; } = 0;
        public static WebApplication BuildWebApp(string[]? args = null)
        {
            // Put the content root/web root where the LocalGPT assembly lives.
            // This is crucial when you start the server from the WinUI process.
            var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

            //var options = new WebApplicationOptions
            //{
            //    ContentRootPath = exeDir,
            //    WebRootPath = Path.Combine(exeDir, "wwwroot"),
            //    Args = args ?? Array.Empty<string>()
            //};
            var options = new WebApplicationOptions
            {
                ApplicationName = typeof(Program).Assembly.GetName().Name, // "LocalGPT"
                ContentRootPath = exeDir,
                WebRootPath = Path.Combine(exeDir, "wwwroot"),
                Args = args ?? Array.Empty<string>()
            };
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger("Startup");
            var builder = WebApplication.CreateBuilder(options);
            var configuration = builder.Configuration;
            configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)

    .AddEnvironmentVariables();
            var configRoot = configuration.Get<LocalGPT.BusinessObjects.ConfigurationRoot>();
            builder.Services.AddLogging(
               logging => LoggingHelper.ConfigureCustomLoggersWithConsoleAndDebug(
                   logging,
                   builder.Services,
                   configuration));

            // Program.cs (only the AI registration part shown)
         
            builder.Services
                .AddOptions<LocalGPT.BusinessObjects.ConfigurationRoot>()
                .Bind(configuration);
            builder.Services.Configure<LocalGPT.BusinessObjects.ConfigurationRoot>(builder.Configuration);
            builder.Services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
            builder.Services.AddSingleton<IAiConnectivityProbe, AiConnectivityProbe>();
            builder.Services.AddSingleton<IAiFeatureReportService, AiFeatureReportService>();
            builder.Services.AddSingleton<ICouncilArtifactService, CouncilArtifactService>();
            builder.Services.AddSingleton<IProjectLibraryInventoryService, ProjectLibraryInventoryService>();
            builder.Services.AddSingleton<IBuildDebugInventoryService, BuildDebugInventoryService>();
            builder.Services.AddSingleton<IMinecraftModWorkspaceService, MinecraftModWorkspaceService>();
            builder.Services.AddScoped<INativeCommandRunner, NativeCommandRunner>();
            var memoryDbPath = EfChatMemoryService.GetDefaultDatabasePath();
            Directory.CreateDirectory(Path.GetDirectoryName(memoryDbPath)!);
            builder.Services.AddDbContextFactory<LocalGptMemoryDbContext>(options =>
                options.UseSqlite($"Data Source={memoryDbPath}"));
            builder.Services.AddScoped<IChatMemoryService, EfChatMemoryService>();
            builder.Services.AddScoped<IApplicationLogReaderService, ApplicationLogReaderService>();
            builder.Services.AddScoped<IAiContextBootstrapService, AiContextBootstrapService>();
            builder.Services.AddScoped<IMultiModelCouncilService, MultiModelCouncilService>();

            builder.Services.AddScoped<IChatClientFactory, ChatClientFactory>();
            // Build a fresh chat client per request/scope from the latest options
            builder.Services.AddScoped<IChatClient>(sp =>
            {
                var factory = sp.GetRequiredService<IChatClientFactory>();
                return factory.Build(); // returns CompositeChatClient
            });
            builder.Services.AddDevExpressAI();

            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.Configure<CircuitOptions>(
            o =>
o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30));
            //var builder = WebApplication.CreateBuilder();
            StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
            builder.Services.AddOptions();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSignalR()
              .AddMessagePackProtocol(options =>
              {
                  options.SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard
                      .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance)
                      .WithSecurity(MessagePack.MessagePackSecurity.UntrustedData);
              }).AddJsonProtocol(options =>
              {
                  options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                  options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                  options.PayloadSerializerOptions.WriteIndented = true;
                  options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                  options.PayloadSerializerOptions.IgnoreReadOnlyFields = false;
                  options.PayloadSerializerOptions.IgnoreReadOnlyProperties = false;
                  options.PayloadSerializerOptions.IncludeFields = false;
                  options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                  options.PayloadSerializerOptions.AllowTrailingCommas = true;
                  options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                  options.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;


              });
            Port = GetFreePort();
            builder.WebHost.UseKestrel().UseUrls($"http://127.0.0.1:{Port}");
            builder.Services.AddResponseCompression
               (opts =>
               {
                   opts.EnableForHttps = true;
                   opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] {
        "application/octet-stream"
   });
               });
                   //builder.Host.UseContentRoot(options.ContentRootPath);
                   //builder.WebHost.UseWebRoot(Path.Combine(options.WebRootPath, "wwwroot"));         // ensure /wwwroot is found

                   // 2) Load static web assets for THIS assembly (enables /_content/* and isolated CSS)
                   // Load static web assets (/_content/** and CSS isolation)
                   builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddHealthChecks();
            builder.Services.AddDevExpressBlazor(o => o.SizeMode = DevExpress.Blazor.SizeMode.Small);
            builder.Services.AddMvc();
            builder.Services.AddScoped<ThemeService>();

            builder.Services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.IgnoreReadOnlyFields = false;
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
                options.JsonSerializerOptions.IncludeFields = false;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
            });
            builder.Services.Configure<ForwardedHeadersOptions>(
                options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            builder.Services.AddDevExpressServerSideBlazorPdfViewer();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }
            _ = app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            app.UseHttpsRedirection();
            _ = app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseRouting();
            _ = app.UseResponseCompression();
            app.UseAntiforgery();                 // ✅ after routing, before endpoints
            app.MapControllers();
            _ = app.MapHub<ChatHub>("/chathub");
            app.MapHealthChecks("/health");
            app.MapGet("/__diag", (IWebHostEnvironment env) => new {
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
            app.MapPost("/__diag/dxaichat-smoke", async ([FromBody] DxaichatSmokeRequest request, IChatClient chatClient, IChatMemoryService memory, CancellationToken ct) =>
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
            app.MapGet("/__artifacts/council/{fileName}", (string fileName, ICouncilArtifactService artifacts) =>
            {
                var safeFileName = Path.GetFileName(fileName);
                if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal) ||
                    !safeFileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Invalid artifact file name.");

                var path = Path.Combine(artifacts.ArtifactRoot, safeFileName);
                if (!File.Exists(path))
                    return Results.NotFound();

                return Results.File(path, "text/plain; charset=utf-8", safeFileName);
            });
            app.MapGet("/__diag/logs", async (IApplicationLogReaderService logs, ILoggerFactory loggerFactory, string? minimumLevel, int? take, bool? writeSmoke, CancellationToken ct) =>
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
            app.MapPost("/__diag/process-review", async ([FromBody] GroundedProcessReviewRequest request, IChatMemoryService memory, IChatClient chatClient, CancellationToken ct) =>
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
                {
                    evidence.Append("- ").AppendLine(fact);
                }

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
            app.MapGet("/__diag/minecraft/workspace-smoke", async (IMinecraftModWorkspaceService workspaceService, string? loader, CancellationToken ct) =>
            {
                var request = new MinecraftModBuildRequest
                {
                    ProjectName = $"LivingCitiesSmoke{DateTime.UtcNow:HHmmss}",
                    ModId = "living_cities_smoke",
                    PackageName = "com.localgpt.livingcitiessmoke",
                    Loader = string.IsNullOrWhiteSpace(loader) ? "Fabric" : loader,
                    MinecraftVersion = "1.21.1",
                    JavaVersion = "21",
                    GradleVersion = "8.14.2",
                    Ide = "Eclipse",
                    IncludeLivingCitiesStarter = true,
                    Description = "Smoke-test the LocalGPT Minecraft Mod Builder with a small Living Cities starter item and report command."
                };

                var workspace = await workspaceService.CreateWorkspaceAsync(request, ct);
                return Results.Ok(new
                {
                    workspace.ProjectName,
                    workspace.RootPath,
                    workspace.MainClassPath,
                    workspace.MetadataPath,
                    workspace.BuildFilePath,
                    workspace.ReadmePath,
                    workspace.BuildCommand,
                    workspace.EclipseImportHint
                });
            });
            app.MapPost("/__diag/council", async ([FromBody] MultiModelCouncilRequest request, IMultiModelCouncilService council, CancellationToken ct) =>
            {
                return Results.Ok(await council.RunAsync(request, ct));
            });
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode()
               .AllowAnonymous();

            WriteRuntimeEndpointFile();
            return app;                           // ⬅️ no Run() here
        }

        private static void WriteRuntimeEndpointFile()
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "runtime");
                Directory.CreateDirectory(directory);

                var payload = new
                {
                    ProcessId = Environment.ProcessId,
                    BaseUrl = $"http://127.0.0.1:{Port}",
                    Port,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };

                File.WriteAllText(
                    Path.Combine(directory, "server.json"),
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // Diagnostics must never block app startup.
            }
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
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

      
