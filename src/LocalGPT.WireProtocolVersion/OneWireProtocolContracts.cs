using System.Text.Json;

namespace LocalGPT.WireProtocol;

/// <summary>Stable constants and compatibility checks for the embedded protocol assembly.</summary>
public static class OneWireProtocol
{
    /// <summary>
    /// Stores version.
    /// </summary>
    public const string Version = "2.1";
    /// <summary>
    /// Stores minimum compatible version.
    /// </summary>
    public const string MinimumCompatibleVersion = "2.0";
    /// <summary>
    /// Stores default service port.
    /// </summary>
    public const int DefaultServicePort = 51140;
    /// <summary>
    /// Stores default discovery port.
    /// </summary>
    public const int DefaultDiscoveryPort = 51141;
    /// <summary>
    /// Stores maximum message bytes.
    /// </summary>
    public const int MaximumMessageBytes = 8 * 1024 * 1024;
    /// <summary>
    /// Stores maximum discovery bytes.
    /// </summary>
    public const int MaximumDiscoveryBytes = 32 * 1024;

    /// <summary>
    /// Determines whether compatible.
    /// </summary>
    public static bool IsCompatible(string? version)
    {
        if (!System.Version.TryParse(version, out var candidate) || !System.Version.TryParse(Version, out var current))
            return false;
        return candidate.Major == current.Major && candidate.Minor <= current.Minor;
    }
}

/// <summary>
/// Lists supported one wire message type values.
/// </summary>
public enum OneWireMessageType
{
    Hello,
    HelloAck,
    CapabilityRequest,
    CapabilityResponse,
    SkillRequest,
    SkillResponse,
    SkillStateUpdate,
    Invoke,
    CouncilRequest,
    WorkAccepted,
    WorkStatusRequest,
    WorkResult,
    InteractionResult,
    ApprovalRequired,
    PermissionUpdate,
    LinkRequest,
    LinkStatus,
    LinkRevoked,
    SecurityProfileRequest,
    SecurityProfileResponse,
    MfaChallenge,
    MfaProof,
    TrustEstablished,
    TrustRevoked,
    Error,
    Ping,
    Pong
}

/// <summary>
/// Lists supported one wire execution mode values.
/// </summary>
public enum OneWireExecutionMode { Once, SequentialSpool, Scheduled, Recurring }
/// <summary>
/// Lists supported one wire transport kind values.
/// </summary>
public enum OneWireTransportKind { Tcp, Http, Mqtt, Uart, Spi, Custom }
/// <summary>
/// Lists supported one wire security mode values.
/// </summary>
public enum OneWireSecurityMode { None, Signed, EncryptedAndSigned }
/// <summary>
/// Lists supported one wire trust level values.
/// </summary>
public enum OneWireTrustLevel { Untrusted, Discovered, Linked, MfaVerified, Trusted }
/// <summary>
/// Lists supported one wire work status values.
/// </summary>
public enum OneWireWorkStatus { PendingApproval, Queued, Running, Completed, Failed, Declined, Cancelled }
/// <summary>
/// Lists supported one wire approval mode values.
/// </summary>
public enum OneWireApprovalMode { AskEveryTime, SameCapability, CurrentWorkOrder, AlwaysAllow, Deny }
/// <summary>
/// Lists supported one wire hardware kind values.
/// </summary>
public enum OneWireHardwareKind { Auto, Cpu, Gpu, Accelerator, Remote }
/// <summary>
/// Lists supported one wire interaction kind values.
/// </summary>
public enum OneWireInteractionKind { None, Human, Automated, HumanAndAutomated }
/// <summary>
/// Lists supported one wire UI feature state values.
/// </summary>
public enum OneWireUiFeatureState { Hidden, Disabled, Enabled }
/// <summary>
/// Lists supported one wire interaction editor values.
/// </summary>
public enum OneWireInteractionEditor { None, ConfirmationOnly, PlainText, RichText, Json }

/// <summary>
/// Bidirectional interaction contract. "Target system" always means the receiver of the current envelope,
/// so the same contract works for LocalGPT-to-plugin and plugin-to-LocalGPT calls without direction-specific DTOs.
/// </summary>
public interface IOneWireInteractionContract
{
    bool RequiresHumanInteractionOnTargetSystem { get; set; }
    bool RequiresAutomatedInteractionOnTargetSystem { get; set; }
    OneWireInteractionKind InteractionKind { get; set; }
    string? InteractionValueJson { get; set; }
    string InteractionValueContentType { get; set; }
}

/// <summary>
/// Interface contract implemented by every transferable 1-Wire envelope. The interaction fields are
/// deliberately target-oriented, making the same fields valid in both LocalGPT-to-plugin and plugin-to-LocalGPT directions.
/// </summary>
public interface IOneWireEnvelope : IOneWireInteractionContract
{
    string ProtocolVersion { get; set; }
    Guid MessageId { get; set; }
    Guid CorrelationId { get; set; }
    Guid? ReplyToMessageId { get; set; }
    OneWireMessageType MessageType { get; set; }
    string SourcePeerId { get; set; }
    string TargetPeerId { get; set; }
    DateTimeOffset CreatedUtc { get; set; }
    DateTimeOffset? ExpiresUtc { get; set; }
    int Sequence { get; set; }
    OneWireExecutionMode ExecutionMode { get; set; }
    string Controller { get; set; }
    string Method { get; set; }
    string Route { get; set; }
    string CapabilityKey { get; set; }
    List<string> Organs { get; set; }
    List<string> Skills { get; set; }
    Dictionary<string, JsonElement>? Properties { get; set; }
    string? EncryptedPayload { get; set; }
    OneWireSecurityMode SecurityMode { get; set; }
    string SecurityKeyId { get; set; }
    string? EncryptionNonce { get; set; }
    string? AuthenticationTag { get; set; }
    string? Signature { get; set; }
    string? Hash { get; set; }
    string? ErrorCheck { get; set; }
    bool UserConfirmed { get; set; }
    OneWireApprovalMode ApprovalMode { get; set; }
    string WorkOrderKey { get; set; }
    DateTimeOffset? NotBeforeUtc { get; set; }
    string WorkflowJson { get; set; }
    string Error { get; set; }
}

/// <summary>
/// Represents an one wire envelope.
/// </summary>
public sealed class OneWireEnvelope : IOneWireEnvelope
{
    /// <summary>
    /// Gets or sets protocol version.
    /// </summary>
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    /// <summary>
    /// Gets or sets message identifier.
    /// </summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets correlation identifier.
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets reply to message identifier.
    /// </summary>
    public Guid? ReplyToMessageId { get; set; }
    /// <summary>
    /// Gets or sets message type.
    /// </summary>
    public OneWireMessageType MessageType { get; set; }
    /// <summary>
    /// Gets or sets source peer identifier.
    /// </summary>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets target peer identifier.
    /// </summary>
    public string TargetPeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets created UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets expires UTC.
    /// </summary>
    public DateTimeOffset? ExpiresUtc { get; set; }
    /// <summary>
    /// Gets or sets sequence.
    /// </summary>
    public int Sequence { get; set; }
    /// <summary>
    /// Gets or sets execution mode.
    /// </summary>
    public OneWireExecutionMode ExecutionMode { get; set; } = OneWireExecutionMode.Once;
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets organs.
    /// </summary>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets skills.
    /// </summary>
    public List<string> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets properties.
    /// </summary>
    public Dictionary<string, JsonElement>? Properties { get; set; }
    /// <summary>
    /// Gets or sets encrypted payload.
    /// </summary>
    public string? EncryptedPayload { get; set; }
    /// <summary>
    /// Gets or sets security mode.
    /// </summary>
    public OneWireSecurityMode SecurityMode { get; set; } = OneWireSecurityMode.None;
    /// <summary>
    /// Gets or sets security key identifier.
    /// </summary>
    public string SecurityKeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets encryption nonce.
    /// </summary>
    public string? EncryptionNonce { get; set; }
    /// <summary>
    /// Gets or sets authentication tag.
    /// </summary>
    public string? AuthenticationTag { get; set; }
    /// <summary>
    /// Gets or sets signature.
    /// </summary>
    public string? Signature { get; set; }
    /// <summary>
    /// Gets or sets hash.
    /// </summary>
    public string? Hash { get; set; }
    /// <summary>
    /// Gets or sets error check.
    /// </summary>
    public string? ErrorCheck { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets approval mode.
    /// </summary>
    public OneWireApprovalMode ApprovalMode { get; set; } = OneWireApprovalMode.AskEveryTime;
    /// <summary>
    /// Gets or sets work order key.
    /// </summary>
    public string WorkOrderKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets not before UTC.
    /// </summary>
    public DateTimeOffset? NotBeforeUtc { get; set; }
    /// <summary>
    /// Gets or sets workflow JSON.
    /// </summary>
    public string WorkflowJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>True when the receiving application must involve its local human before completion.</summary>
    public bool RequiresHumanInteractionOnTargetSystem { get; set; }

    /// <summary>True when the receiving application must run an automated local interaction before completion.</summary>
    public bool RequiresAutomatedInteractionOnTargetSystem { get; set; }

    /// <summary>Derived kind retained on the wire so receivers do not have to infer combined interaction requirements.</summary>
    public OneWireInteractionKind InteractionKind { get; set; } = OneWireInteractionKind.None;

    /// <summary>Bidirectional serialized context/result value for the requested human or automated interaction.</summary>
    public string? InteractionValueJson { get; set; }

    /// <summary>Media type of InteractionValueJson; defaults to JSON but may identify text or a referenced binary manifest.</summary>
    public string InteractionValueContentType { get; set; } = "application/json";

    /// <summary>
    /// Normalizes interaction kind.
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
/// Represents an one wire capability descriptor.
/// </summary>
public sealed class OneWireCapabilityDescriptor
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets controller.
    /// </summary>
    public string Controller { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameter schema JSON.
    /// </summary>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    /// <summary>
    /// Gets or sets organs.
    /// </summary>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets skills.
    /// </summary>
    public List<string> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets required skill keys.
    /// </summary>
    public List<string> RequiredSkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets UI activation keys.
    /// </summary>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets is online.
    /// </summary>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is read only.
    /// </summary>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets requires human confirmation.
    /// </summary>
    public bool RequiresHumanConfirmation { get; set; } = true;
    /// <summary>
    /// Gets or sets supports scheduling.
    /// </summary>
    public bool SupportsScheduling { get; set; }
    /// <summary>
    /// Gets or sets supports recurring execution.
    /// </summary>
    public bool SupportsRecurringExecution { get; set; }
    /// <summary>
    /// Gets or sets requires human interaction on target system.
    /// </summary>
    public bool RequiresHumanInteractionOnTargetSystem { get; set; }
    /// <summary>
    /// Gets or sets requires automated interaction on target system.
    /// </summary>
    public bool RequiresAutomatedInteractionOnTargetSystem { get; set; }
    /// <summary>
    /// Gets or sets interaction value schema JSON.
    /// </summary>
    public string InteractionValueSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    /// <summary>Human-readable description of required inputs, suitable for Council prompt teaching and small external clients.</summary>
    public string InputContract { get; set; } = string.Empty;
    /// <summary>Human-readable description of the produced result.</summary>
    public string OutputContract { get; set; } = string.Empty;
    /// <summary>Security and approval behavior that every Council member must respect.</summary>
    public string SecurityContract { get; set; } = string.Empty;
    /// <summary>Typical organic use case, such as eyes, hands, OCR or reviewed text feedback.</summary>
    public string OrganicUseCase { get; set; } = string.Empty;
    /// <summary>Suggested Council roles or model abilities, for example OCR-capable vision members.</summary>
    public List<string> SuggestedCouncilRoles { get; set; } = [];
    /// <summary>Whether this capability is currently advertised to a securely linked peer.</summary>
    public bool IsExposedToPeer { get; set; } = true;
    /// <summary>Whether the receiver may invoke this capability after its local policy and confirmation checks pass.</summary>
    public bool AllowPeerInvocation { get; set; } = true;
    /// <summary>The receiving frontend is the authoritative confirmation surface for consequential work.</summary>
    public bool RequiresFrontendUserConfirmation { get; set; }
    /// <summary>Preferred frontend editor for human input that is returned through InteractionValueJson.</summary>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>Stable key of the local catalog entry that controls peer exposure and confirmation policy.</summary>
    public string ConfigurationKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source.
    /// </summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Represents an one wire skill descriptor.
/// </summary>
public sealed class OneWireSkillDescriptor
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets source peer identifier.
    /// </summary>
    public string SourcePeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets organs.
    /// </summary>
    public List<string> Organs { get; set; } = [];
    /// <summary>
    /// Gets or sets capability keys.
    /// </summary>
    public List<string> CapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets UI activation keys.
    /// </summary>
    public List<string> UiActivationKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets is online.
    /// </summary>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets updated UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an one wire peer advertisement.
/// </summary>
public sealed class OneWirePeerAdvertisement
{
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets application.
    /// </summary>
    public string Application { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets application version.
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets host name.
    /// </summary>
    public string HostName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets address.
    /// </summary>
    public string Address { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets service port.
    /// </summary>
    public int ServicePort { get; set; }
    /// <summary>
    /// Gets or sets discovery port.
    /// </summary>
    public int DiscoveryPort { get; set; }
    /// <summary>
    /// Gets or sets web base URL.
    /// </summary>
    public string WebBaseUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets seen UTC.
    /// </summary>
    public DateTimeOffset SeenUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets is connected.
    /// </summary>
    public bool IsConnected { get; set; }
    /// <summary>
    /// Gets or sets transport kind.
    /// </summary>
    public OneWireTransportKind TransportKind { get; set; } = OneWireTransportKind.Tcp;
    /// <summary>
    /// Gets or sets supported transports.
    /// </summary>
    public List<string> SupportedTransports { get; set; } = ["tcp", "http-json"];
    /// <summary>
    /// Gets or sets security.
    /// </summary>
    public OneWireSecurityDescriptor Security { get; set; } = new();
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


/// <summary>Compact public security metadata safe to advertise during discovery and handshake.</summary>
public sealed class OneWireSecurityDescriptor
{
    /// <summary>
    /// Gets or sets has runtime secret.
    /// </summary>
    public bool HasRuntimeSecret { get; set; }
    /// <summary>
    /// Gets or sets supports signing.
    /// </summary>
    public bool SupportsSigning { get; set; } = true;
    /// <summary>
    /// Gets or sets supports encryption.
    /// </summary>
    public bool SupportsEncryption { get; set; } = true;
    /// <summary>
    /// Gets or sets supports mfa pairing.
    /// </summary>
    public bool SupportsMfaPairing { get; set; } = true;
    /// <summary>
    /// Gets or sets key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key agreement public key.
    /// </summary>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signing public key.
    /// </summary>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets pairing scheme.
    /// </summary>
    public string PairingScheme { get; set; } = "onewire-pair-v1";
}

/// <summary>Serializable pairing ticket. It contains public material only and is suitable for QR/barcode transport.</summary>
public sealed class OneWirePairingTicket
{
    /// <summary>
    /// Gets or sets scheme.
    /// </summary>
    public string Scheme { get; set; } = "onewire-pair-v1";
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets application.
    /// </summary>
    public string Application { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets protocol version.
    /// </summary>
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    /// <summary>
    /// Gets or sets key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key agreement public key.
    /// </summary>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signing public key.
    /// </summary>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets created UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets expires UTC.
    /// </summary>
    public DateTimeOffset ExpiresUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(10);
    /// <summary>
    /// Gets or sets nonce.
    /// </summary>
    public string Nonce { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

/// <summary>Runtime-only security status shown by each application frontend. No private key material is exposed.</summary>
public sealed class OneWireRuntimeSecurityStatus
{
    /// <summary>
    /// Gets or sets has secret.
    /// </summary>
    public bool HasSecret { get; set; }
    /// <summary>
    /// Gets or sets secret path.
    /// </summary>
    public string SecretPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets created UTC.
    /// </summary>
    public DateTimeOffset? CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets rotated UTC.
    /// </summary>
    public DateTimeOffset? RotatedUtc { get; set; }
    /// <summary>
    /// Gets or sets trusted peer count.
    /// </summary>
    public int TrustedPeerCount { get; set; }
    /// <summary>
    /// Gets or sets mfa enrolled.
    /// </summary>
    public bool MfaEnrolled { get; set; }
    /// <summary>
    /// Gets or sets warning.
    /// </summary>
    public string Warning { get; set; } = string.Empty;
}

/// <summary>Frontend request used when a user imports a public pairing ticket and authorizes trust.</summary>
public sealed class OneWireTrustEstablishmentRequest
{
    /// <summary>
    /// Gets or sets ticket.
    /// </summary>
    public OneWirePairingTicket Ticket { get; set; } = new();
    /// <summary>
    /// Gets or sets mfa code.
    /// </summary>
    public string MfaCode { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets valid for minutes.
    /// </summary>
    public int ValidForMinutes { get; set; } = 1440;
}

/// <summary>Persisted trust metadata. Private keys and MFA seeds never belong in this transferable contract.</summary>
public sealed class OneWireTrustedPeerDescriptor
{
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key agreement public key.
    /// </summary>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signing public key.
    /// </summary>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets trust level.
    /// </summary>
    public OneWireTrustLevel TrustLevel { get; set; } = OneWireTrustLevel.Untrusted;
    /// <summary>
    /// Gets or sets trusted UTC.
    /// </summary>
    public DateTimeOffset TrustedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets valid until UTC.
    /// </summary>
    public DateTimeOffset? ValidUntilUtc { get; set; }
    /// <summary>
    /// Gets or sets mfa verified until UTC.
    /// </summary>
    public DateTimeOffset? MfaVerifiedUntilUtc { get; set; }
}

/// <summary>Encrypted payload body kept intentionally simple for .NET, ESP32 and future transport adapters.</summary>
public sealed class OneWireSensitivePayload
{
    /// <summary>
    /// Gets or sets properties.
    /// </summary>
    public Dictionary<string, JsonElement>? Properties { get; set; }
    /// <summary>
    /// Gets or sets interaction value JSON.
    /// </summary>
    public string? InteractionValueJson { get; set; }
    /// <summary>
    /// Gets or sets interaction value content type.
    /// </summary>
    public string InteractionValueContentType { get; set; } = "application/json";
    /// <summary>
    /// Gets or sets workflow JSON.
    /// </summary>
    public string WorkflowJson { get; set; } = string.Empty;
}


/// <summary>
/// Represents an one wire UI feature descriptor.
/// </summary>
public sealed class OneWireUiFeatureDescriptor
{
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets state.
    /// </summary>
    public OneWireUiFeatureState State { get; set; } = OneWireUiFeatureState.Hidden;
    /// <summary>
    /// Gets or sets reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets required capability keys.
    /// </summary>
    public List<string> RequiredCapabilityKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets required skill keys.
    /// </summary>
    public List<string> RequiredSkillKeys { get; set; } = [];
    /// <summary>
    /// Gets or sets updated UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an one wire hardware descriptor.
/// </summary>
public sealed class OneWireHardwareDescriptor
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public OneWireHardwareKind Kind { get; set; } = OneWireHardwareKind.Auto;
    /// <summary>
    /// Gets or sets index.
    /// </summary>
    public int Index { get; set; } = -1;
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets vendor.
    /// </summary>
    public string Vendor { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets dedicated memory bytes.
    /// </summary>
    public long? DedicatedMemoryBytes { get; set; }
    /// <summary>
    /// Gets or sets logical processor count.
    /// </summary>
    public int LogicalProcessorCount { get; set; }
    /// <summary>
    /// Gets or sets is online.
    /// </summary>
    public bool IsOnline { get; set; } = true;
    /// <summary>
    /// Gets or sets lane key.
    /// </summary>
    public string LaneKey => Kind == OneWireHardwareKind.Auto
        ? $"auto:{Name}"
        : $"{Kind.ToString().ToLowerInvariant()}:{Index}:{Name}";
}

/// <summary>
/// Represents an one wire model self assessment.
/// </summary>
public sealed class OneWireModelSelfAssessment
{
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets member key.
    /// </summary>
    public string MemberKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets DevExpress functions.
    /// </summary>
    public List<string> DxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets controller methods.
    /// </summary>
    public List<string> ControllerMethods { get; set; } = [];
    /// <summary>
    /// Gets or sets organic capabilities.
    /// </summary>
    public List<string> OrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets skills.
    /// </summary>
    public List<string> Skills { get; set; } = [];
    /// <summary>
    /// Gets or sets confidence.
    /// </summary>
    public int Confidence { get; set; } = 50;
    /// <summary>
    /// Gets or sets evidence.
    /// </summary>
    public string Evidence { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets reported UTC.
    /// </summary>
    public DateTimeOffset ReportedUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an one wire recurring execution.
/// </summary>
public sealed class OneWireRecurringExecution
{
    /// <summary>
    /// Gets or sets interval seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 15;
    /// <summary>
    /// Gets or sets debounce milliseconds.
    /// </summary>
    public int DebounceMilliseconds { get; set; } = 750;
    /// <summary>
    /// Gets or sets maximum pending executions.
    /// </summary>
    public int MaximumPendingExecutions { get; set; } = 1;
    /// <summary>
    /// Gets or sets stop after UTC.
    /// </summary>
    public DateTimeOffset? StopAfterUtc { get; set; }
    /// <summary>
    /// Gets or sets maximum executions.
    /// </summary>
    public int? MaximumExecutions { get; set; }
}

/// <summary>
/// Represents an one wire permission rule.
/// </summary>
public sealed class OneWirePermissionRule
{
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets capability key.
    /// </summary>
    public string CapabilityKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets organ.
    /// </summary>
    public string Organ { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets approval mode.
    /// </summary>
    public OneWireApprovalMode ApprovalMode { get; set; } = OneWireApprovalMode.AskEveryTime;
    /// <summary>Controls whether the capability is advertised to this linked peer.</summary>
    public bool IsExposed { get; set; } = true;
    /// <summary>Controls whether an advertised capability can be invoked by this peer.</summary>
    public bool AllowInvocation { get; set; } = true;
    /// <summary>Forces the receiving application's local frontend confirmation path even when a reusable approval mode exists.</summary>
    public bool RequiresFrontendConfirmation { get; set; } = true;
    /// <summary>Editor shown to the local user when the request also needs human-provided information.</summary>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>Only a peer linked by an explicit frontend action may use this rule.</summary>
    public bool RequireLinkedPeer { get; set; } = true;
    /// <summary>
    /// Gets or sets work order key.
    /// </summary>
    public string WorkOrderKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets updated UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets updated by.
    /// </summary>
    public string UpdatedBy { get; set; } = "CurrentUser";
}

/// <summary>
/// Represents an one wire council model route.
/// </summary>
public sealed class OneWireCouncilModelRoute
{
    /// <summary>Provider-qualified participant key used by Council scheduling and saved presets.</summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>Canonical provider kind such as ollama, openai-compatible, openai or azure-openai.</summary>
    public string ProviderKind { get; set; } = string.Empty;
    /// <summary>Human-readable provider name retained for diagnostics and user interfaces.</summary>
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>Exact provider endpoint used to disambiguate same-named models.</summary>
    public string ProviderEndpoint { get; set; } = string.Empty;
    /// <summary>Model or deployment name understood by the selected provider.</summary>
    public string ProviderModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets hardware kind.
    /// </summary>
    public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;
    /// <summary>
    /// Gets or sets hardware index.
    /// </summary>
    public int HardwareIndex { get; set; } = -1;
    /// <summary>
    /// Gets or sets hardware name.
    /// </summary>
    public string HardwareName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets min output tokens.
    /// </summary>
    public int MinOutputTokens { get; set; } = 256;
    /// <summary>
    /// Gets or sets max output tokens.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Gets or sets min context tokens.
    /// </summary>
    public int MinContextTokens { get; set; } = 2048;
    /// <summary>
    /// Gets or sets max context tokens.
    /// </summary>
    public int MaxContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets ollama num gpu.
    /// </summary>
    public int? OllamaNumGpu { get; set; }
    /// <summary>Optional per-model override for the session load slider. Null uses the session-wide percentage.</summary>
    public int? LoadPercentOverride { get; set; }
    /// <summary>
    /// Gets or sets self reported DevExpress functions.
    /// </summary>
    public List<string> SelfReportedDxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets self reported controller methods.
    /// </summary>
    public List<string> SelfReportedControllerMethods { get; set; } = [];
    /// <summary>
    /// Gets or sets self reported organic capabilities.
    /// </summary>
    public List<string> SelfReportedOrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets self reported skills.
    /// </summary>
    public List<string> SelfReportedSkills { get; set; } = [];
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets max concurrent models on lane.
    /// </summary>
    public int MaxConcurrentModelsOnLane { get; set; } = 1;
    /// <summary>
    /// Gets or sets lane key.
    /// </summary>
    public string LaneKey => HardwareKind == OneWireHardwareKind.Auto
        ? $"auto:{ModelName}"
        : $"{HardwareKind.ToString().ToLowerInvariant()}:{HardwareIndex}:{HardwareName}";
}

/// <summary>
/// Represents an one wire council request.
/// </summary>
public sealed class OneWireCouncilRequest
{
    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets team key.
    /// </summary>
    public string TeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets leader model name.
    /// </summary>
    public string LeaderModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model names.
    /// </summary>
    public List<string> ModelNames { get; set; } = [];
    /// <summary>
    /// Gets or sets model routes.
    /// </summary>
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];
    /// <summary>
    /// Gets or sets requested organic capabilities.
    /// </summary>
    public List<string> RequestedOrganicCapabilities { get; set; } = [];
    /// <summary>
    /// Gets or sets external project context JSON.
    /// </summary>
    public string ExternalProjectContextJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project topic identifier.
    /// </summary>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets project revision identifier.
    /// </summary>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets max rounds.
    /// </summary>
    public int MaxRounds { get; set; } = 1;
    /// <summary>
    /// Gets or sets max output tokens.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>
    /// Gets or sets max context tokens.
    /// </summary>
    public int MaxContextTokens { get; set; } = 32768;
    /// <summary>
    /// Gets or sets max parallel models.
    /// </summary>
    public int MaxParallelModels { get; set; } = 1;
    /// <summary>
    /// Gets or sets allow parallel hardware roads.
    /// </summary>
    public bool AllowParallelHardwareRoads { get; set; } = true;
    /// <summary>Session-wide 0..100 load position interpolated between every model road's own minimum and maximum.</summary>
    public int ResourceLoadPercent { get; set; } = 30;
    /// <summary>
    /// Gets or sets include memory.
    /// </summary>
    public bool IncludeMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets save to memory.
    /// </summary>
    public bool SaveToMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets generate implementation artifact.
    /// </summary>
    public bool GenerateImplementationArtifact { get; set; }
    /// <summary>
    /// Gets or sets user confirmed artifact build.
    /// </summary>
    public bool UserConfirmedArtifactBuild { get; set; }
}

/// <summary>Capability-provider abstraction implemented by LocalGPT and organic plugin systems.</summary>
public interface IOneWireCapabilityProvider
{
    /// <summary>
    /// Gets capabilities async.
    /// </summary>
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets skills async.
    /// </summary>
    Task<IReadOnlyList<OneWireSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OneWireUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OneWireUiFeatureDescriptor>>([]);
    Task<IReadOnlyList<OneWireHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OneWireHardwareDescriptor>>([]);
}

/// <summary>Transport-neutral adapter boundary for TCP now and UART/SPI/MQTT adapters later.</summary>
public interface IOneWireTransportAdapter : IAsyncDisposable
{
    string TransportName { get; }
    bool IsConnected { get; }
    /// <summary>
    /// Runs the send async operation.
    /// </summary>
    Task SendAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    /// <summary>
    /// Runs the receive async operation.
    /// </summary>
    IAsyncEnumerable<OneWireEnvelope> ReceiveAsync(CancellationToken cancellationToken = default);
}
