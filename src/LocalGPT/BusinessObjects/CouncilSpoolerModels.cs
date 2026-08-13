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
