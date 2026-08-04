using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

public sealed class ModelPresetService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<ModelPresetService> logger) : IModelPresetService
{
    public async Task<IReadOnlyList<CouncilModelPreset>> GetPresetsAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
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

    public async Task<CouncilModelPreset> SavePresetAsync(CouncilModelPreset preset, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        if (!userConfirmed)
            throw new InvalidOperationException("Fresh human confirmation is required before saving a model preset.");
        ArgumentException.ThrowIfNullOrWhiteSpace(preset.Name);
        var models = JsonSerializer.Deserialize<List<string>>(preset.ModelNamesJson) ?? [];
        models = models.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(24).ToList();
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
            .Take(24)
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
        entity.MaxParallelModels = Math.Clamp(preset.MaxParallelModels, 1, 8);
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
        preset.MaxParallelModels = Math.Clamp(preset.MaxParallelModels, 1, 8);
        preset.OllamaNumGpu = preset.OllamaNumGpu is < 0 ? 0 : preset.OllamaNumGpu;
    }

    private OneWireCouncilModelRoute NormalizeRoute(OneWireCouncilModelRoute route)
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

    private List<string> NormalizeValues(IEnumerable<string>? values) => (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(128)
        .ToList();

    public async Task ArchivePresetAsync(Guid presetId, bool userConfirmed, CancellationToken cancellationToken = default)
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
}
