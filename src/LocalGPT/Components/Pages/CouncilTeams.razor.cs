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
    private readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly IReadOnlyList<(string Value, string Label)> ExecutionModes =
    [
        ("AllMembersParallel", "All role members in parallel"),
        ("AllMembersSequentialOnEachAIHostParallel", "One role member per AI host at a time; AI hosts in parallel"),
        ("AllMembersSequential", "All role members sequentially"),
        ("LeaderSingle", "Council leader or one role member"),
        ("RoundRobinSingle", "One rotating role member"),
        ("AssignedModelSingle", "Assigned model only"),
        ("SystemBenchmarkCalibration", "LocalGPT all-member benchmark calibration engine")
    ];
    private readonly IReadOnlyList<(CouncilAllMembersReadinessPreflightMode Value, string Label)> AllMembersReadinessPreflightModes =
    [
        (CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault, "Legacy compatibility (built-in readiness only)"),
        (CouncilAllMembersReadinessPreflightMode.Disabled, "Disabled"),
        (CouncilAllMembersReadinessPreflightMode.RoleAwareProbe, "Role-aware probe for every selected member")
    ];
    private readonly IReadOnlyList<(CouncilAutomaticFunctionPolicyMode Value, string Label)> AutomaticFunctionPolicyModes =
    [
        (CouncilAutomaticFunctionPolicyMode.Disabled, "Disabled — expose no automatic/native tools"),
        (CouncilAutomaticFunctionPolicyMode.AllPolicyApproved, "All registered functions allowed by LocalGPT safety policy"),
        (CouncilAutomaticFunctionPolicyMode.TeamAllowList, "Use this team's allow-list"),
        (CouncilAutomaticFunctionPolicyMode.ExactAllowList, "Use this step's exact allow-list")
    ];
    private readonly IReadOnlyList<(CouncilRoleResultSynthesisMemberMode Value, string Label)> RoleResultSynthesisMemberModes =
    [
        (CouncilRoleResultSynthesisMemberMode.DeterministicRandomRoleMember, "Random assigned role member (stable per run)"),
        (CouncilRoleResultSynthesisMemberMode.AssignedRoleMember, "One selected role member")
    ];
    private readonly IReadOnlyList<(CouncilTranscriptVisibilityMode Value, string Label)> TranscriptVisibilityModes =
    [
        (CouncilTranscriptVisibilityMode.FullCouncil, "Full Council transcript"),
        (CouncilTranscriptVisibilityMode.SameRole, "Only this role"),
        (CouncilTranscriptVisibilityMode.CurrentRound, "Only this logical round"),
        (CouncilTranscriptVisibilityMode.SameRoleCurrentRound, "This role in this logical round"),
        (CouncilTranscriptVisibilityMode.None, "No accumulated transcript")
    ];
    private readonly IReadOnlyList<(CouncilRoleAiSelectionMode Value, string Label)> AiSelectionModes =
    [
        (CouncilRoleAiSelectionMode.AllSelected, "All selected council AIs"),
        (CouncilRoleAiSelectionMode.RandomRange, "Random role members within range"),
        (CouncilRoleAiSelectionMode.AssignedModels, "All exact models from connected providers"),
        (CouncilRoleAiSelectionMode.AssignedModelsRandomRange, "Random count from exact provider pool (repeats allowed)")
    ];
    private readonly IReadOnlyList<(HumanParticipationMode Value, string Label)> HumanParticipationModes =
    [
        (HumanParticipationMode.None, "No human role"),
        (HumanParticipationMode.Optional, "Human may participate"),
        (HumanParticipationMode.Required, "Human response required"),
        (HumanParticipationMode.HumanOnly, "Human only; no AI")
    ];
    private readonly IReadOnlyList<(CouncilRolePerformanceMode Value, string Label)> PerformanceModes =
    [
        (CouncilRolePerformanceMode.TaskSpecialist, "Task specialist"),
        (CouncilRolePerformanceMode.ImprovisationPlayer, "Improvisation player / actor")
    ];
    private readonly IReadOnlyList<(CouncilRoleBoundaryMode Value, string Label)> BoundaryModes =
    [
        (CouncilRoleBoundaryMode.Bounded, "Bounded role"),
        (CouncilRoleBoundaryMode.Collaborative, "Collaborative role"),
        (CouncilRoleBoundaryMode.Strict, "Strict role ownership")
    ];
    private readonly IReadOnlyList<(CouncilRoleLanguageMode Value, string Label)> LanguageModes =
    [
        (CouncilRoleLanguageMode.ModelChoice, "Model chooses language"),
        (CouncilRoleLanguageMode.SenderLanguage, "Match latest human sender"),
        (CouncilRoleLanguageMode.English, "English")
    ];
    private IReadOnlyList<OrganicCouncilTeamDefinition> _teams = [];
    private IReadOnlyList<OrganicCouncilTeamDefinition> _defaultTemplates = [];
    private IReadOnlyList<CouncilRuntimeClassDefinition> _runtimeClasses = [];
    private IReadOnlyList<DxAiFunctionCatalogEntry> _dxFunctions = [];
    private IReadOnlyList<MultiModelCouncilModelCandidate> _providerModels = [];
    private OrganicCouncilTeamDefinition _editor = new();
    private string _selectedKey = string.Empty;
    private string _rolesJson = "[]";
    private string _workflowJson = "[]";
    private string _capabilitiesJson = "[]";
    private string _allowedFunctionsJson = "[]";
    private string _resetTemplateKey = string.Empty;
    private string _contractsJson = "[]";
    private string _status = string.Empty;
    private bool _hasError;
    private bool _confirmed;
    private bool _busy;
    private bool _dxFunctionPickerExpanded;
    private bool _dxFunctionsLoading;
    private bool _dxFunctionsLoaded;
    private bool _providerModelsRefreshing;

    private string PreflightModeLabel => AllMembersReadinessPreflightModes
        .FirstOrDefault(item => item.Value == _editor.AllMembersReadinessPreflightMode).Label
        ?? _editor.AllMembersReadinessPreflightMode.ToString();
    private int EnabledRoundCount => _editor.WorkflowSteps.Count(step => step.IsEnabled);
    private int ExpandedRoundCount => CalculateExpandedRoundCount(_editor.WorkflowSteps);
    private IEnumerable<DxAiFunctionCatalogEntry> AvailableDxFunctions => _dxFunctions
        .Where(entry => entry.IsAvailable && entry.IsEnabled && !string.IsNullOrWhiteSpace(entry.FunctionName));
    private HashSet<string> RecommendedRuntimeFunctions => _runtimeClasses
        .Where(runtimeClass => _editor.Roles.Any(role => role.RuntimeClassKeys.Contains(runtimeClass.Key, StringComparer.OrdinalIgnoreCase)))
        .SelectMany(runtimeClass => runtimeClass.RecommendedDxFunctions)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        _busy = true;
        try
        {
            var runtimeClassesTask = RuntimeClasses.GetDefinitionsAsync(includeDisabled: true);
            var teamsTask = TeamConfigurations.GetTeamsAsync(includeDisabled: true);
            var templatesTask = TeamConfigurations.GetDefaultTemplatesAsync();
            var runtimeClasses = await runtimeClassesTask.ConfigureAwait(false);
            var teams = await teamsTask.ConfigureAwait(false);
            var templates = await templatesTask.ConfigureAwait(false);

            await InvokeAsync(() =>
            {
                _runtimeClasses = runtimeClasses;
                _teams = teams;
                _defaultTemplates = templates;
                if (string.IsNullOrWhiteSpace(_resetTemplateKey) || !_defaultTemplates.Any(template => template.Key == _resetTemplateKey))
                    _resetTemplateKey = _defaultTemplates.FirstOrDefault()?.Key ?? string.Empty;
                var selected = _teams.FirstOrDefault(team => team.Key == _selectedKey) ?? _teams.FirstOrDefault();
                if (selected is not null)
                    SelectTeam(selected);
                else
                    CreateTeam();
                _status = $"Loaded {_teams.Count} database-backed team configuration(s). Provider discovery continues in the background.";
                _hasError = false;
                Notifier.ShowInfo(nameof(CouncilTeams), _status, "Council Teams");
            }).ConfigureAwait(false);

            _ = RefreshProviderModelsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not load AI Council team configurations.");
            ComponentActivity.RecordFailure(nameof(CouncilTeams), nameof(ReloadAsync), ex);
            try
            {
                await InvokeAsync(() =>
                {
                    _status = ex.Message;
                    _hasError = true;
                    Notifier.ShowError(nameof(CouncilTeams), ex.Message, "Council Teams");
                }).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                Logger.LogDebug("Council Teams was disposed while reporting a load failure.");
            }
        }
        finally
        {
            try
            {
                await InvokeAsync(() => _busy = false).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                Logger.LogDebug("Council Teams was disposed before its busy state could be cleared.");
            }
        }
    }

    private async Task RefreshProviderModelsAsync()
    {
        if (_providerModelsRefreshing)
            return;

        _providerModelsRefreshing = true;
        try
        {
            var candidates = (await CouncilService.GetCandidatesAsync().ConfigureAwait(false))
                .OrderBy(candidate => candidate.Provider, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.Endpoint, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.ModelName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            await InvokeAsync(() => _providerModels = candidates).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            Logger.LogDebug("Council Teams provider discovery completed after the component was disposed.");
        }
        catch (Exception providerEx)
        {
            Logger.LogWarning(providerEx, "Could not refresh provider-qualified Council model candidates; team definitions remain editable with saved bindings.");
            try
            {
                await InvokeAsync(() => _providerModels = []).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                Logger.LogDebug("Council Teams was disposed while provider discovery failure state was being applied.");
            }
        }
        finally
        {
            _providerModelsRefreshing = false;
        }
    }

    private async Task ToggleDxFunctionPickerAsync()
    {
        _dxFunctionPickerExpanded = !_dxFunctionPickerExpanded;
        if (!_dxFunctionPickerExpanded || _dxFunctionsLoaded || _dxFunctionsLoading)
            return;

        await LoadDxFunctionCatalogAsync().ConfigureAwait(false);
    }

    private async Task LoadDxFunctionCatalogAsync()
    {
        if (_dxFunctionsLoaded || _dxFunctionsLoading)
            return;
        _dxFunctionsLoading = true;
        try
        {
            var entries = await FunctionCatalog.GetEntriesAsync().ConfigureAwait(false);
            await InvokeAsync(() =>
            {
                _dxFunctions = entries;
                _dxFunctionsLoaded = true;
                _dxFunctionsLoading = false;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not load the Council Teams DXFunction picker.");
            ComponentActivity.RecordFailure(nameof(CouncilTeams), nameof(ToggleDxFunctionPickerAsync), ex);
            try
            {
                await InvokeAsync(() =>
                {
                    _dxFunctionsLoading = false;
                    _status = "The DXFunction catalog could not be loaded. Team editing remains available.";
                    _hasError = true;
                }).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                Logger.LogDebug("Council Teams was disposed while the DXFunction picker failure was being applied.");
            }
        }
    }

    private void OnTeamChanged(ChangeEventArgs args)
    {
        var key = args.Value?.ToString() ?? string.Empty;
        var team = _teams.FirstOrDefault(item => item.Key == key);
        if (team is not null)
            SelectTeam(team);
    }

    private void SelectTeam(OrganicCouncilTeamDefinition team)
    {
        _selectedKey = team.Key;
        _editor = Clone(team);
        NormalizeEditorOrdering();
        RefreshAdvancedJson();
        _capabilitiesJson = Serialize(team.PreferredCapabilities);
        _allowedFunctionsJson = Serialize(team.AllowedAutomaticFunctions);
        _contractsJson = Serialize(team.ArchitectureContracts);
        _confirmed = false;
    }

    private void CreateTeam()
    {
        _selectedKey = string.Empty;
        _editor = new OrganicCouncilTeamDefinition
        {
            Key = "new-team",
            DisplayName = "New Council Team",
            Purpose = "A user-defined social AI structure.",
            IsEnabled = true,
            AllowedAutomaticFunctions = [],
            Roles =
            [
                new OrganicCouncilRoleDefinition
                {
                    Role = "Participant",
                    Expertise = "A viewpoint chosen by the user",
                    Responsibility = "Contribute according to the saved round prompt",
                    AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
                    MinimumAiParticipants = 1,
                    MaximumAiParticipants = 1
                }
            ],
            WorkflowSteps =
            [
                CreateWorkflowStep(0)
            ]
        };
        _capabilitiesJson = "[]";
        _allowedFunctionsJson = "[]";
        _contractsJson = "[]";
        RefreshAdvancedJson();
        _confirmed = false;
        _status = "Created an unsaved custom structure. Rename its key before saving if needed.";
        _hasError = false;
    }

    private void DuplicateTeam()
    {
        var source = _teams.FirstOrDefault(team => team.Key == _selectedKey) ?? _teams.FirstOrDefault();
        if (source is null)
        {
            CreateTeam();
            return;
        }

        _editor = Clone(source);
        _editor.Key = $"{source.Key}-custom";
        _editor.DisplayName = $"{source.DisplayName} custom";
        _editor.IsSystemSeed = false;
        _editor.IsUserModified = true;
        foreach (var step in _editor.WorkflowSteps)
            step.UseBuiltInBehavior = false;
        EnsureWorkflowRolesDefinedForEditableCopy();
        _selectedKey = string.Empty;
        RefreshAdvancedJson();
        _capabilitiesJson = Serialize(_editor.PreferredCapabilities);
        _allowedFunctionsJson = Serialize(_editor.AllowedAutomaticFunctions);
        _contractsJson = Serialize(_editor.ArchitectureContracts);
        _confirmed = false;
        _status = $"Duplicated '{source.DisplayName}' as an unsaved literal workflow. Change the key if it already exists.";
        _hasError = false;
    }

    private void EnsureWorkflowRolesDefinedForEditableCopy()
    {
        foreach (var step in _editor.WorkflowSteps.Where(step => step.IsEnabled && !string.IsNullOrWhiteSpace(step.Role)))
        {
            if (_editor.Roles.Any(role => string.Equals(role.Role, step.Role, StringComparison.OrdinalIgnoreCase)))
                continue;
            _editor.Roles.Add(new OrganicCouncilRoleDefinition
            {
                Role = step.Role.Trim(),
                Expertise = "Compatibility role from the supplied workflow",
                Responsibility = "Preserve the supplied workflow role explicitly when the team is converted to a user-owned literal workflow.",
                AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected,
                MinimumAiParticipants = 1,
                MaximumAiParticipants = 1,
                HumanParticipationMode = HumanParticipationMode.None
            });
        }
    }

    private void AddRole() => _editor.Roles.Add(new OrganicCouncilRoleDefinition
    {
        Role = $"Role {_editor.Roles.Count + 1}",
        Expertise = string.Empty,
        Responsibility = string.Empty,
        AiSelectionMode = CouncilRoleAiSelectionMode.RandomRange,
        MinimumAiParticipants = 1,
        MaximumAiParticipants = 1,
        HumanParticipationMode = HumanParticipationMode.None
    });

    private void RemoveRole(int index)
    {
        if (index >= 0 && index < _editor.Roles.Count)
            _editor.Roles.RemoveAt(index);
    }

    /// <summary>Applies one convenience X-Round policy without hiding any of the explicit per-step controls below it.</summary>
    private void ApplyXRoundPreset(CouncilWorkflowStepDefinition step, CouncilXRoundPreset preset)
    {
        ArgumentNullException.ThrowIfNull(step);
        step.UseBuiltInBehavior = false;
        step.XFunctionsEnabled = preset != CouncilXRoundPreset.Disabled;
        step.XCanRevisit = preset is CouncilXRoundPreset.Gatekeeper or CouncilXRoundPreset.ReactiveRevisit;
        step.XCanReturnText = preset is CouncilXRoundPreset.Gatekeeper or CouncilXRoundPreset.DerivedSingleModel or CouncilXRoundPreset.DerivedCouncil;
        step.XCanStartSingleModel = preset == CouncilXRoundPreset.DerivedSingleModel;
        step.XCanStartCouncil = preset == CouncilXRoundPreset.DerivedCouncil;
        if (preset == CouncilXRoundPreset.Disabled)
            step.XRequiresHumanApproval = false;
        step.XMaximumTransitions = preset == CouncilXRoundPreset.ReactiveRevisit ? 5 : 3;
        step.XMaximumChildCouncilDepth = Math.Max(1, step.XMaximumChildCouncilDepth);
        _confirmed = false;
        _status = preset == CouncilXRoundPreset.Disabled
            ? $"Cleared X-Round authority from '{step.DisplayName}'."
            : $"Applied the {preset} X-Round starting point to '{step.DisplayName}'. Review the explicit switches before saving.";
    }

    /// <summary>Lists the convenience starting points for configurable X-Round step policies.</summary>
    private enum CouncilXRoundPreset
    {
        Disabled,
        Gatekeeper,
        ReactiveRevisit,
        DerivedSingleModel,
        DerivedCouncil
    }

    private void AddWorkflowStep()
    {
        foreach (var existing in _editor.WorkflowSteps)
            existing.UseBuiltInBehavior = false;
        _editor.WorkflowSteps.Add(CreateWorkflowStep(_editor.WorkflowSteps.Count));
        NormalizeEditorOrdering();
    }

    private CouncilWorkflowStepDefinition CreateWorkflowStep(int index) => new()
    {
        Key = $"round-{index + 1}",
        DisplayName = $"Round {index + 1}",
        SortOrder = (index + 1) * 10,
        Phase = "Discussion",
        Role = "Participant",
        ExecutionMode = "AllMembersSequentialOnEachAIHostParallel",
        RepeatCount = 1,
        MaximumLoopIterations = 1,
        IncludePriorTranscript = true,
        IsEnabled = true,
        CanUseOrganicFunctions = true,
        AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.AllPolicyApproved,
        RoleComplianceRetryCount = 1,
        FinalAnswerRecoveryEnabled = true,
        FinalAnswerRecoveryMaxOutputTokens = 8192,
        XMaximumTransitions = 3,
        XMaximumChildCouncilDepth = 1,
        PromptTemplate = "Contribute to {{TeamName}} as {{Role}}. Address the original request, consider the prior transcript, and state disagreements or missing information plainly.\n\nUser request:\n{{UserPrompt}}\n\nPrior transcript:\n{{Transcript}}"
    };

    private void RemoveWorkflowStep(int index)
    {
        if (index < 0 || index >= _editor.WorkflowSteps.Count)
            return;
        _editor.WorkflowSteps.RemoveAt(index);
        MarkWorkflowCustom();
        NormalizeEditorOrdering();
    }

    private void MoveWorkflowStep(int index, int direction)
    {
        var target = index + direction;
        if (index < 0 || index >= _editor.WorkflowSteps.Count || target < 0 || target >= _editor.WorkflowSteps.Count)
            return;
        (_editor.WorkflowSteps[index], _editor.WorkflowSteps[target]) = (_editor.WorkflowSteps[target], _editor.WorkflowSteps[index]);
        MarkWorkflowCustom();
        NormalizeEditorOrdering();
    }

    private void MarkWorkflowCustom()
    {
        foreach (var step in _editor.WorkflowSteps)
            step.UseBuiltInBehavior = false;
    }

    private void NormalizeEditorOrdering()
    {
        foreach (var role in _editor.Roles)
        {
            role.MinimumAiParticipants = Math.Max(1, role.MinimumAiParticipants);
            role.MaximumAiParticipants = Math.Max(1, role.MaximumAiParticipants);
            if (role.MinimumAiParticipants > role.MaximumAiParticipants)
                role.MaximumAiParticipants = role.MinimumAiParticipants;
            if (!Enum.IsDefined(typeof(CouncilRoleAiSelectionMode), role.AiSelectionMode))
                role.AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected;
            if (!Enum.IsDefined(typeof(HumanParticipationMode), role.HumanParticipationMode))
                role.HumanParticipationMode = HumanParticipationMode.None;
            if (!Enum.IsDefined(typeof(CouncilRolePerformanceMode), role.PerformanceMode))
                role.PerformanceMode = CouncilRolePerformanceMode.TaskSpecialist;
            if (!Enum.IsDefined(typeof(CouncilRoleLanguageMode), role.LanguageMode))
                role.LanguageMode = CouncilRoleLanguageMode.ModelChoice;
            if (!Enum.IsDefined(typeof(CouncilRoleBoundaryMode), role.BoundaryMode))
                role.BoundaryMode = CouncilRoleBoundaryMode.Bounded;
            role.DistinctAiAssignmentGroup = role.DistinctAiAssignmentGroup?.Trim() ?? string.Empty;
            role.MatchAiParticipantCountToRole = role.MatchAiParticipantCountToRole?.Trim() ?? string.Empty;
            role.PairedRole = role.PairedRole?.Trim() ?? string.Empty;
            role.RuntimeClassKeys ??= [];
            role.RuntimeClassKeys = role.RuntimeClassKeys
                .Select(value => value?.Trim().ToLowerInvariant() ?? string.Empty)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            role.AssignedModelKeys ??= [];
            role.AssignedModelKeys = role.AssignedModelKeys
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        for (var index = 0; index < _editor.WorkflowSteps.Count; index++)
        {
            var step = _editor.WorkflowSteps[index];
            if (step.SortOrder == 0)
                step.SortOrder = (index + 1) * 10;
            step.RepeatCount = Math.Clamp(step.RepeatCount, 1, 100);
            step.LogicalRoundNumber = Math.Clamp(step.LogicalRoundNumber, 0, 1000);
            if (!Enum.IsDefined(typeof(CouncilTranscriptVisibilityMode), step.TranscriptVisibility))
                step.TranscriptVisibility = CouncilTranscriptVisibilityMode.FullCouncil;
            if (!Enum.IsDefined(typeof(CouncilAutomaticFunctionPolicyMode), step.AutomaticFunctionPolicyMode) || step.AutomaticFunctionPolicyMode == CouncilAutomaticFunctionPolicyMode.Legacy)
                step.AutomaticFunctionPolicyMode = step.CanUseOrganicFunctions
                    ? step.AllowedAutomaticFunctions is { Count: > 0 } ? CouncilAutomaticFunctionPolicyMode.ExactAllowList : CouncilAutomaticFunctionPolicyMode.AllPolicyApproved
                    : CouncilAutomaticFunctionPolicyMode.Disabled;
            step.CanUseOrganicFunctions = step.AutomaticFunctionPolicyMode != CouncilAutomaticFunctionPolicyMode.Disabled;
            step.AllowedAutomaticFunctions ??= [];
            step.AllowedAutomaticFunctions = step.AllowedAutomaticFunctions.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            step.RoleComplianceRetryCount = Math.Clamp(step.RoleComplianceRetryCount, 0, 3);
            step.FinalAnswerRecoveryMaxOutputTokens = Math.Clamp(step.FinalAnswerRecoveryMaxOutputTokens, 128, 32768);
            step.LoopGroup = step.LoopGroup?.Trim() ?? string.Empty;
            step.MaximumLoopIterations = string.IsNullOrWhiteSpace(step.LoopGroup)
                ? 1
                : Math.Clamp(step.MaximumLoopIterations, 1, 100);
            step.LoopCompletionMarker = step.LoopCompletionMarker?.Trim() ?? string.Empty;
            step.XMaximumTransitions = Math.Clamp(step.XMaximumTransitions, 1, 100);
            step.XMaximumChildCouncilDepth = Math.Clamp(step.XMaximumChildCouncilDepth, 1, 10);
            step.XDefaultTargetStepKey = step.XDefaultTargetStepKey?.Trim().ToLowerInvariant() ?? string.Empty;
            step.XChildCouncilTeamKey = step.XChildCouncilTeamKey?.Trim().ToLowerInvariant() ?? string.Empty;
            step.XChildModelName = step.XChildModelName?.Trim() ?? string.Empty;
            step.AsciiFrameWidth = Math.Clamp(step.AsciiFrameWidth, 20, 240);
            step.AsciiFrameHeight = Math.Clamp(step.AsciiFrameHeight, 8, 120);
            step.WorldStepScale = Math.Clamp(step.WorldStepScale, 1, 1000);
        }
    }

    private void RefreshAdvancedJson()
    {
        _rolesJson = Serialize(_editor.Roles);
        _workflowJson = Serialize(_editor.WorkflowSteps);
    }

    private void ApplyAdvancedJson()
    {
        try
        {
            _editor.Roles = Deserialize<List<OrganicCouncilRoleDefinition>>(_rolesJson);
            _editor.WorkflowSteps = Deserialize<List<CouncilWorkflowStepDefinition>>(_workflowJson);
            MarkWorkflowCustom();
            NormalizeEditorOrdering();
            _status = "Applied the advanced JSON to the visual editor. Review the resulting roles and rounds before saving.";
            _hasError = false;
            _confirmed = false;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _status = ex.Message;
            _hasError = true;
            Notifier.ShowError(nameof(CouncilTeams), ex.Message, "Council JSON rejected");
        }
    }
}
}
