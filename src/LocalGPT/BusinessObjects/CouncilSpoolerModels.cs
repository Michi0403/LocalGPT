namespace LocalGPT.BusinessObjects;


/// <summary>
/// Represents a council spooler snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilSpoolerSnapshot
{
    /// <summary>
    /// Gets or sets the stable run identifier used to identify or correlate this council spooler snapshot instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public Guid RunId { get; set; }
    /// <summary>
    /// Gets or sets the started at UTC associated with this council spooler snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public DateTime StartedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council spooler snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public DateTime UpdatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the completed at UTC associated with this council spooler snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the council spooler snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the current round value that forms part of the council spooler snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current round value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public int CurrentRound { get; set; }
    /// <summary>
    /// Gets or sets the phase value that forms part of the council spooler snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The phase value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public string Phase { get; set; } = "Starting";
    /// <summary>
    /// Gets or sets the prompt value that forms part of the council spooler snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prompt value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable council team key used to identify or correlate this council spooler snapshot instance with related application state.
    /// </summary>
    /// <value>The council team key value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public string CouncilTeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets the model names collection maintained or exposed by this council spooler snapshot instance for downstream processing.
    /// </summary>
    /// <value>The model names value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public List<string> ModelNames { get; set; } = [];
    /// <summary>
    /// Gets or sets the steps collection maintained or exposed by this council spooler snapshot instance for downstream processing.
    /// </summary>
    /// <value>The steps value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public List<MultiModelCouncilStep> Steps { get; set; } = [];
    /// <summary>
    /// Gets or sets the final answer value that forms part of the council spooler snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The final answer value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public string FinalAnswer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this council spooler snapshot instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="CouncilSpoolerSnapshot"/>.</value>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>Lightweight persisted Council run header; intentionally contains no model thinking or transcript payloads.</summary>
public sealed class CouncilSpoolerRunSummary
{
    /// <summary>Stable run identifier.</summary>
    public Guid RunId { get; init; }
    /// <summary>UTC time when the run started.</summary>
    public DateTime StartedAtUtc { get; init; }
    /// <summary>Current or final run status.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Last phase.</summary>
    public string Phase { get; init; } = string.Empty;
    /// <summary>Selected saved team key.</summary>
    public string CouncilTeamKey { get; init; } = string.Empty;
}

/// <summary>Compact outcome of one retained Council member step, without prompts, thinking or answer text.</summary>
public sealed class CouncilMemberOutcome
{
    /// <summary>Gets the Council run identifier.</summary>
    /// <value>The retained run.</value>
    public Guid RunId { get; init; }
    /// <summary>Gets the saved team key of the run.</summary>
    /// <value>The team key.</value>
    public string CouncilTeamKey { get; init; } = string.Empty;
    /// <summary>Gets the provider-qualified member.</summary>
    /// <value>The member that produced the step.</value>
    public string ModelName { get; init; } = string.Empty;
    /// <summary>Gets the role of the step.</summary>
    /// <value>The configured role, possibly with a recovery suffix.</value>
    public string Role { get; init; } = string.Empty;
    /// <summary>Gets the logical round.</summary>
    /// <value>The round of the step.</value>
    public int Round { get; init; }
    /// <summary>Gets whether the step produced a usable answer.</summary>
    /// <value><see langword="true"/> for a step without error and with visible text.</value>
    public bool Succeeded { get; init; }
    /// <summary>Gets whether the step failed with an error.</summary>
    /// <value><see langword="true"/> when the step carries an error.</value>
    public bool Failed { get; init; }
    /// <summary>Gets whether the AI host was unreachable for the step.</summary>
    /// <value><see langword="true"/> for host outages.</value>
    public bool HostUnreachable { get; init; }
    /// <summary>Gets the provider execution time.</summary>
    /// <value>Seconds.</value>
    public double DurationSeconds { get; init; }
}
