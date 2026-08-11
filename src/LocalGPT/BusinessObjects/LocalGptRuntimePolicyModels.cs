using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Lists supported local gpt runtime value values.
/// </summary>
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

/// <summary>
/// Lists supported local gpt runtime collection values.
/// </summary>
public enum LocalGptRuntimeCollection
{
    AllowedNativeExecutables,
    DebugExtensions,
    TextExtensions,
    BinaryDiagnosticExtensions,
    ExcludedDirectoryNames,
    BinaryExtensions,
    SourceExtensions,
    LearnBaseKnownExtensions,
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

/// <summary>
/// Lists supported local gpt runtime pattern values.
/// </summary>
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

/// <summary>
/// Represents a local gpt runtime system variable seed.
/// </summary>
public sealed record LocalGptRuntimeSystemVariableSeed(LocalGptRuntimeValue Key, string Name, string Value, string DataType);
/// <summary>
/// Represents a local gpt runtime collection seed.
/// </summary>
public sealed record LocalGptRuntimeCollectionSeed(LocalGptRuntimeCollection Key, string Name, IReadOnlyList<string> Values);
/// <summary>
/// Represents a local gpt runtime regex seed.
/// </summary>
public sealed record LocalGptRuntimeRegexSeed(LocalGptRuntimePattern Key, string Name, string Pattern, string Flags);

/// <summary>
/// Represents a local gpt runtime policy seed model.
/// </summary>
public sealed record LocalGptRuntimePolicySeedModel
{
    /// <summary>
    /// Gets or sets local gpt core project identifier.
    /// </summary>
    public Guid LocalGptCoreProjectId => Guid.Parse(Values.Single(item => item.Key == LocalGptRuntimeValue.LocalGptCoreProjectId).Value);
    /// <summary>
    /// Gets or sets values.
    /// </summary>
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> Values { get; init; } = [];
    /// <summary>
    /// Gets or sets collections.
    /// </summary>
    public IReadOnlyList<LocalGptRuntimeCollectionSeed> Collections { get; init; } = [];
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public IReadOnlyList<LocalGptRuntimeRegexSeed> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets or sets system variables.
    /// </summary>
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> SystemVariables => [.. Values, .. Collections.Select(item => new LocalGptRuntimeSystemVariableSeed((LocalGptRuntimeValue)(-1), item.Name, System.Text.Json.JsonSerializer.Serialize(item.Values), typeof(string[]).FullName ?? "System.String[]"))];
}

/// <summary>
/// Represents a local gpt runtime regex definition.
/// </summary>
public sealed record LocalGptRuntimeRegexDefinition
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public LocalGptRuntimePattern Key { get; init; }
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets pattern.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets flags.
    /// </summary>
    public string Flags { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets updated on.
    /// </summary>
    public DateTime UpdatedOn { get; init; }
}

/// <summary>
/// Represents a local gpt runtime policy definition.
/// </summary>
public sealed record LocalGptRuntimePolicyDefinition
{
    /// <summary>
    /// Gets or sets values.
    /// </summary>
    public IReadOnlyDictionary<LocalGptRuntimeValue, string> Values { get; init; } = new Dictionary<LocalGptRuntimeValue, string>();
    /// <summary>
    /// Gets or sets collections.
    /// </summary>
    public IReadOnlyDictionary<LocalGptRuntimeCollection, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<LocalGptRuntimeCollection, IReadOnlyList<string>>();
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public IReadOnlyDictionary<LocalGptRuntimePattern, LocalGptRuntimeRegexDefinition> RegexPatterns { get; init; } = new Dictionary<LocalGptRuntimePattern, LocalGptRuntimeRegexDefinition>();
}

/// <summary>
/// Represents a local gpt runtime policy snapshot.
/// </summary>
public sealed record LocalGptRuntimePolicySnapshot
{
    /// <summary>
    /// Gets or sets values.
    /// </summary>
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
    /// <summary>
    /// Gets or sets collections.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    /// <summary>
    /// Gets or sets regex patterns.
    /// </summary>
    public IReadOnlyDictionary<string, string> RegexPatterns { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a local gpt vocabulary snapshot.
/// </summary>
public sealed class LocalGptVocabularySnapshot
{
    /// <summary>
    /// Gets or sets council spooler running.
    /// </summary>
    public string CouncilSpoolerRunning { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council spooler completed.
    /// </summary>
    public string CouncilSpoolerCompleted { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council spooler failed.
    /// </summary>
    public string CouncilSpoolerFailed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human request approval.
    /// </summary>
    public string HumanRequestApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human request feedback.
    /// </summary>
    public string HumanRequestFeedback { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human request guidance.
    /// </summary>
    public string HumanRequestGuidance { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human status pending.
    /// </summary>
    public string HumanStatusPending { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human status approved.
    /// </summary>
    public string HumanStatusApproved { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human status declined.
    /// </summary>
    public string HumanStatusDeclined { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human status answered.
    /// </summary>
    public string HumanStatusAnswered { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human status consumed.
    /// </summary>
    public string HumanStatusConsumed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets human status expired.
    /// </summary>
    public string HumanStatusExpired { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets contribution queued.
    /// </summary>
    public string ContributionQueued { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets contribution injected.
    /// </summary>
    public string ContributionInjected { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets contribution evaluated.
    /// </summary>
    public string ContributionEvaluated { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets verdict pending.
    /// </summary>
    public string VerdictPending { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets verdict supported.
    /// </summary>
    public string VerdictSupported { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets verdict needs correction.
    /// </summary>
    public string VerdictNeedsCorrection { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets verdict mixed.
    /// </summary>
    public string VerdictMixed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets verdict not reviewed.
    /// </summary>
    public string VerdictNotReviewed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets deferred pending approval.
    /// </summary>
    public string DeferredPendingApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets deferred executing.
    /// </summary>
    public string DeferredExecuting { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets deferred completed.
    /// </summary>
    public string DeferredCompleted { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets deferred failed.
    /// </summary>
    public string DeferredFailed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets deferred declined.
    /// </summary>
    public string DeferredDeclined { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets deferred completed elsewhere.
    /// </summary>
    public string DeferredCompletedElsewhere { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor system.
    /// </summary>
    public string ActorSystem { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor human.
    /// </summary>
    public string ActorHuman { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor ai model.
    /// </summary>
    public string ActorAiModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor council.
    /// </summary>
    public string ActorCouncil { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets actor API client.
    /// </summary>
    public string ActorApiClient { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets authority none.
    /// </summary>
    public string AuthorityNone { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets authority human interaction.
    /// </summary>
    public string AuthorityHumanInteraction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets authority human approval.
    /// </summary>
    public string AuthorityHumanApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets catalog DevExpress function.
    /// </summary>
    public string CatalogDxFunction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets catalog public service method.
    /// </summary>
    public string CatalogPublicServiceMethod { get; set; } = string.Empty;
}
