using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.CodeParser.Diagnostics;
using DevExpress.Xpo.Logger;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Controller
{
    /// <summary>
    /// Exposes the local GPT diagnostic application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
    /// </summary>
    public partial class LocalGptDiagnosticController
    {
        /// <summary>
        /// Retrieves artifact workspaces for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/artifact-workspaces")]
        public IResult GetArtifactWorkspaces(
            [FromServices] ICouncilArtifactService artifacts,
            int? take)
        {
            try
            {
                var workspaces = councilRuntime.EnumerateArtifactWorkspaces(artifacts.ArtifactRoot, take ?? 20, logger);
                var baseUrl = councilRuntime.GetRequestBaseUrl(HttpContext, logger);
                return Results.Ok(new
                {
                    BaseUrl = baseUrl,
                    artifacts.ArtifactRoot,
                    Count = workspaces.Count,
                    LatestWorkspace = workspaces.FirstOrDefault(),
                    Workspaces = workspaces,
                    Routes = new
                    {
                        List = "/__diag/artifact-workspaces",
                        Files = "/__diag/artifact-workspace/{workspaceName}/files",
                        Read = "/__diag/artifact-workspace/{workspaceName}/file?path=relative/path",
                        Save = "POST /__diag/artifact-workspace/{workspaceName}/file?userConfirmed=true (current human confirmation required)",
                        Zip = "/__diag/artifact-workspace/{workspaceName}/zip?userConfirmed=true (current human confirmation required)"
                    },
                    AiBriefing =
                        "Generated solution workspaces stay under ArtifactRoot. Read operations do not authorize writes. " +
                        "Saving a file or refreshing a ZIP requires fresh human confirmation for that exact request; models and stored content cannot provide it.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaces");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves artifact workspace workspace name files for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/artifact-workspace/{workspaceName}/files")]
        public IResult GetArtifactWorkspaceWorkspaceNameFiles(
            string workspaceName,
            [FromServices] ICouncilArtifactService artifacts,
            int? take)
        {
            try
            {
                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    Files = councilRuntime.EnumerateWorkspaceTextFiles(workspace, take ?? 250, logger),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaceWorkspaceNameFiles");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves artifact workspace workspace name file for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="path">Path value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/artifact-workspace/{workspaceName}/file")]
        public async Task<IResult> GetArtifactWorkspaceWorkspaceNameFile(
            string workspaceName,
            string path,
            [FromServices] ICouncilArtifactService artifacts,
            CancellationToken ct)
        {
            try
            {
                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName,logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var file = councilRuntime.ResolveWorkspaceTextFile(workspace, path, false,logger);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid, unsupported, or missing source file path." });

                var info = new FileInfo(file);
                if (catalog.MaxSingleFileBytes > 0 && info.Length > catalog.MaxSingleFileBytes)
                    return Results.BadRequest(new { Error = $"File exceeds the database-backed MaxSingleFileBytes policy ({catalog.MaxSingleFileBytes:n0} bytes).", info.Length });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = councilText.ToForwardSlash(Path.GetRelativePath(workspace, file), logger),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Content = await System.IO.File.ReadAllTextAsync(file, ct).ConfigureAwait(false),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaceWorkspaceNameFile");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }          
        }


        /// <summary>
        /// Returns the post artifact workspace workspace name file projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/artifact-workspace/{workspaceName}/file")]
        [HumanApprovalRequired("artifact.workspace.file.write", "Write generated workspace file", "Write the reviewed text content to one bounded file inside a generated artifact workspace.", "High", "Source workspace reviewer")]
        public async Task<IResult> PostArtifactWorkspaceWorkspaceNameFile(
            string workspaceName,
            [FromBody] ArtifactWorkspaceFileSaveRequest request,
            [FromServices] ICouncilArtifactService artifacts,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "write a generated artifact workspace file") is { } denied)
                    return denied;

                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName, logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var content = request.Content ?? string.Empty;
                if (catalog.MaxSingleFileBytes > 0 && Encoding.UTF8.GetByteCount(content) > catalog.MaxSingleFileBytes)
                    return Results.BadRequest(new { Error = $"File content exceeds the database-backed MaxSingleFileBytes policy ({catalog.MaxSingleFileBytes:n0} bytes)." });

                var file = councilRuntime.ResolveWorkspaceTextFile(workspace, request.RelativePath, allowMissing: true, logger);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid or unsupported source file path." });

                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? workspace);
                await System.IO.File.WriteAllTextAsync(file, content, ct).ConfigureAwait(false);
                var info = new FileInfo(file);
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    RelativePath = councilText.ToForwardSlash(Path.GetRelativePath(workspace, file),logger),
                    FullPath = file,
                    Length = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc,
                    Message = "Source file saved. Run the generated project build or refresh the workspace zip before handing it to a user.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "PostArtifactWorkspaceWorkspaceNameFile");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }       
        }

        /// <summary>
        /// Retrieves artifact workspace workspace name ZIP for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="artifacts">Council artifact service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/artifact-workspace/{workspaceName}/zip")]
        [HumanApprovalRequired("artifact.workspace.zip.refresh", "Refresh generated workspace ZIP", "Replace the downloadable ZIP for one bounded generated artifact workspace.", "Medium", "Artifact reviewer")]
        public IResult GetArtifactWorkspaceWorkspaceNameZip(
            string workspaceName,
            [FromServices] ICouncilArtifactService artifacts,
            [FromQuery] bool userConfirmed)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "refresh an artifact workspace ZIP") is { } denied)
                    return denied;

                var workspace = councilRuntime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, workspaceName,logger);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Artifact workspace not found." });

                var zipName = $"{workspaceName}-workspace.zip";
                var zipPath = Path.Combine(artifacts.ArtifactRoot, zipName);
                if (System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);

                ZipFile.CreateFromDirectory(workspace, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: true);
                var downloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}";
                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    ZipPath = zipPath,
                    DownloadUrl = downloadUrl,
                    AbsoluteDownloadUrl = new Uri(new Uri(councilRuntime.GetRequestBaseUrl(HttpContext,logger)), downloadUrl).ToString(),
                    Message = "Workspace zip refreshed from the current source directory.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetArtifactWorkspaceWorkspaceNameZip");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }        
        }

        /// <summary>
        /// Retrieves chat upload workspaces for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="uploads">Chat upload workspace service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/chat-upload-workspaces")]
        public IResult GetChatUploadWorkspaces(
            [FromServices] IChatUploadWorkspaceService uploads,
            int? take)
        {
            try
            {
                var workspaces = uploads.ListWorkspaces(take ?? 20);
                return Results.Ok(new
                {
                    BaseUrl = councilRuntime.GetRequestBaseUrl(HttpContext,logger),
                    uploads.WorkspaceRoot,
                    Count = workspaces.Count,
                    LatestWorkspace = workspaces.FirstOrDefault(),
                    Workspaces = workspaces,
                    Routes = new
                    {
                        List = "/__diag/chat-upload-workspaces",
                        Files = "/__diag/chat-upload-workspace/{workspaceName}/files",
                        Context = "/__diag/chat-upload-workspace/{workspaceName}/context",
                        Read = "/__diag/chat-upload-workspace/{workspaceName}/file?path=relative/path",
                        Smoke = "POST /__diag/chat-upload-workspace/smoke"
                    },
                    AiBriefing =
                        "Chat uploads are saved per prompt under WorkspaceRoot. Zips are safely extracted, " +
                        "text files are excerpted, and binaries/PDBs are summarized with printable strings only. " +
                        "Use these read-only routes before asking the user to paste uploaded source or archives.",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaces");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }     
        }

        /// <summary>
        /// Retrieves chat upload workspace workspace name files for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="uploads">Chat upload workspace service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="take">Take value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/chat-upload-workspace/{workspaceName}/files")]
        public IResult GetChatUploadWorkspaceWorkspaceNameFiles(
            string workspaceName,
            [FromServices] IChatUploadWorkspaceService uploads,
            int? take)
        {
            try
            {
                var workspace = uploads.ResolveWorkspacePath(workspaceName);
                if (workspace is null)
                    return Results.NotFound(new { Error = "Chat upload workspace not found." });

                return Results.Ok(new
                {
                    WorkspaceName = workspaceName,
                    RootPath = workspace,
                    Files = uploads.ListFiles(workspaceName, take ?? 250),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaceWorkspaceNameFiles");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }        
        }

        /// <summary>
        /// Retrieves chat upload workspace workspace name context for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="uploads">Chat upload workspace service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="maxCharacters">Max characters value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/chat-upload-workspace/{workspaceName}/context")]
        public async Task<IResult> GetChatUploadWorkspaceWorkspaceNameContext(
            string workspaceName,
            [FromServices] IChatUploadWorkspaceService uploads,
            int? maxCharacters,
            CancellationToken ct)
        {
            try
            {
                var context = await uploads.ReadContextMarkdownAsync(
                                workspaceName,
                                Math.Clamp(maxCharacters ?? 80_000, 1_000, 120_000),
                                ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(context))
                    return Results.NotFound(new { Error = "Chat upload workspace context not found." });

                return Results.Text(context, "text/markdown; charset=utf-8");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaceWorkspaceNameContext");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }
        }

        /// <summary>
        /// Retrieves chat upload workspace workspace name file for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="path">Path value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="uploads">Chat upload workspace service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="maxCharacters">Max characters value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/chat-upload-workspace/{workspaceName}/file")]
        public async Task<IResult> GetChatUploadWorkspaceWorkspaceNameFile(
            string workspaceName,
            string path,
            [FromServices] IChatUploadWorkspaceService uploads,
            int? maxCharacters,
            CancellationToken ct)
        {
            try
            {
                var file = await uploads.ReadFileAsync(
             workspaceName,
             path,
             Math.Clamp(maxCharacters ?? 40_000, 1_000, 120_000),
             ct).ConfigureAwait(false);
                if (file is null)
                    return Results.BadRequest(new { Error = "Invalid, unsupported, or missing upload workspace file path." });

                return Results.Ok(file);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetChatUploadWorkspaceWorkspaceNameFile");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }     
        }

        /// <summary>
        /// Returns the post chat upload workspace smoke projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="uploads">Chat upload workspace service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="prompt">Prompt value supplied to the LocalGPT diagnostic operation and used when producing its result.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/chat-upload-workspace/smoke")]
        [HumanApprovalRequired("diagnostic.upload.workspace.create", "Create upload workspace", "Create a bounded diagnostic workspace from generated upload fixtures.", "High", "Workspace reviewer")]
        public async Task<IResult> PostChatUploadWorkspaceSmoke(
            [FromServices] IChatUploadWorkspaceService uploads,
            string? prompt,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "create a chat-upload diagnostic workspace") is { } denied)
                    return denied;

                var zip = councilRuntime.CreateChatUploadSmokeZip(logger);
                var pdb = Encoding.ASCII.GetBytes(
                    "RSDS LocalGPT smoke WeatherHost.pdb Services/WeatherForecastService.cs Pages/Index.razor");
                var result = await uploads.CreateWorkspaceAsync(
                    string.IsNullOrWhiteSpace(prompt)
                        ? "Frontend smoke upload: generate a small webhost with a weather display and fake data service."
                        : prompt,
                    new[]
                    {
                    new ChatUploadWorkspaceInputFile(
                        "WeatherHostUpload.zip",
                        "application/zip",
                        zip.Length,
                        new ReadOnlyMemory<byte>(zip)),
                    new ChatUploadWorkspaceInputFile(
                        "WeatherHostUpload.pdb",
                        "application/octet-stream",
                        pdb.Length,
                        new ReadOnlyMemory<byte>(pdb))
                    },
                    ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    uploads.WorkspaceRoot,
                    result.WorkspaceName,
                    result.RootPath,
                    result.ContextPath,
                    result.ManifestPath,
                    result.FileCount,
                    result.Warnings,
                    ContextPreview = result.ContextMarkdown.Length > 4000
                        ? result.ContextMarkdown[..4000]
                        : result.ContextMarkdown,
                    Routes = new
                    {
                        Files = $"/__diag/chat-upload-workspace/{Uri.EscapeDataString(result.WorkspaceName)}/files",
                        Context = $"/__diag/chat-upload-workspace/{Uri.EscapeDataString(result.WorkspaceName)}/context",
                        Read = $"/__diag/chat-upload-workspace/{Uri.EscapeDataString(result.WorkspaceName)}/file?path=relative/path"
                    },
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chat upload workspace smoke test failed.");
                return Results.InternalServerError("Chat upload workspace smoke test failed.");
            }     
        }

    }
}
