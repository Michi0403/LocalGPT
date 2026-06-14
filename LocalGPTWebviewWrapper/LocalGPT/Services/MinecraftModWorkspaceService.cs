using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;

namespace LocalGPT.Services
{
    public partial class MinecraftModWorkspaceService(ILogger<MinecraftModWorkspaceService> logger) : IMinecraftModWorkspaceService
    {
    
        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "MinecraftModWorkspaces");

        public Task<MinecraftModWorkspace?> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var loader = CouncilChatStringFunctions.NormalizeLoader(request.Loader,logger);
                return loader switch
                {
                    "Fabric" => CreateFabricWorkspaceAsync(request, cancellationToken),
                    "NeoForge" => CreateNeoForgeWorkspaceAsync(request, cancellationToken),
                    "Paper" => CreatePaperPluginWorkspaceAsync(request, cancellationToken),
                    "Datapack" => CreateDatapackWorkspaceAsync(request, cancellationToken),
                    "Bedrock" => throw new NotSupportedException("Bedrock add-ons use behavior/resource packs, not Java mod workspaces. LocalGPT should add this as a separate exporter."),
                    _ => throw new NotSupportedException($"Unsupported Minecraft loader '{request.Loader}'.")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateDatapackMcmeta request {request.ToString()}");
                return null;
            }
        }

        public async Task<MinecraftModWorkspace?> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), CouncilChatStringFunctions.CreateFabricSettingsGradle(context.ProjectName), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.BuildFilePath, CouncilChatStringFunctions.CreateFabricBuildGradle(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), CouncilChatStaticsGeneral.CreateCommonGradleProperties(request, context, logger), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? CouncilChatStringFunctions.CreateFabricMainClass(context) : CouncilChatStringFunctions.CreateFabricEmptyMainClass(context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                if (request.IncludeLivingCitiesStarter)
                    await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), CouncilChatStringFunctions.CreateLivingCitiesReportClass(context.PackageName), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.MetadataPath, CouncilChatStringFunctions.CreateFabricMetadata(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                if (request.IncludeLivingCitiesStarter)
                    await WriteCommonResourceFilesAsync(request, context, cancellationToken);
                await WriteBuildHelperAsync(request, context, cancellationToken);
                await File.WriteAllTextAsync(context.ReadmePath, CouncilChatStringFunctions.CreateWorkspaceReadme(request, context, "Fabric"), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);

                logger.LogInformation("Created Fabric Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateFabricWorkspaceAsync request {request.ToString()}");
                return null;
            }
        }

        public async Task<MinecraftModWorkspace?> CreatePaperPluginWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), CouncilChatStringFunctions.CreatePaperSettingsGradle(context.ProjectName), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.BuildFilePath, CouncilChatStringFunctions.CreatePaperBuildGradle(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), CouncilChatStaticsGeneral.CreatePaperGradleProperties(request, context,logger), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.MainClassPath, CouncilChatStringFunctions.CreatePaperMainClass(context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.MetadataPath, CouncilChatStringFunctions.CreatePaperPluginYaml(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), CouncilChatStringFunctions.CreateLivingCitiesReportClass(context.PackageName), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await WriteBuildHelperAsync(request, context, cancellationToken);
                await File.WriteAllTextAsync(context.ReadmePath, CouncilChatStringFunctions.CreateWorkspaceReadme(request, context, "Paper plugin"), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);

                logger.LogInformation("Created Paper plugin workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreatePaperPluginWorkspaceAsync request {request.ToString()}");
                return null;
            }
          
        }

        public async Task<MinecraftModWorkspace?> CreateDatapackWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateDatapackLayout(request);
                var context = workspace.Context;
                var minecraftTagsRoot = Path.Combine(context.ProjectRoot, "data", "minecraft", "tags", "function");

                Directory.CreateDirectory(minecraftTagsRoot);
                Directory.CreateDirectory(Path.Combine(context.ProjectRoot, "docs"));

                await File.WriteAllTextAsync(context.MetadataPath, CouncilChatStaticsGeneral.CreateDatapackMcmeta(request, context,logger), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "load.json"), CouncilChatStringFunctions.CreateFunctionTag(context.ModId, "core/load"), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "tick.json"), CouncilChatStringFunctions.CreateFunctionTag(context.ModId, "core/tick"), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "core/load", CouncilChatStringFunctions.CreateDatapackLoadFunction(context), cancellationToken,logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "core/tick", CouncilChatStringFunctions.CreateDatapackTickFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "core/schedule", CouncilChatStringFunctions.CreateDatapackScheduleFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "city/create", CouncilChatStringFunctions.CreateDatapackCityCreateFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "city/check_villagers", CouncilChatStringFunctions.CreateDatapackCityCheckVillagersFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "city/create_new", CouncilChatStringFunctions.CreateDatapackCityCreateNewFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "city/already_exists", CouncilChatStringFunctions.CreateDatapackCityAlreadyExistsFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "city/register_banner", CouncilChatStringFunctions.CreateDatapackRegisterBannerFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "city/update_population", CouncilChatStringFunctions.CreateDatapackUpdatePopulationFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "citizens/register", CouncilChatStringFunctions.CreateDatapackCitizenRegisterFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "citizens/detect_new", CouncilChatStringFunctions.CreateDatapackCitizenDetectNewFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "citizens/aging", CouncilChatStringFunctions.CreateDatapackCitizenAgingFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "citizens/personalities", CouncilChatStringFunctions.CreateDatapackCitizenPersonalitiesFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "citizens/status", CouncilChatStringFunctions.CreateDatapackCitizenStatusFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "food/update", CouncilChatStringFunctions.CreateDatapackFoodUpdateFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "food/production", CouncilChatStringFunctions.CreateDatapackFoodProductionFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "food/consumption", CouncilChatStringFunctions.CreateDatapackFoodConsumptionFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "security/update", CouncilChatStringFunctions.CreateDatapackSecurityUpdateFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "security/golems", CouncilChatStringFunctions.CreateDatapackSecurityGolemsFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "security/nightwatch", CouncilChatStringFunctions.CreateDatapackSecurityNightwatchFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "chronicle/add_event", CouncilChatStringFunctions.CreateDatapackChronicleAddEventFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "chronicle/update", CouncilChatStringFunctions.CreateDatapackChronicleUpdateFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "ui/give_admin_book", CouncilChatStringFunctions.CreateDatapackAdminBookFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "ui/townhall", CouncilChatStringFunctions.CreateDatapackTownHallFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "ui/status", CouncilChatStringFunctions.CreateDatapackReportFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "ui/chronicle", CouncilChatStringFunctions.CreateDatapackChronicleUiFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "quests/update", CouncilChatStringFunctions.CreateDatapackQuestUpdateFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "quests/generate", CouncilChatStringFunctions.CreateDatapackQuestGenerateFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "buildings/init", CouncilChatStringFunctions.CreateDatapackBuildingsInitFunction(), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "buildings/register_house", CouncilChatStringFunctions.CreateDatapackRegisterHouseFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "buildings/debug_list", CouncilChatStringFunctions.CreateDatapackBuildingDebugListFunction(context), cancellationToken, logger);
                await CouncilChatStaticsGeneral.WriteDatapackFunctionAsync(context, "debug/reset_city", CouncilChatStringFunctions.CreateDatapackResetCityFunction(context), cancellationToken, logger);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md"), CouncilChatStringFunctions.CreateLivingCitiesPlan(request), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "reference-benchmark.md"), CouncilChatStringFunctions.CreateDatapackBenchmarkNotes(context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "build-local.ps1"), CouncilChatStringFunctions.CreateDatapackBuildScript(context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.ReadmePath, CouncilChatStringFunctions.CreateDatapackReadme(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);

                logger.LogInformation("Created Minecraft datapack workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult("powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1", "Copy the zip from build\\ to a world's datapacks folder, then run /reload.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateDatapackWorkspaceAsync request {request.ToString()}");
                return null;
            }
          
        }

        public async Task<MinecraftModWorkspace?> CreateNeoForgeWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), CouncilChatStringFunctions.CreateNeoForgeSettingsGradle(context.ProjectName), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.BuildFilePath, CouncilChatStringFunctions.CreateNeoForgeBuildGradle(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), CouncilChatStaticsGeneral.CreateCommonGradleProperties(request, context,logger), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? CouncilChatStringFunctions.CreateNeoForgeMainClass(context) : CouncilChatStringFunctions.CreateNeoForgeEmptyMainClass(context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                if (request.IncludeLivingCitiesStarter)
                    await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), CouncilChatStringFunctions.CreateLivingCitiesReportClass(context.PackageName), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(context.MetadataPath, CouncilChatStringFunctions.CreateNeoForgeMetadata(request, context), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                if (request.IncludeLivingCitiesStarter)
                    await WriteCommonResourceFilesAsync(request, context, cancellationToken);
                await WriteBuildHelperAsync(request, context, cancellationToken);
                await File.WriteAllTextAsync(context.ReadmePath, CouncilChatStringFunctions.CreateWorkspaceReadme(request, context, "NeoForge"), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);

                logger.LogInformation("Created NeoForge Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateNeoForgeWorkspaceAsync request {request.ToString()}");
                return null;
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
                logger.LogError(ex, $"Error in IsPathInsideWorkspaceRoot path {path.ToString()}");
                return false;
            }
        }

        private GlobalVariableSlopCollectionToRemove.WorkspaceLayout? CreateWorkspaceLayout(MinecraftModBuildRequest request)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var projectName = CouncilChatStringFunctions.NormalizeName(request.ProjectName, "LivingCities");
                var modId = CouncilChatStringFunctions.NormalizeModId(request.ModId, "living_cities");
                var packageName = CouncilChatStringFunctions.NormalizePackageName(request.PackageName);
                var projectRoot = GetUniqueProjectPath(projectName);
                var loader = CouncilChatStringFunctions.NormalizeLoader(request.Loader,logger);
                var mainClassName = CouncilChatStringFunctions.ToPascalCase(modId,logger) + (loader == "Paper" ? "Plugin" : "Mod");
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
                logger.LogError(ex, $"Error in CreateWorkspaceLayout request {request.ToString()}");
                return null;
            }
        }

        private WorkspaceLayout? CreateDatapackLayout(MinecraftModBuildRequest request)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var projectName = CouncilChatStringFunctions.NormalizeName(request.ProjectName, "LivingCitiesDatapack");
                var modId = CouncilChatStringFunctions.NormalizeModId(request.ModId, "living_cities");
                var projectRoot = GetUniqueProjectPath(projectName);

                var context = new WorkspaceContext(
                    ProjectName: projectName,
                    ModId: modId,
                    PackageName: CouncilChatStringFunctions.NormalizePackageName(request.PackageName),
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
                logger.LogError(ex, $"Error in CreateDatapackLayout request {request.ToString()}");
                return null;
            }
        }

        private async Task WriteCommonResourceFilesAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            try
            {
                var langPath = Path.Combine(context.AssetsRoot, "lang", "en_us.json");
                var itemModelPath = Path.Combine(context.AssetsRoot, "models", "item", "city_charter.json");
                var planPath = Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md");

                await File.WriteAllTextAsync(langPath, CouncilChatStringFunctions.CreateEnglishLang(context.ModId), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(itemModelPath, CouncilChatStringFunctions.CreateCityCharterModel(), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
                await File.WriteAllTextAsync(planPath, CouncilChatStringFunctions.CreateLivingCitiesPlan(request), GlobalVariableSlopCollectionToRemove.Utf8NoBom, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteCommonResourceFilesAsync request {request.ToString()} context {context.ToString()}");
            }
        }

        private async Task WriteBuildHelperAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            try
            {
                var scriptPath = Path.Combine(context.ProjectRoot, "build-local.ps1");
                await File.WriteAllTextAsync(scriptPath, CouncilChatStaticsGeneral.CreateBuildLocalScript(request,logger), Utf8NoBom, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteBuildHelperAsync request {request.ToString()} context {context.ToString()}");
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
                logger.LogError(ex, $"Error in GetUniqueProjectPath projectName {projectName.ToString()}");
                return string.Empty;
            }
        }
    }
}