using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Stores a reusable hardware-spooler performance profile. The serialized routes preserve provider-qualified
/// model identity together with benchmarked token ranges, hardware roads, GPU settings and lane concurrency.
/// Applying the profile never changes Council membership; callers apply only routes that match the currently
/// selected provider/endpoint/model identities.
/// </summary>
public sealed class HardwarePerformancePreset
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this hardware performance preset instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the created at UTC associated with this hardware performance preset state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated at UTC associated with this hardware performance preset state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the name value that forms part of the hardware performance preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a bounded explanation of how the profile was created.</summary>
    /// <value>The description value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider-qualified hardware routes as 1-Wire JSON. Each route carries minimum/maximum
    /// context and output tokens plus the associated hardware road settings.
    /// </summary>
    /// <value>The model routes JSON value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    [Required, Column(TypeName = "TEXT")]
    public string ModelRoutesJson { get; set; } = "[]";

    /// <summary>Gets or sets the session-wide hardware load used when a route does not own an override.</summary>
    /// <value>The resource load percent value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public int ResourceLoadPercent { get; set; } = 100;

    /// <summary>Gets or sets the benchmark run that produced this profile, when applicable.</summary>
    /// <value>The source run identifier value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public Guid? SourceRunId { get; set; }

    /// <summary>Gets or sets the bounded source category, for example ProviderBenchmark or Manual.</summary>
    /// <value>The source kind value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    [MaxLength(80)]
    public string SourceKind { get; set; } = "Manual";

    /// <summary>Gets or sets whether the profile is the preferred performance profile.</summary>
    /// <value>The is default value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether the profile is hidden from normal selection without being physically removed.</summary>
    /// <value>The is archived value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets whether a human-approved action created or updated the stored profile.</summary>
    /// <value>The is user approved value exposed by <see cref="HardwarePerformancePreset"/>.</value>
    public bool IsUserApproved { get; set; }
}
