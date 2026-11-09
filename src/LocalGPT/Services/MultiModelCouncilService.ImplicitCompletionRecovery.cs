using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates multi model council behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Derives a completion-recovery rule for every required function of the team that has no explicit rule. The rule
        /// re-runs the last enabled step that is configured to call the function, with an instruction naming the missing
        /// receipt, instead of only reporting the function as missing when the run ends.
        /// </summary>
        /// <param name="result">Council result that receives a warning when a required function has no step that may call it.</param>
        /// <param name="team">Team whose required functions and explicit recovery rules are read.</param>
        /// <param name="configuredSteps">Enabled workflow steps of the team in execution order.</param>
        /// <returns>The implicit recovery rules; empty when every required function has an explicit rule or the policy disables them.</returns>
        private IReadOnlyList<CouncilRequiredFunctionRecoveryRule> BuildImplicitCompletionRecoveryRules(
            MultiModelCouncilResult result,
            OrganicCouncilTeamDefinition team,
            IReadOnlyList<CouncilWorkflowStepDefinition> configuredSteps)
        {
            try
            {
                var parameters = runtimePolicy.GetJson<CouncilExecutionRuntimeParameters>("CouncilExecutionRuntimeParametersJson");
                var attempts = Math.Clamp(parameters.ImplicitCompletionRecoveryAttempts, 0, Math.Max(0, parameters.MaximumCompletionRecoveryAttempts));
                if (attempts == 0)
                    return [];

                var explicitFunctions = team.CompletionRecovery.Rules
                    .Select(rule => rule.FunctionName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var rules = new List<CouncilRequiredFunctionRecoveryRule>();
                foreach (var functionName in (team.RequiredCompletedFunctions ?? [])
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (explicitFunctions.Contains(functionName))
                        continue;

                    var recoveryStep = configuredSteps.LastOrDefault(step =>
                        step.CanUseOrganicFunctions &&
                        (step.AllowedAutomaticFunctions.Contains(functionName, StringComparer.OrdinalIgnoreCase) ||
                         (step.AutomaticFunctionPolicyMode != CouncilAutomaticFunctionPolicyMode.StrictStepAllowList &&
                          team.AllowedAutomaticFunctions.Contains(functionName, StringComparer.OrdinalIgnoreCase))));
                    if (recoveryStep is null)
                    {
                        result.Warnings.Add(
                            $"Required function '{functionName}' has no enabled workflow step whose function policy names it, so LocalGPT cannot re-run a step to recover it. Add it to the allow-list of the step that should call it, or add an explicit completion-recovery rule.");
                        continue;
                    }

                    rules.Add(new CouncilRequiredFunctionRecoveryRule
                    {
                        FunctionName = functionName,
                        WorkflowStepKey = recoveryStep.Key,
                        MaximumAttempts = attempts,
                        RecoveryPromptTemplate = parameters.ImplicitCompletionRecoveryPromptTemplate
                            .Replace("{{FunctionName}}", functionName, StringComparison.Ordinal)
                    });
                    logger.LogInformation(
                        "Council run {RunId} derived an implicit completion-recovery rule for required function {FunctionName} through step {StepKey} with {Attempts} attempt(s).",
                        result.RunId, functionName, recoveryStep.Key, attempts);
                }

                return rules;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Deriving implicit completion-recovery rules for Council run {RunId} failed.", result?.RunId);
                throw;
            }
        }
    }
}
