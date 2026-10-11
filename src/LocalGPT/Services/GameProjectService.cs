using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Builds project-owned game definitions from durable LocalGPT project metadata and hands only the compiled definition
/// to GameDirector. This keeps authoring/build policy in the project layer while the runtime remains project-agnostic.
/// </summary>
/// <param name="projects">Project service used to read durable project identity, requirements and version metadata.</param>
/// <param name="dbContextFactory">Database-context factory used to persist the project-owned game authoring profile.</param>
/// <param name="architecture">Project architecture service used to persist the compiled game definition as a reviewed project artifact.</param>
/// <param name="games">Shared GameDirector session service that consumes compiled definitions.</param>
/// <param name="engine">Scene curator for approved, revision-specific user-authored games.</param>
/// <param name="logger">Logger used to record bounded operational diagnostics.</param>
public sealed class GameProjectService(
    ILocalGptProjectService projects,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IProjectArchitectureService architecture,
    ICouncilGameSessionService games,
    GameSceneEngine engine,
    ILogger<GameProjectService> logger) : IGameProjectService
{
    /// <summary>Reads the editable authoring profile owned by one LocalGPT Game project.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted profile, or <c>null</c> when no profile has been authored yet.</returns>
    public async Task<LocalGptGameProjectProfile?> GetProfileAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var details = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            if (!details.Project.IsGameProject)
                return null;

            return details.GameProfile;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading a LocalGPT game-project authoring profile was cancelled for project {ProjectId}.", projectId);
            else
                logger.LogError(exception, "Reading a LocalGPT game-project authoring profile failed for project {ProjectId}; profile content was omitted.", projectId);
            throw;
        }
    }

    /// <summary>Creates or updates the durable authoring profile owned by one LocalGPT Game project.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="request">Authoring settings approved by the caller.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted project-owned authoring profile.</returns>
    public async Task<LocalGptGameProjectProfile> SaveProfileAsync(
        Guid projectId,
        SaveGameProjectProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Saving a game-project authoring profile requires explicit user confirmation.");

            var details = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            if (!details.Project.IsGameProject)
                throw new InvalidOperationException("Only projects whose project type is Game can own a LocalGPT game authoring profile.");
            if (!Enum.IsDefined(typeof(CouncilGameRuntimeProfile), request.RuntimeProfile))
                throw new ArgumentException("The selected game runtime profile is not supported by this LocalGPT version.", nameof(request));
            if (!Enum.IsDefined(typeof(CouncilAsciiColorMode), request.AsciiColorMode))
                throw new ArgumentException("The selected ASCII color mode is not supported by this LocalGPT version.", nameof(request));
            if (!Enum.IsDefined(typeof(CouncilGameControlMode), request.DefaultControlMode))
                throw new ArgumentException("The selected default game control mode is invalid.", nameof(request));
            if (!Enum.IsDefined(typeof(CouncilGameDirectorMode), request.DirectorMode))
                throw new ArgumentException("The selected GameDirector mode is invalid.", nameof(request));

            var rawGameKey = string.IsNullOrWhiteSpace(request.GameKey) ? details.Project.Name : request.GameKey;
            var normalizedGameKey = rawGameKey.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            normalizedGameKey = new string(normalizedGameKey.Where(character => char.IsLetterOrDigit(character) || character == '-').ToArray()).Trim('-');
            if (string.IsNullOrWhiteSpace(normalizedGameKey))
                throw new ArgumentException("The game key must contain at least one letter or number.", nameof(request));
            if (normalizedGameKey.Length > 160)
                normalizedGameKey = normalizedGameKey[..160];

            var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? details.Project.Name.Trim() : request.DisplayName.Trim();
            if (displayName.Length > 240)
                displayName = displayName[..240];
            var teamKey = string.IsNullOrWhiteSpace(request.DefaultTeamKey) ? "game-project-playtest" : request.DefaultTeamKey.Trim();
            if (teamKey.Length > 160)
                teamKey = teamKey[..160];
            var scenario = string.Join(' ', (request.ScenarioPrompt ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (scenario.Length > 240)
                scenario = scenario[..240];
            var directorModel = (request.GameDirectorModelName ?? string.Empty).Trim();
            if (directorModel.Length > 240)
                directorModel = directorModel[..240];
            var defaultForegroundColor = request.AsciiColorMode == CouncilAsciiColorMode.Ansi16
                ? (request.DefaultForegroundColor is >= 0 and <= 15 ? request.DefaultForegroundColor : 10)
                : (request.DefaultForegroundColor is >= 0 and <= 255 ? request.DefaultForegroundColor : 46);
            var defaultBackgroundColor = request.AsciiColorMode == CouncilAsciiColorMode.Ansi16
                ? (request.DefaultBackgroundColor is >= 0 and <= 15 ? request.DefaultBackgroundColor : 0)
                : Math.Clamp(request.DefaultBackgroundColor, 0, 255);

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var profile = await db.LocalGptGameProjectProfiles
                .SingleOrDefaultAsync(item => item.ProjectId == projectId, cancellationToken)
                .ConfigureAwait(false);
            if (profile is null)
            {
                profile = new LocalGptGameProjectProfile
                {
                    ProjectId = projectId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.LocalGptGameProjectProfiles.Add(profile);
            }

            profile.GameKey = normalizedGameKey;
            profile.DisplayName = displayName;
            profile.RuntimeProfile = request.RuntimeProfile;
            profile.DefaultTeamKey = teamKey;
            profile.DefaultControlMode = request.DefaultControlMode;
            profile.DirectorMode = request.DirectorMode;
            profile.GameDirectorModelName = directorModel;
            profile.CreatureDirectorCount = Math.Clamp(request.CreatureDirectorCount, 1, 8);
            profile.AutoplayDelayMilliseconds = Math.Clamp(request.AutoplayDelayMilliseconds, 250, 10000);
            profile.FrameWidth = Math.Clamp(request.FrameWidth, 20, 240);
            profile.FrameHeight = Math.Clamp(request.FrameHeight, 8, 100);
            profile.AsciiColorMode = request.AsciiColorMode;
            profile.DefaultForegroundColor = defaultForegroundColor;
            profile.DefaultBackgroundColor = defaultBackgroundColor;
            profile.MapSeed = request.MapSeed is > 0 ? request.MapSeed.Value : 0;
            profile.ScenarioPrompt = scenario;
            profile.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved LocalGPT game-project authoring profile {ProfileId} for project {ProjectId}; profile content was omitted from logs.", profile.Id, projectId);
            return profile;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving a LocalGPT game-project authoring profile was cancelled for project {ProjectId}.", projectId);
            else
                logger.LogError(exception, "Saving a LocalGPT game-project authoring profile failed for project {ProjectId}; profile content was omitted.", projectId);
            throw;
        }
    }

    /// <summary>Compiles an approved, persisted Game-project authoring profile into the durable runtime-definition artifact consumed below the Project layer.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="request">Approval to compile the currently persisted Game-project profile and approved requirement baseline.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The built definition and its project artifact identity.</returns>
    public async Task<ProjectGameBuildResult> BuildAsync(
        Guid projectId,
        BuildProjectGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Building a project game requires explicit user confirmation before authoring data and the runtime definition are persisted.");

            var details = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            if (!details.Project.IsGameProject)
                throw new InvalidOperationException("Only projects whose project type is Game can be compiled by the LocalGPT game-project build service.");
            var profile = details.GameProfile
                ?? throw new InvalidOperationException("Save the Game project design before building it. Builds compile persisted Project-system authoring state and never mutate it implicitly.");
            var completedRevision = details.Revisions.FirstOrDefault(item => item.IsCurrent);
            var currentRevisionId = completedRevision?.Id;
            var committedScene = completedRevision is not null && completedRevision.IsUserApproved
                ? ReadPlayableRevisionScene(completedRevision) : null;
            var requirementIds = details.Requirements
                .Where(item => item.IsUserApproved)
                .OrderBy(item => item.CreatedAtUtc)
                .Select(item => item.Id)
                .ToList();

            var definition = new ProjectGameDefinition
            {
                ProjectId = projectId,
                Scene = committedScene,
                ProjectVersion = details.Project.CurrentVersion,
                AuthoringProfileId = profile.Id,
                SourceRevisionId = currentRevisionId,
                SourceRequirementIds = requirementIds,
                GameKey = profile.GameKey,
                DisplayName = profile.DisplayName,
                RuntimeProfile = profile.RuntimeProfile,
                DefaultTeamKey = profile.DefaultTeamKey,
                DefaultControlMode = profile.DefaultControlMode,
                DirectorMode = profile.DirectorMode,
                GameDirectorModelName = profile.GameDirectorModelName,
                CreatureDirectorCount = profile.CreatureDirectorCount,
                AutoplayDelayMilliseconds = profile.AutoplayDelayMilliseconds,
                FrameWidth = profile.FrameWidth,
                FrameHeight = profile.FrameHeight,
                AsciiColorMode = profile.AsciiColorMode,
                DefaultForegroundColor = profile.DefaultForegroundColor,
                DefaultBackgroundColor = profile.DefaultBackgroundColor,
                MapSeed = profile.MapSeed,
                ScenarioPrompt = profile.ScenarioPrompt,
                BuiltAtUtc = DateTime.UtcNow
            };

            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
            var serializedDefinition = JsonSerializer.Serialize(definition, jsonOptions);
            if (serializedDefinition.Length > 60000)
                throw new InvalidOperationException("The committed Game Studio scene exceeds the project game build artifact budget. Split the scene into smaller curated world chunks before building.");
            var artifact = await architecture.SaveArtifactAsync(projectId, new SaveProjectArtifactRequest
            {
                RevisionId = currentRevisionId,
                ArtifactKind = "GameBuild",
                Name = "Runtime Definition",
                Value = serializedDefinition,
                DataType = "application/vnd.localgpt.game+json",
                Flags = $"{definition.RuntimeProfile};Built;Requirements={requirementIds.Count}",
                Description = "Compiled LocalGPT game definition produced from the persisted Game-project authoring profile and current project requirement baseline. GameDirector consumes this artifact; it does not own project authoring policy.",
                IsSensitive = false,
                UserConfirmed = true
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Built LocalGPT game definition {GameKey} for project {ProjectId} as artifact {ArtifactId} from profile {ProfileId} and {RequirementCount} project requirement(s); definition content was omitted from logs.",
                definition.GameKey,
                projectId,
                artifact.Id,
                profile.Id,
                requirementIds.Count);
            return new ProjectGameBuildResult
            {
                ArtifactId = artifact.Id,
                RevisionId = artifact.RevisionId,
                Definition = definition
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Building a LocalGPT game project was cancelled for project {ProjectId}.", projectId);
            else
                logger.LogError(exception, "Building a LocalGPT game project failed for project {ProjectId}; definition content was omitted.", projectId);
            throw;
        }
    }

    /// <summary>Reads the latest approved built definition for one game project.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The latest build, or <c>null</c> when the project has not yet produced one.</returns>
    public async Task<ProjectGameBuildResult?> GetBuildAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var details = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            if (!details.Project.IsGameProject)
                return null;

            var artifact = details.Artifacts
                .Where(item => item.IsUserApproved
                    && string.Equals(item.ArtifactKind, "GameBuild", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Name, "Runtime Definition", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.DataType, "application/vnd.localgpt.game+json", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.UpdatedAtUtc)
                .FirstOrDefault();
            if (artifact is null || string.IsNullOrWhiteSpace(artifact.Value))
                return null;

            var definition = JsonSerializer.Deserialize<ProjectGameDefinition>(
                artifact.Value,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("The project game build artifact did not contain a readable runtime definition.");
            if (definition.ProjectId != projectId)
                throw new InvalidOperationException("The project game build artifact points at a different project identity and cannot be launched.");
            if (!Enum.IsDefined(typeof(CouncilGameRuntimeProfile), definition.RuntimeProfile))
                throw new InvalidOperationException("The project game build artifact targets an unsupported runtime profile.");
            if (!Enum.IsDefined(typeof(CouncilAsciiColorMode), definition.AsciiColorMode))
                throw new InvalidOperationException("The project game build artifact targets an unsupported ASCII color mode.");
            definition.DefaultForegroundColor = definition.AsciiColorMode == CouncilAsciiColorMode.Ansi16
                ? (definition.DefaultForegroundColor is >= 0 and <= 15 ? definition.DefaultForegroundColor : 10)
                : (definition.DefaultForegroundColor is >= 0 and <= 255 ? definition.DefaultForegroundColor : 46);
            definition.DefaultBackgroundColor = definition.AsciiColorMode == CouncilAsciiColorMode.Ansi16
                ? (definition.DefaultBackgroundColor is >= 0 and <= 15 ? definition.DefaultBackgroundColor : 0)
                : Math.Clamp(definition.DefaultBackgroundColor, 0, 255);
            if (!Enum.IsDefined(typeof(CouncilGameControlMode), definition.DefaultControlMode))
                throw new InvalidOperationException("The project game build artifact contains an invalid default control mode.");
            if (!Enum.IsDefined(typeof(CouncilGameDirectorMode), definition.DirectorMode))
                throw new InvalidOperationException("The project game build artifact contains an invalid GameDirector mode.");

            logger.LogDebug("Loaded project game build artifact {ArtifactId} for project {ProjectId}.", artifact.Id, projectId);
            return new ProjectGameBuildResult
            {
                ArtifactId = artifact.Id,
                RevisionId = artifact.RevisionId,
                Definition = definition
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading a LocalGPT game-project build was cancelled for project {ProjectId}.", projectId);
            else
                logger.LogError(exception, "Reading a LocalGPT game-project build failed for project {ProjectId}; definition content was omitted.", projectId);
            throw;
        }
    }

    /// <summary>Lists only durable, explicitly approved project revisions with playable, curated scene content.</summary>
    /// <param name="cancellationToken">Cancellation token for project and revision reads.</param>
    /// <returns>Stable project/revision pairs for the shared chat console.</returns>
    public async Task<IReadOnlyList<ProjectGameRevisionChoice>> ListPlayableRevisionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new List<ProjectGameRevisionChoice>();
            var available = await projects.GetProjectsAsync(includeArchived: false, cancellationToken).ConfigureAwait(false);
            foreach (var item in available.Where(project => project.IsGameProject))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var details = await projects.GetProjectAsync(item.Id, cancellationToken).ConfigureAwait(false);
                if (details is null || details.Project.IsArchived)
                    continue;
                foreach (var revision in details.Revisions.Where(value => value.IsUserApproved)
                             .OrderByDescending(value => value.IsCurrent).ThenByDescending(value => value.CreatedAtUtc))
                {
                    if (ReadPlayableRevisionScene(revision) is not null)
                        result.Add(new ProjectGameRevisionChoice(item.Id, revision.Id, item.Name,
                            revision.RevisionName, details.Project.CurrentVersion, revision.IsCurrent));
                }
            }
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Listing approved Game Studio revisions was cancelled.");
            else
                logger.LogError(exception, "Listing approved Game Studio revisions failed; scene contents were not logged.");
            throw;
        }
    }

    /// <summary>Loads and starts one immutable, approved Game Studio revision, even when it is not the current revision.</summary>
    /// <param name="projectId">Owning project identifier.</param>
    /// <param name="revisionId">Explicitly selected revision identifier.</param>
    /// <param name="request">Current chat and Council ownership context.</param>
    /// <param name="cancellationToken">Cancellation token for the launch operation.</param>
    /// <returns>The new authoritative, scene-backed GameDirector session.</returns>
    public async Task<CouncilGameSessionSnapshot> LaunchRevisionAsync(Guid projectId, Guid revisionId,
        LaunchProjectGameRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var details = await projects.GetProjectAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            if (!details.Project.IsGameProject || details.Project.IsArchived)
                throw new InvalidOperationException("Only active Game projects can be launched in Chat.");
            var revision = details.Revisions.SingleOrDefault(value => value.Id == revisionId)
                ?? throw new KeyNotFoundException($"Revision {revisionId} was not found in project {projectId}.");
            if (!revision.IsUserApproved)
                throw new InvalidOperationException("Only explicitly completed and approved Game Studio revisions can be played.");
            var scene = ReadPlayableRevisionScene(revision)
                ?? throw new InvalidOperationException("This revision has no accepted, playable scene. Finish and curate its game artwork before playing.");
            var profile = details.GameProfile;
            var definition = new ProjectGameDefinition
            {
                ProjectId = projectId,
                ProjectVersion = details.Project.CurrentVersion,
                SourceRevisionId = revision.Id,
                AuthoringProfileId = profile?.Id ?? Guid.Empty,
                Scene = scene,
                GameKey = profile?.GameKey ?? $"project-{projectId:N}",
                DisplayName = profile?.DisplayName ?? details.Project.Name,
                RuntimeProfile = profile?.RuntimeProfile ?? CouncilGameRuntimeProfile.Corridor,
                DefaultTeamKey = profile?.DefaultTeamKey ?? "game-project-playtest",
                DefaultControlMode = profile?.DefaultControlMode ?? CouncilGameControlMode.Shared,
                DirectorMode = profile?.DirectorMode ?? CouncilGameDirectorMode.Deterministic,
                GameDirectorModelName = profile?.GameDirectorModelName ?? string.Empty,
                CreatureDirectorCount = profile?.CreatureDirectorCount ?? 2,
                AutoplayDelayMilliseconds = profile?.AutoplayDelayMilliseconds ?? 1000,
                FrameWidth = scene.ViewWidth,
                FrameHeight = scene.ViewHeight,
                AsciiColorMode = profile?.AsciiColorMode ?? CouncilAsciiColorMode.TerminalDefault,
                DefaultForegroundColor = profile?.DefaultForegroundColor ?? 46,
                DefaultBackgroundColor = profile?.DefaultBackgroundColor ?? 0,
                MapSeed = profile?.MapSeed ?? 0,
                ScenarioPrompt = profile?.ScenarioPrompt ?? string.Empty,
                BuiltAtUtc = DateTime.UtcNow
            };
            var snapshot = await games.StartAsync(new StartCouncilGameRequest
            {
                ProjectId = projectId,
                Definition = definition,
                GameKey = definition.GameKey,
                TeamKey = definition.DefaultTeamKey,
                ConversationId = request.ConversationId,
                CouncilRunId = request.CouncilRunId,
                ControlMode = definition.DefaultControlMode,
                AutoplayEnabled = definition.DefaultControlMode == CouncilGameControlMode.Ai,
                AutoplayDelayMilliseconds = definition.AutoplayDelayMilliseconds,
                DirectorMode = definition.DirectorMode,
                GameDirectorModelName = definition.GameDirectorModelName,
                CreatureDirectorCount = definition.CreatureDirectorCount,
                FrameWidth = definition.FrameWidth,
                FrameHeight = definition.FrameHeight,
                ScenarioPrompt = definition.ScenarioPrompt,
                StartedBy = string.IsNullOrWhiteSpace(request.StartedBy) ? "Human User" : request.StartedBy.Trim()
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Launched approved game revision {RevisionId} from project {ProjectId} in session {SessionId}.",
                revisionId, projectId, snapshot.Id);
            return snapshot;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Launching Game Studio revision {RevisionId} was cancelled.", revisionId);
            else
                logger.LogError(exception, "Launching Game Studio revision {RevisionId} from project {ProjectId} failed.", revisionId, projectId);
            throw;
        }
    }

    /// <summary>Reads exactly the scene committed in a revision's ProjectStructure business-object field.</summary>
    /// <param name="revision">Persisted immutable project revision.</param>
    /// <returns>The accepted scene or <c>null</c> when no playable scene was committed.</returns>
    private GameSceneDocument? ReadPlayableRevisionScene(LocalGptProjectRevision revision)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(revision.ProjectStructureJson))
                return null;
            using var document = JsonDocument.Parse(revision.ProjectStructureJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("scene", out var sceneElement))
                return null;
            var scene = sceneElement.Deserialize<GameSceneDocument>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (scene is null || (scene.Tiles.Count == 0 && scene.Actors.Count == 0 && scene.Layers.Count == 0 && scene.Hud.Count == 0))
                return null;
            var findings = engine.Curate(scene);
            if (findings.Any(value => value.BlocksAcceptance))
            {
                logger.LogWarning("Approved game revision {RevisionId} contains {FindingCount} blocking scene findings; it is not playable.",
                    revision.Id, findings.Count(value => value.BlocksAcceptance));
                return null;
            }
            return scene;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or NullReferenceException)
        {
            logger.LogWarning(exception, "Game revision {RevisionId} has unreadable scene data and will not be launched.", revision.Id);
            return null;
        }
    }

    /// <summary>Launches the latest built definition for one project through GameDirector.</summary>
    /// <param name="projectId">Identifier of the owning project.</param>
    /// <param name="request">Launch-time ownership context.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The authoritative runtime snapshot.</returns>
    public async Task<CouncilGameSessionSnapshot> LaunchAsync(
        Guid projectId,
        LaunchProjectGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var build = await GetBuildAsync(projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Build the game project before launching it. The runtime only accepts a validated project build definition.");
            var definition = build.Definition;
            var snapshot = await games.StartAsync(new StartCouncilGameRequest
            {
                ProjectId = projectId,
                Definition = definition,
                GameKey = definition.GameKey,
                TeamKey = definition.DefaultTeamKey,
                ConversationId = request.ConversationId,
                CouncilRunId = request.CouncilRunId,
                ControlMode = definition.DefaultControlMode,
                AutoplayEnabled = definition.DefaultControlMode == CouncilGameControlMode.Ai,
                AutoplayDelayMilliseconds = definition.AutoplayDelayMilliseconds,
                DirectorMode = definition.DirectorMode,
                GameDirectorModelName = definition.GameDirectorModelName,
                CreatureDirectorCount = definition.CreatureDirectorCount,
                FrameWidth = definition.FrameWidth,
                FrameHeight = definition.FrameHeight,
                MapSeed = definition.MapSeed > 0 ? definition.MapSeed : null,
                ScenarioPrompt = definition.ScenarioPrompt,
                StartedBy = string.IsNullOrWhiteSpace(request.StartedBy) ? "Human User" : request.StartedBy.Trim()
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Launched built game {GameKey} from project {ProjectId} as session {GameSessionId}.",
                definition.GameKey,
                projectId,
                snapshot.Id);
            return snapshot;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Launching a LocalGPT game project was cancelled for project {ProjectId}.", projectId);
            else
                logger.LogError(exception, "Launching a LocalGPT game project failed for project {ProjectId}.", projectId);
            throw;
        }
    }

    /// <summary>Resolves a human-entered game-project selector and launches its latest build.</summary>
    /// <param name="selector">Project identifier, identifier prefix, or project name.</param>
    /// <param name="request">Launch-time ownership context.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The authoritative runtime snapshot.</returns>
    public async Task<CouncilGameSessionSnapshot> LaunchBySelectorAsync(
        string selector,
        LaunchProjectGameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var normalized = (selector ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("A game-project identifier or name is required.", nameof(selector));

            var available = await projects.GetProjectsAsync(includeArchived: false, cancellationToken).ConfigureAwait(false);
            var gameProjects = available.Where(item => item.IsGameProject).ToList();
            LocalGptProjectSummary? selected = null;
            if (Guid.TryParse(normalized, out var id))
                selected = gameProjects.FirstOrDefault(item => item.Id == id);
            if (selected is null)
            {
                var prefixMatches = gameProjects
                    .Where(item => item.Id.ToString("N").StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();
                if (prefixMatches.Count == 1)
                    selected = prefixMatches[0];
            }
            if (selected is null)
            {
                var nameMatches = gameProjects
                    .Where(item => string.Equals(item.Name, normalized, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToList();
                if (nameMatches.Count == 1)
                    selected = nameMatches[0];
            }
            if (selected is null)
                throw new KeyNotFoundException("No unique active Game project matched that selector.");

            return await LaunchAsync(selected.Id, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Resolving a LocalGPT game project selector was cancelled.");
            else
                logger.LogError(exception, "Resolving or launching a LocalGPT game project selector failed; selector content was omitted.");
            throw;
        }
    }
}
