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
    public sealed partial class LocalGptCatalogService
    {
        /// <summary>
        /// Stores the LocalGPT runtime policy data service dependency used by <see cref="LocalGptCatalogService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptRuntimePolicyDataService _runtimePolicy;
        /// <summary>
        /// Stores the logger used by <see cref="LocalGptCatalogService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<LocalGptCatalogService> _logger;

        /// <summary>Initializes the LocalGPT runtime catalog.</summary>
        /// <param name="runtimePolicy">Runtime policy store that owns user-editable catalog values and patterns.</param>
        /// <param name="logger">Logger for bounded catalog diagnostics.</param>
        public LocalGptCatalogService(
            ILocalGptRuntimePolicyDataService runtimePolicy,
            ILogger<LocalGptCatalogService> logger)
        {
            _runtimePolicy = runtimePolicy ?? throw new ArgumentNullException(nameof(runtimePolicy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Retrieves a strongly typed database-backed service parameter document.</summary>
        /// <typeparam name="T">BusinessObject parameter type owned by the runtime policy store.</typeparam>
        /// <param name="key">Runtime value containing the serialized parameter document.</param>
        /// <returns>The deserialized parameter BusinessObject.</returns>
        public T GetRuntimeParameters<T>(string key)
        {
            try
            {
                return _runtimePolicy.GetJson<T>(key);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not resolve runtime parameter document {RuntimeValue} as {ParameterType}.", key, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets the default gradle version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default gradle version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultGradleVersion => _runtimePolicy.GetString("DefaultGradleVersion");
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
        public Regex NameCleaner => _runtimePolicy.GetPattern("builtin.name-cleaner");
        /// <summary>
        /// Gets the mod identifier cleaner value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The mod identifier cleaner value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ModIdCleaner => _runtimePolicy.GetPattern("builtin.mod-id-cleaner");
        /// <summary>
        /// Gets the package part cleaner value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The package part cleaner value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex PackagePartCleaner => _runtimePolicy.GetPattern("builtin.package-part-cleaner");

        /// <summary>
        /// Gets the default minecraft version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default minecraft version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultMinecraftVersion => _runtimePolicy.GetString("DefaultMinecraftVersion");

        /// <summary>
        /// Gets the default java version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default java version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultJavaVersion => _runtimePolicy.GetString("DefaultJavaVersion");
        /// <summary>
        /// Gets the fabric loader version value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The fabric loader version value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string FabricLoaderVersion => _runtimePolicy.GetString("FabricLoaderVersion");




        /// <summary>
        /// Gets the max DevExpress AI chat prompt characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max DevExpress AI chat prompt characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxDxAiChatPromptCharacters => _runtimePolicy.GetInt("MaxDxAiChatPromptCharacters");
        /// <summary>
        /// Gets the max visible prompt characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max visible prompt characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxVisiblePromptCharacters => _runtimePolicy.GetInt("MaxVisiblePromptCharacters");
        /// <summary>
        /// Gets the missing feature pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The missing feature pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex MissingFeaturePattern => _runtimePolicy.GetPattern("builtin.missing-feature-pattern");
        /// <summary>
        /// Gets the capability gap block pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The capability gap block pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex CapabilityGapBlockPattern => _runtimePolicy.GetPattern("builtin.capability-gap-block-pattern");
        /// <summary>
        /// Gets the truncated tail pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The truncated tail pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex TruncatedTailPattern => _runtimePolicy.GetPattern("builtin.truncated-tail-pattern");
        /// <summary>
        /// Gets the thinking block pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The thinking block pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ThinkingBlockPattern => _runtimePolicy.GetPattern("builtin.thinking-block-pattern");
        /// <summary>
        /// Gets the council prompt fence pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council prompt fence pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex CouncilPromptFencePattern => _runtimePolicy.GetPattern("builtin.council-prompt-fence-pattern");
        /// <summary>
        /// Gets the council request block pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council request block pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex CouncilRequestBlockPattern => _runtimePolicy.GetPattern("builtin.council-request-block-pattern");

        /// <summary>
        /// Gets the debug extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The debug extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> DebugExtensions => _runtimePolicy.GetCollection("DebugExtensions");
        /// <summary>
        /// Gets the text extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The text extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> TextExtensions => _runtimePolicy.GetCollection("TextExtensions");
        /// <summary>
        /// Gets the learn base known extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base known extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> LearnBaseKnownExtensions => _runtimePolicy.GetCollection("LearnBaseKnownExtensions");

        /// <summary>
        /// Gets the binary diagnostic extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The binary diagnostic extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> BinaryDiagnosticExtensions => _runtimePolicy.GetCollection("BinaryDiagnosticExtensions");
        /// <summary>
        /// Gets the target framework pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The target framework pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex TargetFrameworkPattern => _runtimePolicy.GetPattern("builtin.target-framework-pattern");
        /// <summary>
        /// Gets the package reference pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The package reference pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex PackageReferencePattern => _runtimePolicy.GetPattern("builtin.package-reference-pattern");
        /// <summary>
        /// Gets the sensitive name pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The sensitive name pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex SensitiveNamePattern => _runtimePolicy.GetPattern("builtin.sensitive-name-pattern");
        /// <summary>
        /// Gets the excluded directory names used by this LocalGPT catalog instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The excluded directory names value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> ExcludedDirectoryNames => _runtimePolicy.GetCollection("ExcludedDirectoryNames");

        /// <summary>
        /// Gets the binary extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The binary extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> BinaryExtensions => _runtimePolicy.GetCollection("BinaryExtensions");

        /// <summary>
        /// Gets the source extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The source extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> SourceExtensions => _runtimePolicy.GetCollection("SourceExtensions");
        /// <summary>
        /// Gets the default Ollama URI that identifies the network or application endpoint associated with this LocalGPT catalog state.
        /// </summary>
        /// <value>The default Ollama URI value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string DefaultOllamaUri => _runtimePolicy.GetString("DefaultOllamaUri");
        /// <summary>
        /// Gets the max participants value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max participants value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxParticipants => _runtimePolicy.GetInt("MaxParticipants");
        /// <summary>
        /// Gets the default max parallel models value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default max parallel models value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultMaxParallelModels => _runtimePolicy.GetInt("DefaultMaxParallelModels");
        /// <summary>
        /// Gets the default heavy model GPU layers value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default heavy model GPU layers value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultHeavyModelGpuLayers => _runtimePolicy.GetInt("DefaultHeavyModelGpuLayers");
        /// <summary>
        /// Gets the min context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The min context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MinContextTokens => _runtimePolicy.GetInt("MinContextTokens");
        /// <summary>
        /// Gets the default context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The default context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int DefaultContextTokens => _runtimePolicy.GetInt("DefaultContextTokens");
        /// <summary>
        /// Gets the max context tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max context tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxContextTokens => _runtimePolicy.GetInt("MaxContextTokens");
        /// <summary>
        /// Gets the min output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The min output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MinOutputTokens => _runtimePolicy.GetInt("MinOutputTokens");
        /// <summary>
        /// Gets the max output tokens value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max output tokens value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxOutputTokens => _runtimePolicy.GetInt("MaxOutputTokens");
        /// <summary>
        /// Gets the stream status pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The stream status pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex StreamStatusPattern => _runtimePolicy.GetPattern("builtin.stream-status-pattern");
        /// <summary>
        /// Gets the word pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The word pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex WordPattern => _runtimePolicy.GetPattern("builtin.word-pattern");
        /// <summary>
        /// Gets the development request pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The development request pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevelopmentRequestPattern => _runtimePolicy.GetPattern("builtin.development-request-pattern");
        /// <summary>
        /// Gets the explicit artifact intent pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The explicit artifact intent pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExplicitArtifactIntentPattern => _runtimePolicy.GetPattern("builtin.explicit-artifact-intent-pattern");
        /// <summary>
        /// Gets the advice only prompt pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The advice only prompt pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex AdviceOnlyPromptPattern => _runtimePolicy.GetPattern("builtin.advice-only-prompt-pattern");
        /// <summary>
        /// Gets the explicit artifact creation command pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The explicit artifact creation command pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExplicitArtifactCreationCommandPattern => _runtimePolicy.GetPattern("builtin.explicit-artifact-creation-command-pattern");
        /// <summary>
        /// Gets the concrete minecraft artifact pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The concrete minecraft artifact pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ConcreteMinecraftArtifactPattern => _runtimePolicy.GetPattern("builtin.concrete-minecraft-artifact-pattern");
        /// <summary>
        /// Gets the concrete dot net artifact pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The concrete dot net artifact pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ConcreteDotNetArtifactPattern => _runtimePolicy.GetPattern("builtin.concrete-dot-net-artifact-pattern");
        /// <summary>
        /// Gets the AI host setup pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The AI host setup pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex AiHostSetupPattern => _runtimePolicy.GetPattern("builtin.ai-host-setup-pattern");
        /// <summary>
        /// Gets the implementation decision pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The implementation decision pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ImplementationDecisionPattern => _runtimePolicy.GetPattern("builtin.implementation-decision-pattern");
        /// <summary>
        /// Gets the implementation choice pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The implementation choice pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ImplementationChoicePattern => _runtimePolicy.GetPattern("builtin.implementation-choice-pattern");
        /// <summary>
        /// Gets the blocking artifact decision pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The blocking artifact decision pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex BlockingArtifactDecisionPattern => _runtimePolicy.GetPattern("builtin.blocking-artifact-decision-pattern");
        /// <summary>
        /// Gets the safe sandbox consent pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The safe sandbox consent pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex SafeSandboxConsentPattern => _runtimePolicy.GetPattern("builtin.safe-sandbox-consent-pattern");
        /// <summary>
        /// Gets or sets explicit do not generate until user decision pattern.
        /// </summary>
        /// <value>The explicit do not generate until user decision pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex ExplicitDoNotGenerateUntilUserDecisionPattern => _runtimePolicy.GetPattern("builtin.explicit-do-not-generate-until-user-decision-pattern");
        /// <summary>
        /// Gets the developer execution intent pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The developer execution intent pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DeveloperExecutionIntentPattern => _runtimePolicy.GetPattern("builtin.developer-execution-intent-pattern");

 


        /// <summary>
        /// Gets the DevExpress import pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The DevExpress import pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevExpressImportPattern => _runtimePolicy.GetPattern("builtin.dev-express-import-pattern");
        /// <summary>
        /// Gets the DevExpress registration pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The DevExpress registration pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DevExpressRegistrationPattern => _runtimePolicy.GetPattern("builtin.dev-express-registration-pattern");
        /// <summary>
        /// Gets the artifact text extensions value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact text extensions value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public FrozenSet<string> ArtifactTextExtensions => _runtimePolicy.GetCollection("ArtifactTextExtensions");

        /// <summary>
        /// Gets the max artifact text file bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max artifact text file bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxArtifactTextFileBytes => _runtimePolicy.GetLong("MaxArtifactTextFileBytes");
     


        /// <summary>
        /// Gets the max files value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max files value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxFiles => _runtimePolicy.GetInt("MaxFiles");
        /// <summary>
        /// Gets the max single file bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max single file bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxSingleFileBytes => _runtimePolicy.GetLong("MaxSingleFileBytes");
        /// <summary>
        /// Gets the max total file bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max total file bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxTotalFileBytes => _runtimePolicy.GetLong("MaxTotalFileBytes");
        /// <summary>
        /// Gets the max ZIP entries value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max ZIP entries value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxZipEntries => _runtimePolicy.GetInt("MaxZipEntries");
        /// <summary>
        /// Gets the max ZIP entry bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max ZIP entry bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxZipEntryBytes => _runtimePolicy.GetLong("MaxZipEntryBytes");
        /// <summary>
        /// Gets the max extracted bytes value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max extracted bytes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public long MaxExtractedBytes => _runtimePolicy.GetLong("MaxExtractedBytes");
        /// <summary>
        /// Gets the max context characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max context characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxContextCharacters => _runtimePolicy.GetInt("MaxContextCharacters");
        /// <summary>
        /// Gets the max excerpt characters per file value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max excerpt characters per file value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxExcerptCharactersPerFile => _runtimePolicy.GetInt("MaxExcerptCharactersPerFile");
        /// <summary>
        /// Gets the max binary string characters value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max binary string characters value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public int MaxBinaryStringCharacters => _runtimePolicy.GetInt("MaxBinaryStringCharacters");
        /// <summary>
        /// Gets the knowledge files value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The knowledge files value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string[] KnowledgeFiles => _runtimePolicy.GetCollection("KnowledgeFiles").Select(value => value.Replace('/', Path.DirectorySeparatorChar)).ToArray();
        /// <summary>
        /// Gets the omission value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The omission value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string omission => _runtimePolicy.GetString("ContextOmission");

        /// <summary>
        /// Gets the short omission value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The short omission value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string shortOmission => _runtimePolicy.GetString("ShortContextOmission");
        /// <summary>
        /// Gets the download URL pattern value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The download URL pattern value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public Regex DownloadUrlPattern => _runtimePolicy.GetPattern("runtime.download-url");
        /// <summary>
        /// Gets the learn base file policy summary value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base file policy summary value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LearnBaseFilePolicySummary => _runtimePolicy.GetString("LearnBaseFilePolicySummary");
        /// <summary>
        /// Gets the learn base duplicate policy summary value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base duplicate policy summary value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LearnBaseDuplicatePolicySummary => _runtimePolicy.GetString("LearnBaseDuplicatePolicySummary");
        /// <summary>
        /// Gets the learn base preset list value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base preset list value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string LearnBasePresetList => string.Join(", ", LearnBasePresets.Select(preset => preset.Label));

        /// <summary>
        /// Gets the learn base presets collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The learn base presets value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public IReadOnlyList<LearnBasePreset> LearnBasePresets => _runtimePolicy.GetJson<LearnBasePreset[]>("LearnBasePresetsJson");
        /// <summary>
        /// Gets the learn base scan profiles collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The learn base scan profiles value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public IReadOnlyList<LearnBaseScanProfile> LearnBaseScanProfiles => _runtimePolicy.GetJson<LearnBaseScanProfile[]>("LearnBaseScanProfilesJson");

        /// <summary>
        /// Gets the routes collection maintained or exposed by this LocalGPT catalog instance for downstream processing.
        /// </summary>
        /// <value>The routes value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public List<TestLabRoute> Routes => [.. _runtimePolicy.GetJson<TestLabRoute[]>("TestLabRoutesJson")];
}
}
