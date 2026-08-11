namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a learn base import request.
    /// </summary>
    public sealed class LearnBaseImportRequest
    {
        /// <summary>
        /// Gets or sets root path.
        /// </summary>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets max projects.
        /// </summary>
        public int MaxProjects { get; set; } = 40;

        /// <summary>
        /// Gets or sets save to knowledge.
        /// </summary>
        public bool SaveToKnowledge { get; set; } = true;

        /// <summary>
        /// Gets or sets file extensions.
        /// </summary>
        public List<string> FileExtensions { get; set; } = [];

        /// <summary>
        /// Gets or sets additional file extensions.
        /// </summary>
        public string AdditionalFileExtensions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets file include regex.
        /// </summary>
        public string FileIncludeRegex { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets file exclude regex.
        /// </summary>
        public string FileExcludeRegex { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets maximum file bytes.
        /// </summary>
        public int MaximumFileBytes { get; set; } = 1_048_576;

        /// <summary>
        /// Gets or sets import learning source manifests.
        /// </summary>
        public bool ImportLearningSourceManifests { get; set; } = true;

        /// <summary>
        /// Gets or sets import known documentation corpora.
        /// </summary>
        public bool ImportKnownDocumentationCorpora { get; set; } = true;

        /// <summary>
        /// Gets or sets import project architecture.
        /// </summary>
        public bool ImportProjectArchitecture { get; set; } = true;
    }

    /// <summary>
    /// Represents a learn base import result.
    /// </summary>
    public sealed class LearnBaseImportResult
    {
        /// <summary>
        /// Gets or sets root path.
        /// </summary>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets import mode.
        /// </summary>
        public string ImportMode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets file policy.
        /// </summary>
        public string FilePolicy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets duplicate policy.
        /// </summary>
        public string DuplicatePolicy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets project count.
        /// </summary>
        public int ProjectCount { get; set; }

        /// <summary>
        /// Gets or sets saved knowledge count.
        /// </summary>
        public int SavedKnowledgeCount { get; set; }

        /// <summary>
        /// Gets or sets projects.
        /// </summary>
        public List<LearnBaseProjectSummary> Projects { get; set; } = [];

        /// <summary>
        /// Gets or sets warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = [];
    }

    /// <summary>
    /// Represents a learn base project summary.
    /// </summary>
    public sealed class LearnBaseProjectSummary
    {
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets source path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets architecture.
        /// </summary>
        public string Architecture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets protocols and components.
        /// </summary>
        public string ProtocolsAndComponents { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets target frameworks.
        /// </summary>
        public string TargetFrameworks { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets package references.
        /// </summary>
        public string PackageReferences { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets important files.
        /// </summary>
        public string ImportantFiles { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets source file count.
        /// </summary>
        public int SourceFileCount { get; set; }

        /// <summary>
        /// Gets or sets binary file count.
        /// </summary>
        public int BinaryFileCount { get; set; }

        /// <summary>
        /// Gets or sets knowledge entry identifier.
        /// </summary>
        public Guid? KnowledgeEntryId { get; set; }
    }
}
