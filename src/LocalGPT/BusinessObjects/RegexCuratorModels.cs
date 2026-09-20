namespace LocalGPT.BusinessObjects;

/// <summary>One independent review of a reusable regex pattern.</summary>
public sealed class RegexCuratorReview
{
    public string ReviewerIdentity { get; set; } = string.Empty;
    public string ReviewerKind { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset ReviewedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Durable curator metadata for one database-backed regular-expression pattern.</summary>
public sealed class RegexCuratorEntry
{
    public string Name { get; set; } = string.Empty;
    public string Provenance { get; set; } = "LegacyOrSeed";
    public string UsageScope { get; set; } = "General";
    public string ReviewStatus { get; set; } = "Approved";
    public bool UserApproved { get; set; } = true;
    public int RequiredIndependentApprovals { get; set; } = 2;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<RegexCuratorReview> Reviews { get; set; } = [];
}

/// <summary>Review request for a reusable regex curator entry.</summary>
public sealed class RegexCuratorReviewRequest
{
    public string Name { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string Notes { get; set; } = string.Empty;
}
