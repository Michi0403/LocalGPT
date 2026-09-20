using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Maintains provenance and independent-review state for reusable regex patterns.</summary>
public interface IRegexCuratorService
{
    Task MarkSuggestedAsync(string name, string provenance, string usageScope, CancellationToken cancellationToken = default);
    Task<RegexCuratorEntry> ReviewAsync(RegexCuratorReviewRequest request, CancellationToken cancellationToken = default);
    Task<RegexCuratorEntry> SetUserApprovalAsync(string name, bool approved, bool userConfirmed, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegexCuratorEntry>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegexPattern>> ListApprovedPatternsAsync(CancellationToken cancellationToken = default);
}
