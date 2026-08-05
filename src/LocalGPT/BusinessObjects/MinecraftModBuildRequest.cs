namespace LocalGPT.BusinessObjects
{
    public class MinecraftModBuildRequest
    {
        public string ProjectName { get; set; } = "LivingCities";
        public string ModId { get; set; } = "living_cities";
        public string PackageName { get; set; } = "com.localgpt.livingcities";
        public string MinecraftVersion { get; set; } = "26.1";
        public string Loader { get; set; } = "Fabric";
        public string JavaVersion { get; set; } = "25";
        public string GradleVersion { get; set; } = "8.14.2";
        public string Ide { get; set; } = "Eclipse";
        public bool IncludeLivingCitiesStarter { get; set; } = true;
        public string Description { get; set; } = string.Empty;
    }
}
