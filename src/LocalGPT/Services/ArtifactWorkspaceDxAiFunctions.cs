using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.IO.Compression;
using System.Text;

namespace LocalGPT.Services;

/// <summary>
/// Lists generated council artifact workspaces through the DI-backed DXFunction registry.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the list artifact workspaces function workflow to provide the corresponding application capability.</param>
/// <param name="artifacts">Council artifact service dependency used by the list artifact workspaces function workflow to provide the corresponding application capability.</param>
/// <param name="runtime">Council runtime service dependency used by the list artifact workspaces function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the list artifact workspaces function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListArtifactWorkspacesFunction(
    IDxAiFunctionJsonService json,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    LocalGptCatalogService catalog,
    ILogger<ListArtifactWorkspacesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list artifact workspaces function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListArtifactWorkspacesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspaces",
        "POST",
        "/api/dxai/functions/council.artifact_workspaces/invoke",
        "Lists generated council artifact workspaces so the council can inspect and continue source generation in an existing workspace.",
        "JSON parameters: take optional integer. Omit it or use a non-positive value to use the database-backed MaxFiles policy instead of a hard-coded catalog ceiling.",
        "Read-only. It only enumerates workspaces under LocalGPT's configured CouncilArtifacts root.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ListArtifactWorkspacesFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"take":{"type":"integer"}},"additionalProperties":false}
        """);

    /// <summary>
    /// Lists artifact workspaces using the database-backed file-count policy for optional caller bounds.
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
            var binding = json.Bind<ArtifactWorkspaceListParameters>(request.Parameters);
            if (!binding.Succeeded)
                return Task.FromResult(json.InvalidParameters(binding.Error));

            var configuredMaximum = Math.Max(1, catalog.MaxFiles);
            var requestedTake = binding.Value.Take.GetValueOrDefault();
            var take = requestedTake > 0 ? Math.Min(requestedTake, configuredMaximum) : configuredMaximum;
            var workspaces = runtime.EnumerateArtifactWorkspaces(artifacts.ArtifactRoot, take, logger);
            logger.LogInformation("DXFunction listed {WorkspaceCount} generated artifact workspace(s); source content was omitted.", workspaces.Count);
            return Task.FromResult(json.Success(new
            {
                ArtifactRoot = artifacts.ArtifactRoot,
                Workspaces = workspaces,
                EffectiveTake = take
            }));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Listing generated artifact workspaces through DXFunction failed; parameters were omitted.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "Generated artifact workspaces could not be listed. Review LocalGPT application logs."
            });
        }
    }
}

/// <summary>
/// Lists editable source and documentation files inside one generated council artifact workspace.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the list artifact workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="artifacts">Council artifact service dependency used by the list artifact workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="runtime">Council runtime service dependency used by the list artifact workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the list artifact workspace files function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListArtifactWorkspaceFilesFunction(
    IDxAiFunctionJsonService json,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    LocalGptCatalogService catalog,
    ILogger<ListArtifactWorkspaceFilesFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list artifact workspace files function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListArtifactWorkspaceFilesFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace_files",
        "POST",
        "/api/dxai/functions/council.artifact_workspace_files/invoke",
        "Lists supported text/source files inside one generated artifact workspace, including C#, Razor, JavaScript, PowerShell, SQL and other database-provisioned text extensions.",
        "JSON parameters: workspaceName required; take optional integer. Omit take or use a non-positive value to use the database-backed MaxFiles policy.",
        "Read-only. Paths are constrained to the selected generated workspace and the database-provisioned ArtifactTextExtensions collection.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ListArtifactWorkspaceFilesFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["workspaceName"],"properties":{"workspaceName":{"type":"string"},"take":{"type":"integer"}},"additionalProperties":false}
        """);

    /// <summary>
    /// Lists text files for the requested workspace.
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
            var binding = json.Bind<ArtifactWorkspaceFilesParameters>(request.Parameters);
            if (!binding.Succeeded)
                return Task.FromResult(json.InvalidParameters(binding.Error));
            var parameters = binding.Value;
            if (string.IsNullOrWhiteSpace(parameters.WorkspaceName))
                return Task.FromResult(json.InvalidParameters("workspaceName is required."));

            var workspaceName = parameters.WorkspaceName.Trim();
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
            if (workspace is null)
            {
                return Task.FromResult(new DxAiFunctionInvocationResult
                {
                    Status = "NotFound",
                    Error = "The requested generated artifact workspace does not exist."
                });
            }

            var configuredMaximum = Math.Max(1, catalog.MaxFiles);
            var requestedTake = parameters.Take.GetValueOrDefault();
            var take = requestedTake > 0 ? Math.Min(requestedTake, configuredMaximum) : configuredMaximum;
            var files = runtime.EnumerateWorkspaceTextFiles(workspace, take, logger);
            logger.LogInformation("DXFunction listed {FileCount} generated workspace text file(s); source content was omitted.", files.Count);
            return Task.FromResult(json.Success(new
            {
                WorkspaceName = workspaceName,
                RootPath = workspace,
                Files = files,
                EffectiveTake = take
            }));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Listing generated artifact workspace files through DXFunction failed; parameters were omitted.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "Generated workspace files could not be listed. Review LocalGPT application logs."
            });
        }
    }
}

/// <summary>
/// Reads one supported text/source file from a generated artifact workspace.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the read artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="artifacts">Council artifact service dependency used by the read artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="runtime">Council runtime service dependency used by the read artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the read artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="text">Council text service dependency used by the read artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReadArtifactWorkspaceFileFunction(
    IDxAiFunctionJsonService json,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    LocalGptCatalogService catalog,
    CouncilTextService text,
    ILogger<ReadArtifactWorkspaceFileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the read artifact workspace file function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ReadArtifactWorkspaceFileFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace_file.read",
        "POST",
        "/api/dxai/functions/council.artifact_workspace_file.read/invoke",
        "Reads one generated workspace text/source file by relative path so the council can review or continue code generation.",
        "JSON parameters: workspaceName and relativePath are required.",
        "Read-only. The path must remain inside the generated workspace and use a database-provisioned ArtifactTextExtensions extension. File size follows MaxSingleFileBytes instead of a hard-coded inline-editor limit.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ReadArtifactWorkspaceFileFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["workspaceName","relativePath"],"properties":{"workspaceName":{"type":"string"},"relativePath":{"type":"string"}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="ReadArtifactWorkspaceFileFunction"/>, keeping the operation consistent with the state and invariants of the surrounding read artifact workspace file function workflow.
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
            var binding = json.Bind<ArtifactWorkspaceFileReadParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            if (string.IsNullOrWhiteSpace(parameters.WorkspaceName) || string.IsNullOrWhiteSpace(parameters.RelativePath))
                return json.InvalidParameters("workspaceName and relativePath are required.");

            var workspaceName = parameters.WorkspaceName.Trim();
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
            if (workspace is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested generated artifact workspace does not exist." };

            var relativePath = parameters.RelativePath.Trim();
            var file = runtime.ResolveWorkspaceTextFile(workspace, relativePath, allowMissing: false, logger);
            if (file is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested source file is missing, unsupported, or outside the generated workspace." };

            var info = new FileInfo(file);
            var maximumBytes = catalog.MaxSingleFileBytes;
            if (maximumBytes > 0 && info.Length > maximumBytes)
                return json.InvalidParameters($"The file exceeds the database-backed MaxSingleFileBytes policy ({maximumBytes:n0} bytes).");

            var content = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXFunction read generated workspace file {RelativePath} ({Length} bytes); file content was omitted from logs.", relativePath, info.Length);
            return json.Success(new
            {
                WorkspaceName = workspaceName,
                RootPath = workspace,
                RelativePath = text.ToForwardSlash(Path.GetRelativePath(workspace, file), logger),
                FullPath = file,
                Length = info.Length,
                info.LastWriteTimeUtc,
                Content = content
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reading a generated workspace source file through DXFunction failed; parameters and content were omitted.");
            return new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "The generated workspace file could not be read. Review LocalGPT application logs."
            };
        }
    }
}

/// <summary>
/// Writes one reviewed text/source file into a generated artifact workspace without executing it.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the write artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="artifacts">Council artifact service dependency used by the write artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="runtime">Council runtime service dependency used by the write artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the write artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="text">Council text service dependency used by the write artifact workspace file function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WriteArtifactWorkspaceFileFunction(
    IDxAiFunctionJsonService json,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    LocalGptCatalogService catalog,
    CouncilTextService text,
    ILogger<WriteArtifactWorkspaceFileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the write artifact workspace file function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="WriteArtifactWorkspaceFileFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace_file.write",
        "POST",
        "/api/dxai/functions/council.artifact_workspace_file.write/invoke",
        "Writes one reviewed plain-text/source file into an existing generated artifact workspace. This is the direct fallback route for generated C#, JavaScript, PowerShell (.ps1), SQL, Razor and other configured text files when CodeDOM is unsuitable or unavailable.",
        "JSON parameters: workspaceName, relativePath and content are required. The relative extension must exist in the database-provisioned ArtifactTextExtensions collection.",
        "Writes only inside the selected generated workspace and never executes, imports, builds, launches or installs the written file. Fresh human approval is required for the exact write.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["workspaceName","relativePath","content"],"properties":{"workspaceName":{"type":"string"},"relativePath":{"type":"string"},"content":{"type":"string"}},"additionalProperties":false}
        """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Writes the exact reviewed source text after the DXFunction approval gate authorizes the request.
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
            if (!request.UserConfirmed)
                return new DxAiFunctionInvocationResult { Status = "HumanApprovalRequired", Error = "Fresh human approval is required before writing a generated workspace file." };

            var binding = json.Bind<ArtifactWorkspaceFileWriteParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            if (string.IsNullOrWhiteSpace(parameters.WorkspaceName) ||
                string.IsNullOrWhiteSpace(parameters.RelativePath) ||
                parameters.Content is null)
            {
                return json.InvalidParameters("workspaceName, relativePath and content are required. Empty content is allowed but the content property must be present.");
            }

            var workspaceName = parameters.WorkspaceName.Trim();
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
            if (workspace is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested generated artifact workspace does not exist." };

            var relativePath = parameters.RelativePath.Trim();
            var file = runtime.ResolveWorkspaceTextFile(workspace, relativePath, allowMissing: true, logger);
            if (file is null)
                return json.InvalidParameters("The requested relativePath is unsupported or escapes the generated workspace.");

            var content = parameters.Content;
            var byteCount = Encoding.UTF8.GetByteCount(content);
            var maximumBytes = catalog.MaxSingleFileBytes;
            if (maximumBytes > 0 && byteCount > maximumBytes)
                return json.InvalidParameters($"The source text exceeds the database-backed MaxSingleFileBytes policy ({maximumBytes:n0} bytes).");

            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? workspace);
            await File.WriteAllTextAsync(file, content, cancellationToken).ConfigureAwait(false);
            var info = new FileInfo(file);
            var normalizedRelativePath = text.ToForwardSlash(Path.GetRelativePath(workspace, file), logger);
            logger.LogInformation("DXFunction wrote generated workspace file {RelativePath} ({Length} bytes); source content was omitted from logs.", normalizedRelativePath, info.Length);
            return json.Success(new
            {
                WorkspaceName = workspaceName,
                RootPath = workspace,
                RelativePath = normalizedRelativePath,
                FullPath = file,
                Length = info.Length,
                info.LastWriteTimeUtc,
                Message = "Reviewed source file saved without execution. Refresh the workspace ZIP separately when a downloadable bundle is wanted."
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Writing a generated workspace source file through DXFunction failed; parameters and content were omitted.");
            return new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "The generated workspace file could not be written. Review LocalGPT application logs."
            };
        }
    }
}

/// <summary>
/// Refreshes the downloadable ZIP for one generated council artifact workspace.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the refresh artifact workspace ZIP function workflow to provide the corresponding application capability.</param>
/// <param name="artifacts">Council artifact service dependency used by the refresh artifact workspace ZIP function workflow to provide the corresponding application capability.</param>
/// <param name="runtime">Council runtime service dependency used by the refresh artifact workspace ZIP function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RefreshArtifactWorkspaceZipFunction(
    IDxAiFunctionJsonService json,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    ILogger<RefreshArtifactWorkspaceZipFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the refresh artifact workspace ZIP function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="RefreshArtifactWorkspaceZipFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace_zip",
        "POST",
        "/api/dxai/functions/council.artifact_workspace_zip/invoke",
        "Refreshes the downloadable ZIP from the current generated source workspace after reviewed file edits.",
        "JSON parameters: workspaceName required.",
        "Creates a ZIP under CouncilArtifacts without executing workspace content. Fresh human approval is required for this exact filesystem mutation.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["workspaceName"],"properties":{"workspaceName":{"type":"string"}},"additionalProperties":false}
        """,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: true);

    /// <summary>
    /// Rebuilds the workspace ZIP after human approval.
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
            if (!request.UserConfirmed)
                return Task.FromResult(new DxAiFunctionInvocationResult { Status = "HumanApprovalRequired", Error = "Fresh human approval is required before refreshing a generated workspace ZIP." });

            var binding = json.Bind<ArtifactWorkspaceZipParameters>(request.Parameters);
            if (!binding.Succeeded)
                return Task.FromResult(json.InvalidParameters(binding.Error));
            if (string.IsNullOrWhiteSpace(binding.Value.WorkspaceName))
                return Task.FromResult(json.InvalidParameters("workspaceName is required."));

            var workspaceName = binding.Value.WorkspaceName.Trim();
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
            if (workspace is null)
                return Task.FromResult(new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The requested generated artifact workspace does not exist." });

            var zipName = $"{workspaceName}-workspace.zip";
            var zipPath = Path.Combine(artifacts.ArtifactRoot, zipName);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(workspace, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: true);
            var downloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}";
            logger.LogInformation("DXFunction refreshed generated artifact workspace ZIP {ZipName}; workspace source content was omitted.", zipName);
            return Task.FromResult(json.Success(new
            {
                WorkspaceName = workspaceName,
                RootPath = workspace,
                ZipPath = zipPath,
                DownloadUrl = downloadUrl,
                Message = "Workspace ZIP refreshed from the current reviewed source directory without executing its contents."
            }));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Refreshing a generated artifact workspace ZIP through DXFunction failed; parameters were omitted.");
            return Task.FromResult(new DxAiFunctionInvocationResult
            {
                Status = "Failed",
                Error = "The generated workspace ZIP could not be refreshed. Review LocalGPT application logs."
            });
        }
    }
}
