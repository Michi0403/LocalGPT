using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates multi model council behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MultiModelCouncilService : IMultiModelCouncilService
    {
        /// <summary>
        /// Stores the local GPT vocabulary service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptVocabularyService vocabulary;
        /// <summary>
        /// Stores the options monitor dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot;
        /// <summary>
        /// Stores the AI context bootstrap service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IAiContextBootstrapService bootstrapService;
        /// <summary>
        /// Stores the chat memory service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IChatMemoryService chatMemory;
        /// <summary>
        /// Stores the council artifact service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilArtifactService artifactService;
        /// <summary>
        /// Stores the council knowledge service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilKnowledgeService knowledgeService;
        /// <summary>
        /// Stores the local GPT project service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ILocalGptProjectService projectService;
        /// <summary>
        /// Stores the project architecture service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IProjectArchitectureService projectArchitecture;
        /// <summary>
        /// Stores the code generation workflow service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICodeGenerationWorkflowService codeGenerationWorkflow;
        /// <summary>
        /// Stores the council code generation plan service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilCodeGenerationPlanService codeGenerationPlanService;
        /// <summary>
        /// Stores the human collaboration service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IHumanCollaborationService humanCollaboration;
        /// <summary>
        /// Stores the council x round service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilXRoundService councilXRounds;
        /// <summary>
        /// Stores the deferred DevExpress AI invocation service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDeferredDxAiInvocationService deferredDxAiInvocations;
        /// <summary>
        /// Stores the organic council blueprint service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IOrganicCouncilBlueprintService organicCouncilBlueprints;
        /// <summary>
        /// Stores the council spooler service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilSpoolerService councilSpooler;
        /// <summary>
        /// Stores the council preflight service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilPreflightService councilPreflight;
        /// <summary>
        /// Stores the council automatic function policy service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilAutomaticFunctionPolicyService councilAutomaticFunctionPolicy;
        /// <summary>
        /// Stores the council DevExpress function policy data service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilDxFunctionPolicyDataService councilDxPolicy;
        /// <summary>
        /// Stores the council DevExpress function orchestrator dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilDxFunctionOrchestrator councilDxFunctions;
        /// <summary>
        /// Stores the council hardware road planner dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilHardwareRoadPlanner hardwareRoadPlanner;
        /// <summary>
        /// Stores the council run configuration service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilRunConfigurationService runConfigurations;
        /// <summary>
        /// Stores the model capability self assessment service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IModelCapabilitySelfAssessmentService modelSelfAssessment;
        /// <summary>
        /// Stores the AI feature report service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IAiFeatureReportService featureReports;
        /// <summary>
        /// Stores the ambient local GPT context dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IAmbientLocalGptContext ambientContext;
        /// <summary>
        /// Stores the council live session service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilLiveSessionService liveCouncilSessions;
        /// <summary>
        /// Stores the council benchmark calibration service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilBenchmarkCalibrationService benchmarkCalibration;
        /// <summary>
        /// Stores the provider model runtime service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IProviderModelRuntimeService providerModels;
        /// <summary>
        /// Stores the logger used by <see cref="MultiModelCouncilService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<MultiModelCouncilService> logger;
        /// <summary>
        /// Stores the council runtime service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilRuntimeService councilRuntime;
        /// <summary>
        /// Stores the council text service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly CouncilTextService councilText;
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="MultiModelCouncilService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService catalog;

        /// <summary>
        /// Initializes the service with its dependency-injected collaborators.
        /// </summary>
        /// <param name="vocabulary">Injected dependency used by the service.</param>
        /// <param name="optionsRoot">Injected dependency used by the service.</param>
        /// <param name="bootstrapService">Injected dependency used by the service.</param>
        /// <param name="chatMemory">Injected dependency used by the service.</param>
        /// <param name="artifactService">Injected dependency used by the service.</param>
        /// <param name="knowledgeService">Injected dependency used by the service.</param>
        /// <param name="projectService">Injected dependency used by the service.</param>
        /// <param name="projectArchitecture">Injected dependency used by the service.</param>
        /// <param name="codeGenerationWorkflow">Injected dependency used by the service.</param>
        /// <param name="codeGenerationPlanService">Injected dependency used by the service.</param>
        /// <param name="humanCollaboration">Injected dependency used by the service.</param>
        /// <param name="councilXRounds">Injected dependency used by the service.</param>
        /// <param name="deferredDxAiInvocations">Injected dependency used by the service.</param>
        /// <param name="organicCouncilBlueprints">Injected dependency used by the service.</param>
        /// <param name="councilSpooler">Injected dependency used by the service.</param>
        /// <param name="councilPreflight">Injected dependency used by the service.</param>
        /// <param name="councilAutomaticFunctionPolicy">Injected dependency used by the service.</param>
        /// <param name="councilDxPolicy">Injected dependency used by the service.</param>
        /// <param name="councilDxFunctions">Injected dependency used by the service.</param>
        /// <param name="hardwareRoadPlanner">Injected dependency used by the service.</param>
        /// <param name="runConfigurations">Injected dependency used by the service.</param>
        /// <param name="modelSelfAssessment">Injected dependency used by the service.</param>
        /// <param name="featureReports">Injected dependency used by the service.</param>
        /// <param name="ambientContext">Injected dependency used by the service.</param>
        /// <param name="liveCouncilSessions">Injected dependency used by the service.</param>
        /// <param name="benchmarkCalibration">Injected dependency used by the service.</param>
        /// <param name="providerModels">Injected dependency used by the service.</param>
        /// <param name="logger">Injected dependency used by the service.</param>
        /// <param name="councilRuntime">Injected dependency used by the service.</param>
        /// <param name="councilText">Injected dependency used by the service.</param>
        /// <param name="catalog">Injected dependency used by the service.</param>
        public MultiModelCouncilService(
            ILocalGptVocabularyService vocabulary,
            IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot,
            IAiContextBootstrapService bootstrapService,
            IChatMemoryService chatMemory,
            ICouncilArtifactService artifactService,
            ICouncilKnowledgeService knowledgeService,
            ILocalGptProjectService projectService,
            IProjectArchitectureService projectArchitecture,
            ICodeGenerationWorkflowService codeGenerationWorkflow,
            ICouncilCodeGenerationPlanService codeGenerationPlanService,
            IHumanCollaborationService humanCollaboration,
            ICouncilXRoundService councilXRounds,
            IDeferredDxAiInvocationService deferredDxAiInvocations,
            IOrganicCouncilBlueprintService organicCouncilBlueprints,
            ICouncilSpoolerService councilSpooler,
            ICouncilPreflightService councilPreflight,
            ICouncilAutomaticFunctionPolicyService councilAutomaticFunctionPolicy,
            ICouncilDxFunctionPolicyDataService councilDxPolicy,
            ICouncilDxFunctionOrchestrator councilDxFunctions,
            ICouncilHardwareRoadPlanner hardwareRoadPlanner,
            ICouncilRunConfigurationService runConfigurations,
            IModelCapabilitySelfAssessmentService modelSelfAssessment,
            IAiFeatureReportService featureReports,
            IAmbientLocalGptContext ambientContext,
            ICouncilLiveSessionService liveCouncilSessions,
            ICouncilBenchmarkCalibrationService benchmarkCalibration,
            IProviderModelRuntimeService providerModels,
            ILogger<MultiModelCouncilService> logger,
            CouncilRuntimeService councilRuntime,
            CouncilTextService councilText,
            LocalGptCatalogService catalog)
        {
            this.vocabulary = vocabulary;
            this.optionsRoot = optionsRoot;
            this.bootstrapService = bootstrapService;
            this.chatMemory = chatMemory;
            this.artifactService = artifactService;
            this.knowledgeService = knowledgeService;
            this.projectService = projectService;
            this.projectArchitecture = projectArchitecture;
            this.codeGenerationWorkflow = codeGenerationWorkflow;
            this.codeGenerationPlanService = codeGenerationPlanService;
            this.humanCollaboration = humanCollaboration;
            this.councilXRounds = councilXRounds;
            this.deferredDxAiInvocations = deferredDxAiInvocations;
            this.organicCouncilBlueprints = organicCouncilBlueprints;
            this.councilSpooler = councilSpooler;
            this.councilPreflight = councilPreflight;
            this.councilAutomaticFunctionPolicy = councilAutomaticFunctionPolicy;
            this.councilDxPolicy = councilDxPolicy;
            this.councilDxFunctions = councilDxFunctions;
            this.hardwareRoadPlanner = hardwareRoadPlanner;
            this.runConfigurations = runConfigurations;
            this.modelSelfAssessment = modelSelfAssessment;
            this.featureReports = featureReports;
            this.ambientContext = ambientContext;
            this.liveCouncilSessions = liveCouncilSessions;
            this.benchmarkCalibration = benchmarkCalibration;
            this.providerModels = providerModels;
            this.logger = logger;
            this.councilRuntime = councilRuntime;
            this.councilText = councilText;
            this.catalog = catalog;
        }


        /// <summary>
        /// Retrieves candidates as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        public Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default) {
    try
    {
        return providerModels.GetCandidatesAsync(cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(GetCandidatesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(GetCandidatesAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Applies configured team model bindings as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task ApplyConfiguredTeamModelBindingsAsync(
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            CancellationToken cancellationToken)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                ArgumentNullException.ThrowIfNull(team);
                request.ModelNames ??= [];

                var hasSavedBindings = team.Roles.Any(role =>
                        role.HumanParticipationMode != HumanParticipationMode.HumanOnly &&
                        (role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange) &&
                        role.AssignedModelKeys is { Count: > 0 }) ||
                    team.WorkflowSteps.Any(step =>
                        step.IsEnabled &&
                        string.Equals(NormalizeConfiguredExecutionMode(step.ExecutionMode), "AssignedModelSingle", StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(step.AssignedModelName)) ||
                    team.WorkflowSteps.Any(step =>
                        step.IsEnabled &&
                        step.XFunctionsEnabled &&
                        step.XCanStartSingleModel &&
                        !string.IsNullOrWhiteSpace(step.XChildModelName)) ||
                    team.WorkflowSteps.Any(step =>
                        step.IsEnabled &&
                        step.SummarizeRoleResults &&
                        step.RoleResultSynthesisMemberMode == CouncilRoleResultSynthesisMemberMode.AssignedRoleMember &&
                        !string.IsNullOrWhiteSpace(step.RoleResultSynthesisModelName));
                if (!hasSavedBindings)
                    return;

                var candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
                foreach (var role in team.Roles.Where(role => role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange))
                {
                    role.AssignedModelKeys ??= [];
                    role.AssignedModelKeys = role.AssignedModelKeys
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => ResolveConfiguredTeamModelBinding(value, candidates, team, role.Role))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                foreach (var step in team.WorkflowSteps.Where(step =>
                             step.IsEnabled &&
                             string.Equals(NormalizeConfiguredExecutionMode(step.ExecutionMode), "AssignedModelSingle", StringComparison.Ordinal) &&
                             !string.IsNullOrWhiteSpace(step.AssignedModelName)))
                {
                    step.AssignedModelName = ResolveConfiguredTeamModelBinding(
                        step.AssignedModelName,
                        candidates,
                        team,
                        $"workflow step {step.DisplayName}");
                }

                foreach (var step in team.WorkflowSteps.Where(step =>
                             step.IsEnabled &&
                             step.XFunctionsEnabled &&
                             step.XCanStartSingleModel &&
                             !string.IsNullOrWhiteSpace(step.XChildModelName)))
                {
                    step.XChildModelName = ResolveConfiguredTeamModelBinding(
                        step.XChildModelName,
                        candidates,
                        team,
                        $"X-Function single-model target for {step.DisplayName}");
                }

                foreach (var step in team.WorkflowSteps.Where(step =>
                             step.IsEnabled &&
                             step.SummarizeRoleResults &&
                             step.RoleResultSynthesisMemberMode == CouncilRoleResultSynthesisMemberMode.AssignedRoleMember &&
                             !string.IsNullOrWhiteSpace(step.RoleResultSynthesisModelName)))
                {
                    step.RoleResultSynthesisModelName = ResolveConfiguredTeamModelBinding(
                        step.RoleResultSynthesisModelName,
                        candidates,
                        team,
                        $"role-result summarizer for {step.DisplayName}");
                }

                var configuredBindings = team.Roles
                    .Where(role =>
                        role.HumanParticipationMode != HumanParticipationMode.HumanOnly &&
                        (role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange))
                    .SelectMany(role => role.AssignedModelKeys)
                    .Concat(team.WorkflowSteps
                        .Where(step =>
                            step.IsEnabled &&
                            string.Equals(NormalizeConfiguredExecutionMode(step.ExecutionMode), "AssignedModelSingle", StringComparison.Ordinal))
                        .Select(step => step.AssignedModelName))
                    .Concat(team.WorkflowSteps
                        .Where(step =>
                            step.IsEnabled &&
                            step.XFunctionsEnabled &&
                            step.XCanStartSingleModel)
                        .Select(step => step.XChildModelName))
                    .Concat(team.WorkflowSteps
                        .Where(step =>
                            step.IsEnabled &&
                            step.SummarizeRoleResults &&
                            step.RoleResultSynthesisMemberMode == CouncilRoleResultSynthesisMemberMode.AssignedRoleMember)
                        .Select(step => step.RoleResultSynthesisModelName))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var addedCount = 0;
                foreach (var modelKey in configuredBindings)
                {
                    if (request.ModelNames.Contains(modelKey, StringComparer.OrdinalIgnoreCase))
                        continue;
                    request.ModelNames.Add(modelKey);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    logger.LogInformation(
                        "Council team {TeamKey} added {AddedCount} exact provider-bound model identity or identities to run {RunId}; saved team assignments remain authoritative.",
                        team.Key,
                        addedCount,
                        request.RunId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not apply provider-bound model assignments for council team {TeamKey}.", team.Key);
                throw;
            }
        }

        /// <summary>
        /// Resolves configured team model binding as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="savedBinding">Saved binding value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="candidates">Multi model council model candidate dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="team">Team value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="roleOrStep">Role or step value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string ResolveConfiguredTeamModelBinding(
            string savedBinding,
            IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
            OrganicCouncilTeamDefinition team,
            string roleOrStep)
        {
            try
            {
                var normalizedBinding = savedBinding.Trim();
                var exact = candidates.FirstOrDefault(candidate =>
                    string.Equals(candidate.SelectionKey, normalizedBinding, StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                    return exact.SelectionKey;

                var legacyMatches = candidates
                    .Where(candidate => string.Equals(candidate.ModelName, normalizedBinding, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (legacyMatches.Count == 1)
                {
                    logger.LogInformation(
                        "Council team {TeamKey} resolved legacy bare model assignment {LegacyModel} for {RoleOrStep} to provider-qualified identity {SelectionKey} for this run.",
                        team.Key,
                        normalizedBinding,
                        roleOrStep,
                        legacyMatches[0].SelectionKey);
                    return legacyMatches[0].SelectionKey;
                }

                if (legacyMatches.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Council team '{team.DisplayName}' stores legacy model assignment '{normalizedBinding}' for '{roleOrStep}', but that model exists on multiple connected providers/hosts. Open Council Teams and bind the exact provider-qualified model; LocalGPT will not guess a host.");
                }

                return normalizedBinding;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Could not resolve saved Council model binding {SavedBinding} for team {TeamKey}, role or step {RoleOrStep}.",
                    savedBinding,
                    team.Key,
                    roleOrStep);
                throw;
            }
        }
}
}
