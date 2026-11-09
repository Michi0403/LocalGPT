using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Describes one factory-created actor instance and its Council assignment slot.</summary>
public sealed class CouncilGameActorRuntimeDescriptor
{
    /// <summary>
    /// Gets or sets the stable instance key used to identify or correlate this council game actor runtime instance with related application state.
    /// </summary>
    /// <value>The instance key value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string InstanceKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor kind value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public CouncilGameActorKind ActorKind { get; set; }
    /// <summary>
    /// Gets or sets the stable runtime class key used to identify or correlate this council game actor runtime instance with related application state.
    /// </summary>
    /// <value>The runtime class key value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the archetype value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The archetype value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string Archetype { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council role value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council role value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string CouncilRole { get; set; } = "World Actor";
    /// <summary>
    /// Gets or sets the council assignment group value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council assignment group value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string CouncilAssignmentGroup { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council assignment slot value that forms part of the council game actor runtime state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council assignment slot value exposed by <see cref="CouncilGameActorRuntimeDescriptor"/>.</value>
    public string CouncilAssignmentSlot { get; set; } = string.Empty;
}
