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
        public string VerificationStatus { get; set; } = "NeedsVerification";
        public string ReviewStatus { get; set; } = "NeedsUserReview";
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? LastVerifiedAtUtc { get; set; }
        public DateTime? LastUsedAtUtc { get; set; }
        public Guid? SupersededByKnowledgeId { get; set; }
        public string StalenessReason { get; set; } = string.Empty;
        public DateTime? StalenessDetectedAtUtc { get; set; }
        public string StalenessDetectedBy { get; set; } = string.Empty;
        public string SourceHash { get; set; } = string.Empty;
        public DateTime? SourceDateUtc { get; set; }
        public bool IsUserApproved { get; set; }
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
    }
}
