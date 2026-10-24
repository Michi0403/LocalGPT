using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;

namespace LocalGPT.Components.Pages;

/// <summary>Provides capability-focused PublisherStudio team affinity and a checked single-model assignment action.</summary>
public partial class CouncilTeams
{
    /// <summary>
    /// Projects discovered PublisherStudio capabilities and persisted,
    /// user-editable team/reset-template preferences from prepared render state. No source-owned capability allowlist
    /// can become a second policy source or silently authorize an operation.
    /// </summary>
    /// <value>PublisherStudio capability keys available to the team affinity editor.</value>
    private IReadOnlyList<string> PublisherOrganicCapabilityKeys
    {
        get
        {
            try
            {
                return CouncilText.FilterPublisherOrganicCapabilityKeys(
                    _publisherAdvertisedCapabilityKeys
                        .Concat(_teams.SelectMany(team => team.PreferredCapabilities))
                        .Concat(_defaultTemplates.SelectMany(template => template.PreferredCapabilities))
                        .Concat(_editor.PreferredCapabilities));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Council Teams could not inspect publisher organic capability preferences.");
                throw;
            }
        }
    }

    /// <summary>Snapshot of online, enabled PublisherStudio capabilities, prepared by the supervised discovery workflow.</summary>
    private IReadOnlyList<string> _publisherAdvertisedCapabilityKeys = [];

    /// <summary>Stores the selected installed and configured provider identity until an explicit Apply click.</summary>
    private string _publisherSingleModelKey = string.Empty;

    /// <summary>Gets only currently available model identities, excluding offline machines and unavailable saved bindings.</summary>
    private IReadOnlyList<LocalGptSelectionOption<string>> PublisherOrganicLiveModelOptions
    {
        get
        {
            try
            {
                return _providerModels
                    .Where(candidate => candidate.IsInstalled && candidate.IsConfigured)
                    .OrderBy(candidate => candidate.Provider, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(candidate => candidate.ModelName, StringComparer.OrdinalIgnoreCase)
                    .Select(candidate => new LocalGptSelectionOption<string>(candidate.SelectionKey,
                        $"{candidate.Provider} · {candidate.ModelName} · {candidate.Endpoint}"))
                    .ToList();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Council Teams failed to list live PublisherStudio model candidates.");
                throw;
            }
        }
    }

    /// <summary>Explicitly prunes provider bindings that discovery no longer reports installed and configured; this never runs automatically.</summary>
    private void RemoveUnavailablePublisherModels()
    {
        try
        {
            var available = _providerModels.Where(model => model.IsInstalled && model.IsConfigured)
                .Select(model => model.SelectionKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (available.Count == 0)
            {
                _hasError = true;
                _status = "No installed and configured providers were found. Refresh discovery before pruning saved bindings.";
                return;
            }
            var removed = 0;
            foreach (var role in _editor.Roles)
                removed += role.AssignedModelKeys.RemoveAll(key => !available.Contains(key));
            if (removed > 0)
            {
                _confirmed = false;
                RefreshAdvancedJson();
            }
            _hasError = false;
            _status = $"Removed {removed} unavailable saved role binding(s). Check empty assigned-role pools and explicitly save if correct.";
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Council Teams failed to prune unavailable provider bindings after explicit user action.");
            throw;
        }
    }

    /// <summary>Binds one currently available provider-qualified model to every AI role without enabling offline hosts.</summary>
    private void BindPublisherOrganicSingleModel()
    {
        try
        {
            var candidate = _providerModels.FirstOrDefault(model =>
                model.IsInstalled && model.IsConfigured &&
                string.Equals(model.SelectionKey, _publisherSingleModelKey, StringComparison.OrdinalIgnoreCase));
            if (candidate is null)
            {
                _hasError = true;
                _status = "The selected provider model is no longer installed and configured. Refresh model discovery.";
                return;
            }
            foreach (var role in _editor.Roles.Where(role => role.HumanParticipationMode != HumanParticipationMode.HumanOnly))
            {
                role.AiSelectionMode = CouncilRoleAiSelectionMode.AssignedModels;
                role.AssignedModelKeys = [candidate.SelectionKey];
                role.MinimumAiParticipants = 1;
                role.MaximumAiParticipants = 1;
                role.DistinctAiAssignmentGroup = string.Empty;
            }
            foreach (var step in _editor.WorkflowSteps.Where(step =>
                string.Equals(step.ExecutionMode, "AssignedModelSingle", StringComparison.OrdinalIgnoreCase)))
                step.AssignedModelName = candidate.SelectionKey;
            _confirmed = false;
            _hasError = false;
            _status = $"Bound {candidate.ModelName} on {candidate.Provider} to this team's AI roles. Review and confirm Save to persist.";
            RefreshAdvancedJson();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Council Teams failed to bind a verified provider model to PublisherStudio roles.");
            throw;
        }
    }
}
