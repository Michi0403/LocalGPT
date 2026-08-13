using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Text.Json;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Represents an one wire operation executor application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="scopeFactory">Service scope factory dependency used by the one wire operation executor workflow to provide the corresponding application capability.</param>
/// <param name="codec">One wire envelope codec dependency used by the one wire operation executor workflow to provide the corresponding application capability.</param>
/// <param name="vocabulary">Local gpt vocabulary service dependency used by the one wire operation executor workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireOperationExecutor(
    IServiceScopeFactory scopeFactory,
    IOneWireEnvelopeCodec codec,
    ILocalGptVocabularyService vocabulary,
    ILogger<OneWireOperationExecutor> logger) : IOneWireOperationExecutor
{

    /// <summary>
    /// Performs execute for <see cref="OneWireOperationExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding one wire operation executor workflow.
    /// </summary>
    /// <param name="item">Item value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> ExecuteAsync(OneWireWorkItem item, CancellationToken cancellationToken = default)
    {
    try
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
                    ModelSelections = wireRequest.ModelRoutes
                        .Where(route => !string.IsNullOrWhiteSpace(route.ProviderKind)
                            && !string.IsNullOrWhiteSpace(route.ProviderEndpoint)
                            && !string.IsNullOrWhiteSpace(route.ProviderModelName))
                        .Select(route => new ProviderModelReference
                        {
                            ProviderKind = route.ProviderKind.Trim(),
                            ProviderName = string.IsNullOrWhiteSpace(route.ProviderName) ? route.ProviderKind.Trim() : route.ProviderName.Trim(),
                            Endpoint = route.ProviderEndpoint.Trim(),
                            ModelName = route.ProviderModelName.Trim(),
                            IsLocal = route.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                                || route.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase),
                            IsConfigured = true,
                            IsReachable = false,
                            SupportsBenchmark = true,
                            Details = "Provider-qualified OneWire Council route."
                        })
                        .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .ToList(),
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
            if (string.Equals(item.CapabilityKey, "localgpt.documentation.profile", StringComparison.OrdinalIgnoreCase))
            {
                var documentation = scope.ServiceProvider.GetRequiredService<IDocumentationCatalogService>();
                return JsonSerializer.Serialize(new
                {
                    Status = documentation.GetStatus(),
                    HtmlRoute = "/help-docs/index.html",
                    ApiRoute = "/help-docs/api/index.html",
                    PdfRoute = "/api/documentation/pdf",
                    ProfileRoute = "/api/documentation/profile"
                }, codec.JsonOptions);
            }

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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(ExecuteAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(ExecuteAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves string for <see cref="OneWireOperationExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding one wire operation executor workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetString(JsonElement element, string name, string fallback) {
    try
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(GetString)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(GetString)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves boolean for <see cref="OneWireOperationExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding one wire operation executor workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetBoolean(JsonElement element, string name) {
    try
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(GetBoolean)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(GetBoolean)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves int for <see cref="OneWireOperationExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding one wire operation executor workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int GetInt(JsonElement element, string name, int fallback) {
    try
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(GetInt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireOperationExecutor)}.{nameof(GetInt)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads payload for <see cref="OneWireOperationExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding one wire operation executor workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="OneWireOperationExecutor"/>.</typeparam>
    /// <param name="envelope">Envelope value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="propertyName">Property name value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    private T ReadPayload<T>(OneWireEnvelope envelope, string propertyName)
    {
        if (envelope.Properties is null || !envelope.Properties.TryGetValue(propertyName, out var element))
            throw new InvalidDataException($"The 1-Wire message is missing {propertyName}.");
        return element.Deserialize<T>(codec.JsonOptions) ?? throw new InvalidDataException($"The 1-Wire {propertyName} payload is empty.");
    }
}

/// <summary>
/// Represents an one wire message dispatcher application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="codec">One wire envelope codec dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="capabilities">One wire capability catalog dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="pendingCouncils">One wire pending council store dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="humanCollaboration">Human collaboration service dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="security">One wire runtime security service dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="replayGuard">One wire replay guard dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="transportSecurityPolicy">One wire transport security policy dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="dispatchContextFactory">One wire dispatch context factory dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="targetApprovalPolicy">One wire target approval policy dependency used by the one wire message dispatcher workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// <summary>
    /// Performs dispatch for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    public Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default) {
    try
    {
        return DispatchAsync(envelope, dispatchContextFactory.CreateInternal(), cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(DispatchAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(DispatchAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs dispatch for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    public async Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, OneWireDispatchContext context, CancellationToken cancellationToken = default)
    {
    try
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

                case OneWireMessageType.CapabilityResponse:
                case OneWireMessageType.SkillResponse:
                case OneWireMessageType.SkillStateUpdate:
                    if (peers.GetPeer(envelope.SourcePeerId) is not { } refreshedPeer)
                        return Error(envelope, "The linked peer has no registered 1-Wire advertisement to refresh.");
                    if (TryRead<List<OneWireCapabilityDescriptor>>(envelope, "Capabilities", out var refreshedCapabilities))
                        refreshedPeer.Capabilities = refreshedCapabilities ?? [];
                    if (TryRead<List<OneWireSkillDescriptor>>(envelope, "Skills", out var refreshedSkills))
                        refreshedPeer.Skills = refreshedSkills ?? [];
                    if (TryRead<List<OneWireUiFeatureDescriptor>>(envelope, "UiFeatures", out var refreshedUiFeatures))
                        refreshedPeer.UiFeatures = refreshedUiFeatures ?? [];
                    if (TryRead<List<OneWireHardwareDescriptor>>(envelope, "Hardware", out var refreshedHardware))
                        refreshedPeer.Hardware = refreshedHardware ?? [];
                    refreshedPeer.IsConnected = true;
                    peers.Upsert(refreshedPeer);
                    logger.LogInformation(
                        "Refreshed live 1-Wire directory for peer {PeerId}: {CapabilityCount} capabilities, {SkillCount} skills, {UiFeatureCount} UI features and {HardwareCount} hardware entries.",
                        refreshedPeer.PeerId,
                        refreshedPeer.Capabilities.Count,
                        refreshedPeer.Skills.Count,
                        refreshedPeer.UiFeatures.Count,
                        refreshedPeer.Hardware.Count);
                    return null;

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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(DispatchAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(DispatchAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs authorize target and queue for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="alwaysRequireHuman">Value indicating whether always require human should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    private async Task<OneWireEnvelope> AuthorizeTargetAndQueueAsync(
        OneWireEnvelope envelope,
        bool alwaysRequireHuman,
        CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(AuthorizeTargetAndQueueAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(AuthorizeTargetAndQueueAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies human response for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="userResponse">User response value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    public void ApplyHumanResponse(OneWireEnvelope envelope, string? userResponse)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(userResponse))
                return;

            envelope.InteractionValueJson = userResponse;
            var editor = targetApprovalPolicy.ReadEditor(envelope);
            envelope.InteractionValueContentType = editor == OneWireInteractionEditor.Json
                ? "application/json"
                : "text/plain; charset=utf-8";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(ApplyHumanResponse)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(ApplyHumanResponse)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs accepted for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="item">Item value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    private OneWireEnvelope Accepted(OneWireEnvelope request, OneWireWorkItem item) {
    try
    {
        return Reply(request, OneWireMessageType.WorkAccepted, new Dictionary<string, object?> { ["WorkItem"] = item });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(Accepted)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(Accepted)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs error for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="error">Error value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    private OneWireEnvelope Error(OneWireEnvelope request, string error) {
    try
    {
        return new()
    {
        MessageType = OneWireMessageType.Error,
        CorrelationId = request.CorrelationId,
        ReplyToMessageId = request.MessageId,
        SourcePeerId = "localgpt",
        TargetPeerId = request.SourcePeerId,
        Error = error
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(Error)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(Error)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs reply for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="type">Type value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="values">Values value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    private OneWireEnvelope Reply(OneWireEnvelope request, OneWireMessageType type, Dictionary<string, object?> values)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(Reply)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(Reply)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to read for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="OneWireMessageDispatcher"/>.</typeparam>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="propertyName">Property name value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Retrieves local advertisement for <see cref="OneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <returns>The one wire peer advertisement produced by the operation.</returns>
    public OneWirePeerAdvertisement GetLocalAdvertisement() {
    try
    {
        return new()
    {
        PeerId = "localgpt",
        DisplayName = "LocalGPT",
        Application = "LocalGPT",
        ApplicationVersion = typeof(OneWireMessageDispatcher).Assembly.GetName().Version?.ToString(3) ?? string.Empty,
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(GetLocalAdvertisement)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireMessageDispatcher)}.{nameof(GetLocalAdvertisement)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an one wire target approval policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="vocabulary">Local gpt vocabulary service dependency used by the one wire target approval policy workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireTargetApprovalPolicy(
    ILocalGptVocabularyService vocabulary,
    ILogger<OneWireTargetApprovalPolicy> logger) : IOneWireTargetApprovalPolicy
{
    /// <summary>
    /// Performs create for <see cref="OneWireTargetApprovalPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire target approval policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire target approval policy operation and used when producing its result.</param>
    /// <returns>The human approval request spec produced by the operation.</returns>
    public HumanApprovalRequestSpec Create(OneWireEnvelope envelope)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireTargetApprovalPolicy)}.{nameof(Create)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireTargetApprovalPolicy)}.{nameof(Create)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads editor for <see cref="OneWireTargetApprovalPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire target approval policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire target approval policy operation and used when producing its result.</param>
    /// <returns>The one wire interaction editor produced by the operation.</returns>
    public OneWireInteractionEditor ReadEditor(OneWireEnvelope envelope)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireTargetApprovalPolicy)}.{nameof(ReadEditor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireTargetApprovalPolicy)}.{nameof(ReadEditor)} failed.");
        throw;
    }
}
}

/// <summary>
/// Coordinates one wire council approval processor behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="pendingCouncils">One wire pending council store dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="humanCollaboration">Human collaboration service dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="spooler">One wire work spooler dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="security">One wire runtime security service dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="targetApprovalPolicy">One wire target approval policy dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="dispatcher">One wire message dispatcher dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="codec">One wire envelope codec dependency used by the one wire council approval processor workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// <summary>
    /// Performs execute as part of the one wire council approval processor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs send error as part of the one wire council approval processor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="error">Error value supplied to the one wire council approval processor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task SendErrorAsync(OneWireEnvelope request, string error, CancellationToken cancellationToken) {
    try
    {
        return connections.SendAsync(request.SourcePeerId, new OneWireEnvelope
        {
            MessageType = OneWireMessageType.Error,
            CorrelationId = request.CorrelationId,
            ReplyToMessageId = request.MessageId,
            SourcePeerId = "localgpt",
            TargetPeerId = request.SourcePeerId,
            Error = error
        }, cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCouncilApprovalProcessorHostedService)}.{nameof(SendErrorAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCouncilApprovalProcessorHostedService)}.{nameof(SendErrorAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates reply as part of the one wire council approval processor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="type">Type value supplied to the one wire council approval processor operation and used when producing its result.</param>
    /// <param name="values">Values value supplied to the one wire council approval processor operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    private OneWireEnvelope CreateReply(OneWireEnvelope request, OneWireMessageType type, Dictionary<string, object?> values) {
    try
    {
        return new()
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireCouncilApprovalProcessorHostedService)}.{nameof(CreateReply)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireCouncilApprovalProcessorHostedService)}.{nameof(CreateReply)} failed.");
        throw;
    }
}
}

/// <summary>
/// Coordinates one wire work processor behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="spooler">One wire work spooler dependency used by the one wire work processor workflow to provide the corresponding application capability.</param>
/// <param name="executor">One wire operation executor dependency used by the one wire work processor workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the one wire work processor workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireWorkProcessorHostedService(
    IOneWireWorkSpooler spooler,
    IOneWireOperationExecutor executor,
    IOneWireConnectionRegistry connections,
    ILogger<OneWireWorkProcessorHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Performs execute as part of the one wire work processor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs send result as part of the one wire work processor service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="item">Item value supplied to the one wire work processor operation and used when producing its result.</param>
    /// <param name="status">Status value supplied to the one wire work processor operation and used when producing its result.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work processor operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the one wire work processor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SendResultAsync(
        OneWireWorkItem item,
        OneWireWorkStatus status,
        string resultJson,
        string error,
        CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkProcessorHostedService)}.{nameof(SendResultAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkProcessorHostedService)}.{nameof(SendResultAsync)} failed.");
        throw;
    }
}
}
