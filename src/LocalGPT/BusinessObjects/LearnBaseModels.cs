namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the input contract for learn base import, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    public sealed class LearnBaseImportRequest
    {
        /// <summary>
        /// Gets or sets the root path used by this learn base import instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The root path value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the max projects value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The max projects value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public int MaxProjects { get; set; } = 40;

        /// <summary>
        /// Gets or sets a value indicating whether save to knowledge applies to the learn base import state.
        /// </summary>
        /// <value>The save to knowledge value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public bool SaveToKnowledge { get; set; } = true;

        /// <summary>
        /// Gets or sets the file extensions collection maintained or exposed by this learn base import instance for downstream processing.
        /// </summary>
        /// <value>The file extensions value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public List<string> FileExtensions { get; set; } = [];

        /// <summary>
        /// Gets or sets the additional file extensions value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The additional file extensions value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public string AdditionalFileExtensions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file include regex value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The file include regex value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public string FileIncludeRegex { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file exclude regex value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The file exclude regex value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public string FileExcludeRegex { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum file bytes value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The maximum file bytes value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public int MaximumFileBytes { get; set; } = 1_048_576;

        /// <summary>
        /// Gets or sets a value indicating whether import learning source manifests applies to the learn base import state.
        /// </summary>
        /// <value>The import learning source manifests value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public bool ImportLearningSourceManifests { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether import known documentation corpora applies to the learn base import state.
        /// </summary>
        /// <value>The import known documentation corpora value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public bool ImportKnownDocumentationCorpora { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether import project architecture applies to the learn base import state.
        /// </summary>
        /// <value>The import project architecture value exposed by <see cref="LearnBaseImportRequest"/>.</value>
        public bool ImportProjectArchitecture { get; set; } = true;
    }

    /// <summary>
    /// Represents the outcome of learn base import, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    public sealed class LearnBaseImportResult
    {
        /// <summary>
        /// Gets or sets the root path used by this learn base import instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The root path value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the import mode value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The import mode value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public string ImportMode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file policy value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The file policy value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public string FilePolicy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duplicate policy value that forms part of the learn base import state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The duplicate policy value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public string DuplicatePolicy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project count that quantifies the associated learn base import data.
        /// </summary>
        /// <value>The project count value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public int ProjectCount { get; set; }

        /// <summary>
        /// Gets or sets the saved knowledge count that quantifies the associated learn base import data.
        /// </summary>
        /// <value>The saved knowledge count value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public int SavedKnowledgeCount { get; set; }

        /// <summary>
        /// Gets or sets the projects collection maintained or exposed by this learn base import instance for downstream processing.
        /// </summary>
        /// <value>The projects value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public List<LearnBaseProjectSummary> Projects { get; set; } = [];

        /// <summary>
        /// Gets or sets the warnings collection maintained or exposed by this learn base import instance for downstream processing.
        /// </summary>
        /// <value>The warnings value exposed by <see cref="LearnBaseImportResult"/>.</value>
        public List<string> Warnings { get; set; } = [];
    }

    /// <summary>
    /// Represents a learn base project summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class LearnBaseProjectSummary
    {
        /// <summary>
        /// Gets or sets the name value that forms part of the learn base project summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source path used by this learn base project summary instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The source path value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the architecture value that forms part of the learn base project summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The architecture value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string Architecture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the protocols and components value that forms part of the learn base project summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The protocols and components value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string ProtocolsAndComponents { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target frameworks value that forms part of the learn base project summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The target frameworks value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string TargetFrameworks { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the package references value that forms part of the learn base project summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The package references value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string PackageReferences { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the important files value that forms part of the learn base project summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The important files value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public string ImportantFiles { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source file count that quantifies the associated learn base project summary data.
        /// </summary>
        /// <value>The source file count value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public int SourceFileCount { get; set; }

        /// <summary>
        /// Gets or sets the binary file count that quantifies the associated learn base project summary data.
        /// </summary>
        /// <value>The binary file count value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public int BinaryFileCount { get; set; }

        /// <summary>
        /// Gets or sets the stable knowledge entry identifier used to identify or correlate this learn base project summary instance with related application state.
        /// </summary>
        /// <value>The knowledge entry identifier value exposed by <see cref="LearnBaseProjectSummary"/>.</value>
        public Guid? KnowledgeEntryId { get; set; }
    }
}
