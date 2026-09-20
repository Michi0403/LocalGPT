using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.WireProtocol;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller;

/// <summary>Prompt/UI-facing helpers for normal 1-Wire pairing, trust and trusted Council-host enrollment.</summary>
[ApiController]
[Route("api/onewire/team")]
public sealed class OneWireTeamController(
    IOneWireRuntimeSecurityService security,
    IOneWireCouncilHostEnrollmentService hosts) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken) => Ok(new
    {
        Security = await security.GetStatusAsync(cancellationToken).ConfigureAwait(false),
        TrustedPeers = await security.GetTrustedPeersAsync(cancellationToken).ConfigureAwait(false),
        CouncilHosts = await hosts.ListAsync(cancellationToken).ConfigureAwait(false)
    });

    [HttpPost("pairing-ticket")]
    public async Task<ActionResult<OneWirePairingTicket>> PairingTicket([FromQuery] int lifetimeMinutes = 15, CancellationToken cancellationToken = default) =>
        Ok(await security.CreatePairingTicketAsync(TimeSpan.FromMinutes(Math.Clamp(lifetimeMinutes, 2, 1440)), cancellationToken).ConfigureAwait(false));

    [HttpGet("trusted")]
    public async Task<ActionResult<IReadOnlyList<OneWireTrustedPeerDescriptor>>> Trusted(CancellationToken cancellationToken) =>
        Ok(await security.GetTrustedPeersAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("trust")]
    [HumanApprovalRequired(
        "onewire.trust.establish",
        "Establish 1-Wire trust",
        "Establish MFA-verified trust from the reviewed remote pairing ticket and local authenticator code.",
        "High",
        "1-Wire trust reviewer",
        requiredBeforeCompletion: true)]
    public async Task<IActionResult> Trust([FromBody] OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken) =>
        Ok(new { established = await security.EstablishTrustAsync(request, cancellationToken).ConfigureAwait(false) });

    [HttpDelete("trust/{peerId}")]
    [HumanApprovalRequired(
        "onewire.trust.revoke",
        "Revoke 1-Wire trust",
        "Remove the selected peer from the trusted 1-Wire runtime identity.",
        "High",
        "1-Wire trust reviewer",
        requiredBeforeCompletion: true)]
    public async Task<IActionResult> Revoke(string peerId, CancellationToken cancellationToken) =>
        Ok(new { revoked = await security.RevokeTrustAsync(peerId, cancellationToken).ConfigureAwait(false) });

    [HttpGet("hosts")]
    public async Task<ActionResult<IReadOnlyList<OneWireCouncilHostEnrollment>>> Hosts(CancellationToken cancellationToken) =>
        Ok(await hosts.ListAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("hosts/{peerId}")]
    [HumanApprovalRequired(
        "onewire.council-host.enroll",
        "Enroll trusted LocalGPT as Council host",
        "Enroll an already paired and currently trusted LocalGPT peer for Council/team work distribution; pairing is never bypassed.",
        "High",
        "Council host reviewer",
        requiredBeforeCompletion: true)]
    public async Task<ActionResult<OneWireCouncilHostEnrollment>> EnrollHost(string peerId, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        Ok(await hosts.EnrollAsync(peerId, userConfirmed, cancellationToken).ConfigureAwait(false));

    [HttpDelete("hosts/{peerId}")]
    [HumanApprovalRequired(
        "onewire.council-host.remove",
        "Remove Council host",
        "Remove the selected trusted LocalGPT from Council work-host enrollment without revoking its 1-Wire trust.",
        "High",
        "Council host reviewer",
        requiredBeforeCompletion: true)]
    public async Task<IActionResult> RemoveHost(string peerId, [FromQuery] bool userConfirmed, CancellationToken cancellationToken) =>
        Ok(new { removed = await hosts.RemoveAsync(peerId, userConfirmed, cancellationToken).ConfigureAwait(false) });
}
