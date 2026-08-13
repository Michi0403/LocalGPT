using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for one wire envelope codec behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireEnvelopeCodec
{
    /// <summary>
    /// Gets the JSON options value that forms part of the one wire envelope codec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JSON options value exposed by <see cref="IOneWireEnvelopeCodec"/>.</value>
    System.Text.Json.JsonSerializerOptions JsonOptions { get; }
    /// <summary>
    /// Performs serialize for <see cref="IOneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <param name="seal">Value indicating whether seal should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string Serialize(OneWireEnvelope envelope, bool seal = true);
    /// <summary>
    /// Performs deserialize and validate for <see cref="IOneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="json">Json value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    OneWireEnvelope DeserializeAndValidate(string json);
    /// <summary>
    /// Performs validate for <see cref="IOneWireEnvelopeCodec"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the one wire envelope codec operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Validate(OneWireEnvelope envelope, out string error);
}



/// <summary>
/// Defines the contract for one wire transport security policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireTransportSecurityPolicy
{
    /// <summary>
    /// Determines whether loopback for <see cref="IOneWireTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport security policy workflow.
    /// </summary>
    /// <param name="address">Address value supplied to the one wire transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsLoopback(System.Net.IPAddress? address);
    /// <summary>
    /// Determines whether protected for <see cref="IOneWireTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport security policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsProtected(OneWireEnvelope envelope);
    /// <summary>
    /// Performs requires protected transport for <see cref="IOneWireTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport security policy workflow.
    /// </summary>
    /// <param name="messageType">Message type value supplied to the one wire transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool RequiresProtectedTransport(OneWireMessageType messageType);
}

/// <summary>
/// Defines the contract for one wire dispatch context behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireDispatchContextFactory
{
    /// <summary>
    /// Creates internal using the configuration and dependencies owned by <see cref="IOneWireDispatchContextFactory"/>.
    /// </summary>
    /// <param name="transport">Transport value supplied to the one wire dispatch context operation and used when producing its result.</param>
    /// <returns>The one wire dispatch context produced by the operation.</returns>
    OneWireDispatchContext CreateInternal(string transport = "internal");
    /// <summary>
    /// Creates external using the configuration and dependencies owned by <see cref="IOneWireDispatchContextFactory"/>.
    /// </summary>
    /// <param name="authenticatedPeerId">Identifier of the authenticated peer to use for this operation.</param>
    /// <param name="connectionId">Identifier of the connection to use for this operation.</param>
    /// <param name="isLoopback">Value indicating whether is loopback should apply to this operation.</param>
    /// <param name="transport">Transport value supplied to the one wire dispatch context operation and used when producing its result.</param>
    /// <returns>The one wire dispatch context produced by the operation.</returns>
    OneWireDispatchContext CreateExternal(string authenticatedPeerId, Guid connectionId, bool isLoopback, string transport);
}

/// <summary>
/// Defines the contract for one wire listen address behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireListenAddressResolver
{
    /// <summary>
    /// Performs resolve for <see cref="IOneWireListenAddressResolver"/>, keeping the operation consistent with the state and invariants of the surrounding one wire listen address workflow.
    /// </summary>
    /// <param name="configured">Configured value supplied to the one wire listen address operation and used when producing its result.</param>
    /// <returns>The system net IP address produced by the operation.</returns>
    System.Net.IPAddress Resolve(OneWireOptions configured);
}

/// <summary>
/// Defines the contract for one wire runtime security behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireRuntimeSecurityService
{
    /// <summary>
    /// Retrieves status as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire runtime security status produced by the operation.</returns>
    Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Ensures created as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs regenerate as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RegenerateAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs delete as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DeleteAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves public descriptor as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire security descriptor produced by the operation.</returns>
    Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates pairing ticket as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="lifetime">Lifetime value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire pairing ticket produced by the operation.</returns>
    Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves otp auth URI as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs establish trust as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs revoke trust as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves trusted peers as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs protect outgoing as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ProtectOutgoingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs unprotect incoming as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task UnprotectIncomingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for local vision OCR behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalVisionOcrService
{
    /// <summary>
    /// Performs recognize as part of the local vision OCR service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local vision OCR result produced by the operation.</returns>
    Task<LocalVisionOcrResult> RecognizeAsync(LocalVisionOcrRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for one wire capability behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireCapabilityCatalog : IOneWireCapabilityProvider
{
    /// <summary>
    /// Retrieves local capabilities in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves local capabilities for peer in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesForPeerAsync(string peerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves local skills in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetLocalSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves local UI features in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGPT.WireProtocol.OneWireUiFeatureDescriptor>> GetLocalUiFeaturesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves local hardware in the one wire capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<LocalGPT.WireProtocol.OneWireHardwareDescriptor>> GetLocalHardwareAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for one wire peer behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWirePeerRegistry
{
    /// <summary>
    /// Retrieves peers in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OneWirePeerAdvertisement> GetPeers();
    /// <summary>
    /// Retrieves peer in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>The one wire peer advertisement produced by the operation.</returns>
    OneWirePeerAdvertisement? GetPeer(string peerId);
    /// <summary>
    /// Performs upsert in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peer">Peer value supplied to the one wire peer operation and used when producing its result.</param>
    void Upsert(OneWirePeerAdvertisement peer);
    /// <summary>
    /// Sets connected in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="connected">Value indicating whether connected should apply to this operation.</param>
    void SetConnected(string peerId, bool connected);
    /// <summary>
    /// Removes expired in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="maximumAge">Maximum age value supplied to the one wire peer operation and used when producing its result.</param>
    void RemoveExpired(TimeSpan maximumAge);
}

/// <summary>
/// Defines the contract for one wire connection behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireConnectionRegistry
{
    /// <summary>
    /// Performs register in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="sender">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    void Register(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender);
    /// <summary>
    /// Registers owned in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="sender">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The GUID produced by the operation.</returns>
    Guid RegisterOwned(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender);
    /// <summary>
    /// Performs unregister in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    void Unregister(string peerId);
    /// <summary>
    /// Performs unregister in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="registrationId">Identifier of the registration to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Unregister(string peerId, Guid registrationId);
    /// <summary>
    /// Determines whether connected in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsConnected(string peerId);
    /// <summary>
    /// Performs send in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="envelope">Envelope value supplied to the one wire connection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    Task<bool> SendAsync(string peerId, OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}


/// <summary>
/// Defines the contract for one wire replay policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireReplayPolicyDataService
{
    /// <summary>
    /// Retrieves snapshot as part of the one wire replay policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The one wire replay policy snapshot produced by the operation.</returns>
    OneWireReplayPolicySnapshot GetSnapshot();
}

/// <summary>
/// Defines the contract for one wire replay guard behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireReplayGuard
{
    /// <summary>
    /// Attempts to accept for <see cref="IOneWireReplayGuard"/>, keeping the operation consistent with the state and invariants of the surrounding one wire replay guard workflow.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="messageId">Identifier of the message to use for this operation.</param>
    /// <param name="createdUtc">Created utc value supplied to the one wire replay guard operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc);
}

/// <summary>
/// Defines the contract for one wire work spooler behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireWorkSpooler
{
    /// <summary>
    /// Performs enqueue for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire work spooler operation and used when producing its result.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    OneWireWorkItem Enqueue(OneWireEnvelope envelope);
    /// <summary>
    /// Performs dequeue for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    Task<OneWireWorkItem> DequeueAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Retrieves snapshot for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OneWireWorkItem> GetSnapshot();
    /// <summary>
    /// Performs get for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    OneWireWorkItem? Get(Guid id);
    /// <summary>
    /// Performs mark running for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    void MarkRunning(Guid id);
    /// <summary>
    /// Performs mark pending approval for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work spooler operation and used when producing its result.</param>
    void MarkPendingApproval(Guid correlationId, string resultJson);
    /// <summary>
    /// Performs complete for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work spooler operation and used when producing its result.</param>
    void Complete(Guid id, string resultJson);
    /// <summary>
    /// Performs fail for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="error">Error value supplied to the one wire work spooler operation and used when producing its result.</param>
    void Fail(Guid id, string error);
    /// <summary>
    /// Applies external result for <see cref="IOneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work spooler operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the one wire work spooler operation and used when producing its result.</param>
    /// <param name="status">Status value supplied to the one wire work spooler operation and used when producing its result.</param>
    void ApplyExternalResult(Guid correlationId, string resultJson, string error, OneWireWorkStatus? status = null);
}

/// <summary>
/// Defines the contract for one wire pending council behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWirePendingCouncilStore
{
    /// <summary>
    /// Performs upsert in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="IOneWirePendingCouncilStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire pending council operation and used when producing its result.</param>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    void Upsert(OneWireEnvelope envelope, Guid? approvalRequestId);
    /// <summary>
    /// Retrieves snapshot in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="IOneWirePendingCouncilStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OneWirePendingCouncilRequest> GetSnapshot();
    /// <summary>
    /// Performs remove in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="IOneWirePendingCouncilStore"/>.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Remove(Guid correlationId, out OneWirePendingCouncilRequest? request);
    /// <summary>
    /// Performs mark checked in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="IOneWirePendingCouncilStore"/>.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    void MarkChecked(Guid correlationId);
}


/// <summary>
/// Defines the contract for one wire target approval policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireTargetApprovalPolicy
{
    /// <summary>
    /// Performs create for <see cref="IOneWireTargetApprovalPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire target approval policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire target approval policy operation and used when producing its result.</param>
    /// <returns>The human approval request spec produced by the operation.</returns>
    HumanApprovalRequestSpec Create(OneWireEnvelope envelope);
    /// <summary>
    /// Reads editor for <see cref="IOneWireTargetApprovalPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire target approval policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire target approval policy operation and used when producing its result.</param>
    /// <returns>The one wire interaction editor produced by the operation.</returns>
    OneWireInteractionEditor ReadEditor(OneWireEnvelope envelope);
}

/// <summary>
/// Defines the contract for organic DevExpress function support behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicDxFunctionSupport
{
    /// <summary>
    /// Retrieves string for <see cref="IOrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetString(System.Text.Json.JsonElement element, string name, string fallback = "");
    /// <summary>
    /// Finds capability for <see cref="IOrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="peer">Peer value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="key">Key value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The one wire capability descriptor produced by the operation.</returns>
    OneWireCapabilityDescriptor? FindCapability(OneWirePeerAdvertisement peer, string key);
    /// <summary>
    /// Creates invoke envelope for <see cref="IOrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
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
    OneWireEnvelope CreateInvokeEnvelope(string peerId, OneWireCapabilityDescriptor capability, System.Text.Json.JsonElement payload,
        OneWireExecutionMode executionMode, string workOrderKey, DateTimeOffset? notBeforeUtc, bool userConfirmed, string interactionValueJson);
    /// <summary>
    /// Performs queued for <see cref="IOrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="work">Work value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    DxAiFunctionInvocationResult Queued(OneWireWorkItem work, string peerId, string capabilityKey);
    /// <summary>
    /// Performs invalid for <see cref="IOrganicDxFunctionSupport"/>, keeping the operation consistent with the state and invariants of the surrounding organic DevExpress function support workflow.
    /// </summary>
    /// <param name="error">Error value supplied to the organic DevExpress function support operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    DxAiFunctionInvocationResult Invalid(string error);
}

/// <summary>
/// Defines the contract for publisher interaction DevExpress support behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublisherInteractionDxSupport
{
    /// <summary>
    /// Performs queue for <see cref="IPublisherInteractionDxSupport"/>, keeping the operation consistent with the state and invariants of the surrounding publisher interaction DevExpress support workflow.
    /// </summary>
    /// <typeparam name="TLogger">Type used for t logger values handled by <see cref="IPublisherInteractionDxSupport"/>.</typeparam>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the publisher interaction DevExpress support operation and used when producing its result.</param>
    /// <param name="connections">One wire connection registry dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
    /// <param name="peers">One wire peer registry dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
    /// <param name="spooler">One wire work spooler dependency used by the publisher interaction DevExpress support workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="createPayload">Create payload value supplied to the publisher interaction DevExpress support operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
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
/// Defines the contract for one wire operation executor behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireOperationExecutor
{
    /// <summary>
    /// Performs execute for <see cref="IOneWireOperationExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding one wire operation executor workflow.
    /// </summary>
    /// <param name="item">Item value supplied to the one wire operation executor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> ExecuteAsync(OneWireWorkItem item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for one wire message dispatcher behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOneWireMessageDispatcher
{
    /// <summary>
    /// Retrieves local advertisement for <see cref="IOneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <returns>The one wire peer advertisement produced by the operation.</returns>
    OneWirePeerAdvertisement GetLocalAdvertisement();
    /// <summary>
    /// Applies human response for <see cref="IOneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="userResponse">User response value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    void ApplyHumanResponse(OneWireEnvelope envelope, string? userResponse);
    /// <summary>
    /// Performs dispatch for <see cref="IOneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs dispatch for <see cref="IOneWireMessageDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding one wire message dispatcher workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="context">Context value supplied to the one wire message dispatcher operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire envelope produced by the operation.</returns>
    Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, OneWireDispatchContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for organic council blueprint behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicCouncilBlueprintService
{
    /// <summary>
    /// Retrieves teams as part of the organic council blueprint service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetTeamsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds team as part of the organic council blueprint service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the organic council blueprint operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic council team definition produced by the operation.</returns>
    Task<OrganicCouncilTeamDefinition?> FindTeamAsync(string? key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds briefing as part of the organic council blueprint service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> BuildBriefingAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds expert preparation prompt as part of the organic council blueprint service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="team">Team value supplied to the organic council blueprint operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildExpertPreparationPrompt(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team);
    /// <summary>
    /// Builds leader synthesis prompt as part of the organic council blueprint service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="team">Team value supplied to the organic council blueprint operation and used when producing its result.</param>
    /// <param name="preparation">Preparation value supplied to the organic council blueprint operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildLeaderSynthesisPrompt(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team, string preparation);
}

/// <summary>
/// Defines the contract for project organic context behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProjectOrganicContextService
{
    /// <summary>
    /// Performs get as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project organic context produced by the operation.</returns>
    Task<ProjectOrganicContext> GetAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs save as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project organic context produced by the operation.</returns>
    Task<ProjectOrganicContext> SaveAsync(Guid projectId, SaveProjectOrganicContextRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Builds briefing as part of the project organic context service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    Task<string> BuildBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for organic council blueprint seed behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IOrganicCouncilBlueprintSeedDataService
{
    /// <summary>
    /// Creates default teams as part of the organic council blueprint seed service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<OrganicCouncilTeamDefinition> CreateDefaultTeams();
}
