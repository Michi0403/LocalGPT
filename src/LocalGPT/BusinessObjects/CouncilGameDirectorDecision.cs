using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Records the GameDirector decision that authorizes or rejects one state transition.</summary>
public sealed class CouncilGameDirectorDecision
{
    /// <summary>
    /// Gets or sets the stable decision identifier used to identify or correlate this council game director decision instance with related application state.
    /// </summary>
    /// <value>The decision identifier value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public Guid DecisionId { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this council game director decision instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the expected turn value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected turn value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public long ExpectedTurn { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether approved applies to the council game director decision state.
    /// </summary>
    /// <value>The approved value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public bool Approved { get; set; }
    /// <summary>
    /// Gets or sets the normalized action value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The normalized action value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string NormalizedAction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the director name value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director name value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string DirectorName { get; set; } = "LocalGPT GameDirector";
    /// <summary>
    /// Gets or sets the director mode value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; set; }
    /// <summary>
    /// Gets or sets the director model name value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director model name value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string DirectorModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reason value that forms part of the council game director decision state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the predictions collection maintained or exposed by this council game director decision instance for downstream processing.
    /// </summary>
    /// <value>The predictions value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public IReadOnlyList<CouncilGameSubdirectorPrediction> Predictions { get; set; } = [];
    /// <summary>
    /// Gets or sets the decided at UTC associated with this council game director decision state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The decided at UTC value exposed by <see cref="CouncilGameDirectorDecision"/>.</value>
    public DateTime DecidedAtUtc { get; set; } = DateTime.UtcNow;
}
