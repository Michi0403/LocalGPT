using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Requests a NEW spreadsheet or text document in the connected PublisherStudio. The request waits for LocalGPT approval and
/// then for PublisherStudio's own approval; nothing is created before both happened. The unique connected PublisherStudio
/// peer is selected automatically when peerId is omitted.
/// </summary>
/// <param name="organicSupport">Organic devexpress function support dependency used to find the advertised capability and build the request envelope.</param>
/// <param name="connections">One wire connection registry dependency used to select a connected peer and send the request.</param>
/// <param name="peers">One wire peer registry dependency used to enumerate known peers.</param>
/// <param name="spooler">One wire work spooler dependency used to record the queued request.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CreatePublisherDocumentFunction(
    IOrganicDxFunctionSupport organicSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<CreatePublisherDocumentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="CreatePublisherDocumentFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.document.create";

    /// <summary>
    /// Gets the descriptor value that forms part of the create publisher document function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="CreatePublisherDocumentFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.document.create.request",
        Method: "POST",
        Route: "/api/onewire/peers/connected/documents/create",
        Purpose: "Creates an approved PublisherStudio document and returns a download in Chat. Use kind spreadsheet (XLSX) or csv with CSV content; for documents send plain text with kind text (DOCX), rtf, html or txt. Omit sessionId when there is one editor. If the peer reports multiple editors, call publisher.sessions.list. Read the result through organic.plugin.work.read and use its downloadUrl.",
        Parameters: "kind, title, content, presentation, page, sessionId, peerId, workOrderKey",
        SafetyNotes: "Requires fresh LocalGPT approval and a fresh PublisherStudio approval for every request. Content is validated by PublisherStudio before anything is inserted; the insertion is undoable.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "OneWire",
        ParameterSchemaJson: """
        {"type":"object","required":["kind","content"],"properties":{"kind":{"type":"string","enum":["spreadsheet","text","rtf","html","txt","csv"]},"title":{"type":"string","maxLength":120},"content":{"type":"string","description":"CSV for spreadsheet and csv; plain text for every document kind."},"presentation":{"type":"string","enum":["visible","silent"]},"page":{"type":"integer","minimum":1},"sessionId":{"type":"string","description":"Only when several PublisherStudio editors are open; take it from publisher.sessions.list."},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}
        """,
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Validates the request and queues the document creation on the selected PublisherStudio peer.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Parameters.ValueKind != JsonValueKind.Object)
                return organicSupport.Invalid("Parameters must be a JSON object.");
            var kind = organicSupport.GetString(request.Parameters, "kind").Trim().ToLowerInvariant();
            var content = organicSupport.GetString(request.Parameters, "content");
            if (kind is not ("spreadsheet" or "text" or "rtf" or "html" or "txt" or "csv"))
                return organicSupport.Invalid("kind must be 'spreadsheet', 'text' (Word), 'rtf', 'html', 'txt', or 'csv'.");
            if (string.IsNullOrWhiteSpace(content))
                return organicSupport.Invalid("content is required (CSV text for a spreadsheet, plain text for a text document).");
            var presentation = organicSupport.GetString(request.Parameters, "presentation", "visible").Trim().ToLowerInvariant();
            if (presentation is not ("visible" or "silent"))
                return organicSupport.Invalid("presentation must be 'visible' or 'silent'.");
            var title = organicSupport.GetString(request.Parameters, "title").Trim();
            var sessionId = organicSupport.GetString(request.Parameters, "sessionId").Trim();
            if (sessionId.Length > 0 && !Guid.TryParse(sessionId, out _))
                return organicSupport.Invalid("sessionId must be an id returned by publisher.sessions.list; omit it when only one PublisherStudio editor is open.");
            int? page = request.Parameters.TryGetProperty("page", out var pageElement) && pageElement.ValueKind == JsonValueKind.Number && pageElement.TryGetInt32(out var pageNumber) && pageNumber >= 1
                ? pageNumber
                : null;

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
                    ? "More than one connected peer can create documents; supply peerId."
                    : "No connected PublisherStudio peer currently advertises document creation. Connect PublisherStudio first.";
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = reason };
            }

            var capability = organicSupport.FindCapability(peer, CapabilityKey)!;
            var payload = JsonSerializer.SerializeToElement(new
            {
                kind,
                title,
                content,
                presentation,
                page,
                sessionId = sessionId.Length == 0 ? null : sessionId
            });
            var envelope = organicSupport.CreateInvokeEnvelope(
                peer.PeerId,
                capability,
                payload,
                OneWireExecutionMode.SequentialSpool,
                organicSupport.GetString(request.Parameters, "workOrderKey", $"document-create:{Guid.NewGuid():N}"),
                null,
                request.UserConfirmed,
                payload.GetRawText());
            var work = spooler.Enqueue(envelope, request.ConversationId);
            if (!await connections.SendAsync(peer.PeerId, envelope, cancellationToken).ConfigureAwait(false))
            {
                spooler.Fail(work.Id, "PublisherStudio disconnected before the document creation request was sent.");
                return new DxAiFunctionInvocationResult { Status = "Failed", Error = "PublisherStudio disconnected before the document creation request was sent." };
            }
            logger.LogInformation("Queued {Kind} document creation request {WorkItemId} for PublisherStudio peer {PeerId}.", kind, work.Id, peer.PeerId);
            return organicSupport.Queued(work, peer.PeerId, CapabilityKey);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(CreatePublisherDocumentFunction)}.{nameof(InvokeAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(CreatePublisherDocumentFunction)}.{nameof(InvokeAsync)} failed.");
            throw;
        }
    }
}
