using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

public enum LocalGptRuntimeValue
{
    LocalGptCoreProjectId,
    RegexTimeoutMilliseconds,
    LocalHumanProfileId,
    CommandPolicyAllowedDecision,
    CommandPolicyDeniedDecision,
    CommandPolicyDeniedProfile,
    DefaultGradleVersion,
    DefaultMinecraftVersion,
    DefaultJavaVersion,
    FabricLoaderVersion,
    MaxDxAiChatPromptCharacters,
    MaxVisiblePromptCharacters,
    DefaultOllamaUri,
    MaxParticipants,
    DefaultMaxParallelModels,
    DefaultHeavyModelGpuLayers,
    MinContextTokens,
    DefaultContextTokens,
    MaxContextTokens,
    MinOutputTokens,
    MaxOutputTokens,
    MaxArtifactTextFileBytes,
    MaxFiles,
    MaxSingleFileBytes,
    MaxTotalFileBytes,
    MaxZipEntries,
    MaxZipEntryBytes,
    MaxExtractedBytes,
    MaxContextCharacters,
    MaxExcerptCharactersPerFile,
    MaxBinaryStringCharacters,
    ContextOmission,
    ShortContextOmission,
    LearnBaseFilePolicySummary,
    LearnBaseDuplicatePolicySummary,
    MinCouncilOutputTokens,
    DefaultCouncilOutputTokens,
    MaxCouncilOutputTokens,
    MinCouncilContextTokens,
    DefaultCouncilContextTokens,
    MaxCouncilContextTokens,
    CouncilSessionName,
    MaxUploadFiles,
    MaxUploadBytes,
    OllamaModeAutoGpu,
    OllamaModeSafeCpu,
    OllamaModeLimitedGpu,
    DetectedOllamaSessionPrefix,
    DefaultOllamaEndpoint,
    DefaultMaxPromptCharacters,
    MaxPromptCharacters,
    MaxBootstrapCharacters,
    MaxSingleConversationMessageCharacters,
    ApplicationDefaultPort,
    ProtocolVersion,
    ProtocolMinimumCompatibleVersion,
    ProtocolDefaultServicePort,
    ProtocolDefaultDiscoveryPort,
    ProtocolMaximumMessageBytes,
    ProtocolMaximumDiscoveryBytes,
    ArtifactBuildMinimumTimeoutSeconds,
    ArtifactBuildMaximumTimeoutSeconds,
    CodeGenerationMaximumPayloadCharacters,
    CodeGenerationMaximumFileCount,
    CodeGenerationMaximumReviewTake,
    ComponentActivityCapacity,
    ComponentActivityMaximumSummaryCharacters,
    RuntimeCapabilityRefreshWarning,
    CouncilCodeGenerationMaximumEmbeddedPlanCharacters,
    CouncilTeamSeedVersion,
    DebugArtifactMaximumInspectionBytes,
    DeferredDxAiMaximumResultCharacters,
    DxAiFunctionCatalogDataType,
    FormattingCollapsedThinkingStart,
    FormattingLiveThinkingStart,
    FormattingThinkStartTag,
    FormattingThinkEndTag,
    FormattingTagLookbehindLength,
    FormattingMissingFinalAnswerNotice,
    HardwareGpuInventoryScript,
    HumanCollaborationMaximumTextLength,
    NativeCommandMinimumTimeoutSeconds,
    NativeCommandMaximumTimeoutSeconds,
    NavigationToggleSidebarName,
    OllamaMaximumAutomaticToolRounds,
    OllamaMaximumToolResultCharacters,
    LocalVisionMaximumImageBytes,
    OneWireSecuritySchemaVersion,
    OneWireTotpPeriodSeconds,
    OneWireTotpAlphabet,
    SqliteTableEditorMaximumRows,
    ProjectMaintenanceMaximumCompilerCandidates,
    ProjectMaintenanceMaximumCapturedCharacters,
    ProjectOrganicArtifactKind,
    ProjectOrganicArtifactName,
    SafeTextDocumentMaximumBytes,
    ThemeDefaultName,
    ThemeContractPath,
    BootstrapDarkModePostfix,
    ProjectMaintenanceToastName,
    ProjectToastName,
    DatabaseMigrationOrganicSkillTableRepairSql,
    DatabaseMigrationOrganicSkillIndexRepairSql,
    DatabaseMigrationCouncilTeamTableRepairSql,
    DatabaseMigrationCouncilTeamIndexRepairSql,
    SqliteGuidExpression,
    LearnBasePresetsJson,
    LearnBaseScanProfilesJson,
    TestLabRoutesJson,
    VocabularyJson,
}

public enum LocalGptRuntimeCollection
{
    AllowedNativeExecutables,
    DebugExtensions,
    TextExtensions,
    BinaryDiagnosticExtensions,
    ExcludedDirectoryNames,
    BinaryExtensions,
    SourceExtensions,
    ArtifactTextExtensions,
    KnowledgeFiles,
    AllowedUploadExtensions,
    AllowedUploadMimeTypes,
    ArchitectureUiStackOptions,
    ArchitectureSolutionShapeOptions,
    ArchitectureRenderModeOptions,
    ArchitectureReferenceLookOptions,
    ProjectRequirementTargetKinds,
    ProjectArtifactKinds,
    ChatHarmonyModelHints,
    ChatDeepSeekModelHints,
    ChatDeepSeekControlTokens,
    ChatGemmaModelHints,
    ChatGemmaControlTokens,
    ChatAppleModelHints,
    ChatAppleControlTokens,
    ChatThinkTagsModelHints,
}

public enum LocalGptRuntimePattern
{
    NameCleaner,
    ModIdCleaner,
    PackagePartCleaner,
    MissingFeaturePattern,
    CapabilityGapBlockPattern,
    TruncatedTailPattern,
    ThinkingBlockPattern,
    CouncilPromptFencePattern,
    CouncilRequestBlockPattern,
    TargetFrameworkPattern,
    PackageReferencePattern,
    SensitiveNamePattern,
    StreamStatusPattern,
    WordPattern,
    DevelopmentRequestPattern,
    ExplicitArtifactIntentPattern,
    AdviceOnlyPromptPattern,
    ExplicitArtifactCreationCommandPattern,
    ConcreteMinecraftArtifactPattern,
    ConcreteDotNetArtifactPattern,
    AiHostSetupPattern,
    ImplementationDecisionPattern,
    ImplementationChoicePattern,
    BlockingArtifactDecisionPattern,
    SafeSandboxConsentPattern,
    ExplicitDoNotGenerateUntilUserDecisionPattern,
    DeveloperExecutionIntentPattern,
    DevExpressImportPattern,
    DevExpressRegistrationPattern,
    DevExpressDocumentPattern,
    ExportFormatPattern,
    BlazorFrontendPattern,
    DotNetPattern,
    MinecraftPattern,
    DatapackPattern,
    MinecraftSkeletonMatrixPattern,
    MinecraftVersionPattern,
    LeadingSlashCommandPattern,
    RootStorageRemovePattern,
    MalformedStorageTargetPattern,
    FrontendPattern,
    WholeSolutionPattern,
    AiHostExperimentPattern,
    LocalGptReplacementPattern,
    TacosPortalPattern,
    BotBackendPattern,
    LoggingPattern,
    WhitespacePattern,
    HelpfulSourceLinePattern,
    LocalGptKnowledgeBlock,
    LocalGptSelfAssessmentBlock,
    SolutionProjectReference,
    CSharpNamespace,
    CSharpServiceRegistration,
    AspNetControllerRoute,
    DotNetSolutionProject,
    InstallerPortContract,
    OneWireCapabilityKey,
    FilePathWithExtension,
    PowerShellInlineCommand,
    PowerShellFile,
    SensitiveArgument,
    DownloadUrl,
    ModelCapabilitySelfAssessment,
    CouncilTaggedPlan,
    CouncilFencedPlan,
    ChatHarmonyThinking,
    ChatHarmonyFinal,
    ChatHarmonyMarker,
    RenderThinkingDetailsStart,
    CouncilCompletionMarker,
    ListAfterHtml,
    ControlledDetailsStart,
    DetailsEnd,
    StablePanelStart,
    StreamIdAttribute,
    PreStart,
    PreEnd,
}

public sealed record LocalGptRuntimeSystemVariableSeed(LocalGptRuntimeValue Key, string Name, string Value, string DataType);
public sealed record LocalGptRuntimeCollectionSeed(LocalGptRuntimeCollection Key, string Name, IReadOnlyList<string> Values);
public sealed record LocalGptRuntimeRegexSeed(LocalGptRuntimePattern Key, string Name, string Pattern, string Flags);

public sealed record LocalGptRuntimePolicySeedModel
{
    public Guid LocalGptCoreProjectId => Guid.Parse(Values.Single(item => item.Key == LocalGptRuntimeValue.LocalGptCoreProjectId).Value);
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> Values { get; init; } = [];
    public IReadOnlyList<LocalGptRuntimeCollectionSeed> Collections { get; init; } = [];
    public IReadOnlyList<LocalGptRuntimeRegexSeed> RegexPatterns { get; init; } = [];
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> SystemVariables => [.. Values, .. Collections.Select(item => new LocalGptRuntimeSystemVariableSeed((LocalGptRuntimeValue)(-1), item.Name, System.Text.Json.JsonSerializer.Serialize(item.Values), typeof(string[]).FullName ?? "System.String[]"))];
}

public sealed record LocalGptRuntimeRegexDefinition
{
    public LocalGptRuntimePattern Key { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public string Flags { get; init; } = string.Empty;
    public DateTime UpdatedOn { get; init; }
}

public sealed record LocalGptRuntimePolicyDefinition
{
    public IReadOnlyDictionary<LocalGptRuntimeValue, string> Values { get; init; } = new Dictionary<LocalGptRuntimeValue, string>();
    public IReadOnlyDictionary<LocalGptRuntimeCollection, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<LocalGptRuntimeCollection, IReadOnlyList<string>>();
    public IReadOnlyDictionary<LocalGptRuntimePattern, LocalGptRuntimeRegexDefinition> RegexPatterns { get; init; } = new Dictionary<LocalGptRuntimePattern, LocalGptRuntimeRegexDefinition>();
}

public sealed record LocalGptRuntimePolicySnapshot
{
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    public IReadOnlyDictionary<string, string> RegexPatterns { get; init; } = new Dictionary<string, string>();
}

public sealed class LocalGptVocabularySnapshot
{
    public string CouncilSpoolerRunning { get; set; } = string.Empty;
    public string CouncilSpoolerCompleted { get; set; } = string.Empty;
    public string CouncilSpoolerFailed { get; set; } = string.Empty;
    public string HumanRequestApproval { get; set; } = string.Empty;
    public string HumanRequestFeedback { get; set; } = string.Empty;
    public string HumanRequestGuidance { get; set; } = string.Empty;
    public string HumanStatusPending { get; set; } = string.Empty;
    public string HumanStatusApproved { get; set; } = string.Empty;
    public string HumanStatusDeclined { get; set; } = string.Empty;
    public string HumanStatusAnswered { get; set; } = string.Empty;
    public string HumanStatusConsumed { get; set; } = string.Empty;
    public string HumanStatusExpired { get; set; } = string.Empty;
    public string ContributionQueued { get; set; } = string.Empty;
    public string ContributionInjected { get; set; } = string.Empty;
    public string ContributionEvaluated { get; set; } = string.Empty;
    public string VerdictPending { get; set; } = string.Empty;
    public string VerdictSupported { get; set; } = string.Empty;
    public string VerdictNeedsCorrection { get; set; } = string.Empty;
    public string VerdictMixed { get; set; } = string.Empty;
    public string VerdictNotReviewed { get; set; } = string.Empty;
    public string DeferredPendingApproval { get; set; } = string.Empty;
    public string DeferredExecuting { get; set; } = string.Empty;
    public string DeferredCompleted { get; set; } = string.Empty;
    public string DeferredFailed { get; set; } = string.Empty;
    public string DeferredDeclined { get; set; } = string.Empty;
    public string DeferredCompletedElsewhere { get; set; } = string.Empty;
    public string ActorSystem { get; set; } = string.Empty;
    public string ActorHuman { get; set; } = string.Empty;
    public string ActorAiModel { get; set; } = string.Empty;
    public string ActorCouncil { get; set; } = string.Empty;
    public string ActorApiClient { get; set; } = string.Empty;
    public string AuthorityNone { get; set; } = string.Empty;
    public string AuthorityHumanInteraction { get; set; } = string.Empty;
    public string AuthorityHumanApproval { get; set; } = string.Empty;
    public string CatalogDxFunction { get; set; } = string.Empty;
    public string CatalogPublicServiceMethod { get; set; } = string.Empty;
}
