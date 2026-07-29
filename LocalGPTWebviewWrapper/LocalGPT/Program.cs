using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.XtraCharts;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Components;
using LocalGPT.Diagnostics;
using LocalGPT.Endpoints;
using LocalGPT.Helper;
using LocalGPT.Hubs;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using LocalGPT.Services.Formatting;
using LocalGPT.Services.Persistence;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TacosPortal.Services;
using LocalGPT.Services.Helpers;
namespace LocalGPT
{
    public static class Program
    {
        // Installer/WebView compatibility contract. Do not remove or silently change these defaults.
        public const int DefaultPort = 5000;
        public const int DefaultOneWirePort = OneWireProtocol.DefaultServicePort;
        public const int DefaultOneWireDiscoveryPort = OneWireProtocol.DefaultDiscoveryPort;

        private static int runtimePort = DefaultPort;
        private static int runtimeOneWirePort = DefaultOneWirePort;
        private static int runtimeOneWireDiscoveryPort = DefaultOneWireDiscoveryPort;

        // Public read-only compatibility surface consumed by the WinUI wrapper and installer wiring.
        // Startup updates the private snapshot atomically; callers cannot mutate it.
        public static System.Int32 Port => System.Threading.Volatile.Read(ref runtimePort);
        public static System.Int32 OneWirePort => System.Threading.Volatile.Read(ref runtimeOneWirePort);
        public static System.Int32 OneWireDiscoveryPort => System.Threading.Volatile.Read(ref runtimeOneWireDiscoveryPort);
        public static string BaseUrl => $"http://127.0.0.1:{Port}";
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
            System.Threading.Volatile.Write(ref runtimePort, ResolveRequestedPort(args, builder.Configuration, logger));
            System.Threading.Volatile.Write(ref runtimeOneWirePort, ResolveConfiguredPort(args, builder.Configuration, "--onewire-port", "LOCALGPT_ONEWIRE_PORT", $"{OneWireOptions.SectionName}:ServicePort", DefaultOneWirePort, allowDynamic: false, logger));
            System.Threading.Volatile.Write(ref runtimeOneWireDiscoveryPort, ResolveConfiguredPort(args, builder.Configuration, "--onewire-discovery-port", "LOCALGPT_ONEWIRE_DISCOVERY_PORT", $"{OneWireOptions.SectionName}:DiscoveryPort", DefaultOneWireDiscoveryPort, allowDynamic: false, logger));
            ConfigureLogging(builder, logger);
            logger.LogInformation("Configured logging.", logger);
            ConfigureOptionsAndServices(builder, logger);
            logger.LogInformation("Configured options and services.", logger);
            ConfigureSignalR(builder.Services, logger);
            logger.LogInformation("Configured SignalR.", logger);
            System.Threading.Volatile.Write(ref runtimePort, ConfigureKestrel(builder, Port, logger));
            ValidatePortContracts(logger);
            var port = Port;
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
            var runtimeEndpointLogger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("LocalGPT.RuntimeEndpoint");
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                WriteRuntimeEndpointFile(Port, runtimeEndpointLogger);
                runtimeEndpointLogger.LogInformation("Wrote runtime endpoint file after the LocalGPT listener started.");
            });
            app.Lifetime.ApplicationStopped.Register(() => DeleteRuntimeEndpointFile(runtimeEndpointLogger));

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

                if (!builder.Environment.IsDevelopment())
                    builder.Logging.AddFilter((category, level) => level >= LogLevel.Warning);

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
                builder.Services.AddSingleton<ICustomVersion>(new CustomVersion("2.0.1"));
                builder.Services.AddSingleton<LocalGptCatalogService>();
                builder.Services.AddSingleton<ICouncilTextPatternDataService, CouncilTextPatternDataService>();
                builder.Services.AddScoped<ICouncilDxFunctionPolicyDataService, CouncilDxFunctionPolicyDataService>();
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
                builder.Services.AddSingleton<IHardwareInventoryService, HardwareInventoryService>();
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

                builder.Services.AddSingleton<ISystemVariableDefinitionService, SystemVariableDefinitionService>();
                builder.Services.AddSingleton<IInitialDataCatalog, InitialDataCatalog>();
                builder.Services.AddSingleton<IDatabaseMigrationCompatibilityService, DatabaseMigrationCompatibilityService>();
                builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
                builder.Services.AddHostedService<DatabaseInitializationHostedService>();
                builder.Services.AddSingleton<IDxAiFunctionJsonService, DxAiFunctionJsonService>();
                builder.Services.AddSingleton<IRegexFunctionParameterService, RegexFunctionParameterService>();
                builder.Services.AddSingleton<IChatProtocolTextService, ChatProtocolTextService>();
                builder.Services.AddSingleton<IChatProtocolProfile, HarmonyChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, DeepSeekChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, GemmaChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, AppleChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, ThinkTagsChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfile, PlainTextChatProtocolProfile>();
                builder.Services.AddSingleton<IChatProtocolProfileCatalog, ChatProtocolProfileCatalog>();
                builder.Services.AddSingleton<IChatResponseFormatterFactory, ChatResponseFormatterFactory>();
                builder.Services.AddSingleton<IChatContentRenderer, ChatContentRenderer>();
                builder.Services.AddSingleton<IChatProtocolResolver, ChatProtocolResolver>();

                builder.Services.AddSingleton<IOrganicCouncilBlueprintSeedDataService, OrganicCouncilBlueprintSeedDataService>();
                builder.Services.AddSingleton<IHumanCollaborationService, HumanCollaborationService>();
                builder.Services.AddSingleton<IDeferredDxAiInvocationService, DeferredDxAiInvocationService>();
                builder.Services.AddScoped<IChatMemoryService, EfChatMemoryService>();
                builder.Services.AddScoped<IApplicationLogReaderService, ApplicationLogReaderService>();
                builder.Services.AddScoped<ICouncilKnowledgeService, CouncilKnowledgeService>();
                builder.Services.AddScoped<ILearningRoundService, LearningRoundService>();
                builder.Services.AddScoped<ILocalGptProjectService, LocalGptProjectService>();
                builder.Services.AddScoped<IProjectArchitectureService, ProjectArchitectureService>();
                builder.Services.AddScoped<IProjectMaintenanceService, ProjectMaintenanceService>();
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
                builder.Services.AddSingleton<ICouncilSpoolerService, LocalGPT.Services.Council.CouncilSpoolerService>();
                builder.Services.AddScoped<IRuntimeCapabilityDirectoryService, LocalGPT.Services.Council.RuntimeCapabilityDirectoryService>();
                builder.Services.AddHostedService<LocalGPT.Services.Council.RuntimeCapabilityDirectoryHostedService>();
                builder.Services.AddScoped<ICouncilPreflightService, LocalGPT.Services.Council.CouncilPreflightService>();
                builder.Services.AddScoped<IDebugArtifactInspectionService, DebugArtifactInspectionService>();
                builder.Services.AddSingleton<DxAiFunctionHandlerMapService>();
                builder.Services.AddScoped<IDxAiFunctionRegistry, DxAiFunctionRegistry>();
                builder.Services.AddScoped<IDxAiFunctionCatalogService, DxAiFunctionCatalogService>();
                builder.Services.AddScoped<ICouncilDxFunctionOrchestrator, CouncilDxFunctionOrchestrator>();
                builder.Services.AddScoped<IPublicServiceMethodInvoker, PublicServiceMethodInvoker>();
                builder.Services.AddHostedService<DxAiFunctionCatalogHostedService>();
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
                builder.Services.AddScoped<IProjectOrganicContextService, ProjectOrganicContextService>();
                builder.Services.AddScoped<ICouncilTeamConfigurationService, CouncilTeamConfigurationService>();
                builder.Services.AddScoped<IOrganicCouncilBlueprintService, OrganicCouncilBlueprintService>();
                builder.Services.Configure<OneWireOptions>(builder.Configuration.GetSection(OneWireOptions.SectionName));
                builder.Services.AddSingleton<IOneWireEnvelopeCodec, OneWireEnvelopeCodec>();
                builder.Services.AddSingleton<IOneWireTransportSecurityPolicy, OneWireTransportSecurityPolicy>();
                builder.Services.AddSingleton<IOneWireDispatchContextFactory, OneWireDispatchContextFactory>();
                builder.Services.AddSingleton<IOneWireListenAddressResolver, OneWireListenAddressResolver>();
                builder.Services.AddSingleton<IOneWireTargetApprovalPolicy, OneWireTargetApprovalPolicy>();
                builder.Services.AddSingleton<IOrganicDxFunctionSupport, OrganicDxFunctionSupport>();
                builder.Services.AddSingleton<IPublisherInteractionDxSupport, PublisherInteractionDxSupport>();
                builder.Services.AddSingleton<IOneWireRuntimeSecurityService, OneWireRuntimeSecurityService>();
                builder.Services.AddSingleton<ILocalVisionOcrService, LocalVisionOcrService>();
                builder.Services.AddSingleton<IOneWirePeerRegistry, OneWirePeerRegistry>();
                builder.Services.AddSingleton<IOneWireConnectionRegistry, OneWireConnectionRegistry>();
                builder.Services.AddSingleton<IOneWireReplayPolicyDataService, OneWireReplayPolicyDataService>();
                builder.Services.AddSingleton<ILocalGptRuntimePolicySeedDataService, LocalGptRuntimePolicySeedDataService>();
                builder.Services.AddSingleton<ILocalGptRuntimePolicyStoreService, LocalGptRuntimePolicyStoreService>();
                builder.Services.AddSingleton<ILocalGptRuntimePolicyDataService, LocalGptRuntimePolicyDataService>();
                builder.Services.AddSingleton<ILocalGptVocabularyService, LocalGptVocabularyService>();
                builder.Services.AddSingleton<IOneWireReplayGuard, OneWireReplayGuard>();
                builder.Services.AddSingleton<IOneWireWorkSpooler, OneWireWorkSpooler>();
                builder.Services.AddSingleton<IOneWirePendingCouncilStore, OneWirePendingCouncilStore>();
                builder.Services.AddSingleton<IOneWireCapabilityCatalog, OneWireCapabilityCatalog>();
                builder.Services.AddSingleton<IOneWireCapabilityProvider>(provider =>
                    provider.GetRequiredService<IOneWireCapabilityCatalog>());
                builder.Services.AddSingleton<IOneWireOperationExecutor, OneWireOperationExecutor>();
                builder.Services.AddSingleton<IOneWireMessageDispatcher, OneWireMessageDispatcher>();
                builder.Services.AddHostedService<OneWireTcpHostedService>();
                builder.Services.AddHostedService<OneWireDiscoveryHostedService>();
                builder.Services.AddHostedService<OneWireCouncilApprovalProcessorHostedService>();
                builder.Services.AddHostedService<OneWireWorkProcessorHostedService>();
                builder.Services.AddScoped<IOrganicSkillRegistryService, OrganicSkillRegistryService>();
                builder.Services.AddScoped<IModelCapabilitySelfAssessmentService, LocalGPT.Services.Council.Skills.ModelCapabilitySelfAssessmentService>();
                builder.Services.AddSingleton<ICouncilHardwareRoadConfigurationService, LocalGPT.Services.Council.Scheduling.CouncilHardwareRoadConfigurationService>();
                builder.Services.AddSingleton<ICouncilHardwareRoadPlanner, LocalGPT.Services.Council.Scheduling.CouncilHardwareRoadPlanner>();
                builder.Services.AddScoped<IMultiModelCouncilService, MultiModelCouncilService>();
                builder.Services.AddScoped<IChatClientFactory, ChatClientFactory>();
                builder.Services.AddScoped<IChatClient>(sp =>
                    sp.GetRequiredService<IChatClientFactory>().Build());

                builder.Services.AddDevExpressAI();
                builder.Services.AddScoped<INotificationService, NotificationService>();
                builder.Services.Configure<CircuitOptions>(options =>
                    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30));
                builder.Services.AddOptions();
                builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
                {
                    // LocalGPT is an offline loopback application. Keep framework limits out of the way and
                    // let available disk/memory plus the selected local model determine the practical ceiling.
                    options.MultipartBodyLengthLimit = long.MaxValue;
                    options.ValueLengthLimit = int.MaxValue;
                    options.KeyLengthLimit = int.MaxValue;
                    options.MultipartHeadersLengthLimit = int.MaxValue;
                    options.MultipartHeadersCountLimit = int.MaxValue;
                    options.MemoryBufferThreshold = int.MaxValue;
                    options.BufferBody = true;
                    options.BufferBodyLengthLimit = long.MaxValue;
                });
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
            services.AddSignalR(options =>
                {
                    // Unlimited on the trusted local loopback transport. This permits large offline media and
                    // document attachments without an arbitrary cloud-style message ceiling.
                    options.MaximumReceiveMessageSize = null;
                    options.EnableDetailedErrors = true;
                })
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
                builder.WebHost.ConfigureKestrel(options =>
                {
                    // Local-only/offline host: avoid artificial request size limits for user-selected files,
                    // videos, audio, archives and model-compatible media. The listener remains loopback-only.
                    options.Limits.MaxRequestBodySize = null;
                    options.Limits.MaxRequestBufferSize = null;
                });
                builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
                return port;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kestrel loopback configuration failed.");
                throw;
            }
        }

        private static int ResolveRequestedPort(string[]? args, IConfiguration configuration, ILogger logger)
        {
            // The installer historically starts LocalGPT.exe with a positional numeric port.
            // Keep that contract first, while also supporting explicit switches/configuration.
            if (args is { Length: > 0 } && int.TryParse(args[0], out var positionalPort))
            {
                if (positionalPort is > 0 and <= 65535)
                    return positionalPort;
                logger.LogWarning("Ignoring invalid positional LocalGPT port {RequestedPort}; default {DefaultPort} remains active.", args[0], DefaultPort);
            }

            return ResolveConfiguredPort(
                args,
                configuration,
                "--port",
                "LOCALGPT_PORT",
                "LocalGPT:Port",
                configuration.GetValue<int?>("ApiCore:HttpPort") is > 0 and <= 65535 ? configuration.GetValue<int>("ApiCore:HttpPort") : DefaultPort,
                allowDynamic: true,
                logger);
        }

        private static int ResolveConfiguredPort(
            string[]? args,
            IConfiguration configuration,
            string switchName,
            string environmentName,
            string configurationKey,
            int fallback,
            bool allowDynamic,
            ILogger logger)
        {
            if (args is { Length: > 0 })
            {
                for (var index = 0; index < args.Length; index++)
                {
                    if (!string.Equals(args[index], switchName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (index + 1 < args.Length && int.TryParse(args[index + 1], out var commandLinePort) &&
                        ((commandLinePort is > 0 and <= 65535) || (allowDynamic && commandLinePort == 0)))
                        return commandLinePort;
                    logger.LogWarning("Ignoring invalid {SwitchName} value; fallback port {FallbackPort} remains active.", switchName, fallback);
                }
            }

            var environmentValue = Environment.GetEnvironmentVariable(environmentName);
            if (int.TryParse(environmentValue, out var environmentPort) &&
                ((environmentPort is > 0 and <= 65535) || (allowDynamic && environmentPort == 0)))
                return environmentPort;

            var configuredPort = configuration.GetValue<int?>(configurationKey);
            if ((configuredPort is > 0 and <= 65535) || (allowDynamic && configuredPort == 0))
                return configuredPort.Value;

            return fallback;
        }

        private static void ValidatePortContracts(ILogger logger)
        {
            // The installer-selected web port is authoritative. Optional organic wiring must adapt
            // around it and must never prevent the desktop bootstrap from starting.
            if (OneWirePort == Port || OneWirePort == OneWireDiscoveryPort)
            {
                var previous = OneWirePort;
                var replacement = GetFreePortExcluding(logger, Port, OneWireDiscoveryPort);
                if (replacement <= 0)
                {
                    logger.LogError(
                        "No safe organic 1-Wire TCP port could be reserved. The LocalGPT installer/bootstrap port {ApplicationPort} remains authoritative; organic TCP startup will be fault-contained.",
                        Port);
                }
                else
                {
                    System.Threading.Volatile.Write(ref runtimeOneWirePort, replacement);
                    logger.LogWarning(
                        "Reassigned conflicting optional organic TCP port {PreviousPort} to {ReplacementPort}; the installer/bootstrap application port {ApplicationPort} was preserved unchanged.",
                        previous, replacement, Port);
                }
            }

            if (Port == OneWireDiscoveryPort)
            {
                logger.LogInformation(
                    "Application TCP and organic discovery UDP both use numeric port {Port}. They are separate transports; the installer/bootstrap listener remains unchanged.",
                    Port);
            }

            logger.LogInformation(
                "Validated LocalGPT port contracts: app/installer TCP {ApplicationPort}, organic TCP {OneWirePort}, discovery UDP {DiscoveryPort}.",
                Port, OneWirePort, OneWireDiscoveryPort);
        }

        private static int GetFreePortExcluding(ILogger logger, params int[] excludedPorts)
        {
            var excluded = excludedPorts.ToHashSet();
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var candidate = GetFreePort(logger);
                if (candidate > 0 && !excluded.Contains(candidate))
                    return candidate;
            }
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
                builder.Services.AddSingleton<CircuitHandler, LocalGptCircuitDiagnosticsHandler>();
                builder.Services.AddLocalization();
                builder.Services.AddSingleton<LocalGPT.Services.Localization.ILocalGptLocalizationService, LocalGPT.Services.Localization.LocalGptLocalizationService>();
                builder.Services.Configure<RequestLocalizationOptions>(options =>
                {
                    var cultures = new[] { new CultureInfo("en-US"), new CultureInfo("de-DE") };
                    options.DefaultRequestCulture = new RequestCulture("en-US");
                    options.SupportedCultures = cultures;
                    options.SupportedUICultures = cultures;
                    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
                });
                builder.Services.AddHealthChecks();
                builder.Services.AddDevExpressBlazor(options => options.SizeMode = DevExpress.Blazor.SizeMode.Medium);
                builder.Services.AddScoped<ControllerRequestLoggingFilter>();
                builder.Services.AddMvc(options =>
                    options.Filters.AddService<ControllerRequestLoggingFilter>());
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
                if (!app.Environment.IsDevelopment())
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
                    BaseUrl = BaseUrl,
                    Port = port,
                    OneWirePort,
                    OneWireDiscoveryPort,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };

                File.WriteAllText(
                    Path.Combine(directory, "server.json"),
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"LocalGPT listening on {BaseUrl}");
                logger.LogInformation("LocalGPT runtime endpoint {BaseUrl} was written for process {ProcessId}.", BaseUrl, Environment.ProcessId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteRuntimeEndpointFile");
                //TryAppendStartupTrace(ex.ToString(), logger);
            }
        }

        private static void DeleteRuntimeEndpointFile(ILogger logger)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "runtime",
                    "server.json");
                if (!File.Exists(path))
                    return;

                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("ProcessId", out var processId)
                    && processId.TryGetInt32(out var ownerProcessId)
                    && ownerProcessId == Environment.ProcessId)
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not remove the LocalGPT runtime endpoint file during shutdown.");
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
