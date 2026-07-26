using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects
{
    public sealed class MultiModelCouncilRequest
    {
        public string Prompt { get; set; } = string.Empty;

        public List<string> ModelNames { get; set; } = [];

        public string? BaseUri { get; set; }

        public int MaxRounds { get; set; } = 1;

        public int MaxOutputTokens { get; set; } = 1024;

        public int MaxParallelModels { get; set; } = 1;

        public bool AllowParallelHardwareRoads { get; set; } = true;

        public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];

        public int MaxContextTokens { get; set; } = 4096;

        public int ModelTimeoutSeconds { get; set; } = 180;

        public string? OllamaKeepAlive { get; set; }

        public int? OllamaNumGpu { get; set; }

        public bool IncludeMemory { get; set; } = true;

        public bool SaveToMemory { get; set; } = true;

        public string? Title { get; set; }

        public Guid? ContinueConversationId { get; set; }

        public bool GenerateImplementationArtifact { get; set; }

        public bool UserConfirmedArtifactBuild { get; set; }

        public bool UseChangeReviewWorkflow { get; set; } = true;

        public Guid? ProjectId { get; set; }

        public Guid? ProjectTopicId { get; set; }

        public Guid? ProjectRevisionId { get; set; }

        public bool CreateProjectForRun { get; set; }

        public bool UserConfirmedProjectLink { get; set; }

        public bool UseOrganicCouncilWorkflow { get; set; }

        public string CouncilTeamKey { get; set; } = "general";

        public string CouncilLeaderModelName { get; set; } = string.Empty;

        public List<string> RequestedOrganicCapabilities { get; set; } = [];

        public string ExternalProjectContextJson { get; set; } = "{}";

        public string OneWireCorrelationId { get; set; } = string.Empty;

        [JsonIgnore]
        public Action<string>? ProgressMessage { get; set; }

        [JsonIgnore]
        public Action<string>? StreamUpdate { get; set; }

        [JsonIgnore]
        public Action<MultiModelCouncilStep>? StepCompleted { get; set; }
    }

    public sealed record MultiModelCouncilModelCandidate(
        string ModelName,
        string Provider,
        string Endpoint,
        bool IsInstalled,
        bool IsConfigured,
        bool IsLoaded,
        string? Details)
    {
        public string DisplayName => $"{ModelName} - {Provider}";
    }

    public sealed class MultiModelCouncilResult
    {
        public Guid RunId { get; set; } = Guid.NewGuid();

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }

        public string Prompt { get; set; } = string.Empty;

        public List<string> ModelNames { get; set; } = [];

        public Guid? ContinuedFromConversationId { get; set; }

        public string? ContinuedFromTitle { get; set; }

        public List<MultiModelCouncilStep> Steps { get; set; } = [];

        public string FinalAnswer { get; set; } = string.Empty;

        public CouncilUserPoll? UserPoll { get; set; }

        public Guid? MemoryConversationId { get; set; }

        public Guid? KnowledgeEntryId { get; set; }

        public Guid? ProjectId { get; set; }

        public Guid? ProjectTopicId { get; set; }

        public Guid? ProjectRevisionId { get; set; }

        public string? LogPath { get; set; }

        public List<CouncilArtifact> Artifacts { get; set; } = [];

        public CodeGenerationReviewSnapshot? ChangeReview { get; set; }

        public List<string> Warnings { get; set; } = [];

        public string CouncilTeamKey { get; set; } = "general";

        public string OneWireCorrelationId { get; set; } = string.Empty;
    }

    public sealed class CouncilArtifact
    {
        public string Name { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string DownloadUrl { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string QualityStatus { get; set; } = "Generated only";

        public string ContractStatus { get; set; } = "Not validated";

        public List<string> ContractChecks { get; set; } = [];

        public List<string> MissingRequirements { get; set; } = [];
    }

    public sealed class CouncilUserPoll
    {
        public string Question { get; set; } = string.Empty;

        public List<CouncilUserPollOption> Options { get; set; } = [];

        public string Reason { get; set; } = string.Empty;
    }

    public sealed class CouncilUserPollOption
    {
        public string Label { get; set; } = string.Empty;

        public string FollowUpPrompt { get; set; } = string.Empty;
    }

    public sealed class MultiModelCouncilStep
    {
        public int SortOrder { get; set; }

        public int Round { get; set; }

        public string Phase { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

        public List<string> CouncilMembers { get; set; } = [];

        public string Role { get; set; } = string.Empty;

        public string HardwareLane { get; set; } = string.Empty;

        public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;

        public int HardwareIndex { get; set; } = -1;

        public int EffectiveMaxOutputTokens { get; set; }

        public int EffectiveMaxContextTokens { get; set; }

        public string Content { get; set; } = string.Empty;

        public string VisibleContent { get; set; } = string.Empty;

        public string? Thinking { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

        public double DurationSeconds { get; set; }

        public string? Error { get; set; }

        [JsonIgnore]
        public string BrainPart => string.IsNullOrWhiteSpace(Role) ? Phase : Role;

        [JsonIgnore]
        public string Moment => $"Round {Round}: {Phase}";
    }
}
