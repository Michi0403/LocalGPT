using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Council.Scheduling;

/// <summary>
/// Represents a council hardware road planner application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilHardwareRoadPlanner(
    ICouncilHardwareRoadConfigurationService configuration,
    ILogger<CouncilHardwareRoadPlanner> logger) : ICouncilHardwareRoadPlanner
{
    /// <summary>
    /// Builds plans for <see cref="CouncilHardwareRoadPlanner"/>, keeping the operation consistent with the state and invariants of the surrounding council hardware road planner workflow.
    /// </summary>
    /// <param name="configuredRoutes">One wire council model route dependency used by the council hardware road planner workflow to provide the corresponding application capability.</param>
    /// <param name="participants">String dependency used by the council hardware road planner workflow to provide the corresponding application capability.</param>
    /// <param name="requestedMaxOutputTokens">Requested max output tokens value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <param name="requestedMaxContextTokens">Requested max context tokens value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <param name="resourceLoadPercent">Resource load percent value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <param name="fallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council hardware road planner operation and used when producing its result.</param>
    /// <returns>The i read only dictionary string council hardware road plan produced by the operation.</returns>
    public IReadOnlyDictionary<string, CouncilHardwareRoadPlan> BuildPlans(
        IReadOnlyCollection<OneWireCouncilModelRoute>? configuredRoutes,
        IReadOnlyCollection<string> participants,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int resourceLoadPercent,
        int? fallbackOllamaNumGpu)
    {
    try
    {
            logger.LogInformation("Building council hardware-road plans for {ParticipantCount} participant(s).", participants.Count);
            var routes = (configuredRoutes ?? [])
                .Where(route => route.IsEnabled && !string.IsNullOrWhiteSpace(route.ModelName))
                .GroupBy(route => route.ModelName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, CouncilHardwareRoadPlan>(StringComparer.OrdinalIgnoreCase);
            foreach (var participant in participants.Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                var modelName = participant.Trim();
                routes.TryGetValue(modelName, out var route);
                route ??= new OneWireCouncilModelRoute
                {
                    ModelName = modelName,
                    HardwareKind = fallbackOllamaNumGpu == 0 ? OneWireHardwareKind.Cpu : OneWireHardwareKind.Auto,
                    HardwareIndex = fallbackOllamaNumGpu == 0 ? 0 : -1,
                    HardwareName = fallbackOllamaNumGpu == 0 ? "CPU" : "Automatic",
                    MinOutputTokens = 256,
                    MaxOutputTokens = Math.Max(256, requestedMaxOutputTokens),
                    MinContextTokens = 2048,
                    MaxContextTokens = Math.Max(2048, requestedMaxContextTokens),
                    OllamaNumGpu = fallbackOllamaNumGpu,
                    MaxConcurrentModelsOnLane = 1
                };

                route = configuration.Normalize(route);
                var minOutput = route.MinOutputTokens;
                var maxOutput = route.MaxOutputTokens;
                var minContext = route.MinContextTokens;
                var maxContext = route.MaxContextTokens;
                var effectiveLoadPercent = configuration.NormalizeLoadPercent(route.LoadPercentOverride ?? resourceLoadPercent);
                var effectiveOutput = Math.Min(configuration.Interpolate(minOutput, maxOutput, effectiveLoadPercent), Math.Max(minOutput, requestedMaxOutputTokens));
                var effectiveContext = Math.Min(configuration.Interpolate(minContext, maxContext, effectiveLoadPercent), Math.Max(minContext, requestedMaxContextTokens));
                var hardwareName = string.IsNullOrWhiteSpace(route.HardwareName)
                    ? route.HardwareKind.ToString()
                    : route.HardwareName.Trim();
                var laneKey = route.HardwareKind == OneWireHardwareKind.Auto
                    ? $"auto:{modelName}"
                    : $"{route.HardwareKind.ToString().ToLowerInvariant()}:{route.HardwareIndex}:{hardwareName}";

                var isOllamaRoute = string.IsNullOrWhiteSpace(route.ProviderKind)
                    || route.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase);
                var effectiveOllamaNumGpu = isOllamaRoute
                    ? route.HardwareKind switch
                    {
                        OneWireHardwareKind.Cpu => 0,
                        OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator => route.OllamaNumGpu is > 0
                            ? route.OllamaNumGpu
                            : null,
                        _ => route.OllamaNumGpu ?? fallbackOllamaNumGpu
                    }
                    : null;

                result[modelName] = new CouncilHardwareRoadPlan(
                    modelName,
                    route.HardwareKind,
                    route.HardwareIndex,
                    hardwareName,
                    laneKey,
                    effectiveLoadPercent,
                    effectiveOutput,
                    effectiveContext,
                    effectiveOllamaNumGpu,
                    Math.Clamp(route.MaxConcurrentModelsOnLane, 1, 16));
            }

            logger.LogInformation("Built {PlanCount} council hardware-road plan(s).", result.Count);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilHardwareRoadPlanner)}.{nameof(BuildPlans)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilHardwareRoadPlanner)}.{nameof(BuildPlans)} failed.");
        throw;
    }
}
}
