using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Exposes regex provenance/review state used by security-sensitive ingestion and classification.</summary>
[ApiController]
[Route("api/regex-curator")]
public sealed class RegexCuratorController(IRegexCuratorService curator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegexCuratorEntry>>> List(CancellationToken cancellationToken) =>
        Ok(await curator.ListAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("review")]
    public async Task<ActionResult<RegexCuratorEntry>> Review([FromBody] RegexCuratorReviewRequest request, CancellationToken cancellationToken) =>
        Ok(await curator.ReviewAsync(request, cancellationToken).ConfigureAwait(false));

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
