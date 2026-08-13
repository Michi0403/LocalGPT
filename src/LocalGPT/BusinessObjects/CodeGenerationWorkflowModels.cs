using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a code generation review statuses application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewStatuses
{
    /// <summary>
    /// Initializes a new <see cref="CodeGenerationReviewStatuses"/> instance and captures the dependencies or initial state required by its code generation review statuses workflow.
    /// </summary>
    private CodeGenerationReviewStatuses() { }
    /// <summary>
    /// Defines the awaiting user decision constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string AwaitingUserDecision = "AwaitingUserDecision";
    /// <summary>
    /// Defines the rejected constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Rejected = "Rejected";
    /// <summary>
    /// Defines the generating constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Generating = "Generating";
    /// <summary>
    /// Defines the generated constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Generated = "Generated";
    /// <summary>
    /// Defines the build passed constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string BuildPassed = "BuildPassed";
    /// <summary>
    /// Defines the build failed constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string BuildFailed = "BuildFailed";
    /// <summary>
    /// Defines the failed constant used by <see cref="CodeGenerationReviewStatuses"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Failed = "Failed";
}

/// <summary>
/// Represents a code generation output kinds application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationOutputKinds
{
    /// <summary>
    /// Initializes a new <see cref="CodeGenerationOutputKinds"/> instance and captures the dependencies or initial state required by its code generation output kinds workflow.
    /// </summary>
    private CodeGenerationOutputKinds() { }
    /// <summary>
    /// Defines the source files constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string SourceFiles = "SourceFiles";
    /// <summary>
    /// Defines the class library constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string ClassLibrary = "ClassLibrary";
    /// <summary>
    /// Defines the console application constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string ConsoleApplication = "ConsoleApplication";
    /// <summary>
    /// Defines the solution constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Solution = "Solution";
    /// <summary>
    /// Defines the LocalGPT addon constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string LocalGptAddon = "LocalGptAddon";
    /// <summary>
    /// Defines the c sharp script constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string CSharpScript = "CSharpScript";
    /// <summary>
    /// Stores PowerShell script output. Explicit reviewed .ps1 files are copied verbatim; when none are supplied LocalGPT creates a non-executed starter script.
    /// </summary>
    public const string PowerShellScript = "PowerShellScript";
    /// <summary>
    /// Defines the java script module constant used by <see cref="CodeGenerationOutputKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string JavaScriptModule = "JavaScriptModule";
}

/// <summary>
/// Represents a code generation change review application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationChangeReview
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this code generation change review instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this code generation change review instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project revision identifier used to identify or correlate this code generation change review instance with related application state.
    /// </summary>
    /// <value>The project revision identifier value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable project topic identifier used to identify or correlate this code generation change review instance with related application state.
    /// </summary>
    /// <value>The project topic identifier value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this code generation change review instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the title value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the goal value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The goal value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string Goal { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current project state.
    /// </summary>
    /// <value>The current project state value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string CurrentProjectState { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council summary value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council summary value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string CouncilSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the change summary value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The change summary value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string ChangeSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the safety summary value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety summary value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string SafetySummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the payload JSON value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The payload JSON value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string PayloadJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the review hash value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review hash value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string ReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string Status { get; set; } = CodeGenerationReviewStatuses.AwaitingUserDecision;
    /// <summary>
    /// Gets or sets the decision note value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision note value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string DecisionNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ZIP file name used by this code generation change review instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The ZIP file name value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string ZipFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the build status value that forms part of the code generation change review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The build status value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public string BuildStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approval consumed applies to the code generation change review state.
    /// </summary>
    /// <value>The approval consumed value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public bool ApprovalConsumed { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this code generation change review state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this code generation change review state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the decided at UTC associated with this code generation change review state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The decided at UTC value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the completed at UTC associated with this code generation change review state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="CodeGenerationChangeReview"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Represents a code generation file spec application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationFileSpec
{
    /// <summary>
    /// Gets or sets the relative path used by this code generation file spec instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="CodeGenerationFileSpec"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content value that forms part of the code generation file spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="CodeGenerationFileSpec"/>.</value>
    public string Content { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the code generation file spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="CodeGenerationFileSpec"/>.</value>
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code DOM type spec application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeDomTypeSpec
{
    /// <summary>
    /// Gets or sets the relative path used by this code DOM type spec instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="CodeDomTypeSpec"/>.</value>
    public string RelativePath { get; set; } = "GeneratedFeature.cs";
    /// <summary>
    /// Gets or sets the namespace value that forms part of the code DOM type spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The namespace value exposed by <see cref="CodeDomTypeSpec"/>.</value>
    public string Namespace { get; set; } = "LocalGPT.Generated";
    /// <summary>
    /// Gets or sets the type name value that forms part of the code DOM type spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The type name value exposed by <see cref="CodeDomTypeSpec"/>.</value>
    public string TypeName { get; set; } = "GeneratedFeature";
    /// <summary>
    /// Gets or sets the method name value that forms part of the code DOM type spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method name value exposed by <see cref="CodeDomTypeSpec"/>.</value>
    public string MethodName { get; set; } = "Describe";
    /// <summary>
    /// Gets or sets the method result value that forms part of the code DOM type spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method result value exposed by <see cref="CodeDomTypeSpec"/>.</value>
    public string MethodResult { get; set; } = "Generated with LocalGPT after human review.";
    /// <summary>
    /// Gets or sets the summary value that forms part of the code DOM type spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary value exposed by <see cref="CodeDomTypeSpec"/>.</value>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation output spec application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationOutputSpec
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the code generation output spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="CodeGenerationOutputSpec"/>.</value>
    public string Kind { get; set; } = CodeGenerationOutputKinds.SourceFiles;
    /// <summary>
    /// Gets or sets the name value that forms part of the code generation output spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="CodeGenerationOutputSpec"/>.</value>
    public string Name { get; set; } = "LocalGptGeneratedFeature";
    /// <summary>
    /// Gets or sets the relative directory used by this code generation output spec instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative directory value exposed by <see cref="CodeGenerationOutputSpec"/>.</value>
    public string RelativeDirectory { get; set; } = ".";
    /// <summary>
    /// Gets or sets the target framework value that forms part of the code generation output spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target framework value exposed by <see cref="CodeGenerationOutputSpec"/>.</value>
    public string TargetFramework { get; set; } = "net10.0";
    /// <summary>
    /// Gets or sets the root namespace value that forms part of the code generation output spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The root namespace value exposed by <see cref="CodeGenerationOutputSpec"/>.</value>
    public string RootNamespace { get; set; } = "LocalGPT.Generated";
    /// <summary>
    /// Gets or sets the description value that forms part of the code generation output spec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="CodeGenerationOutputSpec"/>.</value>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for create code generation review, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class CreateCodeGenerationReviewRequest
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this create code generation review instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project revision identifier used to identify or correlate this create code generation review instance with related application state.
    /// </summary>
    /// <value>The project revision identifier value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable project topic identifier used to identify or correlate this create code generation review instance with related application state.
    /// </summary>
    /// <value>The project topic identifier value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this create code generation review instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the title value that forms part of the create code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the goal value that forms part of the create code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The goal value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public string Goal { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current project state.
    /// </summary>
    /// <value>The current project state value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public string CurrentProjectState { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council summary value that forms part of the create code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council summary value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public string CouncilSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the change summary value that forms part of the create code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The change summary value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public string ChangeSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the safety summary value that forms part of the create code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety summary value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public string SafetySummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this create code generation review instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public List<CodeGenerationFileSpec> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the code DOM types collection maintained or exposed by this create code generation review instance for downstream processing.
    /// </summary>
    /// <value>The code DOM types value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    /// <summary>
    /// Gets or sets the outputs collection maintained or exposed by this create code generation review instance for downstream processing.
    /// </summary>
    /// <value>The outputs value exposed by <see cref="CreateCodeGenerationReviewRequest"/>.</value>
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

/// <summary>
/// Represents the input contract for execute code generation review, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class ExecuteCodeGenerationReviewRequest
{
    /// <summary>
    /// Gets or sets the expected review hash value that forms part of the execute code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected review hash value exposed by <see cref="ExecuteCodeGenerationReviewRequest"/>.</value>
    public string ExpectedReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the execute code generation review state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="ExecuteCodeGenerationReviewRequest"/>.</value>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether build after generation applies to the execute code generation review state.
    /// </summary>
    /// <value>The build after generation value exposed by <see cref="ExecuteCodeGenerationReviewRequest"/>.</value>
    public bool BuildAfterGeneration { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed build applies to the execute code generation review state.
    /// </summary>
    /// <value>The user confirmed build value exposed by <see cref="ExecuteCodeGenerationReviewRequest"/>.</value>
    public bool UserConfirmedBuild { get; set; }
    /// <summary>
    /// Gets or sets the decision note value that forms part of the execute code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision note value exposed by <see cref="ExecuteCodeGenerationReviewRequest"/>.</value>
    public string DecisionNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for reject code generation review, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class RejectCodeGenerationReviewRequest
{
    /// <summary>
    /// Gets or sets the expected review hash value that forms part of the reject code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The expected review hash value exposed by <see cref="RejectCodeGenerationReviewRequest"/>.</value>
    public string ExpectedReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the reject code generation review state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="RejectCodeGenerationReviewRequest"/>.</value>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets the decision note value that forms part of the reject code generation review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision note value exposed by <see cref="RejectCodeGenerationReviewRequest"/>.</value>
    public string DecisionNote { get; set; } = string.Empty;
}

/// <summary>
/// Represents a code generation review payload application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewPayload
{
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this code generation review payload instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="CodeGenerationReviewPayload"/>.</value>
    public List<CodeGenerationFileSpec> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the code DOM types collection maintained or exposed by this code generation review payload instance for downstream processing.
    /// </summary>
    /// <value>The code DOM types value exposed by <see cref="CodeGenerationReviewPayload"/>.</value>
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    /// <summary>
    /// Gets or sets the outputs collection maintained or exposed by this code generation review payload instance for downstream processing.
    /// </summary>
    /// <value>The outputs value exposed by <see cref="CodeGenerationReviewPayload"/>.</value>
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

/// <summary>
/// Represents a code generation review snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationReviewSnapshot
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this code generation review snapshot instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this code generation review snapshot instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project revision identifier used to identify or correlate this code generation review snapshot instance with related application state.
    /// </summary>
    /// <value>The project revision identifier value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public Guid? ProjectRevisionId { get; set; }
    /// <summary>
    /// Gets or sets the stable project topic identifier used to identify or correlate this code generation review snapshot instance with related application state.
    /// </summary>
    /// <value>The project topic identifier value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public Guid? ProjectTopicId { get; set; }
    /// <summary>
    /// Gets or sets the stable council run identifier used to identify or correlate this code generation review snapshot instance with related application state.
    /// </summary>
    /// <value>The council run identifier value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public Guid? CouncilRunId { get; set; }
    /// <summary>
    /// Gets or sets the title value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the goal value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The goal value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string Goal { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current project state.
    /// </summary>
    /// <value>The current project state value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string CurrentProjectState { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the council summary value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council summary value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string CouncilSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the change summary value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The change summary value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string ChangeSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the safety summary value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety summary value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string SafetySummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the review hash value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review hash value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string ReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the status value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the decision note value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The decision note value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string DecisionNote { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ZIP file name used by this code generation review snapshot instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The ZIP file name value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string ZipFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the build status value that forms part of the code generation review snapshot state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The build status value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public string BuildStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether approval consumed applies to the code generation review snapshot state.
    /// </summary>
    /// <value>The approval consumed value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public bool ApprovalConsumed { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this code generation review snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the updated at UTC associated with this code generation review snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public DateTime UpdatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the decided at UTC associated with this code generation review snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The decided at UTC value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the completed at UTC associated with this code generation review snapshot state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The completed at UTC value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the files collection maintained or exposed by this code generation review snapshot instance for downstream processing.
    /// </summary>
    /// <value>The files value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public List<CodeGenerationFileReview> Files { get; set; } = [];
    /// <summary>
    /// Gets or sets the code DOM types collection maintained or exposed by this code generation review snapshot instance for downstream processing.
    /// </summary>
    /// <value>The code DOM types value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public List<CodeDomTypeSpec> CodeDomTypes { get; set; } = [];
    /// <summary>
    /// Gets or sets the outputs collection maintained or exposed by this code generation review snapshot instance for downstream processing.
    /// </summary>
    /// <value>The outputs value exposed by <see cref="CodeGenerationReviewSnapshot"/>.</value>
    public List<CodeGenerationOutputSpec> Outputs { get; set; } = [];
}

/// <summary>
/// Represents a code generation file review application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CodeGenerationFileReview
{
    /// <summary>
    /// Gets or sets the relative path used by this code generation file review instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="CodeGenerationFileReview"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the code generation file review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="CodeGenerationFileReview"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the character count that quantifies the associated code generation file review data.
    /// </summary>
    /// <value>The character count value exposed by <see cref="CodeGenerationFileReview"/>.</value>
    public int CharacterCount { get; set; }
    /// <summary>
    /// Gets or sets the content hash value that forms part of the code generation file review state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content hash value exposed by <see cref="CodeGenerationFileReview"/>.</value>
    public string ContentHash { get; set; } = string.Empty;
}

/// <summary>
/// Represents the outcome of code generation execution, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class CodeGenerationExecutionResult
{
    /// <summary>
    /// Gets or sets the stable review identifier used to identify or correlate this code generation execution instance with related application state.
    /// </summary>
    /// <value>The review identifier value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public Guid ReviewId { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the code generation execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the review hash value that forms part of the code generation execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review hash value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string ReviewHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace name value that forms part of the code generation execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The workspace name value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the workspace path used by this code generation execution instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The workspace path value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string WorkspacePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ZIP file name used by this code generation execution instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The ZIP file name value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string ZipFileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the download URL that identifies the network or application endpoint associated with this code generation execution state.
    /// </summary>
    /// <value>The download URL value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string DownloadUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the build status value that forms part of the code generation execution state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The build status value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public string BuildStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the written files collection maintained or exposed by this code generation execution instance for downstream processing.
    /// </summary>
    /// <value>The written files value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public List<string> WrittenFiles { get; set; } = [];
    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this code generation execution instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="CodeGenerationExecutionResult"/>.</value>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Represents the input contract for DevExpress AI function invocation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class DxAiFunctionInvocationRequest
{
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this DevExpress AI function invocation instance with related application state.
    /// </summary>
    /// <value>The operation identifier value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public Guid? OperationId { get; set; }
    /// <summary>
    /// Gets or sets the parameters value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public JsonElement Parameters { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the DevExpress AI function invocation state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public bool UserConfirmed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether automatic invocation applies to the DevExpress AI function invocation state.
    /// </summary>
    /// <value>The automatic invocation value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public bool AutomaticInvocation { get; set; }
    /// <summary>
    /// Gets or sets the confirmation summary hash value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confirmation summary hash value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public string? ConfirmationSummaryHash { get; set; }
    /// <summary>
    /// Gets or sets the requested by value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested by value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public string RequestedBy { get; set; } = "CurrentUser";
    /// <summary>
    /// Gets or sets the stable conversation identifier used to identify or correlate this DevExpress AI function invocation instance with related application state.
    /// </summary>
    /// <value>The conversation identifier value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this DevExpress AI function invocation instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable project version identifier used to identify or correlate this DevExpress AI function invocation instance with related application state.
    /// </summary>
    /// <value>The project version identifier value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public Guid? ProjectVersionId { get; set; }
    /// <summary>
    /// Gets or sets the application version value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The application version value exposed by <see cref="DxAiFunctionInvocationRequest"/>.</value>
    public string ApplicationVersion { get; set; } = string.Empty;
}

/// <summary>
/// Represents a DevExpress AI function parameter binding application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <typeparam name="T">Type used for t values handled by <see cref="DxAiFunctionParameterBinding{T}"/>.</typeparam>
public sealed class DxAiFunctionParameterBinding<T> where T : new()
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the DevExpress AI function parameter binding state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="DxAiFunctionParameterBinding{T}"/>.</value>
    public bool Succeeded { get; init; }
    /// <summary>
    /// Gets or sets the value value that forms part of the DevExpress AI function parameter binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="DxAiFunctionParameterBinding{T}"/>.</value>
    public T Value { get; init; } = new();
    /// <summary>
    /// Gets or sets the error value that forms part of the DevExpress AI function parameter binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="DxAiFunctionParameterBinding{T}"/>.</value>
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// Represents the outcome of DevExpress AI function invocation, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class DxAiFunctionInvocationResult
{
    /// <summary>
    /// Gets or sets the function name value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DxAiFunctionInvocationResult"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the DevExpress AI function invocation state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="DxAiFunctionInvocationResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the status value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="DxAiFunctionInvocationResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="DxAiFunctionInvocationResult"/>.</value>
    public object? Value { get; set; }
    /// <summary>
    /// Gets or sets the error value that forms part of the DevExpress AI function invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="DxAiFunctionInvocationResult"/>.</value>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets the stable operation identifier used to identify or correlate this DevExpress AI function invocation instance with related application state.
    /// </summary>
    /// <value>The operation identifier value exposed by <see cref="DxAiFunctionInvocationResult"/>.</value>
    public Guid OperationId { get; set; } = Guid.NewGuid();
}
