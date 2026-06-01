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
            var functionRoot = Path.Combine(context.ProjectRoot, "data", context.ModId, "function");
            var minecraftTagsRoot = Path.Combine(context.ProjectRoot, "data", "minecraft", "tags", "function");

            Directory.CreateDirectory(functionRoot);
            Directory.CreateDirectory(minecraftTagsRoot);
            Directory.CreateDirectory(Path.Combine(context.ProjectRoot, "docs"));

            await File.WriteAllTextAsync(context.MetadataPath, CreateDatapackMcmeta(request, context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "load.json"), CreateFunctionTag(context.ModId, "load"), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(minecraftTagsRoot, "tick.json"), CreateFunctionTag(context.ModId, "tick"), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(functionRoot, "load.mcfunction"), CreateDatapackLoadFunction(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(functionRoot, "tick.mcfunction"), CreateDatapackTickFunction(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(functionRoot, "found_city.mcfunction"), CreateDatapackFoundCityFunction(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(functionRoot, "report.mcfunction"), CreateDatapackReportFunction(context), Utf8NoBom, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(context.ProjectRoot, "docs", "living-cities-0.1-plan.md"), CreateLivingCitiesPlan(request), Utf8NoBom, cancellationToken);
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

        private static string CreatePaperGradleProperties(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            org.gradle.jvmargs=-Xmx2G
            org.gradle.daemon=false
            org.gradle.parallel=false

            paper_api_version={{GetPaperApiVersion(request.MinecraftVersion)}}
            plugin_id={{context.ModId}}
            plugin_name={{context.ProjectName}}
            plugin_version=0.1.0
            plugin_main={{context.PackageName}}.{{context.MainClassName}}
            plugin_authors=LocalGPT, Michi0403
            plugin_description={{NormalizeDescription(request.Description)}}
            maven_group={{context.PackageName}}
            """;

        private static string GetPaperApiVersion(string minecraftVersion) =>
            string.IsNullOrWhiteSpace(minecraftVersion) ? "1.21.1-R0.1-SNAPSHOT" : $"{minecraftVersion}-R0.1-SNAPSHOT";

        private static string CreateCommonGradleProperties(MinecraftModBuildRequest request, WorkspaceContext context) =>
            $$"""
            org.gradle.jvmargs=-Xmx3G
            org.gradle.daemon=false
            org.gradle.parallel=false

            minecraft_version={{request.MinecraftVersion}}
            minecraft_version_range=[{{request.MinecraftVersion}},)
            loader_version=0.16.9
            fabric_version={{GetFabricApiVersion(request.MinecraftVersion)}}
            neo_version={{GetNeoForgeVersion(request.MinecraftVersion)}}
            neo_version_range=[{{GetNeoForgeVersion(request.MinecraftVersion)}},)

            mod_id={{context.ModId}}
            mod_name={{context.ProjectName}}
            mod_license=MIT
            mod_version=0.1.0
            mod_group_id={{context.PackageName}}
            maven_group={{context.PackageName}}
            mod_authors=LocalGPT, Michi0403
            mod_description={{NormalizeDescription(request.Description)}}
            """;

        private static string GetFabricApiVersion(string minecraftVersion) =>
            minecraftVersion == "1.21.1" ? "0.116.9+1.21.1" : $"0.116.9+{minecraftVersion}";

        private static string GetNeoForgeVersion(string minecraftVersion) =>
            minecraftVersion == "1.21.1" ? "21.1.231" : "21.1.231";

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
            var gradleVersion = string.IsNullOrWhiteSpace(request.GradleVersion) ? DefaultGradleVersion : request.GradleVersion.Trim();
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
                "pack_format": {{GetPackFormat(request.MinecraftVersion)}},
                "description": "{{EscapeJson(context.ProjectName)}} - LocalGPT generated Living Cities datapack"
              }
            }
            """;

        private static int GetPackFormat(string minecraftVersion) =>
            minecraftVersion.StartsWith("1.21", StringComparison.OrdinalIgnoreCase) ? 48 : 48;

        private static string CreateFunctionTag(string modId, string functionName) =>
            $$"""
            {
              "values": [
                "{{modId}}:{{functionName}}"
              ]
            }
            """;

        private static string CreateDatapackLoadFunction(WorkspaceContext context) =>
            $$$"""
            # LocalGPT Living Cities load function
            scoreboard objectives add lc_year dummy "LC Year"
            scoreboard objectives add lc_population dummy "LC Population"
            scoreboard objectives add lc_food dummy "LC Food"
            scoreboard objectives add lc_security dummy "LC Security"
            scoreboard objectives add lc_prestige dummy "LC Prestige"
            scoreboard players add #global lc_year 0
            tellraw @a [{"text":"[Living Cities] ","color":"green"},{"text":"Datapack loaded. Use /function {{{context.ModId}}}:report."}]
            """;

        private static string CreateDatapackTickFunction(WorkspaceContext context) =>
            $$"""
            # Keep this tiny. Future simulation should be scheduled and city-scoped, not world-scanned every tick.
            execute if score #global lc_year matches 0 run scoreboard players set #global lc_year 1
            """;

        private static string CreateDatapackFoundCityFunction(WorkspaceContext context) =>
            $$$"""
            # Minimal starter city founding hook.
            # Next milestone: validate banner + torch + at least two nearby villagers before registering a city.
            scoreboard players add #city_count lc_population 1
            scoreboard players set #last_city_population lc_population 2
            scoreboard players set #last_city_food lc_food 0
            scoreboard players set #last_city_security lc_security 0
            tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"Starter city registered. This is a generated placeholder for the real banner + torch flow."}]
            """;

        private static string CreateDatapackReportFunction(WorkspaceContext context) =>
            $$$"""
            tellraw @s [{"text":"Living Cities 0.1 starter report","color":"gold"}]
            tellraw @s [{"text":"Population: ","color":"gray"},{"score":{"name":"#last_city_population","objective":"lc_population"}}]
            tellraw @s [{"text":"Food: ","color":"gray"},{"score":{"name":"#last_city_food","objective":"lc_food"}}]
            tellraw @s [{"text":"Security: ","color":"gray"},{"score":{"name":"#last_city_security","objective":"lc_security"}}]
            tellraw @s [{"text":"Next: /function {{{context.ModId}}}:found_city","color":"green"}]
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

            $required = @(
                "pack.mcmeta",
                "data\minecraft\tags\function\load.json",
                "data\minecraft\tags\function\tick.json",
                "data\{{context.ModId}}\function\load.mcfunction",
                "data\{{context.ModId}}\function\tick.mcfunction",
                "data\{{context.ModId}}\function\report.mcfunction"
            )

            foreach ($relative in $required) {
                $path = Join-Path $root $relative
                if (-not (Test-Path $path)) {
                    throw "Missing datapack file: $relative"
                }
            }

            Get-Content (Join-Path $root "pack.mcmeta") -Raw | ConvertFrom-Json | Out-Null
            Get-Content (Join-Path $root "data\minecraft\tags\function\load.json") -Raw | ConvertFrom-Json | Out-Null
            Get-Content (Join-Path $root "data\minecraft\tags\function\tick.json") -Raw | ConvertFrom-Json | Out-Null

            Compress-Archive -Path (Join-Path $root "pack.mcmeta"), (Join-Path $root "data") -DestinationPath $zipPath
            Write-Host "Created datapack: $zipPath"
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
            /function {{context.ModId}}:report
            ```

            ## Structure

            Minecraft 1.21 uses the singular `function` registry folder:

            - `data/minecraft/tags/function/load.json`
            - `data/minecraft/tags/function/tick.json`
            - `data/{{context.ModId}}/function/*.mcfunction`

            ## Living Cities Starter

            This datapack starts the scoreboards and gives placeholder functions for report and city founding. Keep tick work tiny; scale the real system through scheduled, city-scoped functions and stored aggregate values.
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

            - `city_charter` item
            - `/livingcities report`
            - `LivingCitiesReport` placeholder class

            ## Missing LocalGPT feature report

            - Add generated integration tests or gametests for commands and registries.
            - Add a real Minecraft client/server launch harness from the builder UI.
            - Add version-aware dependency lookup for loader, mappings, Fabric API, NeoForge, and Minecraft.
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
