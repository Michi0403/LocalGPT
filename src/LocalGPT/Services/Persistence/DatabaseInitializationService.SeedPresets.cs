using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database initialization behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DatabaseInitializationService
{
    /// <summary>
    /// Performs seed council model presets as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedCouncilModelPresetsAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
    try
    {
            var existingNames = await db.CouncilModelPresets
                .Select(item => item.Name)
                .ToListAsync(token)
                .ConfigureAwait(false);
            var existing = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var presets = CreateDefaultCouncilModelPresets();

            foreach (var preset in presets.Where(item => !existing.Contains(item.Name)))
                db.CouncilModelPresets.Add(preset);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedCouncilModelPresetsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedCouncilModelPresetsAsync)} failed.");
        throw;
    }
}

    /// <summary>Builds the authoritative maintained model presets for additive repairs and post-commit verification.</summary>
    private CouncilModelPreset[] CreateDefaultCouncilModelPresets()
    {
        try
        {
            var presets = new[]
            {
                BuildPreset(
                    "Reactive ASCII Gameplay",
                    "Low-latency one-model-at-a-time defaults for in-chat runtime games. Auto GPU offload supports NVIDIA and AMD through Ollama without hard-coded layer guesses; a bounded benchmark team may refine a copied preset later.",
                    ["qwen3.5:0.8b", "qwen3.5:2b", "qwen3.5:4b", "llama3.2:1b", "phi3:3.8b"],
                    [
                        Route("qwen3.5:0.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 1536, 2048, 8192, null),
                        Route("qwen3.5:2b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 2048, 4096, 12288, null),
                        Route("qwen3.5:4b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 3072, 4096, 16384, null),
                        Route("llama3.2:1b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 1536, 2048, 8192, null),
                        Route("phi3:3.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 3072, 4096, 16384, null)
                    ],
                    isDefault: false,
                    maxParallel: 1,
                    includeMemory: false,
                    createProjectPerRun: false),
                BuildPreset(
                    "Adaptive Mixed Hardware Council",
                    "First-run default for 8-16 GB machines: gemma4:e4b for fast subtasks, deepseek-r1:8b for reasoning and gemma4:12b as the main member, each with its own CPU/GPU token road.",
                    ["gemma4:12b", "deepseek-r1:8b", "gemma4:e4b"],
                    [
                        Route("gemma4:12b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 12288, 4096, 65536, null),
                        Route("deepseek-r1:8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 8192, 4096, 32768, null),
                        Route("gemma4:e4b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 4096, 4096, 32768, null)
                    ],
                    isDefault: true,
                    maxParallel: 3),
                BuildPreset(
                    "Learning Round",
                    "Database, chat-memory, regex, logs, project and knowledge maintenance round with conservative model-specific roads.",
                    ["gemma4:12b", "deepseek-r1:8b", "gemma4:e4b"],
                    [
                        Route("gemma4:12b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 12288, 4096, 65536, null),
                        Route("deepseek-r1:8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 8192, 4096, 32768, null),
                        Route("gemma4:e4b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 4096, 4096, 32768, null)
                    ],
                    isDefault: false,
                    maxParallel: 3),
                BuildPreset(
                    "Large GPU Council (24 GB)",
                    "For 20-24 GB GPUs: gemma4:26b leads, gemma4:12b reviews and gemma4:e4b takes small subtasks.",
                    ["gemma4:26b", "gemma4:12b", "gemma4:e4b"],
                    [
                        Route("gemma4:26b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 1024, 16384, 8192, 65536, null),
                        Route("gemma4:12b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 12288, 4096, 65536, null),
                        Route("gemma4:e4b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 4096, 4096, 32768, null)
                    ],
                    isDefault: false,
                    maxParallel: 1),
                BuildPreset(
                    "Fast Game Council (Low-B)",
                    "Small low-latency models for player controllers, creature/object subdirectors and GameDirector review. The deterministic runtime remains authoritative.",
                    ["qwen3.5:0.8b", "qwen3.5:2b", "llama3.2:1b", "codegemma:2b"],
                    [
                        Route("qwen3.5:0.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 128, 1024, 2048, 8192, null),
                        Route("qwen3.5:2b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 192, 1536, 4096, 12288, null),
                        Route("llama3.2:1b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 128, 1024, 2048, 8192, null),
                        Route("codegemma:2b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 192, 1536, 4096, 12288, null)
                    ],
                    isDefault: false,
                    maxParallel: 2,
                    includeMemory: false,
                    createProjectPerRun: false),
                BuildPreset(
                    "Code Curator Council",
                    "Coder, architecture and independent review models for C#, PowerShell, Java and Minecraft development teams.",
                    ["qwen3-coder:30b", "deepseek-coder-v2:16b", "deepseek-coder:6.7b", "codegemma:7b", "qwen3:8b"],
                    [
                        Route("qwen3-coder:30b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 1024, 24576, 8192, 131072, null),
                        Route("deepseek-coder-v2:16b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 768, 16384, 8192, 98304, null),
                        Route("deepseek-coder:6.7b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 12288, 4096, 65536, null),
                        Route("codegemma:7b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 12288, 4096, 65536, null),
                        Route("qwen3:8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 12288, 4096, 65536, null)
                    ],
                    isDefault: false,
                    maxParallel: 3),
                BuildPreset(
                    "Benchmark Candidate Pool",
                    "Broad installed-model candidate list for the adaptive benchmark Council. Missing models are ignored until the user installs them.",
                    ["qwen3.5:0.8b", "qwen3.5:2b", "qwen3.5:4b", "qwen3:8b", "qwen3-coder:30b", "deepseek-coder:6.7b", "deepseek-coder-v2:16b", "codegemma:7b", "llama3.2:1b", "phi3:3.8b"],
                    [
                        Route("qwen3.5:0.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 128, 1024, 2048, 8192, null),
                        Route("qwen3.5:2b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 192, 1536, 4096, 12288, null),
                        Route("qwen3.5:4b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 3072, 4096, 16384, null),
                        Route("qwen3:8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 4096, 8192, 32768, null),
                        Route("qwen3-coder:30b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 8192, 8192, 65536, null),
                        Route("deepseek-coder:6.7b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 4096, 4096, 32768, null),
                        Route("deepseek-coder-v2:16b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 512, 6144, 8192, 49152, null),
                        Route("codegemma:7b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 4096, 4096, 32768, null),
                        Route("llama3.2:1b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 128, 1024, 2048, 8192, null),
                        Route("phi3:3.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 3072, 4096, 16384, null)
                    ],
                    isDefault: false,
                    maxParallel: 1,
                    includeMemory: false,
                    createProjectPerRun: false)
            };
            return presets;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building default Council model presets failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds preset as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="models">Models value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="routes">Routes value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="isDefault">Value indicating whether is default should apply to this operation.</param>
    /// <param name="maxParallel">Max parallel value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="includeMemory">Include-memory flag.</param>
    /// <param name="createProjectPerRun">Create-project-per-run flag.</param>
    /// <returns>The council model preset produced by the operation.</returns>
    private CouncilModelPreset BuildPreset(
        string name,
        string description,
        string[] models,
        OneWireCouncilModelRoute[] routes,
        bool isDefault,
        int maxParallel,
        bool includeMemory = true,
        bool createProjectPerRun = true) {
    try
    {
        return new()
    {
        Name = name,
        Description = description,
        ModelNamesJson = JsonSerializer.Serialize(models),
        ModelRoutesJson = JsonSerializer.Serialize(routes),
        AllowParallelHardwareRoads = true,
        MaxOutputTokens = routes.Max(route => route.MaxOutputTokens),
        MaxContextTokens = routes.Max(route => route.MaxContextTokens),
        MaxParallelModels = maxParallel,
        IncludeMemory = includeMemory,
        GenerateArtifacts = false,
        CreateProjectPerRun = createProjectPerRun,
        IsDefault = isDefault,
        IsUserApproved = true,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(BuildPreset)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(BuildPreset)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs route as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Model value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="kind">Kind value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="index">Index value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="minOutput">Min output value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="maxOutput">Max output value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="minContext">Min context value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="maxContext">Max context value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="numGpu">Num gpu value supplied to the database initialization operation and used when producing its result.</param>
    /// <returns>The one wire council model route produced by the operation.</returns>
    private OneWireCouncilModelRoute Route(
        string model,
        OneWireHardwareKind kind,
        int index,
        string name,
        int minOutput,
        int maxOutput,
        int minContext,
        int maxContext,
        int? numGpu) {
    try
    {
        return new()
    {
        ModelName = model,
        HardwareKind = kind,
        HardwareIndex = index,
        HardwareName = name,
        MinOutputTokens = minOutput,
        MaxOutputTokens = maxOutput,
        MinContextTokens = minContext,
        MaxContextTokens = maxContext,
        OllamaNumGpu = numGpu,
        MaxConcurrentModelsOnLane = 1,
        IsEnabled = true
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(Route)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(Route)} failed.");
        throw;
    }
}

}
