using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IOneWireEnvelopeCodec
{
    string Serialize(OneWireEnvelope envelope, bool seal = true);
    OneWireEnvelope DeserializeAndValidate(string json);
    bool Validate(OneWireEnvelope envelope, out string error);
}


public interface IOneWireRuntimeSecurityService
{
    Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    Task RegenerateAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(CancellationToken cancellationToken = default);
    Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default);
    Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
    Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default);
    Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default);
    Task ProtectOutgoingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    Task UnprotectIncomingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface ILocalVisionOcrService
{
    Task<LocalVisionOcrResult> RecognizeAsync(LocalVisionOcrRequest request, CancellationToken cancellationToken = default);
}

public interface IOneWireCapabilityCatalog
{
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetLocalCapabilitiesForPeerAsync(string peerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetLocalSkillsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalGPT.WireProtocol.OneWireUiFeatureDescriptor>> GetLocalUiFeaturesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalGPT.WireProtocol.OneWireHardwareDescriptor>> GetLocalHardwareAsync(CancellationToken cancellationToken = default);
}

public interface IOneWirePeerRegistry
{
    IReadOnlyList<OneWirePeerAdvertisement> GetPeers();
    OneWirePeerAdvertisement? GetPeer(string peerId);
    void Upsert(OneWirePeerAdvertisement peer);
    void SetConnected(string peerId, bool connected);
    void RemoveExpired(TimeSpan maximumAge);
}

public interface IOneWireConnectionRegistry
{
    void Register(string peerId, Func<OneWireEnvelope, CancellationToken, Task> sender);
    void Unregister(string peerId);
    bool IsConnected(string peerId);
    Task<bool> SendAsync(string peerId, OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface IOneWireWorkSpooler
{
    OneWireWorkItem Enqueue(OneWireEnvelope envelope);
    Task<OneWireWorkItem> DequeueAsync(CancellationToken cancellationToken);
    IReadOnlyList<OneWireWorkItem> GetSnapshot();
    OneWireWorkItem? Get(Guid id);
    void MarkRunning(Guid id);
    void MarkPendingApproval(Guid correlationId, string resultJson);
    void Complete(Guid id, string resultJson);
    void Fail(Guid id, string error);
    void ApplyExternalResult(Guid correlationId, string resultJson, string error, OneWireWorkStatus? status = null);
}

public interface IOneWirePendingCouncilStore
{
    void Upsert(OneWireEnvelope envelope, Guid? approvalRequestId);
    IReadOnlyList<OneWirePendingCouncilRequest> GetSnapshot();
    bool Remove(Guid correlationId, out OneWirePendingCouncilRequest? request);
    void MarkChecked(Guid correlationId);
}

public interface IOneWireOperationExecutor
{
    Task<string> ExecuteAsync(OneWireWorkItem item, CancellationToken cancellationToken = default);
}

public interface IOneWireMessageDispatcher
{
    Task<OneWireEnvelope?> DispatchAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface IOrganicCouncilBlueprintService
{
    Task<IReadOnlyList<OrganicCouncilTeamDefinition>> GetTeamsAsync(CancellationToken cancellationToken = default);
    Task<OrganicCouncilTeamDefinition?> FindTeamAsync(string? key, CancellationToken cancellationToken = default);
    Task<string> BuildBriefingAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default);
    string BuildExpertPreparationPrompt(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team);
    string BuildLeaderSynthesisPrompt(MultiModelCouncilRequest request, OrganicCouncilTeamDefinition team, string preparation);
}

public interface IProjectOrganicContextService
{
    Task<ProjectOrganicContext> GetAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
    Task<ProjectOrganicContext> SaveAsync(Guid projectId, SaveProjectOrganicContextRequest request, CancellationToken cancellationToken = default);
    Task<string> BuildBriefingAsync(Guid projectId, Guid? revisionId, CancellationToken cancellationToken = default);
}
