namespace LocalGPT.BusinessObjects
{
    public sealed class MultiModelCouncilRequest
    {
        public string Prompt { get; set; } = string.Empty;

        public List<string> ModelNames { get; set; } = [];

        public string? BaseUri { get; set; }

        public int MaxRounds { get; set; } = 2;

        public int MaxOutputTokens { get; set; } = 4096;

        public bool IncludeMemory { get; set; } = true;

        public bool SaveToMemory { get; set; } = true;

        public string? Title { get; set; }
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

        public List<MultiModelCouncilStep> Steps { get; set; } = [];

        public string FinalAnswer { get; set; } = string.Empty;

        public Guid? MemoryConversationId { get; set; }

        public string? LogPath { get; set; }

        public List<string> Warnings { get; set; } = [];
    }

    public sealed class MultiModelCouncilStep
    {
        public int SortOrder { get; set; }

        public int Round { get; set; }

        public string Phase { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;

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
