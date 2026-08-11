namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a minecraft mod workspace.
    /// </summary>
    public class MinecraftModWorkspace
    {
        /// <summary>
        /// Gets or sets project name.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets root path.
        /// </summary>
        public string RootPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets main class path.
        /// </summary>
        public string MainClassPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets metadata path.
        /// </summary>
        public string MetadataPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets build file path.
        /// </summary>
        public string BuildFilePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets readme path.
        /// </summary>
        public string ReadmePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets build command.
        /// </summary>
        public string BuildCommand { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets eclipse import hint.
        /// </summary>
        public string EclipseImportHint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets created at.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}
