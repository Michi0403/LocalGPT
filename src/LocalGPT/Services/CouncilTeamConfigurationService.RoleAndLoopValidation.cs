using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council team configuration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilTeamConfigurationService
    {
    /// <summary>Rejects role-count references that form cycles.</summary>
    /// <param name="roles">Normalized role definitions.</param>
    private void ValidateRoleCountReferenceCycles(IReadOnlyList<OrganicCouncilRoleDefinition> roles)
    {
    try
    {
            var byName = roles
                .Where(role => !string.IsNullOrWhiteSpace(role.Role))
                .ToDictionary(role => role.Role, StringComparer.OrdinalIgnoreCase);
            foreach (var role in roles)
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var current = role;
                while (!string.IsNullOrWhiteSpace(current.MatchAiParticipantCountToRole) &&
                       byName.TryGetValue(current.MatchAiParticipantCountToRole, out var next))
                {
                    if (!visited.Add(current.Role))
                        throw new InvalidOperationException($"Role AI participant count references contain a cycle involving '{role.Role}'.");
                    current = next;
                }
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ValidateRoleCountReferenceCycles)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ValidateRoleCountReferenceCycles)} failed.");
        throw;
    }
}

    /// <summary>Validates distinct-model assignment groups against participant bounds.</summary>
    /// <param name="roles">Normalized role definitions.</param>
    private void ValidateDistinctAssignmentGroups(IReadOnlyList<OrganicCouncilRoleDefinition> roles)
    {
    try
    {
            foreach (var group in roles
                         .Where(role => !string.IsNullOrWhiteSpace(role.DistinctAiAssignmentGroup) &&
                                        role.HumanParticipationMode != HumanParticipationMode.HumanOnly)
                         .GroupBy(role => role.DistinctAiAssignmentGroup, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() > 1 && group.Any(role => role.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected))
                {
                    throw new InvalidOperationException(
                        $"Distinct AI assignment group '{group.Key}' contains more than one AI role, so every role in that group must use a bounded random range instead of all selected AIs.");
                }
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ValidateDistinctAssignmentGroups)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ValidateDistinctAssignmentGroups)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes loop groups as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="steps">Workflow steps to normalize.</param>
    private void NormalizeLoopGroups(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
    try
    {
            foreach (var group in steps
                         .Where(step => !string.IsNullOrWhiteSpace(step.LoopGroup))
                         .GroupBy(step => step.LoopGroup, StringComparer.OrdinalIgnoreCase))
            {
                var maximumIterations = group.Max(step => Math.Clamp(step.MaximumLoopIterations, 1, MaxExpandedWorkflowSteps));
                foreach (var step in group)
                    step.MaximumLoopIterations = maximumIterations;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeLoopGroups)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(NormalizeLoopGroups)} failed.");
        throw;
    }
}

    /// <summary>Validates workflow loop-group consistency and completion markers.</summary>
    /// <param name="steps">Normalized workflow steps.</param>
    private void ValidateLoopGroups(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
    try
    {
            var ordered = steps
                .Where(step => step.IsEnabled)
                .OrderBy(step => step.SortOrder)
                .ThenBy(step => step.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var completedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? activeGroup = null;
            foreach (var step in ordered)
            {
                var group = string.IsNullOrWhiteSpace(step.LoopGroup) ? null : step.LoopGroup;
                if (string.Equals(group, activeGroup, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (activeGroup is not null)
                    completedGroups.Add(activeGroup);
                if (group is not null && completedGroups.Contains(group))
                    throw new InvalidOperationException($"Loop group '{group}' must occupy one consecutive block in workflow sort order.");
                activeGroup = group;
            }

            foreach (var group in ordered
                         .Where(step => !string.IsNullOrWhiteSpace(step.LoopGroup))
                         .GroupBy(step => step.LoopGroup, StringComparer.OrdinalIgnoreCase))
            {
                var markers = group
                    .Where(step => !string.IsNullOrWhiteSpace(step.LoopCompletionMarker))
                    .Select(step => step.LoopCompletionMarker)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (markers.Count > 1)
                    throw new InvalidOperationException($"Loop group '{group.Key}' defines multiple different completion markers. Use one marker for the whole loop.");
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ValidateLoopGroups)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(ValidateLoopGroups)} failed.");
        throw;
    }
}

    /// <summary>
    /// Calculates maximum expanded rounds as part of the council team configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="steps">Normalized workflow steps.</param>
    /// <returns>The bounded maximum expanded-round count.</returns>
    private int CalculateMaximumExpandedRounds(IReadOnlyList<CouncilWorkflowStepDefinition> steps)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(CalculateMaximumExpandedRounds)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTeamConfigurationService)}.{nameof(CalculateMaximumExpandedRounds)} failed.");
        throw;
    }
}

    }
}
