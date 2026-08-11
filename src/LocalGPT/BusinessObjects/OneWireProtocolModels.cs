namespace LocalGPT.BusinessObjects;

/// <summary>Local runtime state for a protocol work item. Transfer contracts live in LocalGPT.WireProtocolVersion.</summary>
public sealed class OneWireWorkItem
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets correlation identifier.
    /// </summary>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets source peer identifier.
    /// </summary>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets request type.
    /// </summary>
    public OneWireMessageType RequestType { get; set; }
    /// <summary>
    /// Gets or sets execution mode.
    /// </summary>
    public OneWireExecutionMode ExecutionMode { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public OneWireWorkStatus Status { get; set; } = OneWireWorkStatus.Queued;
    /// <summary>
    /// Gets or sets created UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets updated UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets not before UTC.
    /// </summary>
    public DateTimeOffset? NotBeforeUtc { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public OneWireEnvelope Request { get; set; } = new();
    /// <summary>
    /// Gets or sets result JSON.
    /// </summary>
    public string ResultJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents an one wire pending council request.
/// </summary>
public sealed class OneWirePendingCouncilRequest
{
    /// <summary>
    /// Gets or sets envelope.
    /// </summary>
    public OneWireEnvelope Envelope { get; set; } = new();
    /// <summary>
    /// Gets or sets approval request identifier.
    /// </summary>
    public Guid? ApprovalRequestId { get; set; }
    /// <summary>
    /// Gets or sets queued UTC.
    /// </summary>
    public DateTimeOffset QueuedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets last checked UTC.
    /// </summary>
    public DateTimeOffset LastCheckedUtc { get; set; }
}

/// <summary>
/// Represents an one wire options.
/// </summary>
public sealed class OneWireOptions
{
    /// <summary>
    /// Stores section name.
    /// </summary>
    public const string SectionName = "OneWire";
    /// <summary>
    /// Gets or sets enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets enable discovery.
    /// </summary>
    public bool EnableDiscovery { get; set; } = true;
    /// <summary>
    /// Gets or sets enable LAN transport.
    /// </summary>
    public bool EnableLanTransport { get; set; }
    /// <summary>
    /// Gets or sets listen address.
    /// </summary>
    public string ListenAddress { get; set; } = "127.0.0.1";
    /// <summary>
    /// Gets or sets service port.
    /// </summary>
    public int ServicePort { get; set; } = OneWireProtocol.DefaultServicePort;
    /// <summary>
    /// Gets or sets discovery port.
    /// </summary>
    public int DiscoveryPort { get; set; } = OneWireProtocol.DefaultDiscoveryPort;
    /// <summary>
    /// Gets or sets broadcast address.
    /// </summary>
    public string BroadcastAddress { get; set; } = "255.255.255.255";
    /// <summary>
    /// Gets or sets broadcast interval seconds.
    /// </summary>
    public int BroadcastIntervalSeconds { get; set; } = 5;
    /// <summary>
    /// Gets or sets peer expiry seconds.
    /// </summary>
    public int PeerExpirySeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets maximum message bytes.
    /// </summary>
    public int MaximumMessageBytes { get; set; } = OneWireProtocol.MaximumMessageBytes;
}


/// <summary>
/// Represents an one wire dispatch context.
/// </summary>
public sealed class OneWireDispatchContext
{
    /// <summary>
    /// Gets or sets authenticated peer identifier.
    /// </summary>
    public string AuthenticatedPeerId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets connection identifier.
    /// </summary>
    public Guid ConnectionId { get; init; }
    /// <summary>
    /// Gets or sets is internal.
    /// </summary>
    public bool IsInternal { get; init; }
    /// <summary>
    /// Gets or sets is loopback.
    /// </summary>
    public bool IsLoopback { get; init; }
    /// <summary>
    /// Gets or sets transport.
    /// </summary>
    public string Transport { get; init; } = string.Empty;


}


/// <summary>
/// Represents an one wire replay policy snapshot.
/// </summary>
public sealed class OneWireReplayPolicySnapshot
{
    /// <summary>
    /// Gets or sets retention.
    /// </summary>
    public TimeSpan Retention { get; init; }
    /// <summary>
    /// Gets or sets allowed future skew.
    /// </summary>
    public TimeSpan AllowedFutureSkew { get; init; }
    /// <summary>
    /// Gets or sets cleanup interval.
    /// </summary>
    public int CleanupInterval { get; init; }
    /// <summary>
    /// Gets or sets maximum tracked messages.
    /// </summary>
    public int MaximumTrackedMessages { get; init; }
}

/// <summary>
/// Represents a local vision ocr request.
/// </summary>
public sealed class LocalVisionOcrRequest
{
    /// <summary>
    /// Gets or sets image data URL.
    /// </summary>
    public string ImageDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets maximum output tokens.
    /// </summary>
    public int MaximumOutputTokens { get; set; } = 1600;
}

/// <summary>
/// Represents a local vision ocr result.
/// </summary>
public sealed class LocalVisionOcrResult
{
    /// <summary>
    /// Gets or sets text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets provider URI.
    /// </summary>
    public string ProviderUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets media type.
    /// </summary>
    public string MediaType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets needs human review.
    /// </summary>
    public bool NeedsHumanReview { get; set; } = true;
}

/// <summary>Describes the active LocalGPT 1-Wire surface and safe runtime settings for local and linked frontends.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class OneWireProtocolProfile
{
    /// <summary>
    /// Gets or sets protocol version.
    /// </summary>
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    /// <summary>
    /// Gets or sets minimum compatible version.
    /// </summary>
    public string MinimumCompatibleVersion { get; set; } = OneWireProtocol.MinimumCompatibleVersion;
    /// <summary>
    /// Gets or sets post envelope route.
    /// </summary>
    public string PostEnvelopeRoute { get; set; } = "/api/onewire/http-json";
    /// <summary>
    /// Gets or sets poll work route.
    /// </summary>
    public string PollWorkRoute { get; set; } = "/api/onewire/http-json/work/{correlationId}";
    /// <summary>
    /// Gets or sets settings.
    /// </summary>
    public OneWirePublicSettings Settings { get; set; } = new();
    /// <summary>
    /// Gets or sets security.
    /// </summary>
    public OneWireSecurityDescriptor Security { get; set; } = new();
    /// <summary>
    /// Gets or sets peer.
    /// </summary>
    public OneWirePeerAdvertisement Peer { get; set; } = new();
    /// <summary>
    /// Gets or sets capabilities.
    /// </summary>
    public List<OneWireCapabilityDescriptor> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets skills.
    /// </summary>
    public List<OneWireSkillDescriptor> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets UI features.
    /// </summary>
    public List<OneWireUiFeatureDescriptor> UiFeatures { get; set; } = [];
    /// <summary>
    /// Gets or sets hardware.
    /// </summary>
    public List<OneWireHardwareDescriptor> Hardware { get; set; } = [];
}

/// <summary>Safe LocalGPT 1-Wire settings shown to users and linked clients.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class OneWirePublicSettings
{
    /// <summary>
    /// Gets or sets enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets discovery enabled.
    /// </summary>
    public bool DiscoveryEnabled { get; set; }
    /// <summary>
    /// Gets or sets LAN transport enabled.
    /// </summary>
    public bool LanTransportEnabled { get; set; }
    /// <summary>
    /// Gets or sets listen address.
    /// </summary>
    public string ListenAddress { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets service port.
    /// </summary>
    public int ServicePort { get; set; }
    /// <summary>
    /// Gets or sets discovery port.
    /// </summary>
    public int DiscoveryPort { get; set; }
    /// <summary>
    /// Gets or sets broadcast interval seconds.
    /// </summary>
    public int BroadcastIntervalSeconds { get; set; }
    /// <summary>
    /// Gets or sets peer expiry seconds.
    /// </summary>
    public int PeerExpirySeconds { get; set; }
    /// <summary>
    /// Gets or sets maximum message bytes.
    /// </summary>
    public int MaximumMessageBytes { get; set; }
    /// <summary>
    /// Gets or sets supported transports.
    /// </summary>
    public List<string> SupportedTransports { get; set; } = ["tcp", "http-json"];
}
