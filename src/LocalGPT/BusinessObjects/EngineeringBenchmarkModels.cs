namespace LocalGPT.BusinessObjects
{
    public sealed class EngineeringBenchmarkRequest
    {
        public bool RunLocalGptArtifacts { get; set; } = true;

        public bool SaveToKnowledge { get; set; } = true;

        public bool ImportLearnBaseFirst { get; set; }

        public bool ValidateBuildableArtifacts { get; set; }

        public int MaxBuildArtifacts { get; set; } = 3;

        public bool UserConfirmedArtifactActions { get; set; }

        public string TaskSet { get; set; } = "engineering";

        public string LearnBaseRootPath { get; set; } = string.Empty;
    }

    public sealed class EngineeringBenchmarkResult
    {
        public Guid RunId { get; set; } = Guid.NewGuid();

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CompletedAtUtc { get; set; }

        public List<EngineeringBenchmarkTaskResult> Tasks { get; set; } = [];

        public Guid? KnowledgeEntryId { get; set; }

        public LearnBaseImportResult? LearnBaseImport { get; set; }

        public List<string> Warnings { get; set; } = [];

        public string TaskSet { get; set; } = "engineering";
    }

    public sealed class EngineeringBenchmarkTaskResult
    {
        public string TaskId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Prompt { get; set; } = string.Empty;

        public List<EngineeringBenchmarkLaneResult> Lanes { get; set; } = [];
    }

    public sealed class EngineeringBenchmarkLaneResult
    {
        public string Lane { get; set; } = string.Empty;

        public string Status { get; set; } = "NotRun";

        public int ValidArchitectureScore { get; set; }

        public int BuildabilityScore { get; set; }

        public int MissingFilesScore { get; set; }

        public int WrongPackagesTemplatesScore { get; set; }

        public int TimeToUsableOutputScore { get; set; }

        public int RepairPromptsScore { get; set; }

        public int DownloadableArtifactScore { get; set; }

        public int TotalScore { get; set; }

        public int RepairPromptCount { get; set; }

        public TimeSpan? Duration { get; set; }

        public List<CouncilArtifact> Artifacts { get; set; } = [];

        public List<string> Evidence { get; set; } = [];

        public List<string> MissingFiles { get; set; } = [];

        public List<EngineeringBenchmarkBuildCheck> BuildChecks { get; set; } = [];

        public string Notes { get; set; } = string.Empty;
    }

    public sealed class EngineeringBenchmarkBuildCheck
    {
        public string ArtifactName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string ExtractedRoot { get; set; } = string.Empty;

        public string SolutionPath { get; set; } = string.Empty;

        public int? ExitCode { get; set; }

        public TimeSpan? Duration { get; set; }

        public string OutputPreview { get; set; } = string.Empty;

        public string ErrorPreview { get; set; } = string.Empty;
    }
}
