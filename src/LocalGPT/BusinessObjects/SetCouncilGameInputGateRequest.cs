using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the input contract for set council game input gate, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SetCouncilGameInputGateRequest
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this set council game input gate instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public Guid SessionId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether human input required applies to the set council game input gate state.
    /// </summary>
    /// <value>The human input required value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public bool HumanInputRequired { get; set; }
    /// <summary>
    /// Gets or sets the legal actions collection maintained or exposed by this set council game input gate instance for downstream processing.
    /// </summary>
    /// <value>The legal actions value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public IReadOnlyList<string> LegalActions { get; set; } = [];
    /// <summary>
    /// Gets or sets the reason value that forms part of the set council game input gate state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The reason value exposed by <see cref="SetCouncilGameInputGateRequest"/>.</value>
    public string Reason { get; set; } = string.Empty;
}
