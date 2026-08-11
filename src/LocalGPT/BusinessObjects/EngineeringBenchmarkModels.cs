namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents an engineering benchmark request.
    /// </summary>
    public sealed class EngineeringBenchmarkRequest
    {
        /// <summary>
        /// Gets or sets run local gpt artifacts.
        /// </summary>
        public bool RunLocalGptArtifacts { get; set; } = true;

        /// <summary>
        /// Gets or sets save to knowledge.
        /// </summary>
        public bool SaveToKnowledge { get; set; } = true;

        /// <summary>
        /// Gets or sets import learn base first.
        /// </summary>
        public bool ImportLearnBaseFirst { get; set; }

        /// <summary>
        /// Gets or sets validate buildable artifacts.
        /// </summary>
        public bool ValidateBuildableArtifacts { get; set; }

        /// <summary>
        /// Gets or sets max build artifacts.
        /// </summary>
        public int MaxBuildArtifacts { get; set; } = 3;

        /// <summary>
        /// Gets or sets user confirmed artifact actions.
        /// </summary>
        public bool UserConfirmedArtifactActions { get; set; }

        /// <summary>
        /// Gets or sets task set.
        /// </summary>
        public string TaskSet { get; set; } = "engineering";

        /// <summary>
        /// Gets or sets learn base root path.
        /// </summary>
        public string LearnBaseRootPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an engineering benchmark result.
    /// </summary>
    public sealed class EngineeringBenchmarkResult
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
        public DateTime CompletedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets tasks.
        /// </summary>
        public List<EngineeringBenchmarkTaskResult> Tasks { get; set; } = [];

        /// <summary>
        /// Gets or sets knowledge entry identifier.
        /// </summary>
        public Guid? KnowledgeEntryId { get; set; }

        /// <summary>
        /// Gets or sets learn base import.
        /// </summary>
        public LearnBaseImportResult? LearnBaseImport { get; set; }

        /// <summary>
        /// Gets or sets warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Gets or sets task set.
        /// </summary>
        public string TaskSet { get; set; } = "engineering";
    }

    /// <summary>
    /// Represents an engineering benchmark task result.
    /// </summary>
    public sealed class EngineeringBenchmarkTaskResult
    {
        /// <summary>
        /// Gets or sets task identifier.
        /// </summary>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets prompt.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets lanes.
        /// </summary>
        public List<EngineeringBenchmarkLaneResult> Lanes { get; set; } = [];
    }

    /// <summary>
    /// Represents an engineering benchmark lane result.
    /// </summary>
    public sealed class EngineeringBenchmarkLaneResult
    {
        /// <summary>
        /// Gets or sets lane.
        /// </summary>
        public string Lane { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets status.
        /// </summary>
        public string Status { get; set; } = "NotRun";

        /// <summary>
        /// Gets or sets valid architecture score.
        /// </summary>
        public int ValidArchitectureScore { get; set; }

        /// <summary>
        /// Gets or sets buildability score.
        /// </summary>
        public int BuildabilityScore { get; set; }

        /// <summary>
        /// Gets or sets missing files score.
        /// </summary>
        public int MissingFilesScore { get; set; }

        /// <summary>
        /// Gets or sets wrong packages templates score.
        /// </summary>
        public int WrongPackagesTemplatesScore { get; set; }

        /// <summary>
        /// Gets or sets time to usable output score.
        /// </summary>
        public int TimeToUsableOutputScore { get; set; }

        /// <summary>
        /// Gets or sets repair prompts score.
        /// </summary>
        public int RepairPromptsScore { get; set; }

        /// <summary>
        /// Gets or sets downloadable artifact score.
        /// </summary>
        public int DownloadableArtifactScore { get; set; }

        /// <summary>
        /// Gets or sets total score.
        /// </summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// Gets or sets repair prompt count.
        /// </summary>
        public int RepairPromptCount { get; set; }

        /// <summary>
        /// Gets or sets duration.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets artifacts.
        /// </summary>
        public List<CouncilArtifact> Artifacts { get; set; } = [];

        /// <summary>
        /// Gets or sets evidence.
        /// </summary>
        public List<string> Evidence { get; set; } = [];

        /// <summary>
        /// Gets or sets missing files.
        /// </summary>
        public List<string> MissingFiles { get; set; } = [];

        /// <summary>
        /// Gets or sets build checks.
        /// </summary>
        public List<EngineeringBenchmarkBuildCheck> BuildChecks { get; set; } = [];

        /// <summary>
        /// Gets or sets notes.
        /// </summary>
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an engineering benchmark build check.
    /// </summary>
    public sealed class EngineeringBenchmarkBuildCheck
    {
        /// <summary>
        /// Gets or sets artifact name.
        /// </summary>
        public string ArtifactName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets extracted root.
        /// </summary>
        public string ExtractedRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets solution path.
        /// </summary>
        public string SolutionPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets exit code.
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// Gets or sets duration.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets output preview.
        /// </summary>
        public string OutputPreview { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets error preview.
        /// </summary>
        public string ErrorPreview { get; set; } = string.Empty;
    }
}
