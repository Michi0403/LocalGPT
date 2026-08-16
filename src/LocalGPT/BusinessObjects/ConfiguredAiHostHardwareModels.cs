using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Persists the hardware facts owned by one configured physical AI host and shared by provider endpoints on that machine.</summary>
public sealed class ConfiguredAiHostHardwareProfile
{
    /// <summary>Identifies the persisted host-hardware record independently from its normalized physical-host key.</summary>
    /// <value>The database identifier for this configured physical host profile.</value>
    public Guid Id { get; set; }

    /// <summary>Provides the normalized physical-host key used to join multiple provider endpoints to the same machine.</summary>
    /// <value><c>local-machine</c> for loopback endpoints, otherwise the normalized endpoint host name.</value>
    public string HostKey { get; set; } = string.Empty;

    /// <summary>Provides the human-readable machine name shown in host configuration and hardware evidence.</summary>
    /// <value>The configured or detected host display name.</value>
    public string HostName { get; set; } = string.Empty;

    /// <summary>Describes the operating system associated with the physical host when that fact is known.</summary>
    /// <value>The imported, detected, or manually supplied operating-system description.</value>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>Describes the operating-system/hardware architecture associated with the configured host.</summary>
    /// <value>The architecture label, for example X64 or Arm64, when known.</value>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>Describes the processor installed in the configured physical host.</summary>
    /// <value>The imported, detected, or manually supplied CPU identity.</value>
    public string CpuName { get; set; } = string.Empty;

    /// <summary>Records total host system memory in bytes without conflating it with dedicated GPU memory.</summary>
    /// <value>The total system-memory capacity in bytes, or <see langword="null"/> when unknown.</value>
    public long? SystemMemoryBytes { get; set; }

    /// <summary>Stores the host's zero-or-more GPU/accelerator definitions as durable JSON for database portability.</summary>
    /// <value>A JSON array of <see cref="ConfiguredAiHostGpu"/> records.</value>
    public string GpusJson { get; set; } = "[]";

    /// <summary>Stores provider endpoints known to resolve to this same physical host so hardware facts are not duplicated per model.</summary>
    /// <value>A JSON array containing the provider endpoint strings associated with the host.</value>
    public string ProviderEndpointsJson { get; set; } = "[]";

    /// <summary>Identifies how the current hardware facts were obtained, such as Manual, HWiNFO, or LocalProbe.</summary>
    /// <value>The bounded provenance category for the current host-hardware values.</value>
    public string SourceKind { get; set; } = "Unknown";

    /// <summary>Classifies the trust level of the current hardware evidence so weak discovery cannot silently overrule confirmed facts.</summary>
    /// <value>The confidence category, for example UserConfirmed, ImportedReport, VendorReported, or DetectedIdentityOnly.</value>
    public string Confidence { get; set; } = "Unknown";

    /// <summary>Indicates that a human explicitly saved or imported the current host-hardware values.</summary>
    /// <value><see langword="true"/> when automatic detection must not overwrite the current confirmed values.</value>
    public bool IsUserConfirmed { get; set; }

    /// <summary>Records when the durable host-hardware profile last changed.</summary>
    /// <value>The UTC timestamp of the latest persisted profile update.</value>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Records when local automatic hardware discovery last refreshed this host, independently from manual edits.</summary>
    /// <value>The UTC detection timestamp, or <see langword="null"/> when the profile has not been auto-detected.</value>
    public DateTime? LastDetectedAtUtc { get; set; }

    /// <summary>Exposes the deserialized GPU definitions used by services and UI while EF persists <see cref="GpusJson"/>.</summary>
    /// <value>The in-memory list of GPUs associated with this configured physical host.</value>
    [NotMapped]
    [JsonIgnore]
    public List<ConfiguredAiHostGpu> Gpus { get; set; } = [];
}

/// <summary>Describes one GPU or accelerator attached to a configured physical AI host.</summary>
public sealed class ConfiguredAiHostGpu
{
    /// <summary>Identifies the GPU within the owning host's local device ordering.</summary>
    /// <value>The host-local GPU index.</value>
    public int Index { get; set; }

    /// <summary>Provides the user-visible or vendor-reported GPU identity.</summary>
    /// <value>The GPU model/device name.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>Provides the normalized vendor associated with the GPU identity.</summary>
    /// <value>The GPU vendor, for example AMD, NVIDIA, or Intel.</value>
    public string Vendor { get; set; } = string.Empty;

    /// <summary>Records dedicated GPU memory using a 64-bit byte count suitable for modern devices above 4 GiB.</summary>
    /// <value>Dedicated VRAM in bytes, or <see langword="null"/> when reliable capacity evidence is unavailable.</value>
    public long? DedicatedMemoryBytes { get; set; }
}

/// <summary>Represents the editable `/install` form state used to confirm, detect, or import hardware for one configured host.</summary>
public sealed class ConfiguredAiHostHardwareDraft
{
    /// <summary>Provides one configured provider endpoint used to resolve the owning physical host.</summary>
    /// <value>The provider endpoint currently being edited on `/install`.</value>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Provides the normalized physical-host key derived from <see cref="Endpoint"/>.</summary>
    /// <value>The host key shared by provider bindings that resolve to the same machine.</value>
    public string HostKey { get; set; } = string.Empty;

    /// <summary>Provides the editable display name for the physical machine.</summary>
    /// <value>The host name saved with the durable hardware profile.</value>
    public string HostName { get; set; } = string.Empty;

    /// <summary>Provides the editable operating-system description for this physical host.</summary>
    /// <value>The operating-system text that will be persisted for the host.</value>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>Provides the editable architecture description for the physical host.</summary>
    /// <value>The architecture text that will be persisted for the host.</value>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>Provides the editable processor identity used by host hardware evidence.</summary>
    /// <value>The CPU name that will be persisted for the host.</value>
    public string CpuName { get; set; } = string.Empty;

    /// <summary>Represents total system RAM in GiB for human-friendly editing before conversion to a 64-bit byte value.</summary>
    /// <value>Total system memory in GiB, or <see langword="null"/> when unknown.</value>
    public double? SystemMemoryGiB { get; set; }

    /// <summary>Provides the compact Install form's primary GPU identity.</summary>
    /// <value>The first/primary GPU model name to persist for the host.</value>
    public string GpuName { get; set; } = string.Empty;

    /// <summary>Provides the compact Install form's primary GPU vendor.</summary>
    /// <value>The vendor associated with <see cref="GpuName"/>.</value>
    public string GpuVendor { get; set; } = string.Empty;

    /// <summary>Represents dedicated primary-GPU VRAM in GiB for human-friendly editing.</summary>
    /// <value>Dedicated VRAM in GiB, or <see langword="null"/> when capacity is unknown.</value>
    public double? DedicatedVramGiB { get; set; }

    /// <summary>Provides the editable/readable provenance category shown beside the host hardware values.</summary>
    /// <value>The source category that will accompany the saved hardware facts.</value>
    public string SourceKind { get; set; } = "Manual";

    /// <summary>Provides the confidence classification shown beside the host hardware values.</summary>
    /// <value>The confidence category that will accompany the saved hardware facts.</value>
    public string Confidence { get; set; } = "UserConfirmed";

    /// <summary>Holds an optional pasted HWiNFO text export until deterministic parsing imports its hardware facts.</summary>
    /// <value>The bounded report text supplied locally by the user; it is never sent to an AI by the import service.</value>
    public string HwInfoReportText { get; set; } = string.Empty;
}
