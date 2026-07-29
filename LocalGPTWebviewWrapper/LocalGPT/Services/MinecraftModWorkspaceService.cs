using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    public partial class MinecraftModWorkspaceService(ILogger<MinecraftModWorkspaceService> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        LocalGptCatalogService catalog) : IMinecraftModWorkspaceService
    {
    
        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "MinecraftModWorkspaces");

        public async Task<MinecraftModWorkspace> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var loader = councilText.NormalizeLoader(request.Loader, logger);
                var workspaceTask = loader switch
                {
                    "Fabric" => CreateFabricWorkspaceAsync(request, cancellationToken),
                    "NeoForge" => CreateNeoForgeWorkspaceAsync(request, cancellationToken),
                    "Paper" => CreatePaperPluginWorkspaceAsync(request, cancellationToken),
                    "Datapack" => CreateDatapackWorkspaceAsync(request, cancellationToken),
                    "Bedrock" => throw new NotSupportedException("Bedrock add-ons use behavior/resource packs, not Java mod workspaces. LocalGPT should add this as a separate exporter."),
                    _ => throw new NotSupportedException($"Unsupported Minecraft loader '{request.Loader}'.")
                };
                return await workspaceTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateWorkspaceAsync");
                throw;
            }
        }

        public async Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), councilText.CreateFabricSettingsGradle(context.ProjectName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.BuildFilePath, councilText.CreateFabricBuildGradle(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), councilRuntime.CreateCommonGradleProperties(request, context, logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? councilText.CreateFabricMainClass(context) : councilText.CreateFabricEmptyMainClass(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), councilText.CreateLivingCitiesReportClass(context.PackageName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MetadataPath, councilText.CreateFabricMetadata(request, context), catalog.Utf8NoBom, cancellationToken);
                if (request.IncludeLivingCitiesStarter)
                    await WriteCommonResourceFilesAsync(request, context, cancellationToken).ConfigureAwait(false);
                await WriteBuildHelperAsync(request, context, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, councilText.CreateWorkspaceReadme(request, context, "Fabric"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created Fabric Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateFabricWorkspaceAsync");
                throw;
            }
        }

        public async Task<MinecraftModWorkspace> CreatePaperPluginWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), councilText.CreatePaperSettingsGradle(context.ProjectName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.BuildFilePath, councilText.CreatePaperBuildGradle(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), councilRuntime.CreatePaperGradleProperties(request, context,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MainClassPath, councilText.CreatePaperMainClass(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MetadataPath, councilText.CreatePaperPluginYaml(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), councilText.CreateLivingCitiesReportClass(context.PackageName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await WriteBuildHelperAsync(request, context, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, councilText.CreateWorkspaceReadme(request, context, "Paper plugin"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created Paper plugin workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreatePaperPluginWorkspaceAsync");
                throw;
            }
          
        }

        public async Task<MinecraftModWorkspace> CreateDatapackWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateDatapackLayout(request);
                var context = workspace.Context;
                var minecraftTagsRoot = Path.Combine(context.ProjectRoot, "data", "minecraft", "tags", "function");

                Directory.CreateDirectory(minecraftTagsRoot);
                Directory.CreateDirectory(Path.Combine(context.ProjectRoot, "docs"));

                await File.WriteAllTextAsync(context.MetadataPath, councilRuntime.CreateDatapackMcmeta(request, context,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "load.json"), councilText.CreateFunctionTag(context.ModId, "core/load"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "tick.json"), councilText.CreateFunctionTag(context.ModId, "core/tick"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "core/load", councilText.CreateDatapackLoadFunction(context), cancellationToken,logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "core/tick", councilText.CreateDatapackTickFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "core/schedule", councilText.CreateDatapackScheduleFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "city/create", councilText.CreateDatapackCityCreateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "city/check_villagers", councilText.CreateDatapackCityCheckVillagersFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "city/create_new", councilText.CreateDatapackCityCreateNewFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "city/already_exists", councilText.CreateDatapackCityAlreadyExistsFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "city/register_banner", councilText.CreateDatapackRegisterBannerFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "city/update_population", councilText.CreateDatapackUpdatePopulationFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "citizens/register", councilText.CreateDatapackCitizenRegisterFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "citizens/detect_new", councilText.CreateDatapackCitizenDetectNewFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "citizens/aging", councilText.CreateDatapackCitizenAgingFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "citizens/personalities", councilText.CreateDatapackCitizenPersonalitiesFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "citizens/status", councilText.CreateDatapackCitizenStatusFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "food/update", councilText.CreateDatapackFoodUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "food/production", councilText.CreateDatapackFoodProductionFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "food/consumption", councilText.CreateDatapackFoodConsumptionFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "security/update", councilText.CreateDatapackSecurityUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "security/golems", councilText.CreateDatapackSecurityGolemsFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "security/nightwatch", councilText.CreateDatapackSecurityNightwatchFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "chronicle/add_event", councilText.CreateDatapackChronicleAddEventFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "chronicle/update", councilText.CreateDatapackChronicleUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "ui/give_admin_book", councilText.CreateDatapackAdminBookFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "ui/townhall", councilText.CreateDatapackTownHallFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "ui/status", councilText.CreateDatapackReportFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "ui/chronicle", councilText.CreateDatapackChronicleUiFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "quests/update", councilText.CreateDatapackQuestUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "quests/generate", councilText.CreateDatapackQuestGenerateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "buildings/init", councilText.CreateDatapackBuildingsInitFunction(), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "buildings/register_house", councilText.CreateDatapackRegisterHouseFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "buildings/debug_list", councilText.CreateDatapackBuildingDebugListFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteDatapackFunctionAsync(context, "debug/reset_city", councilText.CreateDatapackResetCityFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md"), councilText.CreateLivingCitiesPlan(request), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "reference-benchmark.md"), councilText.CreateDatapackBenchmarkNotes(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "build-local.ps1"), councilText.CreateDatapackBuildScript(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, councilText.CreateDatapackReadme(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created Minecraft datapack workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult("powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1", "Copy the zip from build\\ to a world's datapacks folder, then run /reload.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateDatapackWorkspaceAsync");
                throw;
            }
          
        }

        public async Task<MinecraftModWorkspace> CreateNeoForgeWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), councilText.CreateNeoForgeSettingsGradle(context.ProjectName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.BuildFilePath, councilText.CreateNeoForgeBuildGradle(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), councilRuntime.CreateCommonGradleProperties(request, context,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? councilText.CreateNeoForgeMainClass(context) : councilText.CreateNeoForgeEmptyMainClass(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), councilText.CreateLivingCitiesReportClass(context.PackageName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MetadataPath, councilText.CreateNeoForgeMetadata(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await WriteCommonResourceFilesAsync(request, context, cancellationToken).ConfigureAwait(false);
                await WriteBuildHelperAsync(request, context, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, councilText.CreateWorkspaceReadme(request, context, "NeoForge"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created NeoForge Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateNeoForgeWorkspaceAsync");
                throw;
            }
        }

        public bool IsPathInsideWorkspaceRoot(string path)
        {
            try
            {
                var root = Path.GetFullPath(WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var candidate = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Path {Path} is not a valid Minecraft workspace path.", path);
                return false;
            }
        }

        private LocalGptCatalogService.WorkspaceLayout CreateWorkspaceLayout(MinecraftModBuildRequest request)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var projectName = councilText.NormalizeName(request.ProjectName, "LivingCities");
                var modId = councilText.NormalizeModId(request.ModId, "living_cities");
                var packageName = councilText.NormalizePackageName(request.PackageName);
                var projectRoot = GetUniqueProjectPath(projectName);
                var loader = councilText.NormalizeLoader(request.Loader,logger);
                var mainClassName = councilText.ToPascalCase(modId,logger) + (loader == "Paper" ? "Plugin" : "Mod");
                var packagePath = packageName.Replace('.', Path.DirectorySeparatorChar);
                var javaRoot = Path.Combine(projectRoot, "src", "main", "java", packagePath);
                var resourceRoot = Path.Combine(projectRoot, "src", "main", "resources");
                var assetsRoot = Path.Combine(resourceRoot, "assets", modId);
                var langRoot = Path.Combine(assetsRoot, "lang");
                var itemModelsRoot = Path.Combine(assetsRoot, "models", "item");

                Directory.CreateDirectory(javaRoot);
                Directory.CreateDirectory(resourceRoot);
                Directory.CreateDirectory(langRoot);
                Directory.CreateDirectory(itemModelsRoot);
                Directory.CreateDirectory(Path.Combine(projectRoot, "docs"));

                var metadataPath = loader switch
                {
                    "NeoForge" => Path.Combine(resourceRoot, "META-INF", "neoforge.mods.toml"),
                    "Paper" => Path.Combine(resourceRoot, "plugin.yml"),
                    _ => Path.Combine(resourceRoot, "fabric.mod.json")
                };
                Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);

                var context = new WorkspaceContext(
                    ProjectName: projectName,
                    ModId: modId,
                    PackageName: packageName,
                    MainClassName: mainClassName,
                    ProjectRoot: projectRoot,
                    JavaRoot: javaRoot,
                    ResourceRoot: resourceRoot,
                    AssetsRoot: assetsRoot,
                    BuildFilePath: Path.Combine(projectRoot, "build.gradle"),
                    MainClassPath: Path.Combine(javaRoot, $"{mainClassName}.java"),
                    MetadataPath: metadataPath,
                    ReadmePath: Path.Combine(projectRoot, "LOCALGPT_MOD_BRIEF.md"));

                return new WorkspaceLayout(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateWorkspaceLayout");
                throw;
            }
        }

        private WorkspaceLayout CreateDatapackLayout(MinecraftModBuildRequest request)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var projectName = councilText.NormalizeName(request.ProjectName, "LivingCitiesDatapack");
                var modId = councilText.NormalizeModId(request.ModId, "living_cities");
                var projectRoot = GetUniqueProjectPath(projectName);

                var context = new WorkspaceContext(
                    ProjectName: projectName,
                    ModId: modId,
                    PackageName: councilText.NormalizePackageName(request.PackageName),
                    MainClassName: string.Empty,
                    ProjectRoot: projectRoot,
                    JavaRoot: string.Empty,
                    ResourceRoot: projectRoot,
                    AssetsRoot: string.Empty,
                    BuildFilePath: Path.Combine(projectRoot, "build-local.ps1"),
                    MainClassPath: Path.Combine(projectRoot, "data", modId, "function", "load.mcfunction"),
                    MetadataPath: Path.Combine(projectRoot, "pack.mcmeta"),
                    ReadmePath: Path.Combine(projectRoot, "LOCALGPT_DATAPACK_BRIEF.md"));

                Directory.CreateDirectory(projectRoot);
                return new WorkspaceLayout(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateDatapackLayout");
                throw;
            }
        }

        private async Task WriteCommonResourceFilesAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            try
            {
                var langPath = Path.Combine(context.AssetsRoot, "lang", "en_us.json");
                var itemModelPath = Path.Combine(context.AssetsRoot, "models", "item", "city_charter.json");
                var planPath = Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md");

                await File.WriteAllTextAsync(langPath, councilText.CreateEnglishLang(context.ModId), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(itemModelPath, councilText.CreateCityCharterModel(), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(planPath, councilText.CreateLivingCitiesPlan(request), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteCommonResourceFilesAsync");
                throw;
            }
        }

        private async Task WriteBuildHelperAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            try
            {
                var scriptPath = Path.Combine(context.ProjectRoot, "build-local.ps1");
                await File.WriteAllTextAsync(scriptPath, councilRuntime.CreateBuildLocalScript(request,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteBuildHelperAsync");
                throw;
            }
        }

        private string GetUniqueProjectPath(string projectName)
        {
            try
            {
                var basePath = Path.Combine(WorkspaceRoot, projectName);
                if (!Directory.Exists(basePath))
                    return basePath;

                for (var i = 2; ; i++)
                {
                    var path = Path.Combine(WorkspaceRoot, $"{projectName}-{i}");
                    if (!Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not allocate a Minecraft workspace path for project {ProjectName}.", projectName);
                throw;
            }
        }
    }
}
