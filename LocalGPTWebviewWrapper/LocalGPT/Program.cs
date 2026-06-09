using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.Components;
using LocalGPT.Endpoints;
using LocalGPT.Extensions.PlainStatics.CouncilData.Data;
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
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
            TraceStartup("Created builder.", logger);
            ConfigureAppConfiguration(builder, logger);
            TryAppendStartupTrace("Configured app configuration.", logger);
            ConfigureLogging(builder, logger);
            TraceStartup("Configured logging.", logger);
            ConfigureOptionsAndServices(builder, logger);
            TraceStartup("Configured options and services.", logger);
            ConfigureSignalR(builder.Services, logger);
            TraceStartup("Configured SignalR.", logger);
            ConfigureKestrel(builder, logger);
            TraceStartup("Configured Kestrel.", logger);
            ConfigureResponseCompression(builder.Services, logger);
            TraceStartup("Configured response compression.", logger);
            ConfigureBlazorAndMvc(builder, logger);
            TraceStartup("Configured Blazor and MVC.", logger);
            ConfigureJsonOptions(builder.Services, logger);
            TraceStartup("Configured JSON options.", logger);
            ConfigureForwardedHeaders(builder.Services, logger);
            TraceStartup("Configured forwarded headers.", logger);

            var app = builder.Build();
            TraceStartup("Built web application.", logger);
            ConfigureMiddlewareAndEndpoints(app, logger);
            TraceStartup("Configured middleware and endpoints.", logger);
            WriteRuntimeEndpointFile(logger);
            TraceStartup("Wrote runtime endpoint file.", logger);

            return app;
        }

        private static void TraceStartup(string message, ILogger logger)
        {
            try
            {
                var line = $"[{DateTimeOffset.Now:O}] pid={Environment.ProcessId} {message}{Environment.NewLine}";
                TryAppendStartupTrace(line, logger);

                if (!string.Equals(
                    Environment.GetEnvironmentVariable("LOCALGPT_STARTUP_TRACE"),
                    "1",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                TryAppendStartupTrace($"[LocalGPT startup] {line}", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TraceStartup {message}");
            }
           
        }

        private static void TryAppendStartupTrace(string line, ILogger logger)
        {
            try
            {
                foreach (var directory in GetRuntimeTraceDirectories())
                {
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, $"startup-trace-{Environment.ProcessId}.log"), line);
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"Error in TryAppendStartupTrace line {line}");
                TraceStartup(ex.ToString(), logger);
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

        private static void ConfigureAppConfiguration(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {
                builder.Configuration
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile(
                   $"appsettings.{builder.Environment.EnvironmentName}.json",
                   optional: true,
                   reloadOnChange: true)
               .AddEnvironmentVariables();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureAppConfiguration builder {builder.ToString()}",builder);
                TryAppendStartupTrace(ex.ToString(), logger);
            }
           
        }
        /// <summary>
        /// Configure Logging but also here was the logfile bypass method, anyway it... pulled that out of my core and restructured the whole app against every guide and telling so... rly bad.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logger"></param>
        private static void ConfigureLogging(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {

                builder.Services.AddLogging(logging =>
                    LoggingHelper.ConfigureCustomLoggersWithConsoleAndDebug(
                        logging,
                        builder.Services,
                        builder.Configuration));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureLogging builder {builder.ToString()}", builder);
                TryAppendStartupTrace(ex.ToString(), logger);
            }
           
        }
        ///// <summary>
        ///// Written by Codex to auto test xD... via mouse buttons to prevent selenium debug driver triggers js in App.razor to autotest, good reuseable for testing anyway... Later (teaching it to write that own js file maybe).
        ///// </summary>
        ///// <param name="logger"></param>
        ///// <returns></returns>
        //private static bool IsCustomLoggerBypassRequested(ILogger logger)
        //{
        //    try
        //    {
        //        return IsEnvironmentFlagEnabled("LOCALGPT_DISABLE_CUSTOM_LOGGERS", logger) ||
        //     IsEnvironmentFlagEnabled("LOCALGPT_E2E", logger);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, $"Error in IsCustomLoggerBypassRequested", logger);
        //        TryAppendStartupTrace(ex.ToString(), logger);
        //        return false;
        //    }
        //}

        private static bool IsEnvironmentFlagEnabled(string name, ILogger logger)
        {
            try
            {
                return string.Equals(
      Environment.GetEnvironmentVariable(name),
      "1",
      StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsEnvironmentFlagEnabled");
                TryAppendStartupTrace(ex.ToString(), logger);
                return false;
            }

        }

        private static void ConfigureOptionsAndServices(WebApplicationBuilder builder, ILogger logger)
        {
            try
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
                TraceStartup($"Checking SQLite database health at {memoryDbPath}.", logger);
                LocalGptDatabaseRecovery
                    .EnsureHealthyOrRecoverAsync(memoryDbPath, logger)
                    .GetAwaiter()
                    .GetResult();
                TraceStartup("Finished SQLite database health check.", logger);
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureOptionsAndServices");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void ConfigureSignalR(IServiceCollection services, ILogger logger)
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
                    ConfigureSharedJsonSerializerOptions(options.PayloadSerializerOptions, logger);
                    options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                });
        }

        private static void ConfigureKestrel(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {
                Port = GetFreePort(logger);
                builder.WebHost.UseKestrel().UseUrls($"http://127.0.0.1:{Port}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureKestrel");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void ConfigureResponseCompression(IServiceCollection services, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureResponseCompression");
                TryAppendStartupTrace(ex.ToString(), logger);
            }

        }

        private static void ConfigureBlazorAndMvc(WebApplicationBuilder builder, ILogger logger)
        {
            try
            {
                StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

                builder.Services.AddRazorComponents().AddInteractiveServerComponents();
                builder.Services.AddHealthChecks();
                builder.Services.AddDevExpressBlazor(options => options.SizeMode = DevExpress.Blazor.SizeMode.Small);
                builder.Services.AddMvc();
                builder.Services.AddScoped<ThemeService>();
                builder.Services.AddDevExpressServerSideBlazorPdfViewer();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureBlazorAndMvc");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void ConfigureJsonOptions(IServiceCollection services, ILogger logger)
        {
            try
            {
                services.Configure<JsonOptions>(options =>
                {
                    ConfigureSharedJsonSerializerOptions(options.JsonSerializerOptions, logger);
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureJsonOptions");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void ConfigureSharedJsonSerializerOptions(JsonSerializerOptions options, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureSharedJsonSerializerOptions");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void ConfigureForwardedHeaders(IServiceCollection services, ILogger logger)
        {
            try
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownIPNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureJsonOptions");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void ConfigureMiddlewareAndEndpoints(WebApplication app, ILogger logger)
        {
            try
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
                //should be autoresolved soon via MapControllers
                //app.MapLocalGptDiagnosticEndpoints(logger);
                //app.MapMinecraftDiagnosticEndpoints();
                app.MapRazorComponents<App>()
                   .AddInteractiveServerRenderMode()
                   .AllowAnonymous();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureMiddlewareAndEndpoints");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static bool IsGeneratedStaticWebAssetRoot(string path, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsGeneratedStaticWebAssetRoot path {path}");
                TryAppendStartupTrace(ex.ToString(), logger);
                return false;
            }
        }

        private static void WriteRuntimeEndpointFile(ILogger logger)
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteRuntimeEndpointFile");
                TryAppendStartupTrace(ex.ToString(), logger);
            }
        }
        private static int GetFreePort(ILogger logger)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetFreePort");
                TryAppendStartupTrace(ex.ToString(), logger);
                return 0;
            }
        }

    }
}
