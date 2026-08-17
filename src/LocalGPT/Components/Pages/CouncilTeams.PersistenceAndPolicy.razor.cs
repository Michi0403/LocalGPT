using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using LocalGPT;
using LocalGPT.Components;
using LocalGPT.Components.Layout;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using DevExpress.Blazor.PivotTable;
using DevExpress.Blazor.PdfViewer;
using DevExpress.Blazor.Reporting.Models;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;
using Markdig;
using System.Dynamic;
using System.Globalization;
using LocalGPT.Components.Shared;
using Microsoft.AspNetCore.Components;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Components.Pages
{
    public partial class CouncilTeams
    {
    private async Task SaveAsync()
    {
        if (!_confirmed)
        {
            _status = "Review the team and check the confirmation box before saving.";
            _hasError = true;
            Notifier.ShowWarning(nameof(CouncilTeams), _status, "Review required");
            return;
        }

        _busy = true;
        try
        {
            NormalizeEditorOrdering();
            _editor.PreferredCapabilities = Deserialize<List<string>>(_capabilitiesJson);
            _editor.AllowedAutomaticFunctions = Deserialize<List<string>>(_allowedFunctionsJson);
            _editor.ArchitectureContracts = Deserialize<List<string>>(_contractsJson);
            RefreshAdvancedJson();
            var saved = await TeamConfigurations.SaveAsync(new SaveCouncilTeamConfigurationRequest
            {
                Team = _editor,
                IsEnabled = _editor.IsEnabled,
                UserConfirmed = _confirmed
            }).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                _selectedKey = saved.Key;
                _status = $"Saved '{saved.DisplayName}' with {saved.Roles.Count} role(s) and {CalculateExpandedRoundCount(saved.WorkflowSteps)} maximum expanded round(s).";
                _hasError = false;
                Notifier.ShowSuccess(nameof(CouncilTeams), _status, "Council team saved");
                _confirmed = false;
            }).ConfigureAwait(false);
            await ReloadAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or ArgumentException or InvalidOperationException)
        {
            Logger.LogWarning(ex, "AI Council team configuration was rejected.");
            ComponentActivity.RecordFailure(nameof(CouncilTeams), nameof(SaveAsync), ex);
            await InvokeAsync(() =>
            {
                _status = ex.Message;
                _hasError = true;
                Notifier.ShowError(nameof(CouncilTeams), ex.Message, "Council team rejected");
            }).ConfigureAwait(false);
        }
        finally
        {
            await InvokeAsync(() => _busy = false).ConfigureAwait(false);
        }
    }


    private async Task DeleteSelectedAsync()
    {
        if (!_confirmed)
        {
            _status = "Review the destructive action and check the confirmation box before deleting this preset.";
            _hasError = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(_selectedKey))
            return;
        _busy = true;
        try
        {
            var deletedKey = _selectedKey;
            await TeamConfigurations.DeleteAsync(deletedKey, true).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                _status = $"Deleted configured Council preset '{deletedKey}'. Its tombstone remains available for template reset.";
                _hasError = false;
                _confirmed = false;
            }).ConfigureAwait(false);
            await ReloadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Deleting Council team {TeamKey} from the editor failed.", _selectedKey);
            await InvokeAsync(() =>
            {
                _status = ex.Message;
                _hasError = true;
            }).ConfigureAwait(false);
        }
        finally
        {
            await InvokeAsync(() => _busy = false).ConfigureAwait(false);
        }
    }

    private async Task ResetSelectedFromTemplateAsync()
    {
        if (!_confirmed)
        {
            _status = "Review the reset and check the confirmation box before replacing this preset from a supplied template.";
            _hasError = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(_selectedKey) || string.IsNullOrWhiteSpace(_resetTemplateKey))
            return;
        _busy = true;
        try
        {
            var templateKey = _resetTemplateKey;
            var reset = await TeamConfigurations.ResetToTemplateAsync(_selectedKey, templateKey, true).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                _selectedKey = reset.Key;
                _status = $"Reset '{reset.Key}' from supplied template '{templateKey}'.";
                _hasError = false;
                _confirmed = false;
            }).ConfigureAwait(false);
            await ReloadAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Resetting Council team {TeamKey} from template {TemplateKey} failed in the editor.", _selectedKey, _resetTemplateKey);
            await InvokeAsync(() =>
            {
                _status = ex.Message;
                _hasError = true;
            }).ConfigureAwait(false);
        }
        finally
        {
            await InvokeAsync(() => _busy = false).ConfigureAwait(false);
        }
    }

    private void ToggleTeamAutomaticFunction(string functionName, ChangeEventArgs args)
    {
        _editor.AllowedAutomaticFunctions ??= [];
        var enabled = args.Value is bool value && value;
        _editor.AllowedAutomaticFunctions.RemoveAll(item => string.Equals(item, functionName, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            _editor.AllowedAutomaticFunctions.Add(functionName);
        _editor.AllowedAutomaticFunctions = _editor.AllowedAutomaticFunctions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _allowedFunctionsJson = Serialize(_editor.AllowedAutomaticFunctions);
        _confirmed = false;
    }

    private void SetAutomaticFunctionPolicy(CouncilWorkflowStepDefinition step, ChangeEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (!Enum.TryParse<CouncilAutomaticFunctionPolicyMode>(args.Value?.ToString(), ignoreCase: true, out var policy) ||
            policy == CouncilAutomaticFunctionPolicyMode.Legacy)
        {
            policy = CouncilAutomaticFunctionPolicyMode.Disabled;
        }
        step.AutomaticFunctionPolicyMode = policy;
        step.CanUseOrganicFunctions = policy != CouncilAutomaticFunctionPolicyMode.Disabled;
        step.UseBuiltInBehavior = false;
        _confirmed = false;
    }

    private void SetStepAutomaticFunctions(CouncilWorkflowStepDefinition step, ChangeEventArgs args)
    {
        step.AllowedAutomaticFunctions = CouncilText.ParseUserEditableNameList(args.Value?.ToString());
        _confirmed = false;
    }

    private void ReviewConfirmationChanged()
    {
        _status = _confirmed
            ? "Review confirmed. The Save reviewed team button is ready."
            : "Review confirmation cleared.";
        _hasError = false;
    }

    private void TogglePreferredCapability(string functionName, ChangeEventArgs args)
    {
        _editor.PreferredCapabilities ??= [];
        var enabled = args.Value is bool value && value;
        _editor.PreferredCapabilities.RemoveAll(item => string.Equals(item, functionName, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            _editor.PreferredCapabilities.Add(functionName);
        _editor.PreferredCapabilities = _editor.PreferredCapabilities
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _capabilitiesJson = Serialize(_editor.PreferredCapabilities);
        _confirmed = false;
    }

    private string FunctionNamespace(DxAiFunctionCatalogEntry entry)
    {
        var name = entry.FunctionName?.Trim() ?? string.Empty;
        var separator = name.LastIndexOf('.');
        return separator > 0 ? name[..separator] : entry.Kind;
    }

    private void ToggleRoleModel(int roleIndex, string selectionKey, ChangeEventArgs args)
    {
        if (roleIndex < 0 || roleIndex >= _editor.Roles.Count)
            return;
        var role = _editor.Roles[roleIndex];
        role.AssignedModelKeys ??= [];
        var enabled = args.Value is bool value && value;
        role.AssignedModelKeys.RemoveAll(item => string.Equals(item, selectionKey, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            role.AssignedModelKeys.Add(selectionKey);
        role.AssignedModelKeys = role.AssignedModelKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _confirmed = false;
    }

    /// <summary>Sets a provider-bound role to an exact invocation count without removing the more general minimum/maximum range controls.</summary>
    /// <param name="roleIndex">Zero-based index of the role being edited.</param>
    /// <param name="args">Change event containing the requested positive invocation count.</param>
    private void SetExactRoleInvocationCount(int roleIndex, ChangeEventArgs args)
    {
        if (roleIndex < 0 || roleIndex >= _editor.Roles.Count)
            return;
        if (!int.TryParse(args.Value?.ToString(), out var count) || count < 1)
            return;

        var role = _editor.Roles[roleIndex];
        role.MinimumAiParticipants = count;
        role.MaximumAiParticipants = count;
        _confirmed = false;
    }

    private void RemoveRoleModel(int roleIndex, string selectionKey)
    {
        if (roleIndex < 0 || roleIndex >= _editor.Roles.Count)
            return;
        var role = _editor.Roles[roleIndex];
        role.AssignedModelKeys ??= [];
        role.AssignedModelKeys.RemoveAll(item => string.Equals(item, selectionKey, StringComparison.OrdinalIgnoreCase));
        _confirmed = false;
    }

    private IReadOnlyList<string> UnavailableRoleModelKeys(OrganicCouncilRoleDefinition role) =>
        role.AssignedModelKeys
            .Where(selectionKey => !_providerModels.Any(candidate =>
                string.Equals(candidate.SelectionKey, selectionKey, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(selectionKey => selectionKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Returns whether a role uses an exact provider-qualified model pool.</summary>
    /// <param name="role">Role whose AI assignment policy is inspected.</param>
    /// <returns><see langword="true"/> when the role binds exact provider model identities.</returns>
    private bool UsesProviderBoundRolePool(OrganicCouncilRoleDefinition role) =>
        role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange;

    /// <summary>Returns whether a role exposes the configurable minimum/maximum participant-count controls.</summary>
    /// <param name="role">Role whose AI assignment policy is inspected.</param>
    /// <returns><see langword="true"/> for unrestricted random selection or provider-pool random selection.</returns>
    private bool UsesRandomParticipantCount(OrganicCouncilRoleDefinition role) =>
        role.AiSelectionMode is CouncilRoleAiSelectionMode.RandomRange or CouncilRoleAiSelectionMode.AssignedModelsRandomRange;

    private IReadOnlyList<MultiModelCouncilModelCandidate> WorkflowModelCandidates(CouncilWorkflowStepDefinition step)
    {
        var role = FindRolePolicy(step.Role);
        IEnumerable<MultiModelCouncilModelCandidate> candidates = _providerModels;
        if (role is not null && UsesProviderBoundRolePool(role) && role.AssignedModelKeys.Count > 0)
        {
            candidates = candidates.Where(candidate =>
                role.AssignedModelKeys.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase));
        }

        return candidates
            .OrderBy(candidate => candidate.Provider, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Endpoint, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool IsWorkflowModelAvailable(string selectionKey) =>
        _providerModels.Any(candidate =>
            string.Equals(candidate.SelectionKey, selectionKey, StringComparison.OrdinalIgnoreCase));

    private void ToggleRuntimeClass(int roleIndex, string key, ChangeEventArgs args)
    {
        if (roleIndex < 0 || roleIndex >= _editor.Roles.Count)
            return;
        var role = _editor.Roles[roleIndex];
        role.RuntimeClassKeys ??= [];
        var enabled = args.Value is bool value && value;
        role.RuntimeClassKeys.RemoveAll(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            role.RuntimeClassKeys.Add(key);
        role.RuntimeClassKeys = role.RuntimeClassKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _confirmed = false;
    }

    private IReadOnlyList<CouncilRuntimeClassDefinition> RuntimeClassesFor(OrganicCouncilRoleDefinition role) =>
        _runtimeClasses
            .Where(item => role.RuntimeClassKeys.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(item => item.Namespace, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private OrganicCouncilRoleDefinition? FindRolePolicy(string? roleName) =>
        _editor.Roles.FirstOrDefault(role =>
            !string.IsNullOrWhiteSpace(roleName) &&
            string.Equals(role.Role, roleName.Trim(), StringComparison.OrdinalIgnoreCase));

    private string WorkflowRolePolicyLabel(string? roleName)
    {
        var role = FindRolePolicy(roleName);
        return role is null
            ? "missing role policy; run will be blocked"
            : $"{RoleAiBadge(role)}; {RoleHumanBadge(role)}";
    }

    private string RoleAiBadge(OrganicCouncilRoleDefinition role)
    {
        if (role.HumanParticipationMode == HumanParticipationMode.HumanOnly)
            return "Human only · 0 AI";
        if (role.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected)
            return "All selected AIs";
        if (role.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels)
            return $"{role.AssignedModelKeys.Count} provider-bound AI" + (role.AssignedModelKeys.Count == 1 ? string.Empty : "s");
        if (role.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
        {
            var countText = role.MinimumAiParticipants == role.MaximumAiParticipants
                ? role.MinimumAiParticipants.ToString()
                : $"{role.MinimumAiParticipants}–{role.MaximumAiParticipants}";
            return $"Random {countText} invocation(s) from {role.AssignedModelKeys.Count} provider-bound sage/model(s)";
        }
        return role.MinimumAiParticipants == role.MaximumAiParticipants
            ? $"Random {role.MinimumAiParticipants} AI" + (role.MinimumAiParticipants == 1 ? string.Empty : "s")
            : $"Random {role.MinimumAiParticipants}–{role.MaximumAiParticipants} AIs";
    }

    private string RoleHumanBadge(OrganicCouncilRoleDefinition role) => role.HumanParticipationMode switch
    {
        HumanParticipationMode.Optional => "Human optional",
        HumanParticipationMode.Required => "Human required",
        HumanParticipationMode.HumanOnly => "Human only",
        _ => "AI role"
    };

    private string RoleHumanBadgeClass(OrganicCouncilRoleDefinition role) => role.HumanParticipationMode switch
    {
        HumanParticipationMode.Optional => "human-optional",
        HumanParticipationMode.Required => "human-required",
        HumanParticipationMode.HumanOnly => "human-only",
        _ => "human-none"
    };

    private string RolePerformanceBadge(OrganicCouncilRoleDefinition role) => role.PerformanceMode switch
    {
        CouncilRolePerformanceMode.ImprovisationPlayer => "Improvisation player",
        _ => "Task specialist"
    };

    private string RoleBoundaryBadge(OrganicCouncilRoleDefinition role) => role.BoundaryMode switch
    {
        CouncilRoleBoundaryMode.Collaborative => "Collaborative boundary",
        CouncilRoleBoundaryMode.Strict => "Strict boundary",
        _ => "Bounded role"
    };

    private string RoleLanguageBadge(OrganicCouncilRoleDefinition role) => role.LanguageMode switch
    {
        CouncilRoleLanguageMode.SenderLanguage => "Sender language",
        CouncilRoleLanguageMode.English => "English",
        _ => "Model language"
    };

    private string RolePolicyExplanation(OrganicCouncilRoleDefinition role) => role.HumanParticipationMode switch
    {
        HumanParticipationMode.HumanOnly => "This role pauses for a human response and assigns no AI model.",
        HumanParticipationMode.Required => $"The role pauses for a human response, then {RoleAiBadge(role).ToLowerInvariant()} continue with that response in the transcript.",
        HumanParticipationMode.Optional => $"The human may join this role without blocking it; {RoleAiBadge(role).ToLowerInvariant()} are assigned when the round runs.",
        _ => $"No human response is requested for this role; {RoleAiBadge(role).ToLowerInvariant()} are assigned when the round runs."
    };

    private string RolePerformanceExplanation(OrganicCouncilRoleDefinition role) => role.PerformanceMode switch
    {
        CouncilRolePerformanceMode.ImprovisationPlayer => "The AI kernel plays this role as a self-aware improvisation participant, stays inside the fictional scene, and does not seize another role.",
        _ => "The AI kernel approaches this role as a bounded task specialist."
    };

    private string RoleBoundaryExplanation(OrganicCouncilRoleDefinition role) => role.BoundaryMode switch
    {
        CouncilRoleBoundaryMode.Collaborative => "The participant may offer clearly labeled suggestions to neighboring roles but may not perform their decisions.",
        CouncilRoleBoundaryMode.Strict => "The participant may speak and act only for this role; narration, rulings, commands and outcomes belonging to another role are forbidden.",
        _ => "The participant remains inside this role and may reference other roles without deciding their actions or outcomes."
    };

    private string RoleLanguageExplanation(OrganicCouncilRoleDefinition role) => role.LanguageMode switch
    {
        CouncilRoleLanguageMode.SenderLanguage => "Visible output and exposed thinking should follow the latest human sender's language when the model can do so.",
        CouncilRoleLanguageMode.English => "Visible output and exposed thinking are requested in English.",
        _ => "The model may choose the most suitable response language."
    };

    private string RoleCoordinationExplanation(OrganicCouncilRoleDefinition role)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(role.DistinctAiAssignmentGroup))
            details.Add($"models are kept distinct from other roles in group '{role.DistinctAiAssignmentGroup}'");
        if (!string.IsNullOrWhiteSpace(role.MatchAiParticipantCountToRole))
            details.Add($"AI count follows role '{role.MatchAiParticipantCountToRole}'");
        if (!string.IsNullOrWhiteSpace(role.PairedRole))
        {
            details.Add($"members pair one-to-one with role '{role.PairedRole}'");
            var pairedRole = FindRolePolicy(role.PairedRole);
            if (pairedRole is not null &&
                !string.IsNullOrWhiteSpace(role.DistinctAiAssignmentGroup) &&
                string.Equals(pairedRole.DistinctAiAssignmentGroup, role.DistinctAiAssignmentGroup, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(pairedRole.MatchAiParticipantCountToRole, role.Role, StringComparison.OrdinalIgnoreCase))
            {
                details.Add("one distinct model slot is reserved for each paired member");
            }
        }
        return CouncilText.BuildRoleCoordinationExplanation(details, Logger);
    }

    private string WorkflowLoopLabel(CouncilWorkflowStepDefinition step) =>
        string.IsNullOrWhiteSpace(step.LoopGroup)
            ? "single pass"
            : $"loop {step.LoopGroup}, max {Math.Max(1, step.MaximumLoopIterations)}" +
              (string.IsNullOrWhiteSpace(step.LoopCompletionMarker) ? string.Empty : $", marker {step.LoopCompletionMarker}");

    private int CalculateExpandedRoundCount(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
        var ordered = steps
            .Where(step => step.IsEnabled)
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var total = 0;
        for (var index = 0; index < ordered.Count;)
        {
            var step = ordered[index];
            if (string.IsNullOrWhiteSpace(step.LoopGroup))
            {
                total += Math.Max(1, step.RepeatCount);
                index++;
                continue;
            }

            var loopGroup = step.LoopGroup;
            var blockRounds = 0;
            var maximumIterations = 1;
            while (index < ordered.Count && string.Equals(ordered[index].LoopGroup, loopGroup, StringComparison.OrdinalIgnoreCase))
            {
                blockRounds += Math.Max(1, ordered[index].RepeatCount);
                maximumIterations = Math.Max(maximumIterations, Math.Max(1, ordered[index].MaximumLoopIterations));
                index++;
            }
            total += blockRounds * maximumIterations;
        }
        return total;
    }

    private string WorkflowLabel(OrganicCouncilTeamDefinition team) =>
        UsesDefaultWorkflow(team) ? "LocalGPT default orchestration" : "literal custom workflow";

    private bool UsesDefaultWorkflow(OrganicCouncilTeamDefinition team)
    {
        if (!team.IsSystemSeed || team.IsUserModified)
            return false;
        var expected = new Dictionary<string, (int SortOrder, string ExecutionMode)>(StringComparer.OrdinalIgnoreCase)
        {
            ["member-readiness-introduction"] = (5, "AllMembersParallel"),
            ["expert-preparation"] = (10, "LeaderSingle"),
            ["leader-synthesis"] = (20, "LeaderSingle"),
            ["member-proposals"] = (30, "AllMembersParallel"),
            ["peer-review"] = (40, "AllMembersParallel"),
            ["consensus"] = (50, "LeaderSingle")
        };
        var enabled = team.WorkflowSteps.Where(step => step.IsEnabled).ToList();
        return enabled.Count == expected.Count && enabled.All(step =>
            step.UseBuiltInBehavior &&
            step.RepeatCount == 1 &&
            expected.TryGetValue(step.Key, out var contract) &&
            step.SortOrder == contract.SortOrder &&
            string.Equals(step.ExecutionMode, contract.ExecutionMode, StringComparison.OrdinalIgnoreCase));
    }

    private OrganicCouncilTeamDefinition Clone(OrganicCouncilTeamDefinition value) =>
        System.Text.Json.JsonSerializer.Deserialize<OrganicCouncilTeamDefinition>(Serialize(value), JsonOptions) ?? new();

    private string Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, JsonOptions);

    private T Deserialize<T>(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new System.Text.Json.JsonException("The JSON value is empty.");

    
    }
}
