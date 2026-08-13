namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a build debug inventory application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class BuildDebugInventory
    {
        /// <summary>
        /// Gets or sets the captured at UTC associated with this build debug inventory state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The captured at UTC value exposed by <see cref="BuildDebugInventory"/>.</value>
        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether copied files applies to the build debug inventory state.
        /// </summary>
        /// <value>The copied files value exposed by <see cref="BuildDebugInventory"/>.</value>
        public bool CopiedFiles { get; set; }

        /// <summary>
        /// Gets or sets the artifact root value that forms part of the build debug inventory state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact root value exposed by <see cref="BuildDebugInventory"/>.</value>
        public string ArtifactRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the files collection maintained or exposed by this build debug inventory instance for downstream processing.
        /// </summary>
        /// <value>The files value exposed by <see cref="BuildDebugInventory"/>.</value>
        public List<BuildDebugFileSummary> Files { get; set; } = [];

        /// <summary>
        /// Gets or sets the warnings collection maintained or exposed by this build debug inventory instance for downstream processing.
        /// </summary>
        /// <value>The warnings value exposed by <see cref="BuildDebugInventory"/>.</value>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Gets a value indicating whether the operation succeeded applies to the build debug inventory state.
        /// </summary>
        /// <value>The succeeded value exposed by <see cref="BuildDebugInventory"/>.</value>
        public bool Succeeded => Warnings.Count == 0;
    }

    /// <summary>
    /// Represents a build debug file summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public sealed class BuildDebugFileSummary
    {
        /// <summary>
        /// Gets or sets the name value that forms part of the build debug file summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension value that forms part of the build debug file summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The extension value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source path used by this build debug file summary instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The source path value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the copied path used by this build debug file summary instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The copied path value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public string? CopiedPath { get; set; }

        /// <summary>
        /// Gets or sets the length that quantifies the associated build debug file summary data.
        /// </summary>
        /// <value>The length value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the last write UTC associated with this build debug file summary state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The last write UTC value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public DateTime LastWriteUtc { get; set; }

        /// <summary>
        /// Gets or sets the source area value that forms part of the build debug file summary state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The source area value exposed by <see cref="BuildDebugFileSummary"/>.</value>
        public string SourceArea { get; set; } = string.Empty;
    }
}
