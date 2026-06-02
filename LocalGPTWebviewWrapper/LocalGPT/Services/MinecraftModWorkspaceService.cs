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

        private static string NormalizeName(string value, string fallback)
        {
            var normalized = NameCleaner().Replace(value.Trim(), "");
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static string NormalizeModId(string value, string fallback)
        {
            var normalized = ModIdCleaner().Replace(value.Trim().ToLowerInvariant().Replace('-', '_'), "");
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static string NormalizePackageName(string value)
        {
            var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => PackagePartCleaner().Replace(part.ToLowerInvariant(), ""))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            return parts.Length == 0 ? "com.localgpt.livingcities" : string.Join(".", parts);
        }

        private static string NormalizeLoader(string loader)
        {
            if (loader.Contains("data", StringComparison.OrdinalIgnoreCase))
                return "Datapack";
            if (loader.Contains("paper", StringComparison.OrdinalIgnoreCase) ||
                loader.Contains("plugin", StringComparison.OrdinalIgnoreCase) ||
                loader.Contains("bukkit", StringComparison.OrdinalIgnoreCase) ||
                loader.Contains("spigot", StringComparison.OrdinalIgnoreCase))
                return "Paper";
            if (loader.Contains("neo", StringComparison.OrdinalIgnoreCase))
                return "NeoForge";
            if (loader.Contains("bedrock", StringComparison.OrdinalIgnoreCase))
                return "Bedrock";
            return "Fabric";
        }

        private static string ToPascalCase(string value)
        {
            var words = value.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join("", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }

        private static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string CreateFabricSettingsGradle(string projectName) =>
            $$"""
            pluginManagement {
                repositories {
                    maven { url = 'https://maven.fabricmc.net/' }
                    gradlePluginPortal()
                    mavenCentral()
                }
            }

            dependencyResolutionManagement {
                repositories {
                    maven { url = 'https://maven.fabricmc.net/' }
                    mavenCentral()
                }
            }

            rootProject.name = '{{projectName}}'
            """;

        private static string CreateNeoForgeSettingsGradle(string projectName) =>
            $$"""
            pluginManagement {
                repositories {
                    maven { url = 'https://maven.neoforged.net/releases' }
                    gradlePluginPortal()
                    mavenCentral()
                }
            }

            dependencyResolutionManagement {
                repositories {
                    maven { url = 'https://maven.neoforged.net/releases' }
                    mavenCentral()
                }
            }

            rootProject.name = '{{projectName}}'
            """;

        private static string CreateFabricBuildGradle(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            plugins {
                id 'fabric-loom' version '1.10-SNAPSHOT'
                id 'maven-publish'
            }

            version = mod_version
            group = maven_group

            base {
                archivesName = mod_id
            }

            repositories {
                mavenCentral()
                maven { url = 'https://maven.fabricmc.net/' }
            }

            dependencies {
                minecraft "com.mojang:minecraft:${minecraft_version}"
                mappings "net.fabricmc:yarn:${minecraft_version}+build.1:v2"
                modImplementation "net.fabricmc:fabric-loader:${loader_version}"
                modImplementation "net.fabricmc.fabric-api:fabric-api:${fabric_version}"
            }

            processResources {
                inputs.property 'version', project.version
                filesMatching('fabric.mod.json') {
                    expand 'version': project.version
                }
            }

            tasks.withType(JavaCompile).configureEach {
                it.options.release = 21
            }

            java {
                toolchain {
                    languageVersion = JavaLanguageVersion.of(21)
                }
                withSourcesJar()
            }

            jar {
                from('LICENSE') {
                    rename { "${it}_${base.archivesName.get()}" }
                }
            }
            """;

        private static string CreateNeoForgeBuildGradle(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            plugins {
                id 'java-library'
                id 'maven-publish'
                id 'net.neoforged.moddev' version '2.0.107'
            }

            version = mod_version
            group = maven_group

            base {
                archivesName = mod_id
            }

            java.toolchain.languageVersion = JavaLanguageVersion.of(21)

            neoForge {
                version = neo_version

                runs {
                    client {
                        client()
                        systemProperty 'neoforge.enabledGameTestNamespaces', mod_id
                    }
                    server {
                        server()
                        programArgument '--nogui'
                        systemProperty 'neoforge.enabledGameTestNamespaces', mod_id
                    }
                    data {
                        data()
                        programArguments.addAll '--mod', mod_id, '--all', '--output', file('src/generated/resources/').absolutePath, '--existing', file('src/main/resources/').absolutePath
                    }
                }

                mods {
                    "${mod_id}" {
                        sourceSet sourceSets.main
                    }
                }
            }

            sourceSets.main.resources {
                srcDir 'src/generated/resources'
            }
            """;

        private static string CreatePaperSettingsGradle(string projectName) =>
            $$"""
            pluginManagement {
                repositories {
                    gradlePluginPortal()
                    mavenCentral()
                }
            }

            dependencyResolutionManagement {
                repositories {
                    mavenCentral()
                    maven { url = 'https://repo.papermc.io/repository/maven-public/' }
                }
            }

            rootProject.name = '{{projectName}}'
            """;

        private static string CreatePaperBuildGradle(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            plugins {
                id 'java'
            }

            version = plugin_version
            group = maven_group

            base {
                archivesName = plugin_id
            }

            repositories {
                mavenCentral()
                maven { url = 'https://repo.papermc.io/repository/maven-public/' }
            }

            dependencies {
                compileOnly "io.papermc.paper:paper-api:${paper_api_version}"
            }

            tasks.withType(JavaCompile).configureEach {
                it.options.encoding = 'UTF-8'
                it.options.release = 21
            }

            java {
                toolchain {
                    languageVersion = JavaLanguageVersion.of(21)
                }
            }

            processResources {
                filteringCharset = 'UTF-8'
                filesMatching('plugin.yml') {
                    expand(
                        'plugin_version': project.version,
                        'plugin_id': plugin_id,
                        'plugin_name': plugin_name,
                        'plugin_main': plugin_main,
                        'plugin_authors': plugin_authors,
                        'plugin_description': plugin_description
                    )
                }
            }
            """;

        private static string CreatePaperGradleProperties(MinecraftModBuildRequest request, WorkspaceContext context)
        {
            var versions = ResolveDependencyVersions(request, "Paper");
            return $$"""
            org.gradle.jvmargs=-Xmx2G
            org.gradle.daemon=false
            org.gradle.parallel=false

            paper_api_version={{versions.PaperApiVersion}}
            plugin_id={{context.ModId}}
            plugin_name={{context.ProjectName}}
            plugin_version=0.1.0
            plugin_main={{context.PackageName}}.{{context.MainClassName}}
            plugin_authors=LocalGPT, Michi0403
            plugin_description={{NormalizeDescription(request.Description)}}
            maven_group={{context.PackageName}}
            """;
        }

        private static string CreateCommonGradleProperties(MinecraftModBuildRequest request, WorkspaceContext context)
        {
            var versions = ResolveDependencyVersions(request);
            var fabricApiVersion = versions.FabricApiVersion ?? MinecraftDependencyVersionCatalog
                .Resolve("Fabric", request.MinecraftVersion, request.JavaVersion, request.GradleVersion)
                .FabricApiVersion;
            var neoForgeVersion = versions.NeoForgeVersion ?? MinecraftDependencyVersionCatalog
                .Resolve("NeoForge", request.MinecraftVersion, request.JavaVersion, request.GradleVersion)
                .NeoForgeVersion;

            return $$"""
            org.gradle.jvmargs=-Xmx3G
            org.gradle.daemon=false
            org.gradle.parallel=false

            minecraft_version={{versions.RequestedMinecraftVersion}}
            minecraft_version_range=[{{versions.RequestedMinecraftVersion}},)
            loader_version={{versions.FabricLoaderVersion ?? "0.16.9"}}
            fabric_version={{fabricApiVersion}}
            neo_version={{neoForgeVersion}}
            neo_version_range=[{{neoForgeVersion}},)

            mod_id={{context.ModId}}
            mod_name={{context.ProjectName}}
            mod_license=MIT
            mod_version=0.1.0
            mod_group_id={{context.PackageName}}
            maven_group={{context.PackageName}}
            mod_authors=LocalGPT, Michi0403
            mod_description={{NormalizeDescription(request.Description)}}
            """;
        }

        private static MinecraftDependencyVersionInfo ResolveDependencyVersions(
            MinecraftModBuildRequest request,
            string? loaderOverride = null) =>
            MinecraftDependencyVersionCatalog.Resolve(
                loaderOverride ?? request.Loader,
                request.MinecraftVersion,
                request.JavaVersion,
                request.GradleVersion);

        private static string NormalizeDescription(string description)
        {
            var value = string.IsNullOrWhiteSpace(description)
                ? "LocalGPT generated Minecraft Java mod workspace."
                : description.ReplaceLineEndings(" ").Trim();
            return value.Length <= 220 ? value : value[..220];
        }

        private static string CreateFabricMainClass(WorkspaceContext context) =>
            $$"""
            package {{context.PackageName}};

            import com.mojang.brigadier.CommandDispatcher;
            import net.fabricmc.api.ModInitializer;
            import net.fabricmc.fabric.api.command.v2.CommandRegistrationCallback;
            import net.fabricmc.fabric.api.itemgroup.v1.ItemGroupEvents;
            import net.minecraft.item.Item;
            import net.minecraft.item.ItemGroups;
            import net.minecraft.registry.Registries;
            import net.minecraft.registry.Registry;
            import net.minecraft.server.command.CommandManager;
            import net.minecraft.server.command.ServerCommandSource;
            import net.minecraft.text.Text;
            import net.minecraft.util.Identifier;
            import org.slf4j.Logger;
            import org.slf4j.LoggerFactory;

            public class {{context.MainClassName}} implements ModInitializer {
                public static final String MOD_ID = "{{context.ModId}}";
                public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

                public static final Item CITY_CHARTER = Registry.register(
                    Registries.ITEM,
                    Identifier.of(MOD_ID, "city_charter"),
                    new Item(new Item.Settings())
                );

                @Override
                public void onInitialize() {
                    ItemGroupEvents.modifyEntriesEvent(ItemGroups.TOOLS).register(entries -> entries.add(CITY_CHARTER));
                    CommandRegistrationCallback.EVENT.register((dispatcher, registryAccess, environment) -> registerCommands(dispatcher));
                    LOGGER.info("LocalGPT generated Living Cities starter loaded.");
                }

                private static void registerCommands(CommandDispatcher<ServerCommandSource> dispatcher) {
                    dispatcher.register(CommandManager.literal("livingcities")
                        .then(CommandManager.literal("report")
                            .executes(context -> {
                                context.getSource().sendFeedback(() -> Text.literal(LivingCitiesReport.createDemoReport()), false);
                                return 1;
                            })));
                }
            }
            """;

        private static string CreateNeoForgeMainClass(WorkspaceContext context) =>
            $$"""
            package {{context.PackageName}};

            import com.mojang.brigadier.CommandDispatcher;
            import net.minecraft.commands.CommandSourceStack;
            import net.minecraft.commands.Commands;
            import net.minecraft.core.registries.Registries;
            import net.minecraft.network.chat.Component;
            import net.minecraft.world.item.CreativeModeTabs;
            import net.minecraft.world.item.Item;
            import net.neoforged.bus.api.IEventBus;
            import net.neoforged.fml.common.Mod;
            import net.neoforged.neoforge.common.NeoForge;
            import net.neoforged.neoforge.event.BuildCreativeModeTabContentsEvent;
            import net.neoforged.neoforge.event.RegisterCommandsEvent;
            import net.neoforged.neoforge.registries.DeferredHolder;
            import net.neoforged.neoforge.registries.DeferredRegister;
            import org.slf4j.Logger;
            import org.slf4j.LoggerFactory;

            @Mod({{context.MainClassName}}.MOD_ID)
            public class {{context.MainClassName}} {
                public static final String MOD_ID = "{{context.ModId}}";
                public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

                public static final DeferredRegister<Item> ITEMS = DeferredRegister.create(Registries.ITEM, MOD_ID);
                public static final DeferredHolder<Item, Item> CITY_CHARTER = ITEMS.register(
                    "city_charter",
                    () -> new Item(new Item.Properties())
                );

                public {{context.MainClassName}}(IEventBus modEventBus) {
                    ITEMS.register(modEventBus);
                    modEventBus.addListener(this::addCreativeTabItems);
                    NeoForge.EVENT_BUS.addListener((RegisterCommandsEvent event) -> registerCommands(event));
                    LOGGER.info("LocalGPT generated Living Cities starter loaded.");
                }

                private void addCreativeTabItems(BuildCreativeModeTabContentsEvent event) {
                    if (event.getTabKey() == CreativeModeTabs.TOOLS_AND_UTILITIES) {
                        event.accept(CITY_CHARTER.get());
                    }
                }

                public static void registerCommands(RegisterCommandsEvent event) {
                    registerCommands(event.getDispatcher());
                }

                private static void registerCommands(CommandDispatcher<CommandSourceStack> dispatcher) {
                    dispatcher.register(Commands.literal("livingcities")
                        .then(Commands.literal("report")
                            .executes(context -> {
                                context.getSource().sendSuccess(() -> Component.literal(LivingCitiesReport.createDemoReport()), false);
                                return 1;
                            })));
                }
            }
            """;

        private static string CreatePaperMainClass(WorkspaceContext context) =>
            $$"""
            package {{context.PackageName}};

            import org.bukkit.command.Command;
            import org.bukkit.command.CommandSender;
            import org.bukkit.plugin.java.JavaPlugin;
            import org.jetbrains.annotations.NotNull;

            public class {{context.MainClassName}} extends JavaPlugin {
                @Override
                public void onEnable() {
                    getLogger().info("LocalGPT generated Living Cities Paper plugin loaded.");
                }

                @Override
                public boolean onCommand(
                    @NotNull CommandSender sender,
                    @NotNull Command command,
                    @NotNull String label,
                    @NotNull String[] args
                ) {
                    if (!command.getName().equalsIgnoreCase("livingcities")) {
                        return false;
                    }

                    sender.sendMessage(LivingCitiesReport.createDemoReport());
                    return true;
                }
            }
            """;

        private static string CreateFabricEmptyMainClass(WorkspaceContext context) =>
            $$"""
            package {{context.PackageName}};

            import net.fabricmc.api.ModInitializer;
            import org.slf4j.Logger;
            import org.slf4j.LoggerFactory;

            public class {{context.MainClassName}} implements ModInitializer {
                public static final String MOD_ID = "{{context.ModId}}";
                public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

                @Override
                public void onInitialize() {
                    LOGGER.info("LocalGPT generated mod loaded: {}", MOD_ID);
                }
            }
            """;

        private static string CreateNeoForgeEmptyMainClass(WorkspaceContext context) =>
            $$"""
            package {{context.PackageName}};

            import net.neoforged.bus.api.IEventBus;
            import net.neoforged.fml.common.Mod;
            import org.slf4j.Logger;
            import org.slf4j.LoggerFactory;

            @Mod({{context.MainClassName}}.MOD_ID)
            public class {{context.MainClassName}} {
                public static final String MOD_ID = "{{context.ModId}}";
                public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

                public {{context.MainClassName}}(IEventBus modEventBus) {
                    LOGGER.info("LocalGPT generated mod loaded: {}", MOD_ID);
                }
            }
            """;

        private static string CreateLivingCitiesReportClass(string packageName) =>
            $$"""
            package {{packageName}};

            public final class LivingCitiesReport {
                private LivingCitiesReport() {
                }

                public static String createDemoReport() {
                    return "Living Cities 0.1 starter: population=2, food=0, security=0. Next milestone: city founding with banner plus torch.";
                }
            }
            """;

        private static string CreateFabricMetadata(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            {
              "schemaVersion": 1,
              "id": "{{context.ModId}}",
              "version": "${version}",
              "name": "{{context.ProjectName}}",
              "description": "{{EscapeJson(NormalizeDescription(request.Description))}}",
              "authors": [
                "LocalGPT",
                "Michi0403"
              ],
              "contact": {},
              "license": "MIT",
              "environment": "*",
              "entrypoints": {
                "main": [
                  "{{context.PackageName}}.{{context.MainClassName}}"
                ]
              },
              "depends": {
                "fabricloader": ">=0.16.9",
                "minecraft": "~{{request.MinecraftVersion}}",
                "java": ">=21",
                "fabric-api": "*"
              }
            }
            """;

        private static string CreateNeoForgeMetadata(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            modLoader="javafml"
            loaderVersion="${neo_version_range}"
            license="${mod_license}"

            [[mods]]
            modId="${mod_id}"
            version="${mod_version}"
            displayName="${mod_name}"
            authors="${mod_authors}"
            description='''${mod_description}'''

            [[dependencies.${mod_id}]]
            modId="neoforge"
            type="required"
            versionRange="${neo_version_range}"
            ordering="NONE"
            side="BOTH"

            [[dependencies.${mod_id}]]
            modId="minecraft"
            type="required"
            versionRange="${minecraft_version_range}"
            ordering="NONE"
            side="BOTH"
            """;

        private static string CreatePaperPluginYaml(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            name: ${plugin_name}
            version: ${plugin_version}
            main: ${plugin_main}
            api-version: '{{(request.MinecraftVersion.StartsWith("1.21", StringComparison.OrdinalIgnoreCase) ? "1.21" : request.MinecraftVersion)}}'
            authors: [LocalGPT, Michi0403]
            description: ${plugin_description}
            commands:
              livingcities:
                description: Shows the Living Cities starter report.
                usage: /livingcities
                permission: {{context.ModId}}.report
            permissions:
              {{context.ModId}}.report:
                description: Allows reading the Living Cities starter report.
                default: true
            """;

        private static string CreateEnglishLang(string modId) =>
            $$"""
            {
              "item.{{modId}}.city_charter": "City Charter",
              "commands.{{modId}}.report": "Living Cities Report"
            }
            """;

        private static string CreateCityCharterModel() =>
            """
            {
              "parent": "minecraft:item/generated",
              "textures": {
                "layer0": "minecraft:item/paper"
              }
            }
            """;

        private static string CreateBuildLocalScript(MinecraftModBuildRequest request)
        {
            var versions = ResolveDependencyVersions(request);
            var gradleVersion = versions.GradleVersion;
            return $$"""
            [CmdletBinding()]
            param(
                [string]$Task = "build"
            )

            $ErrorActionPreference = "Stop"

            $javaHome = $env:JAVA_HOME
            if ([string]::IsNullOrWhiteSpace($javaHome) -or -not (Test-Path (Join-Path $javaHome "bin\java.exe"))) {
                $javaCandidate = Join-Path $env:ProgramFiles "Microsoft\jdk-21.0.11.10-hotspot"
                if (Test-Path (Join-Path $javaCandidate "bin\java.exe")) {
                    $javaHome = $javaCandidate
                }
            }

            if ([string]::IsNullOrWhiteSpace($javaHome) -or -not (Test-Path (Join-Path $javaHome "bin\java.exe"))) {
                throw "JDK 21 was not found. Install Microsoft.OpenJDK.21 or run LocalGPTWebviewWrapper\build\Setup-MinecraftModToolchain.ps1 -Install."
            }

            $env:JAVA_HOME = $javaHome
            $env:Path = "$(Join-Path $javaHome "bin");$env:Path"

            $localGradle = Join-Path $env:LOCALAPPDATA "LocalGPT\Tools\gradle-{{gradleVersion}}\bin\gradle.bat"
            if (Test-Path $localGradle) {
                & $localGradle $Task
                exit $LASTEXITCODE
            }

            $globalGradle = Get-Command gradle -ErrorAction SilentlyContinue
            if ($null -ne $globalGradle) {
                & $globalGradle.Source $Task
                exit $LASTEXITCODE
            }

            throw "Gradle {{gradleVersion}} was not found. Run LocalGPTWebviewWrapper\build\Setup-MinecraftModToolchain.ps1 -InstallGradle."
            """;
        }

        private static string CreateDatapackMcmeta(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            {
              "pack": {
                "pack_format": {{GetPackFormatJsonValue(request.MinecraftVersion)}},
                "description": "{{EscapeJson(context.ProjectName)}} - LocalGPT generated Living Cities datapack"
              }
            }
            """;

        private static string GetPackFormatJsonValue(string minecraftVersion)
        {
            var packFormat = MinecraftDatapackVersionCatalog.Resolve(minecraftVersion).PackFormat;
            return packFormat.Contains('.', StringComparison.Ordinal)
                ? $"\"{packFormat}\""
                : packFormat;
        }

        private static string CreateFunctionTag(string modId, string functionName) =>
            $$"""
            {
              "values": [
                "{{modId}}:{{functionName}}"
              ]
            }
            """;

        private static async Task WriteDatapackFunctionAsync(
            WorkspaceContext context,
            string functionPath,
            string content,
            CancellationToken cancellationToken)
        {
            var normalizedPath = functionPath.Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(context.ProjectRoot, "data", context.ModId, "function", $"{normalizedPath}.mcfunction");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, content, Utf8NoBom, cancellationToken);
        }

        private static string CreateDatapackLoadFunction(WorkspaceContext context) =>
            $$$"""
            # Living Cities 0.1 - core load
            # Re-running /reload may print "already exists" warnings for objectives; that is harmless.
            scoreboard objectives add lc_year dummy "LC Year"
            scoreboard objectives add lc_population dummy "LC Population"
            scoreboard objectives add lc_food dummy "LC Food"
            scoreboard objectives add lc_security dummy "LC Security"
            scoreboard objectives add lc_prestige dummy "LC Prestige"
            scoreboard objectives add lc_birth_year dummy "LC Birth Year"
            scoreboard objectives add lc_scan_timer dummy "LC Scan Timer"
            scoreboard objectives add lc_menu trigger "Living Cities"
            scoreboard objectives add lc_buildings dummy "LC Buildings"
            scoreboard objectives add lc_tmp dummy "LC Temp"

            scoreboard players set #year lc_year 1
            scoreboard players set #population lc_population 0
            scoreboard players set #food lc_food 100
            scoreboard players set #security lc_security 100
            scoreboard players set #prestige lc_prestige 0
            scoreboard players set #tick lc_scan_timer 0
            scoreboard players set #houses lc_buildings 0
            scoreboard players set #workplaces lc_buildings 0
            scoreboard players set #registered_this_scan lc_population 0

            data merge storage {{{context.ModId}}}:city {founded:0b,year:1,population:0,food:100,security:100,prestige:0,houses:0,workplaces:0}
            data merge storage {{{context.ModId}}}:chronicle {events:[]}
            data merge storage {{{context.ModId}}}:personalities {notables:[]}

            function {{{context.ModId}}}:buildings/init
            function {{{context.ModId}}}:city/register_banner
            tellraw @a [{"text":"[Living Cities] ","color":"green"},{"text":"Datapack loaded. Use /function {{{context.ModId}}}:ui/townhall or the admin book."}]
            """;

        private static string CreateDatapackTickFunction(WorkspaceContext context) =>
            $$$"""
            # Living Cities tick stays small: menu handling every tick, simulation every 5 seconds.
            execute as @a[tag=!lc_received_book] run function {{{context.ModId}}}:ui/give_admin_book
            scoreboard players enable @a lc_menu

            execute as @a[scores={lc_menu=1}] at @s run function {{{context.ModId}}}:city/create
            execute as @a[scores={lc_menu=2}] at @s run function {{{context.ModId}}}:ui/status
            execute as @a[scores={lc_menu=3}] at @s run function {{{context.ModId}}}:city/register_banner
            execute as @a[scores={lc_menu=4}] at @s run function {{{context.ModId}}}:buildings/register_house
            execute as @a[scores={lc_menu=5}] at @s run function {{{context.ModId}}}:ui/chronicle
            execute as @a[scores={lc_menu=6}] at @s run function {{{context.ModId}}}:debug/reset_city
            scoreboard players set @a[scores={lc_menu=1..}] lc_menu 0

            scoreboard players add #tick lc_scan_timer 1
            execute if score #tick lc_scan_timer matches 100.. as @a[limit=1,sort=nearest] at @s run function {{{context.ModId}}}:core/schedule
            execute if score #tick lc_scan_timer matches 100.. run scoreboard players set #tick lc_scan_timer 0
            """;

        private static string CreateDatapackScheduleFunction(WorkspaceContext context) =>
            $$$"""
            # Scheduled aggregate simulation. Keep this local to the selected city area.
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run return 0
            function {{{context.ModId}}}:citizens/register
            function {{{context.ModId}}}:city/update_population
            function {{{context.ModId}}}:food/update
            function {{{context.ModId}}}:security/update
            function {{{context.ModId}}}:quests/update
            function {{{context.ModId}}}:chronicle/update
            """;

        private static string CreateDatapackCityCreateFunction(WorkspaceContext context) =>
            $$$"""
            execute if data storage {{{context.ModId}}}:city {founded:1b} run function {{{context.ModId}}}:city/already_exists
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run function {{{context.ModId}}}:city/check_villagers
            """;

        private static string CreateDatapackCityCheckVillagersFunction(WorkspaceContext context) =>
            $$$"""
            scoreboard players set #nearby_villagers lc_tmp 0
            execute store result score #nearby_villagers lc_tmp if entity @e[type=minecraft:villager,distance=..96]
            execute if score #nearby_villagers lc_tmp matches 2.. run function {{{context.ModId}}}:city/create_new
            execute unless score #nearby_villagers lc_tmp matches 2.. run tellraw @s [{"text":"[Living Cities] ","color":"red"},{"text":"At least 2 villagers must be within 96 blocks before founding a city."}]
            """;

        private static string CreateDatapackCityCreateNewFunction(WorkspaceContext context) =>
            $$$"""
            data merge storage {{{context.ModId}}}:city {founded:1b,year:1,population:0,food:100,security:100,prestige:0,houses:0,workplaces:0,founder:{x:0,y:0,z:0},banner:{x:0,y:0,z:0}}
            execute store result storage {{{context.ModId}}}:city year int 1 run scoreboard players get #year lc_year
            execute store result storage {{{context.ModId}}}:city founder.x int 1 run data get entity @s Pos[0] 1
            execute store result storage {{{context.ModId}}}:city founder.y int 1 run data get entity @s Pos[1] 1
            execute store result storage {{{context.ModId}}}:city founder.z int 1 run data get entity @s Pos[2] 1
            scoreboard players set #food lc_food 100
            scoreboard players set #security lc_security 100
            scoreboard players set #prestige lc_prestige 0
            function {{{context.ModId}}}:citizens/register
            function {{{context.ModId}}}:city/update_population
            function {{{context.ModId}}}:chronicle/add_event
            tellraw @a [{"text":"[Living Cities] ","color":"gold"},{"text":"A city was founded. Register the banner from the town hall menu next."}]
            """;

        private static string CreateDatapackCityAlreadyExistsFunction() =>
            """
            tellraw @s [{"text":"[Living Cities] ","color":"yellow"},{"text":"A city already exists in this starter datapack. Use reset only in a test world."}]
            """;

        private static string CreateDatapackRegisterBannerFunction(WorkspaceContext context) =>
            $$$"""
            say LC register_banner loaded
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.x int 1 run data get entity @s Pos[0] 1
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.y int 1 run data get entity @s Pos[1] 1
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.z int 1 run data get entity @s Pos[2] 1
            execute if entity @s[type=minecraft:player] run tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"Town banner position registered at your current location."}]
            """;

        private static string CreateDatapackUpdatePopulationFunction(WorkspaceContext context) =>
            $$$"""
            scoreboard players set #population lc_population 0
            execute store result score #population lc_population if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96]
            execute store result storage {{{context.ModId}}}:city population int 1 run scoreboard players get #population lc_population
            """;

        private static string CreateDatapackCitizenRegisterFunction(WorkspaceContext context) =>
            $$$"""
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run return 0
            scoreboard players set #registered_this_scan lc_population 0
            execute as @e[type=minecraft:villager,distance=..96,tag=!lc_citizen] at @s run function {{{context.ModId}}}:citizens/detect_new
            function {{{context.ModId}}}:citizens/aging
            function {{{context.ModId}}}:citizens/personalities
            """;

        private static string CreateDatapackCitizenDetectNewFunction() =>
            """
            tag @s add lc_citizen
            scoreboard players operation @s lc_birth_year = #year lc_year
            scoreboard players add #registered_this_scan lc_population 1
            """;

        private static string CreateDatapackCitizenAgingFunction() =>
            """
            execute as @e[type=minecraft:villager,tag=lc_citizen] run scoreboard players operation @s lc_tmp = #year lc_year
            execute as @e[type=minecraft:villager,tag=lc_citizen] run scoreboard players operation @s lc_tmp -= @s lc_birth_year
            """;

        private static string CreateDatapackCitizenPersonalitiesFunction(WorkspaceContext context) =>
            $$$"""
            execute if score #population lc_population matches 5.. as @e[type=minecraft:villager,tag=lc_citizen,tag=!lc_personality,limit=1,sort=random] run tag @s add lc_personality
            execute store result storage {{{context.ModId}}}:personalities.count int 1 if entity @e[type=minecraft:villager,tag=lc_personality]
            """;

        private static string CreateDatapackCitizenStatusFunction(WorkspaceContext context) =>
            $$$"""
            tellraw @s [{"text":"Registered citizens: ","color":"gold"},{"score":{"name":"#population","objective":"lc_population"}}]
            tellraw @s [{"text":"Personalities: ","color":"light_purple"},{"storage":"{{{context.ModId}}}:personalities","nbt":"count"}]
            """;

        private static string CreateDatapackFoodUpdateFunction(WorkspaceContext context) =>
            $$$"""
            function {{{context.ModId}}}:food/production
            function {{{context.ModId}}}:food/consumption
            scoreboard players operation #food lc_food += #food_production lc_tmp
            scoreboard players operation #food lc_food -= #food_consumption lc_tmp
            execute if score #food lc_food matches ..0 run tellraw @a [{"text":"[Living Cities] ","color":"red"},{"text":"Food stores are empty. Growth and migration should pause in the next milestone."}]
            execute store result storage {{{context.ModId}}}:city food int 1 run scoreboard players get #food lc_food
            """;

        private static string CreateDatapackFoodProductionFunction() =>
            """
            scoreboard players set #food_production lc_tmp 0
            scoreboard players set #food_counter lc_tmp 0
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:farmer"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:fisherman"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:butcher"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            execute store result score #food_counter lc_tmp if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96,nbt={VillagerData:{profession:"minecraft:shepherd"}}]
            scoreboard players operation #food_production lc_tmp += #food_counter lc_tmp
            """;

        private static string CreateDatapackFoodConsumptionFunction() =>
            """
            scoreboard players operation #food_consumption lc_tmp = #population lc_population
            """;

        private static string CreateDatapackSecurityUpdateFunction(WorkspaceContext context) =>
            $$$"""
            function {{{context.ModId}}}:security/golems
            function {{{context.ModId}}}:security/nightwatch
            execute store result storage {{{context.ModId}}}:city security int 1 run scoreboard players get #security lc_security
            """;

        private static string CreateDatapackSecurityGolemsFunction() =>
            """
            scoreboard players set #golems lc_tmp 0
            scoreboard players set #security_factor lc_tmp 20
            execute store result score #golems lc_tmp if entity @e[type=minecraft:iron_golem,distance=..96]
            scoreboard players operation #security lc_security = #golems lc_tmp
            scoreboard players operation #security lc_security *= #security_factor lc_tmp
            """;

        private static string CreateDatapackSecurityNightwatchFunction() =>
            """
            execute if score #security lc_security matches ..19 run tellraw @a [{"text":"[Living Cities] ","color":"red"},{"text":"Security is low. Build defenses or protect villagers at night."}]
            """;

        private static string CreateDatapackChronicleAddEventFunction(WorkspaceContext context) =>
            $$$"""
            data modify storage {{{context.ModId}}}:chronicle events append value {type:"city_founded",text:"A city was founded.",year:1}
            """;

        private static string CreateDatapackChronicleUpdateFunction(WorkspaceContext context) =>
            $$$"""
            execute if score #registered_this_scan lc_population matches 1.. run data modify storage {{{context.ModId}}}:chronicle events append value {type:"citizens_registered",text:"New citizens were registered.",year:1}
            """;

        private static string CreateDatapackAdminBookFunction(WorkspaceContext context)
        {
            var bookContent = "{title:\"Living Cities\",author:\"LocalGPT\",pages:[["
                + "{text:\"=== Living Cities ===\\n\\n\",bold:true,color:\"gold\"},"
                + "{text:\"[Found city]\\n\",color:\"green\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 1\"}},"
                + "{text:\"\\n[Status]\\n\",color:\"aqua\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 2\"}},"
                + "{text:\"\\n[Register banner]\\n\",color:\"yellow\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 3\"}},"
                + "{text:\"\\n[Register house]\\n\",color:\"light_purple\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 4\"}},"
                + "{text:\"\\n[Chronicle]\\n\",color:\"gold\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 5\"}},"
                + "{text:\"\\n[Reset test city]\",color:\"red\",click_event:{action:\"run_command\",command:\"/trigger lc_menu set 6\"}}"
                + "]]}";

            return $$"""
                tag @s add lc_received_book
                scoreboard players enable @s lc_menu
                give @s written_book[written_book_content={{bookContent}}] 1
                tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"Admin book created. You can also run /function {{context.ModId}}:ui/townhall."}]
                """;
        }

        private static string CreateDatapackTownHallFunction(WorkspaceContext context) =>
            $$$"""
            tellraw @s [{"text":"=== Living Cities Town Hall ===","color":"gold","bold":true}]
            tellraw @s [{"text":"Found city","color":"green","click_event":{"action":"run_command","command":"/trigger lc_menu set 1"}},{"text":" | "},{"text":"Status","color":"aqua","click_event":{"action":"run_command","command":"/trigger lc_menu set 2"}},{"text":" | "},{"text":"Chronicle","color":"yellow","click_event":{"action":"run_command","command":"/trigger lc_menu set 5"}}]
            tellraw @s [{"text":"Direct report: /function {{{context.ModId}}}:ui/status","color":"gray"}]
            """;

        private static string CreateDatapackReportFunction(WorkspaceContext context) =>
            $$$"""
            tellraw @s [{"text":"=== Living Cities Status ===","color":"gold","bold":true}]
            execute if data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"City founded: ","color":"gray"},{"text":"yes","color":"green"}]
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"City founded: ","color":"gray"},{"text":"no","color":"red"}]
            tellraw @s [{"text":"Population: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"population"}]
            tellraw @s [{"text":"Food: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"food"}]
            tellraw @s [{"text":"Security: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"security"}]
            tellraw @s [{"text":"Houses: ","color":"gray"},{"storage":"{{{context.ModId}}}:city","nbt":"houses"}]
            tellraw @s [{"text":"Next: use the admin book or /function {{{context.ModId}}}:ui/townhall","color":"green"}]
            function {{{context.ModId}}}:citizens/status
            """;

        private static string CreateDatapackChronicleUiFunction(WorkspaceContext context) =>
            $$$"""
            tellraw @s [{"text":"=== Living Cities Chronicle ===","color":"gold","bold":true}]
            tellraw @s [{"storage":"{{{context.ModId}}}:chronicle","nbt":"events"}]
            """;

        private static string CreateDatapackQuestUpdateFunction(WorkspaceContext context) =>
            $$$"""
            function {{{context.ModId}}}:quests/generate
            """;

        private static string CreateDatapackQuestGenerateFunction(WorkspaceContext context) =>
            $$$"""
            execute if score #houses lc_buildings matches ..0 run data merge storage {{{context.ModId}}}:city {quest:"Register at least one house."}
            execute if score #food lc_food matches ..20 run data merge storage {{{context.ModId}}}:city {quest:"Increase food production."}
            execute if score #security lc_security matches ..20 run data merge storage {{{context.ModId}}}:city {quest:"Improve security."}
            """;

        private static string CreateDatapackBuildingsInitFunction() =>
            """
            scoreboard players set #houses lc_buildings 0
            scoreboard players set #workplaces lc_buildings 0
            """;

        private static string CreateDatapackRegisterHouseFunction(WorkspaceContext context) =>
            $$$"""
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"[Living Cities] ","color":"red"},{"text":"Found a city before registering houses."}]
            execute if data storage {{{context.ModId}}}:city {founded:1b} run scoreboard players add #houses lc_buildings 1
            execute if data storage {{{context.ModId}}}:city {founded:1b} store result storage {{{context.ModId}}}:city houses int 1 run scoreboard players get #houses lc_buildings
            execute if data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"House registered for the current city."}]
            """;

        private static string CreateDatapackBuildingDebugListFunction(WorkspaceContext context) =>
            $$$"""
            tellraw @s [{"text":"Registered houses: ","color":"gold"},{"storage":"{{{context.ModId}}}:city","nbt":"houses"}]
            tellraw @s [{"text":"Workplaces: ","color":"gold"},{"storage":"{{{context.ModId}}}:city","nbt":"workplaces"}]
            """;

        private static string CreateDatapackResetCityFunction(WorkspaceContext context) =>
            $$$"""
            data modify storage {{{context.ModId}}}:city set value {}
            data modify storage {{{context.ModId}}}:chronicle set value {events:[]}
            data modify storage {{{context.ModId}}}:personalities set value {notables:[]}
            tag @e[type=minecraft:villager,tag=lc_citizen] remove lc_citizen
            tag @e[type=minecraft:villager,tag=lc_personality] remove lc_personality
            scoreboard players set #population lc_population 0
            scoreboard players set #food lc_food 100
            scoreboard players set #security lc_security 100
            scoreboard players set #houses lc_buildings 0
            function {{{context.ModId}}}:core/load
            tellraw @s [{"text":"[Living Cities] ","color":"yellow"},{"text":"Test city state reset."}]
            """;

        private static string CreateDatapackBuildScript(WorkspaceContext context) =>
            $$"""
            [CmdletBinding()]
            param(
                [string]$Configuration = "Release"
            )

            $ErrorActionPreference = "Stop"
            $root = Split-Path -Parent $MyInvocation.MyCommand.Path
            $buildDir = Join-Path $root "build"
            $zipPath = Join-Path $buildDir "{{context.ProjectName}}-datapack.zip"

            New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
            if (Test-Path $zipPath) {
                Remove-Item $zipPath -Force
            }

            function Get-LocalRelativePath {
                param(
                    [Parameter(Mandatory = $true)][string]$BasePath,
                    [Parameter(Mandatory = $true)][string]$Path
                )

                $baseFull = (Resolve-Path $BasePath).Path.TrimEnd("\", "/") + "\"
                $pathFull = (Resolve-Path $Path).Path
                $baseUri = [Uri]$baseFull
                $pathUri = [Uri]$pathFull
                return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace("/", "\")
            }

            $required = @(
                "pack.mcmeta",
                "data\minecraft\tags\function\load.json",
                "data\minecraft\tags\function\tick.json",
                "data\{{context.ModId}}\function\core\load.mcfunction",
                "data\{{context.ModId}}\function\core\tick.mcfunction",
                "data\{{context.ModId}}\function\city\create.mcfunction",
                "data\{{context.ModId}}\function\citizens\register.mcfunction",
                "data\{{context.ModId}}\function\food\update.mcfunction",
                "data\{{context.ModId}}\function\security\update.mcfunction",
                "data\{{context.ModId}}\function\ui\townhall.mcfunction",
                "data\{{context.ModId}}\function\ui\status.mcfunction"
            )

            foreach ($relative in $required) {
                $path = Join-Path $root $relative
                if (-not (Test-Path $path)) {
                    throw "Missing datapack file: $relative"
                }
            }

            $wrapperMcmeta = Get-ChildItem $root -Directory | ForEach-Object { Join-Path $_.FullName "pack.mcmeta" } | Where-Object { Test-Path $_ }
            if ($wrapperMcmeta.Count -gt 0) {
                throw "Datapack wrapper folder detected. The zip root must contain pack.mcmeta directly, not a nested project folder."
            }

            $legacyFunctions = Get-ChildItem (Join-Path $root "data") -Recurse -Directory -Filter "functions"
            if ($legacyFunctions.Count -gt 0) {
                throw "Found legacy plural 'functions' folder. Minecraft 1.21+ datapacks use singular 'function'."
            }

            Get-Content (Join-Path $root "pack.mcmeta") -Raw | ConvertFrom-Json | Out-Null
            Get-Content (Join-Path $root "data\minecraft\tags\function\load.json") -Raw | ConvertFrom-Json | Out-Null
            Get-Content (Join-Path $root "data\minecraft\tags\function\tick.json") -Raw | ConvertFrom-Json | Out-Null

            $txtPlaceholders = Get-ChildItem (Join-Path $root "data") -Recurse -File -Filter "*.mcfunction.txt"
            if ($txtPlaceholders.Count -gt 0) {
                throw "Found .mcfunction.txt placeholders. Rename or implement them as .mcfunction files before packaging."
            }

            $functionIds = @{}
            $functionFiles = Get-ChildItem (Join-Path $root "data") -Recurse -File -Filter "*.mcfunction"
            foreach ($file in $functionFiles) {
                $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                $parts = $relativePath -split "[\\/]"
                $functionIndex = [Array]::IndexOf($parts, "function")
                if ($parts.Length -lt 4 -or $parts[0] -ne "data" -or $functionIndex -lt 2) {
                    throw "Invalid function path: $relativePath"
                }

                $namespace = $parts[1]
                $pathParts = $parts[($functionIndex + 1)..($parts.Length - 1)]
                $functionPath = ($pathParts -join "/") -replace "\.mcfunction$", ""
                $functionIds["${namespace}:$functionPath"] = $relativePath
            }

            $tagFiles = Get-ChildItem (Join-Path $root "data\minecraft\tags\function") -File -Filter "*.json"
            foreach ($tag in $tagFiles) {
                $json = Get-Content $tag.FullName -Raw | ConvertFrom-Json
                foreach ($value in $json.values) {
                    if (-not $functionIds.ContainsKey([string]$value)) {
                        throw "Function tag $($tag.Name) references missing function: $value"
                    }
                }
            }

            $referencePattern = [regex]'(?<![#/])\bfunction\s+([a-z0-9_.-]+:[a-z0-9_./-]+)'
            foreach ($file in $functionFiles) {
                $content = Get-Content $file.FullName -Raw
                if ($content -match "(?m)^\s*/") {
                    $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                    throw "Function $relativePath contains a leading slash command. Remove leading / inside .mcfunction files."
                }

                if ($content -match "\bdata\s+remove\s+storage\b") {
                    $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                    throw "Function $relativePath uses 'data remove storage'. Use 'data modify storage <id> set value ...' for root storage reset."
                }

                if ($content -match "\bstore\s+result\s+storage\s+[a-z0-9_.-]+:[a-z0-9_/-]+\.[a-z0-9_.-]+\s+(byte|short|int|long|float|double)\b") {
                    $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                    throw "Function $relativePath appears to put an NBT path into the storage id. Use 'storage namespace:id path int 1', for example 'storage living_cities:city year int 1'."
                }

                foreach ($match in $referencePattern.Matches($content)) {
                    $id = $match.Groups[1].Value
                    if (-not $functionIds.ContainsKey($id)) {
                        $relativePath = Get-LocalRelativePath -BasePath $root -Path $file.FullName
                        throw "Function $relativePath references missing function: $id"
                    }
                }
            }

            Compress-Archive -Path (Join-Path $root "pack.mcmeta"), (Join-Path $root "data") -DestinationPath $zipPath
            Write-Host "Validated $($functionFiles.Count) mcfunction files."
            Write-Host "Created datapack: $zipPath"
            """;

        private static string CreateDatapackBenchmarkNotes(WorkspaceContext context) =>
            $$"""
            # Living Cities Reference Benchmark

            This generated datapack was shaped against the provided early `living_cities.zip` reference.

            Reference traits preserved:

            - namespace: `living_cities`
            - singular Minecraft 1.21 datapack folders: `data/<namespace>/function` and `data/minecraft/tags/function`
            - `core/load` and `core/tick` entry points
            - scoreboard objectives for year, population, food, security, prestige, birth year, menu triggers, scan timer, and buildings
            - storage areas for city, chronicle, and personalities
            - trigger/menu-driven administration book
            - no full-world scans in the tick path; scheduled city-local checks only

            Improvements over the early reference:

            - no `.mcfunction.txt` placeholder files
            - build helper validates root zip layout, function tags, singular `function` folders, leading slash mistakes, root storage reset syntax, and `function namespace:path` references
            - generated output includes food, security, chronicle, quests, and building functions as real `.mcfunction` files
            - town hall UI is available through both the admin book and `/function {{context.ModId}}:ui/townhall`
            - `city/register_banner` includes a visible `say LC register_banner loaded` smoke line so testers can separate discovery problems from command behavior

            Remaining needs before your friend tests in a real world:

            - confirm the exact Minecraft Java version and pack format
            - run `/reload`, `/datapack list`, and `/function {{context.ModId}}:ui/townhall`
            - decide whether banner registration should stay menu-based or become a stricter raycast/block-position workflow
            """;

        private static string CreateDatapackReadme(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            # {{context.ProjectName}} Datapack

            Generated by LocalGPT as a vanilla Minecraft Java datapack.

            ## Build

            ```powershell
            .\build-local.ps1
            ```

            The build helper validates JSON files and creates `build\{{context.ProjectName}}-datapack.zip`.

            ## Install

            Copy the zip into a world's `datapacks` folder, then run:

            ```mcfunction
            /reload
            /function {{context.ModId}}:ui/townhall
            ```

            If `/function {{context.ModId}}:city/register_banner` is not offered by autocomplete, debug discovery before command syntax:

            - unzip the datapack and ensure `pack.mcmeta` is at zip root
            - for Minecraft 1.21+ ensure folders are `data/<namespace>/function` and `data/minecraft/tags/function`
            - run `/reload`, `/datapack list`, then `/function {{context.ModId}}:city/register_banner`
            - ensure no file ends in `.mcfunction.txt`
            - run `.\build-local.ps1` to validate references before copying the zip

            ## Structure

            Minecraft 1.21 uses the singular `function` registry folder:

            - `data/minecraft/tags/function/load.json`
            - `data/minecraft/tags/function/tick.json`
            - `data/{{context.ModId}}/function/core/*.mcfunction`
            - `data/{{context.ModId}}/function/city/*.mcfunction`
            - `data/{{context.ModId}}/function/citizens/*.mcfunction`
            - `data/{{context.ModId}}/function/food/*.mcfunction`
            - `data/{{context.ModId}}/function/security/*.mcfunction`
            - `data/{{context.ModId}}/function/ui/*.mcfunction`

            ## Living Cities Starter

            This datapack implements the first Living Cities 0.1 vertical slice: scoreboards, storage, city founding, citizen registration, aggregate population, food, security, chronicle, basic quests, and a town hall/admin-book UI. Keep tick work tiny; scale the real system through scheduled, city-scoped functions and stored aggregate values.
            """;

        private static string CreateWorkspaceReadme(MinecraftModBuildRequest request, WorkspaceContext context, string loader) =>
            $$"""
            # {{context.ProjectName}}

            Generated by LocalGPT as a Minecraft Java {{loader}} mod workspace.

            ## Toolchain

            - Java: JDK {{request.JavaVersion}}
            - Gradle: {{(string.IsNullOrWhiteSpace(request.GradleVersion) ? DefaultGradleVersion : request.GradleVersion)}}
            - Minecraft: {{request.MinecraftVersion}}
            - Loader: {{loader}}
            - Recommended IDE: {{request.Ide}}

            ## Build

            ```powershell
            .\build-local.ps1
            ```

            The helper script uses `JAVA_HOME` when available, otherwise it tries the Microsoft OpenJDK 21 path installed by winget. It uses LocalGPT's local Gradle install under `%LOCALAPPDATA%\LocalGPT\Tools` when present.

            ## Eclipse

            Import this folder as an existing Gradle project:

            `File > Import > Gradle > Existing Gradle Project`

            After the first Gradle sync, use the Gradle tasks for `build` and, depending on loader support, `runClient`.

            ## Living Cities 0.1 starter

            This workspace intentionally starts small:

            - registers one `city_charter` item
            - adds `/livingcities report`
            - stores the full design plan in `docs/living-cities-0.1-plan.md`
            - keeps city simulation logic ready for later services/classes

            The next AI Council milestone should implement city founding with banner plus torch, scoreboard/storage state, and a minimal town hall report.
            """;

        private static string CreateLivingCitiesPlan(MinecraftModBuildRequest request) =>
            $$"""
            # Living Cities 0.1 - LocalGPT Starter Plan

            User request:

            {{request.Description}}

            ## Critical path

            1. Datapack/data structure
            2. Scoreboards or server-side saved data
            3. City founding
            4. Citizen registration
            5. Population management
            6. Minimal town hall UI/report

            ## Performance principles

            - Prefer city-level aggregate simulation over ticking every villager.
            - Avoid world-wide scans.
            - Use registered city areas, local checks, and event-driven updates.
            - Store birth year instead of recalculating and persisting age.
            - Keep town hall output useful as both player UI and debug surface.

            ## Starter implementation included

            - vanilla datapack structure using `data/<namespace>/function`
            - Minecraft `load` and `tick` function tags
            - `lc_*` scoreboard objectives
            - `living_cities:city`, `living_cities:chronicle`, and `living_cities:personalities` storage
            - trigger-based town hall/admin-book UI
            - city founding, citizen registration, population, food, security, chronicle, quest, and building functions
            - `build-local.ps1` validator that checks JSON, missing functions, placeholder files, and function references before zipping

            ## Missing LocalGPT feature report

            - Add a Minecraft Java runtime harness that copies the generated zip into a selected test world's `datapacks` folder and runs `/reload`.
            - Add optional command syntax validation against the exact installed Minecraft server version.
            - Add version-aware datapack `pack_format` lookup from the installed Minecraft version manifest.
            - Add Bedrock behavior/resource pack exporter as a separate target, not mixed into Java mod generation.
            """;

        [GeneratedRegex("[^a-zA-Z0-9_.-]")]
        private static partial Regex NameCleaner();

        [GeneratedRegex("[^a-z0-9_]")]
        private static partial Regex ModIdCleaner();

        [GeneratedRegex("[^a-z0-9_]")]
        private static partial Regex PackagePartCleaner();

        private sealed record WorkspaceContext(
            string ProjectName,
            string ModId,
            string PackageName,
            string MainClassName,
            string ProjectRoot,
            string JavaRoot,
            string ResourceRoot,
            string AssetsRoot,
            string BuildFilePath,
            string MainClassPath,
            string MetadataPath,
            string ReadmePath);

        private sealed class WorkspaceLayout(WorkspaceContext context)
        {
            public WorkspaceContext Context { get; } = context;

            public MinecraftModWorkspace ToResult(
                string buildCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\build-local.ps1",
                string eclipseImportHint = "File > Import > Gradle > Existing Gradle Project") => new()
                {
                    ProjectName = Context.ProjectName,
                    RootPath = Context.ProjectRoot,
                    MainClassPath = Context.MainClassPath,
                    MetadataPath = Context.MetadataPath,
                    BuildFilePath = Context.BuildFilePath,
                    ReadmePath = Context.ReadmePath,
                    BuildCommand = buildCommand,
                    EclipseImportHint = eclipseImportHint
                };
        }
    }
}
