using DevExpress.Blazor;
using System.Collections.Frozen;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates LocalGPT catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the LocalGPT catalog workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public sealed class LocalGptCatalogService(
        ILocalGptRuntimePolicyDataService runtimePolicy,
        ILogger<LocalGptCatalogService> logger)
    {
        /// <summary>
        /// Stores the LocalGPT runtime policy data service dependency used by <see cref="LocalGptCatalogService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptRuntimePolicyDataService _runtimePolicy =
            runtimePolicy ?? throw new ArgumentNullException(nameof(runtimePolicy));
        /// <summary>
        /// Stores the logger used by <see cref="LocalGptCatalogService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<LocalGptCatalogService> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Gets the default gradle version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default gradle version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultGradleVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultGradleVersion);
        /// <summary>
        /// Gets the utf8 no bom value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The utf8 no bom value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Encoding Utf8NoBom { get; } =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        /// <summary>
        /// Gets the name cleaner value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name cleaner value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex NameCleaner => _runtimePolicy.GetPattern(LocalGptRuntimePattern.NameCleaner);
        /// <summary>
        /// Gets the mod identifier cleaner value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The mod identifier cleaner value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ModIdCleaner => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ModIdCleaner);
        /// <summary>
        /// Gets the package part cleaner value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The package part cleaner value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex PackagePartCleaner => _runtimePolicy.GetPattern(LocalGptRuntimePattern.PackagePartCleaner);

        /// <summary>
        /// Gets the default minecraft version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default minecraft version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultMinecraftVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultMinecraftVersion);

        /// <summary>
        /// Gets the default java version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default java version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultJavaVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultJavaVersion);
        /// <summary>
        /// Gets the fabric loader version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The fabric loader version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string FabricLoaderVersion => _runtimePolicy.GetString(LocalGptRuntimeValue.FabricLoaderVersion);




        /// <summary>
        /// Gets the max DevExpress AI chat prompt characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max DevExpress AI chat prompt characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxDxAiChatPromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxDxAiChatPromptCharacters);
        /// <summary>
        /// Gets the max visible prompt characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max visible prompt characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxVisiblePromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxVisiblePromptCharacters);
        /// <summary>
        /// Gets the missing feature pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The missing feature pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex MissingFeaturePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MissingFeaturePattern);
        /// <summary>
        /// Gets the capability gap block pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The capability gap block pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex CapabilityGapBlockPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.CapabilityGapBlockPattern);
        /// <summary>
        /// Gets the truncated tail pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The truncated tail pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex TruncatedTailPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.TruncatedTailPattern);
        /// <summary>
        /// Gets the thinking block pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The thinking block pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ThinkingBlockPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ThinkingBlockPattern);
        /// <summary>
        /// Gets the council prompt fence pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council prompt fence pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex CouncilPromptFencePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.CouncilPromptFencePattern);
        /// <summary>
        /// Gets the council request block pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council request block pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex CouncilRequestBlockPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.CouncilRequestBlockPattern);

        /// <summary>
        /// Gets the debug extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The debug extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> DebugExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.DebugExtensions);
        /// <summary>
        /// Gets the text extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The text extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> TextExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.TextExtensions);
        /// <summary>
        /// Gets the learn base known extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base known extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> LearnBaseKnownExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.LearnBaseKnownExtensions);

        /// <summary>
        /// Gets the binary diagnostic extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The binary diagnostic extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> BinaryDiagnosticExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.BinaryDiagnosticExtensions);
        /// <summary>
        /// Gets the target framework pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The target framework pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex TargetFrameworkPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.TargetFrameworkPattern);
        /// <summary>
        /// Gets the package reference pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The package reference pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex PackageReferencePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.PackageReferencePattern);
        /// <summary>
        /// Gets the sensitive name pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The sensitive name pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex SensitiveNamePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.SensitiveNamePattern);
        /// <summary>
        /// Gets the excluded directory names used by this LocalGPT catalog instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The excluded directory names value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> ExcludedDirectoryNames => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ExcludedDirectoryNames);

        /// <summary>
        /// Gets the binary extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The binary extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> BinaryExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.BinaryExtensions);

        /// <summary>
        /// Gets the source extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The source extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> SourceExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.SourceExtensions);
        /// <summary>
        /// Gets the default Ollama URI that identifies the network or application endpoint associated with this LocalGPT catalog state.
        /// </summary>
        /// <value>The default Ollama URI value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultOllamaUri => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultOllamaUri);
        /// <summary>
        /// Gets the max participants value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max participants value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxParticipants => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxParticipants);
        /// <summary>
        /// Gets the default max parallel models value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default max parallel models value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultMaxParallelModels => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultMaxParallelModels);
        /// <summary>
        /// Gets the default heavy model GPU layers value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default heavy model GPU layers value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultHeavyModelGpuLayers => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultHeavyModelGpuLayers);
        /// <summary>
        /// Gets the min context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The min context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MinContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinContextTokens);
        /// <summary>
        /// Gets the default context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultContextTokens);
        /// <summary>
        /// Gets the max context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxContextTokens);
        /// <summary>
        /// Gets the min output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The min output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MinOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinOutputTokens);
        /// <summary>
        /// Gets the max output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxOutputTokens);
        /// <summary>
        /// Gets the stream status pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The stream status pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex StreamStatusPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.StreamStatusPattern);
        /// <summary>
        /// Gets the word pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The word pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex WordPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.WordPattern);
        /// <summary>
        /// Gets the development request pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The development request pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevelopmentRequestPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevelopmentRequestPattern);
        /// <summary>
        /// Gets the explicit artifact intent pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The explicit artifact intent pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExplicitArtifactIntentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitArtifactIntentPattern);
        /// <summary>
        /// Gets the advice only prompt pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The advice only prompt pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex AdviceOnlyPromptPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.AdviceOnlyPromptPattern);
        /// <summary>
        /// Gets the explicit artifact creation command pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The explicit artifact creation command pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExplicitArtifactCreationCommandPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitArtifactCreationCommandPattern);
        /// <summary>
        /// Gets the concrete minecraft artifact pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The concrete minecraft artifact pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ConcreteMinecraftArtifactPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ConcreteMinecraftArtifactPattern);
        /// <summary>
        /// Gets the concrete dot net artifact pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The concrete dot net artifact pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ConcreteDotNetArtifactPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ConcreteDotNetArtifactPattern);
        /// <summary>
        /// Gets the AI host setup pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The AI host setup pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex AiHostSetupPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.AiHostSetupPattern);
        /// <summary>
        /// Gets the implementation decision pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The implementation decision pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ImplementationDecisionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ImplementationDecisionPattern);
        /// <summary>
        /// Gets the implementation choice pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The implementation choice pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ImplementationChoicePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ImplementationChoicePattern);
        /// <summary>
        /// Gets the blocking artifact decision pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The blocking artifact decision pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex BlockingArtifactDecisionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.BlockingArtifactDecisionPattern);
        /// <summary>
        /// Gets the safe sandbox consent pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The safe sandbox consent pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex SafeSandboxConsentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.SafeSandboxConsentPattern);
        /// <summary>
        /// Gets or sets explicit do not generate until user decision pattern.
        /// </summary>
        /// <value>The explicit do not generate until user decision pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExplicitDoNotGenerateUntilUserDecisionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExplicitDoNotGenerateUntilUserDecisionPattern);
        /// <summary>
        /// Gets the developer execution intent pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The developer execution intent pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DeveloperExecutionIntentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DeveloperExecutionIntentPattern);

 


        /// <summary>
        /// Gets the DevExpress import pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The DevExpress import pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevExpressImportPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressImportPattern);
        /// <summary>
        /// Gets the DevExpress registration pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The DevExpress registration pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevExpressRegistrationPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressRegistrationPattern);
        /// <summary>
        /// Gets the artifact text extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact text extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> ArtifactTextExtensions => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArtifactTextExtensions);

        /// <summary>
        /// Gets the max artifact text file bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max artifact text file bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxArtifactTextFileBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxArtifactTextFileBytes);
     


        /// <summary>
        /// Gets the max files value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max files value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxFiles => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxFiles);
        /// <summary>
        /// Gets the max single file bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max single file bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxSingleFileBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxSingleFileBytes);
        /// <summary>
        /// Gets the max total file bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max total file bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxTotalFileBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxTotalFileBytes);
        /// <summary>
        /// Gets the max ZIP entries value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max ZIP entries value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxZipEntries => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxZipEntries);
        /// <summary>
        /// Gets the max ZIP entry bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max ZIP entry bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxZipEntryBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxZipEntryBytes);
        /// <summary>
        /// Gets the max extracted bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max extracted bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxExtractedBytes => _runtimePolicy.GetLong(LocalGptRuntimeValue.MaxExtractedBytes);
        /// <summary>
        /// Gets the max context characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max context characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxContextCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxContextCharacters);
        /// <summary>
        /// Gets the max excerpt characters per file value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max excerpt characters per file value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxExcerptCharactersPerFile => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxExcerptCharactersPerFile);
        /// <summary>
        /// Gets the max binary string characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max binary string characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxBinaryStringCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxBinaryStringCharacters);
        /// <summary>
        /// Gets the knowledge files value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The knowledge files value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] KnowledgeFiles => _runtimePolicy.GetCollection(LocalGptRuntimeCollection.KnowledgeFiles).Select(value => value.Replace('/', Path.DirectorySeparatorChar)).ToArray();
        /// <summary>
        /// Gets the omission value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The omission value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string omission => _runtimePolicy.GetString(LocalGptRuntimeValue.ContextOmission);

        /// <summary>
        /// Gets the short omission value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The short omission value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string shortOmission => _runtimePolicy.GetString(LocalGptRuntimeValue.ShortContextOmission);
        /// <summary>
        /// Gets the download URL pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The download URL pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DownloadUrlPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DownloadUrl);
        /// <summary>
        /// Gets the learn base file policy summary value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base file policy summary value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LearnBaseFilePolicySummary => _runtimePolicy.GetString(LocalGptRuntimeValue.LearnBaseFilePolicySummary);
        /// <summary>
        /// Gets the learn base duplicate policy summary value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base duplicate policy summary value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LearnBaseDuplicatePolicySummary => _runtimePolicy.GetString(LocalGptRuntimeValue.LearnBaseDuplicatePolicySummary);
        /// <summary>
        /// Gets the learn base preset list value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base preset list value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LearnBasePresetList => string.Join(", ", LearnBasePresets.Select(preset => preset.Label));

        /// <summary>
        /// Gets the learn base presets collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The learn base presets value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public IReadOnlyList<LearnBasePreset> LearnBasePresets => _runtimePolicy.GetJson<LearnBasePreset[]>(LocalGptRuntimeValue.LearnBasePresetsJson);
        /// <summary>
        /// Gets the learn base scan profiles collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The learn base scan profiles value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public IReadOnlyList<LearnBaseScanProfile> LearnBaseScanProfiles => _runtimePolicy.GetJson<LearnBaseScanProfile[]>(LocalGptRuntimeValue.LearnBaseScanProfilesJson);

        /// <summary>
        /// Gets the routes collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The routes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public List<TestLabRoute> Routes => [.. _runtimePolicy.GetJson<TestLabRoute[]>(LocalGptRuntimeValue.TestLabRoutesJson)];
        /// <summary>
        /// Retrieves suggestion as part of the LocalGPT catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <returns>Prompt suggestions with stable keys and optional direct-Council ownership.</returns>
        public List<PromptSuggestion> GetSuggestion()
        {
    try
    {
                _logger.LogTrace("Creating the LocalGPT prompt suggestion catalog.");
                return
                [
                    new("Recall memory", "Use saved chats and former thoughts", "Review your saved LocalGPT memory and former model thoughts, then summarize what you remember about this project and continue from that context."),
                    new("Council starter: project work", "Start the general Organic Project Team", "Start a fresh Organic Project Team Council run. Ask me for the exact project goal, current state, repository or workspace, compiler evidence and approval boundaries, then execute the maintained preparation, architecture, implementation, verification and consensus workflow.", "general-project-council-start", ["general-project"], true),
                    new("Council starter: benchmark models", "Compare installed Ollama models", "Start a fresh adaptive model benchmark Council run. Discover the currently installed Ollama models, ask me which models and workload categories to include, then run bounded comparable tasks. Separate measured speed, deterministic build or test results, code-curator ratings and user judgement. Do not silently overwrite approved presets.", "benchmark-council-start", ["adaptive-model-benchmark"], true),
                    new("Council starter: GameDirector", "Create a governed game session", "Start a fresh GameDirector Runtime Council run. Ask me for the game concept, player objective, creature and reactive-object families, map rules and preferred low-B controller models. Keep the GameDirector authoritative: every player, creature and map-object move is only a proposal until validated and applied by the director.", "game-director-council-start", ["game-director-runtime", "ascii-doom-council-adventure", "green-dragon-runtime-story", "kernel-creature-tournament"], true),
                    new("Council starter: modern C# host", "Build clean hosted .NET architecture", "Start a fresh Modern C# Host Development Team Council run. Ask for the workspace, target runtime, current solution and acceptance criteria. Follow the LocalGPT PowerShell build order: preflight and regex evidence, hosted architecture, bounded implementation, policy checks, build and tests, independent code-curator review, then release and changelog synthesis.", "csharp-host-council-start", ["csharp-modern-host-development"], true),
                    new("Council starter: PowerShell build", "Improve scripts and build policy", "Start a fresh PowerShell Build-System Development Team Council run. Inspect the requested scripts, repository policies, strict-mode behavior, idempotency, logging and exit-code contracts. Produce a bounded patch, execute available static checks, and finish with curator review and reproducible verification commands.", "powershell-build-council-start", ["powershell-build-development"], true),
                    new("Council starter: Java host", "Build Maven or Gradle services", "Start a fresh Java Hosted Application Development Team Council run. Ask for Java version, Maven or Gradle, framework, module structure and deployment target. Plan a modern hosted application, implement within the workspace policy, verify compilation and tests, and perform independent architecture and security review.", "java-hosted-council-start", ["java-hosted-development"], true),
                    new("Council starter: Minecraft", "Build a mod, plugin, datapack or add-on", "Start a fresh Minecraft Development Team Council run. First ask whether the target is Fabric, NeoForge, Paper, vanilla datapack or Bedrock add-on and which game version applies. Then assign Java, data-pack, asset, command and verification roles and produce a buildable, testable project plan without inventing unavailable tools.", "minecraft-development-council-start", ["minecraft-development"], true),
                    new("Council starter: ESP32 / Arduino", "Plan pins, wiring and firmware", "Start a fresh ESP32 / Arduino Wiring Council run. Ask for the exact board, sensors, voltage, pin layout and return transport. Produce a reviewed GPIO map, electrical warnings, transport-neutral telemetry contract, small firmware plan and learning-round checklist before any compile or flash action.", "embedded-wiring-council-start", ["embedded-firmware-wiring"], true),
                    new("Minecraft target choice", "Pick Fabric, NeoForge, Paper, or datapack", "Act as a LocalGPT AI Council member. Compare Fabric mod, NeoForge mod, Paper plugin, vanilla datapack, and future Bedrock add-on for my request. Recommend one target, explain setup, and create a short poll if a decision or missing tool blocks progress.", "minecraft-target-choice", ["minecraft-development"], false),
                    new("Minecraft mod plan", "Plan a buildable Java mod or plugin", "Act as a senior Minecraft Java engineer. Create a buildable Fabric, NeoForge, or Paper plan with exact classes, registry or command steps, assets/data files, Gradle commands, and risks. If LocalGPT is missing a needed feature, include a 'Missing feature report' section.", "minecraft-mod-plan", ["minecraft-development"], false),
                    new("Minecraft datapack", "Generate vanilla datapack files", "Generate a vanilla Minecraft Java datapack. Include pack.mcmeta, load/tick function tags, namespace functions, scoreboard/storage design, validation steps, install commands, and performance notes. If AI Council downloadable artifacts are enabled, create a download-ready datapack zip.", "minecraft-datapack", ["minecraft-development"], false),
                    new("Datapack debug", "Find why /function cannot see files", "Debug a Minecraft Java datapack whose function is not visible in /function. Check zip root layout, pack.mcmeta, pack_format, singular/plural function folders for the target version, load/tick tags, namespace/path casing, .mcfunction.txt mistakes, storage syntax, and provide exact file tree fixes.", "minecraft-datapack-debug", ["minecraft-development"], false),
                    new("Living Cities datapack", "Generate a phased Living Cities datapack", "Use the Living Cities 0.1 technical plan as the target. Produce a buildable, download-ready datapack zip plus optional Java follow-up steps, file paths, commands, scoreboard/storage design, and performance notes for 1000+ citizens.", "living-cities-datapack", ["minecraft-development"], false),
                    new("Missing features", "Write gaps to report file", "Review LocalGPT as a Minecraft mod builder. List missing features, blocked workflows, and required backend/frontend capabilities under a 'Missing feature report' heading.", "minecraft-missing-features", ["minecraft-development"], false),
                    new("Write an email", "Make your text look and sound professional", "Format text as a formal email to a client:"),
                    new("Brainstorm ideas", "Get creative input for your tasks", "Help me brainstorm ideas for:"),
                    new("Fix my writing", "Avoid spelling, grammar, and style errors", "Proofread the following text:"),
                    new("Lost sci-fi sequel", "Imagine the long-awaited continuation of an original science-fiction series", "Hi Team, invent an original science-fiction sequel for a fictional series that has been absent for decades. Explain what you need to learn, how the story should evolve, which engine could support it, and how you would build a convincing prototype without copying an existing franchise.")
                ];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(LocalGptCatalogService)}.{nameof(GetSuggestion)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(LocalGptCatalogService)}.{nameof(GetSuggestion)} failed.");
        throw;
    }
}
        /// <summary>
        /// Gets the living cities prompt value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The living cities prompt value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LivingCitiesPrompt =>
         string.Join(Environment.NewLine, new[]
{
        "Living Cities 0.1 should turn Minecraft villages into persistent cities with population, food, security, personalities, chronicle, quests, and town hall administration.",
        "",
        "First build target:",
        "- generate a vanilla Java Edition datapack first",
        "- default to the newest installed Java Edition generation line; LocalGPT currently maps Minecraft 26.1 to datapack pack_format 101.1 and Java 25",
        "- keep the first generated datapack small, buildable, and installable",
        "- include pack.mcmeta, minecraft load/tick function tags, namespace functions, and build-local.ps1 validation",
        "- include a town hall/admin book UI through trigger commands",
        "- keep the critical path documented: datapack/data structure, scoreboards or saved data, city founding, citizen registration, population management, minimal town hall",
        "- avoid world-wide scans",
        "- plan for 1000+ citizens by simulating city aggregates before individuals"
    });
        /// <summary>
        /// Gets the council knowledge entry new value that forms part of the local GPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council knowledge entry new value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public CouncilKnowledgeEntry CouncilKnowledgeEntryNew => new CouncilKnowledgeEntry()
            {
                Topic = "New LocalGPT knowledge",
                Scope = "AI Council",
                Source = "Manual database editor",
                HelpfulSources = "None yet.",
                Tags = "manual; council",
                Confidence = 60,
                VerificationStatus = "UserVerified",
                ReviewStatus = "Current",
                LastVerifiedAtUtc = DateTime.UtcNow,
                IsUserApproved = true
            };
    /// <summary>
    /// Gets the generate solution routes razor value that forms part of the local GPT catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The generate solution routes razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
    public string GenerateSolutionRoutesRazor =>
           """
            <Router AppAssembly="@typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="@routeData" />
                    <FocusOnNavigate RouteData="@routeData" Selector="h1" />
                </Found>
                <NotFound>
                    <PageTitle>Not Found</PageTitle>
                    <p role="alert">This generated LocalGPT route was not found.</p>
                </NotFound>
            </Router>
            """;
        /// <summary>
        /// Gets the generate solution app razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate solution app razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateSolutionAppRazor =>
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <base href="/" />
                <link href="_content/DevExpress.Blazor.Themes/blazing-berry.bs5.css" rel="stylesheet" />
                <link href="app.css" rel="stylesheet" />
                <HeadOutlet @rendermode="InteractiveServer" />
            </head>
            <body>
                <Routes @rendermode="InteractiveServer" />
                <script src="_framework/blazor.web.js"></script>
            </body>
            </html>
            """;
        /// <summary>
        /// Gets the generate solution project file value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate solution project file value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateSolutionProjectFile =>
           """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="DevExpress.Blazor" Version="25.1.*" />
              </ItemGroup>
            </Project>
            """;
        /// <summary>
        /// Gets the generate source fidelity razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate source fidelity razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateSourceFidelityRazor =>
            """
            @page "/source-fidelity"
            @rendermode InteractiveServer
            @inject ISourceFidelityService FidelityService

            <PageTitle>Source Fidelity</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation />

                <section class="generated-header">
                    <div>
                        <h1>Source Fidelity</h1>
                        <p>Checks whether this generated solution represents the requested source architecture instead of only compiling.</p>
                    </div>
                </section>

                <DxGrid Data="@Rows"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        ShowFilterRow="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.SourceSignal)" Caption="Source Signal" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.GeneratedBoundary)" Caption="Generated Boundary" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Evidence)" Caption="Evidence" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Review rule" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Acceptance" ColSpanMd="12">
                            <DxMemo Text="@ReviewRule" Rows="4" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedSourceFidelityRequirement> Rows { get; set; } = [];
                string ReviewRule { get; } =
                    "A generated replacement is not accepted just because it builds. It must preserve the source application's recognizable workflows, service boundaries, persistence shape, navigation, diagnostics, and artifact/download behavior.";

                protected override void OnInitialized()
                {
                    Rows = FidelityService.GetRequirements();
                }
            }
            """;
        /// <summary>
        /// Gets the generate solution CSS value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate solution CSS value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateSolutionCss =>
            """
            :root {
                color-scheme: light;
                font-family: "Segoe UI", Arial, sans-serif;
            }

            body {
                margin: 0;
                background: #f7f8fa;
                color: #1f2937;
            }

            .generated-shell {
                max-width: 1180px;
                margin: 0 auto;
                padding: 32px;
            }

            .generated-nav {
                display: flex;
                align-items: center;
                gap: 16px;
                margin-bottom: 24px;
                padding-bottom: 14px;
                border-bottom: 1px solid #d9dee7;
            }

            .generated-nav a {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                color: #384252;
                text-decoration: none;
                font-weight: 600;
            }

            .generated-nav a:hover,
            .generated-nav a:focus-visible {
                color: #0b5cab;
            }

            .generated-nav .generated-brand {
                margin-right: auto;
                color: #172033;
                font-weight: 700;
            }

            .generated-nav-icon {
                width: 18px;
                height: 18px;
                flex: 0 0 18px;
            }

            .generated-nav-icon-solid {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-line,
            .generated-nav a:focus-visible .generated-nav-icon-line {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-solid,
            .generated-nav a:focus-visible .generated-nav-icon-solid {
                display: inline-block;
            }

            .generated-hero {
                display: grid;
                grid-template-columns: minmax(0, 1fr) auto;
                gap: 20px;
                align-items: end;
                padding: 28px 0 24px;
            }

            .generated-hero h1 {
                margin: 0;
                font-size: 34px;
                line-height: 1.1;
            }

            .generated-hero p {
                max-width: 760px;
                color: #536173;
            }

            .generated-kicker {
                margin: 0 0 8px;
                color: #0f766e;
                font-weight: 700;
                text-transform: uppercase;
                letter-spacing: 0;
            }

            .generated-actions {
                display: flex;
                gap: 10px;
                flex-wrap: wrap;
                justify-content: flex-end;
            }

            .generated-split {
                display: grid;
                grid-template-columns: minmax(0, 1fr) minmax(320px, 0.8fr);
                gap: 24px;
                align-items: start;
            }

            .generated-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 16px;
                margin-bottom: 20px;
            }

            .generated-header h1 {
                margin: 0;
                font-size: 28px;
            }

            .generated-header p,
            .generated-muted {
                margin: 6px 0 0;
                color: #5f6b7a;
            }

            .generated-grid,
            .generated-form {
                margin-top: 18px;
            }

            .generated-note {
                margin-top: 22px;
            }

            .generated-code {
                overflow: auto;
                padding: 16px;
                border: 1px solid #d9dee7;
                background: #ffffff;
                border-radius: 6px;
            }

            @media (max-width: 860px) {
                .generated-shell {
                    padding: 20px;
                }

                .generated-hero,
                .generated-split {
                    grid-template-columns: 1fr;
                }

                .generated-actions {
                    justify-content: flex-start;
                }
            }
            """;
        /// <summary>
        /// Gets the generate AI host settings razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host settings razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostSettingsRazor =>
            """
            @page "/settings"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Settings</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Settings</h1>
                        <p>Configuration is shown as safe generated defaults. Real persistence should be added through backend services and EF/SQLite after user approval.</p>
                    </div>
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generated Runtime Profile" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model Source" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.BaseUri" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Default Model" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.DefaultModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Keep Alive" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.KeepAlive" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Context Tokens" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.ContextTokens.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="GPU Layers" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.GpuLayers.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Native Runner Attached" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="NativeRunnerAttached" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Pull Planning Enabled" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="AllowPullPlanning" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Settings Summary" ColSpanMd="12">
                            <DxMemo Text="@HealthService.BuildSettingsSummary()" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                GeneratedAiHostSettings LabSettings { get; set; } = new();
                bool NativeRunnerAttached { get; set; }
                bool AllowPullPlanning { get; set; }

                protected override void OnInitialized()
                {
                    LabSettings = HealthService.GetSettings();
                    NativeRunnerAttached = LabSettings.NativeRunnerAttached;
                    AllowPullPlanning = LabSettings.AllowPullPlanning;
                }
            }
            """;
        /// <summary>
        /// Gets the generate AI host logs razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host logs razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostLogsRazor =>
            """
            @page "/logs"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Logs</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Logs</h1>
                        <p>Surface control-plane diagnostics and runtime-boundary notes where users can inspect them.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRuntimeLogRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Level" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Message" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Action" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        /// <summary>
        /// Gets the generate AI host runner plugins razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host runner plugins razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostRunnerPluginsRazor =>
            """
            @page "/runner-plugins"
            @rendermode InteractiveServer
            @inject IPluginCatalogService PluginCatalog
            @inject IInferenceRunner Runner
            @inject IHardwareBudgetService HardwareBudget
            @inject IChatTemplateService ChatTemplates

            <PageTitle>Runner Plugins</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Runner Plugins</h1>
                        <p>Show native-runner boundaries, optional catalog/provider adapters, Python.NET, PowerShell, and managed inference paths as explicit architecture contracts.</p>
                    </div>
                    <DxButton Text="Refresh capability"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="RefreshCapabilityAsync" />
                </section>

                <div class="generated-status-strip">
                    <article>
                        <strong>Native inference</strong>
                        <span>@(Capability?.NativeInferenceImplemented == true ? "Implemented" : "Capability gap")</span>
                    </article>
                    <article>
                        <strong>GPU target</strong>
                        <span>@Budget.TargetGpuLoadPercent% sustained</span>
                    </article>
                    <article>
                        <strong>Parallel models per AI host</strong>
                        <span>@Budget.MaxParallelModels</span>
                    </article>
                </div>

                <DxGrid Data="@PluginCatalog.GetPlugins()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Id)" Caption="Plugin Id" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.DisplayName)" Caption="Name" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Contract)" Caption="Contract" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Approved)" Caption="Approved" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Notes)" Caption="Notes" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Runner capability" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Runner kind" ColSpanMd="4">
                            <DxTextBox Text="@Runner.RunnerKind" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Supported formats" ColSpanMd="8">
                            <DxTextBox Text="@SupportedFormatsText" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Missing capability" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.MissingCapability ?? "Capability not loaded yet.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Next milestone" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.NextMilestone ?? "Click Refresh capability.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>

                <DxGrid Data="@ChatTemplates.GetTemplateRules()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Name)" Caption="Template" />
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Rule)" Caption="Rule" />
                    </Columns>
                </DxGrid>
            </main>

            @code {
                RunnerCapabilityReport? Capability { get; set; }
                HardwareBudgetSnapshot Budget { get; set; } = new(85, 20, 2048, 1, "Sequential by default.");
                string SupportedFormatsText => Capability is null ? string.Empty : string.Join(", ", Capability.SupportedFormats);

                protected override async Task OnInitializedAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }

                async Task RefreshCapabilityAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }
            }
            """;
        /// <summary>
        /// Gets the generate AI host hardware razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host hardware razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostHardwareRazor =>
           """
            @page "/hardware"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Hardware Budget</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Hardware Budget</h1>
                        <p>Represent GPU, CPU, context, queue, and throttling rules before heavy native runner jobs are allowed.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetHardwareBudgetRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Budget" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Policy" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Reason" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        /// <summary>
        /// Gets the generate AI host templates razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host templates razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostTemplatesRazor =>
            """
            @page "/templates"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Chat Templates</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Chat Templates</h1>
                        <p>Track model-specific prompt templates, thinking markers, and compatibility adapters as first-class control-plane data.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetTemplateRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Format" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Detector" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Boundary" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        /// <summary>
        /// Gets the generate AI host model downloads razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host model downloads razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostModelDownloadsRazor =>
            """
            @page "/model-downloads"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Model Downloads</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Model Downloads</h1>
                        <p>Plan model-file downloads with explicit target paths and user approval.</p>
                    </div>
                    <DxButton Text="Create pull plan"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="CreatePullPlan" />
                </section>

                <DxGrid Data="@HealthService.GetDownloadCandidates()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.Name)" Caption="Model" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SourceType)" Caption="Source" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SourceUrl)" Caption="Catalog URL" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.RecommendedFor)" Caption="Recommended For" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.DownloadRoute)" Caption="Route" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SafetyNote)" Caption="Safety Note" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Selected pull request" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model" ColSpanMd="6">
                            <DxTextBox Text="@SelectedModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Streaming" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="StreamProgress" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Generated plan" ColSpanMd="12">
                            <DxMemo Text="@PullPlanText" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                string SelectedModel { get; set; } = "gpt-oss:20b";
                bool StreamProgress { get; set; }
                string PullPlanText { get; set; } = "Click Create pull plan to preview a safe /api/pull response.";

                void CreatePullPlan()
                {
                    var plan = HealthService.CreatePullPlan(new GeneratedModelActionRequest
                    {
                        Model = SelectedModel,
                        Stream = StreamProgress
                    });
                    PullPlanText = $"{plan.Route} for {plan.Model}: {plan.Status}. {plan.Detail}";
                }
            }
            """;
        /// <summary>
        /// Gets the generate AI host running models razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host running models razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostRunningModelsRazor =>
         """
            @page "/running-models"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Running Models</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Running Models</h1>
                        <p>Mirror a local AI host's running-model view as a control-plane status page.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRunningModels()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Name)" Caption="Model" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.ModifiedAt)" Caption="Started" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Size)" Caption="Size" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Digest)" Caption="Digest" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        /// <summary>
        /// Gets the generate AI host chat razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host chat razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostChatRazor =>
             """
            @page "/chat"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Chat</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Chat</h1>
                        <p>Exercise the chat route shape through the generated local model-file runner boundary.</p>
                    </div>
                    <DxButton Text="Send runner chat"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="SendStubChat" />
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Chat request" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model" ColSpanMd="4">
                            <DxTextBox @bind-Text="Model" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Prompt" ColSpanMd="8">
                            <DxMemo @bind-Text="Prompt" Rows="3" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Transcript" ColSpanMd="12">
                            <DxMemo Text="@Transcript" Rows="8" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                string Model { get; set; } = "gpt-oss:20b";
                string Prompt { get; set; } = "Explain the generated AI host control-plane route boundaries.";
                string Transcript { get; set; } = "Click Send runner chat to preview a safe /api/chat response.";

                void SendStubChat()
                {
                    Transcript = HealthService.CreateChatTranscript(Model, Prompt);
                }
            }
            """;
        /// <summary>
        /// Gets the DevExpress document pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The DevExpress document pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevExpressDocumentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DevExpressDocumentPattern);
        /// <summary>
        /// Gets the export format pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The export format pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExportFormatPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.ExportFormatPattern);
        /// <summary>
        /// Gets the blazor frontend pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The blazor frontend pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex BlazorFrontendPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.BlazorFrontendPattern);
        /// <summary>
        /// Gets the dot net pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The dot net pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DotNetPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DotNetPattern);
        /// <summary>
        /// Gets the minecraft pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The minecraft pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex MinecraftPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftPattern);
        /// <summary>
        /// Gets the datapack pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The datapack pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DatapackPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.DatapackPattern);
        /// <summary>
        /// Gets the minecraft skeleton matrix pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The minecraft skeleton matrix pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex MinecraftSkeletonMatrixPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftSkeletonMatrixPattern);
        /// <summary>
        /// Gets the minecraft version pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The minecraft version pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex MinecraftVersionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MinecraftVersionPattern);
        /// <summary>
        /// Gets the leading slash command pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The leading slash command pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex LeadingSlashCommandPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.LeadingSlashCommandPattern);
        /// <summary>
        /// Gets the root storage remove pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The root storage remove pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex RootStorageRemovePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.RootStorageRemovePattern);
        /// <summary>
        /// Gets the malformed storage target pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The malformed storage target pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex MalformedStorageTargetPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.MalformedStorageTargetPattern);
        /// <summary>
        /// Gets the frontend pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The frontend pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex FrontendPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.FrontendPattern);
        /// <summary>
        /// Gets the whole solution pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The whole solution pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex WholeSolutionPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.WholeSolutionPattern);
        /// <summary>
        /// Gets the AI host experiment pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The AI host experiment pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex AiHostExperimentPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.AiHostExperimentPattern);
        /// <summary>
        /// Gets the LocalGPT replacement pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The LocalGPT replacement pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex LocalGptReplacementPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.LocalGptReplacementPattern);
        /// <summary>
        /// Gets the tacos portal pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The tacos portal pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex TacosPortalPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.TacosPortalPattern);
        /// <summary>
        /// Gets the bot backend pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The bot backend pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex BotBackendPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.BotBackendPattern);
        /// <summary>
        /// Gets the logging pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The logging pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex LoggingPattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.LoggingPattern);


        /// <summary>
        /// Gets the JSON options value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The JSON options value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Gets the whitespace pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The whitespace pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex WhitespacePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.WhitespacePattern);
        /// <summary>
        /// Gets the helpful source line pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The helpful source line pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex HelpfulSourceLinePattern => _runtimePolicy.GetPattern(LocalGptRuntimePattern.HelpfulSourceLinePattern);

        /// <summary>
        /// Gets the min council output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The min council output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MinCouncilOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinCouncilOutputTokens);
        /// <summary>
        /// Gets the default council output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default council output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultCouncilOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultCouncilOutputTokens);
        /// <summary>
        /// Gets the max council output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max council output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxCouncilOutputTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxCouncilOutputTokens);
        /// <summary>
        /// Gets the min council context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The min council context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MinCouncilContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MinCouncilContextTokens);
        /// <summary>
        /// Gets the default council context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default council context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultCouncilContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultCouncilContextTokens);
        /// <summary>
        /// Gets the max council context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max council context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxCouncilContextTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxCouncilContextTokens);
        /// <summary>
        /// Gets the council session name value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council session name value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string CouncilSessionName => _runtimePolicy.GetString(LocalGptRuntimeValue.CouncilSessionName);
        /// <summary>
        /// Gets the max upload files value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max upload files value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxUploadFiles => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxUploadFiles);
        /// <summary>
        /// Gets the max upload bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max upload bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxUploadBytes => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxUploadBytes);
        /// <summary>
        /// Gets the allowed upload extensions collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The allowed upload extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public List<string> AllowedUploadExtensions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.AllowedUploadExtensions)];
        /// <summary>
        /// Gets the allowed upload MIME types collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The allowed upload MIME types value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public List<string> AllowedUploadMimeTypes => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.AllowedUploadMimeTypes)];
        /// <summary>
        /// Gets the Ollama mode auto GPU value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The Ollama mode auto GPU value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string OllamaModeAutoGpu => _runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeAutoGpu);
        /// <summary>
        /// Gets the Ollama mode safe CPU value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The Ollama mode safe CPU value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string OllamaModeSafeCpu => _runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeSafeCpu);
        /// <summary>
        /// Gets the Ollama mode limited GPU value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The Ollama mode limited GPU value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string OllamaModeLimitedGpu => _runtimePolicy.GetString(LocalGptRuntimeValue.OllamaModeLimitedGpu);
 
        /// <summary>
        /// Gets the detected Ollama session prefix value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The detected Ollama session prefix value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DetectedOllamaSessionPrefix => _runtimePolicy.GetString(LocalGptRuntimeValue.DetectedOllamaSessionPrefix);
        /// <summary>
        /// Gets the default Ollama endpoint that identifies the network or application endpoint associated with this LocalGPT catalog state.
        /// </summary>
        /// <value>The default Ollama endpoint value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultOllamaEndpoint => _runtimePolicy.GetString(LocalGptRuntimeValue.DefaultOllamaEndpoint);
        /// <summary>
        /// Gets the database-provisioned language and toolchain choices for architecture guidance.
        /// </summary>
        /// <value>The architecture language toolchain options value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] ArchitectureLanguageToolchainOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureLanguageToolchainOptions)];
        /// <summary>
        /// Gets the architecture UI stack options value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The architecture UI stack options value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] ArchitectureUiStackOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureUiStackOptions)];
        /// <summary>
        /// Gets the architecture solution shape options value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The architecture solution shape options value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] ArchitectureSolutionShapeOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureSolutionShapeOptions)];
        /// <summary>
        /// Gets the architecture render mode options value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The architecture render mode options value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] ArchitectureRenderModeOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureRenderModeOptions)];
        /// <summary>
        /// Gets the architecture reference look options value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The architecture reference look options value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] ArchitectureReferenceLookOptions => [.. _runtimePolicy.GetCollection(LocalGptRuntimeCollection.ArchitectureReferenceLookOptions)];
        /// <summary>
        /// Gets the default max prompt characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default max prompt characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultMaxPromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.DefaultMaxPromptCharacters);
        /// <summary>
        /// Gets the max prompt characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max prompt characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxPromptCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxPromptCharacters);
        /// <summary>
        /// Gets the max bootstrap characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max bootstrap characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxBootstrapCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxBootstrapCharacters);
        /// <summary>
        /// Gets the max single conversation message characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max single conversation message characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxSingleConversationMessageCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.MaxSingleConversationMessageCharacters);

    }
}
