using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates model preset behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the model preset workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the model preset workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ModelPresetService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<ModelPresetService> logger) : IModelPresetService
{
    /// <summary>
    /// Retrieves presets as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="includeArchived">Value indicating whether include archived should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<CouncilModelPreset>> GetPresetsAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var query = db.CouncilModelPresets.AsNoTracking();
            if (!includeArchived)
                query = query.Where(item => !item.IsArchived);

            var presets = await query
                .OrderByDescending(item => item.IsDefault)
                .ThenBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var preset in presets)
                NormalizeLoadedPreset(preset);
            return presets;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(GetPresetsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(GetPresetsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Persists preset as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preset">Preset value supplied to the model preset operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The council model preset produced by the operation.</returns>
    public async Task<CouncilModelPreset> SavePresetAsync(CouncilModelPreset preset, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        if (!userConfirmed)
            throw new InvalidOperationException("Fresh human confirmation is required before saving a model preset.");
        ArgumentException.ThrowIfNullOrWhiteSpace(preset.Name);
        var models = JsonSerializer.Deserialize<List<string>>(preset.ModelNamesJson) ?? [];
        models = models.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (models.Count == 0)
            throw new InvalidOperationException("A model preset must contain at least one model.");
        List<OneWireCouncilModelRoute> routes;
        try
        {
            routes = JsonSerializer.Deserialize<List<OneWireCouncilModelRoute>>(preset.ModelRoutesJson) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("ModelRoutesJson is not valid 1-Wire model-route JSON.", ex);
        }
        routes = routes
            .Where(route => !string.IsNullOrWhiteSpace(route.ModelName) && models.Contains(route.ModelName, StringComparer.OrdinalIgnoreCase))
            .GroupBy(route => route.ModelName, StringComparer.OrdinalIgnoreCase)
            .Select(group => NormalizeRoute(group.First()))
            .ToList();

        await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var normalizedName = preset.Name.Trim();
        var entity = preset.Id == Guid.Empty
            ? null
            : await db.CouncilModelPresets.SingleOrDefaultAsync(item => item.Id == preset.Id, cancellationToken).ConfigureAwait(false);
        entity ??= await db.CouncilModelPresets.SingleOrDefaultAsync(item => item.Name == normalizedName, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new CouncilModelPreset { Id = preset.Id == Guid.Empty ? Guid.NewGuid() : preset.Id, CreatedAtUtc = DateTime.UtcNow };
            db.CouncilModelPresets.Add(entity);
        }
        else if (await db.CouncilModelPresets.AnyAsync(item => item.Id != entity.Id && item.Name == normalizedName, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Another model preset already uses this name.");
        }

        if (preset.IsDefault)
        {
            var defaults = await db.CouncilModelPresets.Where(item => item.IsDefault && item.Id != entity.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        entity.Name = normalizedName.Length <= 160 ? normalizedName : normalizedName[..160];
        entity.Description = preset.Description?.Trim() ?? string.Empty;
        entity.ModelNamesJson = JsonSerializer.Serialize(models);
        entity.ModelRoutesJson = JsonSerializer.Serialize(routes);
        entity.AllowParallelHardwareRoads = preset.AllowParallelHardwareRoads;
        entity.MaxOutputTokens = Math.Clamp(preset.MaxOutputTokens, 512, 262144);
        entity.MaxContextTokens = Math.Clamp(preset.MaxContextTokens, 2048, 262144);
        entity.MaxParallelModels = Math.Max(1, preset.MaxParallelModels);
        entity.OllamaNumGpu = preset.OllamaNumGpu is < 0 ? 0 : preset.OllamaNumGpu;
        entity.IncludeMemory = preset.IncludeMemory;
        entity.GenerateArtifacts = preset.GenerateArtifacts;
        entity.CreateProjectPerRun = preset.CreateProjectPerRun;
        entity.IsDefault = preset.IsDefault;
        entity.IsArchived = false;
        entity.IsUserApproved = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Saved council model preset {PresetId} ({PresetName}) with {ModelCount} model(s).", entity.Id, entity.Name, models.Count);
        return entity;
    }

    /// <summary>
    /// Normalizes loaded preset as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="preset">Preset value supplied to the model preset operation and used when producing its result.</param>
    private void NormalizeLoadedPreset(CouncilModelPreset preset)
    {
        try
        {
            var routes = JsonSerializer.Deserialize<List<OneWireCouncilModelRoute>>(preset.ModelRoutesJson) ?? [];
            preset.ModelRoutesJson = JsonSerializer.Serialize(routes
                .Where(route => route is not null && !string.IsNullOrWhiteSpace(route.ModelName))
                .Select(NormalizeRoute)
                .ToList());
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Council model preset {PresetId} contains invalid model-route JSON; the stored value remains available for repair.", preset.Id);
        }

        preset.MaxOutputTokens = Math.Clamp(preset.MaxOutputTokens, 512, 262144);
        preset.MaxContextTokens = Math.Clamp(preset.MaxContextTokens, 2048, 262144);
        preset.MaxParallelModels = Math.Max(1, preset.MaxParallelModels);
        preset.OllamaNumGpu = preset.OllamaNumGpu is < 0 ? 0 : preset.OllamaNumGpu;
    }

    /// <summary>
    /// Normalizes route as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the model preset operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    private OneWireCouncilModelRoute NormalizeRoute(OneWireCouncilModelRoute route)
    {
    try
    {
            route.ModelName = route.ModelName.Trim();
            route.ProviderKind = route.ProviderKind?.Trim() ?? string.Empty;
            route.ProviderName = route.ProviderName?.Trim() ?? string.Empty;
            route.ProviderEndpoint = route.ProviderEndpoint?.Trim() ?? string.Empty;
            route.ProviderModelName = route.ProviderModelName?.Trim() ?? string.Empty;
            var isLegacyOllamaRoute = string.IsNullOrWhiteSpace(route.ProviderKind)
                && string.IsNullOrWhiteSpace(route.ProviderName)
                && string.IsNullOrWhiteSpace(route.ProviderEndpoint)
                && string.IsNullOrWhiteSpace(route.ProviderModelName)
                && !new ProviderModelIdentity().LooksProviderQualified(route.ModelName);
            if (isLegacyOllamaRoute)
            {
                route.ProviderKind = ProviderModelKinds.Ollama;
                route.ProviderName = "Ollama";
                route.ProviderModelName = route.ModelName;
            }
            if (!route.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                route.OllamaNumGpu = null;
            route.HardwareName = route.HardwareName?.Trim() ?? string.Empty;
            route.HardwareIndex = Math.Max(-1, route.HardwareIndex);
            route.MinOutputTokens = Math.Clamp(route.MinOutputTokens, 128, 262144);
            route.MaxOutputTokens = Math.Clamp(Math.Max(route.MinOutputTokens, route.MaxOutputTokens), route.MinOutputTokens, 262144);
            route.MinContextTokens = Math.Clamp(route.MinContextTokens, 512, 262144);
            route.MaxContextTokens = Math.Clamp(Math.Max(route.MinContextTokens, route.MaxContextTokens), route.MinContextTokens, 262144);
            route.OllamaNumGpu = route.HardwareKind switch
            {
                OneWireHardwareKind.Cpu => 0,
                OneWireHardwareKind.Gpu or OneWireHardwareKind.Accelerator => route.OllamaNumGpu is > 0
                    ? route.OllamaNumGpu
                    : null,
                _ => route.OllamaNumGpu is < 0 ? 0 : route.OllamaNumGpu
            };
            if (!route.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                route.OllamaNumGpu = null;
            route.SelfReportedDxFunctions = NormalizeValues(route.SelfReportedDxFunctions);
            route.SelfReportedControllerMethods = NormalizeValues(route.SelfReportedControllerMethods);
            route.SelfReportedOrganicCapabilities = NormalizeValues(route.SelfReportedOrganicCapabilities);
            route.SelfReportedSkills = NormalizeValues(route.SelfReportedSkills);
            return route;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(NormalizeRoute)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(NormalizeRoute)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes values as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">String dependency used by the model preset workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> NormalizeValues(IEnumerable<string>? values) {
    try
    {
        return (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(128)
        .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(NormalizeValues)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(NormalizeValues)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs archive preset as part of the model preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="presetId">Identifier of the preset to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task ArchivePresetAsync(Guid presetId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before archiving a model preset.");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.CouncilModelPresets.SingleOrDefaultAsync(item => item.Id == presetId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Model preset {presetId} was not found.");
            entity.IsArchived = true;
            entity.IsDefault = false;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(ArchivePresetAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ModelPresetService)}.{nameof(ArchivePresetAsync)} failed.");
        throw;
    }
}
}
