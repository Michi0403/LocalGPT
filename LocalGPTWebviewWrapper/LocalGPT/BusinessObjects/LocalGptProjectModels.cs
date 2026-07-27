using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects;

public sealed class LocalGptProject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string Purpose { get; set; } = string.Empty;

    public string RootPath { get; set; } = string.Empty;

    public string ProjectType { get; set; } = "DotNetSolution";

    public string SolutionPath { get; set; } = string.Empty;

    public string SolutionSearchPattern { get; set; } = @"(?i)\.(sln|slnx)$";

    public string FileIncludePattern { get; set; } = @"(?s).*";

    public string FileExcludePattern { get; set; } = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$";

    public string CurrentVersion { get; set; } = "0.1.0";

    public string Status { get; set; } = "Active";

    public bool RecommendGit { get; set; } = true;

    public bool IsArchived { get; set; }

    public ICollection<LocalGptProjectTopic> Topics { get; set; } = [];

    public ICollection<LocalGptProjectVersion> Versions { get; set; } = [];

    public ICollection<LocalGptProjectRevision> Revisions { get; set; } = [];

    public ICollection<LocalGptProjectRequirement> Requirements { get; set; } = [];

    public ICollection<LocalGptProjectArtifact> Artifacts { get; set; } = [];

    public ICollection<ProjectWorkspaceRoot> WorkspaceRoots { get; set; } = [];

    public ICollection<LocalGptProjectTrackedFile> TrackedFiles { get; set; } = [];

    public ICollection<ProjectBuildVerification> BuildVerifications { get; set; } = [];
}

public sealed class LocalGptProjectTopic
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public LocalGptProject? Project { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "Planned";

    public bool IsUserApproved { get; set; }

    public ICollection<LocalGptProjectTopicKnowledgeLink> KnowledgeLinks { get; set; } = [];
}

public sealed class LocalGptProjectVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public LocalGptProject? Project { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public string Version { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string Notes { get; set; } = string.Empty;

    public string PathSnapshot { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public bool IsUserConfirmed { get; set; }
}

public sealed class LocalGptProjectTopicKnowledgeLink
{
    public Guid ProjectTopicId { get; set; }

    public LocalGptProjectTopic? ProjectTopic { get; set; }

    public Guid KnowledgeEntryId { get; set; }

    public CouncilKnowledgeEntry? KnowledgeEntry { get; set; }

    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    public string LinkReason { get; set; } = string.Empty;

    public bool LinkedByHuman { get; set; }
}

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
    public string DisplayName => $"{Name} ({CurrentVersion})";
}

public sealed class LocalGptProjectDetails
{
    public required LocalGptProject Project { get; init; }

    public IReadOnlyList<LocalGptProjectTopic> Topics { get; init; } = [];

    public IReadOnlyList<LocalGptProjectVersion> Versions { get; init; } = [];

    public IReadOnlyList<LocalGptProjectRevision> Revisions { get; init; } = [];

    public IReadOnlyList<LocalGptProjectRequirement> Requirements { get; init; } = [];

    public IReadOnlyList<LocalGptProjectArtifact> Artifacts { get; init; } = [];

    public IReadOnlyList<ProjectWorkspaceRoot> WorkspaceRoots { get; init; } = [];

    public IReadOnlyList<LocalGptProjectTrackedFile> TrackedFiles { get; init; } = [];

    public IReadOnlyList<ProjectBuildVerification> BuildVerifications { get; init; } = [];
}

public sealed class SaveLocalGptProjectRequest
{
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string RootPath { get; set; } = string.Empty;

    public string ProjectType { get; set; } = "DotNetSolution";

    public string SolutionPath { get; set; } = string.Empty;

    public string SolutionSearchPattern { get; set; } = @"(?i)\.(sln|slnx)$";

    public string FileIncludePattern { get; set; } = @"(?s).*";

    public string FileExcludePattern { get; set; } = @"(?i)(^|[\\/])(bin|obj|node_modules|\.git|\.vs|artifacts|security|secrets?)([\\/]|$)|(^|[\\/])(\.env(?:\..*)?|[^\\/]+\.(?:pfx|p12|key|pem))$";

    public string CurrentVersion { get; set; } = "0.1.0";

    public string Status { get; set; } = "Active";

    public bool RecommendGit { get; set; } = true;

    public bool IsArchived { get; set; }

    public bool UserConfirmed { get; set; }
}

public sealed class AddLocalGptProjectTopicRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "Planned";

    public bool UserConfirmed { get; set; }
}

public sealed class AddLocalGptProjectVersionRequest
{
    public string Version { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string PathSnapshot { get; set; } = string.Empty;

    public bool IsCurrent { get; set; } = true;

    public bool UserConfirmed { get; set; }
}

public sealed class LinkProjectTopicKnowledgeRequest
{
    public Guid KnowledgeEntryId { get; set; }

    public string LinkReason { get; set; } = string.Empty;

    public bool UserConfirmed { get; set; }
}
