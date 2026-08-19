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
    /// <summary>
    /// Renders the council teams Razor component and coordinates the component-local state, commands, and presentation behavior used by the surrounding LocalGPT interface.
    /// </summary>
    public partial class CouncilTeams
    {
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true };
    /// <summary>
    /// Stores the in-memory execution modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
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
    /// <summary>
    /// Stores the in-memory all members readiness preflight modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilAllMembersReadinessPreflightMode Value, string Label)> AllMembersReadinessPreflightModes =
    [
        (CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault, "Legacy compatibility (built-in readiness only)"),
        (CouncilAllMembersReadinessPreflightMode.Disabled, "Disabled"),
        (CouncilAllMembersReadinessPreflightMode.RoleAwareProbe, "Role-aware probe for every selected member")
    ];
    /// <summary>
    /// Stores the in-memory automatic function policy modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilAutomaticFunctionPolicyMode Value, string Label)> AutomaticFunctionPolicyModes =
    [
        (CouncilAutomaticFunctionPolicyMode.Disabled, "Disabled — expose no automatic/native tools"),
        (CouncilAutomaticFunctionPolicyMode.AllPolicyApproved, "All registered functions allowed by LocalGPT safety policy"),
        (CouncilAutomaticFunctionPolicyMode.TeamAllowList, "Use this team's allow-list"),
        (CouncilAutomaticFunctionPolicyMode.ExactAllowList, "Use this step's exact allow-list")
    ];
    /// <summary>
    /// Stores the in-memory role result synthesis member modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilRoleResultSynthesisMemberMode Value, string Label)> RoleResultSynthesisMemberModes =
    [
        (CouncilRoleResultSynthesisMemberMode.DeterministicRandomRoleMember, "Random assigned role member (stable per run)"),
        (CouncilRoleResultSynthesisMemberMode.AssignedRoleMember, "One selected role member")
    ];
    /// <summary>
    /// Stores the in-memory member failure recovery modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilMemberFailureRecoveryMode Value, string Label)> MemberFailureRecoveryModes =
    [
        (CouncilMemberFailureRecoveryMode.Disabled, "Disabled — preserve failure without automatic round repair"),
        (CouncilMemberFailureRecoveryMode.RetrySameMember, "Retry the same provider-qualified role member"),
        (CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool, "After same-member safe fallback, use another eligible member from this role pool")
    ];
    /// <summary>
    /// Stores the in-memory transcript visibility modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilTranscriptVisibilityMode Value, string Label)> TranscriptVisibilityModes =
    [
        (CouncilTranscriptVisibilityMode.FullCouncil, "Full Council transcript"),
        (CouncilTranscriptVisibilityMode.SameRole, "Only this role"),
        (CouncilTranscriptVisibilityMode.CurrentRound, "Only this logical round"),
        (CouncilTranscriptVisibilityMode.SameRoleCurrentRound, "This role in this logical round"),
        (CouncilTranscriptVisibilityMode.None, "No accumulated transcript")
    ];
    /// <summary>
    /// Stores the in-memory AI selection modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilRoleAiSelectionMode Value, string Label)> AiSelectionModes =
    [
        (CouncilRoleAiSelectionMode.AllSelected, "All selected council AIs"),
        (CouncilRoleAiSelectionMode.RandomRange, "Random role members within range"),
        (CouncilRoleAiSelectionMode.AssignedModels, "All exact models from connected providers"),
        (CouncilRoleAiSelectionMode.AssignedModelsRandomRange, "Random count from exact provider pool (repeats allowed)")
    ];
    /// <summary>
    /// Stores the in-memory human participation modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(HumanParticipationMode Value, string Label)> HumanParticipationModes =
    [
        (HumanParticipationMode.None, "No human role"),
        (HumanParticipationMode.Optional, "Human may participate"),
        (HumanParticipationMode.Required, "Human response required"),
        (HumanParticipationMode.HumanOnly, "Human only; no AI")
    ];
    /// <summary>
    /// Stores the in-memory performance modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilRolePerformanceMode Value, string Label)> PerformanceModes =
    [
        (CouncilRolePerformanceMode.TaskSpecialist, "Task specialist"),
        (CouncilRolePerformanceMode.ImprovisationPlayer, "Improvisation player / actor")
    ];
    /// <summary>
    /// Stores the in-memory boundary modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilRoleBoundaryMode Value, string Label)> BoundaryModes =
    [
        (CouncilRoleBoundaryMode.Bounded, "Bounded role"),
        (CouncilRoleBoundaryMode.Collaborative, "Collaborative role"),
        (CouncilRoleBoundaryMode.Strict, "Strict role ownership")
    ];
    /// <summary>
    /// Stores the in-memory language modes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private readonly IReadOnlyList<(CouncilRoleLanguageMode Value, string Label)> LanguageModes =
    [
        (CouncilRoleLanguageMode.ModelChoice, "Model chooses language"),
        (CouncilRoleLanguageMode.SenderLanguage, "Match latest human sender"),
        (CouncilRoleLanguageMode.English, "English")
    ];
    /// <summary>
    /// Stores the in-memory teams collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<OrganicCouncilTeamDefinition> _teams = [];
    /// <summary>
    /// Stores the in-memory default templates collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<OrganicCouncilTeamDefinition> _defaultTemplates = [];
    /// <summary>
    /// Stores the in-memory runtime classes collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<CouncilRuntimeClassDefinition> _runtimeClasses = [];
    /// <summary>
    /// Stores the in-memory DevExpress functions collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<DxAiFunctionCatalogEntry> _dxFunctions = [];
    /// <summary>
    /// Stores the in-memory provider models collection maintained internally by <see cref="CouncilTeams"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<MultiModelCouncilModelCandidate> _providerModels = [];
    /// <summary>
    /// Stores the internal editor state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private OrganicCouncilTeamDefinition _editor = new();
    /// <summary>
    /// Stores the internal selected key state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _selectedKey = string.Empty;
    /// <summary>
    /// Stores the internal roles JSON state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _rolesJson = "[]";
    /// <summary>
    /// Stores the internal workflow JSON state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _workflowJson = "[]";
    /// <summary>
    /// Stores the internal capabilities JSON state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _capabilitiesJson = "[]";
    /// <summary>
    /// Stores the internal allowed functions JSON state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _allowedFunctionsJson = "[]";
    /// <summary>
    /// Stores the internal reset template key state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _resetTemplateKey = string.Empty;
    /// <summary>
    /// Stores the internal contracts JSON state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _contractsJson = "[]";
    /// <summary>
    /// Stores the internal status state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private string _status = string.Empty;
    /// <summary>
    /// Stores the internal has error state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _hasError;
    /// <summary>
    /// Stores the internal confirmed state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _confirmed;
    /// <summary>
    /// Stores the internal busy state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _busy;
    /// <summary>
    /// Stores the internal DevExpress function picker expanded state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _dxFunctionPickerExpanded;
    /// <summary>
    /// Stores the internal DevExpress functions loading state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _dxFunctionsLoading;
    /// <summary>
    /// Stores the internal DevExpress functions loaded state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _dxFunctionsLoaded;
    /// <summary>
    /// Stores the internal provider models refreshing state used by <see cref="CouncilTeams"/> while executing its surrounding workflow.
    /// </summary>
    private bool _providerModelsRefreshing;

    /// <summary>
    /// Gets the preflight mode label value that forms part of the council teams state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The preflight mode label value exposed by <see cref="CouncilTeams"/>.</value>
    private string PreflightModeLabel => AllMembersReadinessPreflightModes
        .FirstOrDefault(item => item.Value == _editor.AllMembersReadinessPreflightMode).Label
        ?? _editor.AllMembersReadinessPreflightMode.ToString();
    /// <summary>
    /// Gets the enabled round count that quantifies the associated council teams data.
    /// </summary>
    /// <value>The enabled round count value exposed by <see cref="CouncilTeams"/>.</value>
    private int EnabledRoundCount => _editor.WorkflowSteps.Count(step => step.IsEnabled);
    /// <summary>
    /// Gets the expanded round count that quantifies the associated council teams data.
    /// </summary>
    /// <value>The expanded round count value exposed by <see cref="CouncilTeams"/>.</value>
    private int ExpandedRoundCount => CalculateExpandedRoundCount(_editor.WorkflowSteps);
    /// <summary>
    /// Gets the available DevExpress functions collection maintained or exposed by this council teams instance for downstream processing.
    /// </summary>
    /// <value>The available DevExpress functions value exposed by <see cref="CouncilTeams"/>.</value>
    private IEnumerable<DxAiFunctionCatalogEntry> AvailableDxFunctions => _dxFunctions
        .Where(entry => entry.IsAvailable && entry.IsEnabled && !string.IsNullOrWhiteSpace(entry.FunctionName));
    /// <summary>
    /// Gets the recommended runtime functions collection maintained or exposed by this council teams instance for downstream processing.
    /// </summary>
    /// <value>The recommended runtime functions value exposed by <see cref="CouncilTeams"/>.</value>
    private HashSet<string> RecommendedRuntimeFunctions => _runtimeClasses
        .Where(runtimeClass => _editor.Roles.Any(role => role.RuntimeClassKeys.Contains(runtimeClass.Key, StringComparer.OrdinalIgnoreCase)))
        .SelectMany(runtimeClass => runtimeClass.RecommendedDxFunctions)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Handles the initialized async lifecycle or event notification for <see cref="CouncilTeams"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override Task OnInitializedAsync() => ReloadAsync();

    /// <summary>
    /// Performs reload for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Refreshes provider models for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs toggle DevExpress function picker for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ToggleDxFunctionPickerAsync()
    {
        _dxFunctionPickerExpanded = !_dxFunctionPickerExpanded;
        if (!_dxFunctionPickerExpanded || _dxFunctionsLoaded || _dxFunctionsLoading)
            return;

        await LoadDxFunctionCatalogAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Loads DevExpress function catalog for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Handles the team changed lifecycle or event notification for <see cref="CouncilTeams"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the council teams operation and used when producing its result.</param>
    private void OnTeamChanged(ChangeEventArgs args)
    {
        var key = args.Value?.ToString() ?? string.Empty;
        var team = _teams.FirstOrDefault(item => item.Key == key);
        if (team is not null)
            SelectTeam(team);
    }

    /// <summary>
    /// Performs select team for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <param name="team">Team value supplied to the council teams operation and used when producing its result.</param>
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

    /// <summary>
    /// Creates team for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
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

    /// <summary>
    /// Performs duplicate team for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
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

    /// <summary>
    /// Ensures workflow roles defined for editable copy for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
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

    /// <summary>
    /// Adds role for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
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

    /// <summary>
    /// Removes role for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the council teams operation and used when producing its result.</param>
    private void RemoveRole(int index)
    {
        if (index >= 0 && index < _editor.Roles.Count)
            _editor.Roles.RemoveAt(index);
    }

    /// <summary>Applies one convenience X-Round policy without hiding any of the explicit per-step controls below it.</summary>
    /// <param name="step">Step value supplied to the council teams operation and used when producing its result.</param>
    /// <param name="preset">Preset value supplied to the council teams operation and used when producing its result.</param>
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
        /// <summary>
        /// Selects the disabled option for <see cref="CouncilXRoundPreset"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Disabled,
        /// <summary>
        /// Selects the gatekeeper option for <see cref="CouncilXRoundPreset"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Gatekeeper,
        /// <summary>
        /// Selects the reactive revisit option for <see cref="CouncilXRoundPreset"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        ReactiveRevisit,
        /// <summary>
        /// Selects the derived single model option for <see cref="CouncilXRoundPreset"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        DerivedSingleModel,
        /// <summary>
        /// Selects the derived council option for <see cref="CouncilXRoundPreset"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        DerivedCouncil
    }

    /// <summary>
    /// Adds workflow step for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    private void AddWorkflowStep()
    {
        foreach (var existing in _editor.WorkflowSteps)
            existing.UseBuiltInBehavior = false;
        _editor.WorkflowSteps.Add(CreateWorkflowStep(_editor.WorkflowSteps.Count));
        NormalizeEditorOrdering();
    }

    /// <summary>
    /// Creates workflow step for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the council teams operation and used when producing its result.</param>
    /// <returns>The council workflow step definition produced by the operation.</returns>
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
        MemberFailureRecoveryMode = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool,
        MemberFailureRecoveryAttempts = 3,
        FinalAnswerRecoveryEnabled = true,
        FinalAnswerRecoveryMaxOutputTokens = 8192,
        XMaximumTransitions = 3,
        XMaximumChildCouncilDepth = 1,
        PromptTemplate = "Contribute to {{TeamName}} as {{Role}}. Address the original request, consider the prior transcript, and state disagreements or missing information plainly.\n\nUser request:\n{{UserPrompt}}\n\nPrior transcript:\n{{Transcript}}"
    };

    /// <summary>
    /// Removes workflow step for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the council teams operation and used when producing its result.</param>
    private void RemoveWorkflowStep(int index)
    {
        if (index < 0 || index >= _editor.WorkflowSteps.Count)
            return;
        _editor.WorkflowSteps.RemoveAt(index);
        MarkWorkflowCustom();
        NormalizeEditorOrdering();
    }

    /// <summary>
    /// Performs move workflow step for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    /// <param name="index">Index value supplied to the council teams operation and used when producing its result.</param>
    /// <param name="direction">Direction value supplied to the council teams operation and used when producing its result.</param>
    private void MoveWorkflowStep(int index, int direction)
    {
        var target = index + direction;
        if (index < 0 || index >= _editor.WorkflowSteps.Count || target < 0 || target >= _editor.WorkflowSteps.Count)
            return;
        (_editor.WorkflowSteps[index], _editor.WorkflowSteps[target]) = (_editor.WorkflowSteps[target], _editor.WorkflowSteps[index]);
        MarkWorkflowCustom();
        NormalizeEditorOrdering();
    }

    /// <summary>
    /// Performs mark workflow custom for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    private void MarkWorkflowCustom()
    {
        foreach (var step in _editor.WorkflowSteps)
            step.UseBuiltInBehavior = false;
    }

    /// <summary>
    /// Normalizes editor ordering for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
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
            if (!Enum.IsDefined(typeof(CouncilMemberFailureRecoveryMode), step.MemberFailureRecoveryMode))
                step.MemberFailureRecoveryMode = CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool;
            step.MemberFailureRecoveryAttempts = Math.Clamp(step.MemberFailureRecoveryAttempts, 0, 8);
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

    /// <summary>
    /// Refreshes advanced JSON for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
    private void RefreshAdvancedJson()
    {
        _rolesJson = Serialize(_editor.Roles);
        _workflowJson = Serialize(_editor.WorkflowSteps);
    }

    /// <summary>
    /// Applies advanced JSON for <see cref="CouncilTeams"/>, keeping the operation consistent with the state and invariants of the surrounding council teams workflow.
    /// </summary>
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
