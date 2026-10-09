namespace LocalGPT.BusinessObjects;

/// <summary>Database-backed operational defaults and bounds for the MCP gateway configuration policy.</summary>
public sealed class McpGatewayRuntimeParameters
{
    public string DefaultAddress { get; set; } = string.Empty;
    public int MinimumPort { get; set; }
    public int MaximumPort { get; set; }
    public int DefaultPort { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public string DefaultApiKeyHeader { get; set; } = string.Empty;
    public string ModernProtocolVersion { get; set; } = string.Empty;
    public string LegacyProtocolVersion { get; set; } = string.Empty;
    public int MinimumCacheTtlMilliseconds { get; set; }
    public int MaximumCacheTtlMilliseconds { get; set; }
    public string PublicCacheScope { get; set; } = string.Empty;
    public string PrivateCacheScope { get; set; } = string.Empty;
    public string DefaultAllowedHosts { get; set; } = string.Empty;
    public int MinimumRequestBodyBytes { get; set; }
    public int MaximumRequestBodyBytes { get; set; }
    public int MinimumListItems { get; set; }
    public int MaximumListItems { get; set; }
    public int MinimumResultCharacters { get; set; }
    public int MaximumResultCharacters { get; set; }
    public int ApiKeyBytes { get; set; }
    public bool EnableModernProtocolWhenNoneSelected { get; set; }
    public bool RequireApiKeyForRemoteClients { get; set; }
    public bool EnableDedicatedListenerWhenNoEndpointSelected { get; set; }
}

/// <summary>Database-backed operational defaults and bounds for chat upload workspaces.</summary>
public sealed class ChatUploadWorkspaceRuntimeParameters
{
    public int StreamBufferBytes { get; set; }
    public long ImmediateAnalysisMaximumBytes { get; set; }
    public int DefaultWorkspaceListCount { get; set; }
    public int DefaultFileListCount { get; set; }
    public int MinimumFileListCount { get; set; }
    public int MaximumFileListCount { get; set; }
    public int TextReaderBufferCharacters { get; set; }
    public int MinimumReadSegmentCharacters { get; set; }
    public int MaximumReadSegmentCharacters { get; set; }
}

/// <summary>Database-backed operational defaults and bounds for repository synchronization from chat upload workspaces.</summary>
public sealed class LearningProjectWorkspaceSyncRuntimeParameters
{
    public int WorkspaceScanMaximum { get; set; }
    public int RepositoryMarkerMaximum { get; set; }
    public int GitDirectoryMaximum { get; set; }
    public bool IncludeNewerWorkspacesWhenExplicitWorkspaceIsStale { get; set; }
}


/// <summary>Database-backed provider discovery and default chat-session parameters.</summary>
public sealed class ProviderModelRuntimeParameters
{
    public string DefaultSessionKeepAlive { get; set; } = string.Empty;
    public int DefaultSessionContextTokens { get; set; }
    public int DefaultSessionTimeoutMinutes { get; set; }
    public int AvailabilityPollSeconds { get; set; }
    public int DiscoveryHttpTimeoutSeconds { get; set; }
    public string DefaultOpenAiCompatibleEndpoint { get; set; } = string.Empty;
    public string DefaultOpenAiCompatibleApiKey { get; set; } = string.Empty;
    public string DefaultOpenAiEndpoint { get; set; } = string.Empty;
    public string OpenAiCompatibleApiPath { get; set; } = string.Empty;
    public string OpenAiCompatibleModelsPath { get; set; } = string.Empty;
}

/// <summary>Database-backed operational defaults and bounds shared by adaptive and provider benchmark services.</summary>
public sealed class ModelBenchmarkRuntimeParameters
{
    public int MinimumModels { get; set; }
    public int MaximumModels { get; set; }
    public int MinimumProfilesPerModel { get; set; }
    public int MaximumProfilesPerModel { get; set; }
    public int MinimumTasks { get; set; }
    public int MaximumTasks { get; set; }
    public int MinimumSecondsPerCall { get; set; }
    public int MaximumSecondsPerCall { get; set; }
    public double MinimumImprovementPercent { get; set; }
    public double MaximumImprovementPercent { get; set; }
    public int MinimumRecommendedContextTokens { get; set; }
    public int MaximumRecommendedContextTokens { get; set; }
    public int MinimumRecommendedOutputTokens { get; set; }
    public int MaximumRecommendedOutputTokens { get; set; }
    public int MaximumRepetitionRecoveryAttempts { get; set; }
    public int MaximumPersistedEvidenceItems { get; set; }
    public int MaximumExpectedSections { get; set; }
    public int ClientTimeoutPaddingSeconds { get; set; }
    public int MaximumTaskPromptEvidenceCharacters { get; set; }
    public int MaximumResponseEvidenceCharacters { get; set; }
    public int MaximumProviderTraceEvidenceCharacters { get; set; }
    public int EvidenceStreamBufferBytes { get; set; }
    public int ResponsePreviewCharacters { get; set; }
    public int ReviewerMaximumOutputTokens { get; set; }
}

/// <summary>Database-backed operational defaults and bounds used by Council execution and configuration services.</summary>
public sealed class CouncilExecutionRuntimeParameters
{
    public int MinimumModelTimeoutSeconds { get; set; }
    public int DefaultModelTimeoutSeconds { get; set; }
    public int MaximumModelTimeoutSeconds { get; set; }
    public int MaximumRoleComplianceRetries { get; set; }
    public int MaximumMemberRecoveryAttempts { get; set; }
    public int MinimumFinalRecoveryOutputTokens { get; set; }
    public int MaximumFinalRecoveryOutputTokens { get; set; }
    public int MaximumChildCouncilDepth { get; set; }
    public int MaximumLoopIterations { get; set; }
    public int MaximumContinuationRounds { get; set; }
    public int MaximumCritiqueRounds { get; set; }
    public int MinimumReadinessOutputTokens { get; set; }
    public int MaximumReadinessOutputTokens { get; set; }
    public int AvailabilityWaitDivisor { get; set; }
    public int MinimumAvailabilityWaitSeconds { get; set; }
    public int MaximumAvailabilityWaitSeconds { get; set; }
    public int RecoveryOutputTokenCeiling { get; set; }
    public int RecoveryContextTokenCeiling { get; set; }
}

/// <summary>Database-backed operational bounds for Council game authoring and runtime behavior.</summary>
public sealed class CouncilGameRuntimeParameters
{
    public int MinimumCreatureDirectors { get; set; }
    public int MaximumCreatureDirectors { get; set; }
    public int DefaultCreatureDirectors { get; set; }
    public int MinimumFrameWidth { get; set; }
    public int MaximumFrameWidth { get; set; }
    public int DefaultFrameWidth { get; set; }
    public int MinimumFrameHeight { get; set; }
    public int MaximumFrameHeight { get; set; }
    public int DefaultFrameHeight { get; set; }
    public int MinimumAutoplayDelayMilliseconds { get; set; }
    public int MaximumAutoplayDelayMilliseconds { get; set; }
    public int DefaultAutoplayDelayMilliseconds { get; set; }
    public int MaximumDisplayFrames { get; set; }
    public int DefaultForegroundColor { get; set; }
    public int DefaultBackgroundColor { get; set; }
    public int MinimumWorldStepScale { get; set; }
    public int MaximumWorldStepScale { get; set; }
}

/// <summary>Database-backed query/page limits used by read-only operational services.</summary>
public sealed class ServiceQueryRuntimeParameters
{
    public int ApplicationLogMaximum { get; set; }
    public int ConsoleOperatorMaximum { get; set; }
    public int CouncilSpoolerDefault { get; set; }
    public int CouncilSpoolerMaximum { get; set; }
    public int CouncilSpoolerCheckpointCount { get; set; }
    public int CouncilSpoolerMaximumSteps { get; set; }
    public int DocumentationMaximum { get; set; }
    public int ConversationListMaximum { get; set; }
    public int ConversationMessageMaximum { get; set; }
    public int HumanContributionMaximum { get; set; }
    public int KnowledgeMaximum { get; set; }
    public int LocalPathMaximumEntries { get; set; }
    public int RegexListMaximum { get; set; }
    public int RegexDxListMaximum { get; set; }
    public int RemoteControlExecutionMaximum { get; set; }
    public int ToolchainCandidateMaximum { get; set; }
    public int ToolchainRuntimeResultMaximum { get; set; }
    public int WebSearchMaximumResults { get; set; }
}

/// <summary>Database-backed operational bounds for LearningBase source import.</summary>
public sealed class LearnBaseImportRuntimeParameters
{
    public int MinimumManifestFiles { get; set; }
    public int MaximumManifestFiles { get; set; }
    public int MinimumFileBytes { get; set; }
    public int MaximumManifestFileBytes { get; set; }
    public int MaximumRequestedFileBytes { get; set; }
    public int MinimumLearningItemsPerSource { get; set; }
    public int DefaultLearningItemsPerSource { get; set; }
    public int MaximumLearningItemsPerRound { get; set; }
}

/// <summary>Database-backed operational bounds for Local AI media runtimes.</summary>
public sealed class LocalAiRuntimeParameters
{
    public int MinimumImageDimension { get; set; }
    public int MaximumImageDimension { get; set; }
    public int MaximumVideoWidth { get; set; }
    public int MaximumVideoHeight { get; set; }
    public int MinimumGenerationSteps { get; set; }
    public int MaximumGenerationSteps { get; set; }
    public int MinimumGuidanceScale { get; set; }
    public int MaximumGuidanceScale { get; set; }
    public int MinimumVideoFrames { get; set; }
    public int MaximumVideoFrames { get; set; }
    public int MinimumFramesPerSecond { get; set; }
    public int MaximumFramesPerSecond { get; set; }
    public int MinimumInputMegabytes { get; set; }
    public int MaximumInputMegabytes { get; set; }
    public long MinimumImagePixels { get; set; }
    public long MaximumImagePixels { get; set; }
    public int MinimumAudioSeconds { get; set; }
    public int MaximumAudioSeconds { get; set; }
    public int MinimumAudioBytesPerSecond { get; set; }
    public int MaximumAudioBytesPerSecond { get; set; }
    public int MinimumQueueCapacity { get; set; }
    public int MaximumQueueCapacity { get; set; }
}

/// <summary>Database-backed operational bounds for model preset normalization.</summary>
public sealed class ModelPresetRuntimeParameters
{
    public int MinimumPresetOutputTokens { get; set; }
    public int MaximumPresetOutputTokens { get; set; }
    public int MinimumPresetContextTokens { get; set; }
    public int MaximumPresetContextTokens { get; set; }
    public int MinimumRouteOutputTokens { get; set; }
    public int MinimumRouteContextTokens { get; set; }
    public int MaximumRouteTokens { get; set; }
}

/// <summary>Database-backed operational timing and limits for 1-Wire services.</summary>
public sealed class OneWireRuntimeParameters
{
    public int MinimumPairingLifetimeMinutes { get; set; }
    public int MaximumPairingLifetimeMinutes { get; set; }
    public int DefaultPairingLifetimeMinutes { get; set; }
    public int MinimumTokenValidityMinutes { get; set; }
    public int MaximumTokenValidityMinutes { get; set; }
    public int MinimumBroadcastIntervalSeconds { get; set; }
    public int MaximumBroadcastIntervalSeconds { get; set; }
    public int MinimumMessageBytes { get; set; }
    public int MinimumOrganicCallSeconds { get; set; }
    public int MaximumOrganicCallSeconds { get; set; }
    public int DefaultOrganicCallSeconds { get; set; }
    public int MinimumOrganicResultCharacters { get; set; }
    public int MaximumOrganicResultCharacters { get; set; }
    public int DefaultOrganicResultCharacters { get; set; }
}

/// <summary>Database-backed operational bounds for project maintenance and automation.</summary>
public sealed class ProjectMaintenanceRuntimeParameters
{
    public int MinimumBuildReviewTimeoutSeconds { get; set; }
    public int MaximumBuildReviewTimeoutSeconds { get; set; }
    public int MinimumPriority { get; set; }
    public int MaximumPriority { get; set; }
    public int MaximumAccessRoots { get; set; }
    public int MaximumAccessRules { get; set; }
    public int MaximumAccessMatches { get; set; }
}

/// <summary>Database-backed operational bounds for Python.NET coordination.</summary>
public sealed class PythonNetRuntimeParameters
{
    public int DefaultQueueCapacity { get; set; }
    public int MinimumQueueCapacity { get; set; }
    public int MaximumQueueCapacity { get; set; }
}

/// <summary>Database-backed operational bounds for regex compilation and persistence services.</summary>
public sealed class RegexRuntimeParameters
{
    public int MaximumPatternCharacters { get; set; }
    public int DefaultTimeoutSeconds { get; set; }
    public int MaximumTimeoutSeconds { get; set; }
    public int MaximumStoredPatternListItems { get; set; }
}

/// <summary>Database-backed operational bounds for remote repository/knowledge import.</summary>
public sealed class RemoteKnowledgeRuntimeParameters
{
    public int HttpTimeoutMinutes { get; set; }
    public int MaximumProjects { get; set; }
    public int DownloadBufferBytes { get; set; }
}

/// <summary>Database-backed operational bounds for toolchain discovery and knowledge services.</summary>
public sealed class ToolchainRuntimeParameters
{
    public int DefaultCandidates { get; set; }
    public int MinimumCandidates { get; set; }
    public int MaximumCandidates { get; set; }
    public int MinimumSearchDepth { get; set; }
    public int MaximumSearchDepth { get; set; }
    public int MaximumVisitedDirectories { get; set; }
    public int MaximumRuntimeResults { get; set; }
    public int StreamBufferBytes { get; set; }
}

/// <summary>Database-backed operational bounds for Hugging Face catalog queries.</summary>
public sealed class HuggingFaceCatalogRuntimeParameters
{
    public int MinimumResults { get; set; }
    public int MaximumResults { get; set; }
    public int MinimumRecentWindowDays { get; set; }
    public int MaximumRecentWindowDays { get; set; }
}

/// <summary>Database-backed operational bounds for embedded firmware planning.</summary>
public sealed class EmbeddedFirmwareRuntimeParameters
{
    public int MinimumTelemetryIntervalMilliseconds { get; set; }
    public int MaximumTelemetryIntervalMilliseconds { get; set; }
}

/// <summary>Database-backed service timing defaults that are operational policy rather than mathematical invariants.</summary>
public sealed class ServiceTimingRuntimeParameters
{
    public int UploadContextFreshnessMinutes { get; set; }
    public int AiDiscoveryTimeoutSeconds { get; set; }
    public int ConfiguredChatClientTimeoutMinutes { get; set; }
    public int ChatClientProbeTimeoutSeconds { get; set; }
    public int CouncilHeartbeatSeconds { get; set; }
    public int RemoteControlPollingSeconds { get; set; }
    public int HardwareInventoryCacheMinutes { get; set; }
    public int CouncilSpoolerPersistenceDelayMilliseconds { get; set; }
}
