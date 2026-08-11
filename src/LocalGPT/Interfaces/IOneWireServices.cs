using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the one wire envelope codec contract.
/// </summary>
public interface IOneWireEnvelopeCodec
{
    System.Text.Json.JsonSerializerOptions JsonOptions { get; }
    /// <summary>
    /// Runs the serialize operation.
    /// </summary>
    string Serialize(OneWireEnvelope envelope, bool seal = true);
    /// <summary>
    /// Runs the deserialize and validate operation.
    /// </summary>
    OneWireEnvelope DeserializeAndValidate(string json);
    /// <summary>
    /// Runs the validate operation.
    /// </summary>
    bool Validate(OneWireEnvelope envelope, out string error);
}



/// <summary>
/// Defines the one wire transport security policy contract.
/// </summary>
public interface IOneWireTransportSecurityPolicy
{
    /// <summary>
    /// Determines whether loopback.
    /// </summary>
    bool IsLoopback(System.Net.IPAddress? address);
    /// <summary>
    /// Determines whether protected.
    /// </summary>
    bool IsProtected(OneWireEnvelope envelope);
    /// <summary>
    /// Runs the requires protected transport operation.
    /// </summary>
    bool RequiresProtectedTransport(OneWireMessageType messageType);
}

/// <summary>
/// Defines the one wire dispatch context factory contract.
/// </summary>
public interface IOneWireDispatchContextFactory
{
    /// <summary>
    /// Creates internal.
    /// </summary>
    OneWireDispatchContext CreateInternal(string transport = "internal");
    /// <summary>
    /// Creates external.
    /// </summary>
    OneWireDispatchContext CreateExternal(string authenticatedPeerId, Guid connectionId, bool isLoopback, string transport);
}

/// <summary>
/// Defines the one wire listen address resolver contract.
/// </summary>
public interface IOneWireListenAddressResolver
{
    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    System.Net.IPAddress Resolve(OneWireOptions configured);
}

/// <summary>
/// Defines the one wire runtime security service contract.
/// </summary>
public interface IOneWireRuntimeSecurityService
{
    /// <summary>
    /// Gets status async.
    /// </summary>
    Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Ensures created async.
    /// </summary>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the regenerate async operation.
    /// </summary>
    Task RegenerateAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes async.
    /// </summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets public descriptor async.
    /// </summary>
    Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates pairing ticket async.
    /// </summary>
    Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets otp auth URI async.
    /// </summary>
    Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the establish trust async operation.
    /// </summary>
    Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the revoke trust async operation.
    /// </summary>
    Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets trusted peers async.
    /// </summary>
    Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the protect outgoing async operation.
    /// </summary>
    Task ProtectOutgoingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the unprotect incoming async operation.
    /// </summary>
    Task UnprotectIncomingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the local vision ocr service contract.
/// </summary>
public interface ILocalVisionOcrService
{
    /// <summary>
    /// Runs the recognize async operation.
    /// </summary>
    Task<LocalVisionOcrResult> RecognizeAsync(LocalVisionOcrRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the one wire capability catalog contract.
/// </summary>
public interface IOneWireCapabilityCatalog : IOneWireCapabilityProvider
{
    /// <summary>
    /// Gets local capabilities async.
    /// </summary>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets local capabilities for peer async.
    /// </summary>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesForPeerAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets local skills async.
    /// </summary>
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetLocalSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets local UI features async.
    /// </summary>
    Task<IReadOnlyList<LocalGPT.WireProtocol.OneWireUiFeatureDescriptor>> GetLocalUiFeaturesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets local hardware async.
    /// </summary>
    Task<IReadOnlyList<LocalGPT.WireProtocol.OneWireHardwareDescriptor>> GetLocalHardwareAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the one wire peer registry contract.
/// </summary>
public interface IOneWirePeerRegistry
{
    /// <summary>
    /// Gets peers.
    /// </summary>
    IReadOnlyList<OneWirePeerAdvertisement> GetPeers();
    /// <summary>
    /// Gets peer.
    /// </summary>
    OneWirePeerAdvertisement? GetPeer(string peerId);
    /// <summary>
    /// Runs the upsert operation.
    /// </summary>
    void Upsert(OneWirePeerAdvertisement peer);
    /// <summary>
    /// Sets connected.
    /// </summary>
    void SetConnected(string peerId, bool connected);
    /// <summary>
    /// Removes expired.
    /// </summary>
    void RemoveExpired(TimeSpan maximumAge);
}

/// <summary>
/// Defines the one wire connection registry contract.
/// </summary>
public interface IOneWireConnectionRegistry
{
    /// <summary>
    /// Runs the register operation.
    /// </summary>
    void Register(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender);
    /// <summary>
    /// Registers owned.
    /// </summary>
    Guid RegisterOwned(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender);
    /// <summary>
    /// Runs the unregister operation.
    /// </summary>
    void Unregister(string peerId);
    /// <summary>
    /// Runs the unregister operation.
    /// </summary>
    bool Unregister(string peerId, Guid registrationId);
    /// <summary>
    /// Determines whether connected.
    /// </summary>
    bool IsConnected(string peerId);
    /// <summary>
    /// Runs the send async operation.
    /// </summary>
    Task<bool> SendAsync(string peerId, OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}


/// <summary>
/// Defines the one wire replay policy data service contract.
/// </summary>
public interface IOneWireReplayPolicyDataService
{
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    OneWireReplayPolicySnapshot GetSnapshot();
}

/// <summary>
/// Defines the one wire replay guard contract.
/// </summary>
public interface IOneWireReplayGuard
{
    /// <summary>
    /// Attempts to accept.
    /// </summary>
    bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc);
}

/// <summary>
/// Defines the one wire work spooler contract.
/// </summary>
public interface IOneWireWorkSpooler
{
    /// <summary>
    /// Runs the enqueue operation.
    /// </summary>
    OneWireWorkItem Enqueue(OneWireEnvelope envelope);
    /// <summary>
    /// Runs the dequeue async operation.
    /// </summary>
    Task<OneWireWorkItem> DequeueAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    IReadOnlyList<OneWireWorkItem> GetSnapshot();
    /// <summary>
    /// Runs the get operation.
    /// </summary>
    OneWireWorkItem? Get(Guid id);
    /// <summary>
    /// Runs the mark running operation.
    /// </summary>
    void MarkRunning(Guid id);
    /// <summary>
    /// Runs the mark pending approval operation.
    /// </summary>
    void MarkPendingApproval(Guid correlationId, string resultJson);
    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    void Complete(Guid id, string resultJson);
    /// <summary>
    /// Runs the fail operation.
    /// </summary>
    void Fail(Guid id, string error);
    /// <summary>
    /// Applies external result.
    /// </summary>
    void ApplyExternalResult(Guid correlationId, string resultJson, string error, OneWireWorkStatus? status = null);
}

/// <summary>
/// Defines the one wire pending council store contract.
/// </summary>
public interface IOneWirePendingCouncilStore
{
    /// <summary>
    /// Runs the upsert operation.
    /// </summary>
    void Upsert(OneWireEnvelope envelope, Guid? approvalRequestId);
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    IReadOnlyList<OneWirePendingCouncilRequest> GetSnapshot();
    /// <summary>
    /// Runs the remove operation.
    /// </summary>
    bool Remove(Guid correlationId, out OneWirePendingCouncilRequest? request);
    /// <summary>
    /// Runs the mark checked operation.
    /// </summary>
    void MarkChecked(Guid correlationId);
}


/// <summary>
/// Defines the one wire target approval policy contract.
/// </summary>
public interface IOneWireTargetApprovalPolicy
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    HumanApprovalRequestSpec Create(OneWireEnvelope envelope);
    /// <summary>
    /// Reads editor.
    /// </summary>
    OneWireInteractionEditor ReadEditor(OneWireEnvelope envelope);
}

/// <summary>
/// Defines the organic DevExpress function support contract.
/// </summary>
public interface IOrganicDxFunctionSupport
{
    /// <summary>
    /// Gets string.
    /// </summary>
    string GetString(System.Text.Json.JsonElement element, string name, string fallback = "");
    /// <summary>
    /// Finds capability.
    /// </summary>
    OneWireCapabilityDescriptor? FindCapability(OneWirePeerAdvertisement peer, string key);
    OneWireEnvelope CreateInvokeEnvelope(string peerId, OneWireCapabilityDescriptor capability, System.Text.Json.JsonElement payload,
        OneWireExecutionMode executionMode, string workOrderKey, DateTimeOffset? notBeforeUtc, bool userConfirmed, string interactionValueJson);
    /// <summary>
    /// Runs the queued operation.
    /// </summary>
    DxAiFunctionInvocationResult Queued(OneWireWorkItem work, string peerId, string capabilityKey);
    /// <summary>
    /// Runs the invalid operation.
    /// </summary>
    DxAiFunctionInvocationResult Invalid(string error);
}

/// <summary>
/// Defines the publisher interaction DevExpress support contract.
/// </summary>
public interface IPublisherInteractionDxSupport
{
    /// <summary>
    /// Runs the queue async operation.
    /// </summary>
    Task<DxAiFunctionInvocationResult> QueueAsync<TLogger>(
        DxAiFunctionInvocationRequest request,
        string capabilityKey,
        IOneWireConnectionRegistry connections,
        IOneWirePeerRegistry peers,
        IOneWireWorkSpooler spooler,
        Microsoft.Extensions.Logging.ILogger<TLogger> logger,
        Func<System.Text.Json.JsonElement, System.Text.Json.JsonElement> createPayload,
        CancellationToken cancellationToken);
}

/// <summary>
/// Defines the one wire operation executor contract.
/// </summary>
public interface IOneWireOperationExecutor
{
    /// <summary>
    /// Runs the execute async operation.
    /// </summary>
    Task<string> ExecuteAsync(OneWireWorkItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the one wire message dispatcher contract.
/// </summary>
public interface IOneWireMessageDispatcher
{
    /// <summary>
    /// Gets local advertisement.
    /// </summary>
    OneWirePeerAdvertisement GetLocalAdvertisement();
    /// <summary>
    /// Applies human response.
    /// </summary>
    void ApplyHumanResponse(OneWireEnvelope envelope, string? userResponse);
    /// <summary>
    /// Runs the dispatch async operation.
    /// </summary>
    Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the dispatch async operation.
    /// </summary>
    Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, OneWireDispatchContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the organic council blueprint service contract.
/// </summary>
public interface IOrganicCouncilBlueprintService
{
    /// <summary>
    /// Gets teams async.
    /// </summary>
    Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetTeamsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds team async.
    /// </summary>
    Task<OrganicCouncilTeamDefinition?> FindTeamAsync(string? key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds briefing async.
    /// </summary>
    Task<string> BuildBriefingAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds expert preparation prompt.
    /// </summary>
    string BuildExpertPreparationPrompt(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team);
    /// <summary>
    /// Builds leader synthesis prompt.
    /// </summary>
    string BuildLeaderSynthesisPrompt(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team, string preparation);
}

/// <summary>
/// Defines the project organic context service contract.
/// </summary>
public interface IProjectOrganicContextService
{
    /// <summary>
    /// Gets async.
    /// </summary>
    Task<ProjectOrganicContext> GetAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Saves async.
    /// </summary>
    Task<ProjectOrganicContext> SaveAsync(Guid projectId, SaveProjectOrganicContextRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds briefing async.
    /// </summary>
    Task<string> BuildBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the organic council blueprint seed data service contract.
/// </summary>
public interface IOrganicCouncilBlueprintSeedDataService
{
    /// <summary>
    /// Creates default teams.
    /// </summary>
    IReadOnlyList<OrganicCouncilTeamDefinition> CreateDefaultTeams();
}
