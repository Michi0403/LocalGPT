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
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Creates council change review as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The prepared change review, or <see langword="null"/> when the final answer carries no structured file plan.</returns>
        private async Task<CodeGenerationReviewSnapshot?> CreateCouncilChangeReviewAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken)
        {
    try
    {
                var targetArea = councilText.DetectTargetArea(request.Prompt, result.FinalAnswer, logger);
                var parsedPlan = codeGenerationPlanService.Parse(result.FinalAnswer);
                if (!parsedPlan.Found)
                {
                    // Without an exact file plan there is nothing reviewable to generate. Placeholder example
                    // files would look like progress while implementing none of the requested change.
                    if (!string.IsNullOrWhiteSpace(parsedPlan.Warning))
                        result.Warnings.Add(parsedPlan.Warning);
                    logger.LogInformation(
                        "Council run {RunId} prepared no change review because its final answer carries no structured code-generation plan.",
                        result.RunId);
                    return null;
                }

                var files = parsedPlan.Payload.Files.ToList();
                var codeDomTypes = parsedPlan.Payload.CodeDomTypes.ToList();
                var outputs = parsedPlan.Payload.Outputs.ToList();

                if (!string.IsNullOrWhiteSpace(parsedPlan.Warning))
                    result.Warnings.Add(parsedPlan.Warning);

                var currentState = "No LocalGPT project was selected. The review targets an isolated generated artifact workspace only.";
                if (result.ProjectId is Guid projectId)
                {
                    var projectBriefing = await projectService.BuildProjectBriefingAsync(projectId, result.ProjectTopicId, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(projectBriefing))
                        currentState = projectBriefing;
                }

                var reviewRequest = new CreateCodeGenerationReviewRequest
                {
                    ProjectId = result.ProjectId,
                    ProjectRevisionId = result.ProjectRevisionId,
                    ProjectTopicId = result.ProjectTopicId,
                    CouncilRunId = result.RunId,
                    Title = string.IsNullOrWhiteSpace(request.Title) ? $"Council change review - {targetArea}" : request.Title,
                    Goal = request.Prompt,
                    CurrentProjectState = currentState,
                    CouncilSummary = result.FinalAnswer,
                    ChangeSummary = $"Generate the council-authored structured plan from {parsedPlan.SourceFormat}: {files.Count} explicit file(s), {codeDomTypes.Count} CodeDOM type(s), and {outputs.Count} output target(s). When a project revision is selected, unchanged approved files are cloned into its isolated workspace, only the exact reviewed files are replaced, and the result is registered as a new child revision; the source checkout and the selected revision are never overwritten.",
                    SafetySummary = "This heartbeat records the exact proposed payload before generation. Execution requires the current user to approve the matching review hash. Writes stay inside the resolved project-revision workspace; builds require a separate current confirmation; generated scripts, DLLs, and executables are never run or loaded automatically.",
                    Files = files,
                    CodeDomTypes = codeDomTypes,
                    Outputs = outputs
                };

                var review = await codeGenerationWorkflow.CreateReviewAsync(reviewRequest, cancellationToken).ConfigureAwait(false);
                logger.LogInformation(
                    "Council run {RunId} created change review {ReviewId} with hash prefix {HashPrefix}.",
                    result.RunId,
                    review.Id,
                    review.ReviewHash[..Math.Min(12, review.ReviewHash.Length)]);
                return review;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(CreateCouncilChangeReviewAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(CreateCouncilChangeReviewAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Applies hardware plan as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="step">Step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="plan">Plan value supplied to the multi model council operation and used when producing its result.</param>
        private void ApplyHardwarePlan(MultiModelCouncilStep step, CouncilHardwareRoadPlan plan)
        {
    try
    {
                step.HardwareLane = plan.LaneKey;
                step.HardwareKind = plan.HardwareKind;
                step.HardwareIndex = plan.HardwareIndex;
                step.EffectiveLoadPercent = plan.EffectiveLoadPercent;
                step.EffectiveMaxOutputTokens = plan.EffectiveMaxOutputTokens;
                step.EffectiveMaxContextTokens = plan.EffectiveMaxContextTokens;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(ApplyHardwarePlan)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(ApplyHardwarePlan)} failed.");
        throw;
    }
}

        /// <summary>
        /// Adds council step and execute DevExpress functions as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="step">Step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="stepCompleted">Step completed value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="progressMessage">Progress message value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="strictFunctionNames">Optional exclusive function list for the currently executing configured step.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<MultiModelCouncilStep>> AddCouncilStepAndExecuteDxFunctionsAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilStep step,
            Action<MultiModelCouncilStep>? stepCompleted,
            Action<string>? progressMessage,
            CancellationToken cancellationToken,
            IReadOnlyCollection<string>? strictFunctionNames = null)
        {
            try
            {
                var functionSteps = await councilDxFunctions.ExecuteRequestedCallsAsync(result, step, cancellationToken, strictFunctionNames).ConfigureAwait(false);
                MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                stepCompleted?.Invoke(step);
                foreach (var functionStep in functionSteps)
                {
                    MultiModelCouncilServiceAddOrderedStep(result, functionStep, logger);
                    stepCompleted?.Invoke(functionStep);
                    progressMessage?.Invoke($"Council DXFunction gateway added {functionStep.Role} for round {functionStep.Round} with status {(string.IsNullOrWhiteSpace(functionStep.Error) ? "available" : "failed")}.");
                }
                result.LogPath = await WriteLogAsync(result, CancellationToken.None, logger).ConfigureAwait(false);
                logger.LogDebug($"Added Council step {step.SortOrder} and {functionSteps.Count} database-backed DX function result step(s).");
                return functionSteps;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Council step {step.SortOrder} could not be added with its database-backed DX function results.");
                throw;
            }
        }

        /// <summary>
        /// Adds council step as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="step">Step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="stepCompleted">Step completed value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="progressMessage">Progress message value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="allowDxFunctions">Value indicating whether allow DevExpress functions should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="strictFunctionNames">Optional step-local tool selection applied to this step's native and textual function dispatch.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<MultiModelCouncilStep>> AddCouncilStepAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilStep step,
            Action<MultiModelCouncilStep>? stepCompleted,
            Action<string>? progressMessage,
            bool allowDxFunctions,
            CancellationToken cancellationToken,
            IReadOnlyCollection<string>? strictFunctionNames = null)
        {
    try
    {
                if (allowDxFunctions)
                    return await AddCouncilStepAndExecuteDxFunctionsAsync(result, step, stepCompleted, progressMessage, cancellationToken, strictFunctionNames).ConfigureAwait(false);

                MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                stepCompleted?.Invoke(step);
                result.LogPath = await WriteLogAsync(result, CancellationToken.None, logger).ConfigureAwait(false);
                progressMessage?.Invoke($"Council added {step.ModelName} for round {step.Round} / {step.Phase} without organic function execution.");
                return [];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(AddCouncilStepAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(AddCouncilStepAsync)} failed.");
        throw;
    }
}

    }
}
