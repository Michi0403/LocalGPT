using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Stores one named ASCII actor joint so persistent trainer/creature rigs can be rendered consistently across poses.</summary>
public sealed class CouncilAsciiActorJoint
{
    /// <summary>
    /// Gets or sets the name value that forms part of the council ascii actor joint state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="CouncilAsciiActorJoint"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the zero-based horizontal anchor coordinate inside the actor rig.</summary>
    /// <value>The x value exposed by <see cref="CouncilAsciiActorJoint"/>.</value>
    public int X { get; set; }
    /// <summary>Gets or sets the zero-based vertical anchor coordinate inside the actor rig.</summary>
    /// <value>The y value exposed by <see cref="CouncilAsciiActorJoint"/>.</value>
    public int Y { get; set; }
}
