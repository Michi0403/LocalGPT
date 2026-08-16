using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an inspect debug artifact parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class InspectDebugArtifactParameters
{
    /// <summary>
    /// Gets or sets the file path used by this inspect debug artifact parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file path value exposed by <see cref="InspectDebugArtifactParameters"/>.</value>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a sqlite table read parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SqliteTableReadParameters
{
    /// <summary>
    /// Gets or sets the table name value that forms part of the sqlite table read parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The table name value exposed by <see cref="SqliteTableReadParameters"/>.</value>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the take value that forms part of the sqlite table read parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="SqliteTableReadParameters"/>.</value>
    public int Take { get; set; } = 50;
}

/// <summary>
/// Represents a sqlite row upsert parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SqliteRowUpsertParameters
{
    /// <summary>
    /// Gets or sets the table name value that forms part of the sqlite row upsert parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The table name value exposed by <see cref="SqliteRowUpsertParameters"/>.</value>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable row identifier used to identify or correlate this sqlite row upsert parameters instance with related application state.
    /// </summary>
    /// <value>The row identifier value exposed by <see cref="SqliteRowUpsertParameters"/>.</value>
    public long? RowId { get; set; }
    /// <summary>
    /// Gets or sets the values collection maintained or exposed by this sqlite row upsert parameters instance for downstream processing.
    /// </summary>
    /// <value>The values value exposed by <see cref="SqliteRowUpsertParameters"/>.</value>
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents a sqlite row delete parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SqliteRowDeleteParameters
{
    /// <summary>
    /// Gets or sets the table name value that forms part of the sqlite row delete parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The table name value exposed by <see cref="SqliteRowDeleteParameters"/>.</value>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable row identifier used to identify or correlate this sqlite row delete parameters instance with related application state.
    /// </summary>
    /// <value>The row identifier value exposed by <see cref="SqliteRowDeleteParameters"/>.</value>
    public long RowId { get; set; }
}

/// <summary>
/// Represents a project text document import parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectTextDocumentImportParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project text document import parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectTextDocumentImportParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project text document import parameters instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectTextDocumentImportParameters"/>.</value>
    public Guid? RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the file path used by this project text document import parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file path value exposed by <see cref="ProjectTextDocumentImportParameters"/>.</value>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation review list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewListParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this code generation review list parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="CodeGenerationReviewListParameters"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the take value that forms part of the code generation review list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="CodeGenerationReviewListParameters"/>.</value>
    public int Take { get; set; } = 20;
}

/// <summary>
/// Represents a code generation review get parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewGetParameters
{
    /// <summary>
    /// Gets or sets the stable review identifier used to identify or correlate this code generation review get parameters instance with related application state.
    /// </summary>
    /// <value>The review identifier value exposed by <see cref="CodeGenerationReviewGetParameters"/>.</value>
    public Guid ReviewId { get; set; }
}

/// <summary>
/// Represents a code generation review execute parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewExecuteParameters
{
    /// <summary>
    /// Gets or sets the stable review identifier used to identify or correlate this code generation review execute parameters instance with related application state.
    /// </summary>
    /// <value>The review identifier value exposed by <see cref="CodeGenerationReviewExecuteParameters"/>.</value>
    public Guid ReviewId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the code generation review execute parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="CodeGenerationReviewExecuteParameters"/>.</value>
    public ExecuteCodeGenerationReviewRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a code generation review reject parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewRejectParameters
{
    /// <summary>
    /// Gets or sets the stable review identifier used to identify or correlate this code generation review reject parameters instance with related application state.
    /// </summary>
    /// <value>The review identifier value exposed by <see cref="CodeGenerationReviewRejectParameters"/>.</value>
    public Guid ReviewId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the code generation review reject parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="CodeGenerationReviewRejectParameters"/>.</value>
    public RejectCodeGenerationReviewRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a LocalGPT project list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectListParameters
{
    /// <summary>
    /// Gets or sets a value indicating whether archived applies to the LocalGPT project list parameters state.
    /// </summary>
    /// <value>The include archived value exposed by <see cref="LocalGptProjectListParameters"/>.</value>
    public bool IncludeArchived { get; set; }
}

/// <summary>
/// Represents a LocalGPT project get parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class LocalGptProjectGetParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this LocalGPT project get parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="LocalGptProjectGetParameters"/>.</value>
    public Guid ProjectId { get; set; }
}

/// <summary>
/// Represents a recent application log list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RecentApplicationLogListParameters
{
    /// <summary>
    /// Gets or sets the minimum level value that forms part of the recent application log list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The minimum level value exposed by <see cref="RecentApplicationLogListParameters"/>.</value>
    public string MinimumLevel { get; set; } = "Warning";
    /// <summary>
    /// Gets or sets the take value that forms part of the recent application log list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="RecentApplicationLogListParameters"/>.</value>
    public int Take { get; set; } = 12;
}

/// <summary>
/// Represents a council knowledge list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilKnowledgeListParameters
{
    /// <summary>
    /// Gets or sets a value indicating whether archived applies to the council knowledge list parameters state.
    /// </summary>
    /// <value>The include archived value exposed by <see cref="CouncilKnowledgeListParameters"/>.</value>
    public bool IncludeArchived { get; set; }
    /// <summary>Gets or sets an optional case-insensitive topic/content/tag filter so Council roles can retrieve relevant local knowledge without listing unrelated entries.</summary>
    /// <value>The optional bounded knowledge query.</value>
    public string Query { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the take value that forms part of the council knowledge list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="CouncilKnowledgeListParameters"/>.</value>
    public int Take { get; set; } = 8;
}

/// <summary>
/// Represents a chat memory conversation list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ChatMemoryConversationListParameters
{
    /// <summary>
    /// Gets or sets the take value that forms part of the chat memory conversation list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="ChatMemoryConversationListParameters"/>.</value>
    public int Take { get; set; } = 12;
}

/// <summary>
/// Represents a human collaboration request parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class HumanCollaborationRequestParameters
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the title value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the requested role value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested role value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string RequestedRole { get; set; } = "Human collaborator";
    /// <summary>
    /// Gets or sets the response prompt value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response prompt value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string ResponsePrompt { get; set; } = "Your response";
    /// <summary>
    /// Gets or sets the suggested responses value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The suggested responses value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string[]? SuggestedResponses { get; set; }
    /// <summary>
    /// Gets or sets the prefill text value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prefill text value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string PrefillText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether free text applies to the human collaboration request parameters state.
    /// </summary>
    /// <value>The allow free text value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public bool AllowFreeText { get; set; } = true;
    /// <summary>
    /// Gets or sets the question scope value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The question scope value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string QuestionScope { get; set; } = "Member";
    /// <summary>
    /// Gets or sets the gate value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The gate value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string Gate { get; set; } = "None";
    /// <summary>
    /// Gets or sets the target members value that forms part of the human collaboration request parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target members value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public string[]? TargetMembers { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether required before completion applies to the human collaboration request parameters state.
    /// </summary>
    /// <value>The required before completion value exposed by <see cref="HumanCollaborationRequestParameters"/>.</value>
    public bool RequiredBeforeCompletion { get; set; }
}

/// <summary>
/// Represents a project architecture get parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectArchitectureGetParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project architecture get parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectArchitectureGetParameters"/>.</value>
    public Guid ProjectId { get; set; }
}

/// <summary>
/// Represents a project revision save parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectRevisionSaveParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project revision save parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectRevisionSaveParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project revision save parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectRevisionSaveParameters"/>.</value>
    public SaveProjectRevisionRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project requirement save parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectRequirementSaveParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project requirement save parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectRequirementSaveParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project requirement save parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectRequirementSaveParameters"/>.</value>
    public SaveProjectRequirementRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project artifact save parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectArtifactSaveParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project artifact save parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectArtifactSaveParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project artifact save parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectArtifactSaveParameters"/>.</value>
    public SaveProjectArtifactRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project maintenance get parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectMaintenanceGetParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project maintenance get parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectMaintenanceGetParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project maintenance get parameters instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectMaintenanceGetParameters"/>.</value>
    public Guid? RevisionId { get; set; }
}

/// <summary>
/// Represents a project workspace environment save parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectWorkspaceEnvironmentSaveParameters
{
    /// <summary>
    /// Gets or sets the request value that forms part of the project workspace environment save parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectWorkspaceEnvironmentSaveParameters"/>.</value>
    public SaveProjectWorkspaceRootRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project workspace environment assess parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectWorkspaceEnvironmentAssessParameters
{
    /// <summary>
    /// Gets or sets the stable workspace root identifier used to identify or correlate this project workspace environment assess parameters instance with related application state.
    /// </summary>
    /// <value>The workspace root identifier value exposed by <see cref="ProjectWorkspaceEnvironmentAssessParameters"/>.</value>
    public Guid WorkspaceRootId { get; set; }
}

/// <summary>
/// Represents a project revision workspace register parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectRevisionWorkspaceRegisterParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project revision workspace register parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectRevisionWorkspaceRegisterParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project revision workspace register parameters instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectRevisionWorkspaceRegisterParameters"/>.</value>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the source root path used by this project revision workspace register parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The source root path value exposed by <see cref="ProjectRevisionWorkspaceRegisterParameters"/>.</value>
    public string SourceRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the solution path used by this project revision workspace register parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The solution path value exposed by <see cref="ProjectRevisionWorkspaceRegisterParameters"/>.</value>
    public string SolutionPath { get; set; } = string.Empty;
}

/// <summary>
/// Represents a project files scan parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectFilesScanParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project files scan parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectFilesScanParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project files scan parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectFilesScanParameters"/>.</value>
    public ScanProjectFilesRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project file patterns save parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectFilePatternsSaveParameters
{
    /// <summary>
    /// Gets or sets the stable tracked file identifier used to identify or correlate this project file patterns save parameters instance with related application state.
    /// </summary>
    /// <value>The tracked file identifier value exposed by <see cref="ProjectFilePatternsSaveParameters"/>.</value>
    public Guid TrackedFileId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project file patterns save parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectFilePatternsSaveParameters"/>.</value>
    public SaveTrackedFilePatternRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project revision build verify parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectRevisionBuildVerifyParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project revision build verify parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectRevisionBuildVerifyParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project revision build verify parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectRevisionBuildVerifyParameters"/>.</value>
    public RunProjectBuildVerificationRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project council build review record parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectCouncilBuildReviewRecordParameters
{
    /// <summary>
    /// Gets or sets the stable verification identifier used to identify or correlate this project council build review record parameters instance with related application state.
    /// </summary>
    /// <value>The verification identifier value exposed by <see cref="ProjectCouncilBuildReviewRecordParameters"/>.</value>
    public Guid VerificationId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project council build review record parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectCouncilBuildReviewRecordParameters"/>.</value>
    public RecordCouncilBuildReviewRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a project revision approve parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectRevisionApproveParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project revision approve parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectRevisionApproveParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable revision identifier used to identify or correlate this project revision approve parameters instance with related application state.
    /// </summary>
    /// <value>The revision identifier value exposed by <see cref="ProjectRevisionApproveParameters"/>.</value>
    public Guid RevisionId { get; set; }
    /// <summary>
    /// Gets or sets the request value that forms part of the project revision approve parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request value exposed by <see cref="ProjectRevisionApproveParameters"/>.</value>
    public ApproveRevisionReadyForTestRequest Request { get; set; } = new();
}

/// <summary>
/// Represents a public service method invocation parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicServiceMethodInvocationParameters
{
    /// <summary>
    /// Gets or sets the stable catalog key used to identify or correlate this public service method invocation parameters instance with related application state.
    /// </summary>
    /// <value>The catalog key value exposed by <see cref="PublicServiceMethodInvocationParameters"/>.</value>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters value that forms part of the public service method invocation parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="PublicServiceMethodInvocationParameters"/>.</value>
    public JsonElement Parameters { get; set; }
}

/// <summary>
/// Represents a regex pattern list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RegexPatternListParameters
{
    /// <summary>
    /// Gets or sets the take value that forms part of the regex pattern list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="RegexPatternListParameters"/>.</value>
    public int Take { get; set; } = 5000;
    /// <summary>
    /// Gets or sets the prefix value that forms part of the regex pattern list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prefix value exposed by <see cref="RegexPatternListParameters"/>.</value>
    public string Prefix { get; set; } = string.Empty;
}
