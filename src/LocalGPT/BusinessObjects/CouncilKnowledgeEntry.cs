using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents council knowledge state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    public class CouncilKnowledgeEntry
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this council knowledge instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Gets or sets the created at UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The created at UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the updated at UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The updated at UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the topic value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The topic value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string Topic { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the scope value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The scope value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string Scope { get; set; } = "AI Council";
        /// <summary>
        /// Gets or sets the content value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The content value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        [Required]
        [Column(TypeName = "TEXT")]
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the source value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The source value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string Source { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the helpful sources value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The helpful sources value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string HelpfulSources { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the tags value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The tags value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string Tags { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the confidence value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The confidence value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public int Confidence { get; set; } = 60;
        /// <summary>
        /// Gets or sets the verification status value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The verification status value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string VerificationStatus { get; set; } = "NeedsVerification";
        /// <summary>
        /// Gets or sets the review status value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The review status value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string ReviewStatus { get; set; } = "NeedsUserReview";
        /// <summary>
        /// Gets or sets the expires at UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The expires at UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime? ExpiresAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the last verified at UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The last verified at UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime? LastVerifiedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the last used at UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The last used at UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime? LastUsedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the stable superseded by knowledge identifier used to identify or correlate this council knowledge instance with related application state.
        /// </summary>
        /// <value>The superseded by knowledge identifier value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public Guid? SupersededByKnowledgeId { get; set; }
        /// <summary>
        /// Gets or sets the staleness reason value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The staleness reason value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string StalenessReason { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the staleness detected at UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The staleness detected at UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime? StalenessDetectedAtUtc { get; set; }
        /// <summary>
        /// Gets or sets the staleness detected by value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The staleness detected by value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string StalenessDetectedBy { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the source hash value that forms part of the council knowledge state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The source hash value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public string SourceHash { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the source date UTC associated with this council knowledge state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The source date UTC value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public DateTime? SourceDateUtc { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether user approved applies to the council knowledge state.
        /// </summary>
        /// <value>The is user approved value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public bool IsUserApproved { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether pinned applies to the council knowledge state.
        /// </summary>
        /// <value>The is pinned value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public bool IsPinned { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether archived applies to the council knowledge state.
        /// </summary>
        /// <value>The is archived value exposed by <see cref="CouncilKnowledgeEntry"/>.</value>
        public bool IsArchived { get; set; }

        /// <summary>
        /// Gets a compact human-readable identity for selection controls where topic text alone is not unique.
        /// </summary>
        /// <value>The topic and scope, followed by a short stable identifier when available.</value>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                var topic = string.IsNullOrWhiteSpace(Topic) ? "Untitled knowledge" : Topic.Trim();
                var scope = string.IsNullOrWhiteSpace(Scope) ? "Unscoped" : Scope.Trim();
                var compactTopic = topic.Length <= 92 ? topic : $"{topic[..89]}...";
                var compactId = Id == Guid.Empty ? "unsaved" : Id.ToString("N")[..8];
                return $"{compactTopic} · {scope} · {compactId}";
            }
        }

        /// <summary>
        /// Gets or sets the project-topic relationships that make this knowledge entry available to explicit project scopes.
        /// </summary>
        /// <value>The project-topic relationships associated with this knowledge entry.</value>
        public ICollection<LocalGptProjectTopicKnowledgeLink> ProjectTopicLinks { get; set; } = [];

        /// <summary>
        /// Gets or sets the regex relationships that give this knowledge entry structured recognition semantics.
        /// </summary>
        /// <value>The regex relationships associated with this knowledge entry.</value>
        public ICollection<CouncilKnowledgeRegexPatternLink> RegexPatternLinks { get; set; } = [];

        /// <summary>
        /// Gets or sets explicit human ratings associated with this knowledge entry.
        /// </summary>
        /// <value>The human knowledge ratings associated with this entry.</value>
        public ICollection<CouncilKnowledgeUserRating> UserRatings { get; set; } = [];
    }
}
