using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/onewire")]
public sealed class OneWireController(
    IOneWireCapabilityCatalog capabilities,
    IOneWirePeerRegistry peers,
    IOneWireConnectionRegistry connections,
    IOneWireWorkSpooler work,
    IOneWireEnvelopeCodec codec,
    IOneWireMessageDispatcher dispatcher,
    IOneWireTransportSecurityPolicy transportSecurityPolicy,
    IOneWireDispatchContextFactory dispatchContextFactory,
    IOneWireReplayPolicyDataService replayPolicy,
    IOrganicCouncilBlueprintService teams,
    ICouncilTeamConfigurationService teamConfigurations,
    ILogger<OneWireController> logger) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        Program.Port,
        Program.BaseUrl,
        Program.OneWirePort,
        Program.OneWireDiscoveryPort,
        Peers = peers.GetPeers().Count,
        ConnectedPeers = peers.GetPeers().Count(peer => connections.IsConnected(peer.PeerId)),
        PendingWork = work.GetSnapshot().Count(item => item.Status is OneWireWorkStatus.Queued or OneWireWorkStatus.Running or OneWireWorkStatus.PendingApproval)
    });


    [HttpGet("transport-policy")]
    public IActionResult TransportPolicy()
    {
        try
        {
            var remoteAddress = HttpContext.Connection.RemoteIpAddress;
            var loopback = transportSecurityPolicy.IsLoopback(remoteAddress);
            logger.LogDebug($"Returned 1-Wire transport policy for remote address {remoteAddress}.");
            return Ok(new
            {
                IsLoopback = loopback,
                RequiresProtectionForInvoke = transportSecurityPolicy.RequiresProtectedTransport(OneWireMessageType.Invoke),
                RequiresProtectionForCouncil = transportSecurityPolicy.RequiresProtectedTransport(OneWireMessageType.CouncilRequest)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the 1-Wire transport policy.");
            return Problem(ex.Message);
        }
    }


    [HttpGet("replay-policy")]
    public ActionResult<OneWireReplayPolicySnapshot> ReplayPolicy()
    {
        try
        {
            var snapshot = replayPolicy.GetSnapshot();
            logger.LogDebug($"Returned the configured 1-Wire replay policy.");
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the configured 1-Wire replay policy.");
            return Problem(ex.Message);
        }
    }

    [HttpGet("capabilities")]
    public async Task<ActionResult<IReadOnlyList<OneWireCapabilityDescriptor>>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await capabilities.GetLocalCapabilitiesAsync(cancellationToken).ConfigureAwait(false));

    [HttpGet("peers")]
    public ActionResult<IReadOnlyList<OneWirePeerAdvertisement>> Peers() => Ok(peers.GetPeers());

    [HttpGet("work")]
    public ActionResult<IReadOnlyList<OneWireWorkItem>> Work() => Ok(work.GetSnapshot());

    [HttpGet("work/{id:guid}")]
    public ActionResult<OneWireWorkItem> Work(Guid id) => work.Get(id) is { } item ? Ok(item) : NotFound();

    [HttpGet("council/teams")]
    public async Task<ActionResult<IReadOnlyList<OrganicCouncilTeamDefinition>>> CouncilTeams(CancellationToken cancellationToken) =>
        Ok(await teams.GetTeamsAsync(cancellationToken).ConfigureAwait(false));

    [HttpPost("council/teams")]
    [HumanApprovalRequired(
        "onewire.council-team.save",
        "Save AI Council team configuration",
        "Persist the reviewed team roles, prompts, workflow steps and architecture contracts without deleting seeded or user knowledge.",
        "High",
        "AI Council configuration reviewer")]
    public async Task<ActionResult<OrganicCouncilTeamDefinition>> SaveCouncilTeam(
        [FromBody] SaveCouncilTeamConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.UserConfirmed)
            return BadRequest(new { error = "Explicit user confirmation is required before a Council team configuration is saved." });
        try
        {
            return Ok(await teamConfigurations.SaveAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            logger.LogWarning(ex, "Council team configuration update was rejected for {TeamKey}.", request.Team?.Key);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("dispatch")]
    public async Task<IActionResult> Dispatch([FromBody] OneWireEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!transportSecurityPolicy.IsLoopback(HttpContext.Connection.RemoteIpAddress))
            return NotFound();

        envelope.SourcePeerId = "localgpt";
        var response = await dispatcher.DispatchAsync(envelope, dispatchContextFactory.CreateInternal("local-http-ui"), cancellationToken).ConfigureAwait(false);
        return response is null ? Accepted() : Ok(response);
    }

    [HttpPost("peers/{peerId}/invoke")]
    [HumanApprovalRequired(
        "onewire.peer.invoke",
        "Send organic plugin capability",
        "Send the exact reviewed capability request to the selected connected organic plugin peer.",
        "High",
        "Organic plugin action reviewer",
        requiredBeforeCompletion: true)]
    public async Task<IActionResult> InvokePeer(string peerId, [FromBody] OneWireEnvelope envelope, CancellationToken cancellationToken)
    {
        if (peers.GetPeer(peerId)?.IsConnected != true)
            return Conflict(new { error = "The selected organic application is not linked by both frontends." });
        envelope.SourcePeerId = "localgpt";
        envelope.TargetPeerId = peerId;
        envelope.MessageType = OneWireMessageType.Invoke;
        var queued = work.Enqueue(envelope);
        if (!await connections.SendAsync(peerId, envelope, cancellationToken).ConfigureAwait(false))
        {
            work.Fail(queued.Id, "Peer is not connected.");
            return Conflict(new { error = "Peer is not connected." });
        }
        logger.LogInformation("HTTP user queued 1-Wire capability {CapabilityKey} for peer {PeerId}.", envelope.CapabilityKey, peerId);
        return Accepted(new { WorkItemId = queued.Id, queued.CorrelationId });
    }

    [HttpPost("validate")]
    public IActionResult Validate([FromBody] OneWireEnvelope envelope) => codec.Validate(envelope, out var error)
        ? Ok(new { valid = true })
        : BadRequest(new { valid = false, error });
}

[ApiController]
[Route("api/projects/{projectId:guid}/organic-context")]
public sealed class ProjectOrganicContextController(
    IProjectOrganicContextService context,
    ILogger<ProjectOrganicContextController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectOrganicContext>> Get(Guid projectId, [FromQuery] Guid? revisionId, CancellationToken cancellationToken) =>
        Ok(await context.GetAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [HumanApprovalRequired(
        "project.organic-context.save",
        "Save project organic wiring",
        "Persist the exact reviewed installer, compiler, command, knowledge, RegEx, debug and organ-plugin metadata for this project revision.",
        "High",
        "Project architecture reviewer")]
    public async Task<ActionResult<ProjectOrganicContext>> Save(Guid projectId, [FromBody] SaveProjectOrganicContextRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await context.SaveAsync(projectId, request, cancellationToken).ConfigureAwait(false)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Organic project context update was rejected for {ProjectId}.", projectId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
