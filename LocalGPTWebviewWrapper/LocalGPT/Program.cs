using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.Components;
using LocalGPT.Data;
using LocalGPT.Endpoints;
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
using System.IO.Compression;
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
            var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
            var logger = loggerFactory.CreateLogger("Startup");
            //EnsureGeneratedStaticWebAssetContentRoots(exeDir, logger);

            var builder = WebApplication.CreateBuilder(CreateWebApplicationOptions( args));
            TraceStartup("Created builder.");
            ConfigureAppConfiguration(builder);
            TraceStartup("Configured app configuration.");
            ConfigureLogging(builder);
            TraceStartup("Configured logging.");
            ConfigureOptionsAndServices(builder, logger);
            TraceStartup("Configured options and services.");
            ConfigureSignalR(builder.Services);
            TraceStartup("Configured SignalR.");
            ConfigureKestrel(builder);
            TraceStartup("Configured Kestrel.");
            ConfigureResponseCompression(builder.Services);
            TraceStartup("Configured response compression.");
            ConfigureBlazorAndMvc(builder);
            TraceStartup("Configured Blazor and MVC.");
            ConfigureJsonOptions(builder.Services);
            TraceStartup("Configured JSON options.");
            ConfigureForwardedHeaders(builder.Services);
            TraceStartup("Configured forwarded headers.");

            var app = builder.Build();
            TraceStartup("Built web application.");
            ConfigureMiddlewareAndEndpoints(app);
            TraceStartup("Configured middleware and endpoints.");
            WriteRuntimeEndpointFile();
            TraceStartup("Wrote runtime endpoint file.");

            return app;
        }

        private static void TraceStartup(string message)
        {
            var line = $"[{DateTimeOffset.Now:O}] pid={Environment.ProcessId} {message}{Environment.NewLine}";
            TryAppendStartupTrace(line);

            if (!string.Equals(
                Environment.GetEnvironmentVariable("LOCALGPT_STARTUP_TRACE"),
                "1",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Console.Write($"[LocalGPT startup] {line}");
        }

        private static void TryAppendStartupTrace(string line)
        {
            try
            {
                foreach (var directory in GetRuntimeTraceDirectories())
                {
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, $"startup-trace-{Environment.ProcessId}.log"), line);
                }
            }
            catch
            {
                // Startup tracing must never block app launch.
            }
        }

        private static IEnumerable<string> GetRuntimeTraceDirectories()
        {
            var directories = new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "runtime"),
                Path.Combine(
                    Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty,
                    "LocalGPT",
                    "runtime")
            };

            return directories
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static WebApplicationOptions CreateWebApplicationOptions( string[]? args)
        {
            return new WebApplicationOptions
            {
                ApplicationName = typeof(Program).Assembly.GetName().Name,
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot"),
                Args = args ?? Array.Empty<string>()
            };
        }

        private static void ConfigureAppConfiguration(WebApplicationBuilder builder)
        {
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{builder.Environment.EnvironmentName}.json",
                    optional: true,
                    reloadOnChange: true)
                .AddEnvironmentVariables();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            if (IsCustomLoggerBypassRequested())
            {
                builder.Services.AddLogging(logging =>
                {
                    logging.AddJsonConsole();
                    logging.AddConsole();
#if DEBUG
                    logging.AddDebug();
#endif
                });

                return;
            }

            builder.Services.AddLogging(logging =>
                LoggingHelper.ConfigureCustomLoggersWithConsoleAndDebug(
                    logging,
                    builder.Services,
                    builder.Configuration));
        }

        private static bool IsCustomLoggerBypassRequested()
        {
            return IsEnvironmentFlagEnabled("LOCALGPT_DISABLE_CUSTOM_LOGGERS") ||
                   IsEnvironmentFlagEnabled("LOCALGPT_E2E");
        }

        private static bool IsEnvironmentFlagEnabled(string name)
        {
            return string.Equals(
                Environment.GetEnvironmentVariable(name),
                "1",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ConfigureOptionsAndServices(WebApplicationBuilder builder, ILogger logger)
        {
            builder.Services
                .AddOptions<LocalGPT.BusinessObjects.ConfigurationRoot>()
                .Bind(builder.Configuration);
            builder.Services.Configure<LocalGPT.BusinessObjects.ConfigurationRoot>(builder.Configuration);

            builder.Services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
            builder.Services.AddSingleton<IAiConnectivityProbe, AiConnectivityProbe>();
            builder.Services.AddSingleton<IAiFeatureReportService, AiFeatureReportService>();
            builder.Services.AddSingleton<ICouncilArtifactService, CouncilArtifactService>();
            builder.Services.AddSingleton<IChatUploadWorkspaceService, ChatUploadWorkspaceService>();
            builder.Services.AddSingleton<IProjectLibraryInventoryService, ProjectLibraryInventoryService>();
            builder.Services.AddSingleton<IBuildDebugInventoryService, BuildDebugInventoryService>();
            builder.Services.AddSingleton<IMinecraftModWorkspaceService, MinecraftModWorkspaceService>();
            builder.Services.AddScoped<INativeCommandRunner, NativeCommandRunner>();

            var memoryDbPath = EfChatMemoryService.GetDefaultDatabasePath();
            Directory.CreateDirectory(Path.GetDirectoryName(memoryDbPath)!);
            TraceStartup($"Checking SQLite database health at {memoryDbPath}.");
            LocalGptDatabaseRecovery
                .EnsureHealthyOrRecoverAsync(memoryDbPath, logger)
                .GetAwaiter()
                .GetResult();
            TraceStartup("Finished SQLite database health check.");
            builder.Services.AddDbContextFactory<LocalGptMemoryDbContext>(options =>
                options.UseSqlite($"Data Source={memoryDbPath}"));

            builder.Services.AddScoped<IChatMemoryService, EfChatMemoryService>();
            builder.Services.AddScoped<IApplicationLogReaderService, ApplicationLogReaderService>();
            builder.Services.AddScoped<ICouncilKnowledgeService, CouncilKnowledgeService>();
            builder.Services.AddScoped<ISqliteTableEditorService, SqliteTableEditorService>();
            builder.Services.AddScoped<ILearnBaseKnowledgeImporterService, LearnBaseKnowledgeImporterService>();
            builder.Services.AddScoped<IEngineeringBenchmarkService, EngineeringBenchmarkService>();
            builder.Services.AddScoped<IAiContextBootstrapService, AiContextBootstrapService>();
            builder.Services.AddScoped<IMultiModelCouncilService, MultiModelCouncilService>();
            builder.Services.AddScoped<IChatClientFactory, ChatClientFactory>();
            builder.Services.AddScoped<IChatClient>(sp =>
                sp.GetRequiredService<IChatClientFactory>().Build());

            builder.Services.AddDevExpressAI();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.Configure<CircuitOptions>(options =>
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30));
            builder.Services.AddOptions();
            builder.Services.AddHttpContextAccessor();
        }

        private static void ConfigureSignalR(IServiceCollection services)
        {
            services.AddSignalR()
                .AddMessagePackProtocol(options =>
                {
                    options.SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard
                        .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance)
                        .WithSecurity(MessagePack.MessagePackSecurity.UntrustedData);
                })
                .AddJsonProtocol(options =>
                {
                    ConfigureSharedJsonSerializerOptions(options.PayloadSerializerOptions);
                    options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                });
        }

        private static void ConfigureKestrel(WebApplicationBuilder builder)
        {
            Port = GetFreePort();
            builder.WebHost.UseKestrel().UseUrls($"http://127.0.0.1:{Port}");
        }

        private static void ConfigureResponseCompression(IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                [
                    "application/octet-stream"
                ]);
            });
        }

        private static void ConfigureBlazorAndMvc(WebApplicationBuilder builder)
        {
            StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
        
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddHealthChecks();
            builder.Services.AddDevExpressBlazor(options => options.SizeMode = DevExpress.Blazor.SizeMode.Small);
            builder.Services.AddMvc();
            builder.Services.AddScoped<ThemeService>();
            builder.Services.AddDevExpressServerSideBlazorPdfViewer();
        }

        private static void ConfigureJsonOptions(IServiceCollection services)
        {
            services.Configure<JsonOptions>(options =>
            {
                ConfigureSharedJsonSerializerOptions(options.JsonSerializerOptions);
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        }

        private static void ConfigureSharedJsonSerializerOptions(JsonSerializerOptions options)
        {
            options.PropertyNameCaseInsensitive = true;
            options.WriteIndented = true;
            options.PropertyNamingPolicy = null;
            options.IgnoreReadOnlyFields = false;
            options.IgnoreReadOnlyProperties = false;
            options.IncludeFields = false;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.AllowTrailingCommas = true;
            options.Converters.Add(new JsonStringEnumConverter());
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.WriteAsString;
        }

        private static void ConfigureForwardedHeaders(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        private static void ConfigureMiddlewareAndEndpoints(WebApplication app)
        {
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
            // The bundled desktop/WebView host binds to a random HTTP loopback port.
            // HTTPS redirection has no target port there and only produces noisy startup warnings.
            _ = app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseRouting();
            _ = app.UseResponseCompression();
            app.UseAntiforgery();                 // ✅ after routing, before endpoints
            app.MapControllers();
            _ = app.MapHub<ChatHub>("/chathub");
            app.MapStaticAssets();
            app.MapHealthChecks("/health");
            app.MapLocalGptDiagnosticEndpoints();
            app.MapMinecraftDiagnosticEndpoints();
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode()
               .AllowAnonymous();
        }

        //private static void EnsureGeneratedStaticWebAssetContentRoots(string exeDir, ILogger logger)
        //{
        //    var assemblyName = typeof(Program).Assembly.GetName().Name;
        //    var manifestPath = Path.Combine(exeDir, $"{assemblyName}.staticwebassets.runtime.json");
        //    if (!File.Exists(manifestPath))
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        //        if (!manifest.RootElement.TryGetProperty("ContentRoots", out var contentRoots)
        //            || contentRoots.ValueKind != JsonValueKind.Array)
        //        {
        //            return;
        //        }

        //        foreach (var contentRoot in contentRoots.EnumerateArray())
        //        {
        //            if (contentRoot.ValueKind != JsonValueKind.String)
        //            {
        //                continue;
        //            }

        //            var path = contentRoot.GetString();
        //            if (string.IsNullOrWhiteSpace(path)
        //                || Directory.Exists(path)
        //                || !IsGeneratedStaticWebAssetRoot(path))
        //            {
        //                continue;
        //            }

        //            Directory.CreateDirectory(path);
        //            logger.LogInformation("Recreated missing generated static web asset root {Path}.", path);
        //        }
        //    }
        //    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        //    {
        //        logger.LogWarning(ex, "Could not inspect static web asset manifest {ManifestPath}.", manifestPath);
        //    }
        //}

        private static bool IsGeneratedStaticWebAssetRoot(string path)
        {
            var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
            if (!normalized.Contains(objSegment, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var trimmed = normalized.TrimEnd(Path.DirectorySeparatorChar);
            return trimmed.EndsWith($"{Path.DirectorySeparatorChar}compressed", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith(
                    $"{Path.DirectorySeparatorChar}scopedcss{Path.DirectorySeparatorChar}bundle",
                    StringComparison.OrdinalIgnoreCase);
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

    }
}
