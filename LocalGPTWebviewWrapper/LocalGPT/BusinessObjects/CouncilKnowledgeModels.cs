namespace LocalGPT.BusinessObjects
{
    public class CouncilKnowledgeEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Topic { get; set; } = string.Empty;
        public string Scope { get; set; } = "AI Council";
        public string Content { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string HelpfulSources { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public int Confidence { get; set; } = 60;
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
    }
}
