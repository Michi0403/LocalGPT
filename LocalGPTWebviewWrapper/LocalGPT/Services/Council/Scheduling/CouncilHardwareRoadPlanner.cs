using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Council.Scheduling;

public sealed class CouncilHardwareRoadPlanner(
    ICouncilHardwareRoadConfigurationService configuration,
    ILogger<CouncilHardwareRoadPlanner> logger) : ICouncilHardwareRoadPlanner
{
    public IReadOnlyDictionary<string, CouncilHardwareRoadPlan> BuildPlans(
        IReadOnlyCollection<OneWireCouncilModelRoute>? configuredRoutes,
        IReadOnlyCollection<string> participants,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int resourceLoadPercent,
        int? fallbackOllamaNumGpu)
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

            result[modelName] = new CouncilHardwareRoadPlan(
                modelName,
                route.HardwareKind,
                route.HardwareIndex,
                hardwareName,
                laneKey,
                effectiveLoadPercent,
                effectiveOutput,
                effectiveContext,
                route.OllamaNumGpu ?? fallbackOllamaNumGpu,
                Math.Clamp(route.MaxConcurrentModelsOnLane, 1, 16));
        }

        logger.LogInformation("Built {PlanCount} council hardware-road plan(s).", result.Count);
        return result;
    }
}
