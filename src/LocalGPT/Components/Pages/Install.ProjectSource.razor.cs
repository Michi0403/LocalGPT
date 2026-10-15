using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages;

/// <summary>
/// Renders the install Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
/// </summary>
public partial class Install
{
    /// <summary>
    /// Stores the in-memory runtime plugin source projects collection maintained internally by <see cref="Install"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<LocalGptProjectSummary> RuntimePluginSourceProjects = [];
    /// <summary>
    /// Stores the in-memory runtime plugin source revisions collection maintained internally by <see cref="Install"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<LocalGptProjectRevision> RuntimePluginSourceRevisions = [];
    /// <summary>
    /// Stores the in-memory runtime plugin source files collection maintained internally by <see cref="Install"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<ProjectSourceFileMatch> RuntimePluginSourceFiles = [];
    // The popup template context belongs to DevExpress; the edited business object
    // and its project/revision/file selection belong to this InteractiveServer instance.
    /// <summary>
    /// Stores the internal runtime plugin source context state used by <see cref="Install"/> while executing its surrounding workflow.
    /// </summary>
    private RuntimePluginSourceEditContext RuntimePluginSourceContext = new();
    /// <summary>
    /// Stores the internal runtime plugin source status state used by <see cref="Install"/> while executing its surrounding workflow.
    /// </summary>
    private string RuntimePluginSourceStatus = string.Empty;

    /// <summary>
    /// Refreshes runtime plugin source projects for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RefreshRuntimePluginSourceProjectsAsync()
    {
        try
        {
            var context = RuntimePluginSourceContext;
            var projects = await LocalProjects.GetProjectsAsync(cancellationToken: default).ConfigureAwait(false);
            // Data acquisition can run off-circuit; applying UI state must run on its renderer.
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext)) return;
                RuntimePluginSourceProjects = projects;
                if (context.ProjectId != Guid.Empty && projects.All(item => item.Id != context.ProjectId))
                {
                    context.ProjectId = Guid.Empty;
                    context.RevisionId = Guid.Empty;
                    context.TrackedFileId = Guid.Empty;
                    context.SelectionVersion++;
                    RuntimePluginSourceFiles = [];
                    RuntimePluginSourceRevisions = [];
                }
                RuntimePluginSourceStatus = $"{projects.Count} project(s) available for approved source publishing.";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Refreshing project source selections failed; no code logged.");
            await InvokeAsync(() =>
            {
                RuntimePluginSourceStatus = "Projects could not be loaded. Check project services and diagnostics.";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the runtime plugin source project changed async lifecycle or event notification for <see cref="Install"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="context">Context value supplied to the install operation and used when producing its result.</param>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OnRuntimePluginSourceProjectChangedAsync(RuntimePluginSourceEditContext context, Guid projectId)
    {
        try
        {
            if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id) return;
            if (projectId != Guid.Empty && RuntimePluginSourceProjects.All(item => item.Id != projectId))
                throw new InvalidOperationException("The selected project is not in the current project list.");
            context.ProjectId = projectId;
            context.RevisionId = Guid.Empty;
            context.TrackedFileId = Guid.Empty;
            var selectionVersion = ++context.SelectionVersion;
            RuntimePluginSourceRevisions = [];
            RuntimePluginSourceFiles = [];
            IReadOnlyList<LocalGptProjectRevision> revisions = projectId == Guid.Empty
                ? []
                : await ProjectArchitecture.GetRevisionsAsync(projectId).ConfigureAwait(false);
            var shouldLoadFiles = false;
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id ||
                    context.ProjectId != projectId || context.SelectionVersion != selectionVersion) return;
                RuntimePluginSourceRevisions = revisions;
                shouldLoadFiles = true;
                StateHasChanged();
            }).ConfigureAwait(false);
            if (shouldLoadFiles)
                await LoadRuntimePluginSourceFilesAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Choosing project source failed.");
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext)) return;
                RuntimePluginSourceStatus = "Project source selection failed. Review the project revision.";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the runtime plugin source revision changed async lifecycle or event notification for <see cref="Install"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="context">Context value supplied to the install operation and used when producing its result.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OnRuntimePluginSourceRevisionChangedAsync(RuntimePluginSourceEditContext context, Guid revisionId)
    {
        try
        {
            if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id) return;
            if (revisionId != Guid.Empty && RuntimePluginSourceRevisions.All(item => item.Id != revisionId || item.ProjectId != context.ProjectId))
                throw new InvalidOperationException("The selected revision does not belong to the current project.");
            context.RevisionId = revisionId;
            context.TrackedFileId = Guid.Empty;
            context.SelectionVersion++;
            RuntimePluginSourceFiles = [];
            await LoadRuntimePluginSourceFilesAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Choosing project source revision failed.");
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext)) return;
                RuntimePluginSourceStatus = "Revision selection failed.";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the runtime plugin source file changed lifecycle or event notification for <see cref="Install"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="context">Context value supplied to the install operation and used when producing its result.</param>
    /// <param name="trackedFileId">Identifier of the tracked file to use for this operation.</param>
    /// <returns>A task that completes after the render-affine file selection is applied.</returns>
    private async Task OnRuntimePluginSourceFileChangedAsync(RuntimePluginSourceEditContext context, Guid trackedFileId)
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id) return;
                if (trackedFileId != Guid.Empty && RuntimePluginSourceFiles.All(item => item.TrackedFileId != trackedFileId))
                    throw new InvalidOperationException("The selected file is not an approved tracked source in this revision.");
                context.TrackedFileId = trackedFileId;
                context.SelectionVersion++;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Choosing curated source file failed; source omitted.");
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id) return;
                RuntimePluginSourceStatus = "Source file selection failed; refresh reviewed files.";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads runtime plugin source files for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
    /// </summary>
    /// <param name="context">Context value supplied to the install operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadRuntimePluginSourceFilesAsync(RuntimePluginSourceEditContext context)
    {
        try
        {
            if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id) return;
            var projectId = context.ProjectId;
            var revisionId = context.RevisionId;
            var selectionVersion = context.SelectionVersion;
            if (projectId == Guid.Empty)
            {
                RuntimePluginSourceFiles = [];
                return;
            }
            var navigation = await ProjectSourceNavigation.NavigateAsync(new ProjectSourceNavigationRequest
            {
                ProjectId = projectId,
                RevisionId = revisionId == Guid.Empty ? null : revisionId,
                Take = 100
            }).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                // Delayed responses may not overwrite another draft, revision or file choice.
                if (!ReferenceEquals(context, RuntimePluginSourceContext) || context.PluginId != RuntimePluginDraft.Id ||
                    context.ProjectId != projectId || context.RevisionId != revisionId ||
                    context.SelectionVersion != selectionVersion) return;
                RuntimePluginSourceFiles = navigation.Files;
                if (context.TrackedFileId != Guid.Empty && RuntimePluginSourceFiles.All(item => item.TrackedFileId != context.TrackedFileId))
                    context.TrackedFileId = Guid.Empty;
                RuntimePluginSourceStatus = $"{navigation.Files.Count} approved tracked file(s), {navigation.RelatedPatterns.Count} curator-approved matching regex pattern(s), {navigation.Toolchains.Count} validated toolchain profile(s).";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading curator-matched tracked project files failed; source omitted.");
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext)) return;
                RuntimePluginSourceFiles = [];
                context.TrackedFileId = Guid.Empty;
                RuntimePluginSourceStatus = "Project file navigation failed; scan and curate this exact revision first.";
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Publishes runtime plugin project source for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PublishRuntimePluginProjectSourceAsync()
    {
        try
        {
            var context = RuntimePluginSourceContext;
            if (context.PluginId != RuntimePluginDraft.Id || context.ProjectId == Guid.Empty || context.TrackedFileId == Guid.Empty ||
                RuntimePluginSourceFiles.All(item => item.TrackedFileId != context.TrackedFileId))
                throw new InvalidOperationException("Select a reviewed source file for this exact plugin draft before publishing.");
            var selectionVersion = context.SelectionVersion;
            RuntimePluginBusy = true;
            var result = await ProjectSourceNavigation.PublishApprovedScriptAsync(new PublishTrackedProjectScriptRequest
            {
                ProjectId = context.ProjectId,
                RevisionId = context.RevisionId == Guid.Empty ? null : context.RevisionId,
                TrackedFileId = context.TrackedFileId,
                Plugin = RuntimePluginDraft,
                UserConfirmed = true
            }).ConfigureAwait(false);
            var definitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                if (!ReferenceEquals(context, RuntimePluginSourceContext) ||
                    context.SelectionVersion != selectionVersion || context.PluginId != RuntimePluginDraft.Id) return;
                RuntimePluginDefinitions = definitions;
                RuntimePluginDraft = ClonePlugin(definitions.Single(item => item.Id == result.PluginId));
                context.PluginId = RuntimePluginDraft.Id;
                context.SelectionVersion++;
                SyncRuntimePluginEditorFromDraft();
                RuntimePluginSourceStatus = $"Reviewed project file published as {RuntimePluginDraft.FunctionName} (source hash {result.ApprovedSourceHash}); Build/load remains separate.";
                Notifier.ShowSuccess(toastName, RuntimePluginSourceStatus, "Project source publication");
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Publishing reviewed project file failed; source omitted.");
            await InvokeAsync(() =>
            {
                RuntimePluginSourceStatus = exception.Message;
                Notifier.ShowError(toastName, RuntimePluginSourceStatus, "Project source publication");
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        finally
        {
            await InvokeAsync(() =>
            {
                RuntimePluginBusy = false;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }
}
