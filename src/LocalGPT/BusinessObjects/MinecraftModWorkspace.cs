namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a minecraft mod workspace application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class MinecraftModWorkspace
    {
        /// <summary>
        /// Gets or sets the project name value that forms part of the minecraft mod workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The project name value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string ProjectName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the root path used by this minecraft mod workspace instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The root path value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string RootPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the main class path used by this minecraft mod workspace instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The main class path value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string MainClassPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the metadata path used by this minecraft mod workspace instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The metadata path value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string MetadataPath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the build file path used by this minecraft mod workspace instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The build file path value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string BuildFilePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the readme path used by this minecraft mod workspace instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The readme path value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string ReadmePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the build command value that forms part of the minecraft mod workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The build command value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string BuildCommand { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the eclipse import hint value that forms part of the minecraft mod workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The eclipse import hint value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public string EclipseImportHint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the created at value that forms part of the minecraft mod workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The created at value exposed by <see cref="MinecraftModWorkspace"/>.</value>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}
