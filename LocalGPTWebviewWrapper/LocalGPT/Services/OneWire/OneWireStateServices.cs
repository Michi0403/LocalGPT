using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;

namespace LocalGPT.Services.OneWire;

public sealed class OneWirePeerRegistry(ILogger<OneWirePeerRegistry> logger) : IOneWirePeerRegistry
{
    private readonly ConcurrentDictionary<string, OneWirePeerAdvertisement> peers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<OneWirePeerAdvertisement> GetPeers() => peers.Values
        .OrderByDescending(peer => peer.IsConnected)
        .ThenByDescending(peer => peer.SeenUtc)
        .Select(Clone)
        .ToList();

    public OneWirePeerAdvertisement? GetPeer(string peerId) => peers.TryGetValue(peerId, out var peer) ? Clone(peer) : null;

    public void Upsert(OneWirePeerAdvertisement peer)
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

    public void SetConnected(string peerId, bool connected)
    {
        if (peers.TryGetValue(peerId, out var peer))
        {
            peer.IsConnected = connected;
            peer.SeenUtc = DateTimeOffset.UtcNow;
        }
    }

    public void RemoveExpired(TimeSpan maximumAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maximumAge;
        foreach (var pair in peers.Where(pair => !pair.Value.IsConnected && pair.Value.SeenUtc < cutoff).ToArray())
            peers.TryRemove(pair.Key, out _);
    }

    private OneWirePeerAdvertisement Clone(OneWirePeerAdvertisement peer) => new()
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

public sealed class OneWireConnectionRegistry(ILogger<OneWireConnectionRegistry> logger) : IOneWireConnectionRegistry
{
    private sealed record ConnectionRegistration(Guid Id, Func<OneWireEnvelope, CancellationToken, Task> Sender);

    private readonly ConcurrentDictionary<string, ConnectionRegistration> senders = new(StringComparer.OrdinalIgnoreCase);
    private readonly object registrationGate = new();

    public void Register(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender) =>
        RegisterOwned(peerId, sender);

    public Guid RegisterOwned(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
        ArgumentNullException.ThrowIfNull(sender);
        var registration = new ConnectionRegistration(Guid.NewGuid(), sender);
        lock (registrationGate)
            senders[peerId] = registration;
        logger.LogInformation("Registered live 1-Wire connection {ConnectionId} for {PeerId}.", registration.Id, peerId);
        return registration.Id;
    }

    public void Unregister(string peerId)
    {
        ConnectionRegistration? removed = null;
        lock (registrationGate)
            senders.TryRemove(peerId, out removed);
        if (removed is not null)
            logger.LogInformation("Removed live 1-Wire connection {ConnectionId} for {PeerId}.", removed.Id, peerId);
    }

    public bool Unregister(string peerId, Guid registrationId)
    {
        ConnectionRegistration? removed = null;
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

    public bool IsConnected(string peerId) => senders.ContainsKey(peerId);

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
}

public sealed class OneWireWorkSpooler(ILogger<OneWireWorkSpooler> logger) : IOneWireWorkSpooler
{
    private readonly ConcurrentDictionary<Guid, OneWireWorkItem> workItems = new();
    private readonly ConcurrentQueue<Guid> queue = new();
    private readonly SemaphoreSlim signal = new(0);

    public OneWireWorkItem Enqueue(OneWireEnvelope envelope)
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

    public async Task<OneWireWorkItem> DequeueAsync(CancellationToken cancellationToken)
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

    public IReadOnlyList<OneWireWorkItem> GetSnapshot() => workItems.Values
        .OrderByDescending(item => item.CreatedUtc)
        .Take(250)
        .ToList();

    public OneWireWorkItem? Get(Guid id) => workItems.GetValueOrDefault(id);

    public void MarkRunning(Guid id) => Mutate(id, item => item.Status = OneWireWorkStatus.Running);

    public void MarkPendingApproval(Guid correlationId, string resultJson)
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

    public void Complete(Guid id, string resultJson) => Mutate(id, item =>
    {
        item.Status = OneWireWorkStatus.Completed;
        item.ResultJson = resultJson;
        item.Error = string.Empty;
    });

    public void Fail(Guid id, string error) => Mutate(id, item =>
    {
        item.Status = OneWireWorkStatus.Failed;
        item.Error = error;
    });

    public void ApplyExternalResult(Guid correlationId, string resultJson, string error, OneWireWorkStatus? status = null)
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

    private OneWireWorkItem? FindByCorrelation(Guid correlationId) => workItems.Values
        .OrderByDescending(candidate => candidate.CreatedUtc)
        .FirstOrDefault(candidate => candidate.CorrelationId == correlationId);

    private void Mutate(Guid id, Action<OneWireWorkItem> mutation)
    {
        if (!workItems.TryGetValue(id, out var item))
            return;
        mutation(item);
        item.UpdatedUtc = DateTimeOffset.UtcNow;
    }
}
public sealed class OneWirePendingCouncilStore : IOneWirePendingCouncilStore
{
    private readonly ConcurrentDictionary<Guid, OneWirePendingCouncilRequest> pending = new();

    public void Upsert(OneWireEnvelope envelope, Guid? approvalRequestId)
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
    }

    public IReadOnlyList<OneWirePendingCouncilRequest> GetSnapshot() => pending.Values
        .OrderBy(item => item.QueuedUtc)
        .ToList();

    public bool Remove(Guid correlationId, out OneWirePendingCouncilRequest? request) =>
        pending.TryRemove(correlationId, out request);

    public void MarkChecked(Guid correlationId)
    {
        if (pending.TryGetValue(correlationId, out var request))
            request.LastCheckedUtc = DateTimeOffset.UtcNow;
    }
}

