namespace LocalGPT.BusinessObjects;

/// <summary>One independent review of a reusable regex pattern.</summary>
public sealed class RegexCuratorReview
{
    /// <summary>
    /// Gets or sets the reviewer identity value that forms part of the regex curator review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reviewer identity value exposed by <see cref="RegexCuratorReview"/>.</value>
    public string ReviewerIdentity { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reviewer kind value that forms part of the regex curator review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reviewer kind value exposed by <see cref="RegexCuratorReview"/>.</value>
    public string ReviewerKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the regex curator review state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="RegexCuratorReview"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the notes value that forms part of the regex curator review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="RegexCuratorReview"/>.</value>
    public string Notes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reviewed at UTC associated with this regex curator review state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The reviewed at UTC value exposed by <see cref="RegexCuratorReview"/>.</value>
    public DateTimeOffset ReviewedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Durable curator metadata for one database-backed regular-expression pattern.</summary>
public sealed class RegexCuratorEntry
{
    /// <summary>
    /// Gets or sets the name value that forms part of the regex curator state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provenance value that forms part of the regex curator state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provenance value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public string Provenance { get; set; } = "LegacyOrSeed";
    /// <summary>
    /// Gets or sets the usage scope value that forms part of the regex curator state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The usage scope value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public string UsageScope { get; set; } = "General";
    /// <summary>
    /// Gets or sets the review status value that forms part of the regex curator state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review status value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public string ReviewStatus { get; set; } = "Approved";
    /// <summary>
    /// Gets or sets a value indicating whether user approved applies to the regex curator state.
    /// </summary>
    /// <value>The user approved value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public bool UserApproved { get; set; } = true;
    /// <summary>
    /// Gets or sets the required independent approvals value that forms part of the regex curator state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The required independent approvals value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public int RequiredIndependentApprovals { get; set; } = 2;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this regex curator state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the reviews collection maintained or exposed by this regex curator instance for downstream processing.
    /// </summary>
    /// <value>The reviews value exposed by <see cref="RegexCuratorEntry"/>.</value>
    public List<RegexCuratorReview> Reviews { get; set; } = [];
}

/// <summary>Review request for a reusable regex curator entry.</summary>
public sealed class RegexCuratorReviewRequest
{
    /// <summary>
    /// Gets or sets the name value that forms part of the regex curator review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="RegexCuratorReviewRequest"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the regex curator review state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="RegexCuratorReviewRequest"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the notes value that forms part of the regex curator review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The notes value exposed by <see cref="RegexCuratorReviewRequest"/>.</value>
    public string Notes { get; set; } = string.Empty;
}
