using System.Text.Json;

namespace LocalGPT.BusinessObjects;

public sealed class InspectDebugArtifactParameters
{
    public string FilePath { get; set; } = string.Empty;
}

public sealed class SqliteTableReadParameters
{
    public string TableName { get; set; } = string.Empty;
    public int Take { get; set; } = 50;
}

public sealed class SqliteRowUpsertParameters
{
    public string TableName { get; set; } = string.Empty;
    public long? RowId { get; set; }
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SqliteRowDeleteParameters
{
    public string TableName { get; set; } = string.Empty;
    public long RowId { get; set; }
}

public sealed class ProjectTextDocumentImportParameters
{
    public Guid ProjectId { get; set; }
    public Guid? RevisionId { get; set; }
    public string FilePath { get; set; } = string.Empty;
}

public sealed class CodeGenerationReviewListParameters
{
    public Guid? ProjectId { get; set; }
    public int Take { get; set; } = 20;
}

public sealed class CodeGenerationReviewGetParameters
{
    public Guid ReviewId { get; set; }
}

public sealed class CodeGenerationReviewExecuteParameters
{
    public Guid ReviewId { get; set; }
    public ExecuteCodeGenerationReviewRequest Request { get; set; } = new();
}

public sealed class CodeGenerationReviewRejectParameters
{
    public Guid ReviewId { get; set; }
    public RejectCodeGenerationReviewRequest Request { get; set; } = new();
}

public sealed class LocalGptProjectListParameters
{
    public bool IncludeArchived { get; set; }
}

public sealed class LocalGptProjectGetParameters
{
    public Guid ProjectId { get; set; }
}

public sealed class RecentApplicationLogListParameters
{
    public string MinimumLevel { get; set; } = "Warning";
    public int Take { get; set; } = 12;
}

public sealed class CouncilKnowledgeListParameters
{
    public bool IncludeArchived { get; set; }
    public int Take { get; set; } = 8;
}

public sealed class ChatMemoryConversationListParameters
{
    public int Take { get; set; } = 12;
}

public sealed class HumanCollaborationRequestParameters
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = "Human collaborator";
    public string ResponsePrompt { get; set; } = "Your response";
    public string[]? SuggestedResponses { get; set; }
    public string PrefillText { get; set; } = string.Empty;
    public bool AllowFreeText { get; set; } = true;
    public string QuestionScope { get; set; } = "Member";
    public string Gate { get; set; } = "None";
    public string[]? TargetMembers { get; set; }
    public bool RequiredBeforeCompletion { get; set; }
}

public sealed class ProjectArchitectureGetParameters
{
    public Guid ProjectId { get; set; }
}

public sealed class ProjectRevisionSaveParameters
{
    public Guid ProjectId { get; set; }
    public SaveProjectRevisionRequest Request { get; set; } = new();
}

public sealed class ProjectRequirementSaveParameters
{
    public Guid ProjectId { get; set; }
    public SaveProjectRequirementRequest Request { get; set; } = new();
}

public sealed class ProjectArtifactSaveParameters
{
    public Guid ProjectId { get; set; }
    public SaveProjectArtifactRequest Request { get; set; } = new();
}

public sealed class ProjectMaintenanceGetParameters
{
    public Guid ProjectId { get; set; }
    public Guid? RevisionId { get; set; }
}

public sealed class ProjectRevisionWorkspaceRegisterParameters
{
    public Guid ProjectId { get; set; }
    public Guid RevisionId { get; set; }
    public string SourceRootPath { get; set; } = string.Empty;
    public string SolutionPath { get; set; } = string.Empty;
}

public sealed class ProjectFilesScanParameters
{
    public Guid ProjectId { get; set; }
    public ScanProjectFilesRequest Request { get; set; } = new();
}

public sealed class ProjectFilePatternsSaveParameters
{
    public Guid TrackedFileId { get; set; }
    public SaveTrackedFilePatternRequest Request { get; set; } = new();
}

public sealed class ProjectRevisionBuildVerifyParameters
{
    public Guid ProjectId { get; set; }
    public RunProjectBuildVerificationRequest Request { get; set; } = new();
}

public sealed class ProjectCouncilBuildReviewRecordParameters
{
    public Guid VerificationId { get; set; }
    public RecordCouncilBuildReviewRequest Request { get; set; } = new();
}

public sealed class ProjectRevisionApproveParameters
{
    public Guid ProjectId { get; set; }
    public Guid RevisionId { get; set; }
    public ApproveRevisionReadyForTestRequest Request { get; set; } = new();
}

public sealed class PublicServiceMethodInvocationParameters
{
    public string CatalogKey { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
}

public sealed class RegexPatternListParameters
{
    public int Take { get; set; } = 5000;
    public string Prefix { get; set; } = string.Empty;
}
