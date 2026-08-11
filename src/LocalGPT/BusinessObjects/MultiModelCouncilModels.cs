using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a multi model council request.
    /// </summary>
    public sealed class MultiModelCouncilRequest
    {
        /// <summary>
        /// Gets or sets run identifier.
        /// </summary>
        public Guid RunId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets prompt.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets model names.
        /// </summary>
        public List<string> ModelNames { get; set; } = [];

        /// <summary>Provider-qualified model identities for this run. Bare ModelNames remain supported for legacy presets.</summary>
        public List<ProviderModelReference> ModelSelections { get; set; } = [];

        /// <summary>Exact saved provider routes that the current Chat UI cannot match to a configured/discovered candidate.</summary>
        public List<string> UnavailableModelSelections { get; set; } = [];

        /// <summary>
        /// Gets or sets base URI.
        /// </summary>
        public string? BaseUri { get; set; }

        /// <summary>
        /// Gets or sets max rounds.
        /// </summary>
        public int MaxRounds { get; set; } = 1;

        /// <summary>
        /// Gets or sets max output tokens.
        /// </summary>
        public int MaxOutputTokens { get; set; } = 1024;

        /// <summary>Maximum concurrently executing model requests per participating AI host/PC. Each logical Council phase still waits for all assigned members before advancing.</summary>
        public int MaxParallelModels { get; set; } = 1;

        /// <summary>
        /// Gets or sets allow parallel hardware roads.
        /// </summary>
        public bool AllowParallelHardwareRoads { get; set; } = true;

        /// <summary>0..100 session position between each model route's independent minimum and maximum.</summary>
        public int ResourceLoadPercent { get; set; } = 30;

        /// <summary>
        /// Gets or sets model routes.
        /// </summary>
        public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];

        /// <summary>
        /// Gets or sets max context tokens.
        /// </summary>
        public int MaxContextTokens { get; set; } = 4096;

        /// <summary>
        /// Gets or sets model timeout seconds.
        /// </summary>
        public int ModelTimeoutSeconds { get; set; } = 180;

        /// <summary>
        /// Gets or sets ollama keep alive.
        /// </summary>
        public string? OllamaKeepAlive { get; set; }

        /// <summary>
        /// Gets or sets ollama num gpu.
        /// </summary>
        public int? OllamaNumGpu { get; set; }

        /// <summary>
        /// Gets or sets include memory.
        /// </summary>
        public bool IncludeMemory { get; set; } = true;

        /// <summary>
        /// Gets or sets save to memory.
        /// </summary>
        public bool SaveToMemory { get; set; } = true;

        /// <summary>
        /// Gets or sets title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets continue conversation identifier.
        /// </summary>
        public Guid? ContinueConversationId { get; set; }

        /// <summary>
        /// Gets or sets generate implementation artifact.
        /// </summary>
        public bool GenerateImplementationArtifact { get; set; }

        /// <summary>
        /// Gets or sets user confirmed artifact build.
        /// </summary>
        public bool UserConfirmedArtifactBuild { get; set; }

        /// <summary>
        /// Gets or sets use change review workflow.
        /// </summary>
        public bool UseChangeReviewWorkflow { get; set; } = true;

        /// <summary>
        /// Gets or sets project identifier.
        /// </summary>
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets project topic identifier.
        /// </summary>
        public Guid? ProjectTopicId { get; set; }

        /// <summary>
        /// Gets or sets project revision identifier.
        /// </summary>
        public Guid? ProjectRevisionId { get; set; }

        /// <summary>
        /// Gets or sets create project for run.
        /// </summary>
        public bool CreateProjectForRun { get; set; }

        /// <summary>
        /// Gets or sets user confirmed project link.
        /// </summary>
        public bool UserConfirmedProjectLink { get; set; }

        /// <summary>
        /// Gets or sets use organic council workflow.
        /// </summary>
        public bool UseOrganicCouncilWorkflow { get; set; }

        /// <summary>
        /// Gets or sets council team key.
        /// </summary>
        public string CouncilTeamKey { get; set; } = "general";

        /// <summary>
        /// Gets or sets council leader model name.
        /// </summary>
        public string CouncilLeaderModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets requested organic capabilities.
        /// </summary>
        public List<string> RequestedOrganicCapabilities { get; set; } = [];

        /// <summary>
        /// Gets or sets external project context JSON.
        /// </summary>
        public string ExternalProjectContextJson { get; set; } = "{}";

        /// <summary>
        /// Gets or sets one wire correlation identifier.
        /// </summary>
        public string OneWireCorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets progress message.
        /// </summary>
        [JsonIgnore]
        public Action<string>? ProgressMessage { get; set; }

        /// <summary>
        /// Gets or sets stream update.
        /// </summary>
        [JsonIgnore]
        public Action<string>? StreamUpdate { get; set; }

        /// <summary>
        /// Gets or sets step completed.
        /// </summary>
        [JsonIgnore]
        public Action<MultiModelCouncilStep>? StepCompleted { get; set; }
    }

    /// <summary>
    /// Represents a multi model council model candidate.
    /// </summary>
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
        /// Gets or sets display name.
        /// </summary>
        public string DisplayName => $"{ModelName} - {Provider}";
        /// <summary>
        /// Gets or sets selection key.
        /// </summary>
        public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(Provider, Endpoint, ModelName);

        /// <summary>
        /// Runs the to reference operation.
        /// </summary>
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
    /// Represents a multi model council result.
    /// </summary>
    public sealed class MultiModelCouncilResult
    {
        /// <summary>
        /// Gets or sets run identifier.
        /// </summary>
        public Guid RunId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets started at UTC.
        /// </summary>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets completed at UTC.
        /// </summary>
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets prompt.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets model names.
        /// </summary>
        public List<string> ModelNames { get; set; } = [];

        /// <summary>
        /// Gets or sets model selections.
        /// </summary>
        public List<ProviderModelReference> ModelSelections { get; set; } = [];

        /// <summary>
        /// Gets or sets continued from conversation identifier.
        /// </summary>
        public Guid? ContinuedFromConversationId { get; set; }

        /// <summary>
        /// Gets or sets continued from title.
        /// </summary>
        public string? ContinuedFromTitle { get; set; }

        /// <summary>
        /// Gets or sets steps.
        /// </summary>
        public List<MultiModelCouncilStep> Steps { get; set; } = [];

        /// <summary>
        /// Gets or sets final answer.
        /// </summary>
        public string FinalAnswer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets user poll.
        /// </summary>
        public CouncilUserPoll? UserPoll { get; set; }

        /// <summary>
        /// Gets or sets memory conversation identifier.
        /// </summary>
        public Guid? MemoryConversationId { get; set; }

        /// <summary>
        /// Gets or sets knowledge entry identifier.
        /// </summary>
        public Guid? KnowledgeEntryId { get; set; }

        /// <summary>
        /// Gets or sets project identifier.
        /// </summary>
        public Guid? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets project topic identifier.
        /// </summary>
        public Guid? ProjectTopicId { get; set; }

        /// <summary>
        /// Gets or sets project revision identifier.
        /// </summary>
        public Guid? ProjectRevisionId { get; set; }

        /// <summary>
        /// Gets or sets log path.
        /// </summary>
        public string? LogPath { get; set; }

        /// <summary>
        /// Gets or sets artifacts.
        /// </summary>
        public List<CouncilArtifact> Artifacts { get; set; } = [];

        /// <summary>
        /// Gets or sets change review.
        /// </summary>
        public CodeGenerationReviewSnapshot? ChangeReview { get; set; }

        /// <summary>
        /// Gets or sets warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Gets or sets preflight summary.
        /// </summary>
        public string PreflightSummary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets council team key.
        /// </summary>
        public string CouncilTeamKey { get; set; } = "general";

        /// <summary>
        /// Gets or sets one wire correlation identifier.
        /// </summary>
        public string OneWireCorrelationId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a council artifact.
    /// </summary>
    public sealed class CouncilArtifact
    {
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets kind.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets download URL.
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets summary.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets quality status.
        /// </summary>
        public string QualityStatus { get; set; } = "Generated only";

        /// <summary>
        /// Gets or sets contract status.
        /// </summary>
        public string ContractStatus { get; set; } = "Not validated";

        /// <summary>
        /// Gets or sets contract checks.
        /// </summary>
        public List<string> ContractChecks { get; set; } = [];

        /// <summary>
        /// Gets or sets missing requirements.
        /// </summary>
        public List<string> MissingRequirements { get; set; } = [];
    }

    /// <summary>
    /// Represents a council user poll.
    /// </summary>
    public sealed class CouncilUserPoll
    {
        /// <summary>
        /// Gets or sets question.
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets options.
        /// </summary>
        public List<CouncilUserPollOption> Options { get; set; } = [];

        /// <summary>
        /// Gets or sets reason.
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a council user poll option.
    /// </summary>
    public sealed class CouncilUserPollOption
    {
        /// <summary>
        /// Gets or sets label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets follow up prompt.
        /// </summary>
        public string FollowUpPrompt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a multi model council step.
    /// </summary>
    public sealed class MultiModelCouncilStep
    {
        /// <summary>
        /// Gets or sets sort order.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets round.
        /// </summary>
        public int Round { get; set; }

        /// <summary>
        /// Gets or sets phase.
        /// </summary>
        public string Phase { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets model name.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets provider name.
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets provider endpoint.
        /// </summary>
        public string ProviderEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets provider model name.
        /// </summary>
        public string ProviderModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets council members.
        /// </summary>
        public List<string> CouncilMembers { get; set; } = [];

        /// <summary>
        /// Gets or sets role.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets hardware lane.
        /// </summary>
        public string HardwareLane { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets hardware kind.
        /// </summary>
        public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;

        /// <summary>
        /// Gets or sets hardware index.
        /// </summary>
        public int HardwareIndex { get; set; } = -1;

        /// <summary>
        /// Gets or sets effective load percent.
        /// </summary>
        public int EffectiveLoadPercent { get; set; } = 30;

        /// <summary>
        /// Gets or sets effective max output tokens.
        /// </summary>
        public int EffectiveMaxOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets effective max context tokens.
        /// </summary>
        public int EffectiveMaxContextTokens { get; set; }

        /// <summary>
        /// Gets or sets content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets visible content.
        /// </summary>
        public string VisibleContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets thinking.
        /// </summary>
        public string? Thinking { get; set; }

        /// <summary>
        /// Gets or sets started at UTC.
        /// </summary>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets completed at UTC.
        /// </summary>
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets duration seconds.
        /// </summary>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets error.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets brain part.
        /// </summary>
        [JsonIgnore]
        public string BrainPart => string.IsNullOrWhiteSpace(Role) ? Phase : Role;

        /// <summary>
        /// Gets or sets moment.
        /// </summary>
        [JsonIgnore]
        public string Moment => $"Round {Round}: {Phase}";
    }
}
