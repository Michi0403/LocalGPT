using LocalGPT.BusinessObjects;

namespace LocalGPT.Components.Pages
{
    /// <summary>
    /// Renders the install Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
    /// </summary>
    public partial class Install
    {
        /// <summary>
        /// Stores the internal toolchain environment snapshot state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private ToolchainEnvironmentSnapshot? ToolchainEnvironmentSnapshot;
        /// <summary>
        /// Stores the in-memory toolchain acquisition sources collection maintained internally by <see cref="Install"/> for its current workflow state.
        /// </summary>
        private IReadOnlyList<ToolchainAcquisitionSource> ToolchainAcquisitionSources = [];
        /// <summary>
        /// Stores the in-memory toolchain profiles collection maintained internally by <see cref="Install"/> for its current workflow state.
        /// </summary>
        private IReadOnlyList<ToolchainExecutionProfile> ToolchainProfiles = [];
        /// <summary>
        /// Stores the internal toolchain runtime status state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainRuntimeStatus = string.Empty;
        /// <summary>
        /// Stores the internal toolchain environment search state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainEnvironmentSearch = string.Empty;
        /// <summary>
        /// Stores the internal toolchain environment name state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainEnvironmentName = string.Empty;
        /// <summary>
        /// Stores the internal toolchain environment value state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainEnvironmentValue = string.Empty;
        /// <summary>
        /// Stores the internal toolchain environment edit scope state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private ToolchainEnvironmentScope ToolchainEnvironmentEditScope = ToolchainEnvironmentScope.Application;
        /// <summary>
        /// Stores the internal toolchain environment remove state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private bool ToolchainEnvironmentRemove;
        /// <summary>
        /// Stores the internal toolchain environment toolchain identifier state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private Guid? ToolchainEnvironmentToolchainId;
        /// <summary>
        /// Stores the internal toolchain runtime busy state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private bool ToolchainRuntimeBusy;
        /// <summary>
        /// Stores the internal toolchain profile key state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileKey = string.Empty;
        /// <summary>
        /// Stores the internal toolchain profile name state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileName = string.Empty;
        /// <summary>
        /// Stores the internal toolchain profile capability state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileCapability = string.Empty;
        /// <summary>
        /// Stores the internal toolchain profile installation identifier state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private Guid? ToolchainProfileInstallationId;
        /// <summary>
        /// Stores the internal toolchain profile execution kind state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private ToolchainExecutionKind ToolchainProfileExecutionKind = ToolchainExecutionKind.Module;
        /// <summary>
        /// Stores the internal toolchain profile entry point state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileEntryPoint = string.Empty;
        /// <summary>
        /// Stores the internal toolchain profile working directory state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileWorkingDirectory = string.Empty;
        /// <summary>
        /// Stores the internal toolchain profile arguments JSON state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileArgumentsJson = "[]";
        /// <summary>
        /// Stores the internal toolchain profile environment JSON state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileEnvironmentJson = "{}";
        /// <summary>
        /// Stores the internal toolchain profile configuration JSON state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private string ToolchainProfileConfigurationJson = "{}";
        /// <summary>
        /// Stores the internal toolchain profile is default state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private bool ToolchainProfileIsDefault;
        /// <summary>
        /// Stores the internal toolchain profile requires approval state used by <see cref="Install"/> while executing its surrounding workflow.
        /// </summary>
        private bool ToolchainProfileRequiresApproval = true;
        /// <summary>
        /// Gets the toolchain execution kinds collection maintained or exposed by this install instance for downstream processing.
        /// </summary>
        /// <value>The toolchain execution kinds value exposed by <see cref="Install"/>.</value>
        private IReadOnlyList<ToolchainExecutionKind> ToolchainExecutionKinds { get; } = Enum.GetValues<ToolchainExecutionKind>();

        /// <summary>
        /// Gets the toolchain environment writable scopes collection maintained or exposed by this install instance for downstream processing.
        /// </summary>
        /// <value>The toolchain environment writable scopes value exposed by <see cref="Install"/>.</value>
        private IReadOnlyList<ToolchainEnvironmentScope> ToolchainEnvironmentWritableScopes =>
            ToolchainEnvironmentSnapshot?.SupportsPersistentOperatingSystemScopes == true
                ? [ToolchainEnvironmentScope.Process, ToolchainEnvironmentScope.Application, ToolchainEnvironmentScope.User, ToolchainEnvironmentScope.Machine]
                : [ToolchainEnvironmentScope.Process, ToolchainEnvironmentScope.Application];

        /// <summary>
        /// Gets the visible toolchain environment entries collection maintained or exposed by this install instance for downstream processing.
        /// </summary>
        /// <value>The visible toolchain environment entries value exposed by <see cref="Install"/>.</value>
        private IReadOnlyList<ToolchainEnvironmentEntry> VisibleToolchainEnvironmentEntries =>
            ToolchainEnvironmentSnapshot is null
                ? []
                : ToolchainEnvironment.FilterEntries(ToolchainEnvironmentSnapshot.Entries, ToolchainEnvironmentSearch, 300);

        /// <summary>
        /// Refreshes toolchain runtime management for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
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

        /// <summary>
        /// Performs change toolchain environment for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
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

        /// <summary>
        /// Performs download toolchain source for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
        /// </summary>
        /// <param name="source">Source value supplied to the install operation and used when producing its result.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
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
        /// <summary>
        /// Persists toolchain execution profile for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
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

        /// <summary>
        /// Performs edit toolchain execution profile for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
        /// </summary>
        /// <param name="profile">Profile value supplied to the install operation and used when producing its result.</param>
        private void EditToolchainExecutionProfile(ToolchainExecutionProfile profile)
        {
            try
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
            catch (Exception __componentMethodException)
            {
                Logger.LogError(__componentMethodException, "Component method Install.ToolchainRuntime.EditToolchainExecutionProfile failed.");
                throw;
            }
        }

        /// <summary>
        /// Performs run toolchain execution profile for <see cref="Install"/>, keeping the operation consistent with the state and invariants of the surrounding install workflow.
        /// </summary>
        /// <param name="profile">Profile value supplied to the install operation and used when producing its result.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
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
