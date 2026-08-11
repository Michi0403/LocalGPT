using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a code generation review statuses.
/// </summary>
public sealed class CodeGenerationReviewStatuses
{
    /// <summary>
    /// Runs the code generation review statuses operation.
    /// </summary>
    private CodeGenerationReviewStatuses() { }
    /// <summary>
    /// Stores awaiting user decision.
    /// </summary>
    public const string AwaitingUserDecision = "AwaitingUserDecision";
    /// <summary>
    /// Stores rejected.
    /// </summary>
    public const string Rejected = "Rejected";
    /// <summary>
    /// Stores generating.
    /// </summary>
    public const string Generating = "Generating";
    /// <summary>
    /// Stores generated.
    /// </summary>
    public const string Generated = "Generated";
    /// <summary>
    /// Stores build passed.
    /// </summary>
    public const string BuildPassed = "BuildPassed";
    /// <summary>
    /// Stores build failed.
    /// </summary>
    public const string BuildFailed = "BuildFailed";
    /// <summary>
    /// Stores failed.
    /// </summary>
    public const string Failed = "Failed";
}

/// <summary>
/// Represents a code generation output kinds.
/// </summary>
public sealed class CodeGenerationOutputKinds
{
    /// <summary>
    /// Runs the code generation output kinds operation.
    /// </summary>
    private CodeGenerationOutputKinds() { }
    /// <summary>
    /// Stores source files.
    /// </summary>
    public const string SourceFiles = "SourceFiles";
    /// <summary>
    /// Stores class library.
    /// </summary>
    public const string ClassLibrary = "ClassLibrary";
    /// <summary>
    /// Stores console application.
    /// </summary>
    public const string ConsoleApplication = "ConsoleApplication";
    /// <summary>
    /// Stores solution.
    /// </summary>
    public const string Solution = "Solution";
    /// <summary>
    /// Stores local gpt addon.
    /// </summary>
    public const string LocalGptAddon = "LocalGptAddon";
    /// <summary>
    /// Stores csharp script.
    /// </summary>
    public const string CSharpScript = "CSharpScript";
    /// <summary>
    /// Stores java script module.
    /// </summary>
    public const string JavaScriptModule = "JavaScriptModule";
}

/// <summary>
/// Represents a code generation change review.
/// </summary>
public sealed class CodeGenerationChangeReview
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project revision identifier.
    /// </summary>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets project topic identifier.
    /// </summary>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets council run identifier.
    /// </summary>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets goal.
    /// </summary>
    public string Goal { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current project state.
    /// </summary>
    public string CurrentProjectState { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council summary.
    /// </summary>
    public string CouncilSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets change summary.
    /// </summary>
    public string ChangeSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets safety summary.
    /// </summary>
    public string SafetySummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets payload JSON.
    /// </summary>
    public string PayloadJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets review hash.
    /// </summary>
    public string ReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = CodeGenerationReviewStatuses.AwaitingUserDecision;
    /// <summary>
    /// Gets or sets decision note.
    /// </summary>
    public string DecisionNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets zip file name.
    /// </summary>
    public string ZipFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets build status.
    /// </summary>
    public string BuildStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets approval consumed.
    /// </summary>
    public bool ApprovalConsumed { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets decided at UTC.
    /// </summary>
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets completed at UTC.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Represents a code generation file spec.
/// </summary>
public sealed class CodeGenerationFileSpec
{
    /// <summary>
    /// Gets or sets relative path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code dom type spec.
/// </summary>
public sealed class CodeDomTypeSpec
{
    /// <summary>
    /// Gets or sets relative path.
    /// </summary>
    public string RelativePath { get; set; } = "GeneratedFeature.cs";
    /// <summary>
    /// Gets or sets namespace.
    /// </summary>
    public string Namespace { get; set; } = "LocalGPT.Generated";
    /// <summary>
    /// Gets or sets type name.
    /// </summary>
    public string TypeName { get; set; } = "GeneratedFeature";
    /// <summary>
    /// Gets or sets method name.
    /// </summary>
    public string MethodName { get; set; } = "Describe";
    /// <summary>
    /// Gets or sets method result.
    /// </summary>
    public string MethodResult { get; set; } = "Generated with LocalGPT after human review.";
    /// <summary>
    /// Gets or sets summary.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation output spec.
/// </summary>
public sealed class CodeGenerationOutputSpec
{
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = CodeGenerationOutputKinds.SourceFiles;
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = "LocalGptGeneratedFeature";
    /// <summary>
    /// Gets or sets relative directory.
    /// </summary>
    public string RelativeDirectory { get; set; } = ".";
    /// <summary>
    /// Gets or sets target framework.
    /// </summary>
    public string TargetFramework { get; set; } = "net10.0";
    /// <summary>
    /// Gets or sets root namespace.
    /// </summary>
    public string RootNamespace { get; set; } = "LocalGPT.Generated";
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a create code generation review request.
/// </summary>
public sealed class CreateCodeGenerationReviewRequest
{
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project revision identifier.
    /// </summary>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets project topic identifier.
    /// </summary>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets council run identifier.
    /// </summary>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets goal.
    /// </summary>
    public string Goal { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current project state.
    /// </summary>
    public string CurrentProjectState { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council summary.
    /// </summary>
    public string CouncilSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets change summary.
    /// </summary>
    public string ChangeSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets safety summary.
    /// </summary>
    public string SafetySummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets files.
    /// </summary>
    public List<CodeGenerationFileSpec> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets code dom types.
    /// </summary>
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    /// <summary>
    /// Gets or sets outputs.
    /// </summary>
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

/// <summary>
/// Represents an execute code generation review request.
/// </summary>
public sealed class ExecuteCodeGenerationReviewRequest
{
    /// <summary>
    /// Gets or sets expected review hash.
    /// </summary>
    public string ExpectedReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets build after generation.
    /// </summary>
    public bool BuildAfterGeneration { get; set; }
    /// <summary>
    /// Gets or sets user confirmed build.
    /// </summary>
    public bool UserConfirmedBuild { get; set; }
    /// <summary>
    /// Gets or sets decision note.
    /// </summary>
    public string DecisionNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents a reject code generation review request.
/// </summary>
public sealed class RejectCodeGenerationReviewRequest
{
    /// <summary>
    /// Gets or sets expected review hash.
    /// </summary>
    public string ExpectedReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets decision note.
    /// </summary>
    public string DecisionNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation review payload.
/// </summary>
public sealed class CodeGenerationReviewPayload
{
    /// <summary>
    /// Gets or sets files.
    /// </summary>
    public List<CodeGenerationFileSpec> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets code dom types.
    /// </summary>
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    /// <summary>
    /// Gets or sets outputs.
    /// </summary>
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

/// <summary>
/// Represents a code generation review snapshot.
/// </summary>
public sealed class CodeGenerationReviewSnapshot
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project revision identifier.
    /// </summary>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets project topic identifier.
    /// </summary>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets council run identifier.
    /// </summary>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets goal.
    /// </summary>
    public string Goal { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current project state.
    /// </summary>
    public string CurrentProjectState { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council summary.
    /// </summary>
    public string CouncilSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets change summary.
    /// </summary>
    public string ChangeSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets safety summary.
    /// </summary>
    public string SafetySummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets review hash.
    /// </summary>
    public string ReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets decision note.
    /// </summary>
    public string DecisionNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets zip file name.
    /// </summary>
    public string ZipFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets build status.
    /// </summary>
    public string BuildStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets approval consumed.
    /// </summary>
    public bool ApprovalConsumed { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets decided at UTC.
    /// </summary>
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets completed at UTC.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets files.
    /// </summary>
    public List<CodeGenerationFileReview> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets code dom types.
    /// </summary>
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    /// <summary>
    /// Gets or sets outputs.
    /// </summary>
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

/// <summary>
/// Represents a code generation file review.
/// </summary>
public sealed class CodeGenerationFileReview
{
    /// <summary>
    /// Gets or sets relative path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets character count.
    /// </summary>
    public int CharacterCount { get; set; }
    /// <summary>
    /// Gets or sets content hash.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation execution result.
/// </summary>
public sealed class CodeGenerationExecutionResult
{
    /// <summary>
    /// Gets or sets review identifier.
    /// </summary>
    public Guid ReviewId { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets review hash.
    /// </summary>
    public string ReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets workspace name.
    /// </summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets workspace path.
    /// </summary>
    public string WorkspacePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets zip file name.
    /// </summary>
    public string ZipFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets download URL.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets build status.
    /// </summary>
    public string BuildStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets written files.
    /// </summary>
    public List<string> WrittenFiles { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Represents a DevExpress ai function invocation request.
/// </summary>
public sealed class DxAiFunctionInvocationRequest
{
    /// <summary>
    /// Gets or sets operation identifier.
    /// </summary>
    public Guid? OperationId { get; set; }
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public JsonElement Parameters { get; set; }
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets automatic invocation.
    /// </summary>
    public bool AutomaticInvocation { get; set; }
    /// <summary>
    /// Gets or sets confirmation summary hash.
    /// </summary>
    public string? ConfirmationSummaryHash { get; set; }
    /// <summary>
    /// Gets or sets requested by.
    /// </summary>
    public string RequestedBy { get; set; } = "CurrentUser";
    /// <summary>
    /// Gets or sets conversation identifier.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets project identifier.
    /// </summary>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets project version identifier.
    /// </summary>
    public Guid? ProjectVersionId { get; set; }
    /// <summary>
    /// Gets or sets application version.
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;
}

/// <summary>
/// Represents a DevExpress ai function parameter binding.
/// </summary>
public sealed class DxAiFunctionParameterBinding<T> where T : new()
{
    /// <summary>
    /// Gets or sets succeeded.
    /// </summary>
    public bool Succeeded { get; init; }
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public T Value { get; init; } = new();
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// Represents a DevExpress ai function invocation result.
/// </summary>
public sealed class DxAiFunctionInvocationResult
{
    /// <summary>
    /// Gets or sets function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public object? Value { get; set; }
    /// <summary>
    /// Gets or sets error.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets operation identifier.
    /// </summary>
    public Guid OperationId { get; set; } = Guid.NewGuid();
}
