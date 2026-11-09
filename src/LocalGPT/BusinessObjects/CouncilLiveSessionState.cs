using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents council live session state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
internal sealed class CouncilLiveSessionState : IDisposable
{
    /// <summary>
    /// Initializes a new <see cref="CouncilLiveSessionState"/> instance and captures the dependencies or initial state required by its council live session workflow.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="councilMembers">String dependency used by the council live session workflow to provide the corresponding application capability.</param>
    /// <param name="userMessage">User message value supplied to the council live session operation and used when producing its result.</param>
    /// <param name="initialTranscript">Initial transcript value supplied to the council live session operation and used when producing its result.</param>
    public CouncilLiveSessionState(
        Guid runId,
        IReadOnlyList<string> councilMembers,
        string userMessage,
        string initialTranscript)
    {
        RunId = runId;
        CouncilMembers = councilMembers.ToArray();
        UserMessage = userMessage ?? string.Empty;
        Transcript = new StringBuilder(initialTranscript ?? string.Empty);
    }

    /// <summary>
    /// Gets the sync root value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets the stable run identifier used to identify or correlate this council live session instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public Guid RunId { get; }
    /// <summary>
    /// Gets the council members collection maintained or exposed by this council live session instance for downstream processing.
    /// </summary>
    /// <value>The council members value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public IReadOnlyList<string> CouncilMembers { get; }
    /// <summary>
    /// Gets the user message value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The user message value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public string UserMessage { get; }
    /// <summary>
    /// Gets the additional user messages collection maintained or exposed by this council live session instance for downstream processing.
    /// </summary>
    /// <value>The additional user messages value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public List<string> AdditionalUserMessages { get; } = [];
    /// <summary>
    /// Gets live participant activity states keyed by the run-local participant/phase identity.
    /// </summary>
    /// <value>The participant activities value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public Dictionary<string, CouncilLiveParticipantActivityState> ParticipantActivities { get; } = new(StringComparer.Ordinal);
    /// <summary>
    /// Gets the transcript value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transcript value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public StringBuilder Transcript { get; }
    /// <summary>
    /// Gets the cancellation signal used to stop or abandon work associated with this council live session operation.
    /// </summary>
    /// <value>The cancellation value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public CancellationTokenSource Cancellation { get; } = new();
    /// <summary>
    /// Gets the started at UTC associated with this council live session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council live session state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether running applies to the council live session state.
    /// </summary>
    /// <value>The is running value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets or sets the status message value that forms part of the council live session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status message value exposed by <see cref="CouncilLiveSessionState"/>.</value>
    public string StatusMessage { get; set; } = "Preparing Council run.";
    /// <summary>
    /// Stores the internal notification scheduled state used by <see cref="CouncilLiveSessionState"/> while executing its surrounding workflow.
    /// </summary>
    public int NotificationScheduled;

    /// <summary>
    /// Releases resources owned by <see cref="CouncilLiveSessionState"/> and leaves the council live session workflow in a safely disposed state.
    /// </summary>
    /// <returns>The void dispose cancellation produced by the operation.</returns>
    public void Dispose() => Cancellation.Dispose();
}
