using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Persists hardware-spooler performance profiles and translates measured provider benchmark results into
/// reusable provider-qualified token/hardware roads.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the hardware performance preset workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the hardware performance preset workflow to provide the corresponding application capability.</param>
/// <param name="roadConfiguration">Council hardware road configuration service dependency used by the hardware performance preset workflow to provide the corresponding application capability.</param>
/// <param name="runConfigurations">Council run configuration service used when a user or Council applies a stored performance profile.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class HardwarePerformancePresetService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ICouncilHardwareRoadConfigurationService roadConfiguration,
    ICouncilRunConfigurationService runConfigurations,
    ILogger<HardwarePerformancePresetService> logger) : IHardwarePerformancePresetService
{
    /// <summary>
    /// Retrieves presets as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<HardwarePerformancePreset>> GetPresetsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var query = db.HardwarePerformancePresets.AsNoTracking();
            if (!includeArchived)
                query = query.Where(item => !item.IsArchived);

            var presets = await query
                .OrderByDescending(item => item.IsDefault)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var preset in presets)
                NormalizeLoadedPreset(preset);
            return presets;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Loading hardware performance presets was cancelled.");
            else
                logger.LogError(exception, "Loading hardware performance presets failed.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves preset as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<HardwarePerformancePreset?> GetPresetAsync(
        Guid presetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var preset = await db.HardwarePerformancePresets.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == presetId, cancellationToken)
                .ConfigureAwait(false);
            if (preset is not null)
                NormalizeLoadedPreset(preset);
            return preset;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading hardware performance preset {PresetId} was cancelled.", presetId);
            else
                logger.LogError(exception, "Reading hardware performance preset {PresetId} failed.", presetId);
            throw;
        }
    }

    /// <summary>
    /// Persists preset as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<HardwarePerformancePreset> SavePresetAsync(
        HardwarePerformancePreset preset,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before saving a hardware performance preset.");
            ArgumentNullException.ThrowIfNull(preset);
            ArgumentException.ThrowIfNullOrWhiteSpace(preset.Name);

            var routes = ParseAndNormalizeRoutes(preset.ModelRoutesJson);
            if (routes.Count == 0)
                throw new InvalidOperationException("A hardware performance preset must contain at least one provider/model route.");

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var normalizedName = NormalizeName(preset.Name);
            HardwarePerformancePreset? entity = null;
            if (preset.Id != Guid.Empty)
            {
                entity = await db.HardwarePerformancePresets
                    .SingleOrDefaultAsync(item => item.Id == preset.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
            if (entity is null && preset.SourceRunId is Guid sourceRunId)
            {
                entity = await db.HardwarePerformancePresets
                    .SingleOrDefaultAsync(item => item.SourceRunId == sourceRunId, cancellationToken)
                    .ConfigureAwait(false);
            }
            entity ??= await db.HardwarePerformancePresets
                .SingleOrDefaultAsync(item => item.Name == normalizedName, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                entity = new HardwarePerformancePreset
                {
                    Id = preset.Id == Guid.Empty ? Guid.NewGuid() : preset.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.HardwarePerformancePresets.Add(entity);
            }
            else if (await db.HardwarePerformancePresets.AnyAsync(
                item => item.Id != entity.Id && item.Name == normalizedName,
                cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Another hardware performance preset already uses this name.");
            }

            if (preset.IsDefault)
            {
                var otherDefaults = await db.HardwarePerformancePresets
                    .Where(item => item.IsDefault && item.Id != entity.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                foreach (var item in otherDefaults)
                    item.IsDefault = false;
            }

            entity.Name = normalizedName;
            entity.Description = NormalizeBounded(preset.Description, 1000);
            entity.ModelRoutesJson = JsonSerializer.Serialize(routes);
            entity.ResourceLoadPercent = roadConfiguration.NormalizeLoadPercent(preset.ResourceLoadPercent);
            entity.SourceRunId = preset.SourceRunId;
            entity.SourceKind = NormalizeBounded(string.IsNullOrWhiteSpace(preset.SourceKind) ? "Manual" : preset.SourceKind, 80);
            entity.IsDefault = preset.IsDefault;
            entity.IsArchived = false;
            entity.IsUserApproved = true;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Saved hardware performance preset {PresetId} ({PresetName}) with {RouteCount} provider-qualified route(s).",
                entity.Id,
                entity.Name,
                routes.Count);
            return entity;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving a hardware performance preset was cancelled.");
            else
                logger.LogError(exception, "Saving a hardware performance preset failed.");
            throw;
        }
    }

    /// <summary>
    /// Persists benchmark result as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<HardwarePerformancePreset> SaveBenchmarkResultAsync(
        ProviderModelBenchmarkReport report,
        string presetName,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before a benchmark result is stored as a hardware performance preset.");
            ArgumentNullException.ThrowIfNull(report);

            var successfulTargets = report.Targets
                .Where(target => string.IsNullOrWhiteSpace(target.Error)
                    && !string.IsNullOrWhiteSpace(target.Recommendation.ProfileName))
                .ToList();
            if (successfulTargets.Count == 0)
                throw new InvalidOperationException("The benchmark report contains no successful recommendation to store.");

            var routes = successfulTargets.Select(BuildBenchmarkRoute).ToList();
            var baseName = string.IsNullOrWhiteSpace(presetName)
                ? $"Benchmark performance · {DateTimeOffset.Now:yyyy-MM-dd HHmmss}"
                : presetName.Trim();
            var runSuffix = report.RunId.ToString("N")[..8];
            var normalizedName = NormalizeName($"{baseName} · {runSuffix}");

            var preset = new HardwarePerformancePreset
            {
                Name = normalizedName,
                Description = $"Measured provider-qualified performance profile from benchmark {report.RunId}. Selecting it in Chat hardware spooler applies only matching provider/endpoint/model routes and never changes Council membership.",
                ModelRoutesJson = JsonSerializer.Serialize(routes),
                ResourceLoadPercent = 100,
                SourceRunId = report.RunId,
                SourceKind = "ProviderBenchmark",
                IsDefault = false,
                IsUserApproved = true
            };
            var saved = await SavePresetAsync(preset, userConfirmed: true, cancellationToken).ConfigureAwait(false);
            report.AppliedPerformancePresetId = saved.Id;
            report.AppliedPerformancePresetName = saved.Name;
            return saved;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Converting benchmark {BenchmarkRunId} into a hardware performance preset was cancelled.", report?.RunId);
            else
                logger.LogError(exception, "Converting benchmark {BenchmarkRunId} into a hardware performance preset failed.", report?.RunId);
            throw;
        }
    }

    /// <summary>
    /// Deletes preset as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task DeletePresetAsync(
        Guid presetId,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before deleting a hardware performance preset.");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var entity = await db.HardwarePerformancePresets
                .SingleOrDefaultAsync(item => item.Id == presetId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Hardware performance preset {presetId} was not found.");
            db.HardwarePerformancePresets.Remove(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted hardware performance preset {PresetId} ({PresetName}).", entity.Id, entity.Name);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Deleting hardware performance preset {PresetId} was cancelled.", presetId);
            else
                logger.LogError(exception, "Deleting hardware performance preset {PresetId} failed.", presetId);
            throw;
        }
    }


    /// <summary>Applies a stored performance profile to the saved preparation configuration for the next Council run.</summary>
    /// <inheritdoc />
    public async Task<int> ApplyPresetToPreparationAsync(
        Guid presetId,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before applying a hardware performance preset.");

            var preset = await GetPresetAsync(presetId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Hardware performance preset {presetId} was not found.");
            var preparation = runConfigurations.GetPreparation()
                ?? throw new InvalidOperationException("No saved Council preparation configuration exists. Open Chat configuration and select the Council members first.");
            var (routes, appliedCount) = ApplyMatchingRoutes(preset, preparation.ModelNames, preparation.ModelRoutes);
            if (appliedCount == 0)
                throw new InvalidOperationException("The performance preset has no provider-qualified route matching the prepared Council models.");
            var (presetMaxOutputTokens, presetMaxContextTokens) = ResolvePresetCeilings(preset);

            runConfigurations.SavePreparation(new CouncilPreparationConfiguration(
                preparation.ModelNames,
                routes,
                preset.ResourceLoadPercent,
                presetMaxOutputTokens,
                presetMaxContextTokens,
                preparation.OllamaNumGpu,
                preparation.AllowParallelHardwareRoads,
                preparation.MaxParallelModels,
                preparation.ModelTimeoutSeconds,
                preparation.CritiqueRounds,
                preparation.IncludeMemory,
                preparation.CreateProjectPerRun,
                preparation.CouncilTeamKey));
            logger.LogInformation(
                "Applied hardware performance preset {PresetId} to {RouteCount} prepared Council route(s) without changing membership.",
                preset.Id,
                appliedCount);
            return appliedCount;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Applying hardware performance preset {PresetId} to Council preparation was cancelled.", presetId);
            else
                logger.LogError(exception, "Applying hardware performance preset {PresetId} to Council preparation failed.", presetId);
            throw;
        }
    }

    /// <summary>
    /// Applies preset to run as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<int> ApplyPresetToRunAsync(
        Guid presetId,
        Guid runId,
        bool userConfirmed,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before applying a hardware performance preset to a running Council.");

            var preset = await GetPresetAsync(presetId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Hardware performance preset {presetId} was not found.");
            var snapshot = runConfigurations.Get(runId)
                ?? throw new KeyNotFoundException($"Running Council {runId} was not found.");
            if (!snapshot.IsRunning)
                throw new InvalidOperationException($"Council {runId} is no longer running.");

            var (routes, appliedCount) = ApplyMatchingRoutes(preset, snapshot.Participants, snapshot.ModelRoutes);
            if (appliedCount == 0)
                throw new InvalidOperationException("The performance preset has no provider-qualified route matching the running Council participants.");
            var (presetMaxOutputTokens, presetMaxContextTokens) = ResolvePresetCeilings(preset);

            if (!runConfigurations.Update(
                    runId,
                    routes,
                    preset.ResourceLoadPercent,
                    presetMaxOutputTokens,
                    presetMaxContextTokens,
                    snapshot.FallbackOllamaNumGpu,
                    snapshot.AllowParallelHardwareRoads,
                    snapshot.MaxParallelModels,
                    snapshot.ModelTimeoutSeconds))
            {
                throw new InvalidOperationException($"Council {runId} could not accept the performance-profile revision.");
            }

            logger.LogInformation(
                "Applied hardware performance preset {PresetId} to {RouteCount} route(s) in running Council {RunId} without changing participants.",
                preset.Id,
                appliedCount,
                runId);
            return appliedCount;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Applying hardware performance preset {PresetId} to running Council {RunId} was cancelled.", presetId, runId);
            else
                logger.LogError(exception, "Applying hardware performance preset {PresetId} to running Council {RunId} failed.", presetId, runId);
            throw;
        }
    }

    /// <summary>Resolves the session token ceilings required to let every stored route reach its own saved maximum.</summary>
    /// <param name="preset">Preset value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The int max output tokens int max context tokens produced by the operation.</returns>
    private (int MaxOutputTokens, int MaxContextTokens) ResolvePresetCeilings(HardwarePerformancePreset preset)
    {
        try
        {
            var routes = ParseAndNormalizeRoutes(preset.ModelRoutesJson);
            if (routes.Count == 0)
                throw new InvalidOperationException("The hardware performance preset contains no usable provider/model route.");
            return (
                Math.Max(1, routes.Max(route => route.MaxOutputTokens)),
                Math.Max(256, routes.Max(route => route.MaxContextTokens)));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving session token ceilings failed for hardware performance preset {PresetId}.", preset.Id);
            throw;
        }
    }

    /// <summary>Copies only the hardware/token fields from matching provider-qualified preset routes.</summary>
    /// <param name="preset">Preset value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="modelNames">String dependency used by the hardware performance preset workflow to provide the corresponding application capability.</param>
    /// <param name="existingRoutes">One wire council model route dependency used by the hardware performance preset workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private (List<OneWireCouncilModelRoute> Routes, int AppliedCount) ApplyMatchingRoutes(
        HardwarePerformancePreset preset,
        IEnumerable<string> modelNames,
        IEnumerable<OneWireCouncilModelRoute> existingRoutes)
    {
        try
        {
            var presetRoutes = ParseAndNormalizeRoutes(preset.ModelRoutesJson)
                .ToDictionary(route => route.ModelName, StringComparer.OrdinalIgnoreCase);
            var routes = roadConfiguration.Synchronize(modelNames, existingRoutes)
                .Select(CloneRoute)
                .ToList();
            var appliedCount = 0;
            foreach (var target in routes)
            {
                if (!presetRoutes.TryGetValue(target.ModelName, out var source))
                    continue;

                CopyPerformanceFields(target, source);
                appliedCount++;
            }
            return (routes, appliedCount);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying matching hardware performance routes failed for preset {PresetId}.", preset.Id);
            throw;
        }
    }

    /// <summary>Copies hardware-spooler fields while preserving the target's provider identity and capability metadata.</summary>
    /// <param name="target">Target value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the hardware performance preset operation and used when producing its result.</param>
    private void CopyPerformanceFields(OneWireCouncilModelRoute target, OneWireCouncilModelRoute source)
    {
        try
        {
            target.HardwareKind = source.HardwareKind;
            target.HardwareIndex = source.HardwareIndex;
            target.HardwareName = source.HardwareName;
            target.MinOutputTokens = source.MinOutputTokens;
            target.MaxOutputTokens = source.MaxOutputTokens;
            target.MinContextTokens = source.MinContextTokens;
            target.MaxContextTokens = source.MaxContextTokens;
            target.OllamaNumGpu = source.OllamaNumGpu;
            target.LoadPercentOverride = source.LoadPercentOverride;
            target.IsEnabled = source.IsEnabled;
            target.MaxConcurrentModelsOnLane = source.MaxConcurrentModelsOnLane;
            roadConfiguration.Normalize(target);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying hardware performance fields failed for model {ModelName}.", target.ModelName);
            throw;
        }
    }

    /// <summary>Creates an independent route copy so applying a profile cannot mutate a stored or shared runtime snapshot.</summary>
    /// <param name="route">Route value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    private OneWireCouncilModelRoute CloneRoute(OneWireCouncilModelRoute route)
    {
        try
        {
            return new OneWireCouncilModelRoute
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
                SelfReportedDxFunctions = route.SelfReportedDxFunctions?.ToList() ?? [],
                SelfReportedControllerMethods = route.SelfReportedControllerMethods?.ToList() ?? [],
                SelfReportedOrganicCapabilities = route.SelfReportedOrganicCapabilities?.ToList() ?? [],
                SelfReportedSkills = route.SelfReportedSkills?.ToList() ?? [],
                IsEnabled = route.IsEnabled,
                MaxConcurrentModelsOnLane = route.MaxConcurrentModelsOnLane
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cloning a hardware performance route failed for model {ModelName}.", route.ModelName);
            throw;
        }
    }

    /// <summary>
    /// Builds benchmark route as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="target">Target value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    private OneWireCouncilModelRoute BuildBenchmarkRoute(ProviderModelBenchmarkTargetResult target)
    {
        try
        {
            var successfulProfiles = target.Profiles
                .Where(profile => profile.Tasks.Count > 0 && profile.Tasks.Any(task => task.Succeeded))
                .ToList();
            var minOutput = successfulProfiles.Count == 0
                ? Math.Min(256, target.Recommendation.OutputTokens)
                : successfulProfiles.Min(profile => profile.OutputTokens);
            var maxOutput = successfulProfiles.Count == 0
                ? target.Recommendation.OutputTokens
                : successfulProfiles.Max(profile => profile.OutputTokens);
            var minContext = successfulProfiles.Count == 0
                ? Math.Min(2048, target.Recommendation.ContextTokens)
                : successfulProfiles.Min(profile => profile.ContextTokens);
            var maxContext = successfulProfiles.Count == 0
                ? target.Recommendation.ContextTokens
                : successfulProfiles.Max(profile => profile.ContextTokens);

            minOutput = Math.Min(minOutput, target.Recommendation.OutputTokens);
            maxOutput = Math.Max(maxOutput, target.Recommendation.OutputTokens);
            minContext = Math.Min(minContext, target.Recommendation.ContextTokens);
            maxContext = Math.Max(maxContext, target.Recommendation.ContextTokens);

            var route = new OneWireCouncilModelRoute
            {
                ModelName = target.Model.SelectionKey,
                ProviderKind = target.Model.ProviderKind,
                ProviderName = target.Model.ProviderName,
                ProviderEndpoint = target.Model.Endpoint,
                ProviderModelName = target.Model.ModelName,
                HardwareKind = OneWireHardwareKind.Auto,
                HardwareIndex = -1,
                HardwareName = $"Benchmark · {target.Recommendation.ProfileName}",
                MinOutputTokens = minOutput,
                MaxOutputTokens = maxOutput,
                MinContextTokens = minContext,
                MaxContextTokens = maxContext,
                OllamaNumGpu = target.Model.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                    ? target.Recommendation.OllamaNumGpu
                    : null,
                LoadPercentOverride = ResolveRecommendedLoadPercent(
                    minOutput,
                    maxOutput,
                    target.Recommendation.OutputTokens,
                    minContext,
                    maxContext,
                    target.Recommendation.ContextTokens),
                IsEnabled = true,
                MaxConcurrentModelsOnLane = 1
            };
            return roadConfiguration.Normalize(route);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a benchmark performance road failed for provider-qualified model {ModelIdentity}.", target.Model.StableId);
            throw;
        }
    }

    /// <summary>
    /// Resolves recommended load percent as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="minOutput">Min output value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="maxOutput">Max output value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="recommendedOutput">Recommended output value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="minContext">Min context value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="maxContext">Max context value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="recommendedContext">Recommended context value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ResolveRecommendedLoadPercent(
        int minOutput,
        int maxOutput,
        int recommendedOutput,
        int minContext,
        int maxContext,
        int recommendedContext)
    {
        try
        {
            var outputPercent = ResolvePercent(minOutput, maxOutput, recommendedOutput);
            var contextPercent = ResolvePercent(minContext, maxContext, recommendedContext);
            return roadConfiguration.NormalizeLoadPercent((int)Math.Round((outputPercent + contextPercent) / 2d));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the recommended benchmark load percentage failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves percent as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="minimum">Minimum value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="selected">Selected value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ResolvePercent(int minimum, int maximum, int selected)
    {
        try
        {
            if (maximum <= minimum)
                return 100;
            return Math.Clamp((int)Math.Round((selected - minimum) * 100d / (maximum - minimum)), 0, 100);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a benchmark range percentage failed.");
            throw;
        }
    }

    /// <summary>
    /// Parses and normalize routes as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="routesJson">Routes json value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<OneWireCouncilModelRoute> ParseAndNormalizeRoutes(string routesJson)
    {
        try
        {
            var routes = JsonSerializer.Deserialize<List<OneWireCouncilModelRoute>>(routesJson) ?? [];
            return routes
                .Where(route => route is not null && !string.IsNullOrWhiteSpace(route.ModelName))
                .GroupBy(route => route.ModelName, StringComparer.OrdinalIgnoreCase)
                .Select(group => roadConfiguration.Normalize(group.First()))
                .ToList();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Hardware performance preset route JSON is invalid.");
            throw new InvalidOperationException("ModelRoutesJson is not valid 1-Wire model-route JSON.", exception);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing hardware performance preset routes failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes loaded preset as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preset">Preset value supplied to the hardware performance preset operation and used when producing its result.</param>
    private void NormalizeLoadedPreset(HardwarePerformancePreset preset)
    {
        try
        {
            preset.ModelRoutesJson = JsonSerializer.Serialize(ParseAndNormalizeRoutes(preset.ModelRoutesJson));
            preset.ResourceLoadPercent = roadConfiguration.NormalizeLoadPercent(preset.ResourceLoadPercent);
            preset.SourceKind = NormalizeBounded(string.IsNullOrWhiteSpace(preset.SourceKind) ? "Manual" : preset.SourceKind, 80);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Hardware performance preset {PresetId} contains invalid route JSON; the stored value remains visible for repair.", preset.Id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing loaded hardware performance preset {PresetId} failed.", preset.Id);
            throw;
        }
    }

    /// <summary>
    /// Normalizes name as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeName(string value)
    {
        try
        {
            var normalized = value.Trim();
            return normalized[..Math.Min(normalized.Length, 160)];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a hardware performance preset name failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes bounded as part of the hardware performance preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <param name="maxLength">Max length value supplied to the hardware performance preset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeBounded(string? value, int maxLength)
    {
        try
        {
            var normalized = value?.Trim() ?? string.Empty;
            return normalized[..Math.Min(normalized.Length, maxLength)];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing bounded hardware performance preset text failed.");
            throw;
        }
    }
}
