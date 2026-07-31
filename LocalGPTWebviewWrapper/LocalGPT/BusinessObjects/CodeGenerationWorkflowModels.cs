using System.Text.Json;

namespace LocalGPT.BusinessObjects;

public sealed class CodeGenerationReviewStatuses
{
    private CodeGenerationReviewStatuses() { }
    public const string AwaitingUserDecision = "AwaitingUserDecision";
    public const string Rejected = "Rejected";
    public const string Generating = "Generating";
    public const string Generated = "Generated";
    public const string BuildPassed = "BuildPassed";
    public const string BuildFailed = "BuildFailed";
    public const string Failed = "Failed";
}

public sealed class CodeGenerationOutputKinds
{
    private CodeGenerationOutputKinds() { }
    public const string SourceFiles = "SourceFiles";
    public const string ClassLibrary = "ClassLibrary";
    public const string ConsoleApplication = "ConsoleApplication";
    public const string Solution = "Solution";
    public const string LocalGptAddon = "LocalGptAddon";
    public const string CSharpScript = "CSharpScript";
    public const string JavaScriptModule = "JavaScriptModule";
}

public sealed class CodeGenerationChangeReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ProjectId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public Guid? ProjectTopicId { get; set; }
    public Guid? CouncilRunId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string CurrentProjectState { get; set; } = string.Empty;
    public string CouncilSummary { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
    public string SafetySummary { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string ReviewHash { get; set; } = string.Empty;
    public string Status { get; set; } = CodeGenerationReviewStatuses.AwaitingUserDecision;
    public string DecisionNote { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string ZipFileName { get; set; } = string.Empty;
    public string BuildStatus { get; set; } = string.Empty;
    public bool ApprovalConsumed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class CodeGenerationFileSpec
{
    public string RelativePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

public sealed class CodeDomTypeSpec
{
    public string RelativePath { get; set; } = "GeneratedFeature.cs";
    public string Namespace { get; set; } = "LocalGPT.Generated";
    public string TypeName { get; set; } = "GeneratedFeature";
    public string MethodName { get; set; } = "Describe";
    public string MethodResult { get; set; } = "Generated with LocalGPT after human review.";
    public string Summary { get; set; } = string.Empty;
}

public sealed class CodeGenerationOutputSpec
{
    public string Kind { get; set; } = CodeGenerationOutputKinds.SourceFiles;
    public string Name { get; set; } = "LocalGptGeneratedFeature";
    public string RelativeDirectory { get; set; } = ".";
    public string TargetFramework { get; set; } = "net10.0";
    public string RootNamespace { get; set; } = "LocalGPT.Generated";
    public string Description { get; set; } = string.Empty;
}

public sealed class CreateCodeGenerationReviewRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public Guid? ProjectTopicId { get; set; }
    public Guid? CouncilRunId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string CurrentProjectState { get; set; } = string.Empty;
    public string CouncilSummary { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
    public string SafetySummary { get; set; } = string.Empty;
    public List<CodeGenerationFileSpec> Files { get; set; } = [];
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

public sealed class ExecuteCodeGenerationReviewRequest
{
    public string ExpectedReviewHash { get; set; } = string.Empty;
    public bool UserConfirmed { get; set; }
    public bool BuildAfterGeneration { get; set; }
    public bool UserConfirmedBuild { get; set; }
    public string DecisionNote { get; set; } = string.Empty;
}

public sealed class RejectCodeGenerationReviewRequest
{
    public string ExpectedReviewHash { get; set; } = string.Empty;
    public bool UserConfirmed { get; set; }
    public string DecisionNote { get; set; } = string.Empty;
}

public sealed class CodeGenerationReviewPayload
{
    public List<CodeGenerationFileSpec> Files { get; set; } = [];
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

public sealed class CodeGenerationReviewSnapshot
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public Guid? ProjectTopicId { get; set; }
    public Guid? CouncilRunId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string CurrentProjectState { get; set; } = string.Empty;
    public string CouncilSummary { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
    public string SafetySummary { get; set; } = string.Empty;
    public string ReviewHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DecisionNote { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string ZipFileName { get; set; } = string.Empty;
    public string BuildStatus { get; set; } = string.Empty;
    public bool ApprovalConsumed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public List<CodeGenerationFileReview> Files { get; set; } = [];
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

public sealed class CodeGenerationFileReview
{
    public string RelativePath { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}

public sealed class CodeGenerationExecutionResult
{
    public Guid ReviewId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReviewHash { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public string ZipFileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string BuildStatus { get; set; } = string.Empty;
    public List<string> WrittenFiles { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class DxAiFunctionInvocationRequest
{
    public Guid? OperationId { get; set; }
    public JsonElement Parameters { get; set; }
    public bool UserConfirmed { get; set; }
    public bool AutomaticInvocation { get; set; }
    public string? ConfirmationSummaryHash { get; set; }
    public string RequestedBy { get; set; } = "CurrentUser";
    public Guid? ConversationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectVersionId { get; set; }
    public string ApplicationVersion { get; set; } = string.Empty;
}

public sealed class DxAiFunctionParameterBinding<T> where T : new()
{
    public bool Succeeded { get; init; }
    public T Value { get; init; } = new();
    public string Error { get; init; } = string.Empty;
}

public sealed class DxAiFunctionInvocationResult
{
    public string FunctionName { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string Status { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? Error { get; set; }
    public Guid OperationId { get; set; } = Guid.NewGuid();
}
