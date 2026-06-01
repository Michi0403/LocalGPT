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

        public int MaxContextTokens { get; set; } = 4096;

        public int ModelTimeoutSeconds { get; set; } = 180;

        public string? OllamaKeepAlive { get; set; }

        public int? OllamaNumGpu { get; set; }

        public bool IncludeMemory { get; set; } = true;

        public bool SaveToMemory { get; set; } = true;

        public string? Title { get; set; }

        public Guid? ContinueConversationId { get; set; }

        public bool GenerateImplementationArtifact { get; set; }
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

        public string? LogPath { get; set; }

        public List<CouncilArtifact> Artifacts { get; set; } = [];

        public List<string> Warnings { get; set; } = [];
    }

    public sealed class CouncilArtifact
    {
        public string Name { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string DownloadUrl { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
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

        public string Content { get; set; } = string.Empty;

        public string VisibleContent { get; set; } = string.Empty;

        public string? Thinking { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

        public double DurationSeconds { get; set; }

        public string? Error { get; set; }
    }
}
