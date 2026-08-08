using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;

namespace LocalGPT.Services.OneWire;

public sealed class OneWirePeerRegistry(ILogger<OneWirePeerRegistry> logger) : IOneWirePeerRegistry
{
    private readonly ConcurrentDictionary<string, OneWirePeerAdvertisement> peers = new(StringComparer.OrdinalIgnoreCase);

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

public sealed class OneWireConnectionRegistry(ILogger<OneWireConnectionRegistry> logger) : IOneWireConnectionRegistry
{

    private readonly ConcurrentDictionary<string, OneWireConnectionRegistration> senders = new(StringComparer.OrdinalIgnoreCase);
    private readonly object registrationGate = new();

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

public sealed class OneWireReplayGuard(
    IOneWireReplayPolicyDataService policyData,
    ILogger<OneWireReplayGuard> logger) : IOneWireReplayGuard
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> accepted = new(StringComparer.OrdinalIgnoreCase);
    private int cleanupCounter;

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

public sealed class OneWireWorkSpooler(ILogger<OneWireWorkSpooler> logger) : IOneWireWorkSpooler
{
    private readonly ConcurrentDictionary<Guid, OneWireWorkItem> workItems = new();
    private readonly ConcurrentQueue<Guid> queue = new();
    private readonly SemaphoreSlim signal = new(0);

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
public sealed class OneWirePendingCouncilStore(
    ILogger<OneWirePendingCouncilStore> logger) : IOneWirePendingCouncilStore
{
    private readonly ConcurrentDictionary<Guid, OneWirePendingCouncilRequest> pending = new();

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

