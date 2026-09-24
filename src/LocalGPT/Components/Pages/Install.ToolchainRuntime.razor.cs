using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages
{
    public partial class Install
    {
        private ToolchainEnvironmentSnapshot? ToolchainEnvironmentSnapshot;
        private IReadOnlyList<ToolchainAcquisitionSource> ToolchainAcquisitionSources = [];
        private IReadOnlyList<ToolchainExecutionProfile> ToolchainProfiles = [];
        private string ToolchainRuntimeStatus = string.Empty;
        private string ToolchainEnvironmentSearch = string.Empty;
        private string ToolchainEnvironmentName = string.Empty;
        private string ToolchainEnvironmentValue = string.Empty;
        private ToolchainEnvironmentScope ToolchainEnvironmentEditScope = ToolchainEnvironmentScope.Application;
        private bool ToolchainEnvironmentRemove;
        private Guid? ToolchainEnvironmentToolchainId;
        private bool ToolchainRuntimeBusy;
        private string ToolchainProfileKey = string.Empty;
        private string ToolchainProfileName = string.Empty;
        private string ToolchainProfileCapability = string.Empty;
        private Guid? ToolchainProfileInstallationId;
        private ToolchainExecutionKind ToolchainProfileExecutionKind = ToolchainExecutionKind.Module;
        private string ToolchainProfileEntryPoint = string.Empty;
        private string ToolchainProfileWorkingDirectory = string.Empty;
        private string ToolchainProfileArgumentsJson = "[]";
        private string ToolchainProfileEnvironmentJson = "{}";
        private string ToolchainProfileConfigurationJson = "{}";
        private bool ToolchainProfileIsDefault;
        private bool ToolchainProfileRequiresApproval = true;
        private IReadOnlyList<ToolchainExecutionKind> ToolchainExecutionKinds { get; } = Enum.GetValues<ToolchainExecutionKind>();

        private IReadOnlyList<ToolchainEnvironmentScope> ToolchainEnvironmentWritableScopes =>
            ToolchainEnvironmentSnapshot?.SupportsPersistentOperatingSystemScopes == true
                ? [ToolchainEnvironmentScope.Process, ToolchainEnvironmentScope.Application, ToolchainEnvironmentScope.User, ToolchainEnvironmentScope.Machine]
                : [ToolchainEnvironmentScope.Process, ToolchainEnvironmentScope.Application];

        private IReadOnlyList<ToolchainEnvironmentEntry> VisibleToolchainEnvironmentEntries =>
            ToolchainEnvironmentSnapshot is null
                ? []
                : ToolchainEnvironment.FilterEntries(ToolchainEnvironmentSnapshot.Entries, ToolchainEnvironmentSearch, 300);

        private async Task RefreshToolchainRuntimeManagementAsync()
        {
            try
            {
                ToolchainRuntimeBusy = true;
                ToolchainEnvironmentSnapshot = await ToolchainEnvironment.GetSnapshotAsync().ConfigureAwait(false);
                ToolchainAcquisitionSources = await ToolchainAcquisition.GetCatalogAsync().ConfigureAwait(false);
                ToolchainProfiles = await ToolchainExecutionProfiles.GetProfilesAsync().ConfigureAwait(false);
                ToolchainRuntimeStatus = $"Environment, {ToolchainProfiles.Count} process profile(s), and {ToolchainAcquisitionSources.Count} reviewed acquisition source(s) loaded.";
            }
            catch (Exception exception)
            {
                Logger.LogError("Refreshing toolchain runtime management failed with {ExceptionType}; environment values, URLs and paths were omitted from logs.", exception.GetType().Name);
                ToolchainRuntimeStatus = "Toolchain runtime management could not be refreshed. Review LocalGPT logs.";
            }
            finally
            {
                ToolchainRuntimeBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task ChangeToolchainEnvironmentAsync()
        {
            try
            {
                ToolchainRuntimeBusy = true;
                ToolchainEnvironmentSnapshot = await ToolchainEnvironment.ChangeAsync(new ToolchainEnvironmentChangeRequest
                {
                    Name = ToolchainEnvironmentName,
                    Value = ToolchainEnvironmentValue,
                    Scope = ToolchainEnvironmentEditScope,
                    ToolchainInstallationId = ToolchainEnvironmentEditScope == ToolchainEnvironmentScope.Application ? ToolchainEnvironmentToolchainId : null,
                    Remove = ToolchainEnvironmentRemove,
                    UserConfirmed = true
                }).ConfigureAwait(false);
                ToolchainRuntimeStatus = ToolchainEnvironmentRemove
                    ? "Environment value removed from the selected scope."
                    : "Environment value saved in the selected scope.";
                ToolchainEnvironmentValue = string.Empty;
            }
            catch (Exception exception)
            {
                Logger.LogError("Changing a toolchain environment value failed with {ExceptionType}; name and value were omitted from logs.", exception.GetType().Name);
                ToolchainRuntimeStatus = exception.Message;
            }
            finally
            {
                ToolchainRuntimeBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private async Task DownloadToolchainSourceAsync(ToolchainAcquisitionSource source)
        {
            try
            {
                ToolchainRuntimeBusy = true;
                ToolchainRuntimeStatus = $"Downloading reviewed {source.DisplayName} artifact...";
                var result = await ToolchainAcquisition.DownloadAsync(new ToolchainAcquisitionDownloadRequest
                {
                    SourceKey = source.Key,
                    UserConfirmed = true
                }).ConfigureAwait(false);
                ToolchainRuntimeStatus = $"Downloaded {source.DisplayName}: {result.Bytes:N0} bytes, SHA-256 {result.Sha256}. {result.InstallHint}";
            }
            catch (Exception exception)
            {
                Logger.LogError("Downloading a reviewed toolchain source failed with {ExceptionType}; source URL and local path were omitted from logs.", exception.GetType().Name);
                ToolchainRuntimeStatus = exception.Message;
            }
            finally
            {
                ToolchainRuntimeBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }
        private async Task SaveToolchainExecutionProfileAsync()
        {
            try
            {
                ToolchainRuntimeBusy = true;
                var saved = await ToolchainExecutionProfiles.SaveProfileAsync(new SaveToolchainExecutionProfileRequest
                {
                    UserConfirmed = true,
                    Profile = new ToolchainExecutionProfile
                    {
                        ProfileKey = ToolchainProfileKey,
                        Name = ToolchainProfileName,
                        ToolchainInstallationId = ToolchainProfileInstallationId,
                        CapabilityKey = ToolchainProfileCapability,
                        ExecutionKind = ToolchainProfileExecutionKind,
                        EntryPoint = ToolchainProfileEntryPoint,
                        WorkingDirectory = ToolchainProfileWorkingDirectory,
                        ArgumentsJson = ToolchainProfileArgumentsJson,
                        EnvironmentVariablesJson = ToolchainProfileEnvironmentJson,
                        ConfigurationJson = ToolchainProfileConfigurationJson,
                        IsEnabled = true,
                        IsDefaultForCapability = ToolchainProfileIsDefault,
                        RequiresApproval = ToolchainProfileRequiresApproval,
                        UpdatedBy = "Install UI"
                    }
                }).ConfigureAwait(false);
                ToolchainProfiles = await ToolchainExecutionProfiles.GetProfilesAsync().ConfigureAwait(false);
                ToolchainRuntimeStatus = $"Saved process profile {saved.ProfileKey}.";
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Saving a toolchain process profile failed; command details were omitted from logs.");
                ToolchainRuntimeStatus = exception.Message;
            }
            finally
            {
                ToolchainRuntimeBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

        private void EditToolchainExecutionProfile(ToolchainExecutionProfile profile)
        {
            ToolchainProfileKey = profile.ProfileKey;
            ToolchainProfileName = profile.Name;
            ToolchainProfileCapability = profile.CapabilityKey;
            ToolchainProfileInstallationId = profile.ToolchainInstallationId;
            ToolchainProfileExecutionKind = profile.ExecutionKind;
            ToolchainProfileEntryPoint = profile.EntryPoint;
            ToolchainProfileWorkingDirectory = profile.WorkingDirectory;
            ToolchainProfileArgumentsJson = profile.ArgumentsJson;
            ToolchainProfileEnvironmentJson = profile.EnvironmentVariablesJson;
            ToolchainProfileConfigurationJson = profile.ConfigurationJson;
            ToolchainProfileIsDefault = profile.IsDefaultForCapability;
            ToolchainProfileRequiresApproval = profile.RequiresApproval;
            ToolchainRuntimeStatus = $"Editing process profile {profile.ProfileKey}.";
        }

        private async Task RunToolchainExecutionProfileAsync(ToolchainExecutionProfile profile)
        {
            try
            {
                ToolchainRuntimeBusy = true;
                ToolchainRuntimeStatus = $"Running {profile.Name}...";
                var result = await ToolchainExecutionProfiles.ExecuteAsync(new ToolchainProcessExecutionRequest
                {
                    ProfileKey = profile.ProfileKey,
                    UserConfirmed = true
                }).ConfigureAwait(false);
                ToolchainRuntimeStatus = $"{profile.Name} exited with code {result.ExitCode}. {result.StandardOutput.Trim()} {result.StandardError.Trim()}".Trim();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Running a toolchain process profile failed; command details and output were omitted from logs.");
                ToolchainRuntimeStatus = exception.Message;
            }
            finally
            {
                ToolchainRuntimeBusy = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(false);
            }
        }

    }
}
