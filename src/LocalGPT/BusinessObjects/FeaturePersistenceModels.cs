using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>Persistent configuration for one visible chat prompt that can start an AI Council run.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class CouncilPromptStarterConfiguration
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council prompt starter instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this council prompt starter instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    [MaxLength(160)] public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the title value that forms part of the council prompt starter state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    [MaxLength(240)] public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the short description shown on the prompt card.</summary>
    /// <value>The summary value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    [MaxLength(1000)] public string Summary { get; set; } = string.Empty;
    /// <summary>Gets or sets the full prompt submitted to the selected chat or Council.</summary>
    /// <value>The prompt message value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public string PromptMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets a JSON array of compatible Council team keys.</summary>
    /// <value>The team keys JSON value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public string TeamKeysJson { get; set; } = "[]";
    /// <summary>Gets or sets whether choosing this card starts an AI Council run rather than a normal chat prompt.</summary>
    /// <value>The starts council directly value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public bool StartsCouncilDirectly { get; set; }
    /// <summary>Gets or sets whether the row originated from a maintained LocalGPT seed.</summary>
    /// <value>The is built in value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public bool IsBuiltIn { get; set; }
    /// <summary>Gets or sets whether the starter is available to the UI.</summary>
    /// <value>The is enabled value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the created at UTC associated with this council prompt starter state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council prompt starter state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilPromptStarterConfiguration"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Persistent registration for a built-in or user-imported localization JSON catalog.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class LocalizationCatalogRegistration
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this localization catalog registration instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the culture name value that forms part of the localization catalog registration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The culture name value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    [MaxLength(40)] public string CultureName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the localization catalog registration state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    [MaxLength(240)] public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the catalog path used by this localization catalog registration instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The catalog path value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    [MaxLength(2048)] public string CatalogPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the string count that quantifies the associated localization catalog registration data.
    /// </summary>
    /// <value>The string count value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public int StringCount { get; set; }
    /// <summary>Gets or sets the number of English baseline keys not supplied by this catalog.</summary>
    /// <value>The missing baseline key count value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public int MissingBaselineKeyCount { get; set; }
    /// <summary>Gets or sets whether the catalog was imported by a user.</summary>
    /// <value>The is user override value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public bool IsUserOverride { get; set; }
    /// <summary>Gets or sets whether this catalog is offered by the language selector.</summary>
    /// <value>The is enabled value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the created at UTC associated with this localization catalog registration state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this localization catalog registration state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="LocalizationCatalogRegistration"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Persistent evidence for one documentation build or fallback generation.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class DocumentationBuildRecord
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this documentation build instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the version value that forms part of the documentation build state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    [MaxLength(80)] public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the generated at UTC associated with this documentation build state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The generated at UTC value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether HTML available applies to the documentation build state.
    /// </summary>
    /// <value>The HTML available value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    public bool HtmlAvailable { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether PDF available applies to the documentation build state.
    /// </summary>
    /// <value>The PDF available value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    public bool PdfAvailable { get; set; }
    /// <summary>Gets or sets the HTML generation mode, such as DocFX or static fallback.</summary>
    /// <value>The documentation mode value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    [MaxLength(120)] public string DocumentationMode { get; set; } = string.Empty;
    /// <summary>Gets or sets the PDF generation mode, such as DocFX or minimal fallback.</summary>
    /// <value>The PDF mode value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    [MaxLength(120)] public string PdfMode { get; set; } = string.Empty;
    /// <summary>Gets or sets the source tool selection used for this build.</summary>
    /// <value>The tool source value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    [MaxLength(240)] public string ToolSource { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the output root value that forms part of the documentation build state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The output root value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    [MaxLength(2048)] public string OutputRoot { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the warning value that forms part of the documentation build state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The warning value exposed by <see cref="DocumentationBuildRecord"/>.</value>
    [MaxLength(4000)] public string Warning { get; set; } = string.Empty;
}

/// <summary>Persistent envelope for an embedded firmware plan and its generated artifacts.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class EmbeddedFirmwarePlanRecord
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable plan key used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The plan key value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    [MaxLength(160)] public string PlanKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the device name value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The device name value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    [MaxLength(240)] public string DeviceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable board profile key used to identify or correlate this embedded firmware plan instance with related application state.
    /// </summary>
    /// <value>The board profile key value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    [MaxLength(160)] public string BoardProfileKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    [MaxLength(80)] public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the plan JSON value that forms part of the embedded firmware plan state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The plan JSON value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    public string PlanJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the created at UTC associated with this embedded firmware plan state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this embedded firmware plan state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="EmbeddedFirmwarePlanRecord"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Persistent envelope for an authoritative GameDirector runtime session.</summary>
[DocumentationUpdated("2.1.23")]
public sealed class CouncilGameSessionRecord
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable session key used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The session key value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    [MaxLength(160)] public string SessionKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the stable game key used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The game key value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    [MaxLength(160)] public string GameKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable team key used to identify or correlate this council game session instance with related application state.
    /// </summary>
    /// <value>The team key value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    [MaxLength(160)] public string TeamKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    [MaxLength(80)] public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the snapshot JSON value that forms part of the council game session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The snapshot JSON value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    public string SnapshotJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the created at UTC associated with this council game session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council game session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilGameSessionRecord"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Approval-gated request for creating or updating one persistent feature record.</summary>
/// <typeparam name="TRecord">Persistent record type.</typeparam>
[DocumentationUpdated("2.1.23")]
public sealed class SaveFeatureRecordRequest<TRecord>
{
    /// <summary>
    /// Gets or sets the record value that forms part of the save feature record state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The record value exposed by <see cref="SaveFeatureRecordRequest"/>.</value>
    public TRecord Record { get; set; } = default!;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save feature record state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveFeatureRecordRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}
