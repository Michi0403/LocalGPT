using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Reports the durable project artifact and runtime definition created by a successful Game-project build.
/// </summary>
public sealed class ProjectGameBuildResult
{
    /// <summary>Identifies the project artifact that persists the compiled runtime definition.</summary>
    /// <value>The durable `GameBuild / Runtime Definition` artifact identifier.</value>
    public Guid ArtifactId { get; set; }
    /// <summary>Identifies the current project revision associated with the build when one was available.</summary>
    /// <value>The revision identifier, or <c>null</c> for a build without a current revision.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>Provides the compiled definition that GameDirector can consume.</summary>
    /// <value>The validated project-owned runtime definition.</value>
    public required ProjectGameDefinition Definition { get; set; }
}
