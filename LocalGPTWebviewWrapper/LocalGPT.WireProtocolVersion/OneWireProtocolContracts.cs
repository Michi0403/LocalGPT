using System.Text.Json;

namespace LocalGPT.WireProtocol;

/// <summary>Stable constants and compatibility checks for the embedded protocol assembly.</summary>
public static class OneWireProtocol
{
    public const string Version = "1.4";
    public const string MinimumCompatibleVersion = "1.0";
    public const int DefaultServicePort = 51140;
    public const int DefaultDiscoveryPort = 51141;
    public const int MaximumMessageBytes = int.MaxValue;

    public static bool IsCompatible(string? version)
    {
        if (!System.Version.TryParse(version, out var candidate) || !System.Version.TryParse(Version, out var current))
            return false;
        return candidate.Major == current.Major && candidate.Minor <= current.Minor;
    }
}

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
    Error,
    Ping,
    Pong
}

public enum OneWireExecutionMode { Once, SequentialSpool, Scheduled, Recurring }
public enum OneWireWorkStatus { PendingApproval, Queued, Running, Completed, Failed, Declined, Cancelled }
public enum OneWireApprovalMode { AskEveryTime, SameCapability, CurrentWorkOrder, AlwaysAllow, Deny }
public enum OneWireHardwareKind { Auto, Cpu, Gpu, Accelerator, Remote }
public enum OneWireInteractionKind { None, Human, Automated, HumanAndAutomated }
public enum OneWireUiFeatureState { Hidden, Disabled, Enabled }

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

public sealed class OneWireEnvelope : IOneWireEnvelope
{
    public string ProtocolVersion { get; set; } = OneWireProtocol.Version;
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public Guid? ReplyToMessageId { get; set; }
    public OneWireMessageType MessageType { get; set; }
    public string SourcePeerId { get; set; } = string.Empty;
    public string TargetPeerId { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresUtc { get; set; }
    public int Sequence { get; set; }
    public OneWireExecutionMode ExecutionMode { get; set; } = OneWireExecutionMode.Once;
    public string Controller { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string CapabilityKey { get; set; } = string.Empty;
    public List<string> Organs { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public Dictionary<string, JsonElement>? Properties { get; set; }
    public string? EncryptedPayload { get; set; }
    public string? Signature { get; set; }
    public string? Hash { get; set; }
    public string? ErrorCheck { get; set; }
    public bool UserConfirmed { get; set; }
    public OneWireApprovalMode ApprovalMode { get; set; } = OneWireApprovalMode.AskEveryTime;
    public string WorkOrderKey { get; set; } = string.Empty;
    public DateTimeOffset? NotBeforeUtc { get; set; }
    public string WorkflowJson { get; set; } = string.Empty;
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

public sealed class OneWireCapabilityDescriptor
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    public List<string> Organs { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public List<string> RequiredSkillKeys { get; set; } = [];
    public List<string> UiActivationKeys { get; set; } = [];
    public bool IsOnline { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsReadOnly { get; set; }
    public bool RequiresHumanConfirmation { get; set; } = true;
    public bool SupportsScheduling { get; set; }
    public bool SupportsRecurringExecution { get; set; }
    public bool RequiresHumanInteractionOnTargetSystem { get; set; }
    public bool RequiresAutomatedInteractionOnTargetSystem { get; set; }
    public string InteractionValueSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    public string Source { get; set; } = string.Empty;
}

public sealed class OneWireSkillDescriptor
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourcePeerId { get; set; } = string.Empty;
    public List<string> Organs { get; set; } = [];
    public List<string> CapabilityKeys { get; set; } = [];
    public List<string> UiActivationKeys { get; set; } = [];
    public bool IsOnline { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OneWirePeerAdvertisement
{
    public string PeerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int ServicePort { get; set; }
    public int DiscoveryPort { get; set; }
    public string WebBaseUrl { get; set; } = string.Empty;
    public DateTimeOffset SeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsConnected { get; set; }
    public List<OneWireCapabilityDescriptor> Capabilities { get; set; } = [];
    public List<OneWireSkillDescriptor> Skills { get; set; } = [];
    public List<OneWireUiFeatureDescriptor> UiFeatures { get; set; } = [];
    public List<OneWireHardwareDescriptor> Hardware { get; set; } = [];
}


public sealed class OneWireUiFeatureDescriptor
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public OneWireUiFeatureState State { get; set; } = OneWireUiFeatureState.Hidden;
    public string Reason { get; set; } = string.Empty;
    public List<string> RequiredCapabilityKeys { get; set; } = [];
    public List<string> RequiredSkillKeys { get; set; } = [];
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OneWireHardwareDescriptor
{
    public OneWireHardwareKind Kind { get; set; } = OneWireHardwareKind.Auto;
    public int Index { get; set; } = -1;
    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public long? DedicatedMemoryBytes { get; set; }
    public int LogicalProcessorCount { get; set; }
    public bool IsOnline { get; set; } = true;
    public string LaneKey => Kind == OneWireHardwareKind.Auto
        ? $"auto:{Name}"
        : $"{Kind.ToString().ToLowerInvariant()}:{Index}:{Name}";
}

public sealed class OneWireModelSelfAssessment
{
    public string ModelName { get; set; } = string.Empty;
    public string MemberKey { get; set; } = string.Empty;
    public List<string> DxFunctions { get; set; } = [];
    public List<string> ControllerMethods { get; set; } = [];
    public List<string> OrganicCapabilities { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public int Confidence { get; set; } = 50;
    public string Evidence { get; set; } = string.Empty;
    public DateTimeOffset ReportedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OneWireRecurringExecution
{
    public int IntervalSeconds { get; set; } = 15;
    public int DebounceMilliseconds { get; set; } = 750;
    public int MaximumPendingExecutions { get; set; } = 1;
    public DateTimeOffset? StopAfterUtc { get; set; }
    public int? MaximumExecutions { get; set; }
}

public sealed class OneWirePermissionRule
{
    public string PeerId { get; set; } = string.Empty;
    public string CapabilityKey { get; set; } = string.Empty;
    public string Organ { get; set; } = string.Empty;
    public OneWireApprovalMode ApprovalMode { get; set; } = OneWireApprovalMode.AskEveryTime;
    public string WorkOrderKey { get; set; } = string.Empty;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string UpdatedBy { get; set; } = "CurrentUser";
}

public sealed class OneWireCouncilModelRoute
{
    public string ModelName { get; set; } = string.Empty;
    public OneWireHardwareKind HardwareKind { get; set; } = OneWireHardwareKind.Auto;
    public int HardwareIndex { get; set; } = -1;
    public string HardwareName { get; set; } = string.Empty;
    public int MinOutputTokens { get; set; } = 256;
    public int MaxOutputTokens { get; set; } = 4096;
    public int MinContextTokens { get; set; } = 2048;
    public int MaxContextTokens { get; set; } = 32768;
    public int? OllamaNumGpu { get; set; }
    public List<string> SelfReportedDxFunctions { get; set; } = [];
    public List<string> SelfReportedControllerMethods { get; set; } = [];
    public List<string> SelfReportedOrganicCapabilities { get; set; } = [];
    public List<string> SelfReportedSkills { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public int MaxConcurrentModelsOnLane { get; set; } = 1;
    public string LaneKey => HardwareKind == OneWireHardwareKind.Auto
        ? $"auto:{ModelName}"
        : $"{HardwareKind.ToString().ToLowerInvariant()}:{HardwareIndex}:{HardwareName}";
}

public sealed class OneWireCouncilRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string TeamKey { get; set; } = "general";
    public string LeaderModelName { get; set; } = string.Empty;
    public List<string> ModelNames { get; set; } = [];
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; } = [];
    public List<string> RequestedOrganicCapabilities { get; set; } = [];
    public string ExternalProjectContextJson { get; set; } = "{}";
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTopicId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public int MaxRounds { get; set; } = 1;
    public int MaxOutputTokens { get; set; } = 4096;
    public int MaxContextTokens { get; set; } = 32768;
    public int MaxParallelModels { get; set; } = 1;
    public bool AllowParallelHardwareRoads { get; set; } = true;
    public bool IncludeMemory { get; set; } = true;
    public bool SaveToMemory { get; set; } = true;
    public bool GenerateImplementationArtifact { get; set; }
    public bool UserConfirmedArtifactBuild { get; set; }
}

/// <summary>Capability-provider abstraction implemented by LocalGPT and organic plugin systems.</summary>
public interface IOneWireCapabilityProvider
{
    Task<IReadOnlyList<OneWireCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
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
    Task SendAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default);
    IAsyncEnumerable<OneWireEnvelope> ReceiveAsync(CancellationToken cancellationToken = default);
}
