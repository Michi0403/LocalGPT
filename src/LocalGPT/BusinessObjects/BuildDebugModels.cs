namespace LocalGPT.BusinessObjects
{
    public sealed class BuildDebugInventory
    {
        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

        public bool CopiedFiles { get; set; }

        public string ArtifactRoot { get; set; } = string.Empty;

        public List<BuildDebugFileSummary> Files { get; set; } = [];

        public List<string> Warnings { get; set; } = [];

        public bool Succeeded => Warnings.Count == 0;
    }

    public sealed class BuildDebugFileSummary
    {
        public string Name { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string? CopiedPath { get; set; }

        public long Length { get; set; }

        public DateTime LastWriteUtc { get; set; }

        public string SourceArea { get; set; } = string.Empty;
    }
}
