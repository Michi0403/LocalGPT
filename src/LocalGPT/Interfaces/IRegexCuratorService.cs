using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Maintains provenance and independent-review state for reusable regex patterns.</summary>
public interface IRegexCuratorService
{
    /// <summary>
    /// Performs mark suggested as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="provenance">Provenance value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="usageScope">Usage scope value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task MarkSuggestedAsync(string name, string provenance, string usageScope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs review as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The regex curator entry produced by the operation.</returns>
    Task<RegexCuratorEntry> ReviewAsync(RegexCuratorReviewRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sets user approval as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="approved">Value indicating whether approved should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The regex curator entry produced by the operation.</returns>
    Task<RegexCuratorEntry> SetUserApprovalAsync(string name, bool approved, bool userConfirmed, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs list as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RegexCuratorEntry>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists approved patterns as part of the regex curator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<RegexPattern>> ListApprovedPatternsAsync(CancellationToken cancellationToken = default);
}
