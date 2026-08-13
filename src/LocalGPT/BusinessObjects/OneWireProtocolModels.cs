namespace LocalGPT.BusinessObjects;

/// <summary>Local runtime state for a protocol work item. Transfer contracts live in LocalGPT.WireProtocolVersion.</summary>
public sealed class OneWireWorkItem
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this one wire work item instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="OneWireWorkItem"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this one wire work item instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="OneWireWorkItem"/>.</value>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this one wire work item instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="OneWireWorkItem"/>.</value>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this one wire work item instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="OneWireWorkItem"/>.</value>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the request type value that forms part of the one wire work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request type value exposed by <see cref="OneWireWorkItem"/>.</value>
    public OneWireMessageType RequestType { get; set; }
    /// <summary>
    /// Gets or sets the execution mode value that forms part of the one wire work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The execution mode value exposed by <see cref="OneWireWorkItem"/>.</value>
    public OneWireExecutionMode ExecutionMode { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the one wire work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="OneWireWorkItem"/>.</value>
    public OneWireWorkStatus Status { get; set; } = OneWireWorkStatus.Queued;
    /// <summary>
    /// Gets or sets the created UTC associated with this one wire work item state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OneWireWorkItem"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the updated UTC associated with this one wire work item state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated UTC value exposed by <see cref="OneWireWorkItem"/>.</value>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the not before UTC associated with this one wire work item state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The not before UTC value exposed by <see cref="OneWireWorkItem"/>.</value>
    public DateTimeOffset? NotBeforeUtc { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the one wire work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="OneWireWorkItem"/>.</value>
    public OneWireEnvelope Request { get; set; } = new();
    /// <summary>
    /// Gets or sets the result JSON value that forms part of the one wire work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The result JSON value exposed by <see cref="OneWireWorkItem"/>.</value>
    public string ResultJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the one wire work item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="OneWireWorkItem"/>.</value>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for one wire pending council, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class OneWirePendingCouncilRequest
{
    /// <summary>
    /// Gets or sets the envelope value that forms part of the one wire pending council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The envelope value exposed by <see cref="OneWirePendingCouncilRequest"/>.</value>
    public OneWireEnvelope Envelope { get; set; } = new();
    /// <summary>
    /// Gets or sets the stable approval request identifier used to identify or correlate this one wire pending council instance with related application state.
    /// </summary>
    /// <value>The approval request identifier value exposed by <see cref="OneWirePendingCouncilRequest"/>.</value>
    public Guid? ApprovalRequestId { get; set; }
    /// <summary>
    /// Gets or sets the queued UTC associated with this one wire pending council state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The queued UTC value exposed by <see cref="OneWirePendingCouncilRequest"/>.</value>
    public DateTimeOffset QueuedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the last checked UTC associated with this one wire pending council state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last checked UTC value exposed by <see cref="OneWirePendingCouncilRequest"/>.</value>
    public DateTimeOffset LastCheckedUtc { get; set; }
}

/// <summary>
/// Carries the configurable one wire settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class OneWireOptions
{
    /// <summary>
    /// Defines the section name constant used by <see cref="OneWireOptions"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SectionName = "OneWire";
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the one wire state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OneWireOptions"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether discovery applies to the one wire state.
    /// </summary>
    /// <value>The enable discovery value exposed by <see cref="OneWireOptions"/>.</value>
    public bool EnableDiscovery { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether LAN transport applies to the one wire state.
    /// </summary>
    /// <value>The enable LAN transport value exposed by <see cref="OneWireOptions"/>.</value>
    public bool EnableLanTransport { get; set; }
    /// <summary>
    /// Gets or sets the listen address that identifies the network or application endpoint associated with this one wire state.
    /// </summary>
    /// <value>The listen address value exposed by <see cref="OneWireOptions"/>.</value>
    public string ListenAddress { get; set; } = "127.0.0.1";
    /// <summary>
    /// Gets or sets the service port value that forms part of the one wire state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The service port value exposed by <see cref="OneWireOptions"/>.</value>
    public int ServicePort { get; set; } = OneWireProtocol.DefaultServicePort;
    /// <summary>
    /// Gets or sets the discovery port value that forms part of the one wire state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery port value exposed by <see cref="OneWireOptions"/>.</value>
    public int DiscoveryPort { get; set; } = OneWireProtocol.DefaultDiscoveryPort;
    /// <summary>
    /// Gets or sets the broadcast address that identifies the network or application endpoint associated with this one wire state.
    /// </summary>
    /// <value>The broadcast address value exposed by <see cref="OneWireOptions"/>.</value>
    public string BroadcastAddress { get; set; } = "255.255.255.255";
    /// <summary>
    /// Gets or sets the broadcast interval seconds value that forms part of the one wire state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The broadcast interval seconds value exposed by <see cref="OneWireOptions"/>.</value>
    public int BroadcastIntervalSeconds { get; set; } = 5;
    /// <summary>
    /// Gets or sets the peer expiry seconds value that forms part of the one wire state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The peer expiry seconds value exposed by <see cref="OneWireOptions"/>.</value>
    public int PeerExpirySeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets the maximum message bytes value that forms part of the one wire state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum message bytes value exposed by <see cref="OneWireOptions"/>.</value>
    public int MaximumMessageBytes { get; set; } = OneWireProtocol.MaximumMessageBytes;
}


/// <summary>
/// Represents an one wire dispatch context application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireDispatchContext
{
    /// <summary>
    /// Gets or sets the stable authenticated peer identifier used to identify or correlate this one wire dispatch context instance with related application state.
    /// </summary>
    /// <value>The authenticated peer identifier value exposed by <see cref="OneWireDispatchContext"/>.</value>
    public string AuthenticatedPeerId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable connection identifier used to identify or correlate this one wire dispatch context instance with related application state.
    /// </summary>
    /// <value>The connection identifier value exposed by <see cref="OneWireDispatchContext"/>.</value>
    public Guid ConnectionId { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether internal applies to the one wire dispatch context state.
    /// </summary>
    /// <value>The is internal value exposed by <see cref="OneWireDispatchContext"/>.</value>
    public bool IsInternal { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether loopback applies to the one wire dispatch context state.
    /// </summary>
    /// <value>The is loopback value exposed by <see cref="OneWireDispatchContext"/>.</value>
    public bool IsLoopback { get; init; }
    /// <summary>
    /// Gets or sets the transport value that forms part of the one wire dispatch context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport value exposed by <see cref="OneWireDispatchContext"/>.</value>
    public string Transport { get; init; } = string.Empty;


}


/// <summary>
/// Represents an one wire replay policy snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireReplayPolicySnapshot
{
    /// <summary>
    /// Gets or sets the retention duration used to control timing in the one wire replay policy snapshot workflow.
    /// </summary>
    /// <value>The retention value exposed by <see cref="OneWireReplayPolicySnapshot"/>.</value>
    public TimeSpan Retention { get; init; }
    /// <summary>
    /// Gets or sets the allowed future skew duration used to control timing in the one wire replay policy snapshot workflow.
    /// </summary>
    /// <value>The allowed future skew value exposed by <see cref="OneWireReplayPolicySnapshot"/>.</value>
    public TimeSpan AllowedFutureSkew { get; init; }
    /// <summary>
    /// Gets or sets the cleanup interval duration used to control timing in the one wire replay policy snapshot workflow.
    /// </summary>
    /// <value>The cleanup interval value exposed by <see cref="OneWireReplayPolicySnapshot"/>.</value>
    public int CleanupInterval { get; init; }
    /// <summary>
    /// Gets or sets the maximum tracked messages value that forms part of the one wire replay policy snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum tracked messages value exposed by <see cref="OneWireReplayPolicySnapshot"/>.</value>
    public int MaximumTrackedMessages { get; init; }
}

/// <summary>
/// Represents the input contract for local vision OCR, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class LocalVisionOcrRequest
{
    /// <summary>
    /// Gets or sets the image data URL that identifies the network or application endpoint associated with this local vision OCR state.
    /// </summary>
    /// <value>The image data URL value exposed by <see cref="LocalVisionOcrRequest"/>.</value>
    public string ImageDataUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prompt value that forms part of the local vision OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="LocalVisionOcrRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model name value that forms part of the local vision OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="LocalVisionOcrRequest"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the maximum output tokens value that forms part of the local vision OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum output tokens value exposed by <see cref="LocalVisionOcrRequest"/>.</value>
    public int MaximumOutputTokens { get; set; } = 1600;
}

/// <summary>
/// Represents the outcome of local vision OCR, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class LocalVisionOcrResult
{
    /// <summary>
    /// Gets or sets the text value that forms part of the local vision OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="LocalVisionOcrResult"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model name value that forms part of the local vision OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="LocalVisionOcrResult"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider URI that identifies the network or application endpoint associated with this local vision OCR state.
    /// </summary>
    /// <value>The provider URI value exposed by <see cref="LocalVisionOcrResult"/>.</value>
    public string ProviderUri { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the media type value that forms part of the local vision OCR state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media type value exposed by <see cref="LocalVisionOcrResult"/>.</value>
    public string MediaType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether human review applies to the local vision OCR state.
    /// </summary>
    /// <value>The needs human review value exposed by <see cref="LocalVisionOcrResult"/>.</value>
    public bool NeedsHumanReview { get; set; } = true;
}

/// <summary>Describes the active LocalGPT 1-Wire surface and safe runtime settings for local and linked frontends.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class OneWireProtocolProfile
{
    /// <summary>
    /// Gets or sets the protocol version value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol version value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    /// <summary>
    /// Gets or sets the minimum compatible version value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum compatible version value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public string MinimumCompatibleVersion { get; set; } = OneWireProtocol.MinimumCompatibleVersion;
    /// <summary>
    /// Gets or sets the post envelope route value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The post envelope route value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public string PostEnvelopeRoute { get; set; } = "/api/onewire/http-json";
    /// <summary>
    /// Gets or sets the poll work route value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The poll work route value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public string PollWorkRoute { get; set; } = "/api/onewire/http-json/work/{correlationId}";
    /// <summary>
    /// Gets or sets the settings value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The settings value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public OneWirePublicSettings Settings { get; set; } = new();
    /// <summary>
    /// Gets or sets the security value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The security value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public OneWireSecurityDescriptor Security { get; set; } = new();
    /// <summary>
    /// Gets or sets the peer value that forms part of the one wire protocol profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The peer value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public OneWirePeerAdvertisement Peer { get; set; } = new();
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this one wire protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public List<OneWireCapabilityDescriptor> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this one wire protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public List<OneWireSkillDescriptor> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI features collection maintained or exposed by this one wire protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The UI features value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public List<OneWireUiFeatureDescriptor> UiFeatures { get; set; } = [];
    /// <summary>
    /// Gets or sets the hardware collection maintained or exposed by this one wire protocol profile instance for downstream processing.
    /// </summary>
    /// <value>The hardware value exposed by <see cref="OneWireProtocolProfile"/>.</value>
    public List<OneWireHardwareDescriptor> Hardware { get; set; } = [];
}

/// <summary>Safe LocalGPT 1-Wire settings shown to users and linked clients.</summary>
[DocumentationUpdated("2.3.6")]
public sealed class OneWirePublicSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the one wire public state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether discovery enabled applies to the one wire public state.
    /// </summary>
    /// <value>The discovery enabled value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public bool DiscoveryEnabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether LAN transport enabled applies to the one wire public state.
    /// </summary>
    /// <value>The LAN transport enabled value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public bool LanTransportEnabled { get; set; }
    /// <summary>
    /// Gets or sets the listen address that identifies the network or application endpoint associated with this one wire public state.
    /// </summary>
    /// <value>The listen address value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public string ListenAddress { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the service port value that forms part of the one wire public state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The service port value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public int ServicePort { get; set; }
    /// <summary>
    /// Gets or sets the discovery port value that forms part of the one wire public state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery port value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public int DiscoveryPort { get; set; }
    /// <summary>
    /// Gets or sets the broadcast interval seconds value that forms part of the one wire public state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The broadcast interval seconds value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public int BroadcastIntervalSeconds { get; set; }
    /// <summary>
    /// Gets or sets the peer expiry seconds value that forms part of the one wire public state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The peer expiry seconds value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public int PeerExpirySeconds { get; set; }
    /// <summary>
    /// Gets or sets the maximum message bytes value that forms part of the one wire public state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum message bytes value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public int MaximumMessageBytes { get; set; }
    /// <summary>
    /// Gets or sets the supported transports collection maintained or exposed by this one wire public instance for downstream processing.
    /// </summary>
    /// <value>The supported transports value exposed by <see cref="OneWirePublicSettings"/>.</value>
    public List<string> SupportedTransports { get; set; } = ["tcp", "http-json"];
}
