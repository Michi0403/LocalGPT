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
    /// Coordinates local GPT catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class LocalGptCatalogService
    {
    /// <summary>
        /// <summary>
        /// Gets the dev express document pattern value that forms part of the local GPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
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
        /// <summary>Gets the operator-configured maximum automatic Ollama tool rounds.</summary>
        public int OllamaMaximumAutomaticToolRounds => _runtimePolicy.GetInt(LocalGptRuntimeValue.OllamaMaximumAutomaticToolRounds);
        /// <summary>Gets the operator-configured maximum Ollama tool-result character count.</summary>
        public int OllamaMaximumToolResultCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.OllamaMaximumToolResultCharacters);
 
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

        /// <summary>Gets whether automatic provider-stream repetition termination is enabled by operator policy.</summary>
        public bool ProviderStreamRepetitionWatchdogEnabled => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionWatchdogEnabled) != 0;
        /// <summary>Gets the repetition watchdog rolling character window.</summary>
        public int ProviderStreamRepetitionMaximumBufferedCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMaximumBufferedCharacters);
        /// <summary>Gets the minimum observed character count before repetition classification begins.</summary>
        public int ProviderStreamRepetitionMinimumObservedCharacters => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumObservedCharacters);
        /// <summary>Gets the minimum token count analyzed by repetition classification.</summary>
        public int ProviderStreamRepetitionMinimumAnalyzedTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumAnalyzedTokens);
        /// <summary>Gets the maximum token-cycle period analyzed by the repetition watchdog.</summary>
        public int ProviderStreamRepetitionMaximumPeriodTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMaximumPeriodTokens);
        /// <summary>Gets the short-period boundary for repetition classification.</summary>
        public int ProviderStreamRepetitionShortPeriodMaximumTokens => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionShortPeriodMaximumTokens);
        /// <summary>Gets the required short-cycle repetitions.</summary>
        public int ProviderStreamRepetitionMinimumRepeatedCycles => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumRepeatedCycles);
        /// <summary>Gets the required long-cycle repetitions.</summary>
        public int ProviderStreamRepetitionMinimumLongPeriodRepeatedCycles => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumLongPeriodRepeatedCycles);
        /// <summary>Gets the short-cycle agreement threshold in basis points.</summary>
        public int ProviderStreamRepetitionMinimumPeriodicAgreementBasisPoints => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumPeriodicAgreementBasisPoints);
        /// <summary>Gets the long-cycle agreement threshold in basis points.</summary>
        public int ProviderStreamRepetitionMinimumLongPeriodAgreementBasisPoints => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumLongPeriodAgreementBasisPoints);
        /// <summary>Gets the suspicious sample count required before termination.</summary>
        public int ProviderStreamRepetitionRequiredSuspiciousSamples => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionRequiredSuspiciousSamples);
        /// <summary>Gets the initial watchdog observation delay in milliseconds.</summary>
        public int ProviderStreamRepetitionInitialObservationMilliseconds => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionInitialObservationMilliseconds);
        /// <summary>Gets the watchdog sample interval in milliseconds.</summary>
        public int ProviderStreamRepetitionSampleIntervalMilliseconds => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionSampleIntervalMilliseconds);
        /// <summary>Gets the minimum suspicious watchdog duration in milliseconds.</summary>
        public int ProviderStreamRepetitionMinimumSuspiciousDurationMilliseconds => _runtimePolicy.GetInt(LocalGptRuntimeValue.ProviderStreamRepetitionMinimumSuspiciousDurationMilliseconds);

    
    }
}
