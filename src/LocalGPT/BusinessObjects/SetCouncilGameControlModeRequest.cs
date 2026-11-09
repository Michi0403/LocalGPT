using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for set council game control mode, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SetCouncilGameControlModeRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this set council game control mode instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets the control mode value that forms part of the set council game control mode state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The control mode value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public CouncilGameControlMode ControlMode { get; set; } = CouncilGameControlMode.Shared;
    /// <summary>
    /// Gets or sets a value indicating whether autoplay enabled applies to the set council game control mode state.
    /// </summary>
    /// <value>The autoplay enabled value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public bool AutoplayEnabled { get; set; }
    /// <summary>
    /// Gets or sets the autoplay delay milliseconds value that forms part of the set council game control mode state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The autoplay delay milliseconds value exposed by <see cref="SetCouncilGameControlModeRequest"/>.</value>
    public int AutoplayDelayMilliseconds { get; set; } = 1200;
}
