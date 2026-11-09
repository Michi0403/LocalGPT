using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an ambient LocalGPT context holder application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="snapshot">Snapshot value supplied to the ambient LocalGPT context holder operation and used when producing its result.</param>
internal sealed class AmbientLocalGptContextHolder(AmbientLocalGptContextSnapshot snapshot)
{
    /// <summary>
    /// Gets the snapshot value that forms part of the ambient LocalGPT context holder state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The snapshot value exposed by <see cref="AmbientLocalGptContextHolder"/>.</value>
    public AmbientLocalGptContextSnapshot Snapshot { get; } = snapshot;
}
