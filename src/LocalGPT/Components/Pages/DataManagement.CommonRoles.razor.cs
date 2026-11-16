using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;
using DevExpress.Blazor;
using System.Globalization;

namespace LocalGPT.Components.Pages;

/// <summary>Owns the Data Management editor for reusable Council role presets and the Keyaccount reviewer model.</summary>
public partial class DataManagement
{
    /// <summary>Gets the runtime-policy key that stores the common role presets as JSON.</summary>
    private string CommonRolesPolicyKey => "CouncilCommonRolesJson";

    /// <summary>Gets the runtime-policy key that stores the Keyaccount reviewer model selection key.</summary>
    private string KeyaccountReviewerPolicyKey => "GoalKeyaccountReviewerModelKey";

    /// <summary>Staged common role presets shown in the grid; persisted only by the save command.</summary>
    private List<OrganicCouncilRoleDefinition> CommonRoles { get; set; } = [];

    /// <summary>Grid row that is currently focused and targeted by the edit and remove commands.</summary>
    private OrganicCouncilRoleDefinition? CommonRoleSelected { get; set; }

    /// <summary>Working copy of the role being added or edited, or null while no edit form is open.</summary>
    private OrganicCouncilRoleDefinition? CommonRoleDraft { get; set; }

    /// <summary>Staged role that the working copy will replace; null when the working copy is a new role.</summary>
    private OrganicCouncilRoleDefinition? CommonRoleEditTarget { get; set; }

    /// <summary>Indicates loading, saving or applying common roles is in progress.</summary>
    private bool CommonRolesBusy { get; set; }

    /// <summary>Explicit confirmation required before presets overwrite fields of team roles.</summary>
    private bool CommonRolesApplyConfirmed { get; set; }

    /// <summary>Validation or outcome message for the last common-role command.</summary>
    private string CommonRolesStatus { get; set; } = string.Empty;

    /// <summary>Selectable performance modes for the edit form.</summary>
    private IReadOnlyList<CouncilRolePerformanceMode> CommonRolePerformanceModes { get; } = Enum.GetValues<CouncilRolePerformanceMode>();

    /// <summary>Selectable boundary modes for the edit form.</summary>
    private IReadOnlyList<CouncilRoleBoundaryMode> CommonRoleBoundaryModes { get; } = Enum.GetValues<CouncilRoleBoundaryMode>();

    /// <summary>Selectable language modes for the edit form.</summary>
    private IReadOnlyList<CouncilRoleLanguageMode> CommonRoleLanguageModes { get; } = Enum.GetValues<CouncilRoleLanguageMode>();

    /// <summary>Selectable human participation modes for the edit form.</summary>
    private IReadOnlyList<HumanParticipationMode> CommonRoleHumanModes { get; } = Enum.GetValues<HumanParticipationMode>();

    /// <summary>Currently saved Keyaccount reviewer model selection key; empty means the run's own model is used.</summary>
    private string KeyaccountReviewerKey { get; set; } = string.Empty;

    /// <summary>Model options for the Keyaccount reviewer selector, including a saved but unavailable key.</summary>
    private IReadOnlyList<LocalGptSelectionOption<string>> KeyaccountReviewerOptions { get; set; } = [];

    /// <summary>Gets whether common-role commands may start now.</summary>
    private bool CommonRolesCanRun => !Busy && !CommonRolesBusy;

    /// <summary>Gets whether a grid row is available for the edit and remove commands.</summary>
    private bool CommonRolesCanChangeSelected => CommonRolesCanRun && CommonRoleSelected is not null && CommonRoleDraft is null;

    /// <summary>Gets whether the team-apply button is enabled; requires the explicit confirmation checkbox.</summary>
    private bool CommonRolesCanApply => CommonRolesCanRun && CommonRolesApplyConfirmed && CommonRoles.Count > 0 && CommonRoleDraft is null;

    /// <summary>Loads stored presets (seeding from the default team templates when empty) and the reviewer model setting.</summary>
    /// <returns>Asynchronous load operation.</returns>
    private async Task LoadCommonRolesCoreAsync()
    {
        CommonRolesBusy = true;
        try
        {
            var stored = new List<OrganicCouncilRoleDefinition>();
            try
            {
                stored = RuntimePolicy.GetJson<List<OrganicCouncilRoleDefinition>>(CommonRolesPolicyKey) ?? [];
            }
            catch (Exception error)
            {
                Logger.LogWarning(error, "Stored common Council roles could not be read; the default template roles are offered instead.");
            }

            if (stored.Count == 0)
            {
                var templates = await TeamConfigurations.GetDefaultTemplatesAsync().ConfigureAwait(true);
                stored = templates
                    .SelectMany(template => template.Roles)
                    .Where(role => !string.IsNullOrWhiteSpace(role.Role))
                    .GroupBy(role => role.Role.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => CloneCommonRole(group.First()))
                    .OrderBy(role => role.Role, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            CommonRoles = stored;
            CommonRoleSelected = null;
            CommonRoleDraft = null;
            CommonRolesApplyConfirmed = false;
            KeyaccountReviewerKey = (RuntimePolicy.GetString(KeyaccountReviewerPolicyKey) ?? string.Empty).Trim();
            RebuildKeyaccountReviewerOptions();
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Loading the common Council roles failed.");
            Notifier.ShowError(ToastName, "Common roles could not be loaded; inspect local logs.", "Common roles");
        }
        finally
        {
            CommonRolesBusy = false;
        }
    }

    /// <summary>Rebuilds the reviewer model options from the discovered provider models and the saved key.</summary>
    private void RebuildKeyaccountReviewerOptions()
    {
        try
        {
            var options = PreferredModelCandidates
                .Select(candidate => new LocalGptSelectionOption<string>(candidate.SelectionKey, PreferredModelDisplay(candidate)))
                .ToList();
            if (!string.IsNullOrWhiteSpace(KeyaccountReviewerKey)
                && !options.Any(option => string.Equals(option.Value, KeyaccountReviewerKey, StringComparison.OrdinalIgnoreCase)))
                options.Add(new LocalGptSelectionOption<string>(KeyaccountReviewerKey, "Saved but unavailable · " + KeyaccountReviewerKey));
            KeyaccountReviewerOptions = options;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Building the Keyaccount reviewer model options failed.");
            throw;
        }
    }

    /// <summary>Creates an independent preset copy that carries no team-specific model assignments or pairing.</summary>
    /// <param name="source">Role definition to copy.</param>
    /// <returns>Detached copy without assigned model keys and paired role.</returns>
    private OrganicCouncilRoleDefinition CloneCommonRole(OrganicCouncilRoleDefinition source)
    {
        try
        {
            var copy = JsonText.Deserialize<OrganicCouncilRoleDefinition>(JsonText.Serialize(source)) ?? new OrganicCouncilRoleDefinition();
            copy.AssignedModelKeys = [];
            copy.PairedRole = string.Empty;
            return copy;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Copying a common Council role failed.");
            throw;
        }
    }

    /// <summary>Tracks the focused grid row for the edit and remove commands.</summary>
    /// <param name="args">Focused-row change data from the grid.</param>
    private void OnCommonRoleFocusedRowChanged(GridFocusedRowChangedEventArgs args)
    {
        try
        {
            CommonRoleSelected = args.DataItem as OrganicCouncilRoleDefinition;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Tracking the focused common role failed.");
            Notifier.ShowError(ToastName, "The role selection could not be read.", "Common roles");
        }
    }

    /// <summary>Opens the edit form with a new role that uses the business-object defaults.</summary>
    private void AddCommonRole()
    {
        try
        {
            if (!CommonRolesCanRun) return;
            CommonRoleEditTarget = null;
            CommonRoleDraft = new OrganicCouncilRoleDefinition();
            CommonRolesStatus = string.Empty;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Adding a common Council role failed.");
            Notifier.ShowError(ToastName, "A new role could not be started.", "Common roles");
        }
    }

    /// <summary>Opens the edit form with a copy of the focused role.</summary>
    private void EditCommonRole()
    {
        try
        {
            if (!CommonRolesCanChangeSelected || CommonRoleSelected is null) return;
            CommonRoleEditTarget = CommonRoleSelected;
            CommonRoleDraft = CloneCommonRole(CommonRoleSelected);
            CommonRolesStatus = string.Empty;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Editing a common Council role failed.");
            Notifier.ShowError(ToastName, "The role could not be opened for editing.", "Common roles");
        }
    }

    /// <summary>Validates the edit form and stages the role in the list without persisting it.</summary>
    private void AcceptCommonRoleDraft()
    {
        try
        {
            if (CommonRoleDraft is null) return;
            CommonRoleDraft.Role = (CommonRoleDraft.Role ?? string.Empty).Trim();
            if (CommonRoleDraft.Role.Length == 0)
            {
                CommonRolesStatus = T("DataManagement.CommonRolesNameRequired", "Enter a role name.");
                return;
            }

            var duplicate = CommonRoles.Any(role => !ReferenceEquals(role, CommonRoleEditTarget)
                && string.Equals(role.Role, CommonRoleDraft.Role, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                CommonRolesStatus = T("DataManagement.CommonRolesNameDuplicate", "A common role with this name already exists.");
                return;
            }

            CommonRoleDraft.MinimumAiParticipants = Math.Max(CommonRoleDraft.MinimumAiParticipants, 0);
            CommonRoleDraft.MaximumAiParticipants = Math.Max(CommonRoleDraft.MaximumAiParticipants, CommonRoleDraft.MinimumAiParticipants);

            var next = CommonRoles.ToList();
            var index = next.FindIndex(role => ReferenceEquals(role, CommonRoleEditTarget));
            if (CommonRoleEditTarget is not null && index >= 0)
                next[index] = CommonRoleDraft;
            else
                next.Add(CommonRoleDraft);
            CommonRoles = next.OrderBy(role => role.Role, StringComparer.OrdinalIgnoreCase).ToList();
            CommonRoleSelected = CommonRoleDraft;
            CommonRoleDraft = null;
            CommonRoleEditTarget = null;
            CommonRolesStatus = string.Empty;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Staging a common Council role failed.");
            Notifier.ShowError(ToastName, "The role could not be staged.", "Common roles");
        }
    }

    /// <summary>Closes the edit form and discards the working copy.</summary>
    private void CancelCommonRoleDraft()
    {
        try
        {
            CommonRoleDraft = null;
            CommonRoleEditTarget = null;
            CommonRolesStatus = string.Empty;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Cancelling the common Council role edit failed.");
            Notifier.ShowError(ToastName, "The edit could not be cancelled.", "Common roles");
        }
    }

    /// <summary>Removes the focused role from the staged list; persistence happens on save.</summary>
    private void RemoveCommonRole()
    {
        try
        {
            if (!CommonRolesCanChangeSelected || CommonRoleSelected is null) return;
            CommonRoles = CommonRoles.Where(role => !ReferenceEquals(role, CommonRoleSelected)).ToList();
            CommonRoleSelected = null;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Removing a common Council role failed.");
            Notifier.ShowError(ToastName, "The role could not be removed.", "Common roles");
        }
    }

    /// <summary>Persists the staged presets as runtime-policy JSON and refreshes the runtime policy snapshot.</summary>
    /// <returns>Asynchronous save operation.</returns>
    private async Task SaveCommonRolesAsync()
    {
        if (!CommonRolesCanRun) return;
        CommonRolesBusy = true;
        try
        {
            var json = JsonText.Serialize(CommonRoles);
            await CommonRoleVariables.SetAsync(CommonRolesPolicyKey, json).ConfigureAwait(true);
            await ReloadRuntimePolicyAfterCommonRoleChangeAsync().ConfigureAwait(true);
            Logger.LogInformation("Saved {RoleCount} common Council roles.", CommonRoles.Count);
            Notifier.ShowSuccess(ToastName, "Common roles saved.", "Common roles");
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Saving the common Council roles failed.");
            Notifier.ShowError(ToastName, "Could not save common roles; inspect local logs.", "Common roles");
        }
        finally
        {
            CommonRolesBusy = false;
        }
    }

    /// <summary>Reloads the runtime policy snapshot on a worker thread because the reload itself is synchronous.</summary>
    /// <returns>Asynchronous reload operation.</returns>
    private async Task ReloadRuntimePolicyAfterCommonRoleChangeAsync()
    {
        try
        {
            await Task.Run(() => RuntimePolicy.Reload()).ConfigureAwait(true);
        }
        catch (Exception error)
        {
            Logger.LogError(error, "The runtime policy snapshot could not be reloaded after a common-role change; restart LocalGPT to load it.");
            Notifier.ShowWarning(ToastName, "The value was stored, but the runtime policy could not be reloaded.", "Common roles");
        }
    }

    /// <summary>Copies the preset fields onto a team role and reports whether anything differed.</summary>
    /// <param name="source">Common role preset.</param>
    /// <param name="target">Team role to update; assigned models and paired role stay untouched.</param>
    /// <returns>True when at least one field changed.</returns>
    private bool CopyCommonRoleFields(OrganicCouncilRoleDefinition source, OrganicCouncilRoleDefinition target)
    {
        try
        {
            var changed = target.Expertise != source.Expertise
                || target.Responsibility != source.Responsibility
                || target.PerformanceMode != source.PerformanceMode
                || target.BoundaryMode != source.BoundaryMode
                || target.LanguageMode != source.LanguageMode
                || target.HumanParticipationMode != source.HumanParticipationMode
                || target.MinimumAiParticipants != source.MinimumAiParticipants
                || target.MaximumAiParticipants != source.MaximumAiParticipants;
            target.Expertise = source.Expertise;
            target.Responsibility = source.Responsibility;
            target.PerformanceMode = source.PerformanceMode;
            target.BoundaryMode = source.BoundaryMode;
            target.LanguageMode = source.LanguageMode;
            target.HumanParticipationMode = source.HumanParticipationMode;
            target.MinimumAiParticipants = source.MinimumAiParticipants;
            target.MaximumAiParticipants = source.MaximumAiParticipants;
            return changed;
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Copying common role fields onto a team role failed.");
            throw;
        }
    }

    /// <summary>Applies the staged presets to same-named roles in every enabled team and saves each changed team.</summary>
    /// <returns>Asynchronous apply operation.</returns>
    private async Task ApplyCommonRolesToTeamsAsync()
    {
        if (!CommonRolesCanApply) return;
        CommonRolesBusy = true;
        try
        {
            var teams = await TeamConfigurations.GetTeamsAsync().ConfigureAwait(true);
            var savedTeams = 0;
            var failedTeams = 0;
            foreach (var team in teams)
            {
                try
                {
                    var changed = false;
                    foreach (var role in team.Roles)
                    {
                        var preset = CommonRoles.FirstOrDefault(item => string.Equals(item.Role, role.Role, StringComparison.OrdinalIgnoreCase));
                        if (preset is not null && CopyCommonRoleFields(preset, role))
                            changed = true;
                    }

                    if (!changed) continue;
                    await TeamConfigurations.SaveAsync(new SaveCouncilTeamConfigurationRequest
                    {
                        Team = team,
                        IsEnabled = team.IsEnabled,
                        UserConfirmed = CommonRolesApplyConfirmed
                    }).ConfigureAwait(true);
                    savedTeams++;
                }
                catch (Exception error)
                {
                    failedTeams++;
                    Logger.LogError(error, "Applying common roles to Council team {TeamKey} failed.", team.Key);
                }
            }

            CommonRolesApplyConfirmed = false;
            CommonRolesStatus = string.Format(CultureInfo.CurrentCulture,
                T("DataManagement.CommonRolesApplied", "Updated {0} team(s); {1} failed."), savedTeams, failedTeams);
            Logger.LogInformation("Applied common roles to {SavedTeams} teams with {FailedTeams} failures.", savedTeams, failedTeams);
            if (failedTeams > 0)
                Notifier.ShowWarning(ToastName, CommonRolesStatus, "Common roles");
            else
                Notifier.ShowSuccess(ToastName, CommonRolesStatus, "Common roles");
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Applying common roles to teams failed.");
            Notifier.ShowError(ToastName, "Could not apply common roles to teams; inspect local logs.", "Common roles");
        }
        finally
        {
            CommonRolesBusy = false;
        }
    }

    /// <summary>Stages the reviewer model chosen in the selector; blank means the run's own model.</summary>
    /// <param name="value">Selected provider-qualified model key, or null when cleared.</param>
    private void SetKeyaccountReviewerKey(string? value)
    {
        try
        {
            KeyaccountReviewerKey = (value ?? string.Empty).Trim();
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Staging the Keyaccount reviewer model failed.");
            Notifier.ShowError(ToastName, "The reviewer model could not be changed.", "Common roles");
        }
    }

    /// <summary>Persists the Keyaccount reviewer model runtime value and refreshes the runtime policy snapshot.</summary>
    /// <returns>Asynchronous save operation.</returns>
    private async Task SaveKeyaccountReviewerAsync()
    {
        if (!CommonRolesCanRun) return;
        CommonRolesBusy = true;
        try
        {
            await CommonRoleVariables.SetAsync(KeyaccountReviewerPolicyKey, KeyaccountReviewerKey).ConfigureAwait(true);
            await ReloadRuntimePolicyAfterCommonRoleChangeAsync().ConfigureAwait(true);
            Logger.LogInformation("Saved the Keyaccount reviewer model setting.");
            Notifier.ShowSuccess(ToastName, "Keyaccount reviewer model saved.", "Common roles");
        }
        catch (Exception error)
        {
            Logger.LogError(error, "Saving the Keyaccount reviewer model failed.");
            Notifier.ShowError(ToastName, "Could not save the reviewer model; inspect local logs.", "Common roles");
        }
        finally
        {
            CommonRolesBusy = false;
        }
    }
}
