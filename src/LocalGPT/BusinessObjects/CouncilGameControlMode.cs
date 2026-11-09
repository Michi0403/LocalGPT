using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported council game control mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CouncilGameControlMode
{
    /// <summary>
    /// Selects the human option for <see cref="CouncilGameControlMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Human,
    /// <summary>
    /// Selects the AI option for <see cref="CouncilGameControlMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Ai,
    /// <summary>
    /// Selects the shared option for <see cref="CouncilGameControlMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Shared
}
