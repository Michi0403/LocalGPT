namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a build debug inventory.
    /// </summary>
    public sealed class BuildDebugInventory
    {
        /// <summary>
        /// Gets or sets captured at UTC.
        /// </summary>
        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets copied files.
        /// </summary>
        public bool CopiedFiles { get; set; }

        /// <summary>
        /// Gets or sets artifact root.
        /// </summary>
        public string ArtifactRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets files.
        /// </summary>
        public List<BuildDebugFileSummary> Files { get; set; } = [];

        /// <summary>
        /// Gets or sets warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = [];

        /// <summary>
        /// Gets or sets succeeded.
        /// </summary>
        public bool Succeeded => Warnings.Count == 0;
    }

    /// <summary>
    /// Represents a build debug file summary.
    /// </summary>
    public sealed class BuildDebugFileSummary
    {
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets extension.
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets source path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets copied path.
        /// </summary>
        public string? CopiedPath { get; set; }

        /// <summary>
        /// Gets or sets length.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets last write UTC.
        /// </summary>
        public DateTime LastWriteUtc { get; set; }

        /// <summary>
        /// Gets or sets source area.
        /// </summary>
        public string SourceArea { get; set; } = string.Empty;
    }
}
