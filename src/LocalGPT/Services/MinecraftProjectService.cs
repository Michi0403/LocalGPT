using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>Owns Minecraft Java project text and metadata generation while reusing only generic text policy from <see cref="CouncilTextService"/>.</summary>
    /// <param name="jsonText">JSON text policy used when project metadata embeds human-entered values.</param>
    /// <param name="patterns">Persisted regex/text patterns used for Minecraft identifier normalization.</param>
    /// <param name="datapackService">Datapack version catalog used when resolving datapack-target dependency metadata.</param>
    /// <param name="catalog">Application catalog supplying user-maintained/default toolchain values.</param>
    /// <param name="serviceLogger">Logger for bounded project-generation diagnostics.</param>
    public sealed partial class MinecraftProjectService
    {
        /// <summary>
        /// Stores the JSON text service dependency used by <see cref="MinecraftProjectService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IJsonTextService jsonText;
        /// <summary>
        /// Stores the council text pattern data service dependency used by <see cref="MinecraftProjectService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly ICouncilTextPatternDataService patterns;
        /// <summary>
        /// Stores the minecraft datapack service dependency used by <see cref="MinecraftProjectService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly MinecraftDatapackService datapackService;
        /// <summary>
        /// Stores the local GPT catalog service dependency used by <see cref="MinecraftProjectService"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly LocalGptCatalogService catalog;
        /// <summary>
        /// Stores the logger used by <see cref="MinecraftProjectService"/> to record operational diagnostics without coupling callers to logging details.
        /// </summary>
        private readonly ILogger<MinecraftProjectService> serviceLogger;

        /// <summary>Creates the Minecraft project domain service with its scoped policy collaborators.</summary>
        /// <param name="jsonText">Json text service dependency used by the minecraft project workflow to provide the corresponding application capability.</param>
        /// <param name="patterns">Council text pattern data service dependency used by the minecraft project workflow to provide the corresponding application capability.</param>
        /// <param name="datapackService">Minecraft datapack service dependency used by the minecraft project workflow to provide the corresponding application capability.</param>
        /// <param name="catalog">Local gpt catalog service dependency used by the minecraft project workflow to provide the corresponding application capability.</param>
        /// <param name="serviceLogger">Minecraft project service dependency used by the minecraft project workflow to provide the corresponding application capability.</param>
        public MinecraftProjectService(
            IJsonTextService jsonText,
            ICouncilTextPatternDataService patterns,
            MinecraftDatapackService datapackService,
            LocalGptCatalogService catalog,
            ILogger<MinecraftProjectService> serviceLogger)
        {
            this.jsonText = jsonText;
            this.patterns = patterns;
            this.datapackService = datapackService;
            this.catalog = catalog;
            this.serviceLogger = serviceLogger;
        }

        /// <summary>
        /// Normalizes name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="fallback">Fallback value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeName(string value, string fallback)
        {
    try
    {
                var normalized = patterns.NameCleanerPattern.Replace(value.Trim(), string.Empty);
                return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizeName)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizeName)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes mod identifier as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="fallback">Fallback value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeModId(string value, string fallback)
        {
    try
    {
                var normalized = patterns.ModIdCleanerPattern.Replace(value.Trim().ToLowerInvariant().Replace('-', '_'), string.Empty);
                return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizeModId)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizeModId)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes package name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizePackageName(string value)
        {
    try
    {
                var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(part => patterns.PackagePartCleanerPattern.Replace(part.ToLowerInvariant(), string.Empty))
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .ToArray();

                return parts.Length == 0 ? "com.localgpt.livingcities" : string.Join(".", parts);
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizePackageName)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizePackageName)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes loader as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="loader">Loader value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeLoader(string? loader, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loader))
                    return "Fabric";
                if (loader.Contains("data", StringComparison.OrdinalIgnoreCase)||
                    loader.Contains("plugin", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("vanilla datapack", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("data pack", StringComparison.OrdinalIgnoreCase))
                    return "Datapack";
                if (loader.Contains("paper", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("plugin", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("paper plugin", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("bukkit", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("spigot", StringComparison.OrdinalIgnoreCase))
                    return "Paper";
                if (loader.Contains("neo", StringComparison.OrdinalIgnoreCase)||
                    loader.Contains("neoforge", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("neo forge", StringComparison.OrdinalIgnoreCase))
                    return "NeoForge";
                if (loader.Contains("bedrock", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("bedrock addon", StringComparison.OrdinalIgnoreCase) ||
                    loader.Contains("bedrock add-on", StringComparison.OrdinalIgnoreCase))
                    return "Bedrock";
                if (loader.Contains("fabric", StringComparison.OrdinalIgnoreCase))
                    return "Fabric";
                return "Fabric";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not normalize Minecraft loader {Loader}.", loader);
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs to pascal case as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToPascalCase(string value, ILogger logger)
        {
            try
            {
                var words = value.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return string.Join("", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToPascalCase value {value.ToString()} return normal value");
                return value;
            }
        
        }

        /// <summary>
        /// Creates fabric settings gradle as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateFabricSettingsGradle(string projectName) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricSettingsGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricSettingsGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates neo forge settings gradle as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateNeoForgeSettingsGradle(string projectName) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeSettingsGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeSettingsGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates fabric build gradle as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateFabricBuildGradle(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricBuildGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricBuildGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates neo forge build gradle as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateNeoForgeBuildGradle(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeBuildGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeBuildGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates paper settings gradle as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreatePaperSettingsGradle(string projectName) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperSettingsGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperSettingsGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates paper build gradle as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreatePaperBuildGradle(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperBuildGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperBuildGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Normalizes description as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="description">Description value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string NormalizeDescription(string description)
        {
    try
    {
                var value = string.IsNullOrWhiteSpace(description)
                    ? "LocalGPT generated Minecraft Java mod workspace."
                    : description.ReplaceLineEndings(" ").Trim();
                return value.Length <= 220 ? value : value[..220];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizeDescription)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(NormalizeDescription)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates fabric main class as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateFabricMainClass(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates neo forge main class as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateNeoForgeMainClass(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates paper main class as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreatePaperMainClass(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates fabric empty main class as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateFabricEmptyMainClass(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricEmptyMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricEmptyMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates neo forge empty main class as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateNeoForgeEmptyMainClass(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeEmptyMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeEmptyMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates living cities report class as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="packageName">Package name value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateLivingCitiesReportClass(string packageName) {
    try
    {
        return $$"""
            package {{packageName}};

            public final class LivingCitiesReport {
                private LivingCitiesReport() {
                }

                public static String createDemoReport() {
                    return "Living Cities 0.1 starter: population=2, food=0, security=0. Next milestone: city founding with banner plus torch.";
                }
            }
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateLivingCitiesReportClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateLivingCitiesReportClass)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates fabric metadata as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateFabricMetadata(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
            {
              "schemaVersion": 1,
              "id": "{{context.ModId}}",
              "version": "${version}",
              "name": "{{context.ProjectName}}",
              "description": "{{jsonText.EscapeStringValue(NormalizeDescription(request.Description))}}",
              "authors": [
                "Generated with LocalGPT"
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricMetadata)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateFabricMetadata)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates neo forge metadata as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreateNeoForgeMetadata(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeMetadata)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreateNeoForgeMetadata)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates paper plugin yaml as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="context">Context value supplied to the council text operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CreatePaperPluginYaml(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
            name: ${plugin_name}
            version: ${plugin_version}
            main: ${plugin_main}
            api-version: '{{(request.MinecraftVersion.StartsWith("1.21", StringComparison.OrdinalIgnoreCase) ? "1.21" : request.MinecraftVersion)}}'
            authors: [Generated with LocalGPT]
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperPluginYaml)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftProjectService)}.{nameof(CreatePaperPluginYaml)} failed.");
        throw;
    }
}

    }
}
