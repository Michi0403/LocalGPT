using System.Text.Json;
using LocalGPT.BusinessObjects;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the game studio Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
/// </summary>
public partial class GameStudio
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="GameStudio"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    /// <summary>All supported rendering adapters, without coupling a Council team to a built-in game identity.</summary>
    /// <value>Available project scene presentation styles.</value>
    private readonly IReadOnlyList<GameSceneArtworkMode> ArtworkModes = Enum.GetValues<GameSceneArtworkMode>();
    /// <summary>Unsaved sample input for exercising project-authored regex transitions in the preview.</summary>
    /// <value>The transient test text.</value>
    private string RegexSceneInput { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the game projects collection maintained or exposed by this game studio instance for downstream processing.
    /// </summary>
    /// <value>The game projects value exposed by <see cref="GameStudio"/>.</value>
    private IReadOnlyList<LocalGptProjectSummary> GameProjects { get; set; } = [];
    /// <summary>
    /// Gets or sets the stable selected project identifier used to identify or correlate this game studio instance with related application state.
    /// </summary>
    /// <value>The selected project identifier value exposed by <see cref="GameStudio"/>.</value>
    private Guid SelectedProjectId { get; set; }
    /// <summary>
    /// Gets or sets the draft value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The draft value exposed by <see cref="GameStudio"/>.</value>
    private GameStudioDraft? Draft { get; set; }
    /// <summary>
    /// Gets or sets the active section value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active section value exposed by <see cref="GameStudio"/>.</value>
    private string ActiveSection { get; set; } = "world";
    /// <summary>
    /// Gets or sets a value indicating whether busy applies to the game studio state.
    /// </summary>
    /// <value>The busy value exposed by <see cref="GameStudio"/>.</value>
    private bool Busy { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether confirm close applies to the game studio state.
    /// </summary>
    /// <value>The confirm close value exposed by <see cref="GameStudio"/>.</value>
    private bool ConfirmClose { get; set; }
    /// <summary>
    /// Gets a value indicating whether edit applies to the game studio state.
    /// </summary>
    /// <value>The can edit value exposed by <see cref="GameStudio"/>.</value>
    private bool CanEdit => !Busy && Draft is not null && Draft.ClosedRevisionId is null;
    /// <summary>
    /// Gets or sets the status value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="GameStudio"/>.</value>
    private string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the scene JSON value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scene JSON value exposed by <see cref="GameStudio"/>.</value>
    private string SceneJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the file path used by this game studio instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file path value exposed by <see cref="GameStudio"/>.</value>
    private string FilePath { get; set; } = "scene.json";
    /// <summary>
    /// Gets or sets the file content value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The file content value exposed by <see cref="GameStudio"/>.</value>
    private string FileContent { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the revision name value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision name value exposed by <see cref="GameStudio"/>.</value>
    private string RevisionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the revision summary value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision summary value exposed by <see cref="GameStudio"/>.</value>
    private string RevisionSummary { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the proposal JSON value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal JSON value exposed by <see cref="GameStudio"/>.</value>
    private string ProposalJson { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the patch JSON value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The patch JSON value exposed by <see cref="GameStudio"/>.</value>
    private string PatchJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets the proposal artist value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal artist value exposed by <see cref="GameStudio"/>.</value>
    private string ProposalArtist { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the proposal curator value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal curator value exposed by <see cref="GameStudio"/>.</value>
    private string ProposalCurator { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the proposal description value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal description value exposed by <see cref="GameStudio"/>.</value>
    private string ProposalDescription { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the review critique value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review critique value exposed by <see cref="GameStudio"/>.</value>
    private string ReviewCritique { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether review approved applies to the game studio state.
    /// </summary>
    /// <value>The review approved value exposed by <see cref="GameStudio"/>.</value>
    private bool ReviewApproved { get; set; }
    /// <summary>
    /// Gets or sets the stable selected proposal identifier used to identify or correlate this game studio instance with related application state.
    /// </summary>
    /// <value>The selected proposal identifier value exposed by <see cref="GameStudio"/>.</value>
    private Guid SelectedProposalId { get; set; }
    /// <summary>
    /// Gets or sets the proposals collection maintained or exposed by this game studio instance for downstream processing.
    /// </summary>
    /// <value>The proposals value exposed by <see cref="GameStudio"/>.</value>
    private IReadOnlyList<GameSceneProposal> Proposals { get; set; } = [];
    /// <summary>
    /// Gets or sets the findings collection maintained or exposed by this game studio instance for downstream processing.
    /// </summary>
    /// <value>The findings value exposed by <see cref="GameStudio"/>.</value>
    private IReadOnlyList<GameSceneFinding> Findings { get; set; } = [];
    /// <summary>
    /// Gets or sets the clock value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The clock value exposed by <see cref="GameStudio"/>.</value>
    private GameSceneClock Clock { get; set; } = new();
    /// <summary>
    /// Gets or sets the preview value that forms part of the game studio state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preview value exposed by <see cref="GameStudio"/>.</value>
    private GameScenePreview? Preview { get; set; }

    /// <summary>Initializes project choices in the maintained InteractiveServer circuit.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Initializing Game Studio failed.");
            Notifier.ShowError("GameStudioToasts", "Game Studio could not initialize. Inspect local logs.", "Game Studio");
        }
    }

    /// <summary>Draws pixel-sprite and per-row parallax projection only after Blazor has materialized the canvas.</summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (Draft is not null && Preview is not null)
                await JsRuntime.InvokeVoidAsync("localgptGameStudio.draw", "localgpt-game-studio-pixel-preview", Draft.Scene, Clock).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Rendering the generic pixel-art Game Studio preview failed; the ASCII projection remains available.");
        }
    }

    /// <summary>Refreshes selectable durable Game projects without implicitly editing them.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ReloadAsync()
    {
        if (Busy) return;
        Busy = true;
        try
        {
            GameProjects = (await ProjectService.GetProjectsAsync().ConfigureAwait(true)).Where(project => project.IsGameProject).ToList();
            if (SelectedProjectId == Guid.Empty || GameProjects.All(project => project.Id != SelectedProjectId))
                SelectedProjectId = GameProjects.FirstOrDefault()?.Id ?? Guid.Empty;
            await LoadProjectCoreAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Game Studio project catalog refresh failed.");
            ReportFailure(exception, "Loading game projects");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Changes the current game project and reloads its open/closed working copy.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SelectProjectAsync(Guid id)
    {
        if (Busy) return;
        Busy = true;
        try
        {
            SelectedProjectId = id;
            await LoadProjectCoreAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Game Studio project selection failed.");
            ReportFailure(exception, "Selecting a Game project");
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Loads project core for <see cref="GameStudio"/>, keeping the operation consistent with the state and invariants of the surrounding game studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadProjectCoreAsync()
    {
        try
        {
            Draft = SelectedProjectId == Guid.Empty ? null : await Studio.GetDraftAsync(SelectedProjectId).ConfigureAwait(true);
            Clock = new();
            ConfirmClose = false;
            await RefreshDocumentAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Reading the selected game worktree failed.");
            throw;
        }
    }

    /// <summary>Creates a mutable worktree only through an explicit UI button.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OpenDraftAsync()
    {
        try
        {
        await RunAsync("Opening editable worktree", async () =>
        {
            Draft = await Studio.OpenDraftAsync(SelectedProjectId, true).ConfigureAwait(true);
            Clock = new();
            await RefreshDocumentAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Opening Game Studio worktree failed.");
            throw;
        }
    }

    /// <summary>Switches DevExpress hierarchy areas while keeping the editable scene as one project-owned document.</summary>
    /// <param name="section">Section value supplied to the game studio operation and used when producing its result.</param>
    private void ChangeSection(string section)
    {
        try
        {
            ActiveSection = section;
            if (Draft is not null)
                SceneJson = JsonSerializer.Serialize(Draft.Scene, jsonOptions);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Switching the Game Studio editor section failed.");
            ReportFailure(exception, "Switching Game Studio editor");
        }
    }

    /// <summary>Validates and saves the strongly typed scene model without creating a project revision.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveSceneAsync()
    {
        try
        {
        await RunAsync("Saving editable scene", async () =>
        {
            if (Draft is null) return;
            Draft = await Studio.SaveDraftAsync(SelectedProjectId, Draft, true).ConfigureAwait(true);
            await RefreshDocumentAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving a Game Studio scene failed.");
            throw;
        }
    }

    /// <summary>Validates the entire JSON scene, including actors, world chunks, art masks, scrolling and palette timelines.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveSceneJsonAsync()
    {
        try
        {
        await RunAsync("Validating scene JSON", async () =>
        {
            if (Draft is null) return;
            var nextScene = JsonSerializer.Deserialize<GameSceneDocument>(SceneJson, jsonOptions)
                ?? throw new InvalidOperationException("Scene JSON is empty.");
            Draft.Scene = nextScene;
            Draft = await Studio.SaveDraftAsync(SelectedProjectId, Draft, true).ConfigureAwait(true);
            await RefreshDocumentAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving the scene JSON editor failed.");
            throw;
        }
    }

    /// <summary>Prepares editable C# and scene files without destroying pre-existing human edits.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedAsync()
    {
        try
        {
        await RunAsync("Generating Godot starter files", async () =>
        {
            if (Draft is null) return;
            Draft = await Godot.SeedGodotFilesAsync(SelectedProjectId, Draft.DraftId, true).ConfigureAwait(true);
            await RefreshDocumentAsync().ConfigureAwait(true);
            await SelectFileAsync("scene.json").ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Preparing editable Godot project files failed.");
            throw;
        }
    }

    /// <summary>Publishes the curated scene document to the Godot draft's editable scene resource.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PublishSceneAsync()
    {
        try
        {
        await RunAsync("Updating Godot scene.json", async () =>
        {
            if (Draft is null) return;
            Draft = await Godot.UpdateSceneResourceAsync(SelectedProjectId, Draft.DraftId, true).ConfigureAwait(true);
            await SelectFileAsync("scene.json").ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Publishing the curated Godot scene resource failed.");
            throw;
        }
    }

    /// <summary>Exports files after checking external hashes; an existing human edit is never silently overwritten.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ExportAsync()
    {
        try
        {
        await RunAsync("Exporting Godot project files", async () =>
        {
            if (Draft is null) return;
            var exported = await Godot.ExportAsync(SelectedProjectId, Draft.DraftId, true).ConfigureAwait(true);
            Status = $"Exported {exported.Count} Godot project files to the configured project directory.";
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Exporting edited Godot project files failed.");
            throw;
        }
    }

    /// <summary>Imports files modified by Godot into the draft while retaining the still-open revision.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ImportAsync()
    {
        try
        {
        await RunAsync("Importing Godot edits", async () =>
        {
            if (Draft is null) return;
            var imported = await Godot.ImportChangedAsync(SelectedProjectId, Draft.DraftId, true).ConfigureAwait(true);
            Draft = await Studio.GetDraftAsync(SelectedProjectId).ConfigureAwait(true);
            await RefreshDocumentAsync().ConfigureAwait(true);
            Status = $"Imported {imported.Count} external Godot edits into the open worktree.";
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Importing external Godot edits failed.");
            throw;
        }
    }

    /// <summary>Loads a tracked Godot file into the DevExpress source editor.</summary>
    /// <param name="path">Path value supplied to the game studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SelectFileAsync(string path)
    {
        try
        {
            if (Draft is null) return;
            FilePath = path;
            FileContent = await Studio.ReadFileAsync(SelectedProjectId, Draft.DraftId, path).ConfigureAwait(true);
            ActiveSection = "godot";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Reading the selected Godot source editor file failed.");
            ReportFailure(exception, "Opening Godot project file");
        }
    }

    /// <summary>Saves one source file as another change of the open worktree; does not create a revision.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveFileAsync()
    {
        try
        {
        await RunAsync("Saving Godot source edit", async () =>
        {
            if (Draft is null) return;
            Draft = await Studio.SaveFileAsync(SelectedProjectId, Draft.DraftId, FilePath, FileContent, true).ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving a Godot source edit failed.");
            throw;
        }
    }

    /// <summary>Stores artist-proposed scene art for independent review without changing the active world.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SubmitProposalAsync()
    {
        try
        {
        await RunAsync("Submitting curated scene candidate", async () =>
        {
            if (Draft is null) return;
            var scene = JsonSerializer.Deserialize<GameSceneDocument>(ProposalJson, jsonOptions)
                ?? throw new InvalidOperationException("The AI scene proposal is empty.");
            var proposal = new GameSceneProposal
            {
                DraftId = Draft.DraftId, Artist = ProposalArtist, Curator = ProposalCurator,
                Description = ProposalDescription, Scene = scene
            };
            await Studio.ProposeSceneAsync(SelectedProjectId, Draft.DraftId, proposal, true).ConfigureAwait(true);
            Proposals = await Studio.GetProposalsAsync(SelectedProjectId, Draft.DraftId).ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Submitting an isolated AI artwork candidate failed.");
            throw;
        }
    }

    /// <summary>Lets a smaller local AI or human propose only changed objects while preserving all untouched scene fields.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SubmitPatchAsync()
    {
        try
        {
            await RunAsync("Submitting small AI scene patch", async () =>
            {
                if (Draft is null) return;
                var patch = JsonSerializer.Deserialize<GameScenePatch>(PatchJson, jsonOptions)
                    ?? throw new InvalidOperationException("A scene patch must contain JSON object fields.");
                await Studio.ProposePatchAsync(SelectedProjectId, Draft.DraftId, patch, ProposalArtist, ProposalDescription, true).ConfigureAwait(true);
                Proposals = await Studio.GetProposalsAsync(SelectedProjectId, Draft.DraftId).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Submitting a narrow AI visual patch failed.");
            throw;
        }
    }

    /// <summary>Persists an independent curator review and repeats non-negotiable scene placement validation.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ReviewProposalAsync()
    {
        try
        {
        await RunAsync("Recording independent curator review", async () =>
        {
            if (Draft is null) return;
            await Studio.ReviewProposalAsync(SelectedProjectId, Draft.DraftId, new GameSceneReview
            {
                ProposalId = SelectedProposalId,
                CuratorModel = ProposalCurator,
                Approved = ReviewApproved,
                Critique = ReviewCritique
            }, true).ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Recording the independent curator decision failed.");
            throw;
        }
    }

    /// <summary>Applies a candidate only after independent curator identity and semantic validations pass.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task AcceptProposalAsync()
    {
        try
        {
        await RunAsync("Accepting independently curated art", async () =>
        {
            if (Draft is null) return;
            Draft = await Studio.AcceptProposalAsync(SelectedProjectId, Draft.DraftId, SelectedProposalId, true).ConfigureAwait(true);
            Clock = new();
            await RefreshDocumentAsync().ConfigureAwait(true);
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Accepting the independently curated scene failed.");
            throw;
        }
    }

    /// <summary>Creates the next closed revision only on explicit human approval, then freezes the editor.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CloseAsync()
    {
        try
        {
        await RunAsync("Closing the Godot revision", async () =>
        {
            if (Draft is null || !ConfirmClose) return;
            var revision = await Studio.CloseRevisionAsync(SelectedProjectId, Draft.DraftId, RevisionName, RevisionSummary, ConfirmClose).ConfigureAwait(true);
            Draft = await Studio.GetDraftAsync(SelectedProjectId).ConfigureAwait(true);
            ConfirmClose = false;
            Status = $"Completed project revision '{revision.RevisionName}' ({revision.Id}). Open the next draft to continue editing.";
        }).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Closing the explicit Game Studio revision failed.");
            throw;
        }
    }

    /// <summary>Exercises all catalog-backed recognition bindings on the transient preview clock, not the project draft.</summary>
    /// <returns>An awaitable preview refresh without any saved project mutation.</returns>
    private async Task TestSceneRegexAsync()
    {
        if (Draft is null) return;
        try
        {
            var approved = (await RegexCurator.ListApprovedPatternsAsync().ConfigureAwait(true))
                .Select(pattern => pattern.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var patterns = new Dictionary<string, System.Text.RegularExpressions.Regex>(StringComparer.OrdinalIgnoreCase);
            foreach (var patternName in Draft.Scene.RegexTransitions.Select(binding => binding.RegexPatternName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!approved.Contains(patternName))
                    throw new InvalidOperationException($"Catalog regex '{patternName}' requires independent review and human approval.");
                patterns.Add(patternName, await RegexCatalog.GetRegexAsync(patternName).ConfigureAwait(true)
                    ?? throw new InvalidOperationException($"Catalog regex '{patternName}' was not found."));
            }
            var changes = Engine.ApplyRegexTransitions(Draft.Scene, Clock, RegexSceneInput ?? string.Empty, patterns);
            PreviewNow();
            Status = $"{changes} declarative scene state transition(s) matched; project source remains unchanged.";
        }
        catch (Exception exception)
        {
            Status = $"Regex preview failed: {exception.Message}";
        }
    }

    /// <summary>Creates a preview from scene data at the existing fixed simulation time.</summary>
    private void PreviewNow()
    {
        try
        {
            if (Draft is null) return;
            Findings = Engine.Curate(Draft.Scene);
            Preview = Findings.Any(finding => finding.BlocksAcceptance) ? null : Engine.Preview(Draft.Scene, Clock);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "The Game Studio scene preview failed.");
            ReportFailure(exception, "Rendering scene preview");
        }
    }

    /// <summary>Uses the authored seconds-per-turn mapping on the same world tick clock.</summary>
    private void AdvanceTurn()
    {
        try
        {
            if (Draft is null) return;
            Clock = Engine.Advance(Draft.Scene, Clock, 0, true);
            PreviewNow();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "The Game Studio turn step failed.");
            ReportFailure(exception, "Advancing game turn");
        }
    }

    /// <summary>Advances precisely one second of real-world time, processed at authored fixed ticks.</summary>
    private void AdvanceSecond()
    {
        try
        {
            if (Draft is null) return;
            Clock = Engine.Advance(Draft.Scene, Clock, 1);
            PreviewNow();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "The Game Studio one-second fixed-time step failed.");
            ReportFailure(exception, "Advancing world clock");
        }
    }

    /// <summary>Resets the preview clock, not the authored world or Godot draft.</summary>
    private void ResetClock()
    {
        try
        {
            Clock = new();
            PreviewNow();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Resetting the Game Studio preview clock failed.");
            ReportFailure(exception, "Resetting preview clock");
        }
    }

    /// <summary>Refreshes curation, preview, source JSON and associated AI candidates.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RefreshDocumentAsync()
    {
        try
        {
            SceneJson = Draft is null ? string.Empty : JsonSerializer.Serialize(Draft.Scene, jsonOptions);
            ProposalJson = SceneJson;
            Proposals = Draft is not null && Draft.ClosedRevisionId is null
                ? await Studio.GetProposalsAsync(SelectedProjectId, Draft.DraftId).ConfigureAwait(true) : [];
            PreviewNow();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Refreshing the Game Studio worktree presentation failed.");
            throw;
        }
    }

    /// <summary>Owns busy-state and bounded UI feedback for asynchronous Game Studio edits.</summary>
    /// <param name="action">Action value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="handler">Handler value supplied to the game studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunAsync(string action, Func<Task> handler)
    {
        if (Busy) return;
        Busy = true;
        try
        {
            var statusBefore = Status;
            await handler().ConfigureAwait(true);
            if (Status == statusBefore)
                Status = $"{action} completed in the current Game project.";
            Notifier.ShowSuccess("GameStudioToasts", Status, "Game Studio");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Game Studio UI command {Action} failed.", action);
            ReportFailure(exception, action);
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>Logs errors locally and shows a concise human-actionable notification.</summary>
    /// <param name="exception">Exception value supplied to the game studio operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the game studio operation and used when producing its result.</param>
    private void ReportFailure(Exception exception, string operation)
    {
        try
        {
        Logger.LogError(exception, "Game Studio operation failed: {Operation}.", operation);
        Status = $"{operation} failed: {exception.Message}";
        Notifier.ShowError("GameStudioToasts", Status, "Game Studio");
        }
        catch (Exception notificationException)
        {
            Logger.LogError(notificationException, "Reporting a Game Studio failure failed for operation {Operation}.", operation);
            throw;
        }
    }
}
