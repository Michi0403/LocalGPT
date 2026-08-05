using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>Persistent configuration for one visible chat prompt that can start an AI Council run.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class CouncilPromptStarterConfiguration
{
    /// <summary>Gets or sets the stable database identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the unique machine-readable starter key.</summary>
    [MaxLength(160)] public string Key { get; set; } = string.Empty;
    /// <summary>Gets or sets the user-visible starter title.</summary>
    [MaxLength(240)] public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the short description shown on the prompt card.</summary>
    [MaxLength(1000)] public string Summary { get; set; } = string.Empty;
    /// <summary>Gets or sets the full prompt submitted to the selected chat or Council.</summary>
    public string PromptMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets a JSON array of compatible Council team keys.</summary>
    public string TeamKeysJson { get; set; } = "[]";
    /// <summary>Gets or sets whether choosing this card starts an AI Council run rather than a normal chat prompt.</summary>
    public bool StartsCouncilDirectly { get; set; }
    /// <summary>Gets or sets whether the row originated from a maintained LocalGPT seed.</summary>
    public bool IsBuiltIn { get; set; }
    /// <summary>Gets or sets whether the starter is available to the UI.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC update timestamp.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Persistent registration for a built-in or user-imported localization JSON catalog.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class LocalizationCatalogRegistration
{
    /// <summary>Gets or sets the stable database identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the normalized .NET culture name.</summary>
    [MaxLength(40)] public string CultureName { get; set; } = string.Empty;
    /// <summary>Gets or sets the localized display name.</summary>
    [MaxLength(240)] public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the bounded catalog path.</summary>
    [MaxLength(2048)] public string CatalogPath { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of parsed string entries.</summary>
    public int StringCount { get; set; }
    /// <summary>Gets or sets the number of English baseline keys not supplied by this catalog.</summary>
    public int MissingBaselineKeyCount { get; set; }
    /// <summary>Gets or sets whether the catalog was imported by a user.</summary>
    public bool IsUserOverride { get; set; }
    /// <summary>Gets or sets whether this catalog is offered by the language selector.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC update timestamp.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Persistent evidence for one documentation build or fallback generation.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class DocumentationBuildRecord
{
    /// <summary>Gets or sets the stable database identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the LocalGPT version documented by this build.</summary>
    [MaxLength(80)] public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the UTC generation timestamp.</summary>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets whether an HTML index was published.</summary>
    public bool HtmlAvailable { get; set; }
    /// <summary>Gets or sets whether a versioned PDF was published.</summary>
    public bool PdfAvailable { get; set; }
    /// <summary>Gets or sets the HTML generation mode, such as DocFX or static fallback.</summary>
    [MaxLength(120)] public string DocumentationMode { get; set; } = string.Empty;
    /// <summary>Gets or sets the PDF generation mode, such as DocFX or minimal fallback.</summary>
    [MaxLength(120)] public string PdfMode { get; set; } = string.Empty;
    /// <summary>Gets or sets the source tool selection used for this build.</summary>
    [MaxLength(240)] public string ToolSource { get; set; } = string.Empty;
    /// <summary>Gets or sets the published documentation root.</summary>
    [MaxLength(2048)] public string OutputRoot { get; set; } = string.Empty;
    /// <summary>Gets or sets a bounded warning or fallback explanation.</summary>
    [MaxLength(4000)] public string Warning { get; set; } = string.Empty;
}

/// <summary>Persistent envelope for an embedded firmware plan and its generated artifacts.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class EmbeddedFirmwarePlanRecord
{
    /// <summary>Gets or sets the stable database identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the stable planning identifier.</summary>
    [MaxLength(160)] public string PlanKey { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional LocalGPT project identifier.</summary>
    public Guid? ProjectId { get; set; }
    /// <summary>Gets or sets the target device name.</summary>
    [MaxLength(240)] public string DeviceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the selected board profile key.</summary>
    [MaxLength(160)] public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the planning or approval status.</summary>
    [MaxLength(80)] public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the complete versioned plan JSON.</summary>
    public string PlanJson { get; set; } = "{}";
    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC update timestamp.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Persistent envelope for an authoritative GameDirector runtime session.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class CouncilGameSessionRecord
{
    /// <summary>Gets or sets the stable database identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the stable runtime session key.</summary>
    [MaxLength(160)] public string SessionKey { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional linked chat-memory conversation identifier.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the game/runtime key.</summary>
    [MaxLength(160)] public string GameKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the responsible Council team key.</summary>
    [MaxLength(160)] public string TeamKey { get; set; } = string.Empty;
    /// <summary>Gets or sets the authoritative runtime status.</summary>
    [MaxLength(80)] public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets the latest authoritative runtime snapshot JSON.</summary>
    public string SnapshotJson { get; set; } = "{}";
    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets the UTC update timestamp.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Approval-gated request for creating or updating one persistent feature record.</summary>
/// <typeparam name="TRecord">Persistent record type.</typeparam>
[DocumentationUpdated("2.1.23")]
public sealed class SaveFeatureRecordRequest<TRecord>
{
    /// <summary>Gets or sets the record to create or update.</summary>
    public TRecord Record { get; set; } = default!;
    /// <summary>Gets or sets whether the user explicitly approved this write.</summary>
    public bool UserConfirmed { get; set; }
}
