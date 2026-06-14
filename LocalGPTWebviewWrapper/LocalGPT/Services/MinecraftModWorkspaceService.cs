using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public partial class MinecraftModWorkspaceService(ILogger<MinecraftModWorkspaceService> logger) : IMinecraftModWorkspaceService
    {
        private const string DefaultGradleVersion = "8.14.2";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "MinecraftModWorkspaces");

        public Task<MinecraftModWorkspace> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            var loader = NormalizeLoader(request.Loader);
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

        public async Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            var workspace = CreateWorkspaceLayout(request);
            var context = workspace.Context;

            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), CreateFabricSettingsGradle(context.ProjectName), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.BuildFilePath, CreateFabricBuildGradle(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), CreateCommonGradleProperties(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? CreateFabricMainClass(context) : CreateFabricEmptyMainClass(context), Utf8NoBom, cancellationToken);
            if (request.IncludeLivingCitiesStarter)
                await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), CreateLivingCitiesReportClass(context.PackageName), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.MetadataPath, CreateFabricMetadata(request, context), Utf8NoBom, cancellationToken);
            if (request.IncludeLivingCitiesStarter)
                await WriteCommonResourceFilesAsync(request, context, cancellationToken);
            await WriteBuildHelperAsync(request, context, cancellationToken);
            await File.WriteAllTextAsync(context.ReadmePath, CreateWorkspaceReadme(request, context, "Fabric"), Utf8NoBom, cancellationToken);

            logger.LogInformation("Created Fabric Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
            return workspace.ToResult();
        }

        public async Task<MinecraftModWorkspace> CreatePaperPluginWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            var workspace = CreateWorkspaceLayout(request);
            var context = workspace.Context;

            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), CreatePaperSettingsGradle(context.ProjectName), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.BuildFilePath, CreatePaperBuildGradle(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), CreatePaperGradleProperties(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.MainClassPath, CreatePaperMainClass(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.MetadataPath, CreatePaperPluginYaml(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), CreateLivingCitiesReportClass(context.PackageName), Utf8NoBom, cancellationToken);
            await WriteBuildHelperAsync(request, context, cancellationToken);
            await File.WriteAllTextAsync(context.ReadmePath, CreateWorkspaceReadme(request, context, "Paper plugin"), Utf8NoBom, cancellationToken);

            logger.LogInformation("Created Paper plugin workspace at {ProjectRoot}", context.ProjectRoot);
            return workspace.ToResult();
        }

        public async Task<MinecraftModWorkspace> CreateDatapackWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            var workspace = CreateDatapackLayout(request);
            var context = workspace.Context;
            var minecraftTagsRoot = Path.Combine(context.ProjectRoot, "data", "minecraft", "tags", "function");

            Directory.CreateDirectory(minecraftTagsRoot);
            Directory.CreateDirectory(Path.Combine(context.ProjectRoot, "docs"));

            await File.WriteAllTextAsync(context.MetadataPath, CreateDatapackMcmeta(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "load.json"), CreateFunctionTag(context.ModId, "core/load"), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "tick.json"), CreateFunctionTag(context.ModId, "core/tick"), Utf8NoBom, cancellationToken);
            await WriteDatapackFunctionAsync(context, "core/load", CreateDatapackLoadFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "core/tick", CreateDatapackTickFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "core/schedule", CreateDatapackScheduleFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "city/create", CreateDatapackCityCreateFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "city/check_villagers", CreateDatapackCityCheckVillagersFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "city/create_new", CreateDatapackCityCreateNewFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "city/already_exists", CreateDatapackCityAlreadyExistsFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "city/register_banner", CreateDatapackRegisterBannerFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "city/update_population", CreateDatapackUpdatePopulationFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "citizens/register", CreateDatapackCitizenRegisterFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "citizens/detect_new", CreateDatapackCitizenDetectNewFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "citizens/aging", CreateDatapackCitizenAgingFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "citizens/personalities", CreateDatapackCitizenPersonalitiesFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "citizens/status", CreateDatapackCitizenStatusFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "food/update", CreateDatapackFoodUpdateFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "food/production", CreateDatapackFoodProductionFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "food/consumption", CreateDatapackFoodConsumptionFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "security/update", CreateDatapackSecurityUpdateFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "security/golems", CreateDatapackSecurityGolemsFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "security/nightwatch", CreateDatapackSecurityNightwatchFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "chronicle/add_event", CreateDatapackChronicleAddEventFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "chronicle/update", CreateDatapackChronicleUpdateFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "ui/give_admin_book", CreateDatapackAdminBookFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "ui/townhall", CreateDatapackTownHallFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "ui/status", CreateDatapackReportFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "ui/chronicle", CreateDatapackChronicleUiFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "quests/update", CreateDatapackQuestUpdateFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "quests/generate", CreateDatapackQuestGenerateFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "buildings/init", CreateDatapackBuildingsInitFunction(), cancellationToken);
            await WriteDatapackFunctionAsync(context, "buildings/register_house", CreateDatapackRegisterHouseFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "buildings/debug_list", CreateDatapackBuildingDebugListFunction(context), cancellationToken);
            await WriteDatapackFunctionAsync(context, "debug/reset_city", CreateDatapackResetCityFunction(context), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md"), CreateLivingCitiesPlan(request), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "reference-benchmark.md"), CreateDatapackBenchmarkNotes(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "build-local.ps1"), CreateDatapackBuildScript(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.ReadmePath, CreateDatapackReadme(request, context), Utf8NoBom, cancellationToken);

            logger.LogInformation("Created Minecraft datapack workspace at {ProjectRoot}", context.ProjectRoot);
            return workspace.ToResult("powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1", "Copy the zip from build\\ to a world's datapacks folder, then run /reload.");
        }

        public async Task<MinecraftModWorkspace> CreateNeoForgeWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            var workspace = CreateWorkspaceLayout(request);
            var context = workspace.Context;

            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), CreateNeoForgeSettingsGradle(context.ProjectName), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.BuildFilePath, CreateNeoForgeBuildGradle(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), CreateCommonGradleProperties(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? CreateNeoForgeMainClass(context) : CreateNeoForgeEmptyMainClass(context), Utf8NoBom, cancellationToken);
            if (request.IncludeLivingCitiesStarter)
                await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), CreateLivingCitiesReportClass(context.PackageName), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(context.MetadataPath, CreateNeoForgeMetadata(request, context), Utf8NoBom, cancellationToken);
            if (request.IncludeLivingCitiesStarter)
                await WriteCommonResourceFilesAsync(request, context, cancellationToken);
            await WriteBuildHelperAsync(request, context, cancellationToken);
            await File.WriteAllTextAsync(context.ReadmePath, CreateWorkspaceReadme(request, context, "NeoForge"), Utf8NoBom, cancellationToken);

            logger.LogInformation("Created NeoForge Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
            return workspace.ToResult();
        }

        public bool IsPathInsideWorkspaceRoot(string path)
        {
            var root = Path.GetFullPath(WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private WorkspaceLayout CreateWorkspaceLayout(MinecraftModBuildRequest request)
        {
            Directory.CreateDirectory(WorkspaceRoot);

            var projectName = NormalizeName(request.ProjectName, "LivingCities");
            var modId = NormalizeModId(request.ModId, "living_cities");
            var packageName = NormalizePackageName(request.PackageName);
            var projectRoot = GetUniqueProjectPath(projectName);
            var loader = NormalizeLoader(request.Loader);
            var mainClassName = ToPascalCase(modId) + (loader == "Paper" ? "Plugin" : "Mod");
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

        private WorkspaceLayout CreateDatapackLayout(MinecraftModBuildRequest request)
        {
            Directory.CreateDirectory(WorkspaceRoot);

            var projectName = NormalizeName(request.ProjectName, "LivingCitiesDatapack");
            var modId = NormalizeModId(request.ModId, "living_cities");
            var projectRoot = GetUniqueProjectPath(projectName);

            var context = new WorkspaceContext(
                ProjectName: projectName,
                ModId: modId,
                PackageName: NormalizePackageName(request.PackageName),
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

        private async Task WriteCommonResourceFilesAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            var langPath = Path.Combine(context.AssetsRoot, "lang", "en_us.json");
            var itemModelPath = Path.Combine(context.AssetsRoot, "models", "item", "city_charter.json");
            var planPath = Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md");

            await File.WriteAllTextAsync(langPath, CreateEnglishLang(context.ModId), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(itemModelPath, CreateCityCharterModel(), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(planPath, CreateLivingCitiesPlan(request), Utf8NoBom, cancellationToken);
        }

        private async Task WriteBuildHelperAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            var scriptPath = Path.Combine(context.ProjectRoot, "build-local.ps1");
            await File.WriteAllTextAsync(scriptPath, CreateBuildLocalScript(request), Utf8NoBom, cancellationToken);
        }

        private string GetUniqueProjectPath(string projectName)
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


    }
}