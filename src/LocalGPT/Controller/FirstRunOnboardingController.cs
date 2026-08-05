using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the bounded first-run setup status and completion operation to LocalGPT frontends.
/// </summary>
/// <param name="onboarding">Builds and persists the first-run guide.</param>
[ApiController]
[Route("api/onboarding")]
[DocumentationUpdated("2.1.20")]
public sealed class FirstRunOnboardingController(IFirstRunOnboardingService onboarding) : ControllerBase
{
    /// <summary>
    /// Returns installer profiles, discovered local models, seeded councils and quick-start routes.
    /// </summary>
    /// <param name="refreshConnectivity">When true, performs a bounded loopback provider discovery.</param>
    /// <param name="cancellationToken">Cancels the asynchronous request.</param>
    /// <returns>An action result containing the current onboarding status.</returns>
    [HttpGet("status")]
    public async Task<ActionResult<FirstRunOnboardingStatus>> GetStatus(
        [FromQuery] bool refreshConnectivity = false,
        CancellationToken cancellationToken = default) =>
        Ok(await onboarding.GetStatusAsync(refreshConnectivity, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Records that the user reviewed or dismissed the first-run guide.
    /// </summary>
    /// <param name="request">Contains the required current confirmation flag.</param>
    /// <param name="cancellationToken">Cancels the asynchronous persistence operation.</param>
    /// <returns>An action result indicating successful completion.</returns>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromBody] CompleteFirstRunOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        await onboarding.CompleteAsync(request.UserConfirmed, cancellationToken).ConfigureAwait(false);
        return Ok(new { completed = true });
    }
}

/// <summary>
/// Carries the explicit confirmation required to dismiss first-run onboarding.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class CompleteFirstRunOnboardingRequest
{
    /// <summary>Gets or sets whether the current user explicitly confirmed completion.</summary>
    public bool UserConfirmed { get; set; }
}
