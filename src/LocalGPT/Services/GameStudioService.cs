using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Maintains the live Godot working tree as editable Project artifacts; only explicit CloseRevision creates a durable commit.</summary>
/// <param name="projects">Local gpt project service dependency used by the game studio workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the game studio workflow to provide the corresponding application capability.</param>
/// <param name="engine">Engine value supplied to the game studio operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GameStudioService(
    ILocalGptProjectService projects,
    IProjectArchitectureService architecture,
    GameSceneEngine engine,
    ILogger<GameStudioService> logger) : IGameStudioService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="GameStudioService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Retrieves draft as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft?> GetDraftAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var details = await RequireGameProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            var artifact = details.Artifacts.FirstOrDefault(item => item.ArtifactKind == "GameStudioDraft" && item.Name == "Current Worktree");
            return artifact is null ? null : JsonSerializer.Deserialize<GameStudioDraft>(artifact.Value, jsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading game studio worktree failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Opens a mutable worktree from the last explicitly closed scene, without creating a Project revision.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft> OpenDraftAsync(Guid projectId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var details = await RequireGameProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            var previous = await GetDraftAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (previous is not null && previous.ClosedRevisionId is null)
                return previous;
            var draft = new GameStudioDraft
            {
                ProjectId = projectId,
                ParentRevisionId = previous?.ClosedRevisionId ?? details.Revisions.FirstOrDefault(item => item.IsCurrent)?.Id,
                BranchName = previous?.BranchName ?? "main",
                Scene = previous?.Scene ?? new GameSceneDocument(),
                FilePaths = previous?.FilePaths.ToList() ?? []
            };
            if (previous?.ClosedRevisionId is Guid closed)
            {
                foreach (var path in draft.FilePaths)
                {
                    var file = details.Artifacts.FirstOrDefault(item => item.ArtifactKind == "GameStudioRevisionFile" && item.Name == $"{closed:N}/{path}");
                    if (file is null)
                        throw new InvalidOperationException($"The completed revision is missing tracked Godot file '{path}'. Restore the immutable snapshot before opening the next draft.");
                    await SaveFileArtifactAsync(projectId, draft.DraftId, path, file.Value, cancellationToken).ConfigureAwait(false);
                }
            }
            await SaveDraftArtifactAsync(projectId, draft, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Opened mutable Game Studio worktree {DraftId} for project {ProjectId}.", draft.DraftId, projectId);
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Opening Game Studio draft failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Updates an existing draft's scene; semantic art violations cannot silently become approved game data.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draft">Draft value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft> SaveDraftAsync(Guid projectId, GameStudioDraft draft, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var current = await RequireOpenDraftAsync(projectId, draft.DraftId, cancellationToken).ConfigureAwait(false);
            var findings = engine.Curate(draft.Scene);
            if (findings.Any(item => item.BlocksAcceptance))
                throw new InvalidOperationException(string.Join(" ", findings.Where(item => item.BlocksAcceptance).Take(10).Select(item => item.Message)));
            current.Scene = draft.Scene;
            current.BranchName = string.IsNullOrWhiteSpace(draft.BranchName) ? "main" : draft.BranchName.Trim()[..Math.Min(draft.BranchName.Trim().Length, 160)];
            current.UpdatedAtUtc = DateTime.UtcNow;
            await SaveDraftArtifactAsync(projectId, current, cancellationToken).ConfigureAwait(false);
            return current;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving an open game-scene worktree failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Reads one editable Godot source file from the selected open worktree.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> ReadFileAsync(Guid projectId, Guid draftId, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var draft = await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            ValidatePath(path);
            if (!draft.FilePaths.Contains(path, StringComparer.Ordinal))
                return string.Empty;
            var artifacts = await architecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            return artifacts.FirstOrDefault(item => item.ArtifactKind == "GameStudioWorkingFile" && item.Name == $"{draftId:N}/{path}")?.Value ?? string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a Godot worktree file failed for project {ProjectId}; file content was omitted.", projectId);
            throw;
        }
    }

    /// <summary>Edits a Godot file in place in the OPEN draft; a file save never commits or replaces a completed revision.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft> SaveFileAsync(Guid projectId, Guid draftId, string path, string content, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            ValidatePath(path);
            if (content.Length > 60000)
                throw new ArgumentException("A single Godot text file may not exceed 60,000 characters.", nameof(content));
            var draft = await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            if (draft.FilePaths.Count >= 128 && !draft.FilePaths.Contains(path, StringComparer.Ordinal))
                throw new InvalidOperationException("A draft supports at most 128 tracked Godot source files.");
            await SaveFileArtifactAsync(projectId, draftId, path, content, cancellationToken).ConfigureAwait(false);
            if (!draft.FilePaths.Contains(path, StringComparer.Ordinal))
                draft.FilePaths.Add(path);
            draft.UpdatedAtUtc = DateTime.UtcNow;
            await SaveDraftArtifactAsync(projectId, draft, cancellationToken).ConfigureAwait(false);
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Updating a Godot draft file failed for project {ProjectId}; file content was omitted.", projectId);
            throw;
        }
    }

    /// <summary>Lists unaccepted AI artwork revisions for the current worktree.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<GameSceneProposal>> GetProposalsAsync(Guid projectId, Guid draftId, CancellationToken cancellationToken = default)
    {
        try
        {
            await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var artifacts = await architecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            return artifacts.Where(item => item.ArtifactKind == "GameStudioProposal" && item.Flags == draftId.ToString("N"))
                .Select(item => JsonSerializer.Deserialize<GameSceneProposal>(item.Value, jsonOptions))
                .OfType<GameSceneProposal>().ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing AI scene proposals failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Stores an AI's just-in-time visual candidate separately from the playable world; acceptance is human-controlled.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposal">Proposal value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene proposal produced by the operation.</returns>
    public async Task<GameSceneProposal> ProposeSceneAsync(Guid projectId, Guid draftId, GameSceneProposal proposal, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            proposal.Id = Guid.NewGuid();
            proposal.DraftId = draftId;
            var value = JsonSerializer.Serialize(proposal, jsonOptions);
            if (value.Length > 60000)
                throw new ArgumentException("AI scene proposal exceeds the project artifact storage budget.");
            await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                ArtifactKind = "GameStudioProposal", Name = proposal.Id.ToString("N"), Flags = draftId.ToString("N"),
                Value = value, DataType = "application/vnd.localgpt.scene-proposal+json", UserConfirmed = true,
                Description = "Isolated AI-generated scene candidate. Requires independent curation and human acceptance before gameplay."
            }, cancellationToken).ConfigureAwait(false);
            return proposal;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving a proposed AI game scene failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Merges a small upsert-only artistic patch into an isolated candidate, leaving the current playable draft untouched.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="patch">Patch value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="artist">Artist value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene proposal produced by the operation.</returns>
    public async Task<GameSceneProposal> ProposePatchAsync(Guid projectId, Guid draftId, GameScenePatch patch, string artist, string description, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var draft = await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var scene = JsonSerializer.Deserialize<GameSceneDocument>(JsonSerializer.Serialize(draft.Scene, jsonOptions), jsonOptions)
                ?? throw new InvalidOperationException("The source scene cannot be cloned for a reviewable visual patch.");
            foreach (var tile in patch.Tiles)
            {
                var index = scene.Tiles.FindIndex(item => item.Id == tile.Id);
                if (index >= 0) scene.Tiles[index] = tile; else scene.Tiles.Add(tile);
            }
            foreach (var sprite in patch.Sprites)
            {
                var index = scene.Sprites.FindIndex(item => item.Id == sprite.Id);
                if (index >= 0) scene.Sprites[index] = sprite; else scene.Sprites.Add(sprite);
            }
            foreach (var actor in patch.Actors)
            {
                var index = scene.Actors.FindIndex(item => item.Id == actor.Id);
                if (index >= 0) scene.Actors[index] = actor; else scene.Actors.Add(actor);
            }
            foreach (var layer in patch.Layers)
            {
                var index = scene.Layers.FindIndex(item => item.Id == layer.Id);
                if (index >= 0) scene.Layers[index] = layer; else scene.Layers.Add(layer);
            }
            foreach (var animation in patch.Animations)
            {
                var index = scene.Animations.FindIndex(item => item.TargetId == animation.TargetId);
                if (index >= 0) scene.Animations[index] = animation; else scene.Animations.Add(animation);
            }
            foreach (var hud in patch.Hud)
            {
                var index = scene.Hud.FindIndex(item => item.X == hud.X && item.Y == hud.Y);
                if (index >= 0) scene.Hud[index] = hud; else scene.Hud.Add(hud);
            }
            foreach (var key in patch.PaletteTimeline)
            {
                var index = scene.PaletteTimeline.FindIndex(item => item.AtWorldSecond == key.AtWorldSecond);
                if (index >= 0) scene.PaletteTimeline[index] = key; else scene.PaletteTimeline.Add(key);
            }
            return await ProposeSceneAsync(projectId, draftId, new GameSceneProposal
            {
                Artist = artist, Description = description, Scene = scene
            }, true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving an AI-generated small scene patch as an isolated project proposal failed for {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Exposes one exact candidate to an independent reviewing model without requiring the entire scene-proposal history.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposalId">Identifier of the proposal to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene proposal produced by the operation.</returns>
    public async Task<GameSceneProposal?> GetProposalAsync(Guid projectId, Guid draftId, Guid proposalId, CancellationToken cancellationToken = default)
    {
        try
        {
            return (await GetProposalsAsync(projectId, draftId, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Id == proposalId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a bounded Game Studio proposal failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Stores a distinct curator's explicit acceptance/rejection with independently recomputed semantic diagnostics.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="review">Review value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene review produced by the operation.</returns>
    public async Task<GameSceneReview> ReviewProposalAsync(Guid projectId, Guid draftId, GameSceneReview review, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var proposal = (await GetProposalsAsync(projectId, draftId, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Id == review.ProposalId)
                ?? throw new KeyNotFoundException("The reviewed scene candidate is not part of this worktree.");
            if (string.IsNullOrWhiteSpace(review.CuratorModel) || string.Equals(review.CuratorModel, proposal.Artist, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A scene candidate must be reviewed by a separate curator model or user.");
            review.DraftId = draftId;
            review.Findings = engine.Curate(proposal.Scene).ToList();
            if (review.Findings.Any(item => item.BlocksAcceptance))
                review.Approved = false;
            await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                ArtifactKind = "GameStudioReview", Name = $"{draftId:N}/{proposal.Id:N}", Flags = draftId.ToString("N"),
                Value = JsonSerializer.Serialize(review, jsonOptions), DataType = "application/vnd.localgpt.scene-review+json",
                UserConfirmed = true, Description = "Independent AI/human curator decision with service-rechecked semantic scene diagnostics."
            }, cancellationToken).ConfigureAwait(false);
            return review;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Curating a scene proposal failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Reads the persisted curator decision for a proposal and its exact worktree identity.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposalId">Identifier of the proposal to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game scene review produced by the operation.</returns>
    public async Task<GameSceneReview?> GetReviewAsync(Guid projectId, Guid draftId, Guid proposalId, CancellationToken cancellationToken = default)
    {
        try
        {
            await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            var artifacts = await architecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            var artifact = artifacts.FirstOrDefault(item => item.ArtifactKind == "GameStudioReview" && item.Name == $"{draftId:N}/{proposalId:N}");
            return artifact is null ? null : JsonSerializer.Deserialize<GameSceneReview>(artifact.Value, jsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading independent curator decisions failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Applies only a semantically curated proposal, leaving all previously saved project revisions untouched.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="proposalId">Identifier of the proposal to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    public async Task<GameStudioDraft> AcceptProposalAsync(Guid projectId, Guid draftId, Guid proposalId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var proposal = (await GetProposalsAsync(projectId, draftId, cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Id == proposalId)
                ?? throw new KeyNotFoundException("The selected proposal does not belong to this open draft.");
            var review = await GetReviewAsync(projectId, draftId, proposalId, cancellationToken).ConfigureAwait(false);
            if (review is null || !review.Approved || string.IsNullOrWhiteSpace(review.CuratorModel)
                || string.Equals(review.CuratorModel, proposal.Artist, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("An independent curator must explicitly approve this exact proposal before the human can accept it.");
            var current = await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            current.Scene = proposal.Scene;
            return await SaveDraftAsync(projectId, current, userConfirmed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Accepting an AI scene proposal failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Commit: freezes the scene plus every editable file as revision-scoped artifacts before closing this worktree.</summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="revisionName">Revision name value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local GPT project revision produced by the operation.</returns>
    public async Task<LocalGptProjectRevision> CloseRevisionAsync(Guid projectId, Guid draftId, string revisionName, string summary, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            RequireApproval(userConfirmed);
            var draft = await RequireOpenDraftAsync(projectId, draftId, cancellationToken).ConfigureAwait(false);
            if (engine.Curate(draft.Scene).Any(item => item.BlocksAcceptance))
                throw new InvalidOperationException("The scene has unaccepted semantic errors and cannot be committed.");
            var artifacts = await architecture.GetArtifactsAsync(projectId, cancellationToken).ConfigureAwait(false);
            foreach (var path in draft.FilePaths)
                if (!artifacts.Any(item => item.ArtifactKind == "GameStudioWorkingFile" && item.Name == $"{draftId:N}/{path}"))
                    throw new InvalidOperationException($"Tracked Godot file '{path}' is missing; cannot finalize a partial revision.");
            var revision = await architecture.SaveRevisionAsync(projectId, new SaveProjectRevisionRequest
            {
                ParentRevisionId = draft.ParentRevisionId,
                BranchName = draft.BranchName,
                RevisionName = revisionName,
                Summary = summary,
                ProjectStructureJson = JsonSerializer.Serialize(new { GameStudioDraftId = draft.DraftId, Scene = draft.Scene, GodotFiles = draft.FilePaths }, jsonOptions),
                IsCurrent = true,
                UserConfirmed = true
            }, cancellationToken).ConfigureAwait(false);
            foreach (var path in draft.FilePaths)
            {
                var working = artifacts.Single(item => item.ArtifactKind == "GameStudioWorkingFile" && item.Name == $"{draftId:N}/{path}");
                await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
                {
                    RevisionId = revision.Id, ArtifactKind = "GameStudioRevisionFile", Name = $"{revision.Id:N}/{path}",
                    Value = working.Value, DataType = "text/plain", UserConfirmed = true,
                    Description = "Immutable file snapshot from an explicitly completed Game Studio revision."
                }, cancellationToken).ConfigureAwait(false);
            }
            draft.ClosedRevisionId = revision.Id;
            draft.UpdatedAtUtc = DateTime.UtcNow;
            await SaveDraftArtifactAsync(projectId, draft, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Closed Game Studio revision {RevisionId} with {FileCount} tracked Godot files for project {ProjectId}.", revision.Id, draft.FilePaths.Count, projectId);
            return revision;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Closing a Godot project revision failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Performs require game project as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local GPT project details produced by the operation.</returns>
    private async Task<LocalGptProjectDetails> RequireGameProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var project = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("Project was not found.");
            if (!project.Project.IsGameProject)
                throw new InvalidOperationException("Game Studio is available only to Game projects.");
            return project;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Game project ownership validation failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Performs require open draft as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The game studio draft produced by the operation.</returns>
    private async Task<GameStudioDraft> RequireOpenDraftAsync(Guid projectId, Guid draftId, CancellationToken cancellationToken)
    {
        try
        {
            var draft = await GetDraftAsync(projectId, cancellationToken).ConfigureAwait(false);
            if (draft is null || draft.DraftId != draftId || draft.ClosedRevisionId is not null)
                throw new InvalidOperationException("The selected Godot draft is closed or stale. Open the next worktree before editing.");
            return draft;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Game Studio open-draft guard rejected a modification for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Persists draft artifact as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draft">Draft value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveDraftArtifactAsync(Guid projectId, GameStudioDraft draft, CancellationToken cancellationToken)
    {
        try
        {
            var value = JsonSerializer.Serialize(draft, jsonOptions);
            if (value.Length > 60000)
                throw new InvalidOperationException("The authored scene exceeds the project artifact storage limit. Split scene content into smaller chunks.");
            await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                ArtifactKind = "GameStudioDraft", Name = "Current Worktree", Value = value,
                DataType = "application/vnd.localgpt.game-studio+json", UserConfirmed = true,
                Description = "Mutable Godot/2D/ASCII worktree. Closing creates a new immutable Project revision."
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Persisting the game authoring draft failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Persists file artifact as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="draftId">Identifier of the draft to use for this operation.</param>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="content">Content value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveFileArtifactAsync(Guid projectId, Guid draftId, string path, string content, CancellationToken cancellationToken)
    {
        try
        {
            await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                ArtifactKind = "GameStudioWorkingFile", Name = $"{draftId:N}/{path}", Value = content,
                DataType = "text/plain", UserConfirmed = true,
                Description = "Editable Godot source file in the open draft; saving it does not create a revision."
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Persisting a working Godot file failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>
    /// Validates path as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    private void ValidatePath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length > 180 || path.StartsWith('/') || path.StartsWith('\\')
                || path.Contains("..", StringComparison.Ordinal) || path.Contains(':') || path.Contains('\\')
                || path.Split('/').Any(segment => segment.Length == 0 || segment.StartsWith('.')))
                throw new ArgumentException("Use a project-relative Godot source file path without parent traversal or hidden/cache directories.", nameof(path));
            var extension = Path.GetExtension(path);
            if (extension is not (".cs" or ".tscn" or ".tres" or ".json" or ".gdshader" or ".godot" or ".csproj" or ".sln"))
                throw new ArgumentException("Only editable Godot project/source/scene/resource file types are accepted.", nameof(path));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Godot project-relative file path validation rejected a file.");
            throw;
        }
    }

    /// <summary>
    /// Performs require approval as part of the game studio service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    private void RequireApproval(bool userConfirmed)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Editing or completing the Game Studio worktree requires the existing explicit user approval gate.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Game Studio write was blocked by the approval guard.");
            throw;
        }
    }
}
