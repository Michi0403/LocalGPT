using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.Council.Scheduling;

/// <summary>
/// Coordinates council hardware road configuration behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="runtimePolicy">Seed-backed Council defaults.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class CouncilHardwareRoadConfigurationService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<CouncilHardwareRoadConfigurationService> logger) : ICouncilHardwareRoadConfigurationService
{
    /// <summary>Maximum route output budget from the persisted Council policy, not a locally invented token ceiling.</summary>
    /// <value>Authoritative Council maximum output tokens for editable model roads.</value>
    public int MaximumRoadOutputTokens => Math.Max(1, runtimePolicy.GetInt("MaxCouncilOutputTokens"));

    /// <summary>Maximum route context budget from the persisted Council policy, not a locally invented token ceiling.</summary>
    /// <value>Authoritative Council maximum context tokens for editable model roads.</value>
    public int MaximumRoadContextTokens => Math.Max(256, runtimePolicy.GetInt("MaxCouncilContextTokens"));

    /// <summary>Constructs an automatic road from authoritative runtime defaults or explicit run budgets.</summary>
    /// <param name="modelName">Provider-qualified model selection key.</param>
    /// <param name="requestedMaxOutputTokens">Run output budget, or null to use the Council runtime default.</param>
    /// <param name="requestedMaxContextTokens">Run context budget, or null to use the Council runtime default.</param>
    /// <returns>A new road which has not been mistaken for a benchmark-measured profile.</returns>
    public OneWireCouncilModelRoute CreateDefaultRoute(
        string modelName, int? requestedMaxOutputTokens = null, int? requestedMaxContextTokens = null)
    {
        try
        {
        var maximumOutput = Math.Clamp(
            requestedMaxOutputTokens is > 0 ? requestedMaxOutputTokens.Value : runtimePolicy.GetInt("DefaultCouncilOutputTokens"),
            1, MaximumRoadOutputTokens);
        var maximumContext = Math.Clamp(
            requestedMaxContextTokens is > 0 ? requestedMaxContextTokens.Value : runtimePolicy.GetInt("DefaultCouncilContextTokens"),
            256, MaximumRoadContextTokens);
        return new OneWireCouncilModelRoute
        {
            ModelName = modelName,
            HardwareKind = OneWireHardwareKind.Auto,
            HardwareIndex = -1,
            HardwareName = "Automatic",
            MinOutputTokens = Math.Min(Math.Max(1, runtimePolicy.GetInt("MinCouncilOutputTokens")), maximumOutput),
            MaxOutputTokens = maximumOutput,
            MinContextTokens = Math.Min(Math.Max(256, runtimePolicy.GetInt("MinCouncilContextTokens")), maximumContext),
            MaxContextTokens = maximumContext,
            IsEnabled = true,
            MaxConcurrentModelsOnLane = 1
        };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create the runtime-policy-backed automatic road for {ModelName}.", modelName);
            throw;
        }
    }

    /// <summary>
    /// Performs synchronize as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="modelNames">String dependency used by the council hardware road configuration workflow to provide the corresponding application capability.</param>
    /// <param name="existingRoutes">One wire council model route dependency used by the council hardware road configuration workflow to provide the corresponding application capability.</param>
    /// <param name="requestedMaxOutputTokens">Current run output ceiling for new roads.</param>
    /// <param name="requestedMaxContextTokens">Current run context ceiling for new roads.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OneWireCouncilModelRoute> Synchronize(
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute>? existingRoutes,
        int? requestedMaxOutputTokens = null,
        int? requestedMaxContextTokens = null)
    {
        try
        {
            var requestedModelNames = modelNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var existingList = (existingRoutes ?? [])
                .Where(route => route is not null && !string.IsNullOrWhiteSpace(route.ModelName))
                .ToList();

            if (existingList.Count == requestedModelNames.Count
                && existingList.Select(route => route.ModelName.Trim()).SequenceEqual(requestedModelNames, StringComparer.OrdinalIgnoreCase)
                && existingList.All(IsAlreadyNormalized))
            {
                logger.LogTrace("Council hardware roads are already synchronized for {RouteCount} model(s).", existingList.Count);
                return existingRoutes as IReadOnlyList<OneWireCouncilModelRoute> ?? existingList;
            }

            var existing = existingList
                .GroupBy(route => route.ModelName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            var synchronized = new List<OneWireCouncilModelRoute>();
            foreach (var modelName in requestedModelNames)
            {
                if (!existing.TryGetValue(modelName, out var route))
                {
                    route = CreateDefaultRoute(modelName, requestedMaxOutputTokens, requestedMaxContextTokens);
                }

                route.ModelName = modelName;
                synchronized.Add(Normalize(route));
            }

            logger.LogDebug("Synchronized {RouteCount} changed council hardware road configuration(s).", synchronized.Count);
            return synchronized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not synchronize council hardware road configurations.");
            throw;
        }
    }


    /// <inheritdoc />
    public bool TryRefreshAutomaticRoadBudget(OneWireCouncilModelRoute route,
        int previousMaxOutputTokens, int previousMaxContextTokens,
        int requestedMaxOutputTokens, int requestedMaxContextTokens)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(route);
            // Automatic is a generated profile, not a reason to overwrite an operator's
            // explicit hardware, token, load, concurrency or capability configuration.
            if (route.HardwareKind != OneWireHardwareKind.Auto
                || !string.Equals(route.HardwareName, "Automatic", StringComparison.Ordinal)
                || route.HardwareIndex != -1 || route.LoadPercentOverride.HasValue
                || route.OllamaNumGpu.HasValue || !route.IsEnabled || route.MaxConcurrentModelsOnLane != 1
                || route.SelfReportedDxFunctions?.Count > 0 || route.SelfReportedControllerMethods?.Count > 0
                || route.SelfReportedOrganicCapabilities?.Count > 0 || route.SelfReportedSkills?.Count > 0)
                return false;

            var previous = CreateDefaultRoute(route.ModelName, previousMaxOutputTokens, previousMaxContextTokens);
            var policyDefault = CreateDefaultRoute(route.ModelName);
            var matchesPrevious = route.MinOutputTokens == previous.MinOutputTokens
                && route.MaxOutputTokens == previous.MaxOutputTokens
                && route.MinContextTokens == previous.MinContextTokens
                && route.MaxContextTokens == previous.MaxContextTokens;
            var matchesPolicy = route.MinOutputTokens == policyDefault.MinOutputTokens
                && route.MaxOutputTokens == policyDefault.MaxOutputTokens
                && route.MinContextTokens == policyDefault.MinContextTokens
                && route.MaxContextTokens == policyDefault.MaxContextTokens;
            if (!matchesPrevious && !matchesPolicy)
                return false;

            var updated = CreateDefaultRoute(route.ModelName, requestedMaxOutputTokens, requestedMaxContextTokens);
            route.MinOutputTokens = updated.MinOutputTokens;
            route.MaxOutputTokens = updated.MaxOutputTokens;
            route.MinContextTokens = updated.MinContextTokens;
            route.MaxContextTokens = updated.MaxContextTokens;
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not reconcile the automatic Council road budget for {ModelName}.", route?.ModelName);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<OneWireCouncilModelRoute> ResolvePresetRoutes(
        IEnumerable<string> modelNames, IEnumerable<OneWireCouncilModelRoute> existingRoutes,
        IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
        int? requestedMaxOutputTokens = null, int? requestedMaxContextTokens = null)
    {
        try
        {
            var identity = new ProviderModelIdentity();
            var configured = Synchronize(modelNames, existingRoutes.Select(CloneRoute),
                requestedMaxOutputTokens, requestedMaxContextTokens);
            var resolved = new List<OneWireCouncilModelRoute>();
            var missing = new List<OneWireCouncilModelRoute>();
            foreach (var route in configured)
            {
                var generic = !identity.LooksProviderQualified(route.ModelName)
                    && string.IsNullOrWhiteSpace(route.ProviderEndpoint)
                    && (string.IsNullOrWhiteSpace(route.ProviderKind) || route.ProviderKind == ProviderModelKinds.Ollama);
                var matches = generic
                    ? candidates.Where(candidate => candidate.IsInstalled && candidate.ProviderKind == ProviderModelKinds.Ollama
                        && identity.ModelNamesEquivalent(route.ModelName, candidate.ModelName)).ToList()
                    : candidates.Where(candidate => candidate.SelectionKey.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(route.ProviderEndpoint)
                            && candidate.ProviderKind.Equals(route.ProviderKind, StringComparison.OrdinalIgnoreCase)
                            && identity.NormalizeEndpoint(candidate.Endpoint) == identity.NormalizeEndpoint(route.ProviderEndpoint)
                            && identity.ModelNamesEquivalent(route.ProviderModelName, candidate.ModelName))).ToList();
                if (matches.Count == 0)
                {
                    if (generic)
                        missing.Add(route);
                    else
                        resolved.Add(route);
                    continue;
                }
                foreach (var candidate in matches.OrderBy(item => item.SelectionKey, StringComparer.OrdinalIgnoreCase))
                {
                    var qualified = CloneRoute(route);
                    qualified.ModelName = candidate.SelectionKey;
                    qualified.ProviderKind = candidate.ProviderKind;
                    qualified.ProviderName = candidate.Provider;
                    qualified.ProviderEndpoint = candidate.Endpoint;
                    qualified.ProviderModelName = candidate.ModelName;
                    resolved.Add(Normalize(qualified));
                }
            }
            // A generic template describes a pool, not a requirement to install every model.
            // Preserve unresolved selections when none can run so normal readiness can report them.
            if (resolved.Count == 0)
                resolved.AddRange(missing);
            return resolved.DistinctBy(route => route.ModelName, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving Council preset routes against configured providers failed.");
            throw;
        }
    }

    /// <summary>Copies one editable road without sharing mutable capability lists between providers.</summary>
    /// <param name="route">The original road.</param>
    /// <returns>An independent copy of the road.</returns>
    public OneWireCouncilModelRoute CloneRoute(OneWireCouncilModelRoute route)
    {
        try
        {
            return new()
    {
        ModelName = route.ModelName,
        ProviderKind = route.ProviderKind,
        ProviderName = route.ProviderName,
        ProviderEndpoint = route.ProviderEndpoint,
        ProviderModelName = route.ProviderModelName,
        HardwareKind = route.HardwareKind,
        HardwareIndex = route.HardwareIndex,
        HardwareName = route.HardwareName,
        MinOutputTokens = route.MinOutputTokens,
        MaxOutputTokens = route.MaxOutputTokens,
        MinContextTokens = route.MinContextTokens,
        MaxContextTokens = route.MaxContextTokens,
        OllamaNumGpu = route.OllamaNumGpu,
        LoadPercentOverride = route.LoadPercentOverride,
        SelfReportedDxFunctions = [.. route.SelfReportedDxFunctions],
        SelfReportedControllerMethods = [.. route.SelfReportedControllerMethods],
        SelfReportedOrganicCapabilities = [.. route.SelfReportedOrganicCapabilities],
        SelfReportedSkills = [.. route.SelfReportedSkills],
        IsEnabled = route.IsEnabled,
        MaxConcurrentModelsOnLane = route.MaxConcurrentModelsOnLane
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying a Council hardware road failed.");
            throw;
        }
    }

    /// <summary>Checks whether one existing road already satisfies the normalization invariants used by <see cref="Normalize(OneWireCouncilModelRoute)"/>.</summary>
    /// <param name="route">Existing route inspected without mutating it.</param>
    /// <returns><see langword="true"/> when a synchronization pass would not normalize any route field.</returns>
    private bool IsAlreadyNormalized(OneWireCouncilModelRoute route)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(route.ModelName) || route.ModelName != route.ModelName.Trim())
                return false;
            if (string.IsNullOrWhiteSpace(route.HardwareName) || route.HardwareName != route.HardwareName.Trim())
                return false;
            if (route.HardwareKind == OneWireHardwareKind.Auto ? route.HardwareIndex != -1 : route.HardwareIndex < 0)
                return false;
            if (route.MinOutputTokens < 1 || route.MinOutputTokens > MaximumRoadOutputTokens || route.MaxOutputTokens < route.MinOutputTokens || route.MaxOutputTokens > MaximumRoadOutputTokens)
                return false;
            if (route.MinContextTokens < 256 || route.MinContextTokens > MaximumRoadContextTokens || route.MaxContextTokens < route.MinContextTokens || route.MaxContextTokens > MaximumRoadContextTokens)
                return false;
            if (route.MaxConcurrentModelsOnLane is < 1 or > 16)
                return false;
            if (route.LoadPercentOverride is { } load && (load < 0 || load > 100 || load % 5 != 0))
                return false;

            var isOllamaRoute = string.IsNullOrWhiteSpace(route.ProviderKind)
                || route.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase);
            if (!isOllamaRoute)
                return route.OllamaNumGpu is null;
            return route.HardwareKind switch
            {
                OneWireHardwareKind.Cpu => route.OllamaNumGpu == 0,
                OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator => route.OllamaNumGpu is null or > 0,
                _ => route.OllamaNumGpu is null or >= 0
            };
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Checking an existing council hardware road for normalization drift failed; the route will be normalized again.");
            return false;
        }
    }

    /// <summary>
    /// Performs normalize as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    public OneWireCouncilModelRoute Normalize(OneWireCouncilModelRoute route)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(route);
            route.ModelName = route.ModelName?.Trim() ?? string.Empty;
            route.HardwareName = string.IsNullOrWhiteSpace(route.HardwareName)
                ? route.HardwareKind == OneWireHardwareKind.Auto ? "Automatic" : route.HardwareKind.ToString()
                : route.HardwareName.Trim();
            route.HardwareIndex = route.HardwareKind == OneWireHardwareKind.Auto ? -1 : Math.Max(0, route.HardwareIndex);
            route.MinOutputTokens = Math.Clamp(route.MinOutputTokens <= 0 ? runtimePolicy.GetInt("MinCouncilOutputTokens") : route.MinOutputTokens, 1, MaximumRoadOutputTokens);
            route.MaxOutputTokens = Math.Clamp(Math.Max(route.MinOutputTokens, route.MaxOutputTokens), route.MinOutputTokens, MaximumRoadOutputTokens);
            route.MinContextTokens = Math.Clamp(route.MinContextTokens <= 0 ? runtimePolicy.GetInt("MinCouncilContextTokens") : route.MinContextTokens, 256, MaximumRoadContextTokens);
            route.MaxContextTokens = Math.Clamp(Math.Max(route.MinContextTokens, route.MaxContextTokens), route.MinContextTokens, MaximumRoadContextTokens);
            route.MaxConcurrentModelsOnLane = Math.Clamp(route.MaxConcurrentModelsOnLane, 1, 16);
            route.LoadPercentOverride = route.LoadPercentOverride is null ? null : NormalizeLoadPercent(route.LoadPercentOverride.Value);
            var isOllamaRoute = string.IsNullOrWhiteSpace(route.ProviderKind)
                || route.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase);
            route.OllamaNumGpu = isOllamaRoute
                ? route.HardwareKind switch
                {
                    OneWireHardwareKind.Cpu => 0,
                    OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator => route.OllamaNumGpu is > 0
                        ? route.OllamaNumGpu
                        : null,
                    _ => route.OllamaNumGpu is < 0 ? 0 : route.OllamaNumGpu
                }
                : null;
            logger.LogTrace("Normalized hardware road for model {ModelName}.", route.ModelName);
            return route;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not normalize a council hardware road.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes load percent as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int NormalizeLoadPercent(int value)
    {
    try
    {
            var normalized = Math.Clamp((int)Math.Round(value / 5d) * 5, 0, 100);
            logger.LogTrace("Normalized council hardware load from {RequestedLoad} to {NormalizedLoad} percent.", value, normalized);
            return normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilHardwareRoadConfigurationService)}.{nameof(NormalizeLoadPercent)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilHardwareRoadConfigurationService)}.{nameof(NormalizeLoadPercent)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs interpolate as part of the council hardware road configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="minimum">Minimum value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <param name="loadPercent">Load percent value supplied to the council hardware road configuration operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int Interpolate(int minimum, int maximum, int loadPercent)
    {
    try
    {
            minimum = Math.Max(0, minimum);
            maximum = Math.Max(minimum, maximum);
            var percent = NormalizeLoadPercent(loadPercent);
            var value = minimum + (long)Math.Round((maximum - minimum) * (percent / 100d), MidpointRounding.AwayFromZero);
            var result = (int)Math.Clamp(value, minimum, maximum);
            logger.LogTrace("Interpolated council hardware value {Value} for load {LoadPercent} percent.", result, percent);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilHardwareRoadConfigurationService)}.{nameof(Interpolate)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilHardwareRoadConfigurationService)}.{nameof(Interpolate)} failed.");
        throw;
    }
}
}
