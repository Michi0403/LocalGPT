namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a minecraft mod build request.
    /// </summary>
    public class MinecraftModBuildRequest
    {
        /// <summary>
        /// Gets or sets project name.
        /// </summary>
        public string ProjectName { get; set; } = "LivingCities";
        /// <summary>
        /// Gets or sets mod identifier.
        /// </summary>
        public string ModId { get; set; } = "living_cities";
        /// <summary>
        /// Gets or sets package name.
        /// </summary>
        public string PackageName { get; set; } = "com.localgpt.livingcities";
        /// <summary>
        /// Gets or sets minecraft version.
        /// </summary>
        public string MinecraftVersion { get; set; } = "26.1";
        /// <summary>
        /// Gets or sets loader.
        /// </summary>
        public string Loader { get; set; } = "Fabric";
        /// <summary>
        /// Gets or sets java version.
        /// </summary>
        public string JavaVersion { get; set; } = "25";
        /// <summary>
        /// Gets or sets gradle version.
        /// </summary>
        public string GradleVersion { get; set; } = "8.14.2";
        /// <summary>
        /// Gets or sets ide.
        /// </summary>
        public string Ide { get; set; } = "Eclipse";
        /// <summary>
        /// Gets or sets include living cities starter.
        /// </summary>
        public bool IncludeLivingCitiesStarter { get; set; } = true;
        /// <summary>
        /// Gets or sets description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
