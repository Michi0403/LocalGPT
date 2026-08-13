using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Generic escape hatch for explicitly advertised organic capabilities. Dedicated handlers below keep the
/// common spreadsheet workflow discoverable to every normal LocalGPT chat without requiring a Council run.
/// </summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the invoke organic plugin function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the invoke organic plugin function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the invoke organic plugin function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the invoke organic plugin function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InvokeOrganicPluginFunction(
    IOrganicDxFunctionSupport organicSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<InvokeOrganicPluginFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the invoke organic plugin function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InvokeOrganicPluginFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "organic.plugin.invoke",
        Method: "POST",
        Route: "/api/onewire/peers/{peerId}/invoke",
        Purpose: "Queues a capability on a user-connected organic plugin such as PublisherStudio. Use this for eyes, hands, OpenSCAD, spreadsheet or media functions advertised by that peer.",
        Parameters: "peerId, capabilityKey, payload, executionMode, workOrderKey, notBeforeUtc, interactionValue",
        SafetyNotes: "The target peer applies its per-organ permission rules. LocalGPT also requires human approval before sending the exact side-effecting request.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "OneWire",
        ParameterSchemaJson: """
        {"type":"object","required":["peerId","capabilityKey"],"properties":{"peerId":{"type":"string"},"capabilityKey":{"type":"string"},"payload":{"type":"object"},"executionMode":{"type":"string","enum":["Once","SequentialSpool","Scheduled","Recurring"]},"workOrderKey":{"type":"string"},"notBeforeUtc":{"type":"string","format":"date-time"},"interactionValue":{"description":"Optional bidirectional JSON information for the receiving system."}}}
        """,
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="InvokeOrganicPluginFunction"/>, keeping the operation consistent with the state and invariants of the surrounding invoke organic plugin function workflow.
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
            var peerId = organicSupport.GetString(request.Parameters, "peerId");
            var capabilityKey = organicSupport.GetString(request.Parameters, "capabilityKey");
            if (string.IsNullOrWhiteSpace(peerId) || string.IsNullOrWhiteSpace(capabilityKey))
                return organicSupport.Invalid("peerId and capabilityKey are required.");
            var peer = peers.GetPeer(peerId);
            if (peer is null || !connections.IsConnected(peerId))
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested organic plugin peer is not connected." };
            var capability = organicSupport.FindCapability(peer, capabilityKey);
            if (capability is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The connected peer did not advertise this capability as enabled and online." };

            var payload = request.Parameters.TryGetProperty("payload", out var payloadElement)
                ? payloadElement.Clone()
                : JsonSerializer.SerializeToElement(new { });
            var executionMode = Enum.TryParse<OneWireExecutionMode>(organicSupport.GetString(request.Parameters, "executionMode"), true, out var parsedMode)
                ? parsedMode
                : OneWireExecutionMode.Once;
            var envelope = organicSupport.CreateInvokeEnvelope(
                peerId,
                capability,
                payload,
                executionMode,
                organicSupport.GetString(request.Parameters, "workOrderKey"),
                DateTimeOffset.TryParse(organicSupport.GetString(request.Parameters, "notBeforeUtc"), out var notBefore) ? notBefore : null,
                request.UserConfirmed,
                request.Parameters.TryGetProperty("interactionValue", out var interaction) ? interaction.GetRawText() : payload.GetRawText());
            return await QueueAndSendAsync(envelope, cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(InvokeOrganicPluginFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(InvokeOrganicPluginFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queue and send for <see cref="InvokeOrganicPluginFunction"/>, keeping the operation consistent with the state and invariants of the surrounding invoke organic plugin function workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the invoke organic plugin function operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    private async Task<DxAiFunctionInvocationResult> QueueAndSendAsync(OneWireEnvelope envelope, CancellationToken cancellationToken)
    {
    try
    {
            var work = spooler.Enqueue(envelope);
            if (!await connections.SendAsync(envelope.TargetPeerId, envelope, cancellationToken).ConfigureAwait(false))
            {
                spooler.Fail(work.Id, "The organic plugin disconnected before the request was sent.");
                return new DxAiFunctionInvocationResult { Status = "Failed", Error = "The organic plugin disconnected before the request was sent." };
            }
            logger.LogInformation("Sent organic plugin work {WorkItemId} to {PeerId} for {CapabilityKey}.", work.Id, envelope.TargetPeerId, envelope.CapabilityKey);
            return organicSupport.Queued(work, envelope.TargetPeerId, envelope.CapabilityKey);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(InvokeOrganicPluginFunction)}.{nameof(QueueAndSendAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(InvokeOrganicPluginFunction)}.{nameof(QueueAndSendAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Chat-level spreadsheet entry point. It intentionally does not require a Council run: a normal local model can
/// discover this function, ask for the active PublisherStudio session id, and request bounded read-only evidence.
/// </summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the inspect publisher spreadsheet function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the inspect publisher spreadsheet function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the inspect publisher spreadsheet function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the inspect publisher spreadsheet function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InspectPublisherSpreadsheetFunction(
    IOrganicDxFunctionSupport organicSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<InspectPublisherSpreadsheetFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="InspectPublisherSpreadsheetFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.spreadsheet.inspect";

    /// <summary>
    /// Gets the descriptor value that forms part of the inspect publisher spreadsheet function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InspectPublisherSpreadsheetFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.spreadsheet.inspect",
        Method: "POST",
        Route: "/api/onewire/peers/connected/spreadsheet/inspect",
        Purpose: "Requests bounded read-only workbook/session evidence from a connected PublisherStudio instance. This is available in ordinary LocalGPT chat as well as Council rounds.",
        Parameters: "sessionId, peerId, workOrderKey",
        SafetyNotes: "Workbook content remains controlled by PublisherStudio. Its per-capability permission policy may ask the user before returning the evidence.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "OneWire",
        ParameterSchemaJson: """
        {"type":"object","required":["sessionId"],"properties":{"sessionId":{"type":"string","format":"uuid"},"peerId":{"type":"string","description":"Optional connected PublisherStudio peer. The only matching peer is selected automatically when omitted."},"workOrderKey":{"type":"string"}}}
        """,
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="InspectPublisherSpreadsheetFunction"/>, keeping the operation consistent with the state and invariants of the surrounding inspect publisher spreadsheet function workflow.
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
            var sessionId = organicSupport.GetString(request.Parameters, "sessionId");
            if (!Guid.TryParse(sessionId, out _))
                return organicSupport.Invalid("A valid PublisherStudio spreadsheet sessionId is required.");

            var peerId = organicSupport.GetString(request.Parameters, "peerId");
            var matching = peers.GetPeers()
                .Where(peer => connections.IsConnected(peer.PeerId) && organicSupport.FindCapability(peer, CapabilityKey) is not null)
                .ToList();
            var peer = string.IsNullOrWhiteSpace(peerId)
                ? matching.Count == 1 ? matching[0] : null
                : matching.FirstOrDefault(candidate => string.Equals(candidate.PeerId, peerId, StringComparison.OrdinalIgnoreCase));
            if (peer is null)
            {
                var reason = matching.Count > 1
                    ? "More than one connected peer provides spreadsheet inspection; supply peerId."
                    : "No connected PublisherStudio peer currently advertises spreadsheet inspection.";
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = reason };
            }

            var capability = organicSupport.FindCapability(peer, CapabilityKey)!;
            var payload = JsonSerializer.SerializeToElement(new { sessionId });
            var envelope = organicSupport.CreateInvokeEnvelope(
                peer.PeerId,
                capability,
                payload,
                OneWireExecutionMode.SequentialSpool,
                organicSupport.GetString(request.Parameters, "workOrderKey", $"spreadsheet:{sessionId}"),
                null,
                request.UserConfirmed,
                JsonSerializer.Serialize(new { sessionId, Request = "Read-only spreadsheet evidence for LocalGPT chat." }));
            var work = spooler.Enqueue(envelope);
            if (!await connections.SendAsync(peer.PeerId, envelope, cancellationToken).ConfigureAwait(false))
            {
                spooler.Fail(work.Id, "PublisherStudio disconnected before the spreadsheet request was sent.");
                return new DxAiFunctionInvocationResult { Status = "Failed", Error = "PublisherStudio disconnected before the spreadsheet request was sent." };
            }
            logger.LogInformation("Queued read-only spreadsheet evidence request {WorkItemId} for peer {PeerId}.", work.Id, peer.PeerId);
            return organicSupport.Queued(work, peer.PeerId, CapabilityKey);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(InspectPublisherSpreadsheetFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(InspectPublisherSpreadsheetFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Creates a bounded, reviewable PublisherStudio text proposal from ordinary chat or a Council round.
/// The unique connected PublisherStudio peer is selected automatically when peerId is omitted.
/// </summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the propose publisher text function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the propose publisher text function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the propose publisher text function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the propose publisher text function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ProposePublisherTextFunction(
    IOrganicDxFunctionSupport organicSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<ProposePublisherTextFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="ProposePublisherTextFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.text.insert.propose";

    /// <summary>
    /// Gets the descriptor value that forms part of the propose publisher text function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ProposePublisherTextFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.text.proposal.request",
        Method: "POST",
        Route: "/api/onewire/peers/connected/text/propose",
        Purpose: "Sends a generated text as a reviewable insertion proposal to a connected PublisherStudio instance. It never inserts text automatically.",
        Parameters: "target, text, reason, peerId, workOrderKey",
        SafetyNotes: "Requires fresh LocalGPT approval and remains subject to PublisherStudio's per-capability permission rule. The final insertion is always performed by the user.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "OneWire",
        ParameterSchemaJson: """
        {"type":"object","required":["target","text"],"properties":{"target":{"type":"string"},"text":{"type":"string"},"reason":{"type":"string"},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}
        """,
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="ProposePublisherTextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding propose publisher text function workflow.
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
            var target = organicSupport.GetString(request.Parameters, "target");
            var text = organicSupport.GetString(request.Parameters, "text");
            var reason = organicSupport.GetString(request.Parameters, "reason", "Generated by a LocalGPT Council at the PublisherStudio user's request.");
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(text))
                return organicSupport.Invalid("target and text are required.");

            var requestedPeerId = organicSupport.GetString(request.Parameters, "peerId");
            var matching = peers.GetPeers()
                .Where(peer => connections.IsConnected(peer.PeerId) && organicSupport.FindCapability(peer, CapabilityKey) is not null)
                .ToList();
            var peer = string.IsNullOrWhiteSpace(requestedPeerId)
                ? matching.Count == 1 ? matching[0] : null
                : matching.FirstOrDefault(candidate => string.Equals(candidate.PeerId, requestedPeerId, StringComparison.OrdinalIgnoreCase));
            if (peer is null)
            {
                var error = matching.Count > 1
                    ? "More than one connected peer accepts text proposals; supply peerId."
                    : "No connected PublisherStudio peer currently advertises reviewable text proposals.";
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = error };
            }

            var capability = organicSupport.FindCapability(peer, CapabilityKey)!;
            var payload = JsonSerializer.SerializeToElement(new { target = target.Trim(), text, reason = reason.Trim() });
            var envelope = organicSupport.CreateInvokeEnvelope(
                peer.PeerId,
                capability,
                payload,
                OneWireExecutionMode.SequentialSpool,
                organicSupport.GetString(request.Parameters, "workOrderKey", $"text-proposal:{Guid.NewGuid():N}"),
                null,
                request.UserConfirmed,
                payload.GetRawText());
            var work = spooler.Enqueue(envelope);
            if (!await connections.SendAsync(peer.PeerId, envelope, cancellationToken).ConfigureAwait(false))
            {
                spooler.Fail(work.Id, "PublisherStudio disconnected before the text proposal was sent.");
                return new DxAiFunctionInvocationResult { Status = "Failed", Error = "PublisherStudio disconnected before the text proposal was sent." };
            }
            logger.LogInformation("Queued reviewable text proposal {WorkItemId} for PublisherStudio peer {PeerId}.", work.Id, peer.PeerId);
            return organicSupport.Queued(work, peer.PeerId, CapabilityKey);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProposePublisherTextFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProposePublisherTextFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>Reads the eventual result of a queued organic operation without reissuing it.</summary>
/// <param name="spooler">One wire work spooler dependency used by the read organic plugin work result function workflow to provide the corresponding application capability.</param>
/// <param name="organicSupport">Organic devexpress function support dependency used by the read organic plugin work result function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReadOrganicPluginWorkResultFunction(IOneWireWorkSpooler spooler, IOrganicDxFunctionSupport organicSupport, ILogger<ReadOrganicPluginWorkResultFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the read organic plugin work result function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReadOrganicPluginWorkResultFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        /// <summary>
        /// Stores the internal false state used by <see cref="ReadOrganicPluginWorkResultFunction"/> while executing its surrounding workflow.
        /// </summary>
        Name: "organic.plugin.work.read",
        Method: "GET",
        Route: "/api/onewire/work/{workItemId}",
        Purpose: "Reads status/result/error for a previously queued organic plugin operation such as spreadsheet inspection.",
        Parameters: "workItemId",
        SafetyNotes: "Read-only LocalGPT spool inspection.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "OneWire",
        ParameterSchemaJson: """{"type":"object","required":["workItemId"],"properties":{"workItemId":{"type":"string","format":"uuid"}}}""",
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: false,
        ApprovalRequiredBeforeCompletion: false);

    /// <summary>
    /// Performs invoke for <see cref="ReadOrganicPluginWorkResultFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read organic plugin work result function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            logger.LogInformation("Organic work-result read DXFunction started.");
            if (!Guid.TryParse(organicSupport.GetString(request.Parameters, "workItemId"), out var id))
                return Task.FromResult(organicSupport.Invalid("A valid workItemId is required."));
            var item = spooler.Get(id);
            return Task.FromResult(item is null
                ? new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The 1-Wire work item was not found or expired." }
                : new DxAiFunctionInvocationResult
                {
                    Succeeded = item.Status == OneWireWorkStatus.Completed,
                    Status = item.Status.ToString(),
                    Error = item.Error,
                    Value = new { item.Id, item.CorrelationId, item.CapabilityKey, item.Status, item.ResultJson, item.Error, item.CreatedUtc, item.UpdatedUtc }
                });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ReadOrganicPluginWorkResultFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ReadOrganicPluginWorkResultFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an organic DevExpress function support application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicDxFunctionSupport(ILogger<OrganicDxFunctionSupport> logger) : IOrganicDxFunctionSupport
{
    /// <summary>
    /// Retrieves string for <see cref="OrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetString(JsonElement element, string name, string fallback = "")
    {
    try
    {
            logger.LogTrace("Reading bounded organic DXFunction parameter {ParameterName}.", name);
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? fallback
                : fallback;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(GetString)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(GetString)} failed.");
        throw;
    }
}

    /// <summary>
    /// Finds capability for <see cref="OrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="peer">Peer value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The one wire capability descriptor produced by the operation.</returns>
    public OneWireCapabilityDescriptor? FindCapability(OneWirePeerAdvertisement peer, string key) {
    try
    {
        return peer.Capabilities.FirstOrDefault(item => item.IsEnabled && item.IsOnline && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(FindCapability)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(FindCapability)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates invoke envelope for <see cref="OrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capability">Capability value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="executionMode">Execution mode value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="workOrderKey">Work order key value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="notBeforeUtc">Not before utc value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="interactionValueJson">Interaction value json value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    public OneWireEnvelope CreateInvokeEnvelope(
        string peerId,
        OneWireCapabilityDescriptor capability,
        JsonElement payload,
        OneWireExecutionMode executionMode,
        string workOrderKey,
        DateTimeOffset? notBeforeUtc,
        bool userConfirmed,
        string? interactionValueJson)
    {
    try
    {
            var envelope = new OneWireEnvelope
            {
                MessageType = OneWireMessageType.Invoke,
                SourcePeerId = "localgpt",
                TargetPeerId = peerId,
                CapabilityKey = capability.Key,
                Controller = capability.Controller,
                Method = capability.Method,
                Route = capability.Route,
                Organs = capability.Organs.ToList(),
                Skills = capability.Skills.ToList(),
                ExecutionMode = executionMode,
                WorkOrderKey = workOrderKey,
                NotBeforeUtc = notBeforeUtc,
                UserConfirmed = userConfirmed,
                RequiresHumanInteractionOnTargetSystem = capability.RequiresHumanInteractionOnTargetSystem || capability.RequiresHumanConfirmation,
                RequiresAutomatedInteractionOnTargetSystem = capability.RequiresAutomatedInteractionOnTargetSystem,
                InteractionValueJson = interactionValueJson,
                InteractionValueContentType = "application/json",
                Properties = new Dictionary<string, JsonElement> { ["Parameters"] = payload }
            };
            envelope.NormalizeInteractionKind();
            return envelope;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(CreateInvokeEnvelope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(CreateInvokeEnvelope)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queued for <see cref="OrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="work">Work value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public DxAiFunctionInvocationResult Queued(OneWireWorkItem work, string peerId, string capabilityKey) {
    try
    {
        return new()
    {
        Succeeded = true,
        Status = "Queued",
        Value = new { WorkItemId = work.Id, work.CorrelationId, PeerId = peerId, CapabilityKey = capabilityKey, NextFunction = "organic.plugin.work.read" }
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(Queued)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(Queued)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs invalid for <see cref="OrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="error">Error value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public DxAiFunctionInvocationResult Invalid(string error) {
    try
    {
        return new() { Status = "InvalidParameters", Error = error };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(Invalid)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicDxFunctionSupport)}.{nameof(Invalid)} failed.");
        throw;
    }
}
}

/// <summary>Asks the PublisherStudio user for reviewed text and returns it through the queued work result.</summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the request publisher reviewed text function workflow to provide the corresponding application capability.</param>
/// <param name="publisherInteractionSupport">Publisher interaction devexpress support dependency used by the request publisher reviewed text function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the request publisher reviewed text function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the request publisher reviewed text function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the request publisher reviewed text function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RequestPublisherReviewedTextFunction(
    IOrganicDxFunctionSupport organicSupport,
    IPublisherInteractionDxSupport publisherInteractionSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<RequestPublisherReviewedTextFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="RequestPublisherReviewedTextFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.text.edit.request";

    /// <summary>
    /// Gets the descriptor value that forms part of the request publisher reviewed text function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RequestPublisherReviewedTextFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.text.feedback.request",
        Method: "POST",
        Route: "/api/onewire/peers/connected/text/review",
        Purpose: "Asks the connected PublisherStudio user for specific reviewed text. PublisherStudio opens its bounded text editor, saves the response, closes the editor and returns the exact text through the same 1-Wire correlation.",
        Parameters: "question, initialText, title, peerId, workOrderKey",
        SafetyNotes: "Requires a fresh LocalGPT approval and PublisherStudio frontend approval. Text is returned only to the exact queued request and must be read with organic.plugin.work.read.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "OneWire",
        ParameterSchemaJson: """{"type":"object","required":["question"],"properties":{"question":{"type":"string"},"initialText":{"type":"string"},"title":{"type":"string"},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}""",
        IsCoordinationOnly: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="RequestPublisherReviewedTextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding request publisher reviewed text function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return publisherInteractionSupport.QueueAsync(
            request, CapabilityKey, connections, peers, spooler, logger,
            parameters =>
            {
                var question = organicSupport.GetString(parameters, "question");
                if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("question is required.");
                return JsonSerializer.SerializeToElement(new
                {
                    title = organicSupport.GetString(parameters, "title", "LocalGPT Council text request"),
                    question,
                    initialText = organicSupport.GetString(parameters, "initialText")
                });
            }, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestPublisherReviewedTextFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestPublisherReviewedTextFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>Requests one fresh browser-mediated screenshot from PublisherStudio.</summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the request publisher screen capture function workflow to provide the corresponding application capability.</param>
/// <param name="publisherInteractionSupport">Publisher interaction devexpress support dependency used by the request publisher screen capture function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the request publisher screen capture function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the request publisher screen capture function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the request publisher screen capture function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RequestPublisherScreenCaptureFunction(
    IOrganicDxFunctionSupport organicSupport,
    IPublisherInteractionDxSupport publisherInteractionSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<RequestPublisherScreenCaptureFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="RequestPublisherScreenCaptureFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.screen.capture";
    /// <summary>
    /// Gets the descriptor value that forms part of the request publisher screen capture function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RequestPublisherScreenCaptureFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.screen.capture.request", Method: "POST", Route: "/api/onewire/peers/connected/screen/capture",
        Purpose: "Requests one user-selected PublisherStudio/browser screenshot for visual Council evidence.",
        Parameters: "reason, peerId, workOrderKey",
        SafetyNotes: "Always requires LocalGPT approval, PublisherStudio approval and the browser's current screen-selection prompt. Saved permission cannot bypass getDisplayMedia.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "OneWire", ParameterSchemaJson: """{"type":"object","properties":{"reason":{"type":"string"},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}""",
        IsCoordinationOnly: true, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="RequestPublisherScreenCaptureFunction"/>, keeping the operation consistent with the state and invariants of the surrounding request publisher screen capture function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return publisherInteractionSupport.QueueAsync(request, CapabilityKey, connections, peers, spooler, logger,
            parameters => JsonSerializer.SerializeToElement(new { reason = organicSupport.GetString(parameters, "reason", "Visual evidence requested by the AI Council.") }), cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestPublisherScreenCaptureFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestPublisherScreenCaptureFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>Requests one short browser-mediated screen recording from PublisherStudio.</summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the request publisher screen record function workflow to provide the corresponding application capability.</param>
/// <param name="publisherInteractionSupport">Publisher interaction devexpress support dependency used by the request publisher screen record function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the request publisher screen record function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the request publisher screen record function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the request publisher screen record function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RequestPublisherScreenRecordFunction(
    IOrganicDxFunctionSupport organicSupport,
    IPublisherInteractionDxSupport publisherInteractionSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<RequestPublisherScreenRecordFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="RequestPublisherScreenRecordFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.screen.record";
    /// <summary>
    /// Gets the descriptor value that forms part of the request publisher screen record function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RequestPublisherScreenRecordFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.screen.record.request", Method: "POST", Route: "/api/onewire/peers/connected/screen/record",
        Purpose: "Requests a short user-selected PublisherStudio/browser screen recording for temporal Council evidence.",
        Parameters: "reason, maximumSeconds, includeAudio, peerId, workOrderKey",
        SafetyNotes: "Always requires LocalGPT approval, PublisherStudio approval and a new browser screen-selection prompt. Recording is limited to 15 seconds and bounded by the 1-Wire message limit.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "OneWire", ParameterSchemaJson: """{"type":"object","properties":{"reason":{"type":"string"},"maximumSeconds":{"type":"integer","minimum":1,"maximum":15},"includeAudio":{"type":"boolean"},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}""",
        IsCoordinationOnly: true, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="RequestPublisherScreenRecordFunction"/>, keeping the operation consistent with the state and invariants of the surrounding request publisher screen record function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return publisherInteractionSupport.QueueAsync(request, CapabilityKey, connections, peers, spooler, logger,
            parameters => JsonSerializer.SerializeToElement(new
            {
                reason = organicSupport.GetString(parameters, "reason", "Temporal visual evidence requested by the AI Council."),
                maximumSeconds = parameters.TryGetProperty("maximumSeconds", out var seconds) && seconds.TryGetInt32(out var value) ? Math.Clamp(value, 1, 15) : 10,
                includeAudio = parameters.TryGetProperty("includeAudio", out var audio) && audio.ValueKind is JsonValueKind.True or JsonValueKind.False && audio.GetBoolean()
            }), cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestPublisherScreenRecordFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestPublisherScreenRecordFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>Requests bounded user-approved HTML/DIV/document content from PublisherStudio.</summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the request publisher website content function workflow to provide the corresponding application capability.</param>
/// <param name="publisherInteractionSupport">Publisher interaction devexpress support dependency used by the request publisher website content function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the request publisher website content function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the request publisher website content function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the request publisher website content function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RequestPublisherWebsiteContentFunction(
    IOrganicDxFunctionSupport organicSupport,
    IPublisherInteractionDxSupport publisherInteractionSupport,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<RequestPublisherWebsiteContentFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="RequestPublisherWebsiteContentFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.website.content.request";
    /// <summary>
    /// Gets the descriptor value that forms part of the request publisher website content function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RequestPublisherWebsiteContentFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.website.content.request", Method: "POST", Route: "/api/onewire/peers/connected/web-content/request",
        Purpose: "Asks PublisherStudio for bounded user-approved HTML, DIV or document content that can be shown in LocalGPT chat or reused by another organic add-on.",
        Parameters: "question, initialContent, format, sourceUrl, maximumCharacters, peerId, workOrderKey",
        SafetyNotes: "Requires fresh approval in both applications. PublisherStudio returns only the text explicitly reviewed in its frontend; no arbitrary URL is fetched automatically.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "OneWire", ParameterSchemaJson: """{"type":"object","required":["question"],"properties":{"question":{"type":"string"},"initialContent":{"type":"string"},"format":{"type":"string","enum":["html","div","text","document"]},"sourceUrl":{"type":"string"},"maximumCharacters":{"type":"integer","minimum":1000,"maximum":200000},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}""",
        IsCoordinationOnly: true, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="RequestPublisherWebsiteContentFunction"/>, keeping the operation consistent with the state and invariants of the surrounding request publisher website content function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return publisherInteractionSupport.QueueAsync(request, CapabilityKey, connections, peers, spooler, logger,
            parameters =>
            {
                var question = organicSupport.GetString(parameters, "question");
                if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("question is required.");
                return JsonSerializer.SerializeToElement(new
                {
                    question,
                    initialText = organicSupport.GetString(parameters, "initialContent"),
                    format = organicSupport.GetString(parameters, "format", "html"),
                    sourceUrl = organicSupport.GetString(parameters, "sourceUrl"),
                    maximumCharacters = parameters.TryGetProperty("maximumCharacters", out var maximum) && maximum.TryGetInt32(out var value) ? Math.Clamp(value, 1000, 200000) : 120000
                });
            }, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestPublisherWebsiteContentFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestPublisherWebsiteContentFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}


/// <summary>Requests PublisherStudio's future embedded wiring canvas for a LocalGPT board/pin draft.</summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the request publisher embedded wiring editor function workflow to provide the corresponding application capability.</param>
/// <param name="publisherInteractionSupport">Publisher interaction devexpress support dependency used by the request publisher embedded wiring editor function workflow to provide the corresponding application capability.</param>
/// <param name="embeddedCatalog">Embedded hardware catalog service dependency used by the request publisher embedded wiring editor function workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the request publisher embedded wiring editor function workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the request publisher embedded wiring editor function workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the request publisher embedded wiring editor function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RequestPublisherEmbeddedWiringEditorFunction(
    IOrganicDxFunctionSupport organicSupport,
    IPublisherInteractionDxSupport publisherInteractionSupport,
    IEmbeddedHardwareCatalogService embeddedCatalog,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<RequestPublisherEmbeddedWiringEditorFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Defines the capability key constant used by <see cref="RequestPublisherEmbeddedWiringEditorFunction"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string CapabilityKey = "publisher.embedded.wiring.edit.request";

    /// <summary>
    /// Gets the descriptor value that forms part of the request publisher embedded wiring editor function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RequestPublisherEmbeddedWiringEditorFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        Name: "publisher.embedded.wiring.edit.request",
        Method: "POST",
        Route: "/api/onewire/peers/connected/embedded/wiring/edit",
        Purpose: "Requests the connected PublisherStudio workbench to render and edit a canvas-neutral ESP32/Arduino board, pin and wiring draft with optional OpenSCAD part links and animated signal arrows.",
        Parameters: "boardProfileKey, draft, reason, peerId, workOrderKey",
        SafetyNotes: "Requires fresh approval in both applications. PublisherStudio may edit and return the draft, but LocalGPT must revalidate it before firmware artifact creation; no compile, flash or actuator action is implied.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        Source: "OneWire", ParameterSchemaJson: """{"type":"object","properties":{"boardProfileKey":{"type":"string"},"draft":{"type":"object"},"reason":{"type":"string"},"peerId":{"type":"string"},"workOrderKey":{"type":"string"}},"additionalProperties":false}""",
        IsCoordinationOnly: true, SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Performs invoke for <see cref="RequestPublisherEmbeddedWiringEditorFunction"/>, keeping the operation consistent with the state and invariants of the surrounding request publisher embedded wiring editor function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return publisherInteractionSupport.QueueAsync(request, CapabilityKey, connections, peers, spooler, logger,
            parameters => JsonSerializer.SerializeToElement(new
            {
                boardProfileKey = organicSupport.GetString(parameters, "boardProfileKey", "esp32-classic-generic"),
                draft = parameters.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.Object
                    ? draft.Clone()
                    : JsonSerializer.SerializeToElement(new { }),
                reason = organicSupport.GetString(parameters, "reason", "Review and edit the embedded board pin and wiring plan."),
                workbench = embeddedCatalog.GetPublisherWorkbenchContract()
            }), cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RequestPublisherEmbeddedWiringEditorFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RequestPublisherEmbeddedWiringEditorFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a publisher interaction DevExpress support application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="organicSupport">Organic devexpress function support dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
/// <param name="serviceLogger">Publisher interaction devexpress support dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
public sealed class PublisherInteractionDxSupport(
    IOrganicDxFunctionSupport organicSupport,
    ILogger<PublisherInteractionDxSupport> serviceLogger) : IPublisherInteractionDxSupport
{
    /// <summary>
    /// Performs queue for <see cref="PublisherInteractionDxSupport"/>, keeping the operation consistent with the state and invariants of the surrounding publisher interaction DevExpress support workflow.
    /// </summary>
    /// <typeparam name="TLogger">Type used for t logger values handled by <see cref="PublisherInteractionDxSupport"/>.</typeparam>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the publisher interaction DevExpress support operation and used when producing its result.</param>
    /// <param name="connections">One wire connection registry dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
    /// <param name="peers">One wire peer registry dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
    /// <param name="spooler">One wire work spooler dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="createPayload">Create payload value supplied to the publisher interaction DevExpress support operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> QueueAsync<TLogger>(
        DxAiFunctionInvocationRequest request,
        string capabilityKey,
        IOneWireConnectionRegistry connections,
        IOneWirePeerRegistry peers,
        IOneWireWorkSpooler spooler,
        ILogger<TLogger> logger,
        Func<JsonElement, JsonElement> createPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            serviceLogger.LogTrace("Queueing a Publisher Studio organic interaction for capability {CapabilityKey}; payload content was omitted.", capabilityKey);
            if (request.Parameters.ValueKind != JsonValueKind.Object)
                return organicSupport.Invalid("Parameters must be a JSON object.");
            var requestedPeerId = organicSupport.GetString(request.Parameters, "peerId");
            var matching = peers.GetPeers()
                .Where(peer => connections.IsConnected(peer.PeerId) && organicSupport.FindCapability(peer, capabilityKey) is not null)
                .ToList();
            var peer = string.IsNullOrWhiteSpace(requestedPeerId)
                ? matching.Count == 1 ? matching[0] : null
                : matching.FirstOrDefault(candidate => string.Equals(candidate.PeerId, requestedPeerId, StringComparison.OrdinalIgnoreCase));
            if (peer is null)
            {
                var error = matching.Count > 1
                    ? $"More than one connected peer advertises {capabilityKey}; supply peerId."
                    : $"No connected peer currently advertises {capabilityKey}.";
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = error };
            }

            var payload = createPayload(request.Parameters);
            var capability = organicSupport.FindCapability(peer, capabilityKey)!;
            var envelope = organicSupport.CreateInvokeEnvelope(
                peer.PeerId, capability, payload, OneWireExecutionMode.SequentialSpool,
                organicSupport.GetString(request.Parameters, "workOrderKey", $"{capabilityKey}:{Guid.NewGuid():N}"),
                null, request.UserConfirmed, payload.GetRawText());
            var work = spooler.Enqueue(envelope);
            if (!await connections.SendAsync(peer.PeerId, envelope, cancellationToken).ConfigureAwait(false))
            {
                spooler.Fail(work.Id, "PublisherStudio disconnected before the request was sent.");
                return new DxAiFunctionInvocationResult { Status = "Failed", Error = "PublisherStudio disconnected before the request was sent." };
            }
            logger.LogInformation("Queued PublisherStudio interaction {WorkItemId} for {CapabilityKey} on peer {PeerId}.", work.Id, capabilityKey, peer.PeerId);
            return organicSupport.Queued(work, peer.PeerId, capabilityKey);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Rejected invalid parameters for PublisherStudio capability {CapabilityKey}.", capabilityKey);
            return organicSupport.Invalid(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not queue PublisherStudio capability {CapabilityKey}.", capabilityKey);
            return new DxAiFunctionInvocationResult { Status = "Failed", Error = ex.Message };
        }
    }
}
