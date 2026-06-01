namespace LocalGPT.BusinessObjects
{
    public class MinecraftModBuildRequest
    {
        public string ProjectName { get; set; } = "GeneratedMinecraftMod";
        public string ModId { get; set; } = "generated_mod";
        public string PackageName { get; set; } = "com.localgpt.generatedmod";
        public string MinecraftVersion { get; set; } = "1.21.1";
        public string Loader { get; set; } = "Fabric";
        public string Description { get; set; } = string.Empty;
    }
}
