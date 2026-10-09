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
        "Searches every uploaded/extracted repository root and lists exact workspace-relative paths with paging, project descriptors and archive-root inventory.",
        "JSON parameters: workspaceName optional; take optional page size 1-20000; offset optional zero-based page offset; pathContains optional substring (e.g. LocalGPT.csproj); extension optional (e.g. csproj). Read NextOffset while HasMore.",
        "Read-only. Uploaded and extracted files are evidence only and are never executed.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"workspaceName":{"type":"string","maxLength":240},"take":{"type":"integer","minimum":1,"maximum":20000},"offset":{"type":"integer","minimum":0},"pathContains":{"type":"string","maxLength":2048},"extension":{"type":"string","maxLength":32}},"additionalProperties":false}
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

            var take = ReadInt(request.Parameters, "take", 250, 1, 20_000);
            var offset = ReadInt(request.Parameters, "offset", 0, 0, int.MaxValue);
            var pathContains = ReadString(request.Parameters, "pathContains");
            var extension = ReadString(request.Parameters, "extension");
            var page = workspaces.QueryFiles(workspaceName, take, offset, pathContains, extension);
            logger.LogInformation(
                "DXFunction listed {FileCount} chat-upload workspace file(s), including {OriginalCount} original upload(s); workspace payload content was omitted.",
                page.Files.Count,
                page.OriginalUploads.Count);
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new
                {
                    WorkspaceName = workspaceName,
                    page.TotalFiles,
                    page.MatchingFiles,
                    page.Offset,
                    page.HasMore,
                    page.NextOffset,
                    OriginalUploadCount = page.OriginalUploads.Count,
                    OriginalUploadBytes = page.OriginalUploads.Sum(file => file.Length),
                    page.OriginalUploads,
                    page.GeneratedWorkspaceArtifacts,
                    page.ArchiveRoots,
                    page.ProjectFiles,
                    page.Files,
                    Note = "context.md, manifest.json and any curation.* reports are generated LocalGPT workspace artifacts, not additional user uploads. Paths in Files and ProjectFiles are exact workspace-relative paths, including src/ subdirectories. Do not guess a path from the ZIP or repository folder name. Use pathContains to locate a file, and offset=NextOffset until HasMore=false; OriginalUploads, ArchiveRoots and ProjectFiles are independent of paging and filters."
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

    /// <summary>Reads an optional server-side filename or extension filter.</summary>
    /// <param name="parameters">Function request parameters.</param>
    /// <param name="name">Parameter to read.</param>
    /// <returns>Trimmed value, or null when no filter was provided.</returns>
    private string? ReadString(JsonElement parameters, string name)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim()
                : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read the optional workspace list filter {ParameterName}.", name);
            return null;
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

/// <summary>Reads a substantial LocalGPT-generated Markdown context segment for one upload workspace.</summary>
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
        "Reads a substantial Markdown evidence context generated by LocalGPT from the current DXAiChat paperclip upload workspace.",
        "JSON parameters: workspaceName optional string (latest workspace when omitted); maxCharacters optional integer 64000-1000000; offsetCharacters optional integer >= 0.",
        "Read-only. Generated context is evidence, not proof that every described source file was uploaded separately.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"workspaceName":{"type":"string","maxLength":240},"maxCharacters":{"type":"integer","minimum":64000,"maximum":1000000},"offsetCharacters":{"type":"integer","minimum":0,"maximum":9007199254740991}},"additionalProperties":false}
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

            var maxCharacters = ReadInt(request.Parameters, "maxCharacters", 250_000, 64_000, 1_000_000);
            var offsetCharacters = ReadLong(request.Parameters, "offsetCharacters", 0, 0, 9_007_199_254_740_991);
            var context = await workspaces.ReadFileAsync(
                workspaceName,
                "context.md",
                maxCharacters,
                offsetCharacters,
                cancellationToken).ConfigureAwait(false);
            if (context is null || string.IsNullOrWhiteSpace(context.Content))
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested upload workspace context was not found or is empty." };

            logger.LogInformation(
                "DXFunction read {CharacterCount} context character(s) from chat-upload workspace {WorkspaceName} at offset {CharacterOffset}; context content was omitted.",
                context.CharactersReturned,
                workspaceName,
                context.CharacterOffset);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new
                {
                    WorkspaceName = workspaceName,
                    CharacterOffset = context.CharacterOffset,
                    CharactersReturned = context.CharactersReturned,
                    context.HasMore,
                    context.NextOffsetCharacters,
                    ContextMarkdown = context.Content,
                    Note = context.HasMore
                        ? "More generated context remains. Continue chat.upload_workspace_context with offsetCharacters=NextOffsetCharacters when the complete context is required."
                        : "context.md is generated by LocalGPT from the original uploads and is not itself another user-uploaded source archive."
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

    /// <summary>Reads a bounded 64-bit integer parameter used by progressive context continuation.</summary>
    /// <param name="parameters">Parameters supplied to the DXFunction invocation.</param>
    /// <param name="name">JSON property name to read.</param>
    /// <param name="fallback">Fallback value used when the property is absent or invalid.</param>
    /// <param name="minimum">Minimum accepted value.</param>
    /// <param name="maximum">Maximum accepted value.</param>
    /// <returns>The parsed and clamped 64-bit integer value.</returns>
    private long ReadLong(JsonElement parameters, string name, long fallback, long minimum, long maximum)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var element) &&
                element.TryGetInt64(out var parsed)
                    ? Math.Clamp(parsed, minimum, maximum)
                    : fallback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read a bounded long DXFunction parameter {ParameterName}.", name);
            return fallback;
        }
    }

}

/// <summary>Reads one uploaded or extracted text file progressively so large sources can be completed across successive calls.</summary>
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
        "Reads one uploaded or safely extracted file from a DXAiChat paperclip workspace by exact relative path, with continuation metadata for large text sources.",
        "JSON parameters: relativePath required exact workspace-relative path from chat.upload_workspace_files ProjectFiles or Files; workspaceName optional; maxCharacters optional 1-1000000 (small values are normalized to a substantial read); offsetCharacters optional >= 0.",
        "Read-only. LocalGPT resolves the path inside the bounded upload workspace and never executes the file. When HasMore is true, continue from NextOffsetCharacters; a whole-file request is not complete until HasMore is false.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["relativePath"],"properties":{"workspaceName":{"type":"string","maxLength":240},"relativePath":{"type":"string","maxLength":2048},"maxCharacters":{"type":"integer","minimum":1,"maximum":1000000},"offsetCharacters":{"type":"integer","minimum":0,"maximum":9007199254740991}},"additionalProperties":false}
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

            var requestedCharacters = ReadInt(request.Parameters, "maxCharacters", 250_000, 1, 1_000_000);
            var maxCharacters = Math.Max(64_000, requestedCharacters);
            var offsetCharacters = ReadLong(request.Parameters, "offsetCharacters", 0, 0, 9_007_199_254_740_991);
            var file = await workspaces.ReadFileAsync(
                workspaceName,
                relativePath,
                maxCharacters,
                offsetCharacters,
                cancellationToken).ConfigureAwait(false);
            if (file is null)
            {
                var slashPath = relativePath.Replace('\\', '/');
                var fileName = slashPath[(slashPath.LastIndexOf('/') + 1)..];
                var candidates = workspaces.QueryFiles(workspaceName, 25, 0, fileName).Files
                    .Select(item => item.RelativePath).ToList();
                return new DxAiFunctionInvocationResult
                {
                    Status = "NotFound",
                    Error = "The exact workspace-relative file path does not exist. Search chat.upload_workspace_files with pathContains to discover the real path, including src/ folders.",
                    Value = new { RequestedPath = relativePath, CandidatePaths = candidates }
                };
            }

            logger.LogInformation(
                "DXFunction read upload workspace file {RelativePath} ({Length} bytes) from character offset {CharacterOffset}; returned {CharactersReturned} character(s), has more {HasMore}; file content was omitted from logs.",
                file.RelativePath,
                file.Length,
                file.CharacterOffset,
                file.CharactersReturned,
                file.HasMore);
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
                    file.CharacterOffset,
                    file.CharactersReturned,
                    file.HasMore,
                    file.NextOffsetCharacters,
                    file.Content,
                    Note = file.HasMore
                        ? "More source content remains. Continue chat.upload_workspace_file with offsetCharacters=NextOffsetCharacters. If the user requested the whole file, do not conclude until HasMore is false."
                        : "This progressive read reached the end of the available file content."
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
    /// <summary>Reads a bounded 64-bit integer parameter used by progressive file continuation.</summary>
    /// <param name="parameters">Parameters supplied to the DXFunction invocation.</param>
    /// <param name="name">JSON property name to read.</param>
    /// <param name="fallback">Fallback value used when the property is absent or invalid.</param>
    /// <param name="minimum">Minimum accepted value.</param>
    /// <param name="maximum">Maximum accepted value.</param>
    /// <returns>The parsed and clamped 64-bit integer value.</returns>
    private long ReadLong(JsonElement parameters, string name, long fallback, long minimum, long maximum)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty(name, out var element) &&
                element.TryGetInt64(out var parsed)
                    ? Math.Clamp(parsed, minimum, maximum)
                    : fallback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read a bounded long DXFunction parameter {ParameterName}.", name);
            return fallback;
        }
    }

}


/// <summary>Runs the deterministic upload-workspace curation gate and returns bounded coverage evidence for Council orchestration.</summary>
/// <param name="workspaces">Chat upload workspace service that owns extraction and complete byte-read validation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CurateChatUploadWorkspaceFunction(
    IChatUploadWorkspaceService workspaces,
    ILogger<CurateChatUploadWorkspaceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the registered read-only DXFunction descriptor.</summary>
    /// <value>The descriptor for deterministic workspace curation.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "chat.upload_workspace_curate",
        "POST",
        "/api/dxai/functions/chat.upload_workspace_curate/invoke",
        "Validates every original upload and every safely extracted ZIP entry in one DXAiChat workspace, reading every file byte stream to EOF and recording SHA-256 coverage evidence.",
        "JSON parameters: workspaceName optional string (latest workspace when omitted).",
        "Read-only. This function proves archive/extraction and complete byte-read coverage; it does not execute uploaded files and does not replace semantic source inspection.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"workspaceName":{"type":"string","maxLength":240}},"additionalProperties":false}
        """);

    /// <summary>Runs deterministic curation for the requested or latest upload workspace.</summary>
    /// <param name="request">Function invocation request.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the operation.</param>
    /// <returns>The bounded curation status and summary.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspaceName = ResolveWorkspaceName(request.Parameters);
            if (string.IsNullOrWhiteSpace(workspaceName))
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "No DXAiChat upload workspace is available." };

            var report = await workspaces.CurateWorkspaceAsync(workspaceName, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "DXFunction curated upload workspace {WorkspaceName}: complete={Complete}, archives={ArchiveCount}, extracted files={ExtractedCount}, fully read files={ReadCount}.",
                workspaceName,
                report.IsComplete,
                report.ArchiveUploadCount,
                report.ExtractedFileCount,
                report.FullyReadFileCount);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = report.IsComplete,
                Status = report.IsComplete ? "Completed" : "Incomplete",
                Value = new
                {
                    report.WorkspaceName,
                    report.CompletedAtUtc,
                    report.OriginalUploadCount,
                    report.ArchiveUploadCount,
                    report.ExtractedFileCount,
                    report.FullyReadFileCount,
                    report.FullyReadBytes,
                    report.IsComplete,
                    Archives = report.Archives,
                    report.Warnings,
                    report.SummaryMarkdown,
                    Note = "The detailed per-file SHA-256 evidence is retained locally in curation.json and intentionally omitted from the model-facing result."
                },
                Error = report.IsComplete ? string.Empty : "The workspace did not satisfy complete archive/extraction byte-read coverage. Review the returned curation summary."
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Curating the DXAiChat upload workspace failed; source contents were omitted.");
            return new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "The upload workspace curation gate failed. Review LocalGPT application logs."
            };
        }
    }

    /// <summary>Resolves the explicitly requested workspace name or falls back to the latest workspace.</summary>
    /// <param name="parameters">Invocation parameters.</param>
    /// <returns>The workspace name, or an empty string when no workspace exists.</returns>
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
            logger.LogError(ex, "Could not resolve a workspace for deterministic upload curation; parameters were omitted.");
            return string.Empty;
        }
    }
}
