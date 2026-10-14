using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Produces editable Godot C# projects, safely exports to a user-chosen project root and imports manual Godot edits.</summary>
/// <param name="projects">Local gpt project service dependency used by the game godot workspace workflow to provide the corresponding application capability.</param>
/// <param name="studio">Game studio service dependency used by the game godot workspace workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the game godot workspace workflow to provide the corresponding application capability.</param>
/// <param name="engine">Engine value supplied to the game godot workspace operation and used when producing its result.</param>
/// <param name="platform">Platform runtime service dependency used by the game godot workspace workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GameGodotWorkspaceService(
    ILocalGptProjectService projects,
    IGameStudioService studio,
    IProjectArchitectureService architecture,
    GameSceneEngine engine,
    IPlatformRuntimeService platform,
    ILogger<GameGodotWorkspaceService> logger) : IGameGodotWorkspaceService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="GameGodotWorkspaceService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Creates baseline Godot project, .NET script, scene and editable scene data if those worktree paths are not already present.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft> SeedGodotFilesAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var draft = await RequireOpenAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var script = ReadGodotTemplate();
            var files = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["project.godot"] = "config_version=5\n\n[application]\nconfig/name=\"LocalGPT Game Studio\"\nrun/main_scene=\"res://scenes/Main.tscn\"\n\n[rendering]\nrenderer/rendering_method=\"gl_compatibility\"\n",
                ["GameStudio.csproj"] = "<Project Sdk=\"Godot.NET.Sdk/4.4.1\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n    <EnableDynamicLoading>true</EnableDynamicLoading>\n  </PropertyGroup>\n</Project>\n",
                ["scenes/Main.tscn"] = "[gd_scene load_steps=2 format=3]\n\n[ext_resource type=\"Script\" path=\"res://src/LocalGptSceneRuntime.cs\" id=\"1_runtime\"]\n\n[node name=\"LocalGptWorld\" type=\"Node2D\"]\nscript = ExtResource(\"1_runtime\")\n",
                ["src/LocalGptSceneRuntime.cs"] = script,
                ["scene.json"] = JsonSerializer.Serialize(draft.Scene, jsonOptions)
            };
            foreach (var (path, content) in files)
                if (!draft.FilePaths.Contains(path, StringComparer.Ordinal))
                    draft = await studio.SaveFileAsync(projectId, draftId, path, content, true, cancellationToken).ConfigureAwait(false);
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing editable Godot C# files failed for Game project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Explicitly publishes the currently curated scene document into its editable Godot scene.json source file.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft> UpdateSceneResourceAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var draft = await RequireOpenAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            if (engine.Curate(draft.Scene).Any(item => item.BlocksAcceptance))
                throw new InvalidOperationException("Only a curated scene can be published to the Godot worktree.");
            return await studio.SaveFileAsync(projectId, draftId, "scene.json", JsonSerializer.Serialize(draft.Scene, jsonOptions), true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing a scene to an editable Godot worktree failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Writes a complete draft to the user's configured project root, with preflight conflicts for all externally changed files.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<string>> ExportAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var draft = await RequireOpenAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var root = await ResolveRootAsync(projectId, cancellationToken).ConfigureAwait(false);
            var receipts = await ReadReceiptsAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var pending = new List<(string Path, string Text, string Hash)>();
            foreach (var path in draft.FilePaths)
            {
                var destination = CheckedFullPath(root, path);
                var next = await studio.ReadFileAsync(projectId, draftId, path, cancellationToken).ConfigureAwait(false);
                if (File.Exists(destination))
                {
                    var existing = await File.ReadAllTextAsync(destination, cancellationToken).ConfigureAwait(false);
                    var previous = Hash(existing);
                    if (previous != Hash(next) && (!receipts.TryGetValue(path, out var authorizedHash) || previous != authorizedHash))
                        throw new IOException($"External edits detected in '{path}'. Import them into the draft first; export cannot overwrite them.");
                }
                pending.Add((destination, next, Hash(next)));
            }
            foreach (var (destination, content, _) in pending)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await File.WriteAllTextAsync(destination, content, cancellationToken).ConfigureAwait(false);
            }
            foreach (var (path, result) in draft.FilePaths.Zip(pending))
                receipts[path] = result.Hash;
            await SaveReceiptsAsync(projectId, draftId, receipts, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Exported {FileCount} Godot project files from draft {DraftId} to approved project root.", pending.Count, draftId);
            return draft.FilePaths;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Conflict-safe Godot export failed for project {ProjectId}; local work was preserved where possible.", projectId);
            throw;
        }
    }

    /// <summary>Reimports real changes made with Godot or a code editor into the open draft without closing its revision.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<string>> ImportChangedAsync(Guid projectId, Guid draftId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var draft = await RequireOpenAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var root = await ResolveRootAsync(projectId, cancellationToken).ConfigureAwait(false);
            var receipts = await ReadReceiptsAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var changed = new List<(string Path, string Content)>();
            var tracked = new HashSet<string>(draft.FilePaths, StringComparer.Ordinal);
            if (File.Exists(CheckedFullPath(root, "project.godot")))
            {
                foreach (var full in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly).Take(256))
                {
                    var relative = Path.GetFileName(full);
                    if (relative.StartsWith('.') || relative.Length > 180) continue;
                    if (Path.GetExtension(relative) is not (".cs" or ".tscn" or ".tres" or ".json" or ".gdshader" or ".godot" or ".csproj" or ".sln")) continue;
                    CheckedFullPath(root, relative);
                    tracked.Add(relative);
                }
                foreach (var folder in new[] { "src", "scenes", "assets" })
                {
                    var directory = CheckedFullPath(root, folder);
                    if (!Directory.Exists(directory)) continue;
                    foreach (var full in Directory.EnumerateFiles(directory, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        AttributesToSkip = FileAttributes.ReparsePoint,
                        IgnoreInaccessible = true
                    }).Take(512))
                    {
                        var relative = Path.GetRelativePath(root, full).Replace(Path.DirectorySeparatorChar, '/');
                        if (relative.Split('/').Any(part => part.StartsWith('.') || part is "bin" or "obj" or ".godot")) continue;
                        if (relative.Length > 180) continue;
                        if (Path.GetExtension(relative) is not (".cs" or ".tscn" or ".tres" or ".json" or ".gdshader" or ".godot" or ".csproj" or ".sln")) continue;
                        CheckedFullPath(root, relative);
                        tracked.Add(relative);
                    }
                }
            }
            foreach (var path in tracked.OrderBy(value => value, StringComparer.Ordinal))
            {
                var full = CheckedFullPath(root, path);
                if (!File.Exists(full))
                    continue;
                var content = await File.ReadAllTextAsync(full, cancellationToken).ConfigureAwait(false);
                if (content.Length > 60000)
                    throw new IOException($"Godot file '{path}' exceeds the project's per-file text storage budget.");
                var saved = await studio.ReadFileAsync(projectId, draftId, path, cancellationToken).ConfigureAwait(false);
                if (content == saved)
                    continue;
                if (receipts.TryGetValue(path, out var authorizedHash) && Hash(saved) != authorizedHash)
                    throw new IOException($"Both the draft and Godot changed '{path}' since the last export. Resolve the conflict manually in the editor.");
                if (path == "scene.json")
                {
                    var importedScene = JsonSerializer.Deserialize<GameSceneDocument>(content, jsonOptions)
                        ?? throw new InvalidDataException("Imported scene.json did not contain a world document.");
                    if (engine.Curate(importedScene).Any(item => item.BlocksAcceptance))
                        throw new InvalidDataException("Godot scene.json violates the semantic curation boundary; external scene changes were not imported.");
                }
                changed.Add((path, content));
            }
            foreach (var (path, content) in changed)
            {
                if (path == "scene.json")
                {
                    draft.Scene = JsonSerializer.Deserialize<GameSceneDocument>(content, jsonOptions)!;
                    draft = await studio.SaveDraftAsync(projectId, draft, true, cancellationToken).ConfigureAwait(false);
                }
                draft = await studio.SaveFileAsync(projectId, draftId, path, content, true, cancellationToken).ConfigureAwait(false);
                receipts[path] = Hash(content);
            }
            await SaveReceiptsAsync(projectId, draftId, receipts, cancellationToken).ConfigureAwait(false);
            return changed.Select(item => item.Path).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Importing externally edited Godot files failed for project {ProjectId}; the worktree remains open.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Performs require open as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    private async Task<GameStudioDraft> RequireOpenAsync(Guid projectId, Guid draftId, CancellationToken cancellationToken)
    {
        try
        {
            var draft = await studio.GetDraftAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (draft is null || draft.DraftId != draftId || draft.ClosedRevisionId is not null)
                throw new InvalidOperationException("Godot writes require the currently open project draft.");
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The Godot workspace open-draft guard rejected a write for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Resolves root as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ResolveRootAsync(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var details = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("Game project not found.");
            if (!details.Project.IsGameProject || string.IsNullOrWhiteSpace(details.Project.RootPath) || !Path.IsPathFullyQualified(details.Project.RootPath))
                throw new InvalidOperationException("Configure a fully qualified root path on the Game project before synchronizing Godot files.");
            var root = Path.GetFullPath(details.Project.RootPath);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException("The configured Game project root must exist before Godot export/import.");
            return root;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the user-configured Godot project root failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Performs checked full path as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the game godot workspace operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the game godot workspace operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CheckedFullPath(string root, string path)
    {
        try
        {
            var full = Path.GetFullPath(Path.Combine(root, path));
            if (platform.PathsEqual(root, full) || !platform.IsSameOrDescendantPath(root, full))
                throw new UnauthorizedAccessException("Godot export paths must stay inside the configured project root.");
            var current = root;
            foreach (var segment in Path.GetRelativePath(root, full).Split(Path.DirectorySeparatorChar))
            {
                current = Path.Combine(current, segment);
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new UnauthorizedAccessException("Godot export/import must not traverse symbolic links or directory junctions.");
            }
            return full;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "A Godot project file path escaped its configured project root.");
            throw;
        }
    }

    /// <summary>
    /// Reads receipts as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The dictionary string string produced by the operation.</returns>
    private async Task<Dictionary<string, string>> ReadReceiptsAsync(Guid projectId, Guid draftId, CancellationToken cancellationToken)
    {
        try
        {
            var artifacts = await architecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            var value = artifacts.FirstOrDefault(item => item.ArtifactKind == "GameStudioExportReceipt" && item.Name == draftId.ToString("N"))?.Value;
            return string.IsNullOrEmpty(value) ? new(StringComparer.Ordinal) : JsonSerializer.Deserialize<Dictionary<string, string>>(value, jsonOptions) ?? new(StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Godot export receipts failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Persists receipts as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="hashes">Hashes value supplied to the game godot workspace operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveReceiptsAsync(Guid projectId, Guid draftId, Dictionary<string, string> hashes, CancellationToken cancellationToken)
    {
        try
        {
            await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                ArtifactKind = "GameStudioExportReceipt", Name = draftId.ToString("N"),
                Value = JsonSerializer.Serialize(hashes, jsonOptions), DataType = "application/json", UserConfirmed = true,
                Description = "Previously exported hashes used to protect human edits in external Godot editors."
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving Godot export receipts failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Reads godot template as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string ReadGodotTemplate()
    {
        try
        {
            using var stream = typeof(GameGodotWorkspaceService).Assembly.GetManifestResourceStream("LocalGPT.GameTemplates.GodotSceneRuntime.cs.txt")
                ?? throw new InvalidOperationException("The embedded generic Godot C# runtime template is not available.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the maintained Godot C# source template failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether h as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="content">Content value supplied to the game godot workspace operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Hash(string content)
    {
        try
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Calculating an external source-content comparison failed; content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs require approval as part of the game godot workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    private void RequireApproval(bool userConfirmed)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Godot source generation and disk synchronization require explicit human confirmation.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Godot project mutation blocked by the explicit approval gate.");
            throw;
        }
    }
}
