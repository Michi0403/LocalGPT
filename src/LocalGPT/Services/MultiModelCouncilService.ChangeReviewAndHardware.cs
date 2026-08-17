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
        /// <returns>The code generation review snapshot produced by the operation.</returns>
        private async Task<CodeGenerationReviewSnapshot> CreateCouncilChangeReviewAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken)
        {
    try
    {
                var targetArea = councilText.DetectTargetArea(request.Prompt, result.FinalAnswer, logger);
                var parsedPlan = codeGenerationPlanService.Parse(result.FinalAnswer);
                var files = parsedPlan.Found
                    ? parsedPlan.Payload.Files.ToList()
                    : new List<CodeGenerationFileSpec>();
                var codeDomTypes = parsedPlan.Found
                    ? parsedPlan.Payload.CodeDomTypes.ToList()
                    : new List<CodeDomTypeSpec>();
                var outputs = parsedPlan.Found
                    ? parsedPlan.Payload.Outputs.ToList()
                    : new List<CodeGenerationOutputSpec>();

                if (!parsedPlan.Found)
                {
                    var isBlazor = councilRuntime.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea, logger) ?? false;
                    if (isBlazor)
                    {
                        files.Add(new CodeGenerationFileSpec
                        {
                            RelativePath = "src/CouncilFeaturePage.razor",
                            Purpose = "Council-reviewed Blazor/DevExpress page proposal",
                            Content = councilText.GenerateBlazorDevExpressRazorExample(request, result, logger)
                        });
                        files.Add(new CodeGenerationFileSpec
                        {
                            RelativePath = "src/CouncilFeatureSupport.cs",
                            Purpose = "Council-reviewed support service proposal",
                            Content = councilText.GenerateBlazorSupportCode(request, result, targetArea, logger)
                        });
                    }
                    else
                    {
                        codeDomTypes.Add(new CodeDomTypeSpec
                        {
                            RelativePath = "src/CouncilFeatureRequestExample.cs",
                            Namespace = "LocalGPT.Generated",
                            TypeName = "CouncilFeatureRequestExample",
                            MethodName = "Describe",
                            MethodResult = councilText.TrimForCodeComment(result.FinalAnswer, 4_000, logger),
                            Summary = $"Council-reviewed CodeDOM proposal for {targetArea}."
                        });
                    }

                    var combined = string.Concat(request.Prompt, Environment.NewLine, result.FinalAnswer);
                    var outputKind = combined.Contains(".csx", StringComparison.OrdinalIgnoreCase) ||
                                     combined.Contains("cscript", StringComparison.OrdinalIgnoreCase) ||
                                     combined.Contains("c# script", StringComparison.OrdinalIgnoreCase)
                        ? CodeGenerationOutputKinds.CSharpScript
                        : combined.Contains(".js", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("jscript", StringComparison.OrdinalIgnoreCase) ||
                          combined.Contains("javascript module", StringComparison.OrdinalIgnoreCase)
                            ? CodeGenerationOutputKinds.JavaScriptModule
                            : councilRuntime.IsWholeSolutionTarget(request.Prompt, result.FinalAnswer, logger) ?? false
                                ? CodeGenerationOutputKinds.Solution
                                : combined.Contains("console", StringComparison.OrdinalIgnoreCase) || combined.Contains(".exe", StringComparison.OrdinalIgnoreCase)
                                    ? CodeGenerationOutputKinds.ConsoleApplication
                                    : combined.Contains("plugin", StringComparison.OrdinalIgnoreCase) || combined.Contains("addon", StringComparison.OrdinalIgnoreCase)
                                        ? CodeGenerationOutputKinds.LocalGptAddon
                                        : CodeGenerationOutputKinds.ClassLibrary;

                    outputs.Add(new CodeGenerationOutputSpec
                    {
                        Kind = outputKind,
                        Name = "LocalGptCouncilFeature",
                        RelativeDirectory = "generated",
                        TargetFramework = "net10.0",
                        RootNamespace = "LocalGPT.Generated",
                        Description = councilText.TrimForCodeComment(result.FinalAnswer, 600, logger)
                    });
                }

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
                    ChangeSummary = parsedPlan.Found
                        ? $"Generate the council-authored structured plan from {parsedPlan.SourceFormat}: {files.Count} explicit file(s), {codeDomTypes.Count} CodeDOM type(s), and {outputs.Count} output target(s). When a project revision is selected, unchanged approved files are cloned byte-for-byte into its isolated workspace and only the exact reviewed files are replaced; the source checkout is never overwritten."
                        : $"Generate the bounded fallback plan for {targetArea}: {files.Count} explicit file(s), {codeDomTypes.Count} CodeDOM type(s), and {outputs.Count} output target(s). When a project revision is selected, unchanged approved files are cloned byte-for-byte into its isolated workspace and only the exact reviewed files are replaced; the source checkout is never overwritten.",
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
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<MultiModelCouncilStep>> AddCouncilStepAndExecuteDxFunctionsAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilStep step,
            Action<MultiModelCouncilStep>? stepCompleted,
            Action<string>? progressMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                var functionSteps = await councilDxFunctions.ExecuteRequestedCallsAsync(result, step, cancellationToken).ConfigureAwait(false);
                MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                stepCompleted?.Invoke(step);
                foreach (var functionStep in functionSteps)
                {
                    MultiModelCouncilServiceAddOrderedStep(result, functionStep, logger);
                    stepCompleted?.Invoke(functionStep);
                    progressMessage?.Invoke($"Council DXFunction gateway added {functionStep.Role} for round {functionStep.Round} with status {(string.IsNullOrWhiteSpace(functionStep.Error) ? "available" : "failed")}.");
                }
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
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<MultiModelCouncilStep>> AddCouncilStepAsync(
            MultiModelCouncilResult result,
            MultiModelCouncilStep step,
            Action<MultiModelCouncilStep>? stepCompleted,
            Action<string>? progressMessage,
            bool allowDxFunctions,
            CancellationToken cancellationToken)
        {
    try
    {
                if (allowDxFunctions)
                    return await AddCouncilStepAndExecuteDxFunctionsAsync(result, step, stepCompleted, progressMessage, cancellationToken).ConfigureAwait(false);

                MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                stepCompleted?.Invoke(step);
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
