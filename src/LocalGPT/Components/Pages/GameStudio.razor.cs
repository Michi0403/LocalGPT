using System.Text.Json;
using LocalGPT.BusinessObjects;
using Microsoft.JSInterop;

namespace LocalGPT.Components.Pages;

/// <summary>InteractiveServer DevExpress editing surface for game scenes, AI proposals and mutable Godot C# revision worktrees.</summary>
public partial class GameStudio
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private IReadOnlyList<LocalGptProjectSummary> GameProjects { get; set; } = [];
    private Guid SelectedProjectId { get; set; }
    private GameStudioDraft? Draft { get; set; }
    private string ActiveSection { get; set; } = "world";
    private bool Busy { get; set; }
    private bool ConfirmClose { get; set; }
    private bool CanEdit => !Busy && Draft is not null && Draft.ClosedRevisionId is null;
    private string Status { get; set; } = string.Empty;
    private string SceneJson { get; set; } = string.Empty;
    private string FilePath { get; set; } = "scene.json";
    private string FileContent { get; set; } = string.Empty;
    private string RevisionName { get; set; } = string.Empty;
    private string RevisionSummary { get; set; } = string.Empty;
    private string ProposalJson { get; set; } = string.Empty;
    private string PatchJson { get; set; } = "{}";
    private string ProposalArtist { get; set; } = string.Empty;
    private string ProposalCurator { get; set; } = string.Empty;
    private string ProposalDescription { get; set; } = string.Empty;
    private string ReviewCritique { get; set; } = string.Empty;
    private bool ReviewApproved { get; set; }
    private Guid SelectedProposalId { get; set; }
    private IReadOnlyList<GameSceneProposal> Proposals { get; set; } = [];
    private IReadOnlyList<GameSceneFinding> Findings { get; set; } = [];
    private GameSceneClock Clock { get; set; } = new();
    private GameScenePreview? Preview { get; set; }

    /// <summary>Initializes project choices in the maintained InteractiveServer circuit.</summary>
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

    /// <summary>Reads the durable worktree and refreshes the deterministic console preview.</summary>
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
