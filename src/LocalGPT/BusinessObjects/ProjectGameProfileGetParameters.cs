using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Wraps the project identifier used by the read-only project.game.profile.get DX function.</summary>
public sealed class ProjectGameProfileGetParameters
{
    /// <summary>Identifies the Game project whose editable authoring profile should be read.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
}
