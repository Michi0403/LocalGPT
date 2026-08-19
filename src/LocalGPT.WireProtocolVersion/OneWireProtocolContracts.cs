using System.Text.Json;

namespace LocalGPT.WireProtocol;

/// <summary>Stable constants and compatibility checks for the embedded protocol assembly.</summary>
public static class OneWireProtocol
{
    /// <summary>
    /// Defines the version constant used by <see cref="OneWireProtocol"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Version = "2.1";
    /// <summary>
    /// Defines the minimum compatible version constant used by <see cref="OneWireProtocol"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string MinimumCompatibleVersion = "2.0";
    /// <summary>
    /// Defines the default service port constant used by <see cref="OneWireProtocol"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const int DefaultServicePort = 51140;
    /// <summary>
    /// Defines the default discovery port constant used by <see cref="OneWireProtocol"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const int DefaultDiscoveryPort = 51141;
    /// <summary>
    /// Defines the maximum message bytes constant used by <see cref="OneWireProtocol"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const int MaximumMessageBytes = 8 * 1024 * 1024;
    /// <summary>
    /// Defines the maximum discovery bytes constant used by <see cref="OneWireProtocol"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const int MaximumDiscoveryBytes = 32 * 1024;

    /// <summary>
    /// Determines whether compatible for <see cref="OneWireProtocol"/>, keeping the operation consistent with the state and invariants of the surrounding one wire protocol workflow.
    /// </summary>
    /// <param name="version">Version value supplied to the one wire protocol operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public static bool IsCompatible(string? version)
    {
        if (!System.Version.TryParse(version, out var candidate) || !System.Version.TryParse(Version, out var current))
            return false;
        return candidate.Major == current.Major && candidate.Minor <= current.Minor;
    }
}

/// <summary>
/// Defines the supported one wire message type values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireMessageType
{
    /// <summary>
    /// Selects the hello option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Hello,
    /// <summary>
    /// Selects the hello ack option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HelloAck,
    /// <summary>
    /// Selects the capability request option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CapabilityRequest,
    /// <summary>
    /// Selects the capability response option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CapabilityResponse,
    /// <summary>
    /// Selects the skill request option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SkillRequest,
    /// <summary>
    /// Selects the skill response option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SkillResponse,
    /// <summary>
    /// Selects the skill state update option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SkillStateUpdate,
    /// <summary>
    /// Selects the invoke option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Invoke,
    /// <summary>
    /// Selects the council request option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CouncilRequest,
    /// <summary>
    /// Selects the work accepted option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WorkAccepted,
    /// <summary>
    /// Selects the work status request option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WorkStatusRequest,
    /// <summary>
    /// Selects the work result option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    WorkResult,
    /// <summary>
    /// Selects the interaction result option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    InteractionResult,
    /// <summary>
    /// Selects the approval required option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ApprovalRequired,
    /// <summary>
    /// Selects the permission update option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PermissionUpdate,
    /// <summary>
    /// Selects the link request option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LinkRequest,
    /// <summary>
    /// Selects the link status option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LinkStatus,
    /// <summary>
    /// Selects the link revoked option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LinkRevoked,
    /// <summary>
    /// Selects the security profile request option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SecurityProfileRequest,
    /// <summary>
    /// Selects the security profile response option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SecurityProfileResponse,
    /// <summary>
    /// Selects the MFA challenge option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MfaChallenge,
    /// <summary>
    /// Selects the MFA proof option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MfaProof,
    /// <summary>
    /// Selects the trust established option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TrustEstablished,
    /// <summary>
    /// Selects the trust revoked option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TrustRevoked,
    /// <summary>
    /// Selects the error option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Error,
    /// <summary>
    /// Selects the ping option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Ping,
    /// <summary>
    /// Selects the pong option for <see cref="OneWireMessageType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Pong
}

/// <summary>
/// Defines the supported one wire execution mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireExecutionMode { Once, SequentialSpool, Scheduled, Recurring }
/// <summary>
/// Defines the supported one wire transport kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireTransportKind { Tcp, Http, Mqtt, Uart, Spi, Custom }
/// <summary>
/// Defines the supported one wire security mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireSecurityMode { None, Signed, EncryptedAndSigned }
/// <summary>
/// Defines the supported one wire trust level values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireTrustLevel { Untrusted, Discovered, Linked, MfaVerified, Trusted }
/// <summary>
/// Defines the supported one wire work status values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireWorkStatus { PendingApproval, Queued, Running, Completed, Failed, Declined, Cancelled }
/// <summary>
/// Defines the supported one wire approval mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireApprovalMode { AskEveryTime, SameCapability, CurrentWorkOrder, AlwaysAllow, Deny }
/// <summary>
/// Defines the supported one wire hardware kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireHardwareKind { Auto, Cpu, Gpu, Accelerator, Remote }
/// <summary>
/// Defines the supported one wire interaction kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireInteractionKind { None, Human, Automated, HumanAndAutomated }
/// <summary>
/// Defines the supported one wire UI feature values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireUiFeatureState { Hidden, Disabled, Enabled }
/// <summary>
/// Defines the supported one wire interaction editor values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum OneWireInteractionEditor { None, ConfirmationOnly, PlainText, RichText, Json }

/// <summary>
/// Bidirectional interaction contract. "Target system" always means the receiver of the current envelope,
/// so the same contract works for LocalGPT-to-plugin and plugin-to-LocalGPT calls without direction-specific DTOs.
/// </summary>
public interface IOneWireInteractionContract
{
    /// <summary>
    /// Gets or sets a value indicating whether requires human interaction on target system applies to the one wire interaction contract state.
    /// </summary>
    /// <value>The requires human interaction on target system value exposed by <see cref="IOneWireInteractionContract"/>.</value>
    bool RequiresHumanInteractionOnTargetSystem { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires automated interaction on target system applies to the one wire interaction contract state.
    /// </summary>
    /// <value>The requires automated interaction on target system value exposed by <see cref="IOneWireInteractionContract"/>.</value>
    bool RequiresAutomatedInteractionOnTargetSystem { get; set; }
    /// <summary>
    /// Gets or sets the interaction kind value that forms part of the one wire interaction contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction kind value exposed by <see cref="IOneWireInteractionContract"/>.</value>
    OneWireInteractionKind InteractionKind { get; set; }
    /// <summary>
    /// Gets or sets the interaction value JSON value that forms part of the one wire interaction contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction value JSON value exposed by <see cref="IOneWireInteractionContract"/>.</value>
    string? InteractionValueJson { get; set; }
    /// <summary>
    /// Gets or sets the interaction value content type value that forms part of the one wire interaction contract state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction value content type value exposed by <see cref="IOneWireInteractionContract"/>.</value>
    string InteractionValueContentType { get; set; }
}

/// <summary>
/// Interface contract implemented by every transferable 1-Wire envelope. The interaction fields are
/// deliberately target-oriented, making the same fields valid in both LocalGPT-to-plugin and plugin-to-LocalGPT directions.
/// </summary>
public interface IOneWireEnvelope : IOneWireInteractionContract
{
    /// <summary>
    /// Gets or sets the protocol version value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol version value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string ProtocolVersion { get; set; }
    /// <summary>
    /// Gets or sets the stable message identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The message identifier value exposed by <see cref="IOneWireEnvelope"/>.</value>
    Guid MessageId { get; set; }
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="IOneWireEnvelope"/>.</value>
    Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the stable reply to message identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The reply to message identifier value exposed by <see cref="IOneWireEnvelope"/>.</value>
    Guid? ReplyToMessageId { get; set; }
    /// <summary>
    /// Gets or sets the message type value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message type value exposed by <see cref="IOneWireEnvelope"/>.</value>
    OneWireMessageType MessageType { get; set; }
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string SourcePeerId { get; set; }
    /// <summary>
    /// Gets or sets the stable target peer identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The target peer identifier value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string TargetPeerId { get; set; }
    /// <summary>
    /// Gets or sets the created UTC associated with this one wire envelope state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="IOneWireEnvelope"/>.</value>
    DateTimeOffset CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the expires UTC associated with this one wire envelope state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The expires UTC value exposed by <see cref="IOneWireEnvelope"/>.</value>
    DateTimeOffset? ExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets the sequence value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="IOneWireEnvelope"/>.</value>
    int Sequence { get; set; }
    /// <summary>
    /// Gets or sets the execution mode value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The execution mode value exposed by <see cref="IOneWireEnvelope"/>.</value>
    OneWireExecutionMode ExecutionMode { get; set; }
    /// <summary>
    /// Gets or sets the controller value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string Controller { get; set; }
    /// <summary>
    /// Gets or sets the method value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string Method { get; set; }
    /// <summary>
    /// Gets or sets the route value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string Route { get; set; }
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string CapabilityKey { get; set; }
    /// <summary>
    /// Gets or sets the organs collection maintained or exposed by this one wire envelope instance for downstream processing.
    /// </summary>
    /// <value>The organs value exposed by <see cref="IOneWireEnvelope"/>.</value>
    List<string> Organs { get; set; }
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this one wire envelope instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="IOneWireEnvelope"/>.</value>
    List<string> Skills { get; set; }
    /// <summary>
    /// Gets or sets the properties collection maintained or exposed by this one wire envelope instance for downstream processing.
    /// </summary>
    /// <value>The properties value exposed by <see cref="IOneWireEnvelope"/>.</value>
    Dictionary<string, JsonElement>? Properties { get; set; }
    /// <summary>
    /// Gets or sets the encrypted payload value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encrypted payload value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string? EncryptedPayload { get; set; }
    /// <summary>
    /// Gets or sets the security mode value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The security mode value exposed by <see cref="IOneWireEnvelope"/>.</value>
    OneWireSecurityMode SecurityMode { get; set; }
    /// <summary>
    /// Gets or sets the stable security key identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The security key identifier value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string SecurityKeyId { get; set; }
    /// <summary>
    /// Gets or sets the encryption nonce value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encryption nonce value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string? EncryptionNonce { get; set; }
    /// <summary>
    /// Gets or sets the authentication tag value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authentication tag value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string? AuthenticationTag { get; set; }
    /// <summary>
    /// Gets or sets the signature value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The signature value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string? Signature { get; set; }
    /// <summary>
    /// Gets or sets the hash value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hash value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string? Hash { get; set; }
    /// <summary>
    /// Gets or sets the error check value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error check value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string? ErrorCheck { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the one wire envelope state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="IOneWireEnvelope"/>.</value>
    bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets the approval mode value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The approval mode value exposed by <see cref="IOneWireEnvelope"/>.</value>
    OneWireApprovalMode ApprovalMode { get; set; }
    /// <summary>
    /// Gets or sets the stable work order key used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The work order key value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string WorkOrderKey { get; set; }
    /// <summary>
    /// Gets or sets the not before UTC associated with this one wire envelope state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The not before UTC value exposed by <see cref="IOneWireEnvelope"/>.</value>
    DateTimeOffset? NotBeforeUtc { get; set; }
    /// <summary>
    /// Gets or sets the workflow JSON value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workflow JSON value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string WorkflowJson { get; set; }
    /// <summary>
    /// Gets or sets the error value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="IOneWireEnvelope"/>.</value>
    string Error { get; set; }
}

/// <summary>
/// Represents an one wire envelope application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireEnvelope : IOneWireEnvelope
{
    /// <summary>
    /// Gets or sets the protocol version value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol version value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    /// <summary>
    /// Gets or sets the stable message identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The message identifier value exposed by <see cref="OneWireEnvelope"/>.</value>
    public Guid MessageId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable correlation identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The correlation identifier value exposed by <see cref="OneWireEnvelope"/>.</value>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable reply to message identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The reply to message identifier value exposed by <see cref="OneWireEnvelope"/>.</value>
    public Guid? ReplyToMessageId { get; set; }
    /// <summary>
    /// Gets or sets the message type value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message type value exposed by <see cref="OneWireEnvelope"/>.</value>
    public OneWireMessageType MessageType { get; set; }
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable target peer identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The target peer identifier value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string TargetPeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created UTC associated with this one wire envelope state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OneWireEnvelope"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the expires UTC associated with this one wire envelope state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The expires UTC value exposed by <see cref="OneWireEnvelope"/>.</value>
    public DateTimeOffset? ExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets the sequence value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sequence value exposed by <see cref="OneWireEnvelope"/>.</value>
    public int Sequence { get; set; }
    /// <summary>
    /// Gets or sets the execution mode value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The execution mode value exposed by <see cref="OneWireEnvelope"/>.</value>
    public OneWireExecutionMode ExecutionMode { get; set; } = OneWireExecutionMode.Once;
    /// <summary>
    /// Gets or sets the controller value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the method value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string Method { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the route value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the organs collection maintained or exposed by this one wire envelope instance for downstream processing.
    /// </summary>
    /// <value>The organs value exposed by <see cref="OneWireEnvelope"/>.</value>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this one wire envelope instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="OneWireEnvelope"/>.</value>
    public List<string> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the properties collection maintained or exposed by this one wire envelope instance for downstream processing.
    /// </summary>
    /// <value>The properties value exposed by <see cref="OneWireEnvelope"/>.</value>
    public Dictionary<string, JsonElement>? Properties { get; set; }
    /// <summary>
    /// Gets or sets the encrypted payload value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encrypted payload value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? EncryptedPayload { get; set; }
    /// <summary>
    /// Gets or sets the security mode value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The security mode value exposed by <see cref="OneWireEnvelope"/>.</value>
    public OneWireSecurityMode SecurityMode { get; set; } = OneWireSecurityMode.None;
    /// <summary>
    /// Gets or sets the stable security key identifier used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The security key identifier value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string SecurityKeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the encryption nonce value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The encryption nonce value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? EncryptionNonce { get; set; }
    /// <summary>
    /// Gets or sets the authentication tag value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The authentication tag value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? AuthenticationTag { get; set; }
    /// <summary>
    /// Gets or sets the signature value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The signature value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? Signature { get; set; }
    /// <summary>
    /// Gets or sets the hash value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hash value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? Hash { get; set; }
    /// <summary>
    /// Gets or sets the error check value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error check value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? ErrorCheck { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the one wire envelope state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="OneWireEnvelope"/>.</value>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets the approval mode value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The approval mode value exposed by <see cref="OneWireEnvelope"/>.</value>
    public OneWireApprovalMode ApprovalMode { get; set; } = OneWireApprovalMode.AskEveryTime;
    /// <summary>
    /// Gets or sets the stable work order key used to identify or correlate this one wire envelope instance with related application state.
    /// </summary>
    /// <value>The work order key value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string WorkOrderKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the not before UTC associated with this one wire envelope state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The not before UTC value exposed by <see cref="OneWireEnvelope"/>.</value>
    public DateTimeOffset? NotBeforeUtc { get; set; }
    /// <summary>
    /// Gets or sets workflow JSON.
    /// </summary>
    /// <value>The workflow JSON value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string WorkflowJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the one wire envelope state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string Error { get; set; } = string.Empty;

    /// <summary>True when the receiving application must involve its local human before completion.</summary>
    /// <value>The requires human interaction on target system value exposed by <see cref="OneWireEnvelope"/>.</value>
    public bool RequiresHumanInteractionOnTargetSystem { get; set; }

    /// <summary>True when the receiving application must run an automated local interaction before completion.</summary>
    /// <value>The requires automated interaction on target system value exposed by <see cref="OneWireEnvelope"/>.</value>
    public bool RequiresAutomatedInteractionOnTargetSystem { get; set; }

    /// <summary>Derived kind retained on the wire so receivers do not have to infer combined interaction requirements.</summary>
    /// <value>The interaction kind value exposed by <see cref="OneWireEnvelope"/>.</value>
    public OneWireInteractionKind InteractionKind { get; set; } = OneWireInteractionKind.None;

    /// <summary>Bidirectional serialized context/result value for the requested human or automated interaction.</summary>
    /// <value>The interaction value JSON value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string? InteractionValueJson { get; set; }

    /// <summary>Media type of InteractionValueJson; defaults to JSON but may identify text or a referenced binary manifest.</summary>
    /// <value>The interaction value content type value exposed by <see cref="OneWireEnvelope"/>.</value>
    public string InteractionValueContentType { get; set; } = "application/json";

    /// <summary>
    /// Normalizes interaction kind for <see cref="OneWireEnvelope"/>, keeping the operation consistent with the state and invariants of the surrounding one wire envelope workflow.
    /// </summary>
    public void NormalizeInteractionKind()
    {
        InteractionKind = (RequiresHumanInteractionOnTargetSystem, RequiresAutomatedInteractionOnTargetSystem) switch
        {
            (true, true) => OneWireInteractionKind.HumanAndAutomated,
            (true, false) => OneWireInteractionKind.Human,
            (false, true) => OneWireInteractionKind.Automated,
            _ => OneWireInteractionKind.None
        };
    }
}

/// <summary>
/// Represents one wire capability state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OneWireCapabilityDescriptor
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this one wire capability instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the controller value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The controller value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the method value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string Method { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the route value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter schema JSON value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter schema JSON value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    /// <summary>
    /// Gets or sets the organs collection maintained or exposed by this one wire capability instance for downstream processing.
    /// </summary>
    /// <value>The organs value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this one wire capability instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public List<string> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the required skill keys collection maintained or exposed by this one wire capability instance for downstream processing.
    /// </summary>
    /// <value>The required skill keys value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public List<string> RequiredSkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI activation keys collection maintained or exposed by this one wire capability instance for downstream processing.
    /// </summary>
    /// <value>The UI activation keys value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether online applies to the one wire capability state.
    /// </summary>
    /// <value>The is online value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the one wire capability state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the one wire capability state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires human confirmation applies to the one wire capability state.
    /// </summary>
    /// <value>The requires human confirmation value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool RequiresHumanConfirmation { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether scheduling applies to the one wire capability state.
    /// </summary>
    /// <value>The supports scheduling value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool SupportsScheduling { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether recurring execution applies to the one wire capability state.
    /// </summary>
    /// <value>The supports recurring execution value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool SupportsRecurringExecution { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires human interaction on target system applies to the one wire capability state.
    /// </summary>
    /// <value>The requires human interaction on target system value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool RequiresHumanInteractionOnTargetSystem { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires automated interaction on target system applies to the one wire capability state.
    /// </summary>
    /// <value>The requires automated interaction on target system value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool RequiresAutomatedInteractionOnTargetSystem { get; set; }
    /// <summary>
    /// Gets or sets the interaction value schema JSON value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction value schema JSON value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string InteractionValueSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    /// <summary>Human-readable description of required inputs, suitable for Council prompt teaching and small external clients.</summary>
    /// <value>The input contract value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string InputContract { get; set; } = string.Empty;
    /// <summary>Human-readable description of the produced result.</summary>
    /// <value>The output contract value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string OutputContract { get; set; } = string.Empty;
    /// <summary>Security and approval behavior that every Council member must respect.</summary>
    /// <value>The security contract value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string SecurityContract { get; set; } = string.Empty;
    /// <summary>Typical organic use case, such as eyes, hands, OCR or reviewed text feedback.</summary>
    /// <value>The organic use case value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string OrganicUseCase { get; set; } = string.Empty;
    /// <summary>Suggested Council roles or model abilities, for example OCR-capable vision members.</summary>
    /// <value>The suggested council roles value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public List<string> SuggestedCouncilRoles { get; set; } = [];
    /// <summary>Whether this capability is currently advertised to a securely linked peer.</summary>
    /// <value>The is exposed to peer value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool IsExposedToPeer { get; set; } = true;
    /// <summary>Whether the receiver may invoke this capability after its local policy and confirmation checks pass.</summary>
    /// <value>The allow peer invocation value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool AllowPeerInvocation { get; set; } = true;
    /// <summary>The receiving frontend is the authoritative confirmation surface for consequential work.</summary>
    /// <value>The requires frontend user confirmation value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public bool RequiresFrontendUserConfirmation { get; set; }
    /// <summary>Preferred frontend editor for human input that is returned through InteractionValueJson.</summary>
    /// <value>The interaction editor value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>Stable key of the local catalog entry that controls peer exposure and confirmation policy.</summary>
    /// <value>The configuration key value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string ConfigurationKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source value that forms part of the one wire capability state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="OneWireCapabilityDescriptor"/>.</value>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Represents one wire skill state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OneWireSkillDescriptor
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this one wire skill instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the one wire skill state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable source peer identifier used to identify or correlate this one wire skill instance with related application state.
    /// </summary>
    /// <value>The source peer identifier value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the organs collection maintained or exposed by this one wire skill instance for downstream processing.
    /// </summary>
    /// <value>The organs value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets the capability keys collection maintained or exposed by this one wire skill instance for downstream processing.
    /// </summary>
    /// <value>The capability keys value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public List<string> CapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI activation keys collection maintained or exposed by this one wire skill instance for downstream processing.
    /// </summary>
    /// <value>The UI activation keys value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether online applies to the one wire skill state.
    /// </summary>
    /// <value>The is online value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the one wire skill state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the updated UTC associated with this one wire skill state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated UTC value exposed by <see cref="OneWireSkillDescriptor"/>.</value>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an one wire peer advertisement application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWirePeerAdvertisement
{
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this one wire peer advertisement instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the application value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string Application { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the application version value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application version value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string ApplicationVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the host name value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The host name value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string HostName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the address that identifies the network or application endpoint associated with this one wire peer advertisement state.
    /// </summary>
    /// <value>The address value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string Address { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the service port value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The service port value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public int ServicePort { get; set; }
    /// <summary>
    /// Gets or sets the discovery port value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The discovery port value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public int DiscoveryPort { get; set; }
    /// <summary>
    /// Gets or sets the web base URL that identifies the network or application endpoint associated with this one wire peer advertisement state.
    /// </summary>
    /// <value>The web base URL value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public string WebBaseUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the seen UTC associated with this one wire peer advertisement state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The seen UTC value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public DateTimeOffset SeenUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether connected applies to the one wire peer advertisement state.
    /// </summary>
    /// <value>The is connected value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public bool IsConnected { get; set; }
    /// <summary>
    /// Gets or sets the transport kind value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport kind value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public OneWireTransportKind TransportKind { get; set; } = OneWireTransportKind.Tcp;
    /// <summary>
    /// Gets or sets the supported transports collection maintained or exposed by this one wire peer advertisement instance for downstream processing.
    /// </summary>
    /// <value>The supported transports value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public List<string> SupportedTransports { get; set; } = ["tcp", "http-json"];
    /// <summary>
    /// Gets or sets the security value that forms part of the one wire peer advertisement state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The security value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public OneWireSecurityDescriptor Security { get; set; } = new();
    /// <summary>
    /// Gets or sets the capabilities collection maintained or exposed by this one wire peer advertisement instance for downstream processing.
    /// </summary>
    /// <value>The capabilities value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public List<OneWireCapabilityDescriptor> Capabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this one wire peer advertisement instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public List<OneWireSkillDescriptor> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the UI features collection maintained or exposed by this one wire peer advertisement instance for downstream processing.
    /// </summary>
    /// <value>The UI features value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public List<OneWireUiFeatureDescriptor> UiFeatures { get; set; } = [];
    /// <summary>
    /// Gets or sets the hardware collection maintained or exposed by this one wire peer advertisement instance for downstream processing.
    /// </summary>
    /// <value>The hardware value exposed by <see cref="OneWirePeerAdvertisement"/>.</value>
    public List<OneWireHardwareDescriptor> Hardware { get; set; } = [];
}


/// <summary>Compact public security metadata safe to advertise during discovery and handshake.</summary>
public sealed class OneWireSecurityDescriptor
{
    /// <summary>
    /// Gets or sets a value indicating whether runtime secret applies to the one wire security state.
    /// </summary>
    /// <value>The has runtime secret value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public bool HasRuntimeSecret { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether signing applies to the one wire security state.
    /// </summary>
    /// <value>The supports signing value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public bool SupportsSigning { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether encryption applies to the one wire security state.
    /// </summary>
    /// <value>The supports encryption value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public bool SupportsEncryption { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether MFA pairing applies to the one wire security state.
    /// </summary>
    /// <value>The supports MFA pairing value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public bool SupportsMfaPairing { get; set; } = true;
    /// <summary>
    /// Gets or sets the stable key identifier used to identify or correlate this one wire security instance with related application state.
    /// </summary>
    /// <value>The key identifier value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fingerprint value that forms part of the one wire security state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fingerprint value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key agreement public key used to identify or correlate this one wire security instance with related application state.
    /// </summary>
    /// <value>The key agreement public key value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable signing public key used to identify or correlate this one wire security instance with related application state.
    /// </summary>
    /// <value>The signing public key value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the pairing scheme value that forms part of the one wire security state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The pairing scheme value exposed by <see cref="OneWireSecurityDescriptor"/>.</value>
    public string PairingScheme { get; set; } = "onewire-pair-v1";
}

/// <summary>Serializable pairing ticket. It contains public material only and is suitable for QR/barcode transport.</summary>
public sealed class OneWirePairingTicket
{
    /// <summary>
    /// Gets or sets the scheme value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scheme value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string Scheme { get; set; } = "onewire-pair-v1";
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this one wire pairing ticket instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the application value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string Application { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the protocol version value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The protocol version value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    /// <summary>
    /// Gets or sets the stable key identifier used to identify or correlate this one wire pairing ticket instance with related application state.
    /// </summary>
    /// <value>The key identifier value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fingerprint value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fingerprint value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key agreement public key used to identify or correlate this one wire pairing ticket instance with related application state.
    /// </summary>
    /// <value>The key agreement public key value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable signing public key used to identify or correlate this one wire pairing ticket instance with related application state.
    /// </summary>
    /// <value>The signing public key value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created UTC associated with this one wire pairing ticket state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the expires UTC associated with this one wire pairing ticket state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The expires UTC value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public DateTimeOffset ExpiresUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(10);
    /// <summary>
    /// Gets or sets the nonce value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The nonce value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string Nonce { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the signature value that forms part of the one wire pairing ticket state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The signature value exposed by <see cref="OneWirePairingTicket"/>.</value>
    public string Signature { get; set; } = string.Empty;
}

/// <summary>Runtime-only security status shown by each application frontend. No private key material is exposed.</summary>
public sealed class OneWireRuntimeSecurityStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether secret applies to the one wire runtime security status state.
    /// </summary>
    /// <value>The has secret value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public bool HasSecret { get; set; }
    /// <summary>
    /// Gets or sets the secret path used by this one wire runtime security status instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The secret path value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public string SecretPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key identifier used to identify or correlate this one wire runtime security status instance with related application state.
    /// </summary>
    /// <value>The key identifier value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fingerprint value that forms part of the one wire runtime security status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fingerprint value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created UTC associated with this one wire runtime security status state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public DateTimeOffset? CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the rotated UTC associated with this one wire runtime security status state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The rotated UTC value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public DateTimeOffset? RotatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the trusted peer count that quantifies the associated one wire runtime security status data.
    /// </summary>
    /// <value>The trusted peer count value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public int TrustedPeerCount { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether MFA enrolled applies to the one wire runtime security status state.
    /// </summary>
    /// <value>The MFA enrolled value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public bool MfaEnrolled { get; set; }
    /// <summary>
    /// Gets or sets the warning value that forms part of the one wire runtime security status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The warning value exposed by <see cref="OneWireRuntimeSecurityStatus"/>.</value>
    public string Warning { get; set; } = string.Empty;
}

/// <summary>Frontend request used when a user imports a public pairing ticket and authorizes trust.</summary>
public sealed class OneWireTrustEstablishmentRequest
{
    /// <summary>
    /// Gets or sets the ticket value that forms part of the one wire trust establishment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The ticket value exposed by <see cref="OneWireTrustEstablishmentRequest"/>.</value>
    public OneWirePairingTicket Ticket { get; set; } = new();
    /// <summary>
    /// Gets or sets the MFA code value that forms part of the one wire trust establishment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MFA code value exposed by <see cref="OneWireTrustEstablishmentRequest"/>.</value>
    public string MfaCode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the valid for minutes value that forms part of the one wire trust establishment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The valid for minutes value exposed by <see cref="OneWireTrustEstablishmentRequest"/>.</value>
    public int ValidForMinutes { get; set; } = 1440;
}

/// <summary>Persisted trust metadata. Private keys and MFA seeds never belong in this transferable contract.</summary>
public sealed class OneWireTrustedPeerDescriptor
{
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this one wire trusted peer instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire trusted peer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fingerprint value that forms part of the one wire trusted peer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fingerprint value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key agreement public key used to identify or correlate this one wire trusted peer instance with related application state.
    /// </summary>
    /// <value>The key agreement public key value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable signing public key used to identify or correlate this one wire trusted peer instance with related application state.
    /// </summary>
    /// <value>The signing public key value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the trust level value that forms part of the one wire trusted peer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trust level value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public OneWireTrustLevel TrustLevel { get; set; } = OneWireTrustLevel.Untrusted;
    /// <summary>
    /// Gets or sets the trusted UTC associated with this one wire trusted peer state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The trusted UTC value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public DateTimeOffset TrustedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the valid until UTC associated with this one wire trusted peer state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The valid until UTC value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public DateTimeOffset? ValidUntilUtc { get; set; }
    /// <summary>
    /// Gets or sets the MFA verified until UTC associated with this one wire trusted peer state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The MFA verified until UTC value exposed by <see cref="OneWireTrustedPeerDescriptor"/>.</value>
    public DateTimeOffset? MfaVerifiedUntilUtc { get; set; }
}

/// <summary>Encrypted payload body kept intentionally simple for .NET, ESP32 and future transport adapters.</summary>
public sealed class OneWireSensitivePayload
{
    /// <summary>
    /// Gets or sets the properties collection maintained or exposed by this one wire sensitive payload instance for downstream processing.
    /// </summary>
    /// <value>The properties value exposed by <see cref="OneWireSensitivePayload"/>.</value>
    public Dictionary<string, JsonElement>? Properties { get; set; }
    /// <summary>
    /// Gets or sets the interaction value JSON value that forms part of the one wire sensitive payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction value JSON value exposed by <see cref="OneWireSensitivePayload"/>.</value>
    public string? InteractionValueJson { get; set; }
    /// <summary>
    /// Gets or sets the interaction value content type value that forms part of the one wire sensitive payload state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction value content type value exposed by <see cref="OneWireSensitivePayload"/>.</value>
    public string InteractionValueContentType { get; set; } = "application/json";
    /// <summary>
    /// Gets or sets workflow JSON.
    /// </summary>
    /// <value>The workflow JSON value exposed by <see cref="OneWireSensitivePayload"/>.</value>
    public string WorkflowJson { get; set; } = string.Empty;
}


/// <summary>
/// Represents one wire UI feature state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OneWireUiFeatureDescriptor
{
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this one wire UI feature instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the one wire UI feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the state value that forms part of the one wire UI feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The state value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public OneWireUiFeatureState State { get; set; } = OneWireUiFeatureState.Hidden;
    /// <summary>
    /// Gets or sets the reason value that forms part of the one wire UI feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the required capability keys collection maintained or exposed by this one wire UI feature instance for downstream processing.
    /// </summary>
    /// <value>The required capability keys value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public List<string> RequiredCapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the required skill keys collection maintained or exposed by this one wire UI feature instance for downstream processing.
    /// </summary>
    /// <value>The required skill keys value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public List<string> RequiredSkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets the updated UTC associated with this one wire UI feature state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated UTC value exposed by <see cref="OneWireUiFeatureDescriptor"/>.</value>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents one wire hardware state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class OneWireHardwareDescriptor
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the one wire hardware state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public OneWireHardwareKind Kind { get; set; } = OneWireHardwareKind.Auto;
    /// <summary>
    /// Gets or sets the index value that forms part of the one wire hardware state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The index value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public int Index { get; set; } = -1;
    /// <summary>
    /// Gets or sets the name value that forms part of the one wire hardware state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the vendor value that forms part of the one wire hardware state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vendor value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public string Vendor { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the dedicated memory bytes value that forms part of the one wire hardware state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The dedicated memory bytes value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public long? DedicatedMemoryBytes { get; set; }
    /// <summary>
    /// Gets or sets the logical processor count that quantifies the associated one wire hardware data.
    /// </summary>
    /// <value>The logical processor count value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public int LogicalProcessorCount { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether online applies to the one wire hardware state.
    /// </summary>
    /// <value>The is online value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets the stable lane key used to identify or correlate this one wire hardware instance with related application state.
    /// </summary>
    /// <value>The lane key value exposed by <see cref="OneWireHardwareDescriptor"/>.</value>
    public string LaneKey => Kind == OneWireHardwareKind.Auto
        ? $"auto:{Name}"
        : $"{Kind.ToString().ToLowerInvariant()}:{Index}:{Name}";
}

/// <summary>
/// Represents an one wire model self assessment application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireModelSelfAssessment
{
    /// <summary>
    /// Gets or sets the model name value that forms part of the one wire model self assessment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable member key used to identify or correlate this one wire model self assessment instance with related application state.
    /// </summary>
    /// <value>The member key value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public string MemberKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the DevExpress functions collection maintained or exposed by this one wire model self assessment instance for downstream processing.
    /// </summary>
    /// <value>The DevExpress functions value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public List<string> DxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets the controller methods collection maintained or exposed by this one wire model self assessment instance for downstream processing.
    /// </summary>
    /// <value>The controller methods value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public List<string> ControllerMethods { get; set; } = [];
    /// <summary>
    /// Gets or sets the organic capabilities collection maintained or exposed by this one wire model self assessment instance for downstream processing.
    /// </summary>
    /// <value>The organic capabilities value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public List<string> OrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the skills collection maintained or exposed by this one wire model self assessment instance for downstream processing.
    /// </summary>
    /// <value>The skills value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public List<string> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets the confidence value that forms part of the one wire model self assessment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confidence value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public int Confidence { get; set; } = 50;
    /// <summary>
    /// Gets or sets the evidence value that forms part of the one wire model self assessment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The evidence value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public string Evidence { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reported UTC associated with this one wire model self assessment state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The reported UTC value exposed by <see cref="OneWireModelSelfAssessment"/>.</value>
    public DateTimeOffset ReportedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an one wire recurring execution application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireRecurringExecution
{
    /// <summary>
    /// Gets or sets the interval seconds value that forms part of the one wire recurring execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interval seconds value exposed by <see cref="OneWireRecurringExecution"/>.</value>
    public int IntervalSeconds { get; set; } = 15;
    /// <summary>
    /// Gets or sets the debounce milliseconds value that forms part of the one wire recurring execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The debounce milliseconds value exposed by <see cref="OneWireRecurringExecution"/>.</value>
    public int DebounceMilliseconds { get; set; } = 750;
    /// <summary>
    /// Gets or sets the maximum pending executions value that forms part of the one wire recurring execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum pending executions value exposed by <see cref="OneWireRecurringExecution"/>.</value>
    public int MaximumPendingExecutions { get; set; } = 1;
    /// <summary>
    /// Gets or sets the stop after UTC associated with this one wire recurring execution state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The stop after UTC value exposed by <see cref="OneWireRecurringExecution"/>.</value>
    public DateTimeOffset? StopAfterUtc { get; set; }
    /// <summary>
    /// Gets or sets the maximum executions value that forms part of the one wire recurring execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum executions value exposed by <see cref="OneWireRecurringExecution"/>.</value>
    public int? MaximumExecutions { get; set; }
}

/// <summary>
/// Represents an one wire permission rule application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWirePermissionRule
{
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this one wire permission rule instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable capability key used to identify or correlate this one wire permission rule instance with related application state.
    /// </summary>
    /// <value>The capability key value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the organ value that forms part of the one wire permission rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The organ value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public string Organ { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the approval mode value that forms part of the one wire permission rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The approval mode value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public OneWireApprovalMode ApprovalMode { get; set; } = OneWireApprovalMode.AskEveryTime;
    /// <summary>Controls whether the capability is advertised to this linked peer.</summary>
    /// <value>The is exposed value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public bool IsExposed { get; set; } = true;
    /// <summary>Controls whether an advertised capability can be invoked by this peer.</summary>
    /// <value>The allow invocation value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public bool AllowInvocation { get; set; } = true;
    /// <summary>Forces the receiving application's local frontend confirmation path even when a reusable approval mode exists.</summary>
    /// <value>The requires frontend confirmation value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public bool RequiresFrontendConfirmation { get; set; } = true;
    /// <summary>Editor shown to the local user when the request also needs human-provided information.</summary>
    /// <value>The interaction editor value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>Only a peer linked by an explicit frontend action may use this rule.</summary>
    /// <value>The require linked peer value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public bool RequireLinkedPeer { get; set; } = true;
    /// <summary>
    /// Gets or sets the stable work order key used to identify or correlate this one wire permission rule instance with related application state.
    /// </summary>
    /// <value>The work order key value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public string WorkOrderKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the updated UTC associated with this one wire permission rule state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated UTC value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the updated by value that forms part of the one wire permission rule state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The updated by value exposed by <see cref="OneWirePermissionRule"/>.</value>
    public string UpdatedBy { get; set; } = "CurrentUser";
}

/// <summary>
/// Represents an one wire council model route application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OneWireCouncilModelRoute
{
    /// <summary>Provider-qualified participant key used by Council scheduling and saved presets.</summary>
    /// <value>The model name value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>Canonical provider kind such as ollama, openai-compatible, openai or azure-openai.</summary>
    /// <value>The provider kind value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string ProviderKind { get; set; } = string.Empty;
    /// <summary>Human-readable provider name retained for diagnostics and user interfaces.</summary>
    /// <value>The provider name value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>Exact provider endpoint used to disambiguate same-named models.</summary>
    /// <value>The provider endpoint value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string ProviderEndpoint { get; set; } = string.Empty;
    /// <summary>Model or deployment name understood by the selected provider.</summary>
    /// <value>The provider model name value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string ProviderModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hardware kind value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware kind value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;
    /// <summary>
    /// Gets or sets the hardware index value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware index value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int HardwareIndex { get; set; } = -1;
    /// <summary>
    /// Gets or sets the hardware name value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hardware name value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string HardwareName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the min output tokens value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The min output tokens value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int MinOutputTokens { get; set; } = 256;
    /// <summary>
    /// Gets or sets the max output tokens value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max output tokens value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Gets or sets the min context tokens value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The min context tokens value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int MinContextTokens { get; set; } = 2048;
    /// <summary>
    /// Gets or sets the max context tokens value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max context tokens value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int MaxContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets the Ollama num GPU value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The Ollama num GPU value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int? OllamaNumGpu { get; set; }
    /// <summary>Optional per-model override for the session load slider. Null uses the session-wide percentage.</summary>
    /// <value>The load percent override value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int? LoadPercentOverride { get; set; }
    /// <summary>
    /// Gets or sets the self reported DevExpress functions collection maintained or exposed by this one wire council model route instance for downstream processing.
    /// </summary>
    /// <value>The self reported DevExpress functions value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public List<string> SelfReportedDxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets the self reported controller methods collection maintained or exposed by this one wire council model route instance for downstream processing.
    /// </summary>
    /// <value>The self reported controller methods value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public List<string> SelfReportedControllerMethods { get; set; } = [];
    /// <summary>
    /// Gets or sets the self reported organic capabilities collection maintained or exposed by this one wire council model route instance for downstream processing.
    /// </summary>
    /// <value>The self reported organic capabilities value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public List<string> SelfReportedOrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the self reported skills collection maintained or exposed by this one wire council model route instance for downstream processing.
    /// </summary>
    /// <value>The self reported skills value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public List<string> SelfReportedSkills { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the one wire council model route state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the max concurrent models on lane value that forms part of the one wire council model route state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max concurrent models on lane value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public int MaxConcurrentModelsOnLane { get; set; } = 1;
    /// <summary>
    /// Gets the stable lane key used to identify or correlate this one wire council model route instance with related application state.
    /// </summary>
    /// <value>The lane key value exposed by <see cref="OneWireCouncilModelRoute"/>.</value>
    public string LaneKey => HardwareKind == OneWireHardwareKind.Auto
        ? $"auto:{ModelName}"
        : $"{HardwareKind.ToString().ToLowerInvariant()}:{HardwareIndex}:{HardwareName}";
}

/// <summary>
/// Represents the input contract for one wire council, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class OneWireCouncilRequest
{
    /// <summary>
    /// Gets or sets the prompt value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable team key used to identify or correlate this one wire council instance with related application state.
    /// </summary>
    /// <value>The team key value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public string TeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets the leader model name value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The leader model name value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public string LeaderModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model names collection maintained or exposed by this one wire council instance for downstream processing.
    /// </summary>
    /// <value>The model names value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public List<string> ModelNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the model routes collection maintained or exposed by this one wire council instance for downstream processing.
    /// </summary>
    /// <value>The model routes value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];
    /// <summary>
    /// Gets or sets the requested organic capabilities collection maintained or exposed by this one wire council instance for downstream processing.
    /// </summary>
    /// <value>The requested organic capabilities value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public List<string> RequestedOrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets the external project context JSON value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The external project context JSON value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public string ExternalProjectContextJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this one wire council instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project topic identifier used to identify or correlate this one wire council instance with related application state.
    /// </summary>
    /// <value>The project topic identifier value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets the stable project revision identifier used to identify or correlate this one wire council instance with related application state.
    /// </summary>
    /// <value>The project revision identifier value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the max rounds value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max rounds value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public int MaxRounds { get; set; } = 1;
    /// <summary>
    /// Gets or sets the max output tokens value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max output tokens value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Gets or sets the max context tokens value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max context tokens value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public int MaxContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets the max parallel models value that forms part of the one wire council state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max parallel models value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public int MaxParallelModels { get; set; } = 1;
    /// <summary>
    /// Gets or sets a value indicating whether parallel hardware roads applies to the one wire council state.
    /// </summary>
    /// <value>The allow parallel hardware roads value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public bool AllowParallelHardwareRoads { get; set; } = true;
    /// <summary>Session-wide 0..100 load position interpolated between every model road's own minimum and maximum.</summary>
    /// <value>The resource load percent value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public int ResourceLoadPercent { get; set; } = 30;
    /// <summary>
    /// Gets or sets a value indicating whether memory applies to the one wire council state.
    /// </summary>
    /// <value>The include memory value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public bool IncludeMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether save to memory applies to the one wire council state.
    /// </summary>
    /// <value>The save to memory value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public bool SaveToMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether generate implementation artifact applies to the one wire council state.
    /// </summary>
    /// <value>The generate implementation artifact value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public bool GenerateImplementationArtifact { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed artifact build applies to the one wire council state.
    /// </summary>
    /// <value>The user confirmed artifact build value exposed by <see cref="OneWireCouncilRequest"/>.</value>
    public bool UserConfirmedArtifactBuild { get; set; }
}

/// <summary>Capability-provider abstraction implemented by LocalGPT and organic plugin systems.</summary>
public interface IOneWireCapabilityProvider
{
    /// <summary>
    /// Retrieves capabilities for <see cref="IOneWireCapabilityProvider"/>, keeping the operation consistent with the state and invariants of the surrounding one wire capability workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves skills for <see cref="IOneWireCapabilityProvider"/>, keeping the operation consistent with the state and invariants of the surrounding one wire capability workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves UI features for <see cref="IOneWireCapabilityProvider"/>, keeping the operation consistent with the state and invariants of the surrounding one wire capability workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OneWireUiFeatureDescriptor>>([]);
    /// <summary>
    /// Retrieves hardware for <see cref="IOneWireCapabilityProvider"/>, keeping the operation consistent with the state and invariants of the surrounding one wire capability workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OneWireHardwareDescriptor>>([]);
}

/// <summary>Transport-neutral adapter boundary for TCP now and UART/SPI/MQTT adapters later.</summary>
public interface IOneWireTransportAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the transport name value that forms part of the one wire transport adapter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport name value exposed by <see cref="IOneWireTransportAdapter"/>.</value>
    string TransportName { get; }
    /// <summary>
    /// Gets a value indicating whether connected applies to the one wire transport adapter state.
    /// </summary>
    /// <value>The is connected value exposed by <see cref="IOneWireTransportAdapter"/>.</value>
    bool IsConnected { get; }
    /// <summary>
    /// Performs send for <see cref="IOneWireTransportAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport adapter workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire transport adapter operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task SendAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs receive for <see cref="IOneWireTransportAdapter"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport adapter workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i async enumerable one wire envelope produced by the operation.</returns>
    IAsyncEnumerable<OneWireEnvelope> ReceiveAsync(CancellationToken cancellationToken = default);
}
