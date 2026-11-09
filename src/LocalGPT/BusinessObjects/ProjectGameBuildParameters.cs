using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Wraps the project identifier and approved build request for the project.game.build DX function.</summary>
public sealed class ProjectGameBuildParameters
{
    /// <summary>Identifies the Game project to compile.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Provides the build settings submitted through the approval-gated AI function.</summary>
    /// <value>The Game-project build request.</value>
    public BuildProjectGameRequest Request { get; set; } = new();
}
