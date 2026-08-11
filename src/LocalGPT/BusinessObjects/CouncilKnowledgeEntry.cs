using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a council knowledge entry.
    /// </summary>
    public class CouncilKnowledgeEntry
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Gets or sets created at UTC.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets updated at UTC.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets topic.
        /// </summary>
        public string Topic { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets scope.
        /// </summary>
        public string Scope { get; set; } = "AI Council";
        /// <summary>
        /// Gets or sets content.
        /// </summary>
        [Required]
        [Column(TypeName = "TEXT")]
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets source.
        /// </summary>
        public string Source { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets helpful sources.
        /// </summary>
        public string HelpfulSources { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets tags.
        /// </summary>
        public string Tags { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets confidence.
        /// </summary>
        public int Confidence { get; set; } = 60;
        /// <summary>
        /// Gets or sets verification status.
        /// </summary>
        public string VerificationStatus { get; set; } = "NeedsVerification";
        /// <summary>
        /// Gets or sets review status.
        /// </summary>
        public string ReviewStatus { get; set; } = "NeedsUserReview";
        /// <summary>
        /// Gets or sets expires at UTC.
        /// </summary>
        public DateTime? ExpiresAtUtc { get; set; }
        /// <summary>
        /// Gets or sets last verified at UTC.
        /// </summary>
        public DateTime? LastVerifiedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets last used at UTC.
        /// </summary>
        public DateTime? LastUsedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets superseded by knowledge identifier.
        /// </summary>
        public Guid? SupersededByKnowledgeId { get; set; }
        /// <summary>
        /// Gets or sets staleness reason.
        /// </summary>
        public string StalenessReason { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets staleness detected at UTC.
        /// </summary>
        public DateTime? StalenessDetectedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets staleness detected by.
        /// </summary>
        public string StalenessDetectedBy { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets source hash.
        /// </summary>
        public string SourceHash { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets source date UTC.
        /// </summary>
        public DateTime? SourceDateUtc { get; set; }
        /// <summary>
        /// Gets or sets is user approved.
        /// </summary>
        public bool IsUserApproved { get; set; }
        /// <summary>
        /// Gets or sets is pinned.
        /// </summary>
        public bool IsPinned { get; set; }
        /// <summary>
        /// Gets or sets is archived.
        /// </summary>
        public bool IsArchived { get; set; }
    }
}
