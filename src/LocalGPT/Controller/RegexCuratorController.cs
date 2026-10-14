using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes regex provenance/review state used by security-sensitive ingestion and classification.</summary>
/// <param name="curator">Regex curator service dependency used by the regex curator workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/regex-curator")]
public sealed class RegexCuratorController(IRegexCuratorService curator) : ControllerBase
{
    /// <summary>
    /// Returns the list projection for the regex curator API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegexCuratorEntry>>> List(CancellationToken cancellationToken) =>
        Ok(await curator.ListAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the review projection for the regex curator API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("review")]
    [HumanApprovalRequired("regex.curator.review", "Review reusable regex", "Update provenance and independent review evidence for a reusable regex curator entry.", "Medium", "Regex curator", requiredBeforeCompletion: true)]
    public async Task<ActionResult<RegexCuratorEntry>> Review([FromBody] RegexCuratorReviewRequest request, CancellationToken cancellationToken) =>
        Ok(await curator.ReviewAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Sets approval for the regex curator API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="name">Name value supplied to the regex curator operation and used when producing its result.</param>
    /// <param name="approved">Value indicating whether approved should apply to this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{name}/approval")]
    [HumanApprovalRequired(
        "regex.curator.approve",
        "Approve reusable regex",
        "Approve or withdraw one reviewed regex for security-sensitive project ingestion and classification.",
        "High",
        "Regex curator",
        requiredBeforeCompletion: true)]
    public async Task<ActionResult<RegexCuratorEntry>> SetApproval(string name, [FromQuery] bool approved, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        Ok(await curator.SetUserApprovalAsync(name, approved, userConfirmed, cancellationToken).ConfigureAwait(false));
}
