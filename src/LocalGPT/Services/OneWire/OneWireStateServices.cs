using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Maintains the authoritative directory of one wire peer entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWirePeerRegistry(ILogger<OneWirePeerRegistry> logger) : IOneWirePeerRegistry
{
    /// <summary>
    /// Stores the in-memory peers collection maintained internally by <see cref="OneWirePeerRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, OneWirePeerAdvertisement> peers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves peers in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OneWirePeerAdvertisement> GetPeers() {
    try
    {
        return peers.Values
        .OrderByDescending(peer => peer.IsConnected)
        .ThenByDescending(peer => peer.SeenUtc)
        .Select(Clone)
        .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(GetPeers)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(GetPeers)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves peer in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>The one wire peer advertisement produced by the operation.</returns>
    public OneWirePeerAdvertisement? GetPeer(string peerId) {
    try
    {
        return peers.TryGetValue(peerId, out var peer) ? Clone(peer) : null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(GetPeer)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(GetPeer)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs upsert in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peer">Peer value supplied to the one wire peer operation and used when producing its result.</param>
    public void Upsert(OneWirePeerAdvertisement peer)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(peer);
            ArgumentException.ThrowIfNullOrWhiteSpace(peer.PeerId);
            peer.SeenUtc = DateTimeOffset.UtcNow;
            peers.AddOrUpdate(peer.PeerId, _ => Clone(peer), (_, existing) =>
            {
                var connected = existing.IsConnected || peer.IsConnected;
                var replacement = Clone(peer);
                replacement.IsConnected = connected;
                return replacement;
            });
            logger.LogInformation("1-Wire peer {PeerId} advertised {CapabilityCount} capability entries.", peer.PeerId, peer.Capabilities.Count);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(Upsert)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(Upsert)} failed.");
        throw;
    }
}

    /// <summary>
    /// Sets connected in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="connected">Value indicating whether connected should apply to this operation.</param>
    public void SetConnected(string peerId, bool connected)
    {
    try
    {
            if (peers.TryGetValue(peerId, out var peer))
            {
                peer.IsConnected = connected;
                peer.SeenUtc = DateTimeOffset.UtcNow;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(SetConnected)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(SetConnected)} failed.");
        throw;
    }
}

    /// <summary>
    /// Removes expired in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="maximumAge">Maximum age value supplied to the one wire peer operation and used when producing its result.</param>
    public void RemoveExpired(TimeSpan maximumAge)
    {
    try
    {
            var cutoff = DateTimeOffset.UtcNow - maximumAge;
            foreach (var pair in peers.Where(pair => !pair.Value.IsConnected && pair.Value.SeenUtc < cutoff).ToArray())
                peers.TryRemove(pair.Key, out _);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(RemoveExpired)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(RemoveExpired)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone in the one wire peer directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peer">Peer value supplied to the one wire peer operation and used when producing its result.</param>
    /// <returns>The one wire peer advertisement produced by the operation.</returns>
    private OneWirePeerAdvertisement Clone(OneWirePeerAdvertisement peer) {
    try
    {
        return new()
    {
        PeerId = peer.PeerId,
        DisplayName = peer.DisplayName,
        Application = peer.Application,
        ApplicationVersion = peer.ApplicationVersion,
        HostName = peer.HostName,
        Address = peer.Address,
        ServicePort = peer.ServicePort,
        DiscoveryPort = peer.DiscoveryPort,
        WebBaseUrl = peer.WebBaseUrl,
        SeenUtc = peer.SeenUtc,
        IsConnected = peer.IsConnected,
        TransportKind = peer.TransportKind,
        SupportedTransports = peer.SupportedTransports.ToList(),
        Security = new OneWireSecurityDescriptor
        {
            HasRuntimeSecret = peer.Security.HasRuntimeSecret,
            SupportsSigning = peer.Security.SupportsSigning,
            SupportsEncryption = peer.Security.SupportsEncryption,
            SupportsMfaPairing = peer.Security.SupportsMfaPairing,
            KeyId = peer.Security.KeyId,
            Fingerprint = peer.Security.Fingerprint,
            KeyAgreementPublicKey = peer.Security.KeyAgreementPublicKey,
            SigningPublicKey = peer.Security.SigningPublicKey,
            PairingScheme = peer.Security.PairingScheme
        },
        Capabilities = peer.Capabilities.Select(capability => capability).ToList(),
        Skills = peer.Skills.Select(skill => skill).ToList(),
        UiFeatures = peer.UiFeatures.Select(feature => feature).ToList(),
        Hardware = peer.Hardware.Select(hardware => hardware).ToList()
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(Clone)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePeerRegistry)}.{nameof(Clone)} failed.");
        throw;
    }
}
}

/// <summary>
/// Maintains the authoritative directory of one wire connection entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireConnectionRegistry(ILogger<OneWireConnectionRegistry> logger) : IOneWireConnectionRegistry
{

    /// <summary>
    /// Stores the in-memory senders collection maintained internally by <see cref="OneWireConnectionRegistry"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, OneWireConnectionRegistration> senders = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Stores the internal registration gate state used by <see cref="OneWireConnectionRegistry"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object registrationGate = new();

    /// <summary>
    /// Performs register in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="sender">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    public void Register(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender) {
    try
    {
        RegisterOwned(peerId, sender);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(Register)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(Register)} failed.");
        throw;
    }
}

    /// <summary>
    /// Registers owned in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="sender">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The GUID produced by the operation.</returns>
    public Guid RegisterOwned(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
            ArgumentNullException.ThrowIfNull(sender);
            var registration = new OneWireConnectionRegistration(Guid.NewGuid(), sender);
            lock (registrationGate)
                senders[peerId] = registration;
            logger.LogInformation("Registered live 1-Wire connection {ConnectionId} for {PeerId}.", registration.Id, peerId);
            return registration.Id;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(RegisterOwned)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(RegisterOwned)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs unregister in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    public void Unregister(string peerId)
    {
    try
    {
            OneWireConnectionRegistration? removed = null;
            lock (registrationGate)
                senders.TryRemove(peerId, out removed);
            if (removed is not null)
                logger.LogInformation("Removed live 1-Wire connection {ConnectionId} for {PeerId}.", removed.Id, peerId);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(Unregister)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(Unregister)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs unregister in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="registrationId">Identifier of the registration to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Unregister(string peerId, Guid registrationId)
    {
    try
    {
            OneWireConnectionRegistration? removed = null;
            lock (registrationGate)
            {
                if (!senders.TryGetValue(peerId, out var current) || current.Id != registrationId)
                    return false;
                senders.TryRemove(peerId, out removed);
            }
            if (removed is not null)
                logger.LogInformation("Removed owned live 1-Wire connection {ConnectionId} for {PeerId}.", registrationId, peerId);
            return removed is not null;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(Unregister)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(Unregister)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether connected in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsConnected(string peerId) {
    try
    {
        return senders.ContainsKey(peerId);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(IsConnected)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireConnectionRegistry)}.{nameof(IsConnected)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs send in the one wire connection directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="envelope">Envelope value supplied to the one wire connection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> SendAsync(string peerId, OneWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!senders.TryGetValue(peerId, out var registration))
            return false;
        try
        {
            await registration.Sender(envelope, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
        {
            logger.LogWarning(ex, "Could not send a 1-Wire message to {PeerId}; connection {ConnectionId} will be removed if it is still current.", peerId, registration.Id);
            Unregister(peerId, registration.Id);
            return false;
        }
    }
}

/// <summary>
/// Represents an one wire replay guard application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="policyData">One wire replay policy data service dependency used by the one wire replay guard workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireReplayGuard(
    IOneWireReplayPolicyDataService policyData,
    ILogger<OneWireReplayGuard> logger) : IOneWireReplayGuard
{
    /// <summary>
    /// Stores the in-memory accepted collection maintained internally by <see cref="OneWireReplayGuard"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> accepted = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Stores the internal cleanup counter state used by <see cref="OneWireReplayGuard"/> while executing its surrounding workflow.
    /// </summary>
    private int cleanupCounter;

    /// <summary>
    /// Attempts to accept for <see cref="OneWireReplayGuard"/>, keeping the operation consistent with the state and invariants of the surrounding one wire replay guard workflow.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="messageId">Identifier of the message to use for this operation.</param>
    /// <param name="createdUtc">Created utc value supplied to the one wire replay guard operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryAccept(string peerId, Guid messageId, DateTimeOffset createdUtc)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
            if (messageId == Guid.Empty) return false;

            var now = DateTimeOffset.UtcNow;
            var policy = policyData.GetSnapshot();
            if (createdUtc < now - policy.Retention || createdUtc > now + policy.AllowedFutureSkew)
            {
                logger.LogWarning("Rejected 1-Wire message {MessageId} from {PeerId} because its timestamp is outside the accepted replay window.", messageId, peerId);
                return false;
            }

            var key = $"{peerId}\n{messageId:N}";
            if (!accepted.TryAdd(key, now.Add(policy.Retention)))
            {
                logger.LogWarning("Rejected replayed 1-Wire message {MessageId} from {PeerId}.", messageId, peerId);
                return false;
            }

            if (Interlocked.Increment(ref cleanupCounter) % policy.CleanupInterval == 0 || accepted.Count > policy.MaximumTrackedMessages)
            {
                foreach (var stale in accepted.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToArray())
                    accepted.TryRemove(stale, out _);
            }
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireReplayGuard)}.{nameof(TryAccept)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireReplayGuard)}.{nameof(TryAccept)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an one wire work spooler application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireWorkSpooler(ILogger<OneWireWorkSpooler> logger) : IOneWireWorkSpooler
{
    /// <summary>
    /// Stores the in-memory work items collection maintained internally by <see cref="OneWireWorkSpooler"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, OneWireWorkItem> workItems = new();
    /// <summary>
    /// Stores the internal queue state used by <see cref="OneWireWorkSpooler"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ConcurrentQueue<Guid> queue = new();
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to signal state owned by <see cref="OneWireWorkSpooler"/>.
    /// </summary>
    private readonly SemaphoreSlim signal = new(0);

    /// <summary>
    /// Performs enqueue for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire work spooler operation and used when producing its result.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    public OneWireWorkItem Enqueue(OneWireEnvelope envelope)
    {
    try
    {
            var item = new OneWireWorkItem
            {
                CorrelationId = envelope.CorrelationId,
                SourcePeerId = envelope.SourcePeerId,
                CapabilityKey = envelope.CapabilityKey,
                RequestType = envelope.MessageType,
                ExecutionMode = envelope.ExecutionMode,
                NotBeforeUtc = envelope.NotBeforeUtc,
                Request = envelope,
                Status = OneWireWorkStatus.Queued
            };
            workItems[item.Id] = item;
            queue.Enqueue(item.Id);
            signal.Release();
            logger.LogInformation("Queued 1-Wire work item {WorkItemId} for {CapabilityKey}.", item.Id, item.CapabilityKey);
            return item;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Enqueue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Enqueue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs dequeue for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    public async Task<OneWireWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
    try
    {
            while (true)
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!queue.TryDequeue(out var id) || !workItems.TryGetValue(id, out var item))
                    continue;
                if (item.NotBeforeUtc is { } notBefore && notBefore > DateTimeOffset.UtcNow)
                {
                    var delay = notBefore - DateTimeOffset.UtcNow;
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                return item;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(DequeueAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(DequeueAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves snapshot for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OneWireWorkItem> GetSnapshot() {
    try
    {
        return workItems.Values
        .OrderByDescending(item => item.CreatedUtc)
        .Take(250)
        .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(GetSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(GetSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs get for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    public OneWireWorkItem? Get(Guid id) {
    try
    {
        return workItems.GetValueOrDefault(id);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Get)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Get)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs mark running for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    public void MarkRunning(Guid id) {
    try
    {
        Mutate(id, item => item.Status = OneWireWorkStatus.Running);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(MarkRunning)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(MarkRunning)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs mark pending approval for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work spooler operation and used when producing its result.</param>
    public void MarkPendingApproval(Guid correlationId, string resultJson)
    {
    try
    {
            var item = FindByCorrelation(correlationId);
            if (item is null)
                return;
            Mutate(item.Id, current =>
            {
                current.Status = OneWireWorkStatus.PendingApproval;
                current.ResultJson = resultJson;
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(MarkPendingApproval)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(MarkPendingApproval)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs complete for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work spooler operation and used when producing its result.</param>
    public void Complete(Guid id, string resultJson) {
    try
    {
        Mutate(id, item =>
    {
        item.Status = OneWireWorkStatus.Completed;
        item.ResultJson = resultJson;
        item.Error = string.Empty;
    });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Complete)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Complete)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs fail for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="error">Error value supplied to the one wire work spooler operation and used when producing its result.</param>
    public void Fail(Guid id, string error) {
    try
    {
        Mutate(id, item =>
    {
        item.Status = OneWireWorkStatus.Failed;
        item.Error = error;
    });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Fail)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Fail)} failed.");
        throw;
    }
}

    /// <summary>
    /// Applies external result for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="resultJson">Result json value supplied to the one wire work spooler operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the one wire work spooler operation and used when producing its result.</param>
    /// <param name="status">Status value supplied to the one wire work spooler operation and used when producing its result.</param>
    public void ApplyExternalResult(Guid correlationId, string resultJson, string error, OneWireWorkStatus? status = null)
    {
    try
    {
            var item = FindByCorrelation(correlationId);
            if (item is null)
                return;
            if (status is OneWireWorkStatus.Declined)
            {
                Mutate(item.Id, current =>
                {
                    current.Status = OneWireWorkStatus.Declined;
                    current.ResultJson = resultJson;
                    current.Error = error;
                });
                return;
            }
            if (status is OneWireWorkStatus.Cancelled)
            {
                Mutate(item.Id, current =>
                {
                    current.Status = OneWireWorkStatus.Cancelled;
                    current.ResultJson = resultJson;
                    current.Error = error;
                });
                return;
            }
            if (string.IsNullOrWhiteSpace(error) && status is not OneWireWorkStatus.Failed)
                Complete(item.Id, resultJson);
            else
                Fail(item.Id, error);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(ApplyExternalResult)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(ApplyExternalResult)} failed.");
        throw;
    }
}

    /// <summary>
    /// Finds by correlation for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <returns>The one wire work item produced by the operation.</returns>
    private OneWireWorkItem? FindByCorrelation(Guid correlationId) {
    try
    {
        return workItems.Values
        .OrderByDescending(candidate => candidate.CreatedUtc)
        .FirstOrDefault(candidate => candidate.CorrelationId == correlationId);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(FindByCorrelation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(FindByCorrelation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs mutate for <see cref="OneWireWorkSpooler"/>, keeping the operation consistent with the state and invariants of the surrounding one wire work spooler workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="mutation">Mutation value supplied to the one wire work spooler operation and used when producing its result.</param>
    private void Mutate(Guid id, Action<OneWireWorkItem> mutation)
    {
    try
    {
            if (!workItems.TryGetValue(id, out var item))
                return;
            mutation(item);
            item.UpdatedUtc = DateTimeOffset.UtcNow;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Mutate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireWorkSpooler)}.{nameof(Mutate)} failed.");
        throw;
    }
}
}
/// <summary>
/// Owns persistence and retrieval of one wire pending council state, keeping storage-specific behavior behind a focused application abstraction.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWirePendingCouncilStore(
    ILogger<OneWirePendingCouncilStore> logger) : IOneWirePendingCouncilStore
{
    /// <summary>
    /// Stores the in-memory pending collection maintained internally by <see cref="OneWirePendingCouncilStore"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, OneWirePendingCouncilRequest> pending = new();

    /// <summary>
    /// Performs upsert in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="OneWirePendingCouncilStore"/>.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire pending council operation and used when producing its result.</param>
    /// <param name="approvalRequestId">Identifier of the approval request to use for this operation.</param>
    public void Upsert(OneWireEnvelope envelope, Guid? approvalRequestId)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(envelope);
            pending.AddOrUpdate(
                envelope.CorrelationId,
                _ => new OneWirePendingCouncilRequest
                {
                    Envelope = envelope,
                    ApprovalRequestId = approvalRequestId,
                    QueuedUtc = DateTimeOffset.UtcNow
                },
                (_, existing) =>
                {
                    existing.Envelope = envelope;
                    existing.ApprovalRequestId = approvalRequestId ?? existing.ApprovalRequestId;
                    return existing;
                });
            logger.LogTrace("Upserted pending 1-Wire council request {CorrelationId}.", envelope.CorrelationId);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(Upsert)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(Upsert)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves snapshot in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="OneWirePendingCouncilStore"/>.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OneWirePendingCouncilRequest> GetSnapshot()
    {
    try
    {
            var snapshot = pending.Values
                .OrderBy(item => item.QueuedUtc)
                .ToList();
            logger.LogTrace("Captured {PendingCount} pending 1-Wire council request(s).", snapshot.Count);
            return snapshot;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(GetSnapshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(GetSnapshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs remove in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="OneWirePendingCouncilStore"/>.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Remove(Guid correlationId, out OneWirePendingCouncilRequest? request)
    {
    try
    {
            var removed = pending.TryRemove(correlationId, out request);
            logger.LogTrace("Removed pending 1-Wire council request {CorrelationId}: {Removed}.", correlationId, removed);
            return removed;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(Remove)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(Remove)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs mark checked in the one wire pending council persistence workflow while keeping storage-specific behavior contained within <see cref="OneWirePendingCouncilStore"/>.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    public void MarkChecked(Guid correlationId)
    {
    try
    {
            if (pending.TryGetValue(correlationId, out var request))
            {
                request.LastCheckedUtc = DateTimeOffset.UtcNow;
                logger.LogTrace("Marked pending 1-Wire council request {CorrelationId} as checked.", correlationId);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(MarkChecked)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWirePendingCouncilStore)}.{nameof(MarkChecked)} failed.");
        throw;
    }
}
}

