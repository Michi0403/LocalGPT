using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for council game control, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class CouncilGameControlRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this council game control instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the action value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the axis x value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The axis x value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public double? AxisX { get; set; }
    /// <summary>
    /// Gets or sets the axis y value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The axis y value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public double? AxisY { get; set; }
    /// <summary>
    /// Gets or sets the aim x value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The aim x value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public int? AimX { get; set; }
    /// <summary>
    /// Gets or sets the aim y value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The aim y value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public int? AimY { get; set; }
    /// <summary>
    /// Gets or sets the source value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string Source { get; set; } = "Human";
    /// <summary>
    /// Gets or sets the actor name value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor name value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string ActorName { get; set; } = "Human User";
    /// <summary>
    /// Gets or sets the actor kind value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public CouncilGameActorKind ActorKind { get; set; } = CouncilGameActorKind.Player;
    /// <summary>
    /// Gets or sets the stable runtime class key used to identify or correlate this council game control instance with related application state.
    /// </summary>
    /// <value>The runtime class key value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the expected turn value that forms part of the council game control state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected turn value exposed by <see cref="CouncilGameControlRequest"/>.</value>
    public long? ExpectedTurn { get; set; }
}
