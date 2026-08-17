using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported LocalGPT runtime value values used to select or describe behavior in the surrounding workflow.
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
/// Defines the supported LocalGPT runtime collection values used to select or describe behavior in the surrounding workflow.
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
    ArchitectureLanguageToolchainOptions,
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
/// Defines the supported LocalGPT runtime pattern values used to select or describe behavior in the surrounding workflow.
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
    ToolchainKnowledgeBlock,
    ToolchainVersionToken,
    ToolchainEnvironmentToken,
}

/// <summary>
/// Represents a LocalGPT runtime system variable seed application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Key">Key value supplied to the LocalGPT runtime system variable seed operation and used when producing its result.</param>
/// <param name="Name">Name value supplied to the LocalGPT runtime system variable seed operation and used when producing its result.</param>
/// <param name="Value">Value value supplied to the LocalGPT runtime system variable seed operation and used when producing its result.</param>
/// <param name="DataType">Data type value supplied to the LocalGPT runtime system variable seed operation and used when producing its result.</param>
public sealed record LocalGptRuntimeSystemVariableSeed(LocalGptRuntimeValue Key, string Name, string Value, string DataType);
/// <summary>
/// Represents a LocalGPT runtime collection seed application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Key">Key value supplied to the LocalGPT runtime collection seed operation and used when producing its result.</param>
/// <param name="Name">Name value supplied to the LocalGPT runtime collection seed operation and used when producing its result.</param>
/// <param name="Values">String dependency used by the LocalGPT runtime collection seed workflow to provide the corresponding application capability.</param>
public sealed record LocalGptRuntimeCollectionSeed(LocalGptRuntimeCollection Key, string Name, IReadOnlyList<string> Values);
/// <summary>
/// Represents a LocalGPT runtime regex seed application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Key">Key value supplied to the LocalGPT runtime regex seed operation and used when producing its result.</param>
/// <param name="Name">Name value supplied to the LocalGPT runtime regex seed operation and used when producing its result.</param>
/// <param name="Pattern">Pattern value supplied to the LocalGPT runtime regex seed operation and used when producing its result.</param>
/// <param name="Flags">Flags value supplied to the LocalGPT runtime regex seed operation and used when producing its result.</param>
public sealed record LocalGptRuntimeRegexSeed(LocalGptRuntimePattern Key, string Name, string Pattern, string Flags);

/// <summary>
/// Represents LocalGPT runtime policy seed state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed record LocalGptRuntimePolicySeedModel
{
    /// <summary>
    /// Gets the stable LocalGPT core project identifier used to identify or correlate this LocalGPT runtime policy seed instance with related application state.
    /// </summary>
    /// <value>The LocalGPT core project identifier value exposed by <see cref="LocalGptRuntimePolicySeedModel"/>.</value>
    public Guid LocalGptCoreProjectId => Guid.Parse(Values.Single(item => item.Key == LocalGptRuntimeValue.LocalGptCoreProjectId).Value);
    /// <summary>
    /// Gets or sets the values collection maintained or exposed by this LocalGPT runtime policy seed instance for downstream processing.
    /// </summary>
    /// <value>The values value exposed by <see cref="LocalGptRuntimePolicySeedModel"/>.</value>
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> Values { get; init; } = [];
    /// <summary>
    /// Gets or sets the collections collection maintained or exposed by this LocalGPT runtime policy seed instance for downstream processing.
    /// </summary>
    /// <value>The collections value exposed by <see cref="LocalGptRuntimePolicySeedModel"/>.</value>
    public IReadOnlyList<LocalGptRuntimeCollectionSeed> Collections { get; init; } = [];
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this LocalGPT runtime policy seed instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LocalGptRuntimePolicySeedModel"/>.</value>
    public IReadOnlyList<LocalGptRuntimeRegexSeed> RegexPatterns { get; init; } = [];
    /// <summary>
    /// Gets the system variables collection maintained or exposed by this LocalGPT runtime policy seed instance for downstream processing.
    /// </summary>
    /// <value>The system variables value exposed by <see cref="LocalGptRuntimePolicySeedModel"/>.</value>
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> SystemVariables => [.. Values, .. Collections.Select(item => new LocalGptRuntimeSystemVariableSeed((LocalGptRuntimeValue)(-1), item.Name, System.Text.Json.JsonSerializer.Serialize(item.Values), typeof(string[]).FullName ?? "System.String[]"))];
}

/// <summary>
/// Represents a LocalGPT runtime regex definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record LocalGptRuntimeRegexDefinition
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this LocalGPT runtime regex definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="LocalGptRuntimeRegexDefinition"/>.</value>
    public LocalGptRuntimePattern Key { get; init; }
    /// <summary>
    /// Gets or sets the name value that forms part of the LocalGPT runtime regex definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="LocalGptRuntimeRegexDefinition"/>.</value>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the pattern value that forms part of the LocalGPT runtime regex definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pattern value exposed by <see cref="LocalGptRuntimeRegexDefinition"/>.</value>
    public string Pattern { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the flags value that forms part of the LocalGPT runtime regex definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The flags value exposed by <see cref="LocalGptRuntimeRegexDefinition"/>.</value>
    public string Flags { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the updated on value that forms part of the LocalGPT runtime regex definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The updated on value exposed by <see cref="LocalGptRuntimeRegexDefinition"/>.</value>
    public DateTime UpdatedOn { get; init; }
}

/// <summary>
/// Represents a LocalGPT runtime policy definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record LocalGptRuntimePolicyDefinition
{
    /// <summary>
    /// Gets or sets the values collection maintained or exposed by this LocalGPT runtime policy definition instance for downstream processing.
    /// </summary>
    /// <value>The values value exposed by <see cref="LocalGptRuntimePolicyDefinition"/>.</value>
    public IReadOnlyDictionary<LocalGptRuntimeValue, string> Values { get; init; } = new Dictionary<LocalGptRuntimeValue, string>();
    /// <summary>
    /// Gets or sets the collections collection maintained or exposed by this LocalGPT runtime policy definition instance for downstream processing.
    /// </summary>
    /// <value>The collections value exposed by <see cref="LocalGptRuntimePolicyDefinition"/>.</value>
    public IReadOnlyDictionary<LocalGptRuntimeCollection, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<LocalGptRuntimeCollection, IReadOnlyList<string>>();
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this LocalGPT runtime policy definition instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LocalGptRuntimePolicyDefinition"/>.</value>
    public IReadOnlyDictionary<LocalGptRuntimePattern, LocalGptRuntimeRegexDefinition> RegexPatterns { get; init; } = new Dictionary<LocalGptRuntimePattern, LocalGptRuntimeRegexDefinition>();
}

/// <summary>
/// Represents a LocalGPT runtime policy snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed record LocalGptRuntimePolicySnapshot
{
    /// <summary>
    /// Gets or sets the values collection maintained or exposed by this LocalGPT runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The values value exposed by <see cref="LocalGptRuntimePolicySnapshot"/>.</value>
    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>();
    /// <summary>
    /// Gets or sets the collections collection maintained or exposed by this LocalGPT runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The collections value exposed by <see cref="LocalGptRuntimePolicySnapshot"/>.</value>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Collections { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    /// <summary>
    /// Gets or sets the regex patterns collection maintained or exposed by this LocalGPT runtime policy snapshot instance for downstream processing.
    /// </summary>
    /// <value>The regex patterns value exposed by <see cref="LocalGptRuntimePolicySnapshot"/>.</value>
    public IReadOnlyDictionary<string, string> RegexPatterns { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a LocalGPT vocabulary snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptVocabularySnapshot
{
    /// <summary>
    /// Gets or sets the council spooler running value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council spooler running value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CouncilSpoolerRunning { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council spooler completed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council spooler completed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CouncilSpoolerCompleted { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council spooler failed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council spooler failed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CouncilSpoolerFailed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human request approval value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human request approval value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanRequestApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human request feedback value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human request feedback value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanRequestFeedback { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human request guidance value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human request guidance value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanRequestGuidance { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status pending value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status pending value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusPending { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status approved value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status approved value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusApproved { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status declined value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status declined value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusDeclined { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status answered value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status answered value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusAnswered { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status consumed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status consumed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusConsumed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the human status expired value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human status expired value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string HumanStatusExpired { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contribution queued value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contribution queued value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ContributionQueued { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contribution injected value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contribution injected value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ContributionInjected { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the contribution evaluated value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The contribution evaluated value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ContributionEvaluated { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict pending value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict pending value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictPending { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict supported value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict supported value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictSupported { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict needs correction value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict needs correction value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictNeedsCorrection { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict mixed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict mixed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictMixed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the verdict not reviewed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The verdict not reviewed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string VerdictNotReviewed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred pending approval value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred pending approval value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredPendingApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred executing value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred executing value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredExecuting { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred completed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred completed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredCompleted { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred failed value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred failed value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredFailed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred declined value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred declined value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredDeclined { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the deferred completed elsewhere value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The deferred completed elsewhere value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string DeferredCompletedElsewhere { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor system value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor system value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorSystem { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor human value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor human value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorHuman { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor AI model value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor AI model value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorAiModel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor council value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor council value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorCouncil { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor API client value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor API client value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string ActorApiClient { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authority none value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authority none value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string AuthorityNone { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authority human interaction value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authority human interaction value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string AuthorityHumanInteraction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authority human approval value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authority human approval value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string AuthorityHumanApproval { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the catalog DevExpress function value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The catalog DevExpress function value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CatalogDxFunction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the catalog public service method value that forms part of the LocalGPT vocabulary snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The catalog public service method value exposed by <see cref="LocalGptVocabularySnapshot"/>.</value>
    public string CatalogPublicServiceMethod { get; set; } = string.Empty;
}
