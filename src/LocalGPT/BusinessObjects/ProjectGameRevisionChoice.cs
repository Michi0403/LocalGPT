using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>A completed, approved Game Studio revision available for direct play in the shared Chat game surface.</summary>
/// <param name="ProjectId">Identifier of the project to use for this operation.</param>
/// <param name="RevisionId">Identifier of the revision to use for this operation.</param>
/// <param name="ProjectName">Project name value supplied to the project game revision choice operation and used when producing its result.</param>
/// <param name="RevisionName">Revision name value supplied to the project game revision choice operation and used when producing its result.</param>
/// <param name="ProjectVersion">Project version value supplied to the project game revision choice operation and used when producing its result.</param>
/// <param name="IsCurrent">Value indicating whether current should apply to this operation.</param>
public sealed record ProjectGameRevisionChoice(Guid ProjectId, Guid RevisionId, string ProjectName,
    string RevisionName, string ProjectVersion, bool IsCurrent)
{
    /// <summary>Shows project identity and its exact immutable revision without hiding the revision boundary.</summary>
    /// <value>The display name value exposed by <see cref="ProjectGameRevisionChoice"/>.</value>
    public string DisplayName => $"{ProjectName} · {RevisionName} [{RevisionId.ToString("N")[..8]}]{(IsCurrent ? " · current" : string.Empty)}";
}
