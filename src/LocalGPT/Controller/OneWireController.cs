using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services.OneWire;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes the one wire application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="capabilities">One wire capability catalog dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="work">One wire work spooler dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="codec">One wire envelope codec dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="dispatcher">One wire message dispatcher dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="transportSecurityPolicy">One wire transport security policy dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="dispatchContextFactory">One wire dispatch context factory dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="replayPolicy">One wire replay policy data service dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="teams">Organic council blueprint service dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="teamConfigurations">Council team configuration service dependency used by the one wire workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// <summary>
    /// Returns the status projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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


    /// <summary>
    /// Runs the transport policy operation.
    /// </summary>
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


    /// <summary>
    /// Runs the replay policy operation.
    /// </summary>
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

    /// <summary>
    /// Runs the capabilities operation.
    /// </summary>
    [HttpGet("capabilities")]
    public async Task<ActionResult<IReadOnlyList<OneWireCapabilityDescriptor>>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await capabilities.GetLocalCapabilitiesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the peers projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("peers")]
    public ActionResult<IReadOnlyList<OneWirePeerAdvertisement>> Peers() => Ok(peers.GetPeers());

    /// <summary>
    /// Returns the work projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("work")]
    public ActionResult<IReadOnlyList<OneWireWorkItem>> Work() => Ok(work.GetSnapshot());

    /// <summary>
    /// Returns the work projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("work/{id:guid}")]
    public ActionResult<OneWireWorkItem> Work(Guid id) => work.Get(id) is { } item ? Ok(item) : NotFound();

    /// <summary>
    /// Returns the council teams projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("council/teams")]
    public async Task<ActionResult<IReadOnlyList<OrganicCouncilTeamDefinition>>> CouncilTeams(CancellationToken cancellationToken) =>
        Ok(await teams.GetTeamsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Persists council team for the one wire API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the dispatch projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("dispatch")]
    public async Task<IActionResult> Dispatch([FromBody] OneWireEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!transportSecurityPolicy.IsLoopback(HttpContext.Connection.RemoteIpAddress))
            return NotFound();

        envelope.SourcePeerId = "localgpt";
        var response = await dispatcher.DispatchAsync(envelope, dispatchContextFactory.CreateInternal("local-http-ui"), cancellationToken).ConfigureAwait(false);
        return response is null ? Accepted() : Ok(response);
    }

    /// <summary>
    /// Invokes peer for the one wire API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="envelope">Envelope value supplied to the one wire operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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

    /// <summary>
    /// Returns the validate projection for the one wire API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="error">Error value supplied to the one wire operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] OneWireEnvelope envelope) => codec.Validate(envelope, out var error)
        ? Ok(new { valid = true })
        : BadRequest(new { valid = false, error });
}

/// <summary>
/// Exposes the project organic context application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
/// </summary>
/// <param name="context">Project organic context service dependency used by the project organic context workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/projects/{projectId:guid}/organic-context")]
public sealed class ProjectOrganicContextController(
    IProjectOrganicContextService context,
    ILogger<ProjectOrganicContextController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the project organic context API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public async Task<ActionResult<ProjectOrganicContext>> Get(Guid projectId, [FromQuery] Guid? revisionId, CancellationToken cancellationToken) =>
        Ok(await context.GetAsync(projectId, revisionId, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the save projection for the project organic context API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
