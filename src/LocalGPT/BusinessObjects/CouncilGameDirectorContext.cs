using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Provides the immutable input used by the authoritative GameDirector for one proposed action.</summary>
public sealed class CouncilGameDirectorContext
{
    /// <summary>
    /// Gets or sets the session value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The session value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public required CouncilGameSessionSnapshot Session { get; init; }
    /// <summary>
    /// Gets or sets the proposal value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public required CouncilGameControlRequest Proposal { get; init; }
    /// <summary>
    /// Gets or sets the normalized action value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The normalized action value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public required string NormalizedAction { get; init; }
    /// <summary>
    /// Gets or sets the director mode value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director mode value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public CouncilGameDirectorMode DirectorMode { get; init; }
    /// <summary>
    /// Gets or sets the director model name value that forms part of the council game director context state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The director model name value exposed by <see cref="CouncilGameDirectorContext"/>.</value>
    public string DirectorModelName { get; init; } = string.Empty;
}
