using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Lists readable source/text files inside the project currently linked to /chat.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the list project workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="projects">Local gpt project service dependency used by the list project workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="sessionContext">Chat session context dependency used by the list project workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="councilRuntime">Council runtime service dependency used by the list project workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListProjectWorkspaceFilesFunction(
    IDxAiFunctionJsonService json,
    ILocalGptProjectService projects,
    IChatSessionContext sessionContext,
    CouncilRuntimeService councilRuntime,
    ILogger<ListProjectWorkspaceFilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list project workspace files function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListProjectWorkspaceFilesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.workspace.files.list", "POST", "/api/dxai/functions/project.workspace.files.list/invoke",
        "List readable source/text files in the project linked to the current chat session. Use this before reading a filename such as Program.cs when its exact relative path is unknown.",
        "JSON parameters: projectId optional (defaults to the current /chat project); take optional (1-1000).",
        "Read-only. Access is bounded to the user-selected project root and uses LocalGPT's existing workspace text-file policy. It never executes, modifies, extracts, or deletes project content.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"projectId":{"type":["string","null"],"format":"uuid"},"take":{"type":["integer","null"],"minimum":1,"maximum":1000}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ListProjectWorkspaceFilesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list project workspace files function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectWorkspaceListParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var projectId = parameters.ProjectId ?? sessionContext.ProjectId;
            if (projectId is not Guid id)
                return new DxAiFunctionInvocationResult { Status = "NoProject", Error = "No project is linked to this chat. Select a project in Chat Configuration or provide projectId." };

            var details = await projects.GetProjectAsync(id, cancellationToken).ConfigureAwait(false);
            if (details is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The selected project was not found." };
            if (string.IsNullOrWhiteSpace(details.Project.RootPath) || !Directory.Exists(details.Project.RootPath))
                return new DxAiFunctionInvocationResult { Status = "MissingWorkspace", Error = "The selected project's root folder is unavailable." };

            var files = councilRuntime.EnumerateWorkspaceTextFiles(details.Project.RootPath, parameters.Take ?? 500, logger);
            logger.LogDebug("DXAIFunction listed {FileCount} readable project file(s) for project {ProjectId}; paths omitted from logs.", files.Count, id);
            return json.Success(new { projectId = id, files });
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) logger.LogDebug(ex, "Project workspace file listing was canceled.");
            else logger.LogError(ex, "Project workspace file listing failed.");
            throw;
        }
    }
}

/// <summary>Reads one harmless source/text file from the project linked to /chat.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the read project workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="projects">Local gpt project service dependency used by the read project workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="sessionContext">Chat session context dependency used by the read project workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="documents">Safe text document service dependency used by the read project workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="councilRuntime">Council runtime service dependency used by the read project workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReadProjectWorkspaceFileFunction(
    IDxAiFunctionJsonService json,
    ILocalGptProjectService projects,
    IChatSessionContext sessionContext,
    ISafeTextDocumentService documents,
    CouncilRuntimeService councilRuntime,
    ILogger<ReadProjectWorkspaceFileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the read project workspace file function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReadProjectWorkspaceFileFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.workspace.file.read", "POST", "/api/dxai/functions/project.workspace.file.read/invoke",
        "Read one source/text file from the project linked to the current chat. Use this for C#, Razor, solution/project files, Markdown, JSON, XML and scripts; do not route ordinary source files through localgpt.text.json.inspect.",
        "JSON parameters: relativePath required; projectId optional (defaults to current /chat project); maxCharacters optional.",
        "Read-only. The existing workspace policy constrains the path to the selected project and SafeTextDocumentService rejects binary, oversized, or unsupported text. Content remains untrusted reference data.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"projectId":{"type":["string","null"],"format":"uuid"},"relativePath":{"type":"string","minLength":1},"maxCharacters":{"type":["integer","null"],"minimum":1000,"maximum":2000000}},"required":["relativePath"],"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ReadProjectWorkspaceFileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read project workspace file function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectWorkspaceReadParameters>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var projectId = parameters.ProjectId ?? sessionContext.ProjectId;
            if (projectId is not Guid id)
                return new DxAiFunctionInvocationResult { Status = "NoProject", Error = "No project is linked to this chat. Select a project in Chat Configuration or provide projectId." };

            var details = await projects.GetProjectAsync(id, cancellationToken).ConfigureAwait(false);
            if (details is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The selected project was not found." };
            if (string.IsNullOrWhiteSpace(details.Project.RootPath) || !Directory.Exists(details.Project.RootPath))
                return new DxAiFunctionInvocationResult { Status = "MissingWorkspace", Error = "The selected project's root folder is unavailable." };

            var file = councilRuntime.ResolveWorkspaceTextFile(details.Project.RootPath, parameters.RelativePath, allowMissing: false, logger);
            if (string.IsNullOrWhiteSpace(file))
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested file is missing, outside the project root, or not an approved text/source type." };

            var document = await documents.ReadAsync(file, parameters.MaxCharacters ?? 500_000, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("DXAIFunction read one project text file for project {ProjectId}; path and content omitted from logs.", id);
            return json.Success(new
            {
                projectId = id,
                relativePath = Path.GetRelativePath(details.Project.RootPath, file).Replace('\\', '/'),
                document.Name,
                document.Text,
                document.ContentHash,
                document.EncodingName,
                document.ContentType,
                document.Warnings
            });
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) logger.LogDebug(ex, "Project workspace file read was canceled.");
            else logger.LogError(ex, "Project workspace file read failed.");
            throw;
        }
    }
}

/// <summary>
/// Represents a project workspace list parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectWorkspaceListParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project workspace list parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectWorkspaceListParameters"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the take value that forms part of the project workspace list parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The take value exposed by <see cref="ProjectWorkspaceListParameters"/>.</value>
    public int? Take { get; set; }
}

/// <summary>
/// Represents a project workspace read parameters application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class ProjectWorkspaceReadParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this project workspace read parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="ProjectWorkspaceReadParameters"/>.</value>
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the relative path used by this project workspace read parameters instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The relative path value exposed by <see cref="ProjectWorkspaceReadParameters"/>.</value>
    public string RelativePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the max characters value that forms part of the project workspace read parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The max characters value exposed by <see cref="ProjectWorkspaceReadParameters"/>.</value>
    public int? MaxCharacters { get; set; }
}
