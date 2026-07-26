using LocalGPT.Interfaces;

namespace LocalGPT.Services.Council.Scheduling;

public sealed class CouncilHardwareRoadConfigurationService : ICouncilHardwareRoadConfigurationService
{
    public IReadOnlyList<OneWireCouncilModelRoute> Synchronize(
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute>? existingRoutes)
    {
        var existing = (existingRoutes ?? [])
            .Where(route => route is not null && !string.IsNullOrWhiteSpace(route.ModelName))
            .GroupBy(route => route.ModelName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var synchronized = new List<OneWireCouncilModelRoute>();
        foreach (var modelName in modelNames
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Select(name => name.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!existing.TryGetValue(modelName, out var route))
            {
                route = new OneWireCouncilModelRoute
                {
                    ModelName = modelName,
                    HardwareKind = OneWireHardwareKind.Auto,
                    HardwareIndex = -1,
                    HardwareName = "Automatic",
                    MinOutputTokens = 256,
                    MaxOutputTokens = 262144,
                    MinContextTokens = 2048,
                    MaxContextTokens = 262144,
                    MaxConcurrentModelsOnLane = 1,
                    IsEnabled = true
                };
            }

            route.ModelName = modelName;
            synchronized.Add(Normalize(route));
        }

        return synchronized;
    }

    public OneWireCouncilModelRoute Normalize(OneWireCouncilModelRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        route.ModelName = route.ModelName?.Trim() ?? string.Empty;
        route.HardwareName = string.IsNullOrWhiteSpace(route.HardwareName)
            ? route.HardwareKind == OneWireHardwareKind.Auto ? "Automatic" : route.HardwareKind.ToString()
            : route.HardwareName.Trim();
        route.HardwareIndex = route.HardwareKind == OneWireHardwareKind.Auto ? -1 : Math.Max(0, route.HardwareIndex);
        route.MinOutputTokens = Math.Clamp(route.MinOutputTokens <= 0 ? 256 : route.MinOutputTokens, 1, 262144);
        route.MaxOutputTokens = Math.Clamp(Math.Max(route.MinOutputTokens, route.MaxOutputTokens), route.MinOutputTokens, 262144);
        route.MinContextTokens = Math.Clamp(route.MinContextTokens <= 0 ? 2048 : route.MinContextTokens, 256, 1048576);
        route.MaxContextTokens = Math.Clamp(Math.Max(route.MinContextTokens, route.MaxContextTokens), route.MinContextTokens, 1048576);
        route.MaxConcurrentModelsOnLane = Math.Clamp(route.MaxConcurrentModelsOnLane, 1, 16);
        route.LoadPercentOverride = route.LoadPercentOverride is null ? null : NormalizeLoadPercent(route.LoadPercentOverride.Value);
        route.OllamaNumGpu = route.HardwareKind == OneWireHardwareKind.Cpu
            ? 0
            : route.OllamaNumGpu is < 0 ? 0 : route.OllamaNumGpu;
        return route;
    }

    public int NormalizeLoadPercent(int value) => Math.Clamp((int)Math.Round(value / 5d) * 5, 0, 100);

    public int Interpolate(int minimum, int maximum, int loadPercent)
    {
        minimum = Math.Max(0, minimum);
        maximum = Math.Max(minimum, maximum);
        var percent = NormalizeLoadPercent(loadPercent);
        var value = minimum + (long)Math.Round((maximum - minimum) * (percent / 100d), MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(value, minimum, maximum);
    }
}
