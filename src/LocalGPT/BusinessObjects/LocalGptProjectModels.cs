using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a local gpt project.
/// </summary>
public sealed class LocalGptProject
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets root path.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets project type.
    /// </summary>
    public string ProjectType { get; set; } = "DotNetSolution";

    /// <summary>
    /// Gets or sets solution path.
    /// </summary>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets solution search pattern.
    /// </summary>
    public string SolutionSearchPattern { get; set; } = @"(?i)\.(sln|slnx)$";

    /// <summary>
    /// Gets or sets file include pattern.
    /// </summary>
    public string FileIncludePattern { get; set; } = @"(?s).*";

    /// <summary>
    /// Gets or sets file exclude pattern.
    /// </summary>
    public string FileExcludePattern { get; set; } = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$";

    /// <summary>
    /// Gets or sets current version.
    /// </summary>
    public string CurrentVersion { get; set; } = "0.1.0";

    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets recommend git.
    /// </summary>
    public bool RecommendGit { get; set; } = true;

    /// <summary>
    /// Gets or sets is archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets topics.
    /// </summary>
    public ICollection<LocalGptProjectTopic> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets versions.
    /// </summary>
    public ICollection<LocalGptProjectVersion> Versions { get; set; } = [];

    /// <summary>
    /// Gets or sets revisions.
    /// </summary>
    public ICollection<LocalGptProjectRevision> Revisions { get; set; } = [];

    /// <summary>
    /// Gets or sets requirements.
    /// </summary>
    public ICollection<LocalGptProjectRequirement> Requirements { get; set; } = [];

    /// <summary>
    /// Gets or sets artifacts.
    /// </summary>
    public ICollection<LocalGptProjectArtifact> Artifacts { get; set; } = [];

    /// <summary>
    /// Gets or sets workspace roots.
    /// </summary>
    public ICollection<ProjectWorkspaceRoot> WorkspaceRoots { get; set; } = [];

    /// <summary>
    /// Gets or sets tracked files.
    /// </summary>
    public ICollection<LocalGptProjectTrackedFile> TrackedFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets build verifications.
    /// </summary>
    public ICollection<ProjectBuildVerification> BuildVerifications { get; set; } = [];
}

/// <summary>
/// Represents a local gpt project topic.
/// </summary>
public sealed class LocalGptProjectTopic
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }

    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets description.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Planned";

    /// <summary>
    /// Gets or sets is user approved.
    /// </summary>
    public bool IsUserApproved { get; set; }

    /// <summary>
    /// Gets or sets knowledge links.
    /// </summary>
    public ICollection<LocalGptProjectTopicKnowledgeLink> KnowledgeLinks { get; set; } = [];
}

/// <summary>
/// Represents a local gpt project version.
/// </summary>
public sealed class LocalGptProjectVersion
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public LocalGptProject? Project { get; set; }

    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets version.
    /// </summary>
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets path snapshot.
    /// </summary>
    public string PathSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets is current.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets is user confirmed.
    /// </summary>
    public bool IsUserConfirmed { get; set; }
}

/// <summary>
/// Represents a local gpt project topic knowledge link.
/// </summary>
public sealed class LocalGptProjectTopicKnowledgeLink
{
    /// <summary>
    /// Gets or sets project topic identifier.
    /// </summary>
    public Guid ProjectTopicId { get; set; }

    /// <summary>
    /// Gets or sets project topic.
    /// </summary>
    public LocalGptProjectTopic? ProjectTopic { get; set; }

    /// <summary>
    /// Gets or sets knowledge entry identifier.
    /// </summary>
    public Guid KnowledgeEntryId { get; set; }

    /// <summary>
    /// Gets or sets knowledge entry.
    /// </summary>
    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }

    /// <summary>
    /// Gets or sets linked at UTC.
    /// </summary>
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets link reason.
    /// </summary>
    public string LinkReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets linked by human.
    /// </summary>
    public bool LinkedByHuman { get; set; }
}

/// <summary>
/// Represents a local gpt project summary.
/// </summary>
public sealed record LocalGptProjectSummary(
    Guid Id,
    string Name,
    string Purpose,
    string RootPath,
    string CurrentVersion,
    string Status,
    bool RecommendGit,
    bool IsArchived,
    int TopicCount,
    int VersionCount,
    DateTime UpdatedAtUtc)
{
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName => $"{Name} ({CurrentVersion})";
}

/// <summary>
/// Represents a local gpt project details.
/// </summary>
public sealed class LocalGptProjectDetails
{
    /// <summary>
    /// Gets or sets project.
    /// </summary>
    public required LocalGptProject Project { get; init; }

    /// <summary>
    /// Gets or sets topics.
    /// </summary>
    public IReadOnlyList<LocalGptProjectTopic> Topics { get; init; } = [];

    /// <summary>
    /// Gets or sets versions.
    /// </summary>
    public IReadOnlyList<LocalGptProjectVersion> Versions { get; init; } = [];

    /// <summary>
    /// Gets or sets revisions.
    /// </summary>
    public IReadOnlyList<LocalGptProjectRevision> Revisions { get; init; } = [];

    /// <summary>
    /// Gets or sets requirements.
    /// </summary>
    public IReadOnlyList<LocalGptProjectRequirement> Requirements { get; init; } = [];

    /// <summary>
    /// Gets or sets artifacts.
    /// </summary>
    public IReadOnlyList<LocalGptProjectArtifact> Artifacts { get; init; } = [];

    /// <summary>
    /// Gets or sets workspace roots.
    /// </summary>
    public IReadOnlyList<ProjectWorkspaceRoot> WorkspaceRoots { get; init; } = [];

    /// <summary>
    /// Gets or sets tracked files.
    /// </summary>
    public IReadOnlyList<LocalGptProjectTrackedFile> TrackedFiles { get; init; } = [];

    /// <summary>
    /// Gets or sets build verifications.
    /// </summary>
    public IReadOnlyList<ProjectBuildVerification> BuildVerifications { get; init; } = [];
}

/// <summary>
/// Represents a save local gpt project request.
/// </summary>
public sealed class SaveLocalGptProjectRequest
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets root path.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets project type.
    /// </summary>
    public string ProjectType { get; set; } = "DotNetSolution";

    /// <summary>
    /// Gets or sets solution path.
    /// </summary>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets solution search pattern.
    /// </summary>
    public string SolutionSearchPattern { get; set; } = @"(?i)\.(sln|slnx)$";

    /// <summary>
    /// Gets or sets file include pattern.
    /// </summary>
    public string FileIncludePattern { get; set; } = @"(?s).*";

    /// <summary>
    /// Gets or sets file exclude pattern.
    /// </summary>
    public string FileExcludePattern { get; set; } = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$";

    /// <summary>
    /// Gets or sets current version.
    /// </summary>
    public string CurrentVersion { get; set; } = "0.1.0";

    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets recommend git.
    /// </summary>
    public bool RecommendGit { get; set; } = true;

    /// <summary>
    /// Gets or sets is archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents an add local gpt project topic request.
/// </summary>
public sealed class AddLocalGptProjectTopicRequest
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = "Planned";

    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents an add local gpt project version request.
/// </summary>
public sealed class AddLocalGptProjectVersionRequest
{
    /// <summary>
    /// Gets or sets version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets notes.
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets path snapshot.
    /// </summary>
    public string PathSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets is current.
    /// </summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a link project topic knowledge request.
/// </summary>
public sealed class LinkProjectTopicKnowledgeRequest
{
    /// <summary>
    /// Gets or sets knowledge entry identifier.
    /// </summary>
    public Guid KnowledgeEntryId { get; set; }

    /// <summary>
    /// Gets or sets link reason.
    /// </summary>
    public string LinkReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}
