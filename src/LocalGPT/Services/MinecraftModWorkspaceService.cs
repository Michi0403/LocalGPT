using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates minecraft mod workspace behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="projectService">Minecraft Java project service owning loader/project generation and dependency policy.</param>
    /// <param name="datapackService">Minecraft datapack service owning datapack content and pack metadata.</param>
    /// <param name="catalog">Local gpt catalog service dependency used by the minecraft mod workspace workflow to provide the corresponding application capability.</param>
    public partial class MinecraftModWorkspaceService(ILogger<MinecraftModWorkspaceService> logger,
        MinecraftProjectService projectService,
        MinecraftDatapackService datapackService,
        LocalGptCatalogService catalog,
        IPlatformRuntimeService platform,
        ILocalConsolePlatformService consolePlatform) : IMinecraftModWorkspaceService
    {
    
        /// <summary>
        /// Gets the workspace root value that forms part of the minecraft mod workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The workspace root value exposed by <see cref="MinecraftModWorkspaceService"/>.</value>
        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "MinecraftModWorkspaces");

        /// <summary>
        /// Creates workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        public async Task<MinecraftModWorkspace> CreateWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                var loader = projectService.NormalizeLoader(request.Loader, logger);
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

        /// <summary>
        /// Creates fabric workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        public async Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), projectService.CreateFabricSettingsGradle(context.ProjectName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.BuildFilePath, projectService.CreateFabricBuildGradle(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), projectService.CreateCommonGradleProperties(request, context, logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? projectService.CreateFabricMainClass(context) : projectService.CreateFabricEmptyMainClass(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), projectService.CreateLivingCitiesReportClass(context.PackageName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MetadataPath, projectService.CreateFabricMetadata(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await WriteCommonResourceFilesAsync(request, context, cancellationToken).ConfigureAwait(false);
                await WriteBuildHelperAsync(request, context, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, projectService.CreateWorkspaceReadme(request, context, "Fabric"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created Fabric Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult(CreateBuildCommandDisplay());
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

        /// <summary>
        /// Creates paper plugin workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        public async Task<MinecraftModWorkspace> CreatePaperPluginWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), projectService.CreatePaperSettingsGradle(context.ProjectName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.BuildFilePath, projectService.CreatePaperBuildGradle(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), projectService.CreatePaperGradleProperties(request, context,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MainClassPath, projectService.CreatePaperMainClass(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MetadataPath, projectService.CreatePaperPluginYaml(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), projectService.CreateLivingCitiesReportClass(context.PackageName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await WriteBuildHelperAsync(request, context, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, projectService.CreateWorkspaceReadme(request, context, "Paper plugin"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created Paper plugin workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult(CreateBuildCommandDisplay());
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

        /// <summary>
        /// Creates datapack workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        public async Task<MinecraftModWorkspace> CreateDatapackWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateDatapackLayout(request);
                var context = workspace.Context;
                var minecraftTagsRoot = Path.Combine(context.ProjectRoot, "data", "minecraft", "tags", "function");

                Directory.CreateDirectory(minecraftTagsRoot);
                Directory.CreateDirectory(Path.Combine(context.ProjectRoot, "docs"));

                await File.WriteAllTextAsync(context.MetadataPath, datapackService.CreateDatapackMcmeta(request, context,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "load.json"), datapackService.CreateFunctionTag(context.ModId, "core/load"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "tick.json"), datapackService.CreateFunctionTag(context.ModId, "core/tick"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "core/load", datapackService.CreateDatapackLoadFunction(context), cancellationToken,logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "core/tick", datapackService.CreateDatapackTickFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "core/schedule", datapackService.CreateDatapackScheduleFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "city/create", datapackService.CreateDatapackCityCreateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "city/check_villagers", datapackService.CreateDatapackCityCheckVillagersFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "city/create_new", datapackService.CreateDatapackCityCreateNewFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "city/already_exists", datapackService.CreateDatapackCityAlreadyExistsFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "city/register_banner", datapackService.CreateDatapackRegisterBannerFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "city/update_population", datapackService.CreateDatapackUpdatePopulationFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "citizens/register", datapackService.CreateDatapackCitizenRegisterFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "citizens/detect_new", datapackService.CreateDatapackCitizenDetectNewFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "citizens/aging", datapackService.CreateDatapackCitizenAgingFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "citizens/personalities", datapackService.CreateDatapackCitizenPersonalitiesFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "citizens/status", datapackService.CreateDatapackCitizenStatusFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "food/update", datapackService.CreateDatapackFoodUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "food/production", datapackService.CreateDatapackFoodProductionFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "food/consumption", datapackService.CreateDatapackFoodConsumptionFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "security/update", datapackService.CreateDatapackSecurityUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "security/golems", datapackService.CreateDatapackSecurityGolemsFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "security/nightwatch", datapackService.CreateDatapackSecurityNightwatchFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "chronicle/add_event", datapackService.CreateDatapackChronicleAddEventFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "chronicle/update", datapackService.CreateDatapackChronicleUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "ui/give_admin_book", datapackService.CreateDatapackAdminBookFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "ui/townhall", datapackService.CreateDatapackTownHallFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "ui/status", datapackService.CreateDatapackReportFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "ui/chronicle", datapackService.CreateDatapackChronicleUiFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "quests/update", datapackService.CreateDatapackQuestUpdateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "quests/generate", datapackService.CreateDatapackQuestGenerateFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "buildings/init", datapackService.CreateDatapackBuildingsInitFunction(), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "buildings/register_house", datapackService.CreateDatapackRegisterHouseFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "buildings/debug_list", datapackService.CreateDatapackBuildingDebugListFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await datapackService.WriteDatapackFunctionAsync(context, "debug/reset_city", datapackService.CreateDatapackResetCityFunction(context), cancellationToken, logger).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md"), projectService.CreateLivingCitiesPlan(request), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "reference-benchmark.md"), datapackService.CreateDatapackBenchmarkNotes(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "build-local.ps1"), datapackService.CreateDatapackBuildScript(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, datapackService.CreateDatapackReadme(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created Minecraft datapack workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult(CreateBuildCommandDisplay(), "Copy the zip from build/ to a world's datapacks folder, then run /reload.");
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

        /// <summary>
        /// Creates neo forge workspace as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The minecraft mod workspace produced by the operation.</returns>
        public async Task<MinecraftModWorkspace> CreateNeoForgeWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var workspace = CreateWorkspaceLayout(request);
                var context = workspace.Context;

                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "settings.gradle"), projectService.CreateNeoForgeSettingsGradle(context.ProjectName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.BuildFilePath, projectService.CreateNeoForgeBuildGradle(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "gradle.properties"), projectService.CreateCommonGradleProperties(request, context,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MainClassPath, request.IncludeLivingCitiesStarter ? projectService.CreateNeoForgeMainClass(context) : projectService.CreateNeoForgeEmptyMainClass(context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await File.WriteAllTextAsync(Path.Combine(context.JavaRoot, "LivingCitiesReport.java"), projectService.CreateLivingCitiesReportClass(context.PackageName), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.MetadataPath, projectService.CreateNeoForgeMetadata(request, context), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                if (request.IncludeLivingCitiesStarter)
                    await WriteCommonResourceFilesAsync(request, context, cancellationToken).ConfigureAwait(false);
                await WriteBuildHelperAsync(request, context, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(context.ReadmePath, projectService.CreateWorkspaceReadme(request, context, "NeoForge"), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Created NeoForge Minecraft mod workspace at {ProjectRoot}", context.ProjectRoot);
                return workspace.ToResult(CreateBuildCommandDisplay());
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

        /// <summary>
        /// Determines whether path inside workspace root as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="path">Path value supplied to the minecraft mod workspace operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsPathInsideWorkspaceRoot(string path)
        {
            try
            {
                return platform.IsSameOrDescendantPath(WorkspaceRoot, path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Path {Path} is not a valid Minecraft workspace path.", path);
                return false;
            }
        }

        /// <summary>
        /// Creates workspace layout as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <returns>The workspace layout produced by the operation.</returns>
        private WorkspaceLayout CreateWorkspaceLayout(MinecraftModBuildRequest request)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var projectName = projectService.NormalizeName(request.ProjectName, "LivingCities");
                var modId = projectService.NormalizeModId(request.ModId, "living_cities");
                var packageName = projectService.NormalizePackageName(request.PackageName);
                var projectRoot = GetUniqueProjectPath(projectName);
                var loader = projectService.NormalizeLoader(request.Loader,logger);
                var mainClassName = projectService.ToPascalCase(modId,logger) + (loader == "Paper" ? "Plugin" : "Mod");
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

        /// <summary>
        /// Creates datapack layout as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <returns>The workspace layout produced by the operation.</returns>
        private WorkspaceLayout CreateDatapackLayout(MinecraftModBuildRequest request)
        {
            try
            {
                Directory.CreateDirectory(WorkspaceRoot);

                var projectName = projectService.NormalizeName(request.ProjectName, "LivingCitiesDatapack");
                var modId = projectService.NormalizeModId(request.ModId, "living_cities");
                var projectRoot = GetUniqueProjectPath(projectName);

                var context = new WorkspaceContext(
                    ProjectName: projectName,
                    ModId: modId,
                    PackageName: projectService.NormalizePackageName(request.PackageName),
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

        /// <summary>
        /// Writes common resource files as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the minecraft mod workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task WriteCommonResourceFilesAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            try
            {
                var langPath = Path.Combine(context.AssetsRoot, "lang", "en_us.json");
                var itemModelPath = Path.Combine(context.AssetsRoot, "models", "item", "city_charter.json");
                var planPath = Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md");

                await File.WriteAllTextAsync(langPath, projectService.CreateEnglishLang(context.ModId), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(itemModelPath, datapackService.CreateCityCharterModel(), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(planPath, projectService.CreateLivingCitiesPlan(request), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Writes build helper as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the minecraft mod workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task WriteBuildHelperAsync(MinecraftModBuildRequest request, WorkspaceContext context, CancellationToken cancellationToken)
        {
            try
            {
                var scriptPath = Path.Combine(context.ProjectRoot, "build-local.ps1");
                await File.WriteAllTextAsync(scriptPath, projectService.CreateBuildLocalScript(request,logger), catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
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

        /// <summary>Returns the host-specific command users can run to build the generated workspace.</summary>
        private string CreateBuildCommandDisplay()
        {
            var command = consolePlatform.CreatePowerShellScriptCommand("build-local.ps1");
            return command.DisplayCommand;
        }

        /// <summary>
        /// Retrieves unique project path as part of the minecraft mod workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the minecraft mod workspace operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
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
