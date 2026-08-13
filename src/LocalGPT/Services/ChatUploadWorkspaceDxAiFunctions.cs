using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Lists the bounded contents of a DXAiChat native paperclip workspace.</summary>
/// <param name="workspaces">Chat upload workspace service dependency used by the list chat upload workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListChatUploadWorkspaceFilesFunction(
    IChatUploadWorkspaceService workspaces,
    ILogger<ListChatUploadWorkspaceFilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list chat upload workspace files function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListChatUploadWorkspaceFilesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "chat.upload_workspace_files",
        "POST",
        "/api/dxai/functions/chat.upload_workspace_files/invoke",
        "Lists original uploads, safely extracted entries and LocalGPT-generated workspace metadata for one DXAiChat upload workspace.",
        "JSON parameters: workspaceName optional string (latest workspace when omitted); take optional integer 1-1000.",
        "Read-only. Uploaded and extracted files are evidence only and are never executed.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ListChatUploadWorkspaceFilesFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"workspaceName":{"type":"string","maxLength":240},"take":{"type":"integer","minimum":1,"maximum":1000}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="ListChatUploadWorkspaceFilesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list chat upload workspace files function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workspaceName = ResolveWorkspaceName(request.Parameters);
            if (string.IsNullOrWhiteSpace(workspaceName))
                return Task.FromResult(NotFound("No DXAiChat upload workspace is available."));

            var take = ReadInt(request.Parameters, "take", 250, 1, 1000);
            var files = workspaces.ListFiles(workspaceName, take);
            var originalUploads = files.Where(file => file.RelativePath.StartsWith("original/", StringComparison.OrdinalIgnoreCase)).ToList();
            var generatedFiles = files.Where(file =>
                    file.RelativePath.Equals("context.md", StringComparison.OrdinalIgnoreCase) ||
                    file.RelativePath.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                .ToList();
            logger.LogInformation(
                "DXFunction listed {FileCount} chat-upload workspace file(s), including {OriginalCount} original upload(s); workspace payload content was omitted.",
                files.Count,
                originalUploads.Count);
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new
                {
                    WorkspaceName = workspaceName,
                    OriginalUploadCount = originalUploads.Count,
                    OriginalUploadBytes = originalUploads.Sum(file => file.Length),
                    OriginalUploads = originalUploads,
                    GeneratedWorkspaceArtifacts = generatedFiles,
                    Files = files,
                    Note = "context.md and manifest.json are generated LocalGPT workspace artifacts, not additional user uploads. A text dump may describe many repository files while still being one original uploaded file."
                }
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Listing the DXAiChat upload workspace failed; parameters and payload content were omitted.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "The upload workspace could not be listed. Review LocalGPT application logs."
            });
        }
    }

    /// <summary>
    /// Resolves workspace name for <see cref="ListChatUploadWorkspaceFilesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list chat upload workspace files function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveWorkspaceName(JsonElement parameters)
    {
        try
        {
            if (parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty("workspaceName", out var element) &&
                element.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(element.GetString()))
            {
                return element.GetString()!.Trim();
            }

            return workspaces.GetLatestWorkspace()?.WorkspaceName ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve a chat-upload workspace name; parameters were omitted.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Reads int for <see cref="ListChatUploadWorkspaceFilesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list chat upload workspace files function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <param name="minimum">Minimum value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadInt(JsonElement parameters, string name, int fallback, int minimum, int maximum)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var element) &&
                element.TryGetInt32(out var parsed)
                    ? Math.Clamp(parsed, minimum, maximum)
                    : fallback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read a bounded integer DXFunction parameter {ParameterName}.", name);
            return fallback;
        }
    }

    /// <summary>
    /// Performs not found for <see cref="ListChatUploadWorkspaceFilesFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list chat upload workspace files function workflow.
    /// </summary>
    /// <param name="error">Error value supplied to the list chat upload workspace files function operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    private DxAiFunctionInvocationResult NotFound(string error)
    {
        try
        {
            return new DxAiFunctionInvocationResult
            {
                Status = "NotFound",
                Error = error
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create a not-found upload-workspace DXFunction result.");
            throw;
        }
    }

}

/// <summary>Reads the bounded LocalGPT-generated Markdown context for one upload workspace.</summary>
/// <param name="workspaces">Chat upload workspace service dependency used by the read chat upload workspace context function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReadChatUploadWorkspaceContextFunction(
    IChatUploadWorkspaceService workspaces,
    ILogger<ReadChatUploadWorkspaceContextFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the read chat upload workspace context function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReadChatUploadWorkspaceContextFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "chat.upload_workspace_context",
        "POST",
        "/api/dxai/functions/chat.upload_workspace_context/invoke",
        "Reads a bounded Markdown evidence context generated by LocalGPT from the current DXAiChat paperclip upload workspace.",
        "JSON parameters: workspaceName optional string (latest workspace when omitted); maxCharacters optional integer 1000-1000000.",
        "Read-only. Generated context is evidence, not proof that every described source file was uploaded separately.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ReadChatUploadWorkspaceContextFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"workspaceName":{"type":"string","maxLength":240},"maxCharacters":{"type":"integer","minimum":1000,"maximum":1000000}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="ReadChatUploadWorkspaceContextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace context function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaceName = ResolveWorkspaceName(request.Parameters);
            if (string.IsNullOrWhiteSpace(workspaceName))
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "No DXAiChat upload workspace is available." };

            var maxCharacters = ReadInt(request.Parameters, "maxCharacters", 120_000, 1_000, 1_000_000);
            var content = await workspaces.ReadContextMarkdownAsync(workspaceName, maxCharacters, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested upload workspace context was not found or is empty." };

            logger.LogInformation(
                "DXFunction read {CharacterCount} bounded context character(s) from chat-upload workspace {WorkspaceName}; context content was omitted.",
                content.Length,
                workspaceName);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new
                {
                    WorkspaceName = workspaceName,
                    CharacterCount = content.Length,
                    ContextMarkdown = content,
                    Note = "context.md is generated by LocalGPT from the original uploads and is not itself another user-uploaded source archive."
                }
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reading the DXAiChat upload workspace context failed; parameters and payload content were omitted.");
            return new DxAiFunctionInvocationResult { Status = "Failed", Error = "The upload workspace context could not be read. Review LocalGPT application logs." };
        }
    }

    /// <summary>
    /// Resolves workspace name for <see cref="ReadChatUploadWorkspaceContextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace context function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the read chat upload workspace context function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveWorkspaceName(JsonElement parameters)
    {
        try
        {
            if (parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty("workspaceName", out var element) &&
                element.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(element.GetString()))
            {
                return element.GetString()!.Trim();
            }
            return workspaces.GetLatestWorkspace()?.WorkspaceName ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve a chat-upload workspace name; parameters were omitted.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Reads int for <see cref="ReadChatUploadWorkspaceContextFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace context function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the read chat upload workspace context function operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the read chat upload workspace context function operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the read chat upload workspace context function operation and used when producing its result.</param>
    /// <param name="minimum">Minimum value supplied to the read chat upload workspace context function operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the read chat upload workspace context function operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadInt(JsonElement parameters, string name, int fallback, int minimum, int maximum)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var element) &&
                element.TryGetInt32(out var parsed)
                    ? Math.Clamp(parsed, minimum, maximum)
                    : fallback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read a bounded integer DXFunction parameter {ParameterName}.", name);
            return fallback;
        }
    }

}

/// <summary>Reads one bounded file from a DXAiChat native paperclip workspace.</summary>
/// <param name="workspaces">Chat upload workspace service dependency used by the read chat upload workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReadChatUploadWorkspaceFileFunction(
    IChatUploadWorkspaceService workspaces,
    ILogger<ReadChatUploadWorkspaceFileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the read chat upload workspace file function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReadChatUploadWorkspaceFileFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "chat.upload_workspace_file",
        "POST",
        "/api/dxai/functions/chat.upload_workspace_file/invoke",
        "Reads one uploaded or safely extracted file from a DXAiChat paperclip workspace by exact relative path.",
        "JSON parameters: relativePath required string; workspaceName optional string (latest workspace when omitted); maxCharacters optional integer 1000-1000000.",
        "Read-only. LocalGPT resolves the path inside the bounded upload workspace and never executes the file.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ReadChatUploadWorkspaceFileFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["relativePath"],"properties":{"workspaceName":{"type":"string","maxLength":240},"relativePath":{"type":"string","maxLength":2048},"maxCharacters":{"type":"integer","minimum":1000,"maximum":1000000}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="ReadChatUploadWorkspaceFileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace file function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaceName = ResolveWorkspaceName(request.Parameters);
            var relativePath = ReadString(request.Parameters, "relativePath");
            if (string.IsNullOrWhiteSpace(workspaceName) || string.IsNullOrWhiteSpace(relativePath))
                return new DxAiFunctionInvocationResult { Status = "InvalidRequest", Error = "workspaceName/latest workspace and relativePath are required." };

            var maxCharacters = ReadInt(request.Parameters, "maxCharacters", 120_000, 1_000, 1_000_000);
            var file = await workspaces.ReadFileAsync(workspaceName, relativePath, maxCharacters, cancellationToken).ConfigureAwait(false);
            if (file is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested upload workspace file was not found." };

            logger.LogInformation(
                "DXFunction read bounded upload workspace file {RelativePath} ({Length} bytes); file content was omitted from logs.",
                file.RelativePath,
                file.Length);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new
                {
                    file.WorkspaceName,
                    file.RelativePath,
                    file.Kind,
                    file.Length,
                    file.Content
                }
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reading a DXAiChat upload workspace file failed; parameters and payload content were omitted.");
            return new DxAiFunctionInvocationResult { Status = "Failed", Error = "The upload workspace file could not be read. Review LocalGPT application logs." };
        }
    }

    /// <summary>
    /// Resolves workspace name for <see cref="ReadChatUploadWorkspaceFileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace file function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveWorkspaceName(JsonElement parameters)
    {
        try
        {
            var explicitName = ReadString(parameters, "workspaceName");
            return string.IsNullOrWhiteSpace(explicitName)
                ? workspaces.GetLatestWorkspace()?.WorkspaceName ?? string.Empty
                : explicitName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve a chat-upload workspace name; parameters were omitted.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Reads string for <see cref="ReadChatUploadWorkspaceFileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace file function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadString(JsonElement parameters, string name)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var element) &&
                element.ValueKind == JsonValueKind.String
                    ? element.GetString()?.Trim() ?? string.Empty
                    : string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read string DXFunction parameter {ParameterName}.", name);
            return string.Empty;
        }
    }

    /// <summary>
    /// Reads int for <see cref="ReadChatUploadWorkspaceFileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read chat upload workspace file function workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <param name="minimum">Minimum value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the read chat upload workspace file function operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ReadInt(JsonElement parameters, string name, int fallback, int minimum, int maximum)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var element) &&
                element.TryGetInt32(out var parsed)
                    ? Math.Clamp(parsed, minimum, maximum)
                    : fallback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read a bounded integer DXFunction parameter {ParameterName}.", name);
            return fallback;
        }
    }

}
