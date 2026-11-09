using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Wraps the project identifier and approved authoring settings for the project.game.profile.save DX function.</summary>
public sealed class ProjectGameProfileSaveParameters
{
    /// <summary>Identifies the Game project whose authoring profile should be saved.</summary>
    /// <value>The owning project identifier.</value>
    public Guid ProjectId { get; set; }
    /// <summary>Provides the authoring settings submitted through the approval-gated AI function.</summary>
    /// <value>The Game-project authoring request.</value>
    public SaveGameProjectProfileRequest Request { get; set; } = new();
}
