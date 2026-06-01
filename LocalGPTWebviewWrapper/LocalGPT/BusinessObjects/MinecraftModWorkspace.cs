namespace LocalGPT.BusinessObjects
{
    public class MinecraftModWorkspace
    {
        public string ProjectName { get; set; } = string.Empty;
        public string RootPath { get; set; } = string.Empty;
        public string MainClassPath { get; set; } = string.Empty;
        public string MetadataPath { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}
