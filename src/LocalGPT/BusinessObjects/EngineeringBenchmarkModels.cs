namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the input contract for engineering benchmark, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    public sealed class EngineeringBenchmarkRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether run LocalGPT artifacts applies to the engineering benchmark state.
        /// </summary>
        /// <value>The run LocalGPT artifacts value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public bool RunLocalGptArtifacts { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether save to knowledge applies to the engineering benchmark state.
        /// </summary>
        /// <value>The save to knowledge value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public bool SaveToKnowledge { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether import learn base first applies to the engineering benchmark state.
        /// </summary>
        /// <value>The import learn base first value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public bool ImportLearnBaseFirst { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether validate buildable artifacts applies to the engineering benchmark state.
        /// </summary>
        /// <value>The validate buildable artifacts value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public bool ValidateBuildableArtifacts { get; set; }

        /// <summary>
        /// Gets or sets the max build artifacts value that forms part of the engineering benchmark state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max build artifacts value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public int MaxBuildArtifacts { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether user confirmed artifact actions applies to the engineering benchmark state.
        /// </summary>
        /// <value>The user confirmed artifact actions value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public bool UserConfirmedArtifactActions { get; set; }

        /// <summary>
        /// Gets or sets the task set value that forms part of the engineering benchmark state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The task set value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public string TaskSet { get; set; } = "engineering";

        /// <summary>
        /// Gets or sets the learn base root path used by this engineering benchmark instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The learn base root path value exposed by <see cref="EngineeringBenchmarkRequest"/>.</value>
        public string LearnBaseRootPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the outcome of engineering benchmark, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public sealed class EngineeringBenchmarkResult
    {
        /// <summary>
        /// Gets or sets the stable run identifier used to identify or correlate this engineering benchmark instance with related application state.
        /// </summary>
        /// <value>The run identifier value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public Guid RunId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the started at UTC associated with this engineering benchmark state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The started at UTC value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the completed at UTC associated with this engineering benchmark state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The completed at UTC value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public DateTime CompletedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the tasks collection maintained or exposed by this engineering benchmark instance for downstream processing.
        /// </summary>
        /// <value>The tasks value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public List<EngineeringBenchmarkTaskResult> Tasks { get; set; } = [];

        /// <summary>
        /// Gets or sets the stable knowledge entry identifier used to identify or correlate this engineering benchmark instance with related application state.
        /// </summary>
        /// <value>The knowledge entry identifier value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public Guid? KnowledgeEntryId { get; set; }

        /// <summary>
        /// Gets or sets the learn base import value that forms part of the engineering benchmark state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The learn base import value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public LearnBaseImportResult? LearnBaseImport { get; set; }

        /// <summary>
        /// Gets or sets the warnings collection maintained or exposed by this engineering benchmark instance for downstream processing.
        /// </summary>
        /// <value>The warnings value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Gets or sets the task set value that forms part of the engineering benchmark state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The task set value exposed by <see cref="EngineeringBenchmarkResult"/>.</value>
        public string TaskSet { get; set; } = "engineering";
    }

    /// <summary>
    /// Represents the outcome of engineering benchmark task, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public sealed class EngineeringBenchmarkTaskResult
    {
        /// <summary>
        /// Gets or sets the stable task identifier used to identify or correlate this engineering benchmark task instance with related application state.
        /// </summary>
        /// <value>The task identifier value exposed by <see cref="EngineeringBenchmarkTaskResult"/>.</value>
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name value that forms part of the engineering benchmark task state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="EngineeringBenchmarkTaskResult"/>.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt value that forms part of the engineering benchmark task state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The prompt value exposed by <see cref="EngineeringBenchmarkTaskResult"/>.</value>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the lanes collection maintained or exposed by this engineering benchmark task instance for downstream processing.
        /// </summary>
        /// <value>The lanes value exposed by <see cref="EngineeringBenchmarkTaskResult"/>.</value>
        public List<EngineeringBenchmarkLaneResult> Lanes { get; set; } = [];
    }

    /// <summary>
    /// Represents the outcome of engineering benchmark lane, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public sealed class EngineeringBenchmarkLaneResult
    {
        /// <summary>
        /// Gets or sets the lane value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The lane value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public string Lane { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The status value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public string Status { get; set; } = "NotRun";

        /// <summary>
        /// Gets or sets the valid architecture score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The valid architecture score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int ValidArchitectureScore { get; set; }

        /// <summary>
        /// Gets or sets the buildability score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The buildability score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int BuildabilityScore { get; set; }

        /// <summary>
        /// Gets or sets the missing files score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The missing files score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int MissingFilesScore { get; set; }

        /// <summary>
        /// Gets or sets the wrong packages templates score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The wrong packages templates score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int WrongPackagesTemplatesScore { get; set; }

        /// <summary>
        /// Gets or sets the time to usable output score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The time to usable output score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int TimeToUsableOutputScore { get; set; }

        /// <summary>
        /// Gets or sets the repair prompts score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The repair prompts score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int RepairPromptsScore { get; set; }

        /// <summary>
        /// Gets or sets the downloadable artifact score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The downloadable artifact score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int DownloadableArtifactScore { get; set; }

        /// <summary>
        /// Gets or sets the total score value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The total score value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int TotalScore { get; set; }

        /// <summary>
        /// Gets or sets the repair prompt count that quantifies the associated engineering benchmark lane data.
        /// </summary>
        /// <value>The repair prompt count value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public int RepairPromptCount { get; set; }

        /// <summary>
        /// Gets or sets the duration duration used to control timing in the engineering benchmark lane workflow.
        /// </summary>
        /// <value>The duration value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the artifacts collection maintained or exposed by this engineering benchmark lane instance for downstream processing.
        /// </summary>
        /// <value>The artifacts value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public List<CouncilArtifact> Artifacts { get; set; } = [];

        /// <summary>
        /// Gets or sets the evidence collection maintained or exposed by this engineering benchmark lane instance for downstream processing.
        /// </summary>
        /// <value>The evidence value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public List<string> Evidence { get; set; } = [];

        /// <summary>
        /// Gets or sets the missing files collection maintained or exposed by this engineering benchmark lane instance for downstream processing.
        /// </summary>
        /// <value>The missing files value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public List<string> MissingFiles { get; set; } = [];

        /// <summary>
        /// Gets or sets the build checks collection maintained or exposed by this engineering benchmark lane instance for downstream processing.
        /// </summary>
        /// <value>The build checks value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public List<EngineeringBenchmarkBuildCheck> BuildChecks { get; set; } = [];

        /// <summary>
        /// Gets or sets the notes value that forms part of the engineering benchmark lane state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The notes value exposed by <see cref="EngineeringBenchmarkLaneResult"/>.</value>
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an engineering benchmark build check application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class EngineeringBenchmarkBuildCheck
    {
        /// <summary>
        /// Gets or sets the artifact name value that forms part of the engineering benchmark build check state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact name value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public string ArtifactName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status value that forms part of the engineering benchmark build check state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The status value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extracted root value that forms part of the engineering benchmark build check state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The extracted root value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public string ExtractedRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the solution path used by this engineering benchmark build check instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The solution path value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public string SolutionPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exit code value that forms part of the engineering benchmark build check state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The exit code value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public int? ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the duration duration used to control timing in the engineering benchmark build check workflow.
        /// </summary>
        /// <value>The duration value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the output preview value that forms part of the engineering benchmark build check state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The output preview value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public string OutputPreview { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error preview value that forms part of the engineering benchmark build check state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The error preview value exposed by <see cref="EngineeringBenchmarkBuildCheck"/>.</value>
        public string ErrorPreview { get; set; } = string.Empty;
    }
}
