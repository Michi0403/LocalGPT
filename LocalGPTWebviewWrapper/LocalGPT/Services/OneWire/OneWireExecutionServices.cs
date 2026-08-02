using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

public sealed class OneWireOperationExecutor(
    IServiceScopeFactory scopeFactory,
    IOneWireEnvelopeCodec codec,
    ILocalGptVocabularyService vocabulary,
    ILogger<OneWireOperationExecutor> logger) : IOneWireOperationExecutor
{

    public async Task<string> ExecuteAsync(OneWireWorkItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await using var scope = scopeFactory.CreateAsyncScope();
        if (item.RequestType == OneWireMessageType.CouncilRequest || string.Equals(item.CapabilityKey, "council.run", StringComparison.OrdinalIgnoreCase))
        {
            var wireRequest = ReadPayload<OneWireCouncilRequest>(item.Request, "CouncilRequest");
            var council = scope.ServiceProvider.GetRequiredService<IMultiModelCouncilService>();
            var request = new MultiModelCouncilRequest
            {
                Prompt = wireRequest.Prompt,
                CouncilTeamKey = wireRequest.TeamKey,
                CouncilLeaderModelName = wireRequest.LeaderModelName,
                ModelNames = wireRequest.ModelNames,
                RequestedOrganicCapabilities = wireRequest.RequestedOrganicCapabilities,
                ExternalProjectContextJson = wireRequest.ExternalProjectContextJson,
                OneWireCorrelationId = item.CorrelationId.ToString("D"),
                UseOrganicCouncilWorkflow = true,
                ProjectId = wireRequest.ProjectId,
                ProjectTopicId = wireRequest.ProjectTopicId,
                ProjectRevisionId = wireRequest.ProjectRevisionId,
                MaxRounds = wireRequest.MaxRounds,
                MaxOutputTokens = wireRequest.MaxOutputTokens,
                MaxContextTokens = wireRequest.MaxContextTokens,
                MaxParallelModels = wireRequest.MaxParallelModels,
                ModelRoutes = wireRequest.ModelRoutes,
                ResourceLoadPercent = Math.Clamp(wireRequest.ResourceLoadPercent, 0, 100),
                AllowParallelHardwareRoads = wireRequest.AllowParallelHardwareRoads,
                IncludeMemory = wireRequest.IncludeMemory,
                SaveToMemory = wireRequest.SaveToMemory,
                GenerateImplementationArtifact = wireRequest.GenerateImplementationArtifact,
                UserConfirmedArtifactBuild = wireRequest.UserConfirmedArtifactBuild
            };
            var result = await council.RunAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(result, codec.JsonOptions);
        }

        if (string.Equals(item.CapabilityKey, "localgpt.screenreader.help", StringComparison.OrdinalIgnoreCase))
        {
            var parametersInner = item.Request.Properties is not null && item.Request.Properties.TryGetValue("Parameters", out var parameterElementOuter)
                ? parameterElementOuter.Clone()
                : JsonSerializer.SerializeToElement(new { }, codec.JsonOptions);
            var userPrompt = GetString(parametersInner, "prompt", "Describe meaningful screen changes and suggest the next safe action.");
            var selector = GetString(parametersInner, "selector", "body");
            var dataIncluded = GetBoolean(parametersInner, "dataIncluded") || GetBoolean(parametersInner, "DataIncluded");
            var width = GetInt(parametersInner, "pixelWidth", GetInt(parametersInner, "PixelWidth", 0));
            var height = GetInt(parametersInner, "pixelHeight", GetInt(parametersInner, "PixelHeight", 0));
            var council = scope.ServiceProvider.GetRequiredService<IMultiModelCouncilService>();
            var request = new MultiModelCouncilRequest
            {
                Prompt = $"""
Recurring screen-reader evidence arrived from an explicitly connected organic plugin.
User instruction: {userPrompt}
Captured selector: {selector}
Image metadata: {width}x{height}; inline image data included: {dataIncluded}.
Analyze only the supplied evidence. Report meaningful changes, uncertainties, and one safe next action. Do not claim visual details that cannot be derived by the configured model.
""",
                CouncilTeamKey = "general",
                ExternalProjectContextJson = parametersInner.GetRawText(),
                OneWireCorrelationId = item.CorrelationId.ToString("D"),
                UseOrganicCouncilWorkflow = true,
                MaxRounds = 1,
                MaxParallelModels = 1,
                MaxOutputTokens = 1200,
                MaxContextTokens = 16384,
                AllowParallelHardwareRoads = false,
                IncludeMemory = false,
                SaveToMemory = false,
                GenerateImplementationArtifact = false,
                UserConfirmedArtifactBuild = false
            };
            var result = await council.RunAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                result.FinalAnswer,
                result.RunId,
                result.CompletedAtUtc,
                item.CorrelationId,
                Evidence = new { selector, width, height, dataIncluded }
            }, codec.JsonOptions);
        }

        var parameters = item.Request.Properties is not null && item.Request.Properties.TryGetValue("Parameters", out var parameterElement)
            ? parameterElement.Clone()
            : JsonSerializer.SerializeToElement(new { }, codec.JsonOptions);
        if (string.Equals(item.CapabilityKey, "localgpt.vision.ocr", StringComparison.OrdinalIgnoreCase))
        {
            var ocr = scope.ServiceProvider.GetRequiredService<ILocalVisionOcrService>();
            var request = new LocalVisionOcrRequest
            {
                ImageDataUrl = GetString(parameters, "imageDataUrl", GetString(parameters, "dataUrl", string.Empty)),
                Prompt = GetString(parameters, "prompt", string.Empty),
                ModelName = GetString(parameters, "modelName", string.Empty),
                MaximumOutputTokens = GetInt(parameters, "maximumOutputTokens", 1600)
            };
            var ocrResult = await ocr.RecognizeAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(ocrResult, codec.JsonOptions);
        }

        var functionName = string.IsNullOrWhiteSpace(item.CapabilityKey) ? item.Request.Route : item.CapabilityKey;
        var catalog = scope.ServiceProvider.GetRequiredService<IDxAiFunctionCatalogService>();
        var catalogEntry = await catalog.GetByFunctionNameAsync(functionName, cancellationToken).ConfigureAwait(false);
        if (catalogEntry?.Kind == vocabulary.Get().CatalogPublicServiceMethod)
        {
            var invoker = scope.ServiceProvider.GetRequiredService<IPublicServiceMethodInvoker>();
            var serviceResult = await invoker.InvokeAsync(new PublicServiceMethodInvocationRequest
            {
                CatalogKey = catalogEntry.CatalogKey,
                Parameters = parameters,
                RequestedBy = string.IsNullOrWhiteSpace(item.SourcePeerId) ? "1-Wire peer" : item.SourcePeerId
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Executed configured 1-Wire service method {CatalogKey} for peer {PeerId}.", catalogEntry.CatalogKey, item.SourcePeerId);
            return JsonSerializer.Serialize(new { Succeeded = true, Status = "Completed", Value = serviceResult }, codec.JsonOptions);
        }

        var registry = scope.ServiceProvider.GetRequiredService<IDxAiFunctionRegistry>();
        var invocation = new DxAiFunctionInvocationRequest
        {
            OperationId = item.Id,
            Parameters = parameters,
            UserConfirmed = item.Request.UserConfirmed,
            AutomaticInvocation = item.Request.ExecutionMode != OneWireExecutionMode.Once,
            ConfirmationSummaryHash = item.Request.Hash,
            RequestedBy = string.IsNullOrWhiteSpace(item.SourcePeerId) ? "1-Wire peer" : item.SourcePeerId,
            ApplicationVersion = $"1-Wire/{OneWireProtocol.Version}"
        };
        var resultValue = await registry.InvokeAsync(functionName, invocation, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Executed 1-Wire DX function {FunctionName} with status {Status}.", functionName, resultValue.Status);
        return JsonSerializer.Serialize(resultValue, codec.JsonOptions);
    }

    private string GetString(JsonElement element, string name, string fallback) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private bool GetBoolean(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();

    private int GetInt(JsonElement element, string name, int fallback) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : fallback;

    private T ReadPayload<T>(OneWireEnvelope envelope, string propertyName)
    {
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(propertyName, out var element))
            throw new InvalidDataException($"The 1-Wire message is missing {propertyName}.");
        return element.Deserialize<T>(codec.JsonOptions) ?? throw new InvalidDataException($"The 1-Wire {propertyName} payload is empty.");
    }
}

public sealed class OneWireMessageDispatcher(
    IOneWireEnvelopeCodec codec,
    IOneWireCapabilityCatalog capabilities,
    IOneWirePeerRegistry peers,
    IOneWireWorkSpooler spooler,
    IOneWirePendingCouncilStore pendingCouncils,
    IHumanCollaborationService humanCollaboration,
    IOneWireRuntimeSecurityService security,
    IOneWireReplayGuard replayGuard,
    IOneWireTransportSecurityPolicy transportSecurityPolicy,
    IOneWireDispatchContextFactory dispatchContextFactory,
    IOneWireTargetApprovalPolicy targetApprovalPolicy,
    ILogger<OneWireMessageDispatcher> logger) : IOneWireMessageDispatcher
{
    public Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default) =>
        DispatchAsync(envelope, dispatchContextFactory.CreateInternal(), cancellationToken);

    public async Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, OneWireDispatchContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);

        // TCP/HTTP adapters validate the sealed transport form before optional decryption.
        // Revalidating the now-decrypted fields against the encrypted-form hash would reject valid secured messages.
        if (envelope.SecurityMode == OneWireSecurityMode.None && !codec.Validate(envelope, out var validationError))
            return Error(envelope, validationError);
        if (!OneWireProtocol.IsCompatible(envelope.ProtocolVersion))
            return Error(envelope, $"Unsupported 1-Wire protocol version '{envelope.ProtocolVersion}'.");

        var isInternalLocalCall = context.IsInternal &&
            string.Equals(envelope.SourcePeerId, "localgpt", StringComparison.OrdinalIgnoreCase);
        if (!context.IsInternal)
        {
            if (string.IsNullOrWhiteSpace(context.AuthenticatedPeerId) ||
                !string.Equals(envelope.SourcePeerId, context.AuthenticatedPeerId, StringComparison.OrdinalIgnoreCase))
            {
                return Error(envelope, "The envelope SourcePeerId does not match the peer identity owned by this transport.");
            }
            if (string.Equals(context.AuthenticatedPeerId, "localgpt", StringComparison.OrdinalIgnoreCase))
                return Error(envelope, "An external transport cannot claim the LocalGPT internal peer identity.");

            var protectedTransportRequired = !context.IsLoopback ||
                string.Equals(context.Transport, "http-json", StringComparison.OrdinalIgnoreCase);
            if (protectedTransportRequired && transportSecurityPolicy.RequiresProtectedTransport(envelope.MessageType) &&
                !transportSecurityPolicy.IsProtected(envelope))
            {
                return Error(envelope, "This transport requires MFA-verified signing and encryption before application data can be exchanged.");
            }
            if (!replayGuard.TryAccept(context.AuthenticatedPeerId, envelope.MessageId, envelope.CreatedUtc))
                return Error(envelope, "This 1-Wire message id has already been processed.");
        }

        if (envelope.MessageType != OneWireMessageType.Hello &&
            !isInternalLocalCall &&
            peers.GetPeer(envelope.SourcePeerId)?.IsConnected != true)
        {
            return Error(envelope, "This transport is not an approved 1-Wire link. Link it from both frontends before exchanging capabilities or invoking work.");
        }

        switch (envelope.MessageType)
        {
            case OneWireMessageType.Hello:
                if (TryRead<OneWirePeerAdvertisement>(envelope, "Peer", out var peer) && peer is not null)
                {
                    // A live TCP connection is not the same as a user-approved organic link.
                    // Keep the peer visible but unlinked until the LocalGPT frontend approves it.
                    peer.IsConnected = false;
                    peers.Upsert(peer);
                }
                var linkGate = await humanCollaboration.AuthorizeOrEnqueueAsync(
                    targetApprovalPolicy.Create(envelope),
                    directHumanConfirmation: false,
                    cancellationToken).ConfigureAwait(false);
                if (linkGate.IsDeclined)
                    return Error(envelope, string.IsNullOrWhiteSpace(linkGate.DecisionReason) ? linkGate.Message : linkGate.DecisionReason);
                if (!linkGate.IsAuthorized)
                {
                    pendingCouncils.Upsert(envelope, linkGate.RequestId);
                    return Reply(envelope, OneWireMessageType.ApprovalRequired, new Dictionary<string, object?>
                    {
                        ["ApprovalRequestId"] = linkGate.RequestId,
                        ["Status"] = linkGate.Status,
                        ["Message"] = "Waiting for the LocalGPT frontend user to approve this organic link.",
                        ["LinkApproval"] = true
                    });
                }
                pendingCouncils.Remove(envelope.CorrelationId, out _);
                peers.SetConnected(envelope.SourcePeerId, true);
                return Reply(envelope, OneWireMessageType.HelloAck, new Dictionary<string, object?>
                {
                    ["Peer"] = GetLocalAdvertisement(),
                    ["Security"] = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false),
                    ["LinkedByLocalFrontend"] = true,
                    ["CapabilityDirectoryTransport"] = "tcp-request"
                });

            case OneWireMessageType.SecurityProfileRequest:
                return Reply(envelope, OneWireMessageType.SecurityProfileResponse, new Dictionary<string, object?>
                {
                    ["Security"] = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false),
                    ["TransportKinds"] = new[] { "tcp", "http-json" },
                    ["RuntimeProvisioned"] = (await security.GetStatusAsync(cancellationToken).ConfigureAwait(false)).HasSecret
                });

            case OneWireMessageType.CapabilityRequest:
                return Reply(envelope, OneWireMessageType.CapabilityResponse, new Dictionary<string, object?>
                {
                    ["Capabilities"] = await capabilities.GetLocalCapabilitiesForPeerAsync(envelope.SourcePeerId, cancellationToken).ConfigureAwait(false),
                    ["Skills"] = await capabilities.GetLocalSkillsAsync(cancellationToken).ConfigureAwait(false),
                    ["UiFeatures"] = await capabilities.GetLocalUiFeaturesAsync(cancellationToken).ConfigureAwait(false),
                    ["Hardware"] = await capabilities.GetLocalHardwareAsync(cancellationToken).ConfigureAwait(false)
                });

            case OneWireMessageType.SkillRequest:
                return Reply(envelope, OneWireMessageType.SkillResponse, new Dictionary<string, object?>
                {
                    ["Skills"] = await capabilities.GetLocalSkillsAsync(cancellationToken).ConfigureAwait(false),
                    ["UiFeatures"] = await capabilities.GetLocalUiFeaturesAsync(cancellationToken).ConfigureAwait(false)
                });

            case OneWireMessageType.CouncilRequest:
                return await AuthorizeTargetAndQueueAsync(envelope, alwaysRequireHuman: true, cancellationToken).ConfigureAwait(false);

            case OneWireMessageType.Invoke:
                var advertised = await capabilities.GetLocalCapabilitiesForPeerAsync(envelope.SourcePeerId, cancellationToken).ConfigureAwait(false);
                var selected = advertised.FirstOrDefault(item => item.IsEnabled && item.IsOnline && item.IsExposedToPeer &&
                    string.Equals(item.Key, envelope.CapabilityKey, StringComparison.OrdinalIgnoreCase));
                if (selected is null)
                    return Error(envelope, "The requested capability is not exposed to this linked peer.");
                if (!selected.AllowPeerInvocation)
                    return Error(envelope, "The requested capability is discovery-only until the LocalGPT user enables peer invocation in the DX Function Catalog.");
                envelope.RequiresHumanInteractionOnTargetSystem = selected.RequiresFrontendUserConfirmation || selected.RequiresHumanConfirmation;
                envelope.InteractionKind = envelope.RequiresAutomatedInteractionOnTargetSystem
                    ? (envelope.RequiresHumanInteractionOnTargetSystem ? OneWireInteractionKind.HumanAndAutomated : OneWireInteractionKind.Automated)
                    : (envelope.RequiresHumanInteractionOnTargetSystem ? OneWireInteractionKind.Human : OneWireInteractionKind.None);
                envelope.Properties ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                envelope.Properties["InteractionEditor"] = JsonSerializer.SerializeToElement(selected.InteractionEditor.ToString(), codec.JsonOptions);
                envelope.Properties["ConfigurationKey"] = JsonSerializer.SerializeToElement(selected.ConfigurationKey, codec.JsonOptions);
                return await AuthorizeTargetAndQueueAsync(
                    envelope,
                    alwaysRequireHuman: envelope.RequiresHumanInteractionOnTargetSystem,
                    cancellationToken).ConfigureAwait(false);

            case OneWireMessageType.WorkStatusRequest:
                if (TryRead<Guid>(envelope, "WorkItemId", out var workId))
                {
                    var item = spooler.Get(workId);
                    return item is null ? Error(envelope, "Unknown work item.") : Reply(envelope, OneWireMessageType.WorkResult, new Dictionary<string, object?> { ["WorkItem"] = item });
                }
                return Error(envelope, "WorkItemId is required.");

            case OneWireMessageType.ApprovalRequired:
                var approvalJson = envelope.Properties is null ? string.Empty : JsonSerializer.Serialize(envelope.Properties, codec.JsonOptions);
                spooler.MarkPendingApproval(envelope.CorrelationId, approvalJson);
                return null;

            case OneWireMessageType.WorkResult:
                var resultJson = TryRead<string>(envelope, "ResultJson", out var result) ? result ?? string.Empty : string.Empty;
                OneWireWorkStatus? externalStatus = null;
                if (TryRead<string>(envelope, "Status", out var statusText) && Enum.TryParse<OneWireWorkStatus>(statusText, true, out var parsedStatus))
                    externalStatus = parsedStatus;
                spooler.ApplyExternalResult(envelope.CorrelationId, resultJson, envelope.Error, externalStatus);
                return null;

            case OneWireMessageType.Ping:
                return Reply(envelope, OneWireMessageType.Pong, new Dictionary<string, object?> { ["Utc"] = DateTimeOffset.UtcNow });

            default:
                logger.LogDebug("Ignored 1-Wire message type {MessageType}.", envelope.MessageType);
                return null;
        }
    }

    private async Task<OneWireEnvelope> AuthorizeTargetAndQueueAsync(
        OneWireEnvelope envelope,
        bool alwaysRequireHuman,
        CancellationToken cancellationToken)
    {
        if (!alwaysRequireHuman)
            return Accepted(envelope, spooler.Enqueue(envelope));

        var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(
            targetApprovalPolicy.Create(envelope),
            directHumanConfirmation: false,
            cancellationToken).ConfigureAwait(false);
        if (gate.IsDeclined)
            return Error(envelope, string.IsNullOrWhiteSpace(gate.DecisionReason) ? gate.Message : gate.DecisionReason);
        if (!gate.IsAuthorized)
        {
            pendingCouncils.Upsert(envelope, gate.RequestId);
            return Reply(envelope, OneWireMessageType.ApprovalRequired, new Dictionary<string, object?>
            {
                ["ApprovalRequestId"] = gate.RequestId,
                ["Status"] = gate.Status,
                ["Message"] = gate.Message,
                ["InteractionValueJson"] = envelope.InteractionValueJson
            });
        }
        pendingCouncils.Remove(envelope.CorrelationId, out _);
        ApplyHumanResponse(envelope, gate.UserResponse);
        return Accepted(envelope, spooler.Enqueue(envelope));
    }

    public void ApplyHumanResponse(OneWireEnvelope envelope, string? userResponse)
    {
        if (string.IsNullOrWhiteSpace(userResponse))
            return;

        envelope.InteractionValueJson = userResponse;
        var editor = targetApprovalPolicy.ReadEditor(envelope);
        envelope.InteractionValueContentType = editor == OneWireInteractionEditor.Json
            ? "application/json"
            : "text/plain; charset=utf-8";
    }

    private OneWireEnvelope Accepted(OneWireEnvelope request, OneWireWorkItem item) => Reply(request, OneWireMessageType.WorkAccepted, new Dictionary<string, object?> { ["WorkItem"] = item });

    private OneWireEnvelope Error(OneWireEnvelope request, string error) => new()
    {
        MessageType = OneWireMessageType.Error,
        CorrelationId = request.CorrelationId,
        ReplyToMessageId = request.MessageId,
        SourcePeerId = "localgpt",
        TargetPeerId = request.SourcePeerId,
        Error = error
    };

    private OneWireEnvelope Reply(OneWireEnvelope request, OneWireMessageType type, Dictionary<string, object?> values)
    {
        var properties = values.ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value, codec.JsonOptions), StringComparer.Ordinal);
        return new OneWireEnvelope
        {
            MessageType = type,
            CorrelationId = request.CorrelationId,
            ReplyToMessageId = request.MessageId,
            SourcePeerId = "localgpt",
            TargetPeerId = request.SourcePeerId,
            Properties = properties
        };
    }

    private bool TryRead<T>(OneWireEnvelope envelope, string propertyName, out T? value)
    {
        value = default;
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(propertyName, out var element))
            return false;
        try
        {
            value = element.Deserialize<T>(codec.JsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public OneWirePeerAdvertisement GetLocalAdvertisement() => new()
    {
        PeerId = "localgpt",
        DisplayName = "LocalGPT",
        Application = "LocalGPT",
        ApplicationVersion = "2.1.11-organic-wire",
        HostName = Environment.MachineName,
        Address = "127.0.0.1",
        ServicePort = Program.OneWirePort,
        DiscoveryPort = Program.OneWireDiscoveryPort,
        WebBaseUrl = Program.BaseUrl,
        TransportKind = OneWireTransportKind.Tcp,
        SupportedTransports = ["tcp", "http-json"],
        IsConnected = true
    };
}

public sealed class OneWireTargetApprovalPolicy(
    ILocalGptVocabularyService vocabulary,
    ILogger<OneWireTargetApprovalPolicy> logger) : IOneWireTargetApprovalPolicy
{
    public HumanApprovalRequestSpec Create(OneWireEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        logger.LogTrace("Creating a target approval policy for 1-Wire message type {MessageType} from peer {PeerId}.", envelope.MessageType, envelope.SourcePeerId);
        if (envelope.MessageType == OneWireMessageType.Hello)
        {
            return new HumanApprovalRequestSpec(
                $"onewire:link:{envelope.SourcePeerId}:{envelope.CorrelationId:D}",
                $"onewire.link.{envelope.SourcePeerId}",
                $"Link organic application {envelope.SourcePeerId}",
                "A local application requested an organic 1-Wire link. The transport remains unlinked until the LocalGPT frontend user approves it. Future capabilities remain independently controlled by the DX Function Catalog and the receiving frontend confirmation policy.",
                "High",
                nameof(OneWireMessageDispatcher),
                envelope.SourcePeerId,
                "Local 1-Wire link reviewer",
                RequiredBeforeCompletion: true,
                IsSensitive: true,
                RequestKind: vocabulary.Get().HumanRequestApproval,
                ResponsePrompt: "Approve or decline this exact local organic application link.",
                AllowFreeText: true,
                ParameterFingerprint: envelope.Hash ?? envelope.SourcePeerId);
        }

        var isCouncil = envelope.MessageType == OneWireMessageType.CouncilRequest || string.Equals(envelope.CapabilityKey, "council.run", StringComparison.OrdinalIgnoreCase);
        var operation = isCouncil ? "onewire.council.run" : $"onewire.invoke.{envelope.CapabilityKey}";
        var title = isCouncil
            ? $"Approve council request from {envelope.SourcePeerId}"
            : $"Approve {envelope.CapabilityKey} from {envelope.SourcePeerId}";
        var editor = ReadEditor(envelope);
        var needsValue = editor is OneWireInteractionEditor.PlainText or OneWireInteractionEditor.RichText or OneWireInteractionEditor.Json;
        var description = isCouncil
            ? "An organic plugin requested a bounded LocalGPT council run. Review the prompt, team, project context and requested organ capabilities in the collaboration inbox."
            : needsValue
                ? $"A securely linked organic plugin requested {envelope.CapabilityKey}. The receiving LocalGPT frontend is authoritative. Review the exact request and provide the requested {editor} value before the scheduler continues."
                : "A securely linked organic plugin requested a LocalGPT function. The receiving LocalGPT frontend is authoritative and must confirm the exact capability, organs, work order and payload before it is queued.";
        return new HumanApprovalRequestSpec(
            $"onewire:target:{envelope.SourcePeerId}:{envelope.CorrelationId:D}",
            operation,
            title,
            description,
            "High",
            nameof(OneWireMessageDispatcher),
            envelope.SourcePeerId,
            "External organic-plugin request reviewer",
            RequiredBeforeCompletion: true,
            IsSensitive: true,
            RequestKind: needsValue ? vocabulary.Get().HumanRequestGuidance : vocabulary.Get().HumanRequestApproval,
            ResponsePrompt: needsValue ? $"Enter the {editor} value to return to {envelope.SourcePeerId}." : "Confirm or decline this exact invocation.",
            PrefillText: envelope.InteractionValueJson ?? string.Empty,
            AllowFreeText: needsValue,
            ParameterFingerprint: envelope.Hash ?? string.Empty);
    }

    public OneWireInteractionEditor ReadEditor(OneWireEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        logger.LogTrace("Resolving the target interaction editor for 1-Wire message type {MessageType}.", envelope.MessageType);
        if (envelope.Properties is not null && envelope.Properties.TryGetValue("InteractionEditor", out var value))
        {
            if (value.ValueKind == JsonValueKind.String && Enum.TryParse<OneWireInteractionEditor>(value.GetString(), true, out var parsed))
                return parsed;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric) && Enum.IsDefined(typeof(OneWireInteractionEditor), numeric))
                return (OneWireInteractionEditor)numeric;
        }
        return envelope.RequiresHumanInteractionOnTargetSystem
            ? OneWireInteractionEditor.ConfirmationOnly
            : OneWireInteractionEditor.None;
    }
}

public sealed class OneWireCouncilApprovalProcessorHostedService(
    IOneWirePendingCouncilStore pendingCouncils,
    IHumanCollaborationService humanCollaboration,
    IOneWireWorkSpooler spooler,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IOneWireRuntimeSecurityService security,
    IOneWireTargetApprovalPolicy targetApprovalPolicy,
    IOneWireMessageDispatcher dispatcher,
    IOneWireEnvelopeCodec codec,
    ILogger<OneWireCouncilApprovalProcessorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            foreach (var pending in pendingCouncils.GetSnapshot())
            {
                if (stoppingToken.IsCancellationRequested)
                    break;
                if (pending.Envelope.ExpiresUtc is { } expires && expires <= DateTimeOffset.UtcNow)
                {
                    pendingCouncils.Remove(pending.Envelope.CorrelationId, out _);
                    await SendErrorAsync(pending.Envelope, "The pending 1-Wire request expired before approval.", stoppingToken).ConfigureAwait(false);
                    continue;
                }
                if (pending.LastCheckedUtc > DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(750))
                    continue;
                pendingCouncils.MarkChecked(pending.Envelope.CorrelationId);
                try
                {
                    var gate = await humanCollaboration.AuthorizeOrEnqueueAsync(
                        targetApprovalPolicy.Create(pending.Envelope),
                        directHumanConfirmation: false,
                        stoppingToken).ConfigureAwait(false);
                    if (gate.IsDeclined)
                    {
                        pendingCouncils.Remove(pending.Envelope.CorrelationId, out _);
                        await SendErrorAsync(
                            pending.Envelope,
                            string.IsNullOrWhiteSpace(gate.DecisionReason) ? gate.Message : gate.DecisionReason,
                            stoppingToken).ConfigureAwait(false);
                        continue;
                    }
                    if (!gate.IsAuthorized)
                        continue;

                    if (!pendingCouncils.Remove(pending.Envelope.CorrelationId, out _))
                        continue;

                    if (pending.Envelope.MessageType == OneWireMessageType.Hello)
                    {
                        peers.SetConnected(pending.Envelope.SourcePeerId, true);
                        await connections.SendAsync(
                            pending.Envelope.SourcePeerId,
                            CreateReply(pending.Envelope, OneWireMessageType.HelloAck, new Dictionary<string, object?>
                            {
                                ["Peer"] = dispatcher.GetLocalAdvertisement(),
                                ["Security"] = await security.GetPublicDescriptorAsync(stoppingToken).ConfigureAwait(false),
                                ["LinkedByLocalFrontend"] = true,
                                ["CapabilityDirectoryTransport"] = "tcp-request"
                            }),
                            stoppingToken).ConfigureAwait(false);
                        logger.LogInformation(
                            "LocalGPT frontend approved organic link {CorrelationId} for {PeerId}.",
                            pending.Envelope.CorrelationId, pending.Envelope.SourcePeerId);
                        continue;
                    }

                    dispatcher.ApplyHumanResponse(pending.Envelope, gate.UserResponse);
                    var item = spooler.Enqueue(pending.Envelope);
                    await connections.SendAsync(
                        pending.Envelope.SourcePeerId,
                        CreateReply(pending.Envelope, OneWireMessageType.WorkAccepted, new Dictionary<string, object?> { ["WorkItem"] = item }),
                        stoppingToken).ConfigureAwait(false);
                    logger.LogInformation(
                        "Resumed approved organic request {CorrelationId} from {PeerId} as work item {WorkItemId}.",
                        pending.Envelope.CorrelationId, pending.Envelope.SourcePeerId, item.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not resume pending organic request {CorrelationId}.", pending.Envelope.CorrelationId);
                }
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private Task SendErrorAsync(OneWireEnvelope request, string error, CancellationToken cancellationToken) =>
        connections.SendAsync(request.SourcePeerId, new OneWireEnvelope
        {
            MessageType = OneWireMessageType.Error,
            CorrelationId = request.CorrelationId,
            ReplyToMessageId = request.MessageId,
            SourcePeerId = "localgpt",
            TargetPeerId = request.SourcePeerId,
            Error = error
        }, cancellationToken);

    private OneWireEnvelope CreateReply(OneWireEnvelope request, OneWireMessageType type, Dictionary<string, object?> values) => new()
    {
        MessageType = type,
        CorrelationId = request.CorrelationId,
        ReplyToMessageId = request.MessageId,
        SourcePeerId = "localgpt",
        TargetPeerId = request.SourcePeerId,
        Properties = values.ToDictionary(
            pair => pair.Key,
            pair => JsonSerializer.SerializeToElement(pair.Value, codec.JsonOptions),
            StringComparer.Ordinal)
    };
}

public sealed class OneWireWorkProcessorHostedService(
    IOneWireWorkSpooler spooler,
    IOneWireOperationExecutor executor,
    IOneWireConnectionRegistry connections,
    ILogger<OneWireWorkProcessorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            OneWireWorkItem item;
            try { item = await spooler.DequeueAsync(stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            spooler.MarkRunning(item.Id);
            if (string.Equals(item.Request.SourcePeerId, "localgpt", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(item.Request.TargetPeerId) &&
                !string.Equals(item.Request.TargetPeerId, "localgpt", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("1-Wire work item {WorkItemId} is awaiting execution by external peer {PeerId}.", item.Id, item.Request.TargetPeerId);
                continue;
            }
            try
            {
                var result = await executor.ExecuteAsync(item, stoppingToken).ConfigureAwait(false);
                spooler.Complete(item.Id, result);
                await SendResultAsync(item, OneWireWorkStatus.Completed, result, string.Empty, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "1-Wire work item {WorkItemId} failed.", item.Id);
                spooler.Fail(item.Id, ex.Message);
                await SendResultAsync(item, OneWireWorkStatus.Failed, string.Empty, ex.Message, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task SendResultAsync(
        OneWireWorkItem item,
        OneWireWorkStatus status,
        string resultJson,
        string error,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.SourcePeerId) || string.Equals(item.SourcePeerId, "localgpt", StringComparison.OrdinalIgnoreCase))
            return;
        var envelope = new OneWireEnvelope
        {
            MessageType = OneWireMessageType.WorkResult,
            CorrelationId = item.CorrelationId,
            SourcePeerId = "localgpt",
            TargetPeerId = item.SourcePeerId,
            CapabilityKey = item.CapabilityKey,
            Error = error,
            Properties = new Dictionary<string, JsonElement>
            {
                ["WorkItemId"] = JsonSerializer.SerializeToElement(item.Id),
                ["Status"] = JsonSerializer.SerializeToElement(status.ToString()),
                ["ResultJson"] = JsonSerializer.SerializeToElement(resultJson)
            }
        };
        if (!await connections.SendAsync(item.SourcePeerId, envelope, cancellationToken).ConfigureAwait(false))
            logger.LogWarning("Could not return 1-Wire work result {WorkItemId} to disconnected peer {PeerId}.", item.Id, item.SourcePeerId);
    }
}
