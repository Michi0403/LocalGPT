using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Chat-level, read-only lookup of the PublisherStudio editor sessions and their spreadsheet/text objects. It exists so a
/// model never has to ask the human for a session id: it calls this function and then reads the queued result.
/// The unique connected PublisherStudio peer is selected automatically when peerId is omitted.
/// </summary>
/// <param name="organicSupport">Organic devexpress function support dependency used to find the advertised capability and build the request envelope.</param>
/// <param name="connections">One wire connection registry dependency used to select a connected peer and send the request.</param>
/// <param name="peers">One wire peer registry dependency used to enumerate known peers.</param>
/// <param name="spooler">One wire work spooler dependency used to record the queued request.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListPublisherSessionsFunction(
    IOrganicDxFunctionSupport organicSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<ListPublisherSessionsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="ListPublisherSessionsFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.sessions.list";

    /// <summary>
    /// Gets the descriptor value that forms part of the list publisher sessions function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListPublisherSessionsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.sessions.list",
        Method: "POST",
        Route: "/api/onewire/peers/connected/sessions/list",
        Purpose: "Lists the open PublisherStudio editor sessions of the connected PublisherStudio: session id, publication title, page count and the spreadsheet and text objects (id, kind, title, page). Call this instead of asking the user for a session id or object id. The result arrives through organic.plugin.work.read with the returned workItemId.",
        Parameters: "peerId, workOrderKey",
        SafetyNotes: "Read-only. Returns only titles and ids, never document content. PublisherStudio applies its own per-capability permission rule.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "OneWire",
        ParameterSchemaJson: """
        {"type":"object","properties":{"peerId":{"type":"string","description":"Optional connected PublisherStudio peer. The only matching peer is selected automatically when omitted."},"workOrderKey":{"type":"string"}},"additionalProperties":false}
        """,
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: false,
        ApprovalRequiredBeforeCompletion: false);

    /// <summary>
    /// Queues the read-only session lookup on the selected PublisherStudio peer.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object && request.Parameters.ValueKind != JsonValueKind.Undefined && request.Parameters.ValueKind != JsonValueKind.Null)
                return organicSupport.Invalid("Parameters must be a JSON object.");
            var requestedPeerId = organicSupport.GetString(request.Parameters, "peerId");
            var matching = peers.GetPeers()
                .Where(peer => connections.IsConnected(peer.PeerId) && organicSupport.FindCapability(peer, CapabilityKey) is not null)
                .ToList();
            var peer = string.IsNullOrWhiteSpace(requestedPeerId)
                ? matching.Count == 1 ? matching[0] : null
                : matching.FirstOrDefault(candidate => string.Equals(candidate.PeerId, requestedPeerId, StringComparison.OrdinalIgnoreCase));
            if (peer is null)
            {
                var reason = matching.Count > 1
                    ? "More than one connected peer lists sessions; supply peerId."
                    : "No connected PublisherStudio peer currently advertises session listing. Connect PublisherStudio first.";
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = reason };
            }

            var capability = organicSupport.FindCapability(peer, CapabilityKey)!;
            var payload = JsonSerializer.SerializeToElement(new { });
            var envelope = organicSupport.CreateInvokeEnvelope(
                peer.PeerId,
                capability,
                payload,
                OneWireExecutionMode.SequentialSpool,
                organicSupport.GetString(request.Parameters, "workOrderKey", $"sessions-list:{Guid.NewGuid():N}"),
                null,
                request.UserConfirmed,
                payload.GetRawText());
            var work = spooler.Enqueue(envelope);
            if (!await connections.SendAsync(peer.PeerId, envelope, cancellationToken).ConfigureAwait(false))
            {
                spooler.Fail(work.Id, "PublisherStudio disconnected before the session list request was sent.");
                return new DxAiFunctionInvocationResult { Status = "Failed", Error = "PublisherStudio disconnected before the session list request was sent." };
            }
            logger.LogInformation("Queued read-only PublisherStudio session list request {WorkItemId} for peer {PeerId}.", work.Id, peer.PeerId);
            return organicSupport.Queued(work, peer.PeerId, CapabilityKey);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListPublisherSessionsFunction)}.{nameof(InvokeAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(ListPublisherSessionsFunction)}.{nameof(InvokeAsync)} failed.");
            throw;
        }
    }
}
