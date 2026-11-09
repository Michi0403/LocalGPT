using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Holds one live participant stream separately from the ordered Council transcript so parallel AI hosts remain visible without interleaving provider markup.
/// </summary>
/// <param name="activityKey">Activity key value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="modelName">Model name value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="phase">Phase value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="role">Role value supplied to the council live participant activity operation and used when producing its result.</param>
/// <param name="routeLabel">Route label value supplied to the council live participant activity operation and used when producing its result.</param>
internal sealed class CouncilLiveParticipantActivityState(
    string activityKey,
    string modelName,
    string phase,
    string role,
    string routeLabel)
{
    /// <summary>
    /// Gets the stable activity key used to identify or correlate this council live participant activity instance with related application state.
    /// </summary>
    /// <value>The activity key value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string ActivityKey { get; } = activityKey;
    /// <summary>
    /// Gets the model name value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string ModelName { get; } = modelName;
    /// <summary>
    /// Gets the phase value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The phase value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string Phase { get; } = phase;
    /// <summary>
    /// Gets the role value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The role value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string Role { get; } = role;
    /// <summary>
    /// Gets the route label value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route label value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string RouteLabel { get; } = routeLabel;
    /// <summary>
    /// Gets the content value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public StringBuilder Content { get; } = new();
    /// <summary>Gets or sets the number of mutations applied to <see cref="Content"/>; used to reuse an unchanged text snapshot.</summary>
    /// <value>A counter incremented by every append or trim of the live participant stream.</value>
    public long ContentVersion { get; set; }
    /// <summary>Gets or sets the last materialized text of <see cref="Content"/>.</summary>
    /// <value>The complete stream text at <see cref="ContentSnapshotVersion"/>.</value>
    public string ContentSnapshot { get; set; } = string.Empty;
    /// <summary>Gets or sets the <see cref="ContentVersion"/> at which <see cref="ContentSnapshot"/> was materialized.</summary>
    /// <value>-1 until the first snapshot is taken.</value>
    public long ContentSnapshotVersion { get; set; } = -1;
    /// <summary>Gets or sets the authoritative final participant answer once the model request completes.</summary>
    /// <value>The final content value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string FinalContent { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status message value that forms part of the council live participant activity state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status message value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public string StatusMessage { get; set; } = "Waiting for the model runtime.";
    /// <summary>
    /// Gets or sets a value indicating whether running applies to the council live participant activity state.
    /// </summary>
    /// <value>The is running value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets the started at UTC associated with this council live participant activity state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council live participant activity state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilLiveParticipantActivityState"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
