using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Components;
using LocalGPT.Endpoints;
using LocalGPT.Helper;
using LocalGPT.Hubs;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using LocalGPT.Services.Formatting;
using LocalGPT.Services.Persistence;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
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

        public static WebApplication BuildWebApp(string[]? args = null)
        {
            var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
            var logger = loggerFactory.CreateLogger("Startup");
            //EnsureGeneratedStaticWebAssetContentRoots(exeDir, logger);

            var builder = WebApplication.CreateBuilder(CreateWebApplicationOptions(args));
            builder.Host.UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });
            logger.LogInformation("Created builder with startup service-provider validation enabled.");
            ConfigureAppConfiguration(builder, logger);
            logger.LogInformation("Configured app configuration.", logger);
            ConfigureLogging(builder, logger);
            logger.LogInformation("Configured logging.", logger);
            ConfigureOptionsAndServices(builder, logger);
            logger.LogInformation("Configured options and services.", logger);
            ConfigureSignalR(builder.Services, logger);
            logger.LogInformation("Configured SignalR.", logger);
            var port = ConfigureKestrel(builder, ResolveRequestedPort(args, logger), logger);
            logger.LogInformation("Configured Kestrel on loopback port {Port}.", port);
            ConfigureResponseCompression(builder.Services, logger);
            logger.LogInformation("Configured response compression.", logger);
            ConfigureBlazorAndMvc(builder, logger);
            logger.LogInformation("Configured Blazor and MVC.", logger);
            ConfigureJsonOptions(builder.Services, logger);
            logger.LogInformation("Configured JSON options.", logger);
            ConfigureForwardedHeaders(builder.Services, logger);
            logger.LogInformation("Configured forwarded headers.", logger);

            var app = builder.Build();
            logger.LogInformation("Built web application.", logger);
            ConfigureMiddlewareAndEndpoints(app, logger);
            logger.LogInformation("Configured middleware and endpoints.", logger);
            WriteRuntimeEndpointFile(port, logger);
            logger.LogInformation("Wrote runtime endpoint file.", logger);

            return app;
        }

        //private static void TraceStartup(string message, ILogger logger)
        //{
        //    try
        //    {
        //        var line = $"[{DateTimeOffset.Now:O}] pid={Environment.ProcessId} {message}{Environment.NewLine}";
        //        //TryAppendStartupTrace(line, logger);

        //        if (!string.Equals(
        //            Environment.GetEnvironmentVariable("LOCALGPT_STARTUP_TRACE"),
        //            "1",
        //            StringComparison.OrdinalIgnoreCase))
        //        {
        //            return;
        //        }
        //        //TryAppendStartupTrace($"[LocalGPT startup] {line}", logger);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, $"Error in TraceStartup {message}");
        //    }
           
        //}

        //private static void TryAppendStartupTrace(string line, ILogger logger)
        //{
        //    try
        //    {
        //        foreach (var directory in GetRuntimeTraceDirectories())
        //        {
        //            Directory.CreateDirectory(directory);
        //            File.AppendAllText(Path.Combine(directory, $"startup-trace-{Environment.ProcessId}.log"), line);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        logger.LogError(ex, $"Error in TryAppendStartupTrace line {line}");
        //        TraceStartup(ex.ToString(), logger);
        //        // Startup tracing must never block app launch.
        //    }
        //}

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
                logger.LogError(ex, "Application configuration setup failed.");
                ///*TryAppendStartupTrace*/(ex.ToString(), logger);
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
                    new LoggingConfigurationService(builder.Services, builder.Configuration).Configure(logging));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureLogging builder {builder.ToString()}", builder);
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                builder.Services.Configure<NativeCommandOptions>(
                    builder.Configuration.GetSection(NativeCommandOptions.SectionName));
                builder.Services.Configure<ArtifactBuildOptions>(
                    builder.Configuration.GetSection(ArtifactBuildOptions.SectionName));

                // PublisherStudio-style application boundaries: runtime helpers are injected services,
                // not mutable process-wide utility classes.
                builder.Services.AddSingleton<ICustomVersion>(new CustomVersion("0.1.4"));
                builder.Services.AddSingleton<LocalGptCatalogService>();
                builder.Services.AddSingleton<CouncilTextService>();
                builder.Services.AddSingleton<CouncilRuntimeService>();
                builder.Services.AddSingleton<SqliteUtilityService>();
                builder.Services.AddScoped<DevExpressChatService>();
                builder.Services.AddScoped<IChatMemoryMessageMapper, ChatMemoryMessageMapper>();
                builder.Services.AddSingleton<AiDiscoveryService>();
                builder.Services.AddSingleton<SqliteGridPresentationService>();
                builder.Services.AddSingleton<NavigationUrlService>();
                builder.Services.AddSingleton<ComponentActivityService>();
                builder.Services.AddSingleton<IComponentActivityService>(services =>
                    services.GetRequiredService<ComponentActivityService>());
                builder.Services.AddSingleton<IServiceActivityService>(services =>
                    services.GetRequiredService<ComponentActivityService>());
                builder.Services.AddSingleton<ISupervisedTaskRunner, SupervisedTaskRunner>();
                builder.Services.AddSingleton<AmbientLocalGptContext>();
                builder.Services.AddSingleton<IAmbientLocalGptContext>(services => services.GetRequiredService<AmbientLocalGptContext>());
                builder.Services.AddSingleton<ILocalHumanInteractionContext>(services => services.GetRequiredService<AmbientLocalGptContext>());
                builder.Services.AddSingleton<IHumanApprovalExecutionContext>(services => services.GetRequiredService<AmbientLocalGptContext>());
                builder.Services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
                builder.Services.AddSingleton<IAiConnectivityProbe, AiConnectivityProbe>();
                builder.Services.AddSingleton<IAiFeatureReportService, AiFeatureReportService>();
                builder.Services.AddSingleton<IArtifactBuildExecutor, ArtifactBuildExecutor>();
                builder.Services.AddSingleton<ICouncilArtifactService, CouncilArtifactService>();
                builder.Services.AddSingleton<IChatUploadWorkspaceService, ChatUploadWorkspaceService>();
                builder.Services.AddSingleton<IProjectLibraryInventoryService, ProjectLibraryInventoryService>();
                builder.Services.AddSingleton<IBuildDebugInventoryService, BuildDebugInventoryService>();
                builder.Services.AddSingleton<IMinecraftModWorkspaceService, MinecraftModWorkspaceService>();
                builder.Services.AddScoped<INativeCommandRunner, NativeCommandRunner>();
                builder.Services.AddScoped<IRegexPatternService, RegexPatternService>();
                builder.Services.AddScoped<IPromptConfigService, PromptConfigService>();
                builder.Services.AddScoped<IVariableStoreService, VariableStoreService>();

                var configuredDatabasePath = builder.Configuration[$"{LocalGptDatabaseOptions.SectionName}:Path"];
                var memoryDbPath = string.IsNullOrWhiteSpace(configuredDatabasePath)
                    ? Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "LocalGPT",
                        "localgpt-memory.db")
                    : Path.IsPathRooted(configuredDatabasePath)
                        ? configuredDatabasePath
                        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, configuredDatabasePath));
                var databaseOptions = new LocalGptDatabaseOptions(
                    memoryDbPath,
                    Math.Clamp(
                        builder.Configuration.GetValue<int?>($"{LocalGptDatabaseOptions.SectionName}:ProbeCommandTimeoutSeconds") ?? 5,
                        1,
                        60));

                builder.Services.AddSingleton(databaseOptions);
                builder.Services.AddSingleton<IDatabaseFileHealthService, DatabaseFileHealthService>();
                builder.Services.AddDbContextFactory<LocalGptMemoryDbContext>(options =>
                    options.UseSqlite($"Data Source={databaseOptions.DatabasePath}"));

                builder.Services.AddSingleton<IInitialDataCatalog, InitialDataCatalog>();
                builder.Services.AddSingleton<IDatabaseMigrationCompatibilityService, DatabaseMigrationCompatibilityService>();
                builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
                builder.Services.AddHostedService<DatabaseInitializationHostedService>();
                builder.Services.AddSingleton<IChatProtocolProfile, HarmonyChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, DeepSeekChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, GemmaChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, AppleChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, ThinkTagsChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, PlainTextChatProtocolProfile>();
                builder.Services.AddSingleton<IChatResponseFormatterFactory, ChatResponseFormatterFactory>();
                builder.Services.AddSingleton<IChatContentRenderer, ChatContentRenderer>();
                builder.Services.AddSingleton<IChatProtocolResolver, ChatProtocolResolver>();

                builder.Services.AddSingleton<IHumanCollaborationService, HumanCollaborationService>();
                builder.Services.AddSingleton<IDeferredDxAiInvocationService, DeferredDxAiInvocationService>();
                builder.Services.AddScoped<IChatMemoryService, EfChatMemoryService>();
                builder.Services.AddScoped<IApplicationLogReaderService, ApplicationLogReaderService>();
                builder.Services.AddScoped<ICouncilKnowledgeService, CouncilKnowledgeService>();
                builder.Services.AddScoped<ILocalGptProjectService, LocalGptProjectService>();
                builder.Services.AddScoped<IProjectArchitectureService, ProjectArchitectureService>();
                builder.Services.AddScoped<IModelPresetService, ModelPresetService>();
                builder.Services.AddScoped<ISqliteEditorPreferenceService, SqliteEditorPreferenceService>();
                builder.Services.AddScoped<ISafeTextDocumentService, SafeTextDocumentService>();
                builder.Services.AddScoped<IKnowledgeRatingService, KnowledgeRatingService>();
                builder.Services.AddScoped<ISqliteTableEditorService, SqliteTableEditorService>();
                builder.Services.AddScoped<ILearnBaseKnowledgeImporterService, LearnBaseKnowledgeImporterService>();
                builder.Services.AddScoped<IEngineeringBenchmarkService, EngineeringBenchmarkService>();
                builder.Services.AddScoped<IAiContextBootstrapService, AiContextBootstrapService>();
                builder.Services.AddScoped<ICodeGenerationWorkflowService, CodeGenerationWorkflowService>();
                builder.Services.AddScoped<ICouncilCodeGenerationPlanService, CouncilCodeGenerationPlanService>();
                builder.Services.AddScoped<IDxAiFunctionRegistry, DxAiFunctionRegistry>();
                builder.Services.AddScoped<IChatSessionContext, ChatSessionContext>();
                builder.Services.AddScoped<IDxAiFunctionServiceClient, DxAiFunctionServiceClient>();

                var dxAiHandlerTypes = typeof(Program).Assembly.DefinedTypes
                    .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                        typeof(IDxAiFunctionHandler).IsAssignableFrom(type.AsType()))
                    .Select(type => type.AsType())
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .ToList();
                foreach (var handlerType in dxAiHandlerTypes)
                    builder.Services.AddScoped(typeof(IDxAiFunctionHandler), handlerType);
                logger.LogInformation("Registered {DxAiFunctionHandlerCount} DI-backed DXAIFunction handler(s).", dxAiHandlerTypes.Count);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
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

        private static int ConfigureKestrel(WebApplicationBuilder builder, int requestedPort, ILogger logger)
        {
            try
            {
                var port = requestedPort > 0 ? requestedPort : GetFreePort(logger);
                builder.WebHost.UseKestrel().UseUrls($"http://127.0.0.1:{port}");
                return port;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kestrel loopback configuration failed.");
                throw;
            }
        }

        private static int ResolveRequestedPort(string[]? args, ILogger logger)
        {
            if (args is not { Length: > 0 } || string.IsNullOrWhiteSpace(args[0]))
                return 0;

            if (int.TryParse(args[0], out var parsedPort) && parsedPort is > 0 and <= 65535)
                return parsedPort;

            logger.LogWarning("Ignoring invalid requested port {RequestedPort}; a free loopback port will be selected.", args[0]);
            return 0;
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
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //using (var scope = app.Services.CreateScope())
                //{
                //    var migrator = new MigrationMigratorFactory()
                //        .Create<MigrationBuilder>(scope.ServiceProvider);

                //    await migrator.MigrateAsync();
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ConfigureMiddlewareAndEndpoints");
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
                return false;
            }
        }

        private static void WriteRuntimeEndpointFile(int port, ILogger logger)
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
                    BaseUrl = $"http://127.0.0.1:{port}",
                    Port = port,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };

                File.WriteAllText(
                    Path.Combine(directory, "server.json"),
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteRuntimeEndpointFile");
                //TryAppendStartupTrace(ex.ToString(), logger);
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
                //TryAppendStartupTrace(ex.ToString(), logger);
                return 0;
            }
        }

    }
}
