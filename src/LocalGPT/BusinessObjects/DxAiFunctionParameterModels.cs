using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an inspect debug artifact parameters.
/// </summary>
public sealed class InspectDebugArtifactParameters
{
    /// <summary>
    /// Gets or sets file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a sqlite table read parameters.
/// </summary>
public sealed class SqliteTableReadParameters
{
    /// <summary>
    /// Gets or sets table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets take.
    /// </summary>
    public int Take { get; set; } = 50;
}

/// <summary>
/// Represents a sqlite row upsert parameters.
/// </summary>
public sealed class SqliteRowUpsertParameters
{
    /// <summary>
    /// Gets or sets table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets row identifier.
    /// </summary>
    public long? RowId { get; set; }
    /// <summary>
    /// Gets or sets values.
    /// </summary>
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents a sqlite row delete parameters.
/// </summary>
public sealed class SqliteRowDeleteParameters
{
    /// <summary>
    /// Gets or sets table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets row identifier.
    /// </summary>
    public long RowId { get; set; }
}

/// <summary>
/// Represents a project text document import parameters.
/// </summary>
public sealed class ProjectTextDocumentImportParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation review list parameters.
/// </summary>
public sealed class CodeGenerationReviewListParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets take.
    /// </summary>
    public int Take { get; set; } = 20;
}

/// <summary>
/// Represents a code generation review get parameters.
/// </summary>
public sealed class CodeGenerationReviewGetParameters
{
    /// <summary>
    /// Gets or sets review identifier.
    /// </summary>
    public Guid ReviewId { get; set; }
}

/// <summary>
/// Represents a code generation review execute parameters.
/// </summary>
public sealed class CodeGenerationReviewExecuteParameters
{
    /// <summary>
    /// Gets or sets review identifier.
    /// </summary>
    public Guid ReviewId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public ExecuteCodeGenerationReviewRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a code generation review reject parameters.
/// </summary>
public sealed class CodeGenerationReviewRejectParameters
{
    /// <summary>
    /// Gets or sets review identifier.
    /// </summary>
    public Guid ReviewId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public RejectCodeGenerationReviewRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a local gpt project list parameters.
/// </summary>
public sealed class LocalGptProjectListParameters
{
    /// <summary>
    /// Gets or sets include archived.
    /// </summary>
    public bool IncludeArchived { get; set; }
}

/// <summary>
/// Represents a local gpt project get parameters.
/// </summary>
public sealed class LocalGptProjectGetParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
}

/// <summary>
/// Represents a recent application log list parameters.
/// </summary>
public sealed class RecentApplicationLogListParameters
{
    /// <summary>
    /// Gets or sets minimum level.
    /// </summary>
    public string MinimumLevel { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets take.
    /// </summary>
    public int Take { get; set; } = 12;
}

/// <summary>
/// Represents a council knowledge list parameters.
/// </summary>
public sealed class CouncilKnowledgeListParameters
{
    /// <summary>
    /// Gets or sets include archived.
    /// </summary>
    public bool IncludeArchived { get; set; }
    /// <summary>
    /// Gets or sets take.
    /// </summary>
    public int Take { get; set; } = 8;
}

/// <summary>
/// Represents a chat memory conversation list parameters.
/// </summary>
public sealed class ChatMemoryConversationListParameters
{
    /// <summary>
    /// Gets or sets take.
    /// </summary>
    public int Take { get; set; } = 12;
}

/// <summary>
/// Represents a human collaboration request parameters.
/// </summary>
public sealed class HumanCollaborationRequestParameters
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets requested role.
    /// </summary>
    public string RequestedRole { get; set; } = "Human collaborator";
    /// <summary>
    /// Gets or sets response prompt.
    /// </summary>
    public string ResponsePrompt { get; set; } = "Your response";
    /// <summary>
    /// Gets or sets suggested responses.
    /// </summary>
    public string[]? SuggestedResponses { get; set; }
    /// <summary>
    /// Gets or sets prefill text.
    /// </summary>
    public string PrefillText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets allow free text.
    /// </summary>
    public bool AllowFreeText { get; set; } = true;
    /// <summary>
    /// Gets or sets question scope.
    /// </summary>
    public string QuestionScope { get; set; } = "Member";
    /// <summary>
    /// Gets or sets gate.
    /// </summary>
    public string Gate { get; set; } = "None";
    /// <summary>
    /// Gets or sets target members.
    /// </summary>
    public string[]? TargetMembers { get; set; }
    /// <summary>
    /// Gets or sets required before completion.
    /// </summary>
    public bool RequiredBeforeCompletion { get; set; }
}

/// <summary>
/// Represents a project architecture get parameters.
/// </summary>
public sealed class ProjectArchitectureGetParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
}

/// <summary>
/// Represents a project revision save parameters.
/// </summary>
public sealed class ProjectRevisionSaveParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public SaveProjectRevisionRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project requirement save parameters.
/// </summary>
public sealed class ProjectRequirementSaveParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public SaveProjectRequirementRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project artifact save parameters.
/// </summary>
public sealed class ProjectArtifactSaveParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public SaveProjectArtifactRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project maintenance get parameters.
/// </summary>
public sealed class ProjectMaintenanceGetParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid? RevisionId { get; set; }
}

/// <summary>
/// Represents a project workspace environment save parameters.
/// </summary>
public sealed class ProjectWorkspaceEnvironmentSaveParameters
{
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public SaveProjectWorkspaceRootRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project workspace environment assess parameters.
/// </summary>
public sealed class ProjectWorkspaceEnvironmentAssessParameters
{
    /// <summary>
    /// Gets or sets workspace root identifier.
    /// </summary>
    public Guid WorkspaceRootId { get; set; }
}

/// <summary>
/// Represents a project revision workspace register parameters.
/// </summary>
public sealed class ProjectRevisionWorkspaceRegisterParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets source root path.
    /// </summary>
    public string SourceRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets solution path.
    /// </summary>
    public string SolutionPath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a project files scan parameters.
/// </summary>
public sealed class ProjectFilesScanParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public ScanProjectFilesRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project file patterns save parameters.
/// </summary>
public sealed class ProjectFilePatternsSaveParameters
{
    /// <summary>
    /// Gets or sets tracked file identifier.
    /// </summary>
    public Guid TrackedFileId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public SaveTrackedFilePatternRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project revision build verify parameters.
/// </summary>
public sealed class ProjectRevisionBuildVerifyParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public RunProjectBuildVerificationRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project council build review record parameters.
/// </summary>
public sealed class ProjectCouncilBuildReviewRecordParameters
{
    /// <summary>
    /// Gets or sets verification identifier.
    /// </summary>
    public Guid VerificationId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public RecordCouncilBuildReviewRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project revision approve parameters.
/// </summary>
public sealed class ProjectRevisionApproveParameters
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets revision identifier.
    /// </summary>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets request.
    /// </summary>
    public ApproveRevisionReadyForTestRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a public service method invocation parameters.
/// </summary>
public sealed class PublicServiceMethodInvocationParameters
{
    /// <summary>
    /// Gets or sets catalog key.
    /// </summary>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public JsonElement Parameters { get; set; }
}

/// <summary>
/// Represents a regex pattern list parameters.
/// </summary>
public sealed class RegexPatternListParameters
{
    /// <summary>
    /// Gets or sets take.
    /// </summary>
    public int Take { get; set; } = 5000;
    /// <summary>
    /// Gets or sets prefix.
    /// </summary>
    public string Prefix { get; set; } = string.Empty;
}
