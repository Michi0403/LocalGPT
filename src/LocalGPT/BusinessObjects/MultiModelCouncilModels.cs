using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the input contract for multi model council, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    public sealed class MultiModelCouncilRequest
    {
        /// <summary>
        /// Gets or sets the stable run identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The run identifier value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public Guid RunId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the prompt value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The prompt value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model names collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The model names value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public List<string> ModelNames { get; set; } = [];

        /// <summary>Provider-qualified model identities for this run. Bare ModelNames remain supported for legacy presets.</summary>
        /// <value>The model selections value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public List<ProviderModelReference> ModelSelections { get; set; } = [];

        /// <summary>Exact saved provider routes that the current Chat UI cannot match to a configured/discovered candidate.</summary>
        /// <value>The unavailable model selections value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public List<string> UnavailableModelSelections { get; set; } = [];

        /// <summary>
        /// Gets or sets the base URI that identifies the network or application endpoint associated with this multi model council state.
        /// </summary>
        /// <value>The base URI value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string? BaseUri { get; set; }

        /// <summary>
        /// Gets or sets the max rounds value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max rounds value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int MaxRounds { get; set; } = 1;

        /// <summary>
        /// Gets or sets the max output tokens value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max output tokens value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int MaxOutputTokens { get; set; } = 1024;

        /// <summary>Maximum concurrently executing model requests per participating AI host/PC. Each logical Council phase still waits for all assigned members before advancing.</summary>
        /// <value>The max parallel models value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int MaxParallelModels { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether parallel hardware roads applies to the multi model council state.
        /// </summary>
        /// <value>The allow parallel hardware roads value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool AllowParallelHardwareRoads { get; set; } = true;

        /// <summary>0..100 session position between each model route's independent minimum and maximum.</summary>
        /// <value>The resource load percent value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int ResourceLoadPercent { get; set; } = 30;

        /// <summary>
        /// Gets or sets the model routes collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The model routes value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];

        /// <summary>
        /// Gets or sets the max context tokens value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max context tokens value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int MaxContextTokens { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the model timeout seconds value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model timeout seconds value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int ModelTimeoutSeconds { get; set; } = 180;

        /// <summary>
        /// Gets or sets the Ollama keep alive value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The Ollama keep alive value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string? OllamaKeepAlive { get; set; }

        /// <summary>
        /// Gets or sets the Ollama num GPU value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The Ollama num GPU value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public int? OllamaNumGpu { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether memory applies to the multi model council state.
        /// </summary>
        /// <value>The include memory value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool IncludeMemory { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether save to memory applies to the multi model council state.
        /// </summary>
        /// <value>The save to memory value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool SaveToMemory { get; set; } = true;

        /// <summary>
        /// Gets or sets the title value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The title value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the stable continue conversation identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The continue conversation identifier value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public Guid? ContinueConversationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether generate implementation artifact applies to the multi model council state.
        /// </summary>
        /// <value>The generate implementation artifact value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool GenerateImplementationArtifact { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user confirmed artifact build applies to the multi model council state.
        /// </summary>
        /// <value>The user confirmed artifact build value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool UserConfirmedArtifactBuild { get; set; }

        /// <summary>
        /// Gets or sets use change review workflow.
        /// </summary>
        /// <value>The use change review workflow value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool UseChangeReviewWorkflow { get; set; } = true;

        /// <summary>
        /// Gets or sets the stable project identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The project identifier value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the stable project topic identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The project topic identifier value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public Guid? ProjectTopicId { get; set; }

        /// <summary>
        /// Gets or sets the stable project revision identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The project revision identifier value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public Guid? ProjectRevisionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether create project for run applies to the multi model council state.
        /// </summary>
        /// <value>The create project for run value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool CreateProjectForRun { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user confirmed project link applies to the multi model council state.
        /// </summary>
        /// <value>The user confirmed project link value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool UserConfirmedProjectLink { get; set; }

        /// <summary>
        /// Gets or sets use organic council workflow.
        /// </summary>
        /// <value>The use organic council workflow value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public bool UseOrganicCouncilWorkflow { get; set; }

        /// <summary>
        /// Gets or sets the stable council team key used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The council team key value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string CouncilTeamKey { get; set; } = "general";

        /// <summary>
        /// Gets or sets the council leader model name value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The council leader model name value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string CouncilLeaderModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requested organic capabilities collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The requested organic capabilities value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public List<string> RequestedOrganicCapabilities { get; set; } = [];

        /// <summary>
        /// Gets or sets the external project context JSON value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The external project context JSON value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string ExternalProjectContextJson { get; set; } = "{}";

        /// <summary>
        /// Gets or sets the stable one wire correlation identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The one wire correlation identifier value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        public string OneWireCorrelationId { get; set; } = string.Empty;

        /// <summary>Gets or sets the process-local nesting depth for a Council started by an X-Function.</summary>
        /// <value>The x round child depth value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        [JsonIgnore]
        public int XRoundChildDepth { get; set; }

        /// <summary>
        /// Gets or sets the progress message value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The progress message value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        [JsonIgnore]
        public Action<string>? ProgressMessage { get; set; }

        /// <summary>
        /// Gets or sets the stream update value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The stream update value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        [JsonIgnore]
        public Action<string>? StreamUpdate { get; set; }

        /// <summary>
        /// Gets or sets the step completed value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The step completed value exposed by <see cref="MultiModelCouncilRequest"/>.</value>
        [JsonIgnore]
        public Action<MultiModelCouncilStep>? StepCompleted { get; set; }
    }

    /// <summary>
    /// Represents a multi model council model candidate application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="ModelName">Model name value supplied to the multi model council model candidate operation and used when producing its result.</param>
    /// <param name="Provider">Provider value supplied to the multi model council model candidate operation and used when producing its result.</param>
    /// <param name="Endpoint">Endpoint value supplied to the multi model council model candidate operation and used when producing its result.</param>
    /// <param name="IsInstalled">Value indicating whether installed should apply to this operation.</param>
    /// <param name="IsConfigured">Value indicating whether configured should apply to this operation.</param>
    /// <param name="IsLoaded">Value indicating whether loaded should apply to this operation.</param>
    /// <param name="Details">Details value supplied to the multi model council model candidate operation and used when producing its result.</param>
    /// <param name="ProviderKind">Provider kind value supplied to the multi model council model candidate operation and used when producing its result.</param>
    /// <param name="IsLocal">Value indicating whether local should apply to this operation.</param>
    /// <param name="SupportsBenchmark">Value indicating whether benchmark should apply to this operation.</param>
    public sealed record MultiModelCouncilModelCandidate(
        string ModelName,
        string Provider,
        string Endpoint,
        bool IsInstalled,
        bool IsConfigured,
        bool IsLoaded,
        string? Details,
        string ProviderKind = ProviderModelKinds.Ollama,
        bool IsLocal = true,
        bool SupportsBenchmark = true)
    {
        /// <summary>
        /// Gets the display name value that forms part of the multi model council model candidate state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The display name value exposed by <see cref="MultiModelCouncilModelCandidate"/>.</value>
        public string DisplayName => $"{ModelName} - {Provider}";
        /// <summary>
        /// Gets the stable selection key used to identify or correlate this multi model council model candidate instance with related application state.
        /// </summary>
        /// <value>The selection key value exposed by <see cref="MultiModelCouncilModelCandidate"/>.</value>
        public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(Provider, Endpoint, ModelName);

        /// <summary>
        /// Performs to reference for <see cref="MultiModelCouncilModelCandidate"/>, keeping the operation consistent with the state and invariants of the surrounding multi model council model candidate workflow.
        /// </summary>
        /// <returns>The provider model reference produced by the operation.</returns>
        public ProviderModelReference ToReference() => new()
        {
            ProviderKind = ProviderKind,
            ProviderName = Provider,
            Endpoint = Endpoint,
            ModelName = ModelName,
            IsLocal = IsLocal,
            IsReachable = IsInstalled,
            IsConfigured = IsConfigured,
            IsLoaded = IsLoaded,
            SupportsBenchmark = SupportsBenchmark,
            Details = Details ?? string.Empty
        };
    }

    /// <summary>
    /// Represents the outcome of multi model council, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public sealed class MultiModelCouncilResult
    {
        /// <summary>
        /// Gets or sets the stable run identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The run identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid RunId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the started at UTC associated with this multi model council state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The started at UTC value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the completed at UTC associated with this multi model council state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The completed at UTC value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the prompt value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The prompt value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model names collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The model names value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public List<string> ModelNames { get; set; } = [];

        /// <summary>
        /// Gets or sets the model selections collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The model selections value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public List<ProviderModelReference> ModelSelections { get; set; } = [];

        /// <summary>
        /// Gets or sets continued from conversation identifier.
        /// </summary>
        /// <value>The continued from conversation identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid? ContinuedFromConversationId { get; set; }

        /// <summary>
        /// Gets or sets the continued from title value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The continued from title value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string? ContinuedFromTitle { get; set; }

        /// <summary>
        /// Gets or sets the steps collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The steps value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public List<MultiModelCouncilStep> Steps { get; set; } = [];

        /// <summary>
        /// Gets or sets the final answer value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The final answer value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string FinalAnswer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user poll value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The user poll value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public CouncilUserPoll? UserPoll { get; set; }

        /// <summary>
        /// Gets or sets the stable memory conversation identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The memory conversation identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid? MemoryConversationId { get; set; }

        /// <summary>
        /// Gets or sets the stable knowledge entry identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The knowledge entry identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid? KnowledgeEntryId { get; set; }

        /// <summary>
        /// Gets or sets the stable project identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The project identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the stable project topic identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The project topic identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid? ProjectTopicId { get; set; }

        /// <summary>
        /// Gets or sets the stable project revision identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The project revision identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public Guid? ProjectRevisionId { get; set; }

        /// <summary>
        /// Gets or sets the log path used by this multi model council instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The log path value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string? LogPath { get; set; }

        /// <summary>
        /// Gets or sets the artifacts collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The artifacts value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public List<CouncilArtifact> Artifacts { get; set; } = [];

        /// <summary>
        /// Gets or sets the change review value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The change review value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public CodeGenerationReviewSnapshot? ChangeReview { get; set; }

        /// <summary>
        /// Gets or sets the warnings collection maintained or exposed by this multi model council instance for downstream processing.
        /// </summary>
        /// <value>The warnings value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Gets or sets the preflight summary value that forms part of the multi model council state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The preflight summary value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string PreflightSummary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stable council team key used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The council team key value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string CouncilTeamKey { get; set; } = "general";

        /// <summary>
        /// Gets or sets the stable one wire correlation identifier used to identify or correlate this multi model council instance with related application state.
        /// </summary>
        /// <value>The one wire correlation identifier value exposed by <see cref="MultiModelCouncilResult"/>.</value>
        public string OneWireCorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a council artifact application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class CouncilArtifact
    {
        /// <summary>
        /// Gets or sets the name value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="CouncilArtifact"/>.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the kind value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The kind value exposed by <see cref="CouncilArtifact"/>.</value>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path used by this council artifact instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The file path value exposed by <see cref="CouncilArtifact"/>.</value>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the download URL that identifies the network or application endpoint associated with this council artifact state.
        /// </summary>
        /// <value>The download URL value exposed by <see cref="CouncilArtifact"/>.</value>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the summary value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The summary value exposed by <see cref="CouncilArtifact"/>.</value>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quality status value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The quality status value exposed by <see cref="CouncilArtifact"/>.</value>
        public string QualityStatus { get; set; } = "Generated only";

        /// <summary>
        /// Gets or sets the contract status value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The contract status value exposed by <see cref="CouncilArtifact"/>.</value>
        public string ContractStatus { get; set; } = "Not validated";

        /// <summary>
        /// Gets or sets the contract checks collection maintained or exposed by this council artifact instance for downstream processing.
        /// </summary>
        /// <value>The contract checks value exposed by <see cref="CouncilArtifact"/>.</value>
        public List<string> ContractChecks { get; set; } = [];

        /// <summary>
        /// Gets or sets the missing requirements collection maintained or exposed by this council artifact instance for downstream processing.
        /// </summary>
        /// <value>The missing requirements value exposed by <see cref="CouncilArtifact"/>.</value>
        public List<string> MissingRequirements { get; set; } = [];
    }

    /// <summary>
    /// Represents a council user poll application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class CouncilUserPoll
    {
        /// <summary>
        /// Gets or sets the question value that forms part of the council user poll state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The question value exposed by <see cref="CouncilUserPoll"/>.</value>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the options collection maintained or exposed by this council user poll instance for downstream processing.
        /// </summary>
        /// <value>The options value exposed by <see cref="CouncilUserPoll"/>.</value>
        public List<CouncilUserPollOption> Options { get; set; } = [];

        /// <summary>
        /// Gets or sets the reason value that forms part of the council user poll state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The reason value exposed by <see cref="CouncilUserPoll"/>.</value>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>Identifies semantic behavior attached to a Council poll option independently from its translated label.</summary>
    public enum CouncilUserPollOptionKind
    {
        /// <summary>No special UI-side behavior is required.</summary>
        Standard = 0,
        /// <summary>The option confirms removal of unavailable or faulty Council members.</summary>
        ExcludeUnavailableMembers = 1
    }

    /// <summary>
    /// Represents a council user poll option application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class CouncilUserPollOption
    {
        /// <summary>
        /// Gets or sets the label value that forms part of the council user poll option state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The label value exposed by <see cref="CouncilUserPollOption"/>.</value>
        public string Label { get; set; } = string.Empty;

        /// <summary>Gets or sets the semantic option kind used by UI behavior without inspecting translated labels.</summary>
        /// <value>The kind value exposed by <see cref="CouncilUserPollOption"/>.</value>
        public CouncilUserPollOptionKind Kind { get; set; } = CouncilUserPollOptionKind.Standard;

        /// <summary>
        /// Gets or sets the follow up prompt value that forms part of the council user poll option state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The follow up prompt value exposed by <see cref="CouncilUserPollOption"/>.</value>
        public string FollowUpPrompt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a multi model council step application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class MultiModelCouncilStep
    {
        /// <summary>
        /// Gets or sets the sort order value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The sort order value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the round value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The round value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int Round { get; set; }

        /// <summary>
        /// Gets or sets the phase value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The phase value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string Phase { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The model name value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The provider name value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider endpoint that identifies the network or application endpoint associated with this multi model council step state.
        /// </summary>
        /// <value>The provider endpoint value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string ProviderEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider model name value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The provider model name value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string ProviderModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the council members collection maintained or exposed by this multi model council step instance for downstream processing.
        /// </summary>
        /// <value>The council members value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public List<string> CouncilMembers { get; set; } = [];

        /// <summary>
        /// Gets or sets the role value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The role value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hardware lane value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The hardware lane value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string HardwareLane { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hardware kind value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The hardware kind value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;

        /// <summary>
        /// Gets or sets the hardware index value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The hardware index value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int HardwareIndex { get; set; } = -1;

        /// <summary>
        /// Gets or sets the effective load percent value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The effective load percent value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int EffectiveLoadPercent { get; set; } = 30;

        /// <summary>
        /// Gets or sets the effective max output tokens value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The effective max output tokens value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int EffectiveMaxOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the effective max context tokens value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The effective max context tokens value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int EffectiveMaxContextTokens { get; set; }

        /// <summary>
        /// Gets or sets the content value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The content value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible content value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The visible content value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string VisibleContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the thinking value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The thinking value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string? Thinking { get; set; }

        /// <summary>
        /// Gets or sets the started at UTC associated with this multi model council step state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The started at UTC value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the completed at UTC associated with this multi model council step state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The completed at UTC value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the duration seconds value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The duration seconds value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the error value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The error value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string? Error { get; set; }

        /// <summary>Gets or sets the configured workflow-step key that produced this immutable transcript entry.</summary>
        /// <value>The workflow step key value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string WorkflowStepKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the one-based X-Round revision number for the configured workflow step.</summary>
        /// <value>The workflow revision value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public int WorkflowRevision { get; set; } = 1;

        /// <summary>Gets or sets the causal reason that revisited this workflow step, without replacing earlier transcript revisions.</summary>
        /// <value>The x round cause value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        public string XRoundCause { get; set; } = string.Empty;

        /// <summary>
        /// Gets the brain part value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The brain part value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        [JsonIgnore]
        public string BrainPart => string.IsNullOrWhiteSpace(Role) ? Phase : Role;

        /// <summary>
        /// Gets the moment value that forms part of the multi model council step state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The moment value exposed by <see cref="MultiModelCouncilStep"/>.</value>
        [JsonIgnore]
        public string Moment => $"Round {Round}: {Phase}";
    }
}
