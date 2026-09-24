using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages;

public partial class Install
{
    /// <summary>Gets the runtime extension kinds available in the editor.</summary>
    private IReadOnlyList<RuntimePluginKind> RuntimePluginKinds { get; } = Enum.GetValues<RuntimePluginKind>();
    /// <summary>Stores persisted runtime extension definitions displayed by Setup.</summary>
    private IReadOnlyList<RuntimePluginDefinition> RuntimePluginDefinitions = Array.Empty<RuntimePluginDefinition>();
    /// <summary>Stores the current runtime extension editor draft.</summary>
    private RuntimePluginDefinition RuntimePluginDraft = new();
    /// <summary>Tracks whether the runtime extension editor popup is visible.</summary>
    private bool RuntimePluginEditorVisible;
    /// <summary>Tracks runtime extension persistence/build activity.</summary>
    private bool RuntimePluginBusy;
    /// <summary>Stores the latest runtime extension status message.</summary>
    private string RuntimePluginStatus = string.Empty;

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
        RuntimePluginEditorVisible = true;
    }

    /// <summary>Copies a persisted runtime extension into the editor.</summary>
    /// <param name="plugin">Persisted plugin selected by the user.</param>
    private void EditRuntimePlugin(RuntimePluginDefinition plugin)
    {
        RuntimePluginDraft = ClonePlugin(plugin);
        RuntimePluginEditorVisible = true;
    }

    /// <summary>Closes the runtime extension editor without execution.</summary>
    private void CloseRuntimePluginEditor() => RuntimePluginEditorVisible = false;

    /// <summary>Inserts the repository-owned starter template for the selected runtime kind.</summary>
    private void InsertRuntimePluginTemplate()
    {
        RuntimePluginDraft.SourceCode = RuntimePlugins.GetTemplate(RuntimePluginDraft.Kind);
    }

    /// <summary>Persists the current runtime extension definition after explicit local action.</summary>
    private async Task SaveRuntimePluginAsync()
    {
        try
        {
            RuntimePluginBusy = true;
            var saved = await RuntimePlugins.SaveAsync(new SaveRuntimePluginRequest { Plugin = RuntimePluginDraft, UserConfirmed = true }).ConfigureAwait(false);
            RuntimePluginStatus = $"Saved runtime extension {saved.FunctionName}. Build/load remains a separate action.";
            RuntimePluginEditorVisible = false;
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
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

    /// <summary>Builds/materializes and activates one explicitly selected runtime extension.</summary>
    /// <param name="id">Persisted runtime extension identifier.</param>
    private async Task BuildRuntimePluginAsync(Guid id)
    {
        try
        {
            RuntimePluginBusy = true;
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
            await RuntimePlugins.DeleteAsync(id, userConfirmed: true).ConfigureAwait(false);
            RuntimePluginDefinitions = await RuntimePlugins.ListAsync().ConfigureAwait(false);
            RuntimePluginStatus = "Runtime extension removed.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Deleting runtime extension {PluginId} failed.", id);
            RuntimePluginStatus = exception.Message;
        }
        finally
        {
            RuntimePluginBusy = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private RuntimePluginDefinition NewPluginDraft() => new()
    {
        Name = "New runtime extension",
        FunctionName = "plugin.example",
        Purpose = "Describe what this runtime extension provides.",
        SafetyNotes = "User-authored executable code. Review source and permissions before build/load.",
        Kind = RuntimePluginKind.CSharpScript,
        RequiresHumanConfirmation = true,
        IsReadOnly = true,
        AvailableToAi = true,
        IsEnabled = true
    };

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
