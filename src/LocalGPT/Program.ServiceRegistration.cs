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
using LocalGPT.Services.Helpers;

namespace LocalGPT
{
    /// <summary>
    /// Represents a program application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public static partial class Program
    {
    /// <summary>
        /// Performs configure options and services for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="builder">Builder value supplied to the program operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
                builder.Services.Configure<RemoteWebEndpointOptions>(
                    builder.Configuration.GetSection(RemoteWebEndpointOptions.SectionName));

                // PublisherStudio-style application boundaries: runtime helpers are injected services,
                // not mutable process-wide utility classes.
                var applicationVersion = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
                builder.Services.AddSingleton<ICustomVersion>(new CustomVersion(applicationVersion));
                builder.Services.AddSingleton<LocalGptCatalogService>();
                builder.Services.AddSingleton<ILocalGptRequestFactoryService, LocalGptRequestFactoryService>();
                builder.Services.AddSingleton<ICouncilTextPatternDataService, CouncilTextPatternDataService>();
                builder.Services.AddScoped<ICouncilDxFunctionPolicyDataService, CouncilDxFunctionPolicyDataService>();
                builder.Services.AddSingleton<CouncilTextService>();
                builder.Services.AddSingleton<CouncilRuntimeService>();
                builder.Services.AddSingleton<SqliteUtilityService>();
                builder.Services.AddSingleton<CouncilKnowledgeContentService>();
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
                builder.Services.AddSingleton<IJsonTextService, JsonTextService>();
                builder.Services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
                builder.Services.AddSingleton<IAiProviderConfigurationRegistryService, AiProviderConfigurationRegistryService>();
                builder.Services.AddSingleton<INetworkCertificateService, NetworkCertificateService>();
                builder.Services.AddSingleton<IAiConnectivityProbe, AiConnectivityProbe>();
                builder.Services.AddSingleton<IOllamaProcessService, OllamaProcessService>();
                builder.Services.AddSingleton<IAiFeatureReportService, AiFeatureReportService>();
                builder.Services.AddSingleton<IArtifactBuildExecutor, ArtifactBuildExecutor>();
                builder.Services.AddSingleton<ICouncilArtifactService, CouncilArtifactService>();
                builder.Services.AddSingleton<IChatUploadWorkspaceService, ChatUploadWorkspaceService>();
                builder.Services.AddSingleton<IProjectLibraryInventoryService, ProjectLibraryInventoryService>();
                builder.Services.AddSingleton<IBuildDebugInventoryService, BuildDebugInventoryService>();
                builder.Services.AddSingleton<IHardwareInventoryService, HardwareInventoryService>();
                builder.Services.AddScoped<IConfiguredAiHostHardwareService, ConfiguredAiHostHardwareService>();
                builder.Services.AddSingleton<MinecraftDatapackService>();
                builder.Services.AddSingleton<MinecraftProjectService>();
                builder.Services.AddSingleton<IMinecraftModWorkspaceService, MinecraftModWorkspaceService>();
                builder.Services.AddScoped<INativeCommandRunner, NativeCommandRunner>();
                builder.Services.AddSingleton<IRegexCompilationService, RegexCompilationService>();
                builder.Services.AddSingleton<IRegexPatternService, RegexPatternService>();
                builder.Services.AddSingleton<IKnowledgeRegexLinkService, KnowledgeRegexLinkService>();
                builder.Services.AddScoped<IPromptConfigService, PromptConfigService>();
                builder.Services.AddScoped<IVariableStoreService, VariableStoreService>();
                builder.Services.AddScoped<IFirstRunOnboardingService, FirstRunOnboardingService>();
                builder.Services.AddSingleton<IConsoleCommandService, ConsoleCommandService>();
                builder.Services.AddScoped<ICanIRunHardwareRecommendationService, CanIRunHardwareRecommendationService>();
                builder.Services.AddScoped<IAiProviderBootstrapService, AiProviderBootstrapService>();
                builder.Services.AddScoped<IInitialSetupAssistantService, InitialSetupAssistantService>();
                builder.Services.AddHttpClient("LocalGPTCanIRun")
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

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
                builder.Services.AddSingleton<IDatabaseLoggerReadiness, DatabaseLoggerReadiness>();
                builder.Services.AddSingleton<IDatabaseFileHealthService, DatabaseFileHealthService>();
                builder.Services.AddDbContextFactory<LocalGptMemoryDbContext>(options =>
                    options.UseSqlite($"Data Source={databaseOptions.DatabasePath}"));

                builder.Services.AddSingleton<ISystemVariableDefinitionService, SystemVariableDefinitionService>();
                builder.Services.AddSingleton<IInitialDataCatalog, InitialDataCatalog>();
                builder.Services.AddSingleton<IDatabaseMigrationCompatibilityService, DatabaseMigrationCompatibilityService>();
                builder.Services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
                builder.Services.AddHostedService<DatabaseInitializationHostedService>();
                builder.Services.AddSingleton<IDxAiFunctionJsonService, DxAiFunctionJsonService>();
                builder.Services.AddScoped<IDxAiFunctionCallRecoveryService, DxAiFunctionCallRecoveryService>();
                builder.Services.AddSingleton<ILocalPathExplorerService, LocalPathExplorerService>();
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
                builder.Services.AddSingleton<IStructuredTextTranslationService, StructuredTextTranslationService>();
                builder.Services.AddSingleton<IChatContentRenderer, ChatContentRenderer>();
                builder.Services.AddSingleton<IChatProtocolResolver, ChatProtocolResolver>();

                builder.Services.AddSingleton<IOrganicCouncilBlueprintSeedDataService, OrganicCouncilBlueprintSeedDataService>();
                builder.Services.AddSingleton<IHumanCollaborationService, HumanCollaborationService>();
                builder.Services.AddSingleton<ICouncilXRoundService, CouncilXRoundService>();
                builder.Services.AddSingleton<ICouncilLiveSessionService, CouncilLiveSessionService>();
                builder.Services.AddSingleton<ICouncilGameActorRuntimeFactory, CouncilGameActorRuntimeFactory>();
                builder.Services.AddSingleton<ICouncilGameSubdirector, CreatureCouncilGameSubdirector>();
                builder.Services.AddSingleton<ICouncilGameSubdirector, ReactiveObjectCouncilGameSubdirector>();
                builder.Services.AddSingleton<ICouncilGameDirectorService, CouncilGameDirectorService>();
                builder.Services.AddSingleton<ICouncilGameSessionService, CouncilGameSessionService>();
                builder.Services.AddSingleton<CouncilGameDxParameterReader>();
                builder.Services.AddSingleton<IDeferredDxAiInvocationService, DeferredDxAiInvocationService>();
                builder.Services.AddScoped<IChatMemoryService, EfChatMemoryService>();
                builder.Services.AddScoped<IApplicationLogReaderService, ApplicationLogReaderService>();
                builder.Services.AddScoped<ICouncilKnowledgeService, CouncilKnowledgeService>();
                builder.Services.AddScoped<ILearningProjectWorkspaceSyncService, LearningProjectWorkspaceSyncService>();
                builder.Services.AddScoped<ILearningRoundService, LearningRoundService>();
                builder.Services.AddScoped<ILocalGptProjectService, LocalGptProjectService>();
                builder.Services.AddScoped<IProjectArchitectureService, ProjectArchitectureService>();
                builder.Services.AddScoped<IToolchainKnowledgeService, ToolchainKnowledgeService>();
                builder.Services.AddScoped<IToolchainDiscoveryService, ToolchainDiscoveryService>();
                builder.Services.AddScoped<IProjectMaintenanceService, ProjectMaintenanceService>();
                builder.Services.AddScoped<IFeaturePersistenceService, FeaturePersistenceService>();
                builder.Services.AddSingleton<IEmbeddedHardwareCatalogService, EmbeddedHardwareCatalogService>();
                builder.Services.AddSingleton<IEmbeddedWiringService, EmbeddedWiringService>();
                builder.Services.AddSingleton<IEmbeddedTelemetryBridgeService, EmbeddedTelemetryBridgeService>();
                builder.Services.AddSingleton<IEmbeddedTelemetryIngressService, EmbeddedTelemetryIngressService>();
                builder.Services.AddScoped<IEmbeddedFirmwarePlanningService, EmbeddedFirmwarePlanningService>();
                builder.Services.AddScoped<IModelPresetService, ModelPresetService>();
                builder.Services.AddScoped<IHardwarePerformancePresetService, HardwarePerformancePresetService>();
                builder.Services.AddScoped<ISqliteEditorPreferenceService, SqliteEditorPreferenceService>();
                builder.Services.AddScoped<ISafeTextDocumentService, SafeTextDocumentService>();
                builder.Services.AddScoped<IKnowledgeRatingService, KnowledgeRatingService>();
                builder.Services.AddScoped<ISqliteTableEditorService, SqliteTableEditorService>();
                builder.Services.AddScoped<ILearnBaseKnowledgeImporterService, LearnBaseKnowledgeImporterService>();
                builder.Services.AddSingleton<IDocumentationTranslationAdapter, DocumentationTranslationAdapter>();
                builder.Services.AddSingleton<IDocumentationCatalogService, DocumentationCatalogService>();
                builder.Services.AddScoped<IDocumentationViewerService, DocumentationViewerService>();
                builder.Services.AddScoped<IRemoteKnowledgeImportService, RemoteKnowledgeImportService>();
                builder.Services.AddScoped<IRemoteControlTemplateService, RemoteControlTemplateService>();
                builder.Services.AddScoped<IRemoteControlTransportService, RemoteControlTransportService>();
                builder.Services.AddScoped<IRemoteControlExecutionStoreService, RemoteControlExecutionStoreService>();
                builder.Services.AddScoped<IRemoteControlPipelineService, RemoteControlPipelineService>();
                builder.Services.AddScoped<IRemoteControlConnectorService, RemoteControlConnectorService>();
                builder.Services.AddHttpClient("LocalGPTRemoteControl")
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
                builder.Services.AddHostedService<RemoteControlPollingHostedService>();
                builder.Services.AddSingleton<RemoteImportDxParameterReader>();
                builder.Services.AddScoped<IEngineeringBenchmarkService, EngineeringBenchmarkService>();
                builder.Services.AddScoped<IAiContextBootstrapService, AiContextBootstrapService>();
                builder.Services.AddScoped<ICodeGenerationWorkflowService, CodeGenerationWorkflowService>();
                builder.Services.AddScoped<ICouncilCodeGenerationPlanService, CouncilCodeGenerationPlanService>();
                builder.Services.AddSingleton<ICouncilSpoolerService, LocalGPT.Services.Council.CouncilSpoolerService>();
                builder.Services.AddScoped<IRuntimeCapabilityDirectoryService, LocalGPT.Services.Council.RuntimeCapabilityDirectoryService>();
                builder.Services.AddHostedService<LocalGPT.Services.Council.RuntimeCapabilityDirectoryHostedService>();
                builder.Services.AddScoped<ICouncilPreflightService, LocalGPT.Services.Council.CouncilPreflightService>();
                builder.Services.AddScoped<IDebugArtifactInspectionService, DebugArtifactInspectionService>();
                builder.Services.AddSingleton<IUserDxAiFunctionService, UserDxAiFunctionService>();
                builder.Services.AddSingleton<DxAiFunctionHandlerMapService>();
                builder.Services.AddScoped<IDxAiFunctionRegistry, DxAiFunctionRegistry>();
                builder.Services.AddScoped<HardwarePerformancePresetDxAiSupport>();
                builder.Services.AddSingleton<DxAiFunctionCatalogSynchronizationGate>();
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
                builder.Services.AddScoped<ICouncilAutomaticFunctionPolicyService, CouncilAutomaticFunctionPolicyService>();
                builder.Services.AddScoped<ICouncilRuntimeClassService, CouncilRuntimeClassService>();
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
                builder.Services.AddSingleton<IOrganicAddonManifestService, OrganicAddonManifestService>();
                builder.Services.AddScoped<IOrganicSkillRegistryService, OrganicSkillRegistryService>();
                builder.Services.AddScoped<IModelCapabilitySelfAssessmentService, LocalGPT.Services.Council.Skills.ModelCapabilitySelfAssessmentService>();
                builder.Services.AddSingleton<ICouncilHardwareRoadConfigurationService, LocalGPT.Services.Council.Scheduling.CouncilHardwareRoadConfigurationService>();
                builder.Services.AddSingleton<ICouncilHardwareRoadPlanner, LocalGPT.Services.Council.Scheduling.CouncilHardwareRoadPlanner>();
                builder.Services.AddSingleton<ICouncilRunConfigurationService, CouncilRunConfigurationService>();
                builder.Services.AddScoped<IProviderModelRuntimeService, ProviderModelRuntimeService>();
                builder.Services.AddSingleton<IProviderModelReviewerPolicyService, ProviderModelReviewerPolicyService>();
                builder.Services.AddScoped<IProviderModelBenchmarkService, ProviderModelBenchmarkService>();
                builder.Services.AddScoped<ICouncilBenchmarkCalibrationService, CouncilBenchmarkCalibrationService>();
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

        /// <summary>
        /// Performs configure signal r for <see cref="Program"/>, keeping the operation consistent with the state and invariants of the surrounding program workflow.
        /// </summary>
        /// <param name="services">Service collection dependency used by the program workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        private static void ConfigureSignalR(IServiceCollection services, ILogger logger)
        {
            services.AddSignalR(options =>
                {
                    // Unlimited on the trusted local loopback transport. This permits large offline media and
                    // document attachments without an arbitrary cloud-style message ceiling.
                    options.MaximumReceiveMessageSize = null;
                    options.EnableDetailedErrors = true;
                    // A large local Council can temporarily keep the browser main thread busy while
                    // rendering provider output. Give transient stalls more room before SignalR
                    // declares the client dead; a genuinely lost browser can still rejoin through
                    // the server-owned Council live-session snapshot.
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
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

    }
}
