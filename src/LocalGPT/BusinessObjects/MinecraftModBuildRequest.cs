namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents the input contract for minecraft mod build, carrying the values a caller supplies to the corresponding application operation.
    /// </summary>
    public class MinecraftModBuildRequest
    {
        /// <summary>
        /// Gets or sets the project name value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The project name value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string ProjectName { get; set; } = "LivingCities";
        /// <summary>
        /// Gets or sets the stable mod identifier used to identify or correlate this minecraft mod build instance with related application state.
        /// </summary>
        /// <value>The mod identifier value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string ModId { get; set; } = "living_cities";
        /// <summary>
        /// Gets or sets the package name value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The package name value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string PackageName { get; set; } = "com.localgpt.livingcities";
        /// <summary>
        /// Gets or sets the minecraft version value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The minecraft version value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string MinecraftVersion { get; set; } = "26.1";
        /// <summary>
        /// Gets or sets the loader value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The loader value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string Loader { get; set; } = "Fabric";
        /// <summary>
        /// Gets or sets the java version value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The java version value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string JavaVersion { get; set; } = "25";
        /// <summary>
        /// Gets or sets the gradle version value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The gradle version value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string GradleVersion { get; set; } = "8.14.2";
        /// <summary>
        /// Gets or sets the ide value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The ide value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string Ide { get; set; } = "Eclipse";
        /// <summary>
        /// Gets or sets a value indicating whether living cities starter applies to the minecraft mod build state.
        /// </summary>
        /// <value>The include living cities starter value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public bool IncludeLivingCitiesStarter { get; set; } = true;
        /// <summary>
        /// Gets or sets the description value that forms part of the minecraft mod build state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The description value exposed by <see cref="MinecraftModBuildRequest"/>.</value>
        public string Description { get; set; } = string.Empty;
    }
}
