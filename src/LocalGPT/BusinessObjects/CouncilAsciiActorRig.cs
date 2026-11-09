using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Stores a small persistent ASCII actor rig that keeps model joints connected while presentation poses change.</summary>
public sealed class CouncilAsciiActorRig
{
    /// <summary>
    /// Gets or sets the stable rig key used to identify or correlate this council ascii actor rig instance with related application state.
    /// </summary>
    /// <value>The rig key value exposed by <see cref="CouncilAsciiActorRig"/>.</value>
    public string RigKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the width value that forms part of the council ascii actor rig state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="CouncilAsciiActorRig"/>.</value>
    public int Width { get; set; } = 13;
    /// <summary>
    /// Gets or sets the height value that forms part of the council ascii actor rig state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="CouncilAsciiActorRig"/>.</value>
    public int Height { get; set; } = 6;
    /// <summary>Gets or sets the identity glyph rendered at the rig head anchor.</summary>
    /// <value>The head glyph value exposed by <see cref="CouncilAsciiActorRig"/>.</value>
    public char HeadGlyph { get; set; } = '@';
    /// <summary>Gets or sets the identity glyph rendered at the rig torso anchor.</summary>
    /// <value>The torso glyph value exposed by <see cref="CouncilAsciiActorRig"/>.</value>
    public char TorsoGlyph { get; set; } = '#';
    /// <summary>
    /// Gets or sets the joints collection maintained or exposed by this council ascii actor rig instance for downstream processing.
    /// </summary>
    /// <value>The joints value exposed by <see cref="CouncilAsciiActorRig"/>.</value>
    public List<CouncilAsciiActorJoint> Joints { get; set; } = [];
}
