using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported LocalGPT runtime value values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum LocalGptRuntimeValue
{
    /// <summary>
    /// Selects the local GPT core project identifier option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalGptCoreProjectId,
    /// <summary>
    /// Selects the regex timeout milliseconds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RegexTimeoutMilliseconds,
    /// <summary>
    /// Selects the local human profile identifier option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalHumanProfileId,
    /// <summary>
    /// Selects the command policy allowed decision option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CommandPolicyAllowedDecision,
    /// <summary>
    /// Selects the command policy denied decision option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CommandPolicyDeniedDecision,
    /// <summary>
    /// Selects the command policy denied profile option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CommandPolicyDeniedProfile,
    /// <summary>
    /// Selects the default gradle version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultGradleVersion,
    /// <summary>
    /// Selects the default minecraft version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultMinecraftVersion,
    /// <summary>
    /// Selects the default java version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultJavaVersion,
    /// <summary>
    /// Selects the fabric loader version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FabricLoaderVersion,
    /// <summary>
    /// Selects the max DevExpress AI chat prompt characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxDxAiChatPromptCharacters,
    /// <summary>
    /// Selects the max visible prompt characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxVisiblePromptCharacters,
    /// <summary>
    /// Selects the default Ollama URI option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultOllamaUri,
    /// <summary>
    /// Selects the max participants option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxParticipants,
    /// <summary>
    /// Selects the default max parallel models option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultMaxParallelModels,
    /// <summary>
    /// Selects the default heavy model GPU layers option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultHeavyModelGpuLayers,
    /// <summary>
    /// Selects the min context tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinContextTokens,
    /// <summary>
    /// Selects the default context tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultContextTokens,
    /// <summary>
    /// Selects the max context tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxContextTokens,
    /// <summary>
    /// Selects the min output tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinOutputTokens,
    /// <summary>
    /// Selects the max output tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxOutputTokens,
    /// <summary>
    /// Selects the max artifact text file bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxArtifactTextFileBytes,
    /// <summary>
    /// Selects the max files option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxFiles,
    /// <summary>
    /// Selects the max single file bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxSingleFileBytes,
    /// <summary>
    /// Selects the max total file bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxTotalFileBytes,
    /// <summary>
    /// Selects the max ZIP entries option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxZipEntries,
    /// <summary>
    /// Selects the max ZIP entry bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxZipEntryBytes,
    /// <summary>
    /// Selects the max extracted bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxExtractedBytes,
    /// <summary>
    /// Selects the max context characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxContextCharacters,
    /// <summary>
    /// Selects the max excerpt characters per file option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxExcerptCharactersPerFile,
    /// <summary>
    /// Selects the max binary string characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxBinaryStringCharacters,
    /// <summary>
    /// Selects the context omission option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ContextOmission,
    /// <summary>
    /// Selects the short context omission option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ShortContextOmission,
    /// <summary>
    /// Selects the learn base file policy summary option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LearnBaseFilePolicySummary,
    /// <summary>
    /// Selects the learn base duplicate policy summary option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LearnBaseDuplicatePolicySummary,
    /// <summary>
    /// Selects the min council output tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinCouncilOutputTokens,
    /// <summary>
    /// Selects the default council output tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultCouncilOutputTokens,
    /// <summary>
    /// Selects the max council output tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxCouncilOutputTokens,
    /// <summary>
    /// Selects the min council context tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinCouncilContextTokens,
    /// <summary>
    /// Selects the default council context tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultCouncilContextTokens,
    /// <summary>
    /// Selects the max council context tokens option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxCouncilContextTokens,
    /// <summary>
    /// Selects the council session name option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilSessionName,
    /// <summary>
    /// Selects the max upload files option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxUploadFiles,
    /// <summary>
    /// Selects the max upload bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxUploadBytes,
    /// <summary>
    /// Selects the Ollama mode auto GPU option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OllamaModeAutoGpu,
    /// <summary>
    /// Selects the Ollama mode safe CPU option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OllamaModeSafeCpu,
    /// <summary>
    /// Selects the Ollama mode limited GPU option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OllamaModeLimitedGpu,
    /// <summary>
    /// Selects the detected Ollama session prefix option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DetectedOllamaSessionPrefix,
    /// <summary>
    /// Selects the default Ollama endpoint option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultOllamaEndpoint,
    /// <summary>
    /// Selects the default max prompt characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DefaultMaxPromptCharacters,
    /// <summary>
    /// Selects the max prompt characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxPromptCharacters,
    /// <summary>
    /// Selects the max bootstrap characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxBootstrapCharacters,
    /// <summary>
    /// Selects the max single conversation message characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MaxSingleConversationMessageCharacters,
    /// <summary>
    /// Selects the application default port option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ApplicationDefaultPort,
    /// <summary>
    /// Selects the protocol version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProtocolVersion,
    /// <summary>
    /// Selects the protocol minimum compatible version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProtocolMinimumCompatibleVersion,
    /// <summary>
    /// Selects the protocol default service port option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProtocolDefaultServicePort,
    /// <summary>
    /// Selects the protocol default discovery port option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProtocolDefaultDiscoveryPort,
    /// <summary>
    /// Selects the protocol maximum message bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProtocolMaximumMessageBytes,
    /// <summary>
    /// Selects the protocol maximum discovery bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProtocolMaximumDiscoveryBytes,
    /// <summary>
    /// Selects the artifact build minimum timeout seconds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArtifactBuildMinimumTimeoutSeconds,
    /// <summary>
    /// Selects the artifact build maximum timeout seconds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArtifactBuildMaximumTimeoutSeconds,
    /// <summary>
    /// Selects the code generation maximum payload characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CodeGenerationMaximumPayloadCharacters,
    /// <summary>
    /// Selects the code generation maximum file count option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CodeGenerationMaximumFileCount,
    /// <summary>
    /// Selects the code generation maximum review take option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CodeGenerationMaximumReviewTake,
    /// <summary>
    /// Selects the component activity capacity option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ComponentActivityCapacity,
    /// <summary>
    /// Selects the component activity maximum summary characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ComponentActivityMaximumSummaryCharacters,
    /// <summary>
    /// Selects the runtime capability refresh warning option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RuntimeCapabilityRefreshWarning,
    /// <summary>
    /// Selects the council code generation maximum embedded plan characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilCodeGenerationMaximumEmbeddedPlanCharacters,
    /// <summary>
    /// Selects the council team seed version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilTeamSeedVersion,
    /// <summary>
    /// Selects the debug artifact maximum inspection bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DebugArtifactMaximumInspectionBytes,
    /// <summary>
    /// Selects the deferred DevExpress AI maximum result characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DeferredDxAiMaximumResultCharacters,
    /// <summary>
    /// Selects the DevExpress AI function catalog data type option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DxAiFunctionCatalogDataType,
    /// <summary>
    /// Selects the formatting collapsed thinking start option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FormattingCollapsedThinkingStart,
    /// <summary>
    /// Selects the formatting live thinking start option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FormattingLiveThinkingStart,
    /// <summary>
    /// Selects the formatting think start tag option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FormattingThinkStartTag,
    /// <summary>
    /// Selects the formatting think end tag option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FormattingThinkEndTag,
    /// <summary>
    /// Selects the formatting tag lookbehind length option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FormattingTagLookbehindLength,
    /// <summary>
    /// Selects the formatting missing final answer notice option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FormattingMissingFinalAnswerNotice,
    /// <summary>
    /// Selects the hardware GPU inventory script option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HardwareGpuInventoryScript,
    /// <summary>
    /// Selects the human collaboration maximum text length option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HumanCollaborationMaximumTextLength,
    /// <summary>
    /// Selects the native command minimum timeout seconds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NativeCommandMinimumTimeoutSeconds,
    /// <summary>
    /// Selects the native command maximum timeout seconds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NativeCommandMaximumTimeoutSeconds,
    /// <summary>
    /// Selects the navigation toggle sidebar name option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NavigationToggleSidebarName,
    /// <summary>
    /// Selects the Ollama maximum automatic tool rounds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OllamaMaximumAutomaticToolRounds,
    /// <summary>
    /// Selects the Ollama maximum tool result characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OllamaMaximumToolResultCharacters,
    /// <summary>
    /// Selects the local vision maximum image bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalVisionMaximumImageBytes,
    /// <summary>
    /// Selects the one wire security schema version option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OneWireSecuritySchemaVersion,
    /// <summary>
    /// Selects the one wire totp period seconds option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OneWireTotpPeriodSeconds,
    /// <summary>
    /// Selects the one wire totp alphabet option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OneWireTotpAlphabet,
    /// <summary>
    /// Selects the sqlite table editor maximum rows option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SqliteTableEditorMaximumRows,
    /// <summary>
    /// Selects the project maintenance maximum compiler candidates option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectMaintenanceMaximumCompilerCandidates,
    /// <summary>
    /// Selects the project maintenance maximum captured characters option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectMaintenanceMaximumCapturedCharacters,
    /// <summary>
    /// Selects the project organic artifact kind option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectOrganicArtifactKind,
    /// <summary>
    /// Selects the project organic artifact name option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectOrganicArtifactName,
    /// <summary>
    /// Selects the safe text document maximum bytes option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SafeTextDocumentMaximumBytes,
    /// <summary>
    /// Selects the theme default name option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ThemeDefaultName,
    /// <summary>
    /// Selects the theme contract path option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ThemeContractPath,
    /// <summary>
    /// Selects the bootstrap dark mode postfix option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BootstrapDarkModePostfix,
    /// <summary>
    /// Selects the project maintenance toast name option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectMaintenanceToastName,
    /// <summary>
    /// Selects the project toast name option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectToastName,
    /// <summary>
    /// Selects the database migration organic skill table repair SQL option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DatabaseMigrationOrganicSkillTableRepairSql,
    /// <summary>
    /// Selects the database migration organic skill index repair SQL option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DatabaseMigrationOrganicSkillIndexRepairSql,
    /// <summary>
    /// Selects the database migration council team table repair SQL option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DatabaseMigrationCouncilTeamTableRepairSql,
    /// <summary>
    /// Selects the database migration council team index repair SQL option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DatabaseMigrationCouncilTeamIndexRepairSql,
    /// <summary>
    /// Selects the sqlite GUID expression option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SqliteGuidExpression,
    /// <summary>
    /// Selects the learn base presets JSON option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LearnBasePresetsJson,
    /// <summary>
    /// Selects the learn base scan profiles JSON option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LearnBaseScanProfilesJson,
    /// <summary>
    /// Selects the test lab routes JSON option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TestLabRoutesJson,
    /// <summary>
    /// Selects the vocabulary JSON option for <see cref="LocalGptRuntimeValue"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    VocabularyJson,
}

/// <summary>
/// Defines the supported LocalGPT runtime collection values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum LocalGptRuntimeCollection
{
    /// <summary>
    /// Selects the allowed native executables option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AllowedNativeExecutables,
    /// <summary>
    /// Selects the debug extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DebugExtensions,
    /// <summary>
    /// Selects the text extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TextExtensions,
    /// <summary>
    /// Selects the binary diagnostic extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BinaryDiagnosticExtensions,
    /// <summary>
    /// Selects the excluded directory names option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ExcludedDirectoryNames,
    /// <summary>
    /// Selects the binary extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BinaryExtensions,
    /// <summary>
    /// Selects the source extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SourceExtensions,
    /// <summary>
    /// Selects the learn base known extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LearnBaseKnownExtensions,
    /// <summary>
    /// Selects the artifact text extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArtifactTextExtensions,
    /// <summary>
    /// Selects the knowledge files option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    KnowledgeFiles,
    /// <summary>
    /// Selects the allowed upload extensions option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AllowedUploadExtensions,
    /// <summary>
    /// Selects the allowed upload MIME types option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AllowedUploadMimeTypes,
    /// <summary>
    /// Selects the architecture language toolchain options option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArchitectureLanguageToolchainOptions,
    /// <summary>
    /// Selects the architecture UI stack options option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArchitectureUiStackOptions,
    /// <summary>
    /// Selects the architecture solution shape options option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArchitectureSolutionShapeOptions,
    /// <summary>
    /// Selects the architecture render mode options option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArchitectureRenderModeOptions,
    /// <summary>
    /// Selects the architecture reference look options option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ArchitectureReferenceLookOptions,
    /// <summary>
    /// Selects the project requirement target kinds option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectRequirementTargetKinds,
    /// <summary>
    /// Selects the project artifact kinds option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ProjectArtifactKinds,
    /// <summary>
    /// Selects the chat harmony model hints option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatHarmonyModelHints,
    /// <summary>
    /// Selects the chat deep seek model hints option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatDeepSeekModelHints,
    /// <summary>
    /// Selects the chat deep seek control tokens option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatDeepSeekControlTokens,
    /// <summary>
    /// Selects the chat gemma model hints option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatGemmaModelHints,
    /// <summary>
    /// Selects the chat gemma control tokens option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatGemmaControlTokens,
    /// <summary>
    /// Selects the chat apple model hints option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatAppleModelHints,
    /// <summary>
    /// Selects the chat apple control tokens option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatAppleControlTokens,
    /// <summary>
    /// Selects the chat think tags model hints option for <see cref="LocalGptRuntimeCollection"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatThinkTagsModelHints,
}

/// <summary>
/// Defines the supported LocalGPT runtime pattern values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum LocalGptRuntimePattern
{
    /// <summary>
    /// Selects the name cleaner option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NameCleaner,
    /// <summary>
    /// Selects the mod identifier cleaner option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ModIdCleaner,
    /// <summary>
    /// Selects the package part cleaner option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PackagePartCleaner,
    /// <summary>
    /// Selects the missing feature pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MissingFeaturePattern,
    /// <summary>
    /// Selects the capability gap block pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CapabilityGapBlockPattern,
    /// <summary>
    /// Selects the truncated tail pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TruncatedTailPattern,
    /// <summary>
    /// Selects the thinking block pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ThinkingBlockPattern,
    /// <summary>
    /// Selects the council prompt fence pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilPromptFencePattern,
    /// <summary>
    /// Selects the council request block pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilRequestBlockPattern,
    /// <summary>
    /// Selects the target framework pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TargetFrameworkPattern,
    /// <summary>
    /// Selects the package reference pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PackageReferencePattern,
    /// <summary>
    /// Selects the sensitive name pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SensitiveNamePattern,
    /// <summary>
    /// Selects the stream status pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StreamStatusPattern,
    /// <summary>
    /// Selects the word pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WordPattern,
    /// <summary>
    /// Selects the development request pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DevelopmentRequestPattern,
    /// <summary>
    /// Selects the explicit artifact intent pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ExplicitArtifactIntentPattern,
    /// <summary>
    /// Selects the advice only prompt pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AdviceOnlyPromptPattern,
    /// <summary>
    /// Selects the explicit artifact creation command pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ExplicitArtifactCreationCommandPattern,
    /// <summary>
    /// Selects the concrete minecraft artifact pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ConcreteMinecraftArtifactPattern,
    /// <summary>
    /// Selects the concrete dot net artifact pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ConcreteDotNetArtifactPattern,
    /// <summary>
    /// Selects the AI host setup pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AiHostSetupPattern,
    /// <summary>
    /// Selects the implementation decision pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ImplementationDecisionPattern,
    /// <summary>
    /// Selects the implementation choice pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ImplementationChoicePattern,
    /// <summary>
    /// Selects the blocking artifact decision pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BlockingArtifactDecisionPattern,
    /// <summary>
    /// Selects the safe sandbox consent pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SafeSandboxConsentPattern,
    /// <summary>
    /// Selects the explicit do not generate until user decision pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ExplicitDoNotGenerateUntilUserDecisionPattern,
    /// <summary>
    /// Selects the developer execution intent pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DeveloperExecutionIntentPattern,
    /// <summary>
    /// Selects the dev express import pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DevExpressImportPattern,
    /// <summary>
    /// Selects the dev express registration pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DevExpressRegistrationPattern,
    /// <summary>
    /// Selects the dev express document pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DevExpressDocumentPattern,
    /// <summary>
    /// Selects the export format pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ExportFormatPattern,
    /// <summary>
    /// Selects the blazor frontend pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BlazorFrontendPattern,
    /// <summary>
    /// Selects the dot net pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DotNetPattern,
    /// <summary>
    /// Selects the minecraft pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinecraftPattern,
    /// <summary>
    /// Selects the datapack pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DatapackPattern,
    /// <summary>
    /// Selects the minecraft skeleton matrix pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinecraftSkeletonMatrixPattern,
    /// <summary>
    /// Selects the minecraft version pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MinecraftVersionPattern,
    /// <summary>
    /// Selects the leading slash command pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LeadingSlashCommandPattern,
    /// <summary>
    /// Selects the root storage remove pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RootStorageRemovePattern,
    /// <summary>
    /// Selects the malformed storage target pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MalformedStorageTargetPattern,
    /// <summary>
    /// Selects the frontend pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FrontendPattern,
    /// <summary>
    /// Selects the whole solution pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WholeSolutionPattern,
    /// <summary>
    /// Selects the AI host experiment pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AiHostExperimentPattern,
    /// <summary>
    /// Selects the local GPT replacement pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalGptReplacementPattern,
    /// <summary>
    /// Selects the tacos portal pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TacosPortalPattern,
    /// <summary>
    /// Selects the bot backend pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    BotBackendPattern,
    /// <summary>
    /// Selects the logging pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LoggingPattern,
    /// <summary>
    /// Selects the whitespace pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WhitespacePattern,
    /// <summary>
    /// Selects the helpful source line pattern option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HelpfulSourceLinePattern,
    /// <summary>
    /// Selects the local GPT knowledge block option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalGptKnowledgeBlock,
    /// <summary>
    /// Selects the local GPT self assessment block option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalGptSelfAssessmentBlock,
    /// <summary>
    /// Selects the solution project reference option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SolutionProjectReference,
    /// <summary>
    /// Selects the c sharp namespace option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CSharpNamespace,
    /// <summary>
    /// Selects the c sharp service registration option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CSharpServiceRegistration,
    /// <summary>
    /// Selects the asp net controller route option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AspNetControllerRoute,
    /// <summary>
    /// Selects the dot net solution project option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DotNetSolutionProject,
    /// <summary>
    /// Selects the installer port contract option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    InstallerPortContract,
    /// <summary>
    /// Selects the one wire capability key option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OneWireCapabilityKey,
    /// <summary>
    /// Selects the file path with extension option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FilePathWithExtension,
    /// <summary>
    /// Selects the power shell inline command option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PowerShellInlineCommand,
    /// <summary>
    /// Selects the power shell file option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PowerShellFile,
    /// <summary>
    /// Selects the sensitive argument option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SensitiveArgument,
    /// <summary>
    /// Selects the download URL option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DownloadUrl,
    /// <summary>
    /// Selects the model capability self assessment option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ModelCapabilitySelfAssessment,
    /// <summary>
    /// Selects the council tagged plan option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilTaggedPlan,
    /// <summary>
    /// Selects the council fenced plan option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilFencedPlan,
    /// <summary>
    /// Selects the chat harmony thinking option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatHarmonyThinking,
    /// <summary>
    /// Selects the chat harmony final option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatHarmonyFinal,
    /// <summary>
    /// Selects the chat harmony marker option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ChatHarmonyMarker,
    /// <summary>
    /// Selects the render thinking details start option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RenderThinkingDetailsStart,
    /// <summary>
    /// Selects the council completion marker option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilCompletionMarker,
    /// <summary>
    /// Selects the list after HTML option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ListAfterHtml,
    /// <summary>
    /// Selects the controlled details start option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ControlledDetailsStart,
    /// <summary>
    /// Selects the details end option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DetailsEnd,
    /// <summary>
    /// Selects the stable panel start option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StablePanelStart,
    /// <summary>
    /// Selects the stream identifier attribute option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StreamIdAttribute,
    /// <summary>
    /// Selects the pre start option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PreStart,
    /// <summary>
    /// Selects the pre end option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PreEnd,
    /// <summary>
    /// Selects the toolchain knowledge block option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ToolchainKnowledgeBlock,
    /// <summary>
    /// Selects the toolchain version token option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ToolchainVersionToken,
    /// <summary>
    /// Selects the toolchain environment token option for <see cref="LocalGptRuntimePattern"/>, giving callers a named value for that supported mode or state.
    /// </summary>
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
