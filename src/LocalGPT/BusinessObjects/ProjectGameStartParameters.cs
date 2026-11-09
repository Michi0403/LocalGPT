using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Wraps the project identifier used by the coordination-only project.game.start DX function.</summary>
public sealed class ProjectGameStartParameters
{
    /// <summary>Identifies the Game project whose latest approved build should be launched.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
}
