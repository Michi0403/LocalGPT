using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>HTTP entry points for the certificate service, using the same validated BusinessObject as the DXForm.</summary>
/// <param name="certificates">Canonical DI-owned certificate service.</param>
/// <param name="logger">Structured diagnostics without leaking private key material.</param>
[ApiController]
[Route("api/network/certificates")]
public sealed class NetworkCertificateController(INetworkCertificateService certificates, ILogger<NetworkCertificateController> logger) : ControllerBase
{
    /// <summary>Retrieves safe defaults for the local certificate editor; access is approval-gated because paths reveal host details.</summary>
    /// <returns>Default certificate request model.</returns>
    [HttpGet("defaults")]
    [HumanApprovalRequired("localgpt.certificate.defaults", "Inspect certificate defaults", "Read local host and certificate path defaults.", "Medium", "Certificate configuration", requiredBeforeCompletion: true)]
    public ActionResult<NetworkCertificateCreateRequest> Defaults()
    {
        try
        {
            return Ok(certificates.CreateDefaultRequest());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to read certificate-creation defaults.");
            return Problem("Certificate defaults are unavailable.");
        }
    }

    /// <summary>Issues a locally approved X.509 certificate from DataAnnotations-validated request values.</summary>
    /// <param name="request">Validated combined certificate purposes and output preferences.</param>
    /// <param name="cancellationToken">Cancels generation.</param>
    /// <returns>Issued certificate metadata or a structured validation problem.</returns>
    [HttpPost]
    [HumanApprovalRequired("localgpt.certificate.create", "Create network certificate", "Write a new local PFX and optionally install it into the certificate store.", "High", "Certificate configuration", requiredBeforeCompletion: true)]
    public async Task<ActionResult<NetworkCertificateCreateResult>> CreateAsync([FromBody] NetworkCertificateCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // [ApiController] returns ValidationProblemDetails (HTTP 400) for DataAnnotations errors.
            return Ok(await certificates.CreateAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "A certificate request was rejected after form/model validation.");
            ModelState.AddModelError(string.Empty, exception.Message);
            return ValidationProblem(ModelState);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Certificate generation failed.");
            return Problem("Certificate generation failed; check application diagnostics.");
        }
    }
}
