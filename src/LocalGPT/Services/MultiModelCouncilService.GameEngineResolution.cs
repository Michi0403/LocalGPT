using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Services
{
    /// <summary>
    /// Bridges configured Council workflow steps into the Council-driven game a run owns. The game's ruleset decides
    /// roles, evidence, assets and texts; this partial only runs the model turns and records the engine step.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Narrows a step marked <see cref="CouncilWorkflowStepDefinition.LimitToActiveGameEntrants"/> to the members of the
        /// entrants the run's game currently pairs. Any lookup problem keeps the configured role pool.
        /// </summary>
        /// <param name="result">Council run result whose game is consulted.</param>
        /// <param name="definition">Workflow step being prepared.</param>
        /// <param name="roleParticipants">Configured members of the step's role.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The members that take the step.</returns>
        private async Task<IReadOnlyList<string>> LimitGameEntrantParticipantsAsync(
            MultiModelCouncilResult result,
            CouncilWorkflowStepDefinition definition,
            IReadOnlyList<string> roleParticipants,
            CancellationToken cancellationToken)
        {
            if (!definition.LimitToActiveGameEntrants || roleParticipants.Count <= 2)
                return roleParticipants;

            try
            {
                return await gameCouncil.LimitToActiveEntrantsAsync(result.RunId, definition.Role, roleParticipants, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "The active game pairing for step {StepKey} of run {CouncilRunId} could not be resolved; the configured role pool runs unchanged.", definition.Key, result.RunId);
                return roleParticipants;
            }
        }

        /// <summary>Runs one <c>SystemGameEngineResolution</c> step and records it as an in-process engine transcript step.</summary>
        /// <param name="result">Council run result that receives the step.</param>
        /// <param name="request">Original Council request (progress reporting).</param>
        /// <param name="definition">Configured workflow step.</param>
        /// <param name="rolePairings">Runtime role pairings of the run.</param>
        /// <param name="participants">Active members of the run.</param>
        /// <param name="round">Logical round of the step.</param>
        /// <param name="phase">Phase label of the step.</param>
        /// <param name="initializeOnly"><c>true</c> when the step initializes the game (lineup).</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="drawAsset">Optional asset drawing turn used by the lineup's asset check.</param>
        /// <returns>The engine transcript step.</returns>
        private async Task<MultiModelCouncilStep> RunGameEngineSystemStepAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            CouncilWorkflowStepDefinition definition,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            IReadOnlyList<string> participants,
            int round,
            string phase,
            bool initializeOnly,
            CancellationToken cancellationToken,
            Func<GameAssetDraftRequest, CancellationToken, Task<GameAssetDraft?>>? drawAsset = null)
        {
            try
            {
                var startedAtUtc = DateTime.UtcNow;
                var pairings = rolePairings
                    .Select(pairing => (pairing.RoleName, pairing.Participant, pairing.PairedRoleName, pairing.PairedParticipant))
                    .ToList();
                var outcome = await gameCouncil.ResolveAsync(result.RunId, result.Steps, pairings, initializeOnly, drawAsset, cancellationToken).ConfigureAwait(false);
                var systemStep = new MultiModelCouncilStep
                {
                    SortOrder = result.Steps.Count + 1,
                    Round = round,
                    Phase = phase,
                    ModelName = outcome.EngineName,
                    ProviderName = "LocalGPT",
                    ProviderEndpoint = "in-process",
                    ProviderModelName = outcome.EngineModel,
                    CouncilMembers = participants.ToList(),
                    Role = definition.Role,
                    Content = outcome.SummaryMarkdown,
                    VisibleContent = outcome.SummaryMarkdown,
                    StartedAtUtc = startedAtUtc,
                    CompletedAtUtc = DateTime.UtcNow
                };
                systemStep.DurationSeconds = Math.Max(0d, (systemStep.CompletedAtUtc - systemStep.StartedAtUtc).TotalSeconds);
                if (!string.IsNullOrWhiteSpace(outcome.ProgressMessage))
                    request.ProgressMessage?.Invoke(outcome.ProgressMessage);
                logger.LogInformation(
                    "Council run {CouncilRunId} step {StepKey} resolved its game engine turn; initializeOnly={InitializeOnly}, completed={Completed}, gameEnded={GameEnded}. AI content was omitted.",
                    result.RunId,
                    definition.Key,
                    initializeOnly,
                    outcome.Completed,
                    outcome.GameEnded);
                return systemStep;
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(exception, "Game engine resolution was cancelled for Council run {CouncilRunId}.", result.RunId);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Game engine resolution failed for Council run {CouncilRunId}.", result.RunId);
                throw;
            }
        }

        /// <summary>
        /// Runs one asset drawing turn of a Council-driven game's lineup: attempt 0 asks the entrant's own member in the role
        /// of the game's self-drawing step, later attempts ask the members of the role of its artist step in order. Only
        /// members of the current run are asked.
        /// </summary>
        /// <param name="assetRequest">Entrant, other assets and attempt number.</param>
        /// <param name="result">Council result that receives the drawing turn as evidence.</param>
        /// <param name="request">Original Council request.</param>
        /// <param name="team">Configured team.</param>
        /// <param name="baseUri">Base URI of the running application.</param>
        /// <param name="participants">Active members of the run.</param>
        /// <param name="roleAssignments">Runtime role assignments of the run.</param>
        /// <param name="rolePairings">Runtime role pairings of the run.</param>
        /// <param name="round">Logical round of the lineup.</param>
        /// <param name="modelRoutes">Run-scoped hardware roads.</param>
        /// <param name="keepAlive">Provider keep-alive setting.</param>
        /// <param name="ollamaNumGpu">Ollama GPU layer setting.</param>
        /// <param name="maxContextTokens">Context token budget.</param>
        /// <param name="modelTimeoutSeconds">Per-model timeout.</param>
        /// <param name="automaticFunctionPolicy">Function policy of the lineup step (no tools are exposed to the drawing turn).</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The drawing, an empty drawing when the turn failed, or <see langword="null"/> when no further drawer exists.</returns>
        private async Task<GameAssetDraft?> DrawGameAssetAsync(
            GameAssetDraftRequest assetRequest,
            MultiModelCouncilResult result,
            MultiModelCouncilRequest request,
            OrganicCouncilTeamDefinition team,
            string baseUri,
            IReadOnlyList<string> participants,
            IDictionary<string, CouncilRoleRuntimeAssignment> roleAssignments,
            IReadOnlyList<CouncilParticipantPairing> rolePairings,
            int round,
            IReadOnlyDictionary<string, CouncilHardwareRoadPlan> modelRoutes,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            CouncilAutomaticFunctionPolicyResolution automaticFunctionPolicy,
            CancellationToken cancellationToken)
        {
            try
            {
                var plan = await gameCouncil.PlanAssetAsync(result.RunId, cancellationToken).ConfigureAwait(false);
                if (plan is null)
                    return null;

                var drawsSelf = assetRequest.Attempt == 0;
                string drawerRole;
                string drawer;
                if (drawsSelf)
                {
                    drawerRole = team.WorkflowSteps
                        .FirstOrDefault(step => string.Equals(step.Key, plan.SelfStepKey, StringComparison.OrdinalIgnoreCase))?.Role
                        ?? string.Empty;
                    drawer = assetRequest.MemberModelName;
                }
                else
                {
                    drawerRole = team.WorkflowSteps
                        .FirstOrDefault(step => step.IsEnabled && string.Equals(step.Key, plan.ArtistStepKey, StringComparison.OrdinalIgnoreCase))?.Role
                        ?? string.Empty;
                    var artists = roleAssignments.Values
                        .Where(assignment => string.Equals(assignment.RoleName, drawerRole, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(assignment => assignment.AiParticipants)
                        .Where(member => participants.Contains(member, StringComparer.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (assetRequest.Attempt - 1 >= artists.Count)
                        return null;
                    drawer = artists[assetRequest.Attempt - 1];
                }

                var assignment = roleAssignments.Values.FirstOrDefault(item => string.Equals(item.RoleName, drawerRole, StringComparison.OrdinalIgnoreCase));
                if (assignment is null || string.IsNullOrWhiteSpace(drawer) || !participants.Contains(drawer, StringComparer.OrdinalIgnoreCase))
                {
                    // Without the entrant's own role the artists are still asked; without an artist the check ends.
                    return drawsSelf ? new GameAssetDraft(drawer, string.Empty) : null;
                }

                var prompt = await gameCouncil.BuildAssetPromptAsync(result.RunId, assetRequest, drawer, drawsSelf, cancellationToken).ConfigureAwait(false);
                var assetDefinition = new CouncilWorkflowStepDefinition
                {
                    Key = plan.AssetStepKey,
                    DisplayName = prompt.PhaseLabel,
                    Phase = prompt.PhaseLabel,
                    Role = drawerRole,
                    PromptTemplate = prompt.Prompt,
                    ExecutionMode = "LeaderSingle",
                    RepeatCount = 1,
                    IncludePriorTranscript = false,
                    ProducesFinalAnswer = false,
                    UseBuiltInBehavior = false,
                    IsEnabled = true,
                    RequiresHumanCheckpoint = false,
                    CanUseOrganicFunctions = false,
                    AutomaticFunctionPolicyMode = CouncilAutomaticFunctionPolicyMode.Disabled,
                    RoleComplianceRetryCount = 0,
                    MemberFailureRecoveryMode = CouncilMemberFailureRecoveryMode.Disabled,
                    MemberFailureRecoveryAttempts = 0,
                    FinalAnswerRecoveryEnabled = false
                };
                var assetPhase = $"{prompt.PhaseLabel} · {prompt.EntrantName} · {assetRequest.Attempt + 1}";
                request.ProgressMessage?.Invoke(prompt.ProgressMessage);
                await RunConfiguredParticipantAsync(
                    result,
                    request,
                    assetDefinition,
                    team,
                    baseUri,
                    participants,
                    assignment,
                    rolePairings,
                    drawer,
                    round,
                    assetPhase,
                    0,
                    1,
                    string.Empty,
                    1,
                    1,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    modelRoutes,
                    keepAlive,
                    ollamaNumGpu,
                    maxContextTokens,
                    modelTimeoutSeconds,
                    false,
                    automaticFunctionPolicy,
                    cancellationToken).ConfigureAwait(false);

                var drawingStep = result.Steps.LastOrDefault(step =>
                    step.Round == round &&
                    string.Equals(step.Phase, assetPhase, StringComparison.Ordinal) &&
                    string.Equals(step.ModelName, drawer, StringComparison.OrdinalIgnoreCase));
                if (drawingStep is null)
                    return new GameAssetDraft(drawer, string.Empty);

                drawingStep.WorkflowStepKey = assetDefinition.Key;
                if (!drawsSelf)
                    drawingStep.RecoveryTargetModelName = assetRequest.MemberModelName;
                return new GameAssetDraft(
                    drawer,
                    string.IsNullOrWhiteSpace(drawingStep.Error) ? drawingStep.VisibleContent ?? drawingStep.Content ?? string.Empty : string.Empty);
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(exception, "A game asset drawing turn was cancelled for Council run {CouncilRunId}.", result.RunId);
                throw;
            }
            catch (Exception ex)
            {
                // A failed drawing never stops the game; the placeholder stays visible.
                logger.LogWarning(ex, "Game asset drawing turn {Attempt} for member {MemberModel} failed.", assetRequest.Attempt + 1, assetRequest.MemberModelName);
                return new GameAssetDraft(string.Empty, string.Empty);
            }
        }
    }
}
