namespace LocalGPT.BusinessObjects;

/// <summary>Local runtime state for a protocol work item. Transfer contracts live in LocalGPT.WireProtocolVersion.</summary>
public sealed class OneWireWorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; }
    public string SourcePeerId { get; set; } = string.Empty;
    public string CapabilityKey { get; set; } = string.Empty;
    public OneWireMessageType RequestType { get; set; }
    public OneWireExecutionMode ExecutionMode { get; set; }
    public OneWireWorkStatus Status { get; set; } = OneWireWorkStatus.Queued;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NotBeforeUtc { get; set; }
    public OneWireEnvelope Request { get; set; } = new();
    public string ResultJson { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class OneWirePendingCouncilRequest
{
    public OneWireEnvelope Envelope { get; set; } = new();
    public Guid? ApprovalRequestId { get; set; }
    public DateTimeOffset QueuedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastCheckedUtc { get; set; }
}

public sealed class OneWireOptions
{
    public const string SectionName = "OneWire";
    public bool Enabled { get; set; } = true;
    public bool EnableDiscovery { get; set; } = true;
    public int ServicePort { get; set; } = OneWireProtocol.DefaultServicePort;
    public int DiscoveryPort { get; set; } = OneWireProtocol.DefaultDiscoveryPort;
    public string BroadcastAddress { get; set; } = "255.255.255.255";
    public int BroadcastIntervalSeconds { get; set; } = 5;
    public int PeerExpirySeconds { get; set; } = 30;
    public int MaximumMessageBytes { get; set; } = OneWireProtocol.MaximumMessageBytes;
}
