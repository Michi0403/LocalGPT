using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Generic escape hatch for explicitly advertised organic capabilities. Dedicated handlers below keep the
/// common spreadsheet workflow discoverable to every normal LocalGPT chat without requiring a Council run.
/// </summary>
public sealed class InvokeOrganicPluginFunction(
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<InvokeOrganicPluginFunction> logger) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object)
            return OrganicDxFunctionSupport.Invalid("Parameters must be a JSON object.");
        var peerId = OrganicDxFunctionSupport.GetString(request.Parameters, "peerId");
        var capabilityKey = OrganicDxFunctionSupport.GetString(request.Parameters, "capabilityKey");
        if (string.IsNullOrWhiteSpace(peerId) || string.IsNullOrWhiteSpace(capabilityKey))
            return OrganicDxFunctionSupport.Invalid("peerId and capabilityKey are required.");
        var peer = peers.GetPeer(peerId);
        if (peer is null || !connections.IsConnected(peerId))
            return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested organic plugin peer is not connected." };
        var capability = OrganicDxFunctionSupport.FindCapability(peer, capabilityKey);
        if (capability is null)
            return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The connected peer did not advertise this capability as enabled and online." };

        var payload = request.Parameters.TryGetProperty("payload", out var payloadElement)
            ? payloadElement.Clone()
            : JsonSerializer.SerializeToElement(new { });
        var executionMode = Enum.TryParse<OneWireExecutionMode>(OrganicDxFunctionSupport.GetString(request.Parameters, "executionMode"), true, out var parsedMode)
            ? parsedMode
            : OneWireExecutionMode.Once;
        var envelope = OrganicDxFunctionSupport.CreateInvokeEnvelope(
            peerId,
            capability,
            payload,
            executionMode,
            OrganicDxFunctionSupport.GetString(request.Parameters, "workOrderKey"),
            DateTimeOffset.TryParse(OrganicDxFunctionSupport.GetString(request.Parameters, "notBeforeUtc"), out var notBefore) ? notBefore : null,
            request.UserConfirmed,
            request.Parameters.TryGetProperty("interactionValue", out var interaction) ? interaction.GetRawText() : payload.GetRawText());
        return await QueueAndSendAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DxAiFunctionInvocationResult> QueueAndSendAsync(OneWireEnvelope envelope, CancellationToken cancellationToken)
    {
        var work = spooler.Enqueue(envelope);
        if (!await connections.SendAsync(envelope.TargetPeerId, envelope, cancellationToken).ConfigureAwait(false))
        {
            spooler.Fail(work.Id, "The organic plugin disconnected before the request was sent.");
            return new DxAiFunctionInvocationResult { Status = "Failed", Error = "The organic plugin disconnected before the request was sent." };
        }
        logger.LogInformation("Sent organic plugin work {WorkItemId} to {PeerId} for {CapabilityKey}.", work.Id, envelope.TargetPeerId, envelope.CapabilityKey);
        return OrganicDxFunctionSupport.Queued(work, envelope.TargetPeerId, envelope.CapabilityKey);
    }
}

/// <summary>
/// Chat-level spreadsheet entry point. It intentionally does not require a Council run: a normal local model can
/// discover this function, ask for the active PublisherStudio session id, and request bounded read-only evidence.
/// </summary>
public sealed class InspectPublisherSpreadsheetFunction(
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<InspectPublisherSpreadsheetFunction> logger) : IDxAiFunctionHandler
{
    private const string CapabilityKey = "publisher.spreadsheet.inspect";

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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object)
            return OrganicDxFunctionSupport.Invalid("Parameters must be a JSON object.");
        var sessionId = OrganicDxFunctionSupport.GetString(request.Parameters, "sessionId");
        if (!Guid.TryParse(sessionId, out _))
            return OrganicDxFunctionSupport.Invalid("A valid PublisherStudio spreadsheet sessionId is required.");

        var peerId = OrganicDxFunctionSupport.GetString(request.Parameters, "peerId");
        var matching = peers.GetPeers()
            .Where(peer => connections.IsConnected(peer.PeerId) && OrganicDxFunctionSupport.FindCapability(peer, CapabilityKey) is not null)
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

        var capability = OrganicDxFunctionSupport.FindCapability(peer, CapabilityKey)!;
        var payload = JsonSerializer.SerializeToElement(new { sessionId });
        var envelope = OrganicDxFunctionSupport.CreateInvokeEnvelope(
            peer.PeerId,
            capability,
            payload,
            OneWireExecutionMode.SequentialSpool,
            OrganicDxFunctionSupport.GetString(request.Parameters, "workOrderKey", $"spreadsheet:{sessionId}"),
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
        return OrganicDxFunctionSupport.Queued(work, peer.PeerId, CapabilityKey);
    }
}

/// <summary>
/// Creates a bounded, reviewable PublisherStudio text proposal from ordinary chat or a Council round.
/// The unique connected PublisherStudio peer is selected automatically when peerId is omitted.
/// </summary>
public sealed class ProposePublisherTextFunction(
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    ILogger<ProposePublisherTextFunction> logger) : IDxAiFunctionHandler
{
    private const string CapabilityKey = "publisher.text.insert.propose";

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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Parameters.ValueKind != JsonValueKind.Object)
            return OrganicDxFunctionSupport.Invalid("Parameters must be a JSON object.");
        var target = OrganicDxFunctionSupport.GetString(request.Parameters, "target");
        var text = OrganicDxFunctionSupport.GetString(request.Parameters, "text");
        var reason = OrganicDxFunctionSupport.GetString(request.Parameters, "reason", "Generated by a LocalGPT Council at the PublisherStudio user's request.");
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(text))
            return OrganicDxFunctionSupport.Invalid("target and text are required.");

        var requestedPeerId = OrganicDxFunctionSupport.GetString(request.Parameters, "peerId");
        var matching = peers.GetPeers()
            .Where(peer => connections.IsConnected(peer.PeerId) && OrganicDxFunctionSupport.FindCapability(peer, CapabilityKey) is not null)
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

        var capability = OrganicDxFunctionSupport.FindCapability(peer, CapabilityKey)!;
        var payload = JsonSerializer.SerializeToElement(new { target = target.Trim(), text, reason = reason.Trim() });
        var envelope = OrganicDxFunctionSupport.CreateInvokeEnvelope(
            peer.PeerId,
            capability,
            payload,
            OneWireExecutionMode.SequentialSpool,
            OrganicDxFunctionSupport.GetString(request.Parameters, "workOrderKey", $"text-proposal:{Guid.NewGuid():N}"),
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
        return OrganicDxFunctionSupport.Queued(work, peer.PeerId, CapabilityKey);
    }
}

/// <summary>Reads the eventual result of a queued organic operation without reissuing it.</summary>
public sealed class ReadOrganicPluginWorkResultFunction(IOneWireWorkSpooler spooler) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
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

    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(OrganicDxFunctionSupport.GetString(request.Parameters, "workItemId"), out var id))
            return Task.FromResult(OrganicDxFunctionSupport.Invalid("A valid workItemId is required."));
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
}

internal static class OrganicDxFunctionSupport
{
    public static string GetString(JsonElement element, string name, string fallback = "") =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;

    public static OneWireCapabilityDescriptor? FindCapability(OneWirePeerAdvertisement peer, string key) =>
        peer.Capabilities.FirstOrDefault(item => item.IsEnabled && item.IsOnline && string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    public static OneWireEnvelope CreateInvokeEnvelope(
        string peerId,
        OneWireCapabilityDescriptor capability,
        JsonElement payload,
        OneWireExecutionMode executionMode,
        string workOrderKey,
        DateTimeOffset? notBeforeUtc,
        bool userConfirmed,
        string? interactionValueJson)
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

    public static DxAiFunctionInvocationResult Queued(OneWireWorkItem work, string peerId, string capabilityKey) => new()
    {
        Succeeded = true,
        Status = "Queued",
        Value = new { WorkItemId = work.Id, work.CorrelationId, PeerId = peerId, CapabilityKey = capabilityKey, NextFunction = "organic.plugin.work.read" }
    };

    public static DxAiFunctionInvocationResult Invalid(string error) => new() { Status = "InvalidParameters", Error = error };
}
