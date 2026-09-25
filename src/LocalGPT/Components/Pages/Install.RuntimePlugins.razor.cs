using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using LocalGPT.BusinessObjects;
using LocalGPT.Runtime.Plugins;

namespace LocalGPT.Components.Pages;

public partial class Install
{
    /// <summary>Gets the runtime extension kinds available in the editor.</summary>
    private IReadOnlyList<RuntimePluginKind> RuntimePluginKinds { get; } = Enum.GetValues<RuntimePluginKind>();
    /// <summary>Stores persisted runtime extension definitions displayed by Setup.</summary>
    private IReadOnlyList<RuntimePluginDefinition> RuntimePluginDefinitions = Array.Empty<RuntimePluginDefinition>();
    /// <summary>Stores the current runtime extension editor draft.</summary>
    private RuntimePluginDefinition RuntimePluginDraft = new();
    /// <summary>Stores DevExpress HTML-editor markup separately from the persisted plain executable source.</summary>
    private string RuntimePluginSourceMarkup = string.Empty;
    /// <summary>Tracks whether the runtime extension editor popup is visible.</summary>
    private bool RuntimePluginEditorVisible;
    /// <summary>Tracks runtime extension persistence/build activity.</summary>
    private bool RuntimePluginBusy;
    /// <summary>Stores the latest runtime extension status message.</summary>
    private string RuntimePluginStatus = string.Empty;

    /// <summary>Gets validated compiler/runtime candidates appropriate to the selected extension kind.</summary>
    private IReadOnlyList<ProjectCompilerInstallation> RuntimePluginCompilerInstallations => RuntimePluginDraft.Kind switch
    {
        RuntimePluginKind.CSharpScript => CompilerInstallations
            .Where(item => item.IsEnabled && (string.Equals(item.KnowledgeProfileKey, "dotnet-sdk", StringComparison.OrdinalIgnoreCase) || string.Equals(item.Language, "DotNet", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item => item.LastValidationSucceeded)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList(),
        RuntimePluginKind.JavaScript => CompilerInstallations
            .Where(item => item.IsEnabled && (string.Equals(item.KnowledgeProfileKey, "node", StringComparison.OrdinalIgnoreCase) || string.Equals(item.Language, "JavaScript", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item => item.LastValidationSucceeded)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList(),
        _ => Array.Empty<ProjectCompilerInstallation>()
    };


    /// <summary>Keeps the DevExpress HTML editor focused on source editing by retaining only undo/redo and code-block insertion.</summary>
    /// <param name="toolbar">DevExpress HTML-editor toolbar to customize before initialization.</param>
    private void CustomizeRuntimePluginEditorToolbar(IToolbar toolbar)
    {
        ArgumentNullException.ThrowIfNull(toolbar);
        toolbar.Groups.Clear();
        toolbar.Groups.Add(HtmlEditorToolbarGroupNames.UndoRedo);
        toolbar.Groups.Add(HtmlEditorToolbarGroupNames.InsertElement);
        var insertGroup = toolbar.Groups[HtmlEditorToolbarGroupNames.InsertElement];
        insertGroup.Items.Clear();
        insertGroup.Items.Add(HtmlEditorToolbarItemNames.InsertCodeBlock);
    }

    /// <summary>Loads persisted runtime extensions for the Setup workbench.</summary>
    private async Task RefreshRuntimePluginsAsync()
    {
        try
        {
            RuntimePluginBusy = true;
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            RuntimePluginStatus = $"{RuntimePluginDefinitions.Count} persisted runtime extension(s).";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Loading runtime extensions failed.");
            RuntimePluginStatus = "Runtime extensions could not be loaded. Review LocalGPT logs.";
        }
        finally
        {
            RuntimePluginBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Starts a new runtime extension draft.</summary>
    private void NewRuntimePlugin()
    {
        RuntimePluginDraft = NewPluginDraft();
        SyncRuntimePluginEditorFromDraft();
        RuntimePluginEditorVisible = true;
    }

    /// <summary>Copies a persisted runtime extension into the editor.</summary>
    /// <param name="plugin">Persisted plugin selected by the user.</param>
    private void EditRuntimePlugin(RuntimePluginDefinition plugin)
    {
        RuntimePluginDraft = ClonePlugin(plugin);
        SyncRuntimePluginEditorFromDraft();
        RuntimePluginEditorVisible = true;
    }

    /// <summary>Closes the runtime extension editor without execution.</summary>
    private void CloseRuntimePluginEditor() => RuntimePluginEditorVisible = false;

    /// <summary>Synchronizes the plain executable source into the DevExpress HTML editor as one encoded code block.</summary>
    private void SyncRuntimePluginEditorFromDraft()
    {
        RuntimePluginSourceMarkup = RuntimePluginDefinitionSupport.ToEditorMarkup(RuntimePluginDraft.SourceCode);
    }

    /// <summary>Synchronizes the DevExpress HTML editor back into plain source before persistence or compilation.</summary>
    private void SyncRuntimePluginDraftFromEditor()
    {
        if (RuntimePluginDraft.Kind is RuntimePluginKind.CSharpScript or RuntimePluginKind.JavaScript)
            RuntimePluginDraft.SourceCode = RuntimePluginDefinitionSupport.FromEditorMarkup(RuntimePluginSourceMarkup);
    }

    /// <summary>Inserts the repository-owned starter template for the selected runtime kind.</summary>
    private void InsertRuntimePluginTemplate()
    {
        RuntimePluginDraft.SourceCode = RuntimePlugins.GetTemplate(RuntimePluginDraft.Kind);
        SyncRuntimePluginEditorFromDraft();
    }

    /// <summary>Updates the selected runtime kind and resets an incompatible compiler selection without discarding source text.</summary>
    /// <param name="kind">New runtime-extension kind selected by the local user.</param>
    private void RuntimePluginKindChanged(RuntimePluginKind kind)
    {
        SyncRuntimePluginDraftFromEditor();
        RuntimePluginDraft.Kind = kind;
        if (RuntimePluginCompilerInstallations.All(item => item.Id != RuntimePluginDraft.CompilerInstallationId))
            RuntimePluginDraft.CompilerInstallationId = RuntimePluginCompilerInstallations.FirstOrDefault()?.Id;
        SyncRuntimePluginEditorFromDraft();
    }

    /// <summary>Persists the current runtime extension definition after explicit local action.</summary>
    private async Task SaveRuntimePluginAsync()
    {
        try
        {
            RuntimePluginBusy = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false) /* renderer dispatch */;
            SyncRuntimePluginDraftFromEditor();
            var saved = await RuntimePlugins.SaveAsync(new SaveRuntimePluginRequest { Plugin = RuntimePluginDraft, UserConfirmed = true }).ConfigureAwait(false);
            RuntimePluginDraft = ClonePlugin(saved);
            SyncRuntimePluginEditorFromDraft();
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            RuntimePluginStatus = $"Saved runtime extension {saved.FunctionName}. Build/load remains a separate explicit action.";
            Notifier.ShowSuccess(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving runtime extension failed; executable source content was omitted from logs.");
            RuntimePluginStatus = exception.Message;
            Notifier.ShowError(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        finally
        {
            RuntimePluginBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Saves the current definition and immediately performs the separate explicit build/load action.</summary>
    private async Task SaveAndBuildRuntimePluginAsync()
    {
        try
        {
            RuntimePluginBusy = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false) /* renderer dispatch */;
            SyncRuntimePluginDraftFromEditor();
            var saved = await RuntimePlugins.SaveAsync(new SaveRuntimePluginRequest { Plugin = RuntimePluginDraft, UserConfirmed = true }).ConfigureAwait(false);
            var result = await RuntimePlugins.BuildAndLoadAsync(saved.Id, userConfirmed: true).ConfigureAwait(false);
            RuntimePluginDraft = ClonePlugin((await RuntimePlugins.ListAsync().ConfigureAwait(false)).First(item => item.Id == saved.Id));
            SyncRuntimePluginEditorFromDraft();
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            RuntimePluginStatus = $"{result.Status}: {result.Message}";
            if (result.Succeeded)
                Notifier.ShowSuccess(toastName, RuntimePluginStatus, "Runtime extensions");
            else
                Notifier.ShowWarning(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Saving/building runtime extension failed; executable source content was omitted from logs.");
            RuntimePluginStatus = exception.Message;
            Notifier.ShowError(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        finally
        {
            RuntimePluginBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Builds/materializes and activates one explicitly selected runtime extension.</summary>
    /// <param name="id">Persisted runtime extension identifier.</param>
    private async Task BuildRuntimePluginAsync(Guid id)
    {
        try
        {
            RuntimePluginBusy = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false) /* renderer dispatch */;
            var result = await RuntimePlugins.BuildAndLoadAsync(id, userConfirmed: true).ConfigureAwait(false);
            RuntimePluginStatus = $"{result.Status}: {result.Message}";
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            if (result.Succeeded)
                Notifier.ShowSuccess(toastName, RuntimePluginStatus, "Runtime extensions");
            else
                Notifier.ShowWarning(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Building/loading runtime extension {PluginId} failed.", id);
            RuntimePluginStatus = exception.Message;
            Notifier.ShowError(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        finally
        {
            RuntimePluginBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Deletes one explicitly selected runtime extension definition.</summary>
    /// <param name="id">Persisted runtime extension identifier.</param>
    private async Task DeleteRuntimePluginAsync(Guid id)
    {
        try
        {
            RuntimePluginBusy = true;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false) /* renderer dispatch */;
            await RuntimePlugins.DeleteAsync(id, userConfirmed: true).ConfigureAwait(false);
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            RuntimePluginStatus = "Runtime extension removed.";
            Notifier.ShowSuccess(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Deleting runtime extension {PluginId} failed.", id);
            RuntimePluginStatus = exception.Message;
            Notifier.ShowError(toastName, RuntimePluginStatus, "Runtime extensions");
        }
        finally
        {
            RuntimePluginBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    /// <summary>Creates the default persisted runtime-extension draft and prefers a validated .NET SDK when available.</summary>
    /// <returns>A detached runtime-extension definition ready for editor binding.</returns>
    private RuntimePluginDefinition NewPluginDraft() => new()
    {
        Name = "New runtime extension",
        FunctionName = "plugin.example",
        Purpose = "Describe what this runtime extension provides.",
        SafetyNotes = "User-authored executable code. Review source and permissions before build/load.",
        Kind = RuntimePluginKind.CSharpScript,
        CompilerInstallationId = CompilerInstallations.FirstOrDefault(item => item.IsEnabled && string.Equals(item.KnowledgeProfileKey, "dotnet-sdk", StringComparison.OrdinalIgnoreCase) && item.LastValidationSucceeded)?.Id,
        RequiresHumanConfirmation = true,
        IsReadOnly = true,
        AvailableToAi = true,
        IsEnabled = true
    };

    /// <summary>Creates a detached editor copy of one persisted runtime-extension definition.</summary>
    /// <param name="source">Persisted definition to copy without attaching it to the editor state.</param>
    /// <returns>A detached editable definition.</returns>
    private RuntimePluginDefinition ClonePlugin(RuntimePluginDefinition source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        FunctionName = source.FunctionName,
        Purpose = source.Purpose,
        SafetyNotes = source.SafetyNotes,
        ParameterSchemaJson = source.ParameterSchemaJson,
        Kind = source.Kind,
        SourceCode = source.SourceCode,
        PackagePayloadBase64 = source.PackagePayloadBase64,
        EntryAssemblyName = source.EntryAssemblyName,
        EntryTypeName = source.EntryTypeName,
        CompilerInstallationId = source.CompilerInstallationId,
        IsEnabled = source.IsEnabled,
        AvailableToAi = source.AvailableToAi,
        IsReadOnly = source.IsReadOnly,
        RequiresHumanConfirmation = source.RequiresHumanConfirmation,
        SupportsAutomaticInvocation = source.SupportsAutomaticInvocation,
        ContentHash = source.ContentHash,
        LastBuildStatus = source.LastBuildStatus,
        LastBuildMessage = source.LastBuildMessage,
        LastLoadedAtUtc = source.LastLoadedAtUtc,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };
}
