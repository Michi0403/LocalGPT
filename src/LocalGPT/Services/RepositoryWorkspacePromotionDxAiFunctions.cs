using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Copies an already safely extracted uploaded repository or a selected registered project into an editable,
/// isolated Council artifact workspace. Quarantine and the original project remain unchanged.
/// </summary>
/// <param name="uploads">Read-only upload workspace inventory and root resolver.</param>
/// <param name="projects">Registered project metadata.</param>
/// <param name="artifacts">Application-owned Council artifact root.</param>
/// <param name="json">DXFunction binding and response service.</param>
/// <param name="developmentWorkspaces">Shared source-provenance and archive verification service.</param>
/// <param name="logger">Diagnostic logger.</param>
public sealed class PromoteRepositoryWorkspaceFunction(
    IChatUploadWorkspaceService uploads,
    ILocalGptProjectService projects,
    ICouncilArtifactService artifacts,
    IDxAiFunctionJsonService json,
    ICouncilDevelopmentWorkspaceService developmentWorkspaces,
    ILogger<PromoteRepositoryWorkspaceFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Explicit approval-gated archive or project snapshot promotion.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.repository.workspace.stage", "POST", "/api/dxai/functions/council.repository.workspace.stage/invoke",
        "Stages a complete selected uploaded ZIP extraction or registered project folder into an editable, disposable Council artifact workspace, preserving all source and relative paths.",
        "For ZIPs use workspaceName and exact archiveRoot returned by chat.upload_workspace_files. For a registered project use projectId; for an explicitly approved absolute local folder use sourcePath. Does not write into the original repository.",
        "Fresh human approval required. Refuses symlinks, unapproved paths and oversized snapshots; no execution, no commits, no overwrite of the original source.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"workspaceName":{"type":"string","maxLength":240},"archiveRoot":{"type":"string","maxLength":2048},"sourcePath":{"type":"string","maxLength":2048},"projectId":{"type":["string","null"],"format":"uuid"}},"additionalProperties":false}""");

    /// <summary>Snapshots selected repository bytes into a new workspace without executing or modifying the source.</summary>
    /// <param name="request">Approved snapshot selection.</param>
    /// <param name="cancellationToken">Cancellation token used between copied files.</param>
    /// <returns>The exact editable workspace name, path and copy inventory.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.UserConfirmed)
            return new DxAiFunctionInvocationResult { Status = "HumanApprovalRequired", Error = "Fresh approval is required before staging repository files." };
        var binding = json.Bind<RepositoryWorkspaceStageParameters>(request.Parameters);
        if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
        string? target = null;
        try
        {
            var p = binding.Value;
            string source;
            if (p.ProjectId is Guid projectId)
            {
                if (!string.IsNullOrWhiteSpace(p.SourcePath) || !string.IsNullOrWhiteSpace(p.ArchiveRoot) || !string.IsNullOrWhiteSpace(p.WorkspaceName))
                    return json.InvalidParameters("Select exactly one of projectId, sourcePath, or workspaceName with archiveRoot.");
                var project = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
                if (project is null || string.IsNullOrWhiteSpace(project.Project.RootPath))
                    return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The selected registered project has no local source path." };
                source = Path.GetFullPath(project.Project.RootPath);
            }
            else if (!string.IsNullOrWhiteSpace(p.SourcePath))
            {
                if (!string.IsNullOrWhiteSpace(p.ArchiveRoot) || !string.IsNullOrWhiteSpace(p.WorkspaceName) ||
                    !Path.IsPathFullyQualified(p.SourcePath))
                    return json.InvalidParameters("sourcePath must be an absolute local folder path, without an upload workspace selection.");
                source = Path.GetFullPath(p.SourcePath);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(p.WorkspaceName) || string.IsNullOrWhiteSpace(p.ArchiveRoot))
                    return json.InvalidParameters("Provide workspaceName and an exact archiveRoot from chat.upload_workspace_files.");
                var origin = uploads.ResolveWorkspacePath(p.WorkspaceName);
                if (origin is null)
                    return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The upload workspace is unavailable." };
                var catalog = uploads.QueryFiles(p.WorkspaceName, 1, 0);
                if (!catalog.ArchiveRoots.Contains(p.ArchiveRoot, StringComparer.Ordinal))
                    return json.InvalidParameters("archiveRoot must match an actual safely extracted ZIP root returned by the workspace inventory.");
                var extractionRoot = Path.GetFullPath(Path.Combine(origin, "extracted"));
                source = Path.GetFullPath(Path.Combine(origin, p.ArchiveRoot));
                if (!source.StartsWith(extractionRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return json.InvalidParameters("The selected root must remain inside the safely extracted upload directory.");
            }
            if (!Directory.Exists(source) || (File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The selected source directory is unavailable or is a link." };
            var sourceRoot = Path.GetFullPath(source);
            var artifactRoot = Path.GetFullPath(artifacts.ArtifactRoot);
            if (artifactRoot.Equals(sourceRoot, StringComparison.OrdinalIgnoreCase) ||
                artifactRoot.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return json.InvalidParameters("The selected source contains the CouncilArtifacts output directory; refusing recursive staging.");
            Directory.CreateDirectory(artifacts.ArtifactRoot);
            var workspaceName = "repository-stage-" + Guid.NewGuid().ToString("N");
            target = Path.Combine(artifacts.ArtifactRoot, workspaceName);
            Directory.CreateDirectory(target);
            long bytes = 0;
            var files = 0;
            foreach (var path in Directory.EnumerateFileSystemEntries(source, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("A repository snapshot contains a symbolic link or reparse point; refusing promotion.");
                var relative = Path.GetRelativePath(source, path);
                var copyPath = Path.Combine(target, relative);
                if (Directory.Exists(path)) { Directory.CreateDirectory(copyPath); continue; }
                var size = new FileInfo(path).Length;
                if (++files > 50000 || size > 512L * 1024 * 1024 || bytes + size > 1024L * 1024 * 1024)
                    throw new InvalidDataException("Repository snapshot exceeds the file count or total byte safety limit.");
                bytes += size;
                Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
                File.Copy(path, copyPath, overwrite: false);
            }
            var fingerprinted = await developmentWorkspaces.RecordStageBaselineAsync(artifacts.ArtifactRoot, workspaceName, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Staged {Count} files, {Bytes} bytes into approved isolated repository workspace {WorkspaceName}; {Fingerprinted} baseline source hashes recorded.", files, bytes, workspaceName, fingerprinted);
            return json.Success(new { WorkspaceName = workspaceName, RootPath = target, FileCount = files, TotalBytes = bytes,
                Next = "Use council.artifact_workspace_file.read/write or council.artifact_workspace.codedom.write, optional guest file checks, then council.artifact_workspace_zip and council.development.artifact.verify. Original project/archive stays unchanged; no Hyperlight repository mount." });
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Repository staging was canceled.");
            if (target is not null)
            {
                try { Directory.Delete(target, recursive: true); }
                catch (Exception cleanupFailure) { logger.LogWarning(cleanupFailure, "Cancelled staging workspace cleanup failed."); }
            }
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Repository staging failed; original sources remain unchanged.");
            if (target is not null)
            {
                try { Directory.Delete(target, recursive: true); }
                catch (Exception cleanupFailure) { logger.LogWarning(cleanupFailure, "Partial repository staging workspace could not be deleted."); }
            }
            return new DxAiFunctionInvocationResult { Status = "Failed", Error = "Repository staging failed; no source was changed. Check LocalGPT logs for details." };
        }
    }
}

/// <summary>One safe upload archive root or registered project to stage.</summary>
public sealed class RepositoryWorkspaceStageParameters
{
    /// <summary>Existing upload workspace name.</summary>
    public string WorkspaceName { get; set; } = string.Empty;
    /// <summary>Exact relative archive extraction root.</summary>
    public string ArchiveRoot { get; set; } = string.Empty;
    /// <summary>Explicit absolute local path, permitted only by fresh human approval.</summary>
    public string SourcePath { get; set; } = string.Empty;
    /// <summary>Selected registered project when the source is a filesystem path rather than an upload.</summary>
    public Guid? ProjectId { get; set; }
}
