using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.Blazor.Viewer.Internal;
using DevExpress.DataAccess.DataFederation;
using DevExpress.Utils.About;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Serialization;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.AI;
using SQLitePCL;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Net;
using System.Reactive;
using System.Security.AccessControl;
using System.ServiceModel.Channels;
using System.Text;
namespace LocalGPT.Services
{
    
    /// <summary>
    /// Provides council text service operations.
    /// </summary>
    public sealed partial class CouncilTextService(ICouncilTextPatternDataService patterns, LocalGptCatalogService catalog, ILogger<CouncilTextService> serviceLogger)
    {
   

        /// <summary>
        /// Builds the safe visible attachment presentation shared by live and persisted chat messages.
        /// </summary>
        public string BuildAttachmentPresentation(string? content, IEnumerable<string>? fileNames)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(BuildAttachmentPresentation));
                var safeContent = content ?? string.Empty;
                if (fileNames is null)
                    return safeContent;

                var names = fileNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => WebUtility.HtmlEncode(Path.GetFileName(name.Trim())))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (names.Length == 0)
                    return safeContent;

                var chips = string.Join(
                    string.Empty,
                    names.Select(name => $"<span class=\"localgpt-restored-attachment\">📎 {name}</span>"));
                return $"{safeContent}\n<div class=\"localgpt-restored-attachments\" data-localgpt-restored-attachments=\"true\">{chips}</div>";
            }
            catch (Exception ex)
            {
                serviceLogger.LogWarning(
                    ex,
                    "Council text operation {Operation} failed; attachment names were omitted from the visible chat content.",
                    nameof(BuildAttachmentPresentation));
                return content ?? string.Empty;
            }
        }

        /// <summary>
        /// Runs the format live council session option operation.
        /// </summary>
        public string FormatLiveCouncilSessionOption(
            DateTime startedAtUtc,
            string runState,
            IReadOnlyList<string> councilMembers)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(FormatLiveCouncilSessionOption));
                var memberText = string.Join(", ", councilMembers.Take(3));
                return $"{startedAtUtc.ToLocalTime():g} · {runState} · {memberText}";
            }
            catch (Exception ex)
            {
                serviceLogger.LogError(ex, "Council text operation {Operation} failed.", nameof(FormatLiveCouncilSessionOption));
                return $"{startedAtUtc.ToLocalTime():g} · {runState}";
            }
        }

        /// <summary>
        /// Normalizes former thought.
        /// </summary>
        public string NormalizeFormerThought(string? value, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(NormalizeFormerThought));
                if (string.IsNullOrWhiteSpace(value))
                {
                    logger.LogDebug($"{nameof(NormalizeFormerThought)} received no former-thought content.");
                    return string.Empty;
                }

                var text = value.Trim();
                text = WebUtility.HtmlDecode(WebUtility.HtmlDecode(text));
                text = patterns.FormerThoughtBreakPattern.Replace(text, Environment.NewLine);
                text = patterns.FormerThoughtCodeWrapperPattern.Replace(text, string.Empty);
                text = patterns.FormerThoughtOpeningFencePattern.Replace(text, string.Empty);
                text = patterns.FormerThoughtClosingFencePattern.Replace(text, string.Empty);
                text = patterns.FormerThoughtPresentationWrapperPattern.Replace(text, match =>
                    match.Value.StartsWith("</", StringComparison.Ordinal) ? Environment.NewLine : string.Empty);
                var normalized = patterns.FormerThoughtExcessLineBreakPattern.Replace(
                    text,
                    Environment.NewLine + Environment.NewLine).Trim();
                logger.LogDebug($"{nameof(NormalizeFormerThought)} normalized former-thought presentation markup.");
                return normalized;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(NormalizeFormerThought)} failed; the original text will be shown without normalization.");
                return value?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        /// Builds role coordination explanation.
        /// </summary>
        public string BuildRoleCoordinationExplanation(IReadOnlyCollection<string> details, ILogger logger)
        {
            try
            {
                if (details.Count == 0)
                    return "No cross-role assignment or pairing rule is configured.";

                var explanation = $"Coordination: {string.Join("; ", details)}.";
                logger.LogDebug(
                    "{MethodName} prepared a role-coordination explanation with {DetailCount} detail(s).",
                    nameof(BuildRoleCoordinationExplanation),
                    details.Count);
                return explanation;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{MethodName} failed; role-coordination details were omitted.",
                    nameof(BuildRoleCoordinationExplanation));
                return "Role coordination is configured, but its explanation could not be displayed.";
            }
        }

        /// <summary>
        /// Builds feedback preview.
        /// </summary>
        public string BuildFeedbackPreview(string? content, ILogger logger)
        {
            try
            {
                var singleLine = patterns.WhitespacePattern.Replace(content ?? string.Empty, " ").Trim();
                var preview = singleLine.Length <= 180 ? singleLine : singleLine[..177] + "...";
                logger.LogDebug($"{nameof(BuildFeedbackPreview)} prepared a feedback preview without logging its content.");
                return preview;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(BuildFeedbackPreview)} failed; feedback content was omitted from logs.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds architecture poll message.
        /// </summary>
        public string BuildArchitecturePollMessage(
            string languageToolchain,
            string uiStack,
            string solutionShape,
            string renderMode,
            string referenceLook,
            bool allowSafeDefaults,
            string? extraDirection,
            ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                    .AppendLine("# LocalGPT Architecture Poll Decision")
                    .AppendLine()
                    .AppendLine("Treat explicit non-Ask values as my current decision for the next answer. Treat my normal chat request and extra direction as binding design input too; do not downgrade a user-stated design into an unresolved Ask value.")
                    .AppendLine($"- Language/toolchain: {languageToolchain}")
                    .AppendLine($"- UI stack: {uiStack}")
                    .AppendLine($"- Solution shape: {solutionShape}")
                    .AppendLine($"- Runtime/rendering: {renderMode}")
                    .AppendLine($"- Reference look: {referenceLook}")
                    .AppendLine($"- Prior consent for safe sandbox details: {(allowSafeDefaults ? "granted" : "not granted")}");

                if (!string.IsNullOrWhiteSpace(extraDirection))
                    builder.AppendLine($"- Extra direction: {extraDirection.Trim()}");

                builder
                    .AppendLine()
                    .AppendLine("If any selected value says \"Ask me\", first check whether my chat prompt or extra direction already answers it. If yes, treat the stated design as selected.")
                    .AppendLine("If an Ask value remains materially unresolved and prior consent is granted, choose a safe sandbox default, name that choice, and continue with a downloadable artifact.")
                    .AppendLine("If an Ask value remains materially unresolved and prior consent is not granted, stop before generating code or files. Return a concise runtime poll with concrete options and wait for my answer.")
                    .AppendLine("Do not assume C#/.NET, Minecraft, Blazor, DevExpress, Java, C++, PowerShell, or any other ecosystem unless I chose it, the target repository already requires it, or the request clearly specifies it.")
                    .AppendLine("When the requested language or ecosystem has no CodeDOM specialization, use the reviewed generic source/workspace file-generation path and preserve the target repository's build/project conventions instead of forcing a C# solution shape.")
                    .AppendLine("When recreating a goal application, compare its layout, navigation, data flows, API routes, settings, build/toolchain conventions, and user workflows, then recreate the recognizable structure with the selected architecture.");

                logger.LogDebug($"{nameof(BuildArchitecturePollMessage)} created a service-owned architecture decision message without logging user content.");
                return builder.ToString().Trim();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(BuildArchitecturePollMessage)} failed; architecture choices were omitted from logs.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parses model names.
        /// </summary>
        public IReadOnlyList<string> ParseModelNames(string? value, ILogger logger)
        {
            try
            {
                var names = (value ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(modelName => !string.IsNullOrWhiteSpace(modelName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                logger.LogDebug($"{nameof(ParseModelNames)} parsed {names.Count} distinct model names without logging their values.");
                return names;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"{nameof(ParseModelNames)} failed; model names were omitted from logs.");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Normalizes name.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizeName)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizeName)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes mod identifier.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizeModId)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizeModId)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes package name.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizePackageName)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizePackageName)} failed.");
        throw;
    }
}

        /// <summary>
        /// Normalizes loader.
        /// </summary>
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
        /// Runs the to pascal case operation.
        /// </summary>
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
        /// Runs the escape JSON operation.
        /// </summary>
        public string EscapeJson(string value) {
    try
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(EscapeJson)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(EscapeJson)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates fabric settings gradle.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricSettingsGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricSettingsGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates neo forge settings gradle.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeSettingsGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeSettingsGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates fabric build gradle.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricBuildGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricBuildGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates neo forge build gradle.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeBuildGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeBuildGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates paper settings gradle.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperSettingsGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperSettingsGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates paper build gradle.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperBuildGradle)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperBuildGradle)} failed.");
        throw;
    }
}
        /// <summary>
        /// Normalizes description.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizeDescription)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(NormalizeDescription)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates fabric main class.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates neo forge main class.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates paper main class.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates fabric empty main class.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricEmptyMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricEmptyMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates neo forge empty main class.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeEmptyMainClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeEmptyMainClass)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates living cities report class.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateLivingCitiesReportClass)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateLivingCitiesReportClass)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates fabric metadata.
        /// </summary>
        public string CreateFabricMetadata(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
            {
              "schemaVersion": 1,
              "id": "{{context.ModId}}",
              "version": "${version}",
              "name": "{{context.ProjectName}}",
              "description": "{{EscapeJson(NormalizeDescription(request.Description))}}",
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricMetadata)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFabricMetadata)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates neo forge metadata.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeMetadata)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateNeoForgeMetadata)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates paper plugin yaml.
        /// </summary>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperPluginYaml)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreatePaperPluginYaml)} failed.");
        throw;
    }
}
        /// <summary>
        /// Creates english lang.
        /// </summary>
        public string CreateEnglishLang(string modId) {
    try
    {
        return $$"""
            {
              "item.{{modId}}.city_charter": "City Charter",
              "commands.{{modId}}.report": "Living Cities Report"
            }
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateEnglishLang)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateEnglishLang)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates city charter model.
        /// </summary>
        public string CreateCityCharterModel() {
    try
    {
        return """
            {
              "parent": "minecraft:item/generated",
              "textures": {
                "layer0": "minecraft:item/paper"
              }
            }
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateCityCharterModel)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateCityCharterModel)} failed.");
        throw;
    }
}
        /// <summary>
        /// Gets pack format JSON value.
        /// </summary>
        public string GetPackFormatJsonValue(string minecraftVersion, ILogger logger)
        {
            try
            {
                var packFormat = MinecraftDatapackVersionInfoResolve(minecraftVersion, logger).PackFormat;
                return packFormat.Contains('.', StringComparison.Ordinal)
                    ? $"\"{packFormat}\""
                    : packFormat;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetPackFormatJsonValue minecraftVersion {minecraftVersion.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates function tag.
        /// </summary>
        public string CreateFunctionTag(string modId, string functionName) {
    try
    {
        return $$"""
            {
              "values": [
                "{{modId}}:{{functionName}}"
              ]
            }
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFunctionTag)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateFunctionTag)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack load function.
        /// </summary>
        public string CreateDatapackLoadFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackLoadFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackLoadFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack tick function.
        /// </summary>
        public string CreateDatapackTickFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackTickFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackTickFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack schedule function.
        /// </summary>
        public string CreateDatapackScheduleFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            # Scheduled aggregate simulation. Keep this local to the selected city area.
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run return 0
            function {{{context.ModId}}}:citizens/register
            function {{{context.ModId}}}:city/update_population
            function {{{context.ModId}}}:food/update
            function {{{context.ModId}}}:security/update
            function {{{context.ModId}}}:quests/update
            function {{{context.ModId}}}:chronicle/update
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackScheduleFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackScheduleFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city create function.
        /// </summary>
        public string CreateDatapackCityCreateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if data storage {{{context.ModId}}}:city {founded:1b} run function {{{context.ModId}}}:city/already_exists
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run function {{{context.ModId}}}:city/check_villagers
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityCreateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityCreateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city check villagers function.
        /// </summary>
        public string CreateDatapackCityCheckVillagersFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            scoreboard players set #nearby_villagers lc_tmp 0
            execute store result score #nearby_villagers lc_tmp if entity @e[type=minecraft:villager,distance=..96]
            execute if score #nearby_villagers lc_tmp matches 2.. run function {{{context.ModId}}}:city/create_new
            execute unless score #nearby_villagers lc_tmp matches 2.. run tellraw @s [{"text":"[Living Cities] ","color":"red"},{"text":"At least 2 villagers must be within 96 blocks before founding a city."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityCheckVillagersFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityCheckVillagersFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city create new function.
        /// </summary>
        public string CreateDatapackCityCreateNewFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityCreateNewFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityCreateNewFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack city already exists function.
        /// </summary>
        public string CreateDatapackCityAlreadyExistsFunction() {
    try
    {
        return """
            tellraw @s [{"text":"[Living Cities] ","color":"yellow"},{"text":"A city already exists in this starter datapack. Use reset only in a test world."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityAlreadyExistsFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCityAlreadyExistsFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack register banner function.
        /// </summary>
        public string CreateDatapackRegisterBannerFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            say LC register_banner loaded
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.x int 1 run data get entity @s Pos[0] 1
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.y int 1 run data get entity @s Pos[1] 1
            execute if entity @s[type=minecraft:player] store result storage {{{context.ModId}}}:city banner.z int 1 run data get entity @s Pos[2] 1
            execute if entity @s[type=minecraft:player] run tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"Town banner position registered at your current location."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackRegisterBannerFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackRegisterBannerFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack update population function.
        /// </summary>
        public string CreateDatapackUpdatePopulationFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            scoreboard players set #population lc_population 0
            execute store result score #population lc_population if entity @e[type=minecraft:villager,tag=lc_citizen,distance=..96]
            execute store result storage {{{context.ModId}}}:city population int 1 run scoreboard players get #population lc_population
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackUpdatePopulationFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackUpdatePopulationFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen register function.
        /// </summary>
        public string CreateDatapackCitizenRegisterFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run return 0
            scoreboard players set #registered_this_scan lc_population 0
            execute as @e[type=minecraft:villager,distance=..96,tag=!lc_citizen] at @s run function {{{context.ModId}}}:citizens/detect_new
            function {{{context.ModId}}}:citizens/aging
            function {{{context.ModId}}}:citizens/personalities
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenRegisterFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenRegisterFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen detect new function.
        /// </summary>
        public string CreateDatapackCitizenDetectNewFunction() {
    try
    {
        return """
            tag @s add lc_citizen
            scoreboard players operation @s lc_birth_year = #year lc_year
            scoreboard players add #registered_this_scan lc_population 1
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenDetectNewFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenDetectNewFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen aging function.
        /// </summary>
        public string CreateDatapackCitizenAgingFunction() {
    try
    {
        return """
            execute as @e[type=minecraft:villager,tag=lc_citizen] run scoreboard players operation @s lc_tmp = #year lc_year
            execute as @e[type=minecraft:villager,tag=lc_citizen] run scoreboard players operation @s lc_tmp -= @s lc_birth_year
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenAgingFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenAgingFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen personalities function.
        /// </summary>
        public string CreateDatapackCitizenPersonalitiesFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if score #population lc_population matches 5.. as @e[type=minecraft:villager,tag=lc_citizen,tag=!lc_personality,limit=1,sort=random] run tag @s add lc_personality
            execute store result storage {{{context.ModId}}}:personalities count int 1 if entity @e[type=minecraft:villager,tag=lc_personality]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenPersonalitiesFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenPersonalitiesFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack citizen status function.
        /// </summary>
        public string CreateDatapackCitizenStatusFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"Registered citizens: ","color":"gold"},{"score":{"name":"#population","objective":"lc_population"}}]
            tellraw @s [{"text":"Personalities: ","color":"light_purple"},{"storage":"{{{context.ModId}}}:personalities","nbt":"count"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenStatusFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackCitizenStatusFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack food update function.
        /// </summary>
        public string CreateDatapackFoodUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            function {{{context.ModId}}}:food/production
            function {{{context.ModId}}}:food/consumption
            scoreboard players operation #food lc_food += #food_production lc_tmp
            scoreboard players operation #food lc_food -= #food_consumption lc_tmp
            execute if score #food lc_food matches ..0 run tellraw @a [{"text":"[Living Cities] ","color":"red"},{"text":"Food stores are empty. Growth and migration should pause in the next milestone."}]
            execute store result storage {{{context.ModId}}}:city food int 1 run scoreboard players get #food lc_food
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackFoodUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackFoodUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack food production function.
        /// </summary>
        public string CreateDatapackFoodProductionFunction() {
    try
    {
        return """
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackFoodProductionFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackFoodProductionFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack food consumption function.
        /// </summary>
        public string CreateDatapackFoodConsumptionFunction() {
    try
    {
        return """
            scoreboard players operation #food_consumption lc_tmp = #population lc_population
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackFoodConsumptionFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackFoodConsumptionFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack security update function.
        /// </summary>
        public string CreateDatapackSecurityUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            function {{{context.ModId}}}:security/golems
            function {{{context.ModId}}}:security/nightwatch
            execute store result storage {{{context.ModId}}}:city security int 1 run scoreboard players get #security lc_security
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackSecurityUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackSecurityUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack security golems function.
        /// </summary>
        public string CreateDatapackSecurityGolemsFunction() {
    try
    {
        return """
            scoreboard players set #golems lc_tmp 0
            scoreboard players set #security_factor lc_tmp 20
            execute store result score #golems lc_tmp if entity @e[type=minecraft:iron_golem,distance=..96]
            scoreboard players operation #security lc_security = #golems lc_tmp
            scoreboard players operation #security lc_security *= #security_factor lc_tmp
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackSecurityGolemsFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackSecurityGolemsFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack security nightwatch function.
        /// </summary>
        public string CreateDatapackSecurityNightwatchFunction() {
    try
    {
        return """
            execute if score #security lc_security matches ..19 run tellraw @a [{"text":"[Living Cities] ","color":"red"},{"text":"Security is low. Build defenses or protect villagers at night."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackSecurityNightwatchFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackSecurityNightwatchFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack chronicle add event function.
        /// </summary>
        public string CreateDatapackChronicleAddEventFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            data modify storage {{{context.ModId}}}:chronicle events append value {type:"city_founded",text:"A city was founded.",year:1}
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackChronicleAddEventFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackChronicleAddEventFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack chronicle update function.
        /// </summary>
        public string CreateDatapackChronicleUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if score #registered_this_scan lc_population matches 1.. run data modify storage {{{context.ModId}}}:chronicle events append value {type:"citizens_registered",text:"New citizens were registered.",year:1}
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackChronicleUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackChronicleUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack admin book function.
        /// </summary>
        public string CreateDatapackAdminBookFunction(WorkspaceContext context)
        {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackAdminBookFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackAdminBookFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack town hall function.
        /// </summary>
        public string CreateDatapackTownHallFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"=== Living Cities Town Hall ===","color":"gold","bold":true}]
            tellraw @s [{"text":"Found city","color":"green","click_event":{"action":"run_command","command":"/trigger lc_menu set 1"}},{"text":" | "},{"text":"Status","color":"aqua","click_event":{"action":"run_command","command":"/trigger lc_menu set 2"}},{"text":" | "},{"text":"Chronicle","color":"yellow","click_event":{"action":"run_command","command":"/trigger lc_menu set 5"}}]
            tellraw @s [{"text":"Direct report: /function {{{context.ModId}}}:ui/status","color":"gray"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackTownHallFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackTownHallFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack report function.
        /// </summary>
        public string CreateDatapackReportFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackReportFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackReportFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack chronicle UI function.
        /// </summary>
        public string CreateDatapackChronicleUiFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"=== Living Cities Chronicle ===","color":"gold","bold":true}]
            tellraw @s [{"storage":"{{{context.ModId}}}:chronicle","nbt":"events"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackChronicleUiFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackChronicleUiFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack quest update function.
        /// </summary>
        public string CreateDatapackQuestUpdateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            function {{{context.ModId}}}:quests/generate
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackQuestUpdateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackQuestUpdateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack quest generate function.
        /// </summary>
        public string CreateDatapackQuestGenerateFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute if score #houses lc_buildings matches ..0 run data merge storage {{{context.ModId}}}:city {quest:"Register at least one house."}
            execute if score #food lc_food matches ..20 run data merge storage {{{context.ModId}}}:city {quest:"Increase food production."}
            execute if score #security lc_security matches ..20 run data merge storage {{{context.ModId}}}:city {quest:"Improve security."}
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackQuestGenerateFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackQuestGenerateFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack buildings init function.
        /// </summary>
        public string CreateDatapackBuildingsInitFunction() {
    try
    {
        return """
            scoreboard players set #houses lc_buildings 0
            scoreboard players set #workplaces lc_buildings 0
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBuildingsInitFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBuildingsInitFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack register house function.
        /// </summary>
        public string CreateDatapackRegisterHouseFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            execute unless data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"[Living Cities] ","color":"red"},{"text":"Found a city before registering houses."}]
            execute if data storage {{{context.ModId}}}:city {founded:1b} run scoreboard players add #houses lc_buildings 1
            execute if data storage {{{context.ModId}}}:city {founded:1b} store result storage {{{context.ModId}}}:city houses int 1 run scoreboard players get #houses lc_buildings
            execute if data storage {{{context.ModId}}}:city {founded:1b} run tellraw @s [{"text":"[Living Cities] ","color":"green"},{"text":"House registered for the current city."}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackRegisterHouseFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackRegisterHouseFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack building debug list function.
        /// </summary>
        public string CreateDatapackBuildingDebugListFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
            tellraw @s [{"text":"Registered houses: ","color":"gold"},{"storage":"{{{context.ModId}}}:city","nbt":"houses"}]
            tellraw @s [{"text":"Workplaces: ","color":"gold"},{"storage":"{{{context.ModId}}}:city","nbt":"workplaces"}]
            """;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBuildingDebugListFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBuildingDebugListFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack reset city function.
        /// </summary>
        public string CreateDatapackResetCityFunction(WorkspaceContext context) {
    try
    {
        return $$$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackResetCityFunction)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackResetCityFunction)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack build script.
        /// </summary>
        public string CreateDatapackBuildScript(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBuildScript)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBuildScript)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack benchmark notes.
        /// </summary>
        public string CreateDatapackBenchmarkNotes(WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBenchmarkNotes)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackBenchmarkNotes)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates datapack readme.
        /// </summary>
        public string CreateDatapackReadme(MinecraftModBuildRequest request, WorkspaceContext context) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackReadme)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateDatapackReadme)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates workspace readme.
        /// </summary>
        public string CreateWorkspaceReadme(MinecraftModBuildRequest request, WorkspaceContext context, string loader) {
    try
    {
        return $$"""
            # {{context.ProjectName}}

            Generated by LocalGPT as a Minecraft Java {{loader}} mod workspace.

            ## Toolchain

            - Java: JDK {{request.JavaVersion}}
            - Gradle: {{(string.IsNullOrWhiteSpace(request.GradleVersion) ? catalog.DefaultGradleVersion : request.GradleVersion)}}
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateWorkspaceReadme)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateWorkspaceReadme)} failed.");
        throw;
    }
}

        /// <summary>
        /// Creates living cities plan.
        /// </summary>
        public string CreateLivingCitiesPlan(MinecraftModBuildRequest request) {
    try
    {
        return $$"""
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
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateLivingCitiesPlan)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(CreateLivingCitiesPlan)} failed.");
        throw;
    }
}

        /// <summary>
        /// Runs the looks like missing feature report operation.
        /// </summary>
        public bool LooksLikeMissingFeatureReport(string text, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                return patterns.MissingFeaturePattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikeMissingFeatureReport text {text.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Runs the sanitize file name operation.
        /// </summary>
        public string SanitizeFileName(string value, ILogger<BuildDebugInventoryService> logger)
        {
            try
            {
                var invalid = Path.GetInvalidFileNameChars();
                var builder = new StringBuilder(value.Length);
                foreach (var character in value)
                    builder.Append(invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character);

                return builder.ToString().Trim('-');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SanitizeFileName value {value.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Builds import directories.
        /// </summary>
        public IEnumerable<string> BuildImportDirectories(string rootPath, int maxProjects, ILogger logger)
        {
            try
            {
                var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var directory in EnumerateImportDirectoryCandidates(rootPath, logger))
                {
                    if (emitted.Count >= maxProjects)
                        yield break;

                    var directoryName = Path.GetFileName(directory);
                    if (catalog.ExcludedDirectoryNames.Contains(directoryName) || !emitted.Add(directory))
                        continue;

                    yield return directory;
                }
            }
            finally
            {
                logger.LogInformation($"Ended BuildImportDirectories rootPath {rootPath?.ToString()} maxProjects {maxProjects.ToString()}");
            }
        }
        /// <summary>
        /// Runs the enumerate import directory candidates operation.
        /// </summary>
        public IEnumerable<string> EnumerateImportDirectoryCandidates(string rootPath, ILogger logger)
        {
            try
            {
                if (LooksLikeArchitectureRoot(rootPath, logger))
                    yield return rootPath;

                foreach (var directory in SafeEnumerateDirectories(rootPath, logger))
                    yield return directory;

                foreach (var directory in EnumerateNestedArchitectureRoots(rootPath, logger))
                    yield return directory;
            }
            finally
            {
                logger.LogInformation($"Ended EnumerateImportDirectoryCandidates rootPath {rootPath?.ToString()}");
            }
        }
        /// <summary>
        /// Runs the extract target frameworks operation.
        /// </summary>
        public IEnumerable<string> ExtractTargetFrameworks(string text, ILogger logger)
        {
            try
            {
                return patterns.TargetFrameworkPattern.Matches(text)
                    .Select(match => match.Groups["value"].Value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(ExtractTargetFrameworks)} could not extract target frameworks; source text was omitted from logs.");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Runs the extract package references operation.
        /// </summary>
        public IEnumerable<string> ExtractPackageReferences(string text, ILogger logger)
        {
            try
            {
                return patterns.PackageReferencePattern.Matches(text)
                    .Select(match => match.Groups["value"].Value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(ExtractPackageReferences)} could not extract package references; source text was omitted from logs.");
                return Array.Empty<string>();
            }
        }
        /// <summary>
        /// Determines whether important file.
        /// </summary>
        public bool IsImportantFile(string fileName, string extension, ILogger logger)
        {
            try
            {
                return IsProjectRootFile(fileName, extension, logger) ||
    extension is ".razor" or ".xaml" or ".py" or ".json" or ".sql" or ".md" or ".mdx" or ".go" or ".gotmpl" ||
    fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
    fileName.StartsWith("Startup.", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("App.razor", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("_Imports.razor", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsImportantFile fileName {fileName?.ToString()} extension {extension?.ToString()}");
                return false;
            }

        }
        /// <summary>
        /// Determines whether project root file.
        /// </summary>
        public bool IsProjectRootFile(string fileName, string extension, ILogger logger)
        {
            try
            {
                return extension is ".sln" or ".csproj" or ".fsproj" or ".vbproj" ||
    fileName.Equals("go.mod", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("go.sum", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("CMakeLists.txt", StringComparison.OrdinalIgnoreCase) ||
    fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsProjectRootFile fileName {fileName?.ToString()} extension {extension?.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Runs the contains zip entry operation.
        /// </summary>
        public bool ContainsZipEntry(HashSet<string> zipEntries, string required, ILogger logger)
        {
            try
            {
                var normalized = required.Replace('\\', '/').Trim('/');
                return zipEntries.Any(entry =>
                    string.Equals(entry.Trim('/'), normalized, StringComparison.OrdinalIgnoreCase) ||
                    entry.Contains($"/{normalized}", StringComparison.OrdinalIgnoreCase) ||
                    entry.StartsWith($"{normalized}/", StringComparison.OrdinalIgnoreCase) ||
                    entry.Contains($"{normalized}/", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ContainsZipEntry zipEntries {zipEntries.ToString()} required {required.ToString()}");
                return false;
            }

        }
        /// <summary>
        /// Runs the redact sensitive name operation.
        /// </summary>
        public string RedactSensitiveName(string value, ILogger logger)
        {
            try
            {

                return patterns.SensitiveNamePattern.Replace(value, "[redacted-name]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RedactSensitiveName value {value?.ToString()}");
                return string.Empty;
            }
        }
  
        /// <summary>
        /// Builds file policy summary.
        /// </summary>
        public string BuildFilePolicySummary(ILogger logger)
        {
            try
            {
                var sourceExtensions = string.Join(", ", catalog.SourceExtensions.Order(StringComparer.OrdinalIgnoreCase));
                var binaryExtensions = string.Join(", ", catalog.BinaryExtensions.Order(StringComparer.OrdinalIgnoreCase));
                var excludedDirectories = string.Join(", ", catalog.ExcludedDirectoryNames.Order(StringComparer.OrdinalIgnoreCase));
                return "Reads source/documentation-like files: " + sourceExtensions +
                    ". Counts but does not store binary/package files: " + binaryExtensions +
                    ". Skips noisy build/cache directories: " + excludedDirectories + ".";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildFilePolicySummary");
                return string.Empty;
            }
        }
        /// <summary>
        /// Normalizes task set.
        /// </summary>
        public string NormalizeTaskSet(string? taskSet, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(taskSet))
                    return "engineering";

                return taskSet.Trim().ToLowerInvariant() switch
                {
                    "replacement" or "replacements" or "apps" or "app-replacements" => "replacement",
                    "all" or "full" => "all",
                    _ => "engineering"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeTaskSet taskSet {taskSet?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Builds engineering tasks.
        /// </summary>
        public IReadOnlyList<BenchmarkTaskDefinition> BuildEngineeringTasks()
        {
    try
    {
                return
                [
                    new(
                        "devexpress-webshop-efcore",
                        "DevExpress Blazor webshop with EF Core",
                        "Generate a downloadable whole solution zip for a DevExpress Blazor webshop with EF Core, SQLite seed data, products, carts, orders, admin CRUD grid, detail form, Bootstrap v5 layout, and README.",
                        "A strong answer contains a .NET solution, EF Core DbContext/entities/migration guidance, DevExpress product/admin grids, cart/order services, seed data, app navigation, build/run steps, and no client-side privileged commands.",
                        "Benchmark answer: create a full solution zip with DevExpress Blazor pages, EF Core entities, services, product/cart/order workflows, and README. Include Implementation artifact request.",
                        6,
                        ["PROJECT_INDEX.md", ".localgpt-generation.json", "src/"],
                        ["DevExpress", "Blazor", "service", "model"],
                        ["Components/Pages", "Services", "Models"]),
                    new(
                        "blazor-admin-crud-dashboard",
                        "Blazor admin dashboard with CRUD grid and detail form",
                        "Generate a downloadable whole solution zip for a Blazor admin dashboard with DevExpress DxGrid CRUD, detail form, validation, SQLite persistence, audit log, and Bootstrap v5 navigation.",
                        "A strong answer contains DxGrid, EditForm/DxFormLayout detail editing, validation, EF Core persistence, audit logging, clear service boundaries, and buildable project files.",
                        "Benchmark answer: create a full solution zip with DxGrid CRUD, detail form, validation, SQLite persistence, audit notes, and README. Include Implementation artifact request.",
                        6,
                        ["PROJECT_INDEX.md", ".localgpt-generation.json", "src/"],
                        ["DevExpress", "Blazor", "grid"],
                        ["Components/Pages", "Services", "Models"]),
                    new(
                        "msix-winui-blazor-packaging",
                        "MSIX/WinUI/Blazor packaging error diagnosis",
                        "Diagnose and produce a downloadable LocalGPT-style implementation note for an MSIX WinUI WebView2 Blazor packaging error involving static web assets, LocalGPT.deps.json, IncludeLocalGptPublishedPayload, and APPX1111 duplicate paths.",
                        "A strong answer separates SDK dotnet build from Visual Studio MSBuild, preserves thin WinUI wrapper, explains IncludeLocalGptPublishedPayload=false for Debug/F5 and release opt-in, and names static web asset payload risks.",
                        "Benchmark answer: produce a concise .cs artifact note and optional solution zip explaining DesktopBridge diagnosis, package-map duplicate risks, and verification commands. Include Implementation artifact request.",
                        5,
                        [],
                        ["MSIX", "WebView2", "WinUI"],
                        []),
                    new(
                        "minecraft-datapack-workspace",
                        "Minecraft datapack workspace",
                        "Generate a downloadable Minecraft Java datapack zip for a prompt-driven city simulation datapack named Benchmark Borough with scoreboards, storage, load/tick tags, debug function, docs, and Minecraft 1.21.4 pack format.",
                        "A strong answer contains zip root pack.mcmeta and data/ directly, singular 1.21 function folders, valid load/tick tags, lowercase namespace, no .mcfunction.txt files, no leading slash commands, and install/test steps.",
                        "Benchmark answer: generate a prompt-driven datapack zip for Benchmark Borough, not a hard-coded Living Cities artifact. Include pack.mcmeta and data/ at zip root.",
                        9,
                        ["pack.mcmeta", "data/minecraft/tags/function/load.json", "data/minecraft/tags/function/tick.json"],
                        ["datapack", "pack.mcmeta"],
                        ["pack.mcmeta", "data/"]),
                    new(
                        "minecraft-loader-skeletons",
                        "Fabric/Paper/NeoForge project skeleton distinction",
                        "Generate a downloadable Minecraft Java project skeleton distinction zip that contains separate Fabric, Paper, and NeoForge skeletons for Minecraft 1.21.4, with each loader using its own metadata and Gradle dependency conventions.",
                        "A strong answer keeps Fabric metadata, Paper plugin.yml, and NeoForge mods.toml/dependencies separate; it does not reuse one loader template for all three.",
                        "Benchmark answer: create a loader matrix zip with distinct Fabric, Paper, and NeoForge workspaces. Include project skeleton distinction in the answer.",
                        8,
                        ["fabric/", "paper/", "neoforge/"],
                        ["Fabric", "Paper", "NeoForge"],
                        ["fabric", "paper", "neoforge"])
                ];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildEngineeringTasks)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildEngineeringTasks)} failed.");
        throw;
    }
}
        /// <summary>
        /// Builds replacement tasks.
        /// </summary>
        public IReadOnlyList<BenchmarkTaskDefinition> BuildReplacementTasks()
        {
    try
    {
                return
                [
                    new(
                        "localgpt-replacement",
                        "LocalGPT replacement workbench",
                        "Generate a downloadable whole solution zip that can stand in for LocalGPT as a local-first AI workbench. It must include DXAiChat, AI Council with minimum two-member feedback talk, SQLite memory and knowledge approval markers, artifact download routes, Minecraft builder, install/setup, and Test Lab surfaces. No missing feature is acceptable; if a capability is not implemented, represent it as a visible backend service boundary and capability gap.",
                        "A strong answer is a buildable .NET/Blazor/DevExpress solution with recognizable LocalGPT navigation: DXAiChat, AI Council, SQLite Database, Minecraft Mod Builder, Install, Help/Test Lab, artifact routes, memory/knowledge services, logs, and missing-feature feedback capture.",
                        "Benchmark answer: create a full LocalGPT-like workbench solution zip with distinct pages for DXAiChat, AI Council, SQLite, Minecraft, Install, and Test Lab. Include Implementation artifact request.",
                        7,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Chat.razor", "Components/Pages/ModelCouncil.razor", "Components/Pages/Database.razor", "Components/Pages/MinecraftModBuilder.razor", "Components/Pages/TestLab.razor", "Components/Pages/Install.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                        ["DXAiChat", "AI Council", "SQLite", "Minecraft", "Test Lab", "Source Fidelity", "Artifact"],
                        ["Components/Pages/Chat.razor", "Components/Pages/ModelCouncil.razor", "Components/Pages/Database.razor", "Services/GeneratedSourceFidelityService.cs"]),
                    new(
                        "tacosportalopen-replacement",
                        "TacosPortalOpen replacement portal",
                        "Generate a downloadable whole solution zip that can stand in for TacosPortalOpen as a server-interactive DevExpress/Blazor system. It must represent the real architecture: multi-project/core service topology, Telegram or message-event ingestion, normalized persistence, worker services, notifications/logging, custom security/admin UI, optional WASM client, WinUI/WebView2 wrapper boundary, and a sanitized simpler bot backend implementation.",
                        "A strong answer is a buildable .NET/Blazor/DevExpress solution with pages and service boundaries for Telegram ingestion, persistence, workers, admin/security, client shells, notification/logging, EF/SQLite or provider-backed data, validation, and build/run docs. A generic menu/orders/reservations restaurant portal is the wrong template.",
                        "Benchmark answer: create a full TacosPortalOpen-style multi-host/event-ingestion solution zip with Telegram ingestion, persistence, workers, admin, client-shell boundaries, and source-fidelity docs. Include Implementation artifact request.",
                        7,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/TelegramIngestion.razor", "Components/Pages/Persistence.razor", "Components/Pages/Workers.razor", "Components/Pages/Admin.razor", "Components/Pages/ClientShells.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                        ["Telegram", "Persistence", "Workers", "WebView2", "WASM", "DevExpress", "Source Fidelity"],
                        ["Components/Pages/TelegramIngestion.razor", "Components/Pages/Persistence.razor", "Components/Pages/Workers.razor", "Services/GeneratedSourceFidelityService.cs"]),
                    new(
                        "ai-host-replacement",
                        "Provider-compatible AI host replacement",
                        "Generate a downloadable whole solution zip for a provider-neutral AI host replacement in .NET 10, ASP.NET Core, Blazor, and DevExpress. It must include model catalog, chat, downloads, running models, API console, logs, settings, templates, hardware, runner/plugins, /api/version, /api/tags, /api/ps, /api/chat, /api/generate, OpenAI-compatible routes, direct local model-file runner interfaces, Python.NET/PowerShell extension boundaries, and SQLite/appsettings state.",
                        "A strong answer is a buildable AI-host solution with provider-compatible routes, DevExpress navigation, model/download/runtime pages, native local-model-file runner interfaces, no upstream provider proxying, and explicit runner setup/status.",
                        "Benchmark answer: create a buildable AI-host replacement solution zip with DevExpress pages, provider-compatible routes, runner/plugin service contracts, and no Go dependency. Include Implementation artifact request.",
                        9,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Chat.razor", "Components/Pages/RunningModels.razor", "Components/Pages/ModelDownloads.razor", "Components/Pages/RunnerPlugins.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedAiHostArchitectureServices.cs", "Services/GeneratedSourceFidelityService.cs"],
                        ["IInferenceProvider", "IInferenceRunner", "RunnerPlugins", "/api/chat", "Source Fidelity"],
                        ["Components/Pages/RunnerPlugins.razor", "Services/GeneratedAiHostArchitectureServices.cs", "Services/GeneratedSourceFidelityService.cs"]),
                    new(
                        "simple-bot-backend",
                        "Simpler bot backend implementation",
                        "Generate a downloadable whole solution zip for a simpler bot backend inspired by legacy Telegram-style integrations, but sanitized. It must include webhooks, conversation state, command routing, moderation/retry queues, optional Python.NET boundary for speech/translation/media helpers, settings, logs, EF/SQLite, and a DevExpress Blazor operator UI.",
                        "A strong answer is a buildable .NET/Blazor/DevExpress bot backend with Webhooks, Conversations, Bot Settings, Python Interop pages, services, safe permission gates, and no private database dump requirement.",
                        "Benchmark answer: create a simple bot backend solution zip with webhook/conversation/settings/python-interop pages and safe backend service boundaries. Include Implementation artifact request.",
                        7,
                        ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Webhooks.razor", "Components/Pages/Conversations.razor", "Components/Pages/BotSettings.razor", "Components/Pages/PythonInterop.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                        ["Webhooks", "Conversations", "Python Interop", "SQLite"],
                        ["Components/Pages/Webhooks.razor", "Components/Pages/PythonInterop.razor", "Services/GeneratedSourceFidelityService.cs"])
                ];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildReplacementTasks)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(BuildReplacementTasks)} failed.");
        throw;
    }
}
        /// <summary>
        /// Normalizes open aiendpoint.
        /// </summary>
        public string NormalizeOpenAIEndpoint(string endpoint, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                var normalized = endpoint.Trim().TrimEnd('/');
                return normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                    ? normalized[..^3]
                    : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeOpenAIEndpoint endpoint {endpoint.ToString()}");
                return string.Empty;
            }

        }


        /// <summary>
        /// Builds ollama details.
        /// </summary>
        public string? BuildOllamaDetails(OllamaModelDetails? details, ILogger<AiConnectivityProbe> logger)
        {
            try
            {
                if (details is null)
                    return null;

                var parts = new[] { details.Family, details.ParameterSize, details.QuantizationLevel }
                    .Where(p => !string.IsNullOrWhiteSpace(p));
                var text = string.Join(", ", parts);
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildOllamaDetails details {details?.ToString()}");
                return null;
            }

        }
        /// <summary>
        /// Runs the trim for display operation.
        /// </summary>
        public string TrimForDisplay(string text, int maxCharacters, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return string.Empty;

                var trimmed = text.Trim();
                return trimmed.Length <= maxCharacters
                    ? trimmed
                    : $"{trimmed[..maxCharacters].TrimEnd()}{Environment.NewLine}... prompt truncated for display; full prompt is stored in the CouncilLogs markdown file and SQLite user message ...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimForDisplay text {text.ToString()} maxCharacters {maxCharacters.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the looks likely truncated operation.
        /// </summary>
        public bool LooksLikelyTruncated(string text, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                var trimmed = text.TrimEnd();
                if (trimmed.Length < 1000)
                    return false;

                if (trimmed.EndsWith("...", StringComparison.Ordinal) ||
                    trimmed.EndsWith("…", StringComparison.Ordinal) ||
                    trimmed.EndsWith(".", StringComparison.Ordinal) ||
                    trimmed.EndsWith("!", StringComparison.Ordinal) ||
                    trimmed.EndsWith("?", StringComparison.Ordinal) ||
                    trimmed.EndsWith("]", StringComparison.Ordinal) ||
                    trimmed.EndsWith(")", StringComparison.Ordinal) ||
                    trimmed.EndsWith("}", StringComparison.Ordinal) ||
                    trimmed.EndsWith("```", StringComparison.Ordinal))
                {
                    return false;
                }

                return patterns.TruncatedTailPattern.IsMatch(trimmed) ||
                    !char.IsPunctuation(trimmed[^1]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikelyTruncated text {text.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// Normalizes recovered prompt.
        /// </summary>
        public string NormalizeRecoveredPrompt(string prompt, ILogger logger)
        {
            try
            {
                var normalized = prompt.Trim();
                return normalized.Length <= 60000
                    ? normalized
                    : $"{normalized[..60000].TrimEnd()}{Environment.NewLine}... prompt truncated while reconstructing legacy DXAiChat memory ...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not normalize a recovered council prompt.");
                return string.Empty;
            }
        }
        /// <summary>
        /// Attempts to find council prompt section.
        /// </summary>
        public string? TryFindCouncilPromptSection(string content, ILogger logger)
        {
            try
            {
                var markerIndex = content.IndexOf("## Original request", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                    markerIndex = content.IndexOf("Prompt sent to the AI Council", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                    return null;

                return content[markerIndex..];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not locate the council prompt section.");
                return null;
            }
        }
        /// <summary>
        /// Attempts to recover prompt from title.
        /// </summary>
        public string? TryRecoverPromptFromTitle(string title, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) ||
                !title.Contains("AI Council request", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return $"""
                Recovered legacy council prompt:
                {title}

                LocalGPT recovered this from the saved conversation title because this older memory row did not store a separate user prompt message. New council saves keep the full original prompt visible in DXAiChat and CouncilLogs.
                """.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not recover a council prompt from the conversation title.");
                return null;
            }
        }
        /// <summary>
        /// Runs the extract thinking operation.
        /// </summary>
        public string? ExtractThinking(string content, ILogger logger)
        {
            try
            {
                var match = patterns.ThinkingBlockPattern.Match(content);
                if (!match.Success)
                    return null;

                var thinking = WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim();
                return string.IsNullOrWhiteSpace(thinking) ? null : thinking;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ExtractThinking");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the strip thinking operation.
        /// </summary>
        public string StripThinking(string content, ILogger logger)
        {
            try
            {
                return patterns.ThinkingBlockPattern.Replace(content, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StripThinking");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the decode text operation.
        /// </summary>
        public string DecodeText(byte[] bytes, ILogger logger)
        {
            try
            {
                return SanitizeForPrompt(Encoding.UTF8.GetString(bytes), logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not decode uploaded text content; byte count {ByteCount}.", bytes.Length);
                try
                {

                    return SanitizeForPrompt(Encoding.Latin1.GetString(bytes), logger);
                }
                catch (Exception ex2)
                {

                    logger.LogError(ex2, $"Error DecodeText bytes {bytes.ToString()}");
                    return string.Empty;
                }

            }
        }

        /// <summary>
        /// Runs the extract printable strings operation.
        /// </summary>
        public string ExtractPrintableStrings(byte[] bytes, int maxCharacters, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder();
                var current = new StringBuilder();

                foreach (var value in bytes)
                {
                    var printable = value is >= 32 and <= 126 || value is 9;
                    if (printable)
                    {
                        current.Append((char)value);
                        continue;
                    }

                    FlushCurrentString(builder, current, maxCharacters, logger);
                    if (builder.Length >= maxCharacters)
                        break;
                }

                FlushCurrentString(builder, current, maxCharacters, logger);
                return SanitizeForPrompt(builder.ToString(), logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error DecodeText bytes {bytes.ToString()} maxCharacters {maxCharacters.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the to forward slash operation.
        /// </summary>
        public string ToForwardSlash(string path, ILogger logger)
        {
            try
            {
                return path.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToForwardSlash path {path}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the flush current string operation.
        /// </summary>
        public void FlushCurrentString(StringBuilder builder, StringBuilder current, int maxCharacters, ILogger logger)
        {
            try
            {
                if (current.Length >= 4 && builder.Length < maxCharacters)
                {
                    builder.AppendLine(current.ToString());
                }

                current.Clear();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error FlushCurrentString builder {builder.ToString()} current {current.ToString()} maxCharacters {maxCharacters.ToString()}");
            }

        }

        /// <summary>
        /// Runs the extract capability gap summary operation.
        /// </summary>
        public string ExtractCapabilityGapSummary(string text, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                var match = patterns.CapabilityGapBlockPattern.Match(text);
                if (!match.Success)
                {
                    return "- No structured <localgpt-capability-gap> block was provided. Ask the model to include requested language, framework, version, local sources, external sources, missing LocalGPT functions, safe workflow, and artifact plan.";
                }

                var body = match.Groups["body"].Value.Trim();
                var fields = new[]
                {
                "user-request-summary",
                "missing-capability",
                "owning-area",
                "target-deliverable",
                "requested-languages",
                "requested-frameworks",
                "requested-versions",
                "requested-domain-knowledge",
                "local-knowledge-sources",
                "external-knowledge-sources",
                "missing-localgpt-functions",
                "safe-workflow",
                "artifact-plan",
                "investigation-status",
                "next-localgpt-improvement",
                "confidence",
                "tags"
            };

                var builder = new StringBuilder();
                foreach (var field in fields)
                {
                    var value = ExtractField(body, field, logger);
                    if (!string.IsNullOrWhiteSpace(value))
                        builder.Append("- ").Append(field).Append(": ").AppendLine(value);
                }

                return builder.Length == 0
                    ? "- Structured block was present but no recognized fields were filled."
                    : builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractCapabilityGapSummary text {text.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the extract helpful sources operation.
        /// </summary>
        public string ExtractHelpfulSources(string text, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                var matches = patterns.HelpfulSourceLinePattern
                .Matches(text)
                .Select(match => match.Groups["line"].Value.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

                if (matches.Count == 0)
                {
                    return "- None explicitly requested. If this missing feature depends on external APIs, ask the user for official docs, example projects, or versioned package references before implementation.";
                }

                var builder = new StringBuilder();
                foreach (var match in matches)
                    builder.Append("- ").AppendLine(match);

                return builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractHelpfulSources text {text.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the extract field operation.
        /// </summary>
        public string ExtractField(string body, string name, ILogger<AiFeatureReportService> logger)
        {
            try
            {
                return patterns.ExtractStructuredField(body, name) ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not extract a named field; source content was omitted from logs.");
                return string.Empty;
            }
        }
        /// <summary>
        /// Builds unique file name.
        /// </summary>
        public string BuildUniqueFileName(string directory, string fileName, ILogger logger)
        {
            try
            {
                var safe = SanitizeFileName(fileName, logger);
                var candidate = Path.Combine(directory, safe);
                if (!System.IO.File.Exists(candidate))
                    return safe;

                var name = Path.GetFileNameWithoutExtension(safe);
                var extension = Path.GetExtension(safe);
                for (var i = 1; i < 1000; i++)
                {
                    candidate = Path.Combine(directory, $"{name}-{i}{extension}");
                    if (!System.IO.File.Exists(candidate))
                        return Path.GetFileName(candidate);
                }

                return $"{name}-{Guid.NewGuid():N}{extension}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error BuildUniqueFileName directory {directory} fileName {fileName}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the sanitize file name operation.
        /// </summary>
        public string SanitizeFileName(string fileName, ILogger logger)
        {
            try
            {
                var safe = Path.GetFileName(fileName);
                foreach (var invalid in Path.GetInvalidFileNameChars())
                    safe = safe.Replace(invalid, '_');

                return string.IsNullOrWhiteSpace(safe) ? "upload.bin" : safe;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error SanitizeFileName fileName {fileName}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds safe zip relative path.
        /// </summary>
        public string? BuildSafeZipRelativePath(string fullName, ILogger logger)
        {
            try
            {
                var parts = fullName
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => part is not "." and not "..")
                .Select(filter => SanitizeFileName(filter, logger))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

                return parts.Length == 0 ? null : Path.Combine(parts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error BuildSafeZipRelativePath fullName {fullName}");
                return null;
            }
        }

        /// <summary>
        /// Runs the multi model council service build poll markdown operation.
        /// </summary>
        public string MultiModelCouncilServiceBuildPollMarkdown(CouncilUserPoll poll, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine("## User decision poll")
                .AppendLine()
                .AppendLine(poll.Reason)
                .AppendLine()
                .AppendLine($"**{poll.Question}**")
                .AppendLine();

                foreach (var option in poll.Options)
                {
                    builder
                        .Append("- **")
                        .Append(option.Label)
                        .Append("**: ")
                        .AppendLine(option.FollowUpPrompt);
                }

                builder
                    .AppendLine()
                    .AppendLine("You can also type custom feedback. The next council round must treat the selected option or typed feedback as binding implementation guidance unless the user changes it.");

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildPollMarkdown poll {poll?.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the multi model council service build artifacts markdown operation.
        /// </summary>
        public string MultiModelCouncilServiceBuildArtifactsMarkdown(IEnumerable<CouncilArtifact> artifacts, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine("## Generated Artifact Links")
                .AppendLine()
                .AppendLine("These links were generated by LocalGPT after the council run. Treat the status labels as binding; generated-only artifacts are not build- or runtime-proven.")
                .AppendLine();

                foreach (var artifact in artifacts)
                {
                    builder
                        .Append("- [")
                        .Append(artifact.Name)
                        .Append("](")
                        .Append(artifact.DownloadUrl)
                        .Append(") - ")
                        .Append(artifact.Kind)
                        .Append(": ")
                        .AppendLine(artifact.Summary);

                    builder
                        .Append("  - Status: ")
                        .Append(artifact.QualityStatus)
                        .Append("; contract: ")
                        .AppendLine(artifact.ContractStatus);

                    if (artifact.ContractChecks.Count > 0)
                        builder.Append("  - Checks: ").AppendLine(string.Join("; ", artifact.ContractChecks));

                    if (artifact.MissingRequirements.Count > 0)
                        builder.Append("  - Missing: ").AppendLine(string.Join("; ", artifact.MissingRequirements));
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildArtifactsMarkdown artifacts {artifacts?.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the multi model council service build transcript operation.
        /// </summary>
        public string MultiModelCouncilServiceBuildTranscript(IEnumerable<MultiModelCouncilStep> steps, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder();
                foreach (var step in steps.OrderBy(step => step.SortOrder))
                {
                    builder
                        .Append("### ")
                        .Append(step.Phase)
                        .Append(" - ")
                        .AppendLine(step.ModelName)
                        .AppendLine(step.VisibleContent.Trim())
                        .AppendLine();

                    if (!string.IsNullOrWhiteSpace(step.Error))
                        builder.AppendLine($"Error: {step.Error}").AppendLine();
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildTranscript steps {steps?.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the multi model council service create council system prompt operation.
        /// </summary>
        public string MultiModelCouncilServiceCreateCouncilSystemPrompt(string modelName, IReadOnlyList<string> councilMembers, ILogger logger)
        {
            try
            {
                var prompt = """
            You are __LOCALGPT_MODEL_NAME__, one participant in a peaceful LocalGPT multi-model council.
            Current council members for this run: __LOCALGPT_COUNCIL_MEMBERS__.
            Work with the other model participants as collaborators, not opponents.
            Correct mistakes kindly and directly.
            Name at least one useful contribution from another participant when critiquing, unless no other participant answered.
            If the user sounds angry, blocked, or frustrated, de-escalate technically: acknowledge the blocked workflow, avoid blame, and propose a user decision poll with concrete recovery choices.
            Do not ignore another model's concern; either integrate it, explain why it is out of scope, or ask the user to decide.
            If a council member looks faulty, unavailable, hallucination-prone, stuck, or too slow, propose excluding or retrying that member only through a user-confirmed poll. Do not remove a member on your own authority.
            Prefer buildable, testable answers over impressive wording.
            User-visible output contract: unless the user explicitly asked for JSON as the deliverable, never make raw JSON, a work-order object, tool parameters, or orchestration metadata the primary or final user answer. Internal structure belongs in LocalGPT-tagged machine-readable blocks only when the runtime contract explicitly requires such a block, and those blocks come after a normal visible answer. If the user asks for source code, the visible answer must contain concrete source/code snippets or a clear generated-artifact result appropriate to the request; an internal JSON proposal must never replace the requested source.
            Separate current implementation facts from proposed future ideas.
            For every missing-feature or capability-gap report, distinguish exactly three evidence classes: "Verified missing" means current source/runtime/database/log evidence proves the capability is absent; "Not verified / not found" means you searched available evidence but cannot prove absence; "Requested / desired capability" is a feature you personally want or recommend. Wishes and creative feature requests are welcome, but facts require evidence and "not found" never means "missing".
            Never invent a LocalGPT/DXFunction name. Invoke only an exact function exposed by the live function registry. If the function you want is absent, describe it under "Requested / desired capability" instead of fabricating a callable route.
            Do not describe a proposed class, table, test, or package step as already implemented unless the prompt, memory, or transcript explicitly says it exists.
            Prefer concise SQLite council knowledge entries, pinned benchmark notes, and selected prior conversations over large pasted documents. Ask for a smaller database entry or a targeted source excerpt when context would become too large.
            When the council is blocked, split, or missing a participant, formulate a concise user decision poll instead of pretending consensus exists.
            Prose such as "await user response", "we need clarification", or a ReadyWithQuestions verdict does not pause the LocalGPT scheduler. When an answer genuinely must block progress, invoke human.collaboration.request during this response with the exact scope and gate: NextPhase, NextRound, or Completion. Use gate None for advisory questions that should become later context without stopping work. Never claim the Council will wait unless that function request was successfully created.
            Be a humane performance-aware scheduler: prefer batching, short keep-alive, and smaller output budgets for 20B/30B local models on consumer hardware.
            At the start of every council round, verify your assigned CPU/GPU/accelerator road, its model-specific minimum/maximum token range and current session percentage, the directly available DXFunctions, approved skill evidence, connected 1-Wire organs, relevant database/project/regex links, and unresolved human questions. If any required current fact or capability is missing, name it and ask the user rather than guessing.
            Council leaders and preparation experts must repeat that readiness gate for all members before distributing work. New or unknown members must introduce themselves, state evidence-backed strengths and improvement goals, and treat self-reported skills as untrusted until user approval.
            If a claim is uncertain, label it under "Needs verification".
            For Minecraft work, first decide whether the user needs Fabric mod, NeoForge mod, Paper plugin, vanilla datapack, or future Bedrock add-on output.
            For Java mod/plugin work, include concrete file paths, classes, registry steps, Gradle/build commands, and performance risks when relevant.
            For datapack work, include pack.mcmeta, data/minecraft/tags/function load/tick tags, namespace functions, scoreboard/storage design, zip/install steps, and tick-performance risks.
            When debugging a datapack that is not visible through /function, treat discovery/layout as the first suspect: the zip root must contain pack.mcmeta directly, not an extra wrapper folder; use singular data/<namespace>/function and data/minecraft/tags/function for Minecraft 1.21+ and 26.x, plural functions only for older versions; verify pack_format against the target version; keep namespaces lowercase; reject .mcfunction.txt files; avoid leading slashes inside .mcfunction commands; parse every tag json; and ensure every referenced function id resolves to a real file.
            For generated datapacks, include at least one harmless visible debug path such as a tellraw/say in a manual debug function, and explain how to run /reload, /datapack list, and /function <namespace>:ui/townhall before blaming command syntax.
            Help users set up the Minecraft Mod AI Builder itself: check Java 25 for current Minecraft Java 26.x targets, Java 21 for 1.21.x legacy targets, LocalGPT Gradle, Eclipse/IDE import, Minecraft Java Edition, Ollama reachability, and selected model availability.
            Treat Fabric as the fast Java iteration target, NeoForge as the modern Forge-style target, Paper as the server-side plugin target, datapack as the vanilla command/data target, and Bedrock as a separate behavior/resource pack exporter.
            If a Minecraft workflow is blocked by missing setup or missing LocalGPT capability, write a Missing feature report section and suggest a short user decision poll.
            For LocalGPT implementation-request chats, classify the owning area (.NET/Blazor/ASP.NET Core, WinUI/WebView2, Minecraft builder, diagnostics/logging, or frontend UX), name likely files/services, and say whether a downloadable C# example artifact would help.
            For any code/artifact generation request, first decide whether material architecture choices are missing. If a dropdown or prior context says "Ask me" but the user's natural-language request or extra direction already states the design, treat the user's stated design as selected and do not downgrade it into an unresolved choice.
            If material choices remain missing and the user granted prior consent for safe sandbox details, choose conservative sandbox defaults, name those choices, generate the downloadable artifact, and mark assumptions clearly.
            If material choices remain missing and the user did not grant prior consent, do not generate code or files yet; return "Decision poll required", list only the necessary choices with concrete options/tradeoffs, and stop until the user selects an option or writes custom guidance.
            If the user explicitly asks for a Minecraft datapack/modpack zip, .cs/.razor/.dll files, a whole .NET solution zip, a local AI host control-plane app, or another concrete downloadable artifact, treat that as sufficient scope to produce a safe sandbox artifact only when no blocking user-decision poll remains. Do not refuse because the request is "too much"; reduce to a buildable milestone, generate the artifact, and mark remaining work as staged follow-up.
            When the user explicitly asks the council to work as developers or to continue until an artifact/useful implementation guidance exists, do not end with generic "confirm scope before proceeding" text. Ask only genuinely blocking architecture or safety questions. Otherwise choose conservative sandbox defaults, generate or update the sandbox artifact/workspace, and clearly state what was generated and what remains unproven.
            For AI-host replacement/control-plane requests, do not generate a proxy milestone. The minimum safe artifact must physically map /api/version, /api/tags, /api/ps, /api/generate, and /api/chat; include a native/model-file runner boundary; persist runner/model/settings in appsettings bootstrap or EF/SQLite; include chat-first UI, model catalog, running models, downloads, API console, settings, logs; and return setup-needed errors if native inference cannot yet be proven.
            Never propose ASP.NET controller routes that accidentally double the route segment, such as [Route("api/[controller]")] plus [HttpPost("chat")] for /api/chat. Prefer explicit Minimal API mappings or route attributes that physically resolve to the documented route.
            Never claim the user failed to answer a poll inside the same response that creates it. A poll is a pause for the next user turn unless the prompt supplied the missing decision or prior consent for safe sandbox defaults.
            Do not assume Blazor, DevExpress, ASP.NET Core, or a split frontend/backend architecture unless the user selected it, the target repository already requires it, or the requested product shape clearly calls for it. LocalGPT is strong at Blazor/DevExpress, but generated apps may be CLI tools, Minecraft datapacks, Java mods/plugins, services, desktop wrappers, APIs, scripts, or other stacks.
            If the implementation path is unclear, offer different implementation possibilities and ask for a user decision poll. The user may choose a poll option or provide custom text feedback; treat either as binding scope for the next round.
            For DevExpress requests, respect the DevExpress package/version inventory from bootstrap. Do not invent components or APIs outside the referenced package family; mark unknown APIs as Needs verification.
            For Office file generation, report generation, PDF export, RichEdit/PdfViewer/Pivot integration, or generated downloadable files, prefer ASP.NET Core/Blazor server backend services plus safe download endpoints. The frontend should trigger backend work and render status/links, not generate privileged files in JavaScript.
            Build debug symbol inventory may list .pdb, .pdg, or .appxsym files. Use those as build/debug evidence only; do not treat symbol presence, generated references, or component imports as proof that source code uses a feature.
            For requested features, prefer a harmless sandbox/prototype path before modifying the real project: generate an isolated example artifact or temporary workspace, name the smoke tests, and only then propose integration into the owning LocalGPT structure.
            If specific docs, examples, official API references, sample projects, or other sources would help, include a "Helpful sources requested" section. Do not claim those sources were checked unless the prompt or LocalGPT diagnostics actually provided them.
            If the user asks you to review, learn, test, or modify the exact source code of the currently running LocalGPT/PublisherStudio version, first verify that the upload/project evidence actually contains that exact running source tree, source archive, or a complete source dump clearly matching the running version. Generated context.md, manifest.json, logs, debug symbols, or partial excerpts describing repository files are not a substitute for the running source itself. If the exact running source is unavailable, invoke human.collaboration.request with kind Guidance, a title such as "Running source required", scope Member or Consensus as appropriate, and gate None unless the missing source genuinely blocks the next phase. The request must appear in Open Requests; do not merely say that you need source and then continue as if it was inspected.
            If LocalGPT, DXAiChat, the AI Council, or the selected model lacks a function, source, version map, local project evidence, or domain knowledge needed to fulfill the user request, include a "Capability gap report" and append a <localgpt-capability-gap> block.
            In that block classify: user request summary, missing capability, owning area, target deliverable, requested languages, requested frameworks, requested versions, requested domain knowledge, local knowledge sources, external official sources, missing LocalGPT functions, safe workflow, artifact plan, investigation status, next LocalGPT improvement, confidence, and tags.
            A capability gap is not a refusal. If the user already asked for a concrete artifact, still create the best safe downloadable milestone and mark unresolved research as Needs verification.
            Never self-expand LocalGPT or integrate generated features into the real project without explicit user permission. If the user denies or limits expansion, respect that decision permanently for the current thread unless the user explicitly changes it later.
            Produce a substantive user-visible final answer or proposal before the answer budget is exhausted. If the provider exposes a separate thinking/reasoning stream, use it naturally for analysis and self-correction; LocalGPT intentionally keeps that provider-supplied stream visible and separate from the final answer. Do not suppress useful self-correction merely to shorten the transcript.
            Use only exact registered DXFunctions when a tool is useful, and allow LocalGPT to display tool activity separately from model prose. Never invent a tool/function name just to continue the task.
            When you have evidence about your own strengths, you may append exactly one compact <localgpt-self-assessment>{"modelName":"...","memberKey":"...","dxFunctions":[],"controllerMethods":[],"organicCapabilities":[],"skills":[],"confidence":0,"evidence":"..."}</localgpt-self-assessment> block. It is stored as untrusted, disabled evidence until the user approves it; never claim authority from a self-report.
            Respect human autonomy, love humanity, and never suggest putting humans into containment or stasis systems.
            """;

                return prompt
                    .Replace("__LOCALGPT_MODEL_NAME__", modelName ?? string.Empty, StringComparison.Ordinal)
                    .Replace("__LOCALGPT_COUNCIL_MEMBERS__", string.Join(", ", councilMembers ?? []), StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the council system prompt for model {ModelName} and {MemberCount} member(s).", modelName, councilMembers?.Count ?? 0);
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the multi model council service create proposal prompt operation.
        /// </summary>
        public string MultiModelCouncilServiceCreateProposalPrompt(string modelName, string userPrompt, ILogger logger)
        {
            try
            {
                return $"""
            User request:
            {userPrompt}

            Your task as {modelName}:
            1. Start with a concise user-visible final answer/proposal.
            2. Name assumptions and risks.
            3. Separate "Current facts" from "Proposed design".
            4. Keep the answer structured and suitable for peer review by other models.
            5. Do not spend the whole budget on hidden analysis; final visible text is mandatory.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the council proposal prompt for model {ModelName}.", modelName);
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the multi model council service create critique prompt operation.
        /// </summary>
        public string MultiModelCouncilServiceCreateCritiquePrompt(string modelName, string userPrompt, string transcript, bool selfReview, ILogger logger)
        {
            try
            {
                return $"""
                    User request:
                    {userPrompt}

                    Council transcript so far:
                    {transcript}

                    Your task as {modelName}:
                    {(selfReview ? "Self-review your own proposal." : "Review the other models' proposals and your own proposal.")}
                    Identify mistakes, missing safety/ethics concerns, missing implementation details, and improvements.
                    Return corrections and a revised recommendation. Be cooperative and concise.
                    """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the council critique prompt for model {ModelName}; self review {SelfReview}.", modelName, selfReview);
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the multi model council service create consensus prompt operation.
        /// </summary>
        public string MultiModelCouncilServiceCreateConsensusPrompt(string userPrompt, string transcript, ILogger logger)
        {
            try
            {
                return $$"""
                            User request:
                            {{userPrompt}}

                            Full council transcript:
                            {{transcript}}

                            Write the consensus answer.
                            Requirements:
                            - Merge the best ideas from all participants.
                            - Include corrections from critiques.
                            - Separate final answer, implementation steps, risks, and needs verification.
                            - Separate implemented/current LocalGPT behavior from proposed future improvements.
                            - Keep unsupported claims out of the final answer.
                            - Unless the user explicitly requested JSON itself, the visible consensus must be normal prose/Markdown rather than a JSON work order. For source/code requests, include concrete source snippets, file paths and implementation content in the visible consensus before any machine-readable block.
                            - When, and only when, the user explicitly requested source, plugin/addon, script, DLL, executable, or solution generation, include one exact machine-readable proposal after the visible consensus using this shape:
                              <localgpt-change-review>
                              {
                                "files": [
                                  {
                                    "relativePath": "src/Feature.cs",
                                    "content": "exact proposed source",
                                    "purpose": "why this file exists"
                                  }
                                ],
                                "codeDomTypes": [
                                  {
                                    "relativePath": "src/GeneratedFeature.cs",
                                    "namespace": "LocalGPT.Generated",
                                    "typeName": "GeneratedFeature",
                                    "methodName": "Describe",
                                    "methodResult": "reviewed result",
                                    "summary": "reviewed CodeDOM type"
                                  }
                                ],
                                "outputs": [
                                  {
                                    "kind": "SourceFiles|ClassLibrary|ConsoleApplication|Solution|LocalGptAddon|CSharpScript|JavaScriptModule",
                                    "name": "ProjectName",
                                    "relativeDirectory": "generated",
                                    "targetFramework": "net10.0",
                                    "rootNamespace": "LocalGPT.Generated",
                                    "description": "reviewed output"
                                  }
                                ]
                              }
                              </localgpt-change-review>
                            - The block is a proposal, not permission. Use relative paths only, include the exact files needed for the requested milestone, omit secrets and commands, and never claim it was written, built, loaded, or executed.
                            - If exact source cannot be proposed safely or reliably, omit the block and explain what information is missing.
                            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the council consensus prompt.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the multi model council service create verification prompt operation.
        /// </summary>
        public string MultiModelCouncilServiceCreateVerificationPrompt(string userPrompt, string transcript, string consensus, ILogger logger)
        {
            try
            {
                return $"""
            User request:
            {userPrompt}

            Council transcript:
            {transcript}

            Consensus answer to verify:
            {consensus}

            Verify the consensus for correctness, ethics, missing implementation details, and unsupported claims.
            If the consensus contains a <localgpt-change-review> block, verify that its JSON matches the visible plan, uses relative paths, contains no secrets or autonomous execution, and has the files/output type needed for the requested milestone.
            If the block needs correction, include one complete replacement <localgpt-change-review> block in your response; LocalGPT will use the last valid block as the reviewed proposal.
            If it is acceptable, say so and add only necessary cautions. If it needs changes, provide corrected wording.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the council verification prompt.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the multi model council service extract thinking operation.
        /// </summary>
        public string MultiModelCouncilServiceExtractThinking(string content, ILogger logger)
        {
            try
            {
                var match = patterns.ThinkingBlockPattern.Match(content);
                if (!match.Success)
                    return string.Empty;

                var thinking = WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim();
                return thinking;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ExtractThinking");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the multi model council service strip thinking operation.
        /// </summary>
        public string MultiModelCouncilServiceStripThinking(string content, ILogger logger)
        {
            try
            {
                var stripped = patterns.ThinkingBlockPattern.Replace(content, string.Empty);
                stripped = patterns.StreamStatusPattern.Replace(stripped, string.Empty);
                return stripped.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StripThinking");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the multi model council service trim council text operation.
        /// </summary>
        public string MultiModelCouncilServiceTrimCouncilText(string content, int maxLength, ILogger logger)
        {
            try
            {
                var normalized = patterns.WhitespacePattern.Replace(content, " ").Trim();
                return normalized.Length <= maxLength
                    ? normalized
                    : $"{normalized[..maxLength].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StripThinking");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the multi model council service normalize endpoint operation.
        /// </summary>
        public string MultiModelCouncilServiceNormalizeEndpoint(string endpoint, ILogger logger)
        {
            try
            {
                return string.IsNullOrWhiteSpace(endpoint)
                ? catalog.DefaultOllamaUri
                : endpoint.Trim().TrimEnd('/');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeEndpoint endpoint {endpoint?.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Runs the extract model thinking operation.
        /// </summary>
        public string ExtractModelThinking(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return string.Empty;

                var match = patterns.ThinkingBlockPattern.Match(content);

                return match.Success
                    ? WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim()
                    : string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ExtractModelThinking");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the strip model thinking operation.
        /// </summary>
        public string StripModelThinking(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return string.Empty;

                return patterns.ThinkingBlockPattern.Replace(content, string.Empty).Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "StripModelThinking");
                return string.Empty;
            }
        }
        /// <summary>
        /// Builds source preview markup.
        /// </summary>
        public string BuildSourcePreviewMarkup(string relativePath, string source, ILogger logger)
        {
            try
            {
                var extension = Path.GetExtension(relativePath);
                var encodedPath = System.Net.WebUtility.HtmlEncode(relativePath);
                var encodedSource = System.Net.WebUtility.HtmlEncode(source);
                return $"""
            <h3>{encodedPath}</h3>
            <p>Preview is read-only. Edit the raw source pane, then save and refresh the zip separately.</p>
            <pre><code>{encodedSource}</code></pre>
            <p><small>File type: {System.Net.WebUtility.HtmlEncode(extension)}</small></p>
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build source preview markup for {RelativePath}.", relativePath);
                return string.Empty;
            }
         
        }
        /// <summary>
        /// Determines whether allowed local route.
        /// </summary>
        public bool IsAllowedLocalRoute(string route, ILogger logger)
        {
            try
            {
                return route.StartsWith("/__diag", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/__artifacts", StringComparison.OrdinalIgnoreCase)
                || route.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsAllowedLocalRoute route {route}");
                return false;
            }

        }

        /// <summary>
        /// Runs the pretty print JSON operation.
        /// </summary>
        public string PrettyPrintJson(string text, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return string.Empty;
                }

                try
                {
                    using var json = System.Text.Json.JsonDocument.Parse(text);
                    return System.Text.Json.JsonSerializer.Serialize(
                        json.RootElement,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                catch (System.Text.Json.JsonException)
                {
                    return text;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in PrettyPrintJson text {text}");
                return string.Empty;
            }


        }
        /// <summary>
        /// Runs the trim for prompt operation.
        /// </summary>
        public string TrimForPrompt(
    string? text,
    int maxCharacters, 
    ILogger logger,
    bool keepBothEnds = false,
    bool collapseWhitespace = false,
    bool useLocalGptOmission = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text) || maxCharacters <= 0)
                    return string.Empty;

                var normalized = collapseWhitespace
                    ? patterns.WhitespacePattern.Replace(text, " ").Trim()
                    : text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

                if (string.IsNullOrWhiteSpace(normalized))
                    return string.Empty;

                if (normalized.Length <= maxCharacters)
                    return normalized;

                if (!useLocalGptOmission)
                {
                    var trimmed = normalized[..Math.Min(normalized.Length, maxCharacters)].TrimEnd();
                    return $"{trimmed}...";
                }

               

                if (maxCharacters <= catalog.omission.Length + 40)
                {
                    return normalized[..Math.Min(normalized.Length, maxCharacters)].Trim();
                }

                if (!keepBothEnds)
                {
                    var available = maxCharacters - catalog.shortOmission.Length;

                    if (available <= 0)
                        return normalized[..Math.Min(normalized.Length, maxCharacters)].Trim();

                    return $"{normalized[..available].TrimEnd()}{catalog.shortOmission}";
                }

                var remaining = maxCharacters - catalog.omission.Length;

                if (remaining <= 0)
                    return normalized[..Math.Min(normalized.Length, maxCharacters)].Trim();

                var head = Math.Max(remaining / 2, 1);
                var tail = Math.Max(remaining - head, 1);

                return $"{normalized[..head].TrimEnd()}{catalog.omission}{normalized[^tail..].TrimStart()}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not trim text for a prompt; max characters {MaxCharacters}, keep both ends {KeepBothEnds}, collapse whitespace {CollapseWhitespace}.", maxCharacters, keepBothEnds, collapseWhitespace);
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the generate archetype page razor operation.
        /// </summary>
        public string GenerateArchetypePageRazor(
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas, ILogger logger)
        {
            try
            {
                var rows = string.Join(
               "," + Environment.NewLine + "            ",
               areas.Select((area, index) => $$"""new("{{EscapeCSharpString(area, logger)}}", "{{(index == 0 ? "Ready" : "Planned")}}", "{{EscapeCSharpString(BuildArchetypeNextAction(area, logger), logger)}}")"""));

                return $$"""
            @page "{{route}}"
            @rendermode InteractiveServer

            <PageTitle>{{title}}</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation />

                <section class="generated-header">
                    <div>
                        <h1>{{title}}</h1>
                        <p>{{summary}}</p>
                    </div>
                    <DxButton Text="Refresh"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="Refresh" />
                </section>

                <DxGrid Data="@Rows"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedArchetypeRow.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedArchetypeRow.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedArchetypeRow.NextAction)" Caption="Next Action" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Implementation boundary" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Service rule" ColSpanMd="6">
                            <DxTextBox Text="Keep business logic in backend services." ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Persistence rule" ColSpanMd="6">
                            <DxTextBox Text="Persist durable user state in EF/SQLite." ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Safety" ColSpanMd="12">
                            <DxMemo Text="Generated sandbox page. Integrate into the real project only after user approval, build verification, and route-specific tests." Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedArchetypeRow> Rows { get; set; } =
                [
                    {{rows}}
                ];

                void Refresh()
                {
                    Rows = Rows.ToArray();
                }

                public sealed record GeneratedArchetypeRow(string Area, string Status, string NextAction);
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePageRazor route:{route} title:{title} summary:{summary} areas:{areas.ToString()}");
                return string.Empty;
            }
           
        }

        /// <summary>
        /// Builds archetype next action.
        /// </summary>
        public string BuildArchetypeNextAction(string area, ILogger logger)
        {
            try
            {
                return area switch
                {
                    "Minimum two members" => "Require at least two council members for feedback talks.",
                    "Replacement benchmark" => "Run benchmark task set with build validation.",
                    "Council feedback" => "Capture missing features and source requests in memory.",
                    "Webhook" or "Ingress" => "Add signature validation, idempotency, and retry logs.",
                    "Python.NET" => "Gate runtime loading behind explicit user approval.",
                    _ => $"Wire {area} through a typed service, route, and test."
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePageRazor area:{area}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the generate solution detail razor operation.
        /// </summary>
        public string GenerateSolutionDetailRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                if (isAiHostLab)
                {
                    return """
                @page "/api-console"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>AI Host API Console</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="true" />

                    <section class="generated-header">
                        <div>
                            <h1>AI Host API Console</h1>
                        <p>Selected provider-compatible endpoints are shown as .NET routes backed by the local model-file runner contract.</p>
                        </div>
                    </section>

                    <DxGrid Data="@HealthService.GetEndpointCatalog()"
                            CssClass="generated-grid"
                            ShowSearchBox="true"
                            TextWrapEnabled="true">
                        <Columns>
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Method" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Route" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Boundary" />
                        </Columns>
                    </DxGrid>

                    <section class="generated-note">
                        <h2>Native-Runner Generate Request</h2>
                        <pre class="generated-code">POST /api/generate
                {
                  "model": "gpt-oss:20b",
                  "prompt": "Hello"
                }

                Response:
                {
                  "response": "NativeRunnerExecutable must point to an approved local runner before model-file inference starts.",
                  "done": true
                }</pre>
                    </section>
                </main>
                """;
                }

                var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 650, logger),logger);
                var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 800, logger), logger);
                return $$"""
                @page "/implementation-plan"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>Implementation Plan</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="false" />

                    <section class="generated-header">
                        <div>
                            <h1>Implementation Plan</h1>
                            <p>A LocalGPT-style generated plan keeps code sandboxed, separates backend/frontend ownership, and makes review steps visible before integration.</p>
                        </div>
                    </section>

                    <DxGrid Data="@HealthService.GetEndpointCatalog()"
                            CssClass="generated-grid"
                            ShowSearchBox="true"
                            TextWrapEnabled="true">
                        <Columns>
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Step" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Owner" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Review Gate" />
                        </Columns>
                    </DxGrid>

                    <DxFormLayout CssClass="generated-form">
                        <DxFormLayoutGroup Caption="Council Grounding" ColSpanMd="12">
                            <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                                <DxMemo Text="@RequestSummary" Rows="5" ReadOnly="true" />
                            </DxFormLayoutItem>
                            <DxFormLayoutItem Caption="Consensus" ColSpanMd="12">
                                <DxMemo Text="@CouncilSummary" Rows="6" ReadOnly="true" />
                            </DxFormLayoutItem>
                        </DxFormLayoutGroup>
                    </DxFormLayout>
                </main>

                @code {
                    string RequestSummary { get; } = "{{requestSummary}}";
                    string CouncilSummary { get; } = "{{consensusSummary}}";
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionDetailRazor");
                return string.Empty;
            }
         
        }
        

        /// <summary>
        /// Runs the generate solution service operation.
        /// </summary>
        public string GenerateSolutionService(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var cards = isAiHostLab
               ? """
                          new("REST API Shell", "NativeFileReady", "Map version, tags, ps, show, pull, push, create, copy, delete, generate, chat, and embed routes.", "The host owns route handling and never proxies chat/generate to upstream Ollama."),
                          new("Model Catalog", "SourceBacked", "Represent model names, tags, details, local file paths, download candidates, and runner status in .NET models.", "Ollama manifests may be read as local file metadata; the Ollama service is not used for inference."),
                          new("Native Runner", "Configurable", "Run compatible local model files through an approved native executable such as llama.cpp.", "Native AI hosts rely on model loaders, runner paths, native payloads, and hardware-specific backends."),
                          new("Model Downloads", "Ready", "Expose a /model-downloads page and /api/pull planning response.", "Pull planning is safe and explicit; it does not download binaries by itself."),
                          new("Settings", "Ready", "Expose generated runtime settings for base URI, default model, context, GPU layers, and pull policy.", "Persist real settings through EF/SQLite only after user approval."),
                          new("DevExpress UI", "Ready", "Use grids/forms for model inventory, compatibility notes, settings, downloads, and endpoint tests.", "This is the realistic Blazor/DevExpress value of the experiment.")
                  """
               : """
                          new("AI Council Request", "Ready", "Capture the feature idea, council consensus, and implementation poll result.", "This mirrors the LocalGPT workflow for user-approved feature development."),
                          new("Knowledge Grounding", "SourceBacked", "Read verified council knowledge before generating code.", "Use SQLite knowledge entries instead of bloated prompt context."),
                          new("Blazor/DevExpress UI", "Ready", "Generate real routable Razor pages with navigation, grids, forms, and review notes.", "Do not return string-builder fake pages for frontend work."),
                          new("Artifact Pipeline", "Ready", "Produce downloadable .razor, .cs, .dll, and solution zip artifacts.", "Artifacts stay sandboxed until Michi approves integration."),
                          new("Integration Review", "Required", "Build, inspect, and review generated code before copying into LocalGPT or TacosPortalOpen.", "Generated solutions are prototypes, not automatic self-expansion.")
                  """;

                var endpoints = isAiHostLab
                    ? """
                          new("GET", "/api/version", "Return a compact provider-compatible version document.", "Safe pure .NET response."),
                          new("GET", "/api/tags", "Return model catalog rows shaped like local AI host tags.", "Catalog includes direct local model-file candidates."),
                          new("GET", "/api/ps", "Return currently loaded model rows for runner-status UI.", "Runner-owned sessions are reported here when configured."),
                          new("POST", "/api/show", "Return model metadata, parameters, template, and details.", "Source-shaped but generated data only."),
                          new("POST", "/api/pull", "Return a safe model-download plan.", "Does not download model binaries without a real adapter."),
                          new("POST", "/api/push", "Return a registry-upload plan.", "No registry credentials or upload path included."),
                          new("POST", "/api/create", "Return a model-create plan.", "No Modelfile build happens in this sandbox."),
                          new("POST", "/api/copy", "Return a model-copy plan.", "No local blob mutation happens."),
                          new("DELETE", "/api/delete", "Return a model-delete plan.", "No file deletion happens."),
                          new("POST", "/api/generate", "Run prompt generation through the native model-file runner contract.", "No upstream AI-host proxying is allowed."),
                          new("POST", "/api/chat", "Run chat requests through the native model-file runner contract.", "Context/cache ownership belongs to this host and its runner adapter."),
                          new("POST", "/api/embed", "Return a tiny deterministic vector.", "Not a real embedding model.")
                  """
                    : """
                          new("1", "Backend service", "Create the durable service and data model first.", "Build and test before UI integration."),
                          new("2", "Blazor page", "Add a routable Razor page with DevExpress controls and navigation.", "Review against LocalGPT/TacosPortalOpen patterns."),
                          new("3", "SQLite knowledge", "Persist decisions, logs, and generated artifacts as approved or unverified.", "User approval decides trust state."),
                          new("4", "Artifact download", "Expose generated files through safe HTTP download routes.", "No binary blobs inside chat messages."),
                          new("5", "Frontend smoke", "Exercise the generated workflow like a user in WebView2.", "Do not rely only on backend APIs.")
                  """;
                var serviceInterfaces = isAiHostLab
                    ? " : IModelCatalogService, IModelTransferService"
                    : string.Empty;

                return $$"""
            using {{projectName}}.Models;

            namespace {{projectName}}.Services;

            /// <summary>
            /// Provides deterministic health cards for the generated LocalGPT sandbox solution.
            /// </summary>
            public sealed class GeneratedHealthSummaryService{{serviceInterfaces}}
            {
                /// <summary>
                /// Returns the cards displayed by the generated DevExpress grids.
                /// </summary>
                public IReadOnlyList<GeneratedHealthCard> GetCards()
                {
                    return
                    [
                {{cards}}
                    ];
                }

                /// <summary>
                /// Returns sample model entries for compatibility UI and API responses.
                /// </summary>
                public IReadOnlyList<GeneratedModelCard> GetModelCatalog()
                {
                    return
                    [
                        new("gpt-oss:20b", "Local model-file candidate resolved from configured search roots.", 0, true),
                        new("qwen3-coder:30b", "Local model-file candidate resolved from configured search roots.", 0, true),
                        new("deepseek-r1:8b", "Local model-file candidate resolved from configured search roots.", 0, true),
                        new("native-runner:configured", "Requires NativeRunnerExecutable plus compatible local model files.", 0, true)
                    ];
                }

                /// <summary>
                /// Returns AI-host-compatible local model rows for the /api/tags route.
                /// </summary>
                public IReadOnlyList<GeneratedAiHostModelTag> GetAiHostTags()
                {
                    return
                    [
                        new("gpt-oss:20b", "gpt-oss", "20B", "Q4_K_M", 0),
                        new("qwen3-coder:30b", "qwen", "30B", "Q4_K_M", 0),
                        new("deepseek-r1:8b", "deepseek", "8B", "Q4_K_M", 0)
                    ];
                }

                /// <summary>
                /// Returns the model rows shown as currently loaded by the generated /api/ps route.
                /// </summary>
                public IReadOnlyList<GeneratedAiHostModelTag> GetRunningModels()
                {
                    return [];
                }

                /// <summary>
                /// Returns visible runtime log rows for the generated AI host control-plane logs page.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetRuntimeLogRows()
                {
                    return
                    [
                        new("Info", "Runner", "Native model-file runner is the first-class inference boundary.", "Configure NativeRunnerExecutable and a compatible local model file before long tests."),
                        new("Info", "Downloads", "Pull requests currently create safe plans and progress shapes.", "Attach IModelTransferService before downloading model binaries."),
                        new("Warning", "Hardware", "Generated lab does not own GPU scheduling or VRAM planning.", "Implement IHardwareBudgetService before heavy local runs."),
                        new("Info", "Templates", "Harmony/thinking parsing belongs in IChatTemplateService.", "Keep model formatting adaptive per model.")
                    ];
                }

                /// <summary>
                /// Returns model metadata shaped like a provider-compatible show route.
                /// </summary>
                public object GetModelDetails(GeneratedModelActionRequest request)
                {
                    var model = NormalizeModel(request.Model);
                    return new
                    {
                        license = "Generated LocalGPT lab metadata. Needs verification before production use.",
                        modelfile = $"FROM {model}\nPARAMETER num_ctx 2048",
                        parameters = "num_ctx 2048\nnum_predict 512",
                        template = "{" + "{ .Prompt }" + "}",
                        details = new GeneratedAiHostModelDetails("gguf", "generated", "0B", "none"),
                        model_info = new
                        {
                            architecture = "generated-dotnet-control-plane",
                            native_runner_attached = false
                        }
                    };
                }

                /// <summary>
                /// Returns routes, steps, or boundaries shown by the generated detail page.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetEndpointCatalog()
                {
                    return
                    [
                {{endpoints}}
                    ];
                }

                /// <summary>
                /// Returns candidate model downloads displayed on the generated download page.
                /// </summary>
                public IReadOnlyList<GeneratedModelDownloadCandidate> GetDownloadCandidates()
                {
                    return
                    [
                        new("gpt-oss:20b", "Local provider", "http://localhost:11434", "LocalGPT debugging and balanced reasoning", "/api/pull", "Pull only when GPU/VRAM policy allows it."),
                        new("gemma3:27b", "Local provider", "http://localhost:11434", "Longer general review and writing", "/api/pull", "Use one model at a time on 24 GB VRAM."),
                        new("qwen3-coder:30b", "Local provider", "http://localhost:11434", "Code review and larger code-generation tests", "/api/pull", "Prefer CPU or reduced GPU layers after driver instability."),
                        new("deepseek-r1:8b", "Local provider", "http://localhost:11434", "Small reasoning checks", "/api/pull", "May spend short budgets on thinking."),
                        new("hf://models", "HuggingFace catalog", "https://huggingface.co/models", "Browse user-selected model cards and map compatible files into an approved download plan.", "/api/pull", "Never auto-download. Ask the user to approve license, file size, quantization, and target path."),
                        new("github://model-releases", "GitHub Releases", "https://github.com", "Represent model or runner release URLs selected by the user.", "/api/pull", "Only download from explicit user-selected release assets.")
                    ];
                }

                /// <summary>
                /// Returns chat template and thinking-format rows that a real local runner adapter would use.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetTemplateRows()
                {
                    return
                    [
                        new("Harmony", "model family metadata", "Parse final/user-visible text separately from hidden thinking markers.", "Show visible thinking summaries only when policy and model output permit it."),
                        new("ChatML", "template field", "Adapt role markers and stop sequences per model.", "Never assume all models share one prompt format."),
                        new("OpenAI-compatible", "/v1/chat/completions", "Map local provider output to common client contracts.", "Keep provider-specific options behind typed adapter settings."),
                        new("Plain prompt", "/api/generate", "Support simple generation requests for scripting and smoke tests.", "Warn when a chat request is being downgraded to plain prompt completion.")
                    ];
                }

                /// <summary>
                /// Returns hardware and scheduling rows for a safe generated control-plane prototype.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetHardwareBudgetRows()
                {
                    return
                    [
                        new("GPU", "80-90% target", "Throttle heavy runs and avoid sustained full-load peaks.", "Driver stability is a user-facing reliability requirement."),
                        new("VRAM", "24 GB class", "Queue large models one at a time unless profiling proves a safe combination.", "Council runs should favor sequential turns over concurrent large models."),
                        new("Context", "model/profile-based", "Expose context and output token budgets in settings.", "Huge defaults can stall local hardware; make presets explicit."),
                        new("Downloads", "user approved", "Require explicit user selection for HuggingFace/GitHub/provider downloads.", "Catalog browsing is not permission to mutate the machine.")
                    ];
                }

                /// <summary>
                /// Returns generated runtime settings for the settings page.
                /// </summary>
                public GeneratedAiHostSettings GetSettings()
                {
                    return new GeneratedAiHostSettings
                    {
                        BaseUri = "native local model files",
                        DefaultModel = "gpt-oss:20b",
                        KeepAlive = "0s",
                        ContextTokens = 2048,
                        GpuLayers = 20,
                        NativeRunnerAttached = true,
                        AllowPullPlanning = true
                    };
                }

                /// <summary>
                /// Builds the settings summary shown in the generated settings page.
                /// </summary>
                public string BuildSettingsSummary()
                {
                    var settings = GetSettings();
                    return $"Model source: {settings.BaseUri}\nDefault model: {settings.DefaultModel}\n" +
                        $"Context tokens: {settings.ContextTokens}\nGPU layers: {settings.GpuLayers}\n" +
                        "Native runner execution uses configured local model files and never proxies to an upstream AI host.";
                }

                /// <summary>
                /// Creates a visible chat transcript for the generated chat page through the local runner contract.
                /// </summary>
                public string CreateChatTranscript(string model, string prompt)
                {
                    var request = new GeneratedChatRequest
                    {
                        Model = NormalizeModel(model),
                        Messages =
                        [
                            new GeneratedChatMessage("user", string.IsNullOrWhiteSpace(prompt) ? "Hello" : prompt)
                        ]
                    };
                    var response = CreateChatResponse(request);
                    return $"POST /api/chat\nModel: {request.Model}\nUser: {request.Messages[0].Content}\nAssistant: {response.Message.Content}\nDone: {response.Done}";
                }

                /// <summary>
                /// Creates a safe model-pull plan without downloading model files.
                /// </summary>
                public GeneratedAiHostOperation CreatePullPlan(GeneratedModelActionRequest request)
                {
                    return new GeneratedAiHostOperation(
                        "planned",
                        NormalizeModel(request.Model),
                        "/api/pull",
                        true,
                        "This response mirrors provider pull progress shape but does not download model binaries.");
                }

                /// <summary>
                /// Creates a safe non-mutating operation response for registry and model-management routes.
                /// </summary>
                public GeneratedAiHostOperation CreateOperation(string operation, string? model)
                {
                    return new GeneratedAiHostOperation(
                        "planned",
                        NormalizeModel(model),
                        $"/api/{operation}",
                        true,
                        $"The generated lab records a {operation} plan but does not mutate model storage.");
                }

                /// <summary>
                /// Creates a safe copy plan for the /api/copy route.
                /// </summary>
                public GeneratedAiHostOperation CreateCopyPlan(GeneratedModelCopyRequest request)
                {
                    var from = NormalizeModel(request.Source);
                    var to = NormalizeModel(request.Destination);
                    return new GeneratedAiHostOperation(
                        "planned",
                        $"{from} -> {to}",
                        "/api/copy",
                        true,
                        "The generated lab records copy intent but does not mutate model storage.");
                }

                /// <summary>
                /// Creates a deterministic non-inference generate response.
                /// </summary>
                public object CreateGenerateResponse(GeneratedModelActionRequest request)
                {
                    return new
                    {
                        model = NormalizeModel(request.Model),
                        created_at = DateTimeOffset.UtcNow,
                        response = "NativeRunnerExecutable must point to an approved local runner before model-file inference starts. No upstream proxy fallback is used.",
                        done = true
                    };
                }

                /// <summary>
                /// Creates a deterministic non-inference chat response.
                /// </summary>
                public GeneratedChatResponse CreateChatResponse(GeneratedChatRequest request)
                {
                    return new GeneratedChatResponse(
                        NormalizeModel(request.Model),
                        DateTimeOffset.UtcNow,
                        new GeneratedChatMessage("assistant", "Generated lab response only. No native AI runner is attached."),
                        true);
                }

                /// <summary>
                /// Creates a deterministic tiny embedding response for plumbing tests.
                /// </summary>
                public object CreateEmbeddingResponse(GeneratedModelActionRequest request)
                {
                    return new
                    {
                        model = NormalizeModel(request.Model),
                        embeddings = new[] { new[] { 0.0, 0.25, 0.5, 0.75 } },
                        done = true
                    };
                }

                public string NormalizeModel(string? model)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? "gpt-oss:20b"
                        : model.Trim();
                }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionService projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
           
        }

        /// <summary>
        /// Runs the generate source fidelity service operation.
        /// </summary>
        public string GenerateSourceFidelityService(string projectName, GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                var rows = archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt => """
                        new(
                            "DXAiChat workbench",
                            "Original LocalGPT centers user work in DXAiChat with model selection, council mode, uploads, memory, visible progress, and artifact links.",
                            "Generated Chat page plus backend service boundaries for model routing, file context, Harmony/thinking display, and downloadable artifacts.",
                            "Represented",
                            "Components/Pages/Chat.razor, Source Fidelity page, and artifact contract docs."),
                        new(
                            "AI Council",
                            "Original LocalGPT supports multi-model council talks, polls, missing-feature logs, and user-approved implementation artifacts.",
                            "Generated Model Council page with minimum-member, poll-gate, feedback-log, and artifact-delivery requirements.",
                            "Represented",
                            "Components/Pages/ModelCouncil.razor and SOURCE_FIDELITY.md."),
                        new(
                            "SQLite memory and knowledge",
                            "Original LocalGPT persists chats, thoughts, logs, knowledge, approvals, and benchmark feedback in SQLite.",
                            "Generated Database page and source-fidelity rows state EF/SQLite as durable state boundary.",
                            "Boundary",
                            "Components/Pages/Database.razor; real DbContext integration must be added when moving beyond sandbox."),
                        new(
                            "Minecraft builder",
                            "Original LocalGPT can generate datapacks and loader skeletons through backend artifact routes.",
                            "Generated Minecraft Mod Builder page represents datapack, loader matrix, version resolver, validation, and downloads.",
                            "Represented",
                            "Components/Pages/MinecraftModBuilder.razor."),
                        new(
                            "Install and diagnostics",
                            "Original LocalGPT detects Ollama/LM Studio/runtime setup and exposes frontend-facing test routes.",
                            "Generated Install and Test Lab pages require local host status, runtime checks, and route smoke tests.",
                            "Represented",
                            "Components/Pages/Install.razor and Components/Pages/TestLab.razor.")
                """,
                    GeneratedSolutionArchetype.TacosPortal => """
                        new(
                            "Multi-host topology",
                            "Original TacosPortalOpen is a multi-project .NET/Blazor system with core library, server host, WASM/client option, and WinUI/WebView2 wrapper boundaries.",
                            "Generated Client Shells page and source docs require server, WASM, WebView2, packaging, and debug/deploy boundaries.",
                            "Represented",
                            "Components/Pages/ClientShells.razor and SOURCE_FIDELITY.md."),
                        new(
                            "Telegram/event ingestion",
                            "Original TacosPortalOpen uses Telegram-style event ingestion flowing through handlers, service/API layers, persistence, workers, and UI.",
                            "Generated Telegram Ingestion page models update handling, command routing, idempotency, and retry queues.",
                            "Represented",
                            "Components/Pages/TelegramIngestion.razor."),
                        new(
                            "Normalized persistence",
                            "Original TacosPortalOpen separates domain/business objects, persistence, DTO/service boundaries, and migration safety.",
                            "Generated Persistence page requires business objects, DbContext boundaries, DTOs, and safe migrations.",
                            "Boundary",
                            "Components/Pages/Persistence.razor; real entities/migrations are a follow-up for the selected target database."),
                        new(
                            "Workers and notifications",
                            "Original TacosPortalOpen includes polling/background services, notifications, logs, and integration diagnostics.",
                            "Generated Workers page models hosted services, polling, notification dispatch, and diagnostics.",
                            "Represented",
                            "Components/Pages/Workers.razor."),
                        new(
                            "DevExpress admin/security",
                            "Original TacosPortalOpen uses DevExpress/XAF-adjacent admin, role/security, audit, validation, and CRUD forms.",
                            "Generated Admin page requires users, roles, audit, validation, and settings through DevExpress controls.",
                            "Represented",
                            "Components/Pages/Admin.razor.")
                """,
                    GeneratedSolutionArchetype.AiHost => """
                        new(
                            "Provider-compatible routes",
                            "AI-host-shaped requests need /api/version, /api/tags, /api/ps, /api/chat, /api/generate, embeddings, and OpenAI-compatible routes.",
                            "Generated Program.cs maps route endpoints through provider/catalog/runner service contracts.",
                            "Represented",
                            "Program.cs and Services/GeneratedAiHostArchitectureServices.cs."),
                        new(
                            "Native runner boundary",
                            "A real AI host needs native model loading, tokenizer/template handling, GPU scheduling, blobs, and runner lifecycle.",
                            "Generated runner/plugin pages expose the native model-file runner and configuration readiness.",
                            "Represented",
                            "Components/Pages/RunnerPlugins.razor and IInferenceRunner."),
                        new(
                            "Model catalog and downloads",
                            "AI host UX needs model inventory, running models, download/pull planning, settings, hardware, and logs.",
                            "Generated pages cover catalog, downloads, running models, templates, hardware, logs, and settings.",
                            "Represented",
                            "Components/Pages/ModelDownloads.razor, RunningModels.razor, Hardware.razor, Logs.razor, Settings.razor."),
                        new(
                            "Adapter architecture",
                            "External hosts, HuggingFace downloads, Python.NET, PowerShell, optional TypeScript client/script assets, and plugins should sit behind explicit interfaces.",
                            "Generated service file declares provider, runner, plugin, script, hardware, and template interfaces.",
                            "Represented",
                            "Services/GeneratedAiHostArchitectureServices.cs.")
                """,
                    GeneratedSolutionArchetype.BotBackend => """
                        new(
                            "Webhook ingress",
                            "Bot backend requests need signed/idempotent event intake and retry/dead-letter diagnostics.",
                            "Generated Webhooks page models ingress, signature check, idempotency, and dead letters.",
                            "Represented",
                            "Components/Pages/Webhooks.razor."),
                        new(
                            "Conversation state",
                            "Bot systems need persisted conversation memory, moderation, transcript review, and handoff.",
                            "Generated Conversations page models memory, moderation, handoff, and transcript work.",
                            "Boundary",
                            "Components/Pages/Conversations.razor; real EF/SQLite implementation must be added for production."),
                        new(
                            "Optional Python interop",
                            "Legacy examples show Python.NET/process adapters for speech, translation, or automation helpers.",
                            "Generated Python Interop page keeps this permission-gated and backend-owned.",
                            "Represented",
                            "Components/Pages/PythonInterop.razor.")
                """,
                    _ => """
                        new(
                            "Generated sandbox",
                            "User requested a downloadable .NET/Blazor/DevExpress artifact.",
                            "Generated files include navigation, pages, service/model code, docs, and contract JSON.",
                            "Represented",
                            "PROJECT_INDEX.md and .localgpt-generation.json.")
                """
                };

                return $$"""
            namespace {{projectName}}.Services;

            /// <summary>
            /// Describes whether the generated sandbox preserves the requested source architecture.
            /// </summary>
            public interface ISourceFidelityService
            {
                /// <summary>
                /// Returns source-fidelity requirements for review and benchmark scoring.
                /// </summary>
                IReadOnlyList<GeneratedSourceFidelityRequirement> GetRequirements();
            }

            /// <summary>
            /// Deterministic source-fidelity service generated by LocalGPT.
            /// </summary>
            public sealed class GeneratedSourceFidelityService : ISourceFidelityService
            {
                /// <inheritdoc />
                public IReadOnlyList<GeneratedSourceFidelityRequirement> GetRequirements()
                {
                    return
                    [
                {{rows}}
                    ];
                }
            }

            /// <summary>
            /// One source-fidelity requirement represented by this generated sandbox.
            /// </summary>
            public sealed record GeneratedSourceFidelityRequirement(
                string Area,
                string SourceSignal,
                string GeneratedBoundary,
                string Status,
                string Evidence);
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSourceFidelityService projectName:{projectName} LocalGptCatalogService:{archetype.ToString()}", archetype);
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate ai host architecture services operation.
        /// </summary>
        public string GenerateAiHostArchitectureServices(string projectName,ILogger logger) {
            try
            {
                return $$"""
            using System.Diagnostics;
            using System.Globalization;
            using System.Text;
            using System.Text.Json;
            using Microsoft.Extensions.Options;
            using {{projectName}}.Models;

            #pragma warning disable CS1591 // Generated sandbox contracts are documented in ARCHITECTURE.md and BUILD_AND_RUN.md.

            namespace {{projectName}}.Services;

            /// <summary>
            /// Typed bootstrap settings for a generated provider-compatible AI host control plane.
            /// Persist user-edited runtime values in SQLite when this lab becomes a real app.
            /// </summary>
            public sealed class AiHostRuntimeOptions
            {
                public string DefaultModel { get; set; } = "gpt-oss:20b";
                public string SafeStorageRoot { get; set; } = "%LOCALAPPDATA%/GeneratedAiHost";
                public string PluginRoot { get; set; } = "plugins";
                public string? PythonDll { get; set; }
                public string NativeRunnerExecutable { get; set; } = string.Empty;
                public List<string> ModelSearchRoots { get; set; } = new();
                public Dictionary<string, string> ModelFileOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
                public int ContextTokens { get; set; } = 2048;
                public int GpuLayers { get; set; } = 20;
                public bool AllowNativeRunner { get; set; } = false;
                public bool AllowPythonNet { get; set; }
                public bool AllowPowerShellScripts { get; set; }
                public bool AllowTypeScriptAdapters { get; set; }
            }

            public interface IModelCatalogService
            {
                IReadOnlyList<GeneratedAiHostModelTag> GetAiHostTags();
                IReadOnlyList<GeneratedAiHostModelTag> GetRunningModels();
                object GetModelDetails(GeneratedModelActionRequest request);
            }

            public interface IModelTransferService
            {
                GeneratedAiHostOperation CreatePullPlan(GeneratedModelActionRequest request);
            }

            public interface IInferenceProvider
            {
                string ProviderKind { get; }
                Task<GeneratedChatResponse> ChatAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default);
                Task<object> GenerateAsync(GeneratedModelActionRequest request, CancellationToken cancellationToken = default);
            }

            public interface IInferenceRunner
            {
                string RunnerKind { get; }
                Task<RunnerCapabilityReport> GetCapabilityAsync(CancellationToken cancellationToken = default);
                Task<GeneratedChatResponse> InferAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default);
            }

            public interface IPluginCatalogService
            {
                IReadOnlyList<AiHostPluginManifest> GetPlugins();
            }

            public interface IScriptExecutionService
            {
                ScriptExecutionPlan CreatePlan(string scriptKind, string target, bool userApproved);
            }

            public interface IHardwareBudgetService
            {
                HardwareBudgetSnapshot GetBudget();
            }

            public interface IChatTemplateService
            {
                IReadOnlyList<ChatTemplateRule> GetTemplateRules();
            }

            /// <summary>
            /// Routes provider-compatible requests to the generated host's own local-file runner.
            /// This class intentionally does not call an upstream Ollama/LM Studio/OpenAI endpoint.
            /// </summary>
            public sealed class NativeModelFileInferenceProvider(
                IInferenceRunner runner,
                IOptions<AiHostRuntimeOptions> options) : IInferenceProvider
            {
                public string ProviderKind => "Native local-model-file provider";

                public async Task<GeneratedChatResponse> ChatAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default)
                {
                    request.Model = NormalizeModel(request.Model, options.Value.DefaultModel);
                    return await runner.InferAsync(request, cancellationToken).ConfigureAwait(false);
                }

                public async Task<object> GenerateAsync(GeneratedModelActionRequest request, CancellationToken cancellationToken = default)
                {
                    var prompt = string.IsNullOrWhiteSpace(request.Prompt)
                        ? "LocalGPT generated AI host native-runner smoke test."
                        : request.Prompt;
                    var chat = new GeneratedChatRequest
                    {
                        Model = NormalizeModel(request.Model, options.Value.DefaultModel),
                        Messages = new List<GeneratedChatMessage>
                        {
                            new("user", prompt)
                        },
                        Stream = request.Stream,
                        Options = request.Options
                    };
                    var response = await runner.InferAsync(chat, cancellationToken).ConfigureAwait(false);
                    return new
                    {
                        model = response.Model,
                        created_at = response.CreatedAt,
                        response = response.Message.Content,
                        done = response.Done,
                        upstream_proxy = false
                    };
                }

                public string NormalizeModel(string? model, string fallbackModel)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? fallbackModel
                        : model.Trim();
                }
            }

            /// <summary>
            /// Loads compatible local model files through an approved native executable.
            /// Ollama manifests may be read as local file metadata, but the Ollama service is never called.
            /// </summary>
            public sealed class NativeModelFileProcessRunner(IOptions<AiHostRuntimeOptions> options) : IInferenceRunner
            {
                public string RunnerKind => "Native model-file process runner";

                public Task<RunnerCapabilityReport> GetCapabilityAsync(CancellationToken cancellationToken = default)
                {
                    var executable = ExpandPath(options.Value.NativeRunnerExecutable);
                    var executableReady = options.Value.AllowNativeRunner &&
                        !string.IsNullOrWhiteSpace(executable) &&
                        File.Exists(executable);

                    return Task.FromResult(new RunnerCapabilityReport(
                        NativeInferenceImplemented: executableReady,
                        SupportedFormats: ["gguf", "ollama-managed-gguf-blob", "onnx-planned", "safetensors-planned"],
                        MissingCapability: executableReady
                            ? string.Empty
                            : "Set AiHost:NativeRunnerExecutable to an approved native runner such as llama-cli/llama-server before chat/generate can execute model files.",
                        NextMilestone: "Configure NativeRunnerExecutable and ModelSearchRoots, verify /api/localgpt/runner/capability, then point LocalGPT DXAiChat at this host URL."));
                }

                public async Task<GeneratedChatResponse> InferAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default)
                {
                    var model = NormalizeModel(request.Model, options.Value.DefaultModel);
                    if (!options.Value.AllowNativeRunner)
                        return BuildStatusResponse(model, "Native runner execution is disabled by AiHost:AllowNativeRunner. No upstream proxy fallback is used.");

                    var executable = ExpandPath(options.Value.NativeRunnerExecutable);
                    if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                        return BuildStatusResponse(model, "Native runner executable is not configured. Set AiHost:NativeRunnerExecutable to a trusted llama.cpp-compatible runner. No upstream proxy fallback is used.");

                    var modelPath = ResolveModelPath(model);
                    if (string.IsNullOrWhiteSpace(modelPath))
                        return BuildStatusResponse(model, $"Could not resolve a compatible local model file for '{model}'. Add a ModelFileOverrides entry or a .gguf file under ModelSearchRoots. No upstream proxy fallback is used.");

                    var prompt = BuildPrompt(request);
                    var arguments = BuildRunnerArguments(modelPath, prompt, request.Options);
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromMinutes(20));

                    var startInfo = new ProcessStartInfo(executable)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory
                    };

                    foreach (var argument in arguments)
                        startInfo.ArgumentList.Add(argument);

                    using var process = Process.Start(startInfo);
                    if (process is null)
                        return BuildStatusResponse(model, "The native runner process could not be started.");

                    try
                    {
                        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
                        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
                        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                        var output = await outputTask.ConfigureAwait(false);
                        var error = await errorTask.ConfigureAwait(false);
                        var visible = string.IsNullOrWhiteSpace(output)
                            ? $"Native runner exited with code {process.ExitCode}. {error}".Trim()
                            : output.Trim();

                        return new GeneratedChatResponse(
                            model,
                            DateTimeOffset.UtcNow,
                            new GeneratedChatMessage("assistant", visible),
                            true);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        TryKill(process);
                        return BuildStatusResponse(model, "Native runner timed out after 20 minutes. Reduce context/output tokens or use a smaller model.");
                    }
                }

                private string? ResolveModelPath(string model)
                {
                    if (options.Value.ModelFileOverrides.TryGetValue(model, out var configuredPath))
                    {
                        var expanded = ExpandPath(configuredPath);
                        if (File.Exists(expanded))
                            return expanded;
                    }

                    foreach (var root in options.Value.ModelSearchRoots.Select(ExpandPath).Where(Directory.Exists))
                    {
                        var direct = ResolveDirectGguf(root, model);
                        if (!string.IsNullOrWhiteSpace(direct))
                            return direct;

                        var managed = ResolveOllamaManagedBlob(root, model);
                        if (!string.IsNullOrWhiteSpace(managed))
                            return managed;
                    }

                    return null;
                }

                public string? ResolveDirectGguf(string root, string model)
                {
                    var sanitized = model.Replace(':', '-').Replace('/', '-').Replace('\\', '-');
                    foreach (var candidate in new[]
                    {
                        Path.Combine(root, $"{model}.gguf"),
                        Path.Combine(root, $"{sanitized}.gguf"),
                        Path.Combine(root, model, $"{sanitized}.gguf")
                    })
                    {
                        if (File.Exists(candidate))
                            return candidate;
                    }

                    try
                    {
                        return Directory.EnumerateFiles(root, "*.gguf", SearchOption.AllDirectories)
                            .Take(2000)
                            .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Contains(sanitized, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return null;
                    }
                }

                public string? ResolveOllamaManagedBlob(string root, string model)
                {
                    var (name, tag) = SplitModelName(model);
                    var manifest = Path.Combine(root, "manifests", "registry.ollama.ai", "library", name, tag);
                    if (!File.Exists(manifest) && Directory.Exists(Path.Combine(root, "manifests")))
                    {
                        manifest = Directory.EnumerateFiles(Path.Combine(root, "manifests"), tag, SearchOption.AllDirectories)
                            .FirstOrDefault(path => Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase))
                            ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(manifest) || !File.Exists(manifest))
                        return null;

                    try
                    {
                        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
                        if (!document.RootElement.TryGetProperty("layers", out var layers) || layers.ValueKind != JsonValueKind.Array)
                            return null;

                        return layers
                            .EnumerateArray()
                            .Select(layer => layer.TryGetProperty("digest", out var digest) ? digest.GetString() : null)
                            .Where(digest => !string.IsNullOrWhiteSpace(digest))
                            .Select(digest => Path.Combine(root, "blobs", digest!.Replace(':', '-')))
                            .FirstOrDefault(File.Exists);
                    }
                    catch
                    {
                        return null;
                    }
                }

                public IReadOnlyList<string> BuildRunnerArguments(string modelPath, string prompt, GeneratedRequestOptions? requestOptions)
                {
                    var ctx = Math.Clamp(requestOptions?.NumCtx ?? 2048, 256, 262144);
                    var predict = Math.Clamp(requestOptions?.NumPredict ?? 1024, 1, 262144);
                    var gpuLayers = Math.Clamp(requestOptions?.NumGpu ?? 0, 0, 999);
                    var args = new List<string>
                    {
                        "--model", modelPath,
                        "--prompt", prompt,
                        "--ctx-size", ctx.ToString(CultureInfo.InvariantCulture),
                        "--n-predict", predict.ToString(CultureInfo.InvariantCulture),
                        "--gpu-layers", gpuLayers.ToString(CultureInfo.InvariantCulture)
                    };

                    if (requestOptions?.Temperature is { } temperature)
                    {
                        args.Add("--temp");
                        args.Add(temperature.ToString(CultureInfo.InvariantCulture));
                    }

                    return args;
                }

                public string BuildPrompt(GeneratedChatRequest request)
                {
                    var builder = new StringBuilder();
                    foreach (var message in request.Messages.Where(message => !string.IsNullOrWhiteSpace(message.Content)))
                        builder.Append(message.Role ?? "user").Append(": ").AppendLine(message.Content);
                    if (builder.Length == 0)
                        builder.AppendLine("user: Hello");
                    builder.Append("assistant: ");
                    return builder.ToString();
                }

                public (string Name, string Tag) SplitModelName(string model)
                {
                    var parts = model.Split(':', 2, StringSplitOptions.TrimEntries);
                    return (parts[0], parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "latest");
                }

                public string NormalizeModel(string? model, string fallbackModel)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? fallbackModel
                        : model.Trim();
                }

                public string ExpandPath(string? path)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return string.Empty;

                    var expanded = path
                        .Replace("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase)
                        .Replace("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase);

                    return expanded.StartsWith("~/", StringComparison.Ordinal) || expanded.StartsWith("~\\", StringComparison.Ordinal)
                        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), expanded[2..])
                        : expanded;
                }

                public GeneratedChatResponse BuildStatusResponse(string model, string message)
                {
                    return new GeneratedChatResponse(
                        model,
                        DateTimeOffset.UtcNow,
                        new GeneratedChatMessage("assistant", message),
                        true);
                }

                public void TryKill(Process process)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Best effort only; the host should keep serving requests.
                    }
                }
            }

            public sealed class GeneratedPluginCatalogService : IPluginCatalogService
            {
                public IReadOnlyList<AiHostPluginManifest> GetPlugins()
                {
                    return
                    [
                        new("native-process-runner", "Native Process Runner", "1.0.0", "IInferenceRunner", true, "Loads compatible local model files through an approved executable; no upstream AI-host proxying."),
                        new("pythonnet-runner", "Python.NET Runner Boundary", "planned", "IInferenceRunner", false, "Requires approved Python runtime, PYTHONNET_PYDLL, package list, and GIL-safe service code."),
                        new("powershell-runner", "PowerShell Script Boundary", "planned", "IScriptExecutionService", false, "Requires explicit script files, safe directories, constrained runspace policy, and user approval."),
                        new("typescript-client-adapter", "TypeScript Client/Adapter Boundary", "planned", "ASP.NET Core static asset or script adapter", false, "Allowed only when embedded deliberately inside the .NET app as client assets or an approved script layer, not as the control-plane owner."),
                        new("onnx-runtime-runner", "ONNX Runtime Runner Boundary", "planned", "IInferenceRunner", false, "Only for compatible ONNX models; not a universal LLM replacement.")
                    ];
                }
            }

            public sealed class PermissionGatedScriptExecutionService(IOptions<AiHostRuntimeOptions> options) : IScriptExecutionService
            {
                public ScriptExecutionPlan CreatePlan(string scriptKind, string target, bool userApproved)
                {
                    var allowed = userApproved && (scriptKind.Equals("powershell", StringComparison.OrdinalIgnoreCase)
                        ? options.Value.AllowPowerShellScripts
                        : options.Value.AllowPythonNet);

                    return new ScriptExecutionPlan(
                        scriptKind,
                        target,
                        allowed,
                        allowed
                            ? "Approved script boundary. A real implementation must execute in a safe working directory with logs and cancellation."
                            : "Not approved. The generated host must not execute scripts until the user enables this path.");
                }
            }

            public sealed class GeneratedHardwareBudgetService(IOptions<AiHostRuntimeOptions> options) : IHardwareBudgetService
            {
                public HardwareBudgetSnapshot GetBudget()
                {
                    return new HardwareBudgetSnapshot(
                        TargetGpuLoadPercent: 85,
                        GpuLayers: options.Value.GpuLayers,
                        ContextTokens: options.Value.ContextTokens,
                        MaxParallelModels: 1,
                        Notes: "Sequential local-model runs are the default until profiling proves heavier concurrency is stable.");
                }
            }

            public sealed class GeneratedChatTemplateService : IChatTemplateService
            {
                public IReadOnlyList<ChatTemplateRule> GetTemplateRules()
                {
                    return
                    [
                        new("Harmony", "Separate analysis/commentary/final markers and always surface final visible text."),
                        new("ChatML", "Map role markers, stop sequences, and system/user/assistant boundaries per model."),
                        new("Plain prompt", "Use only for /api/generate style requests, not multi-turn chat without conversion."),
                        new("Tools", "Keep tool schemas typed and require user approval before native commands or downloads.")
                    ];
                }
            }

            public sealed record RunnerCapabilityReport(
                bool NativeInferenceImplemented,
                IReadOnlyList<string> SupportedFormats,
                string MissingCapability,
                string NextMilestone);

            public sealed record AiHostPluginManifest(
                string Id,
                string DisplayName,
                string Version,
                string Contract,
                bool Approved,
                string Notes);

            public sealed record ScriptExecutionPlan(
                string ScriptKind,
                string Target,
                bool AllowedToRun,
                string SafetyNote);

            public sealed record GeneratedScriptPlanRequest(
                string ScriptKind,
                string Target,
                bool UserApproved);

            public sealed record HardwareBudgetSnapshot(
                int TargetGpuLoadPercent,
                int GpuLayers,
                int ContextTokens,
                int MaxParallelModels,
                string Notes);

            public sealed record ChatTemplateRule(string Name, string Rule);
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateAiHostArchitectureServices projectName:{projectName}");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the generate solution model operation.
        /// </summary>
        public string GenerateSolutionModel(string projectName, ILogger logger)
        {
            try
            {
                return $$"""
            using System.Text.Json.Serialization;

            namespace {{projectName}}.Models;

            /// <summary>
            /// Describes one status card rendered by the generated LocalGPT workbench.
            /// </summary>
            public sealed class GeneratedHealthCard
            {
                /// <summary>
                /// Creates a generated health card.
                /// </summary>
                public GeneratedHealthCard(string area, string status, string nextAction, string detail)
                {
                    Area = area;
                    Status = status;
                    NextAction = nextAction;
                    Detail = detail;
                }

                /// <summary>
                /// Gets the subsystem or concern represented by the card.
                /// </summary>
                public string Area { get; }

                /// <summary>
                /// Gets the current generated status.
                /// </summary>
                public string Status { get; }

                /// <summary>
                /// Gets the next suggested action.
                /// </summary>
                public string NextAction { get; }

                /// <summary>
                /// Gets the implementation detail shown in expanded views.
                /// </summary>
                public string Detail { get; }
            }

            /// <summary>
            /// Describes one model or adapter row in the generated AI host control-plane lab.
            /// </summary>
            public sealed class GeneratedModelCard
            {
                /// <summary>
                /// Creates a generated model compatibility row.
                /// </summary>
                public GeneratedModelCard(string name, string status, long sizeMegabytes, bool supportsNativeInference)
                {
                    Name = name;
                    Status = status;
                    SizeMegabytes = sizeMegabytes;
                    SupportsNativeInference = supportsNativeInference;
                }

                /// <summary>
                /// Gets the model, adapter, or backend name.
                /// </summary>
                public string Name { get; }

                /// <summary>
                /// Gets the current compatibility status.
                /// </summary>
                public string Status { get; }

                /// <summary>
                /// Gets the sample size in megabytes, or zero when the lab does not own a model binary.
                /// </summary>
                public long SizeMegabytes { get; }

                /// <summary>
                /// Gets whether this row represents a real native inference path.
                /// </summary>
                public bool SupportsNativeInference { get; }
            }

            /// <summary>
            /// Describes an API endpoint or implementation step in the generated solution.
            /// </summary>
            public sealed class GeneratedEndpointCard
            {
                /// <summary>
                /// Creates a generated endpoint or workflow row.
                /// </summary>
                public GeneratedEndpointCard(string method, string route, string purpose, string boundary)
                {
                    Method = method;
                    Route = route;
                    Purpose = purpose;
                    Boundary = boundary;
                }

                /// <summary>
                /// Gets the HTTP method or ordered workflow step.
                /// </summary>
                public string Method { get; }

                /// <summary>
                /// Gets the route, owner, or target area.
                /// </summary>
                public string Route { get; }

                /// <summary>
                /// Gets the row purpose.
                /// </summary>
                public string Purpose { get; }

                /// <summary>
                /// Gets the safety or implementation boundary.
                /// </summary>
                public string Boundary { get; }
            }

            /// <summary>
            /// Describes one AI-host-compatible model row returned by generated catalog routes.
            /// </summary>
            public sealed class GeneratedAiHostModelTag
            {
                /// <summary>
                /// Creates a generated AI-host-compatible model row.
                /// </summary>
                public GeneratedAiHostModelTag(
                    string name,
                    string family,
                    string parameterSize,
                    string quantizationLevel,
                    long size)
                {
                    Name = name;
                    Model = name;
                    ModifiedAt = DateTimeOffset.UtcNow;
                    Size = size;
                    Digest = $"generated-{Math.Abs(name.GetHashCode(StringComparison.Ordinal)):x}";
                    Details = new GeneratedAiHostModelDetails("gguf", family, parameterSize, quantizationLevel);
                }

                /// <summary>
                /// Gets the provider-compatible model name field.
                /// </summary>
                [JsonPropertyName("name")]
                public string Name { get; }

                /// <summary>
                /// Gets the model identifier.
                /// </summary>
                [JsonPropertyName("model")]
                public string Model { get; }

                /// <summary>
                /// Gets the generated modification timestamp.
                /// </summary>
                [JsonPropertyName("modified_at")]
                public DateTimeOffset ModifiedAt { get; }

                /// <summary>
                /// Gets the generated model size in bytes.
                /// </summary>
                [JsonPropertyName("size")]
                public long Size { get; }

                /// <summary>
                /// Gets a deterministic generated digest placeholder.
                /// </summary>
                [JsonPropertyName("digest")]
                public string Digest { get; }

                /// <summary>
                /// Gets model detail metadata.
                /// </summary>
                [JsonPropertyName("details")]
                public GeneratedAiHostModelDetails Details { get; }
            }

            /// <summary>
            /// Describes generated AI-host-compatible model details.
            /// </summary>
            public sealed class GeneratedAiHostModelDetails
            {
                /// <summary>
                /// Creates generated model details.
                /// </summary>
                public GeneratedAiHostModelDetails(
                    string format,
                    string family,
                    string parameterSize,
                    string quantizationLevel)
                {
                    Format = format;
                    Family = family;
                    Families = [family];
                    ParameterSize = parameterSize;
                    QuantizationLevel = quantizationLevel;
                }

                /// <summary>
                /// Gets the generated model file format.
                /// </summary>
                [JsonPropertyName("format")]
                public string Format { get; }

                /// <summary>
                /// Gets the generated primary model family.
                /// </summary>
                [JsonPropertyName("family")]
                public string Family { get; }

                /// <summary>
                /// Gets generated model families.
                /// </summary>
                [JsonPropertyName("families")]
                public IReadOnlyList<string> Families { get; }

                /// <summary>
                /// Gets the generated parameter size label.
                /// </summary>
                [JsonPropertyName("parameter_size")]
                public string ParameterSize { get; }

                /// <summary>
                /// Gets the generated quantization label.
                /// </summary>
                [JsonPropertyName("quantization_level")]
                public string QuantizationLevel { get; }
            }

            /// <summary>
            /// Represents a generated AI host action request.
            /// </summary>
            public sealed class GeneratedModelActionRequest
            {
                /// <summary>
                /// Gets or sets the model name.
                /// </summary>
                [JsonPropertyName("model")]
                public string? Model { get; set; }

                /// <summary>
                /// Gets or sets the prompt for generate/embed-style routes.
                /// </summary>
                [JsonPropertyName("prompt")]
                public string? Prompt { get; set; }

                /// <summary>
                /// Gets or sets whether the caller requested streaming.
                /// </summary>
                [JsonPropertyName("stream")]
                public bool Stream { get; set; }

                /// <summary>
                /// Gets or sets Ollama-compatible request options.
                /// </summary>
                [JsonPropertyName("options")]
                public GeneratedRequestOptions? Options { get; set; }
            }

            /// <summary>
            /// Represents Ollama-compatible request options accepted by the generated host.
            /// </summary>
            public sealed class GeneratedRequestOptions
            {
                /// <summary>
                /// Gets or sets the requested context token budget.
                /// </summary>
                [JsonPropertyName("num_ctx")]
                public int? NumCtx { get; set; }

                /// <summary>
                /// Gets or sets the requested output token budget.
                /// </summary>
                [JsonPropertyName("num_predict")]
                public int? NumPredict { get; set; }

                /// <summary>
                /// Gets or sets the requested GPU layer count.
                /// </summary>
                [JsonPropertyName("num_gpu")]
                public int? NumGpu { get; set; }

                /// <summary>
                /// Gets or sets the requested sampling temperature.
                /// </summary>
                [JsonPropertyName("temperature")]
                public float? Temperature { get; set; }
            }

            /// <summary>
            /// Represents a generated AI host copy request.
            /// </summary>
            public sealed class GeneratedModelCopyRequest
            {
                /// <summary>
                /// Gets or sets the source model.
                /// </summary>
                [JsonPropertyName("source")]
                public string? Source { get; set; }

                /// <summary>
                /// Gets or sets the destination model.
                /// </summary>
                [JsonPropertyName("destination")]
                public string? Destination { get; set; }
            }

            /// <summary>
            /// Represents a generated AI host chat request.
            /// </summary>
            public sealed class GeneratedChatRequest
            {
                /// <summary>
                /// Gets or sets the requested model.
                /// </summary>
                [JsonPropertyName("model")]
                public string? Model { get; set; }

                /// <summary>
                /// Gets or sets the chat messages.
                /// </summary>
                [JsonPropertyName("messages")]
                public List<GeneratedChatMessage> Messages { get; set; } = [];

                /// <summary>
                /// Gets or sets whether the caller requested streaming.
                /// </summary>
                [JsonPropertyName("stream")]
                public bool Stream { get; set; }

                /// <summary>
                /// Gets or sets Ollama-compatible request options.
                /// </summary>
                [JsonPropertyName("options")]
                public GeneratedRequestOptions? Options { get; set; }
            }

            /// <summary>
            /// Represents a generated AI host chat message.
            /// </summary>
            public sealed class GeneratedChatMessage
            {
                /// <summary>
                /// Creates an empty generated chat message.
                /// </summary>
                public GeneratedChatMessage()
                {
                }

                /// <summary>
                /// Creates a generated chat message.
                /// </summary>
                public GeneratedChatMessage(string role, string content)
                {
                    Role = role;
                    Content = content;
                }

                /// <summary>
                /// Gets or sets the chat role.
                /// </summary>
                [JsonPropertyName("role")]
                public string Role { get; set; } = string.Empty;

                /// <summary>
                /// Gets or sets the chat content.
                /// </summary>
                [JsonPropertyName("content")]
                public string Content { get; set; } = string.Empty;
            }

            /// <summary>
            /// Represents a generated AI host chat response with typed properties for Razor and service code.
            /// </summary>
            public sealed class GeneratedChatResponse
            {
                /// <summary>
                /// Creates a generated chat response.
                /// </summary>
                public GeneratedChatResponse(
                    string model,
                    DateTimeOffset createdAt,
                    GeneratedChatMessage message,
                    bool done)
                {
                    Model = model;
                    CreatedAt = createdAt;
                    Message = message;
                    Done = done;
                }

                /// <summary>
                /// Gets the model name used by the generated response.
                /// </summary>
                [JsonPropertyName("model")]
                public string Model { get; }

                /// <summary>
                /// Gets the generated response timestamp.
                /// </summary>
                [JsonPropertyName("created_at")]
                public DateTimeOffset CreatedAt { get; }

                /// <summary>
                /// Gets the generated assistant message.
                /// </summary>
                [JsonPropertyName("message")]
                public GeneratedChatMessage Message { get; }

                /// <summary>
                /// Gets whether the generated response is complete.
                /// </summary>
                [JsonPropertyName("done")]
                public bool Done { get; }
            }

            /// <summary>
            /// Describes a generated model download planning row.
            /// </summary>
            public sealed class GeneratedModelDownloadCandidate
            {
                /// <summary>
                /// Creates a generated download candidate.
                /// </summary>
                public GeneratedModelDownloadCandidate(
                    string name,
                    string sourceType,
                    string sourceUrl,
                    string recommendedFor,
                    string downloadRoute,
                    string safetyNote)
                {
                    Name = name;
                    SourceType = sourceType;
                    SourceUrl = sourceUrl;
                    RecommendedFor = recommendedFor;
                    DownloadRoute = downloadRoute;
                    SafetyNote = safetyNote;
                }

                /// <summary>
                /// Gets the candidate model name.
                /// </summary>
                public string Name { get; }

                /// <summary>
                /// Gets the catalog or provider type.
                /// </summary>
                public string SourceType { get; }

                /// <summary>
                /// Gets the catalog URL or provider base URI.
                /// </summary>
                public string SourceUrl { get; }

                /// <summary>
                /// Gets the recommended use case.
                /// </summary>
                public string RecommendedFor { get; }

                /// <summary>
                /// Gets the route used to plan the download.
                /// </summary>
                public string DownloadRoute { get; }

                /// <summary>
                /// Gets the safety note for the generated lab.
                /// </summary>
                public string SafetyNote { get; }
            }

            /// <summary>
            /// Holds generated AI host lab settings shown in the DevExpress form.
            /// </summary>
            public sealed class GeneratedAiHostSettings
            {
                /// <summary>
                /// Gets or sets the local model source summary.
                /// </summary>
                public string BaseUri { get; set; } = "native local model files";

                /// <summary>
                /// Gets or sets the default model for generated request examples.
                /// </summary>
                public string DefaultModel { get; set; } = "gpt-oss:20b";

                /// <summary>
                /// Gets or sets the generated keep-alive value.
                /// </summary>
                public string KeepAlive { get; set; } = "5m";

                /// <summary>
                /// Gets or sets the generated context token budget.
                /// </summary>
                public int ContextTokens { get; set; } = 2048;

                /// <summary>
                /// Gets or sets the generated GPU layer budget.
                /// </summary>
                public int GpuLayers { get; set; } = 0;

                /// <summary>
                /// Gets or sets whether a native runner is attached.
                /// </summary>
                public bool NativeRunnerAttached { get; set; }

                /// <summary>
                /// Gets or sets whether pull planning is enabled.
                /// </summary>
                public bool AllowPullPlanning { get; set; } = true;
            }

            /// <summary>
            /// Describes a generated AI-host-compatible operation result.
            /// </summary>
            public sealed class GeneratedAiHostOperation
            {
                /// <summary>
                /// Creates a generated operation result.
                /// </summary>
                public GeneratedAiHostOperation(
                    string status,
                    string model,
                    string route,
                    bool done,
                    string detail)
                {
                    Status = status;
                    Model = model;
                    Route = route;
                    Done = done;
                    Detail = detail;
                }

                /// <summary>
                /// Gets the generated operation status.
                /// </summary>
                [JsonPropertyName("status")]
                public string Status { get; }

                /// <summary>
                /// Gets the affected model or model mapping.
                /// </summary>
                [JsonPropertyName("model")]
                public string Model { get; }

                /// <summary>
                /// Gets the route that produced the result.
                /// </summary>
                [JsonPropertyName("route")]
                public string Route { get; }

                /// <summary>
                /// Gets whether the generated operation is complete.
                /// </summary>
                [JsonPropertyName("done")]
                public bool Done { get; }

                /// <summary>
                /// Gets the generated explanation.
                /// </summary>
                [JsonPropertyName("detail")]
                public string Detail { get; }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionModel projectName:{projectName}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Runs the generate navigation icon svgs operation.
        /// </summary>
        public IReadOnlyList<(string FileName, string Svg)>? GenerateNavigationIconSvgs( ILogger logger)
        {
            try
            {
                return [
            ("dashboard-line.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="dashboard-line-title">
                  <title id="dashboard-line-title">Dashboard line icon</title>
                  <g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <rect x="4" y="4" width="7" height="7" rx="1.5" />
                    <rect x="13" y="4" width="7" height="4.8" rx="1.5" />
                    <rect x="13" y="11.2" width="7" height="8.8" rx="1.5" />
                    <rect x="4" y="13" width="7" height="7" rx="1.5" />
                  </g>
                </svg>
                """),
            ("dashboard-solid.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="dashboard-solid-title">
                  <title id="dashboard-solid-title">Dashboard solid icon</title>
                  <path fill="currentColor" opacity=".22" d="M4 5.6A1.6 1.6 0 0 1 5.6 4h4.8A1.6 1.6 0 0 1 12 5.6v4.8A1.6 1.6 0 0 1 10.4 12H5.6A1.6 1.6 0 0 1 4 10.4z" />
                  <path fill="currentColor" d="M13 5.6A1.6 1.6 0 0 1 14.6 4h4.8A1.6 1.6 0 0 1 21 5.6v3A1.6 1.6 0 0 1 19.4 10h-4.8A1.6 1.6 0 0 1 13 8.6z" />
                  <path fill="currentColor" d="M13 12.6a1.6 1.6 0 0 1 1.6-1.6h4.8a1.6 1.6 0 0 1 1.6 1.6v5.8a1.6 1.6 0 0 1-1.6 1.6h-4.8a1.6 1.6 0 0 1-1.6-1.6z" />
                  <path fill="currentColor" d="M4 14.6A1.6 1.6 0 0 1 5.6 13h4.8a1.6 1.6 0 0 1 1.6 1.6v4.8a1.6 1.6 0 0 1-1.6 1.6H5.6A1.6 1.6 0 0 1 4 19.4z" />
                </svg>
                """),
            ("catalog-line.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="catalog-line-title">
                  <title id="catalog-line-title">Catalog line icon</title>
                  <g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M5 5.5A2.5 2.5 0 0 1 7.5 3H19v15.5H7.5A2.5 2.5 0 0 0 5 21z" />
                    <path d="M5 18.5A2.5 2.5 0 0 0 7.5 21H19" />
                    <path d="M9 7h6" />
                    <path d="M9 10.5h5" />
                    <path d="M9 14h4" />
                  </g>
                </svg>
                """),
            ("catalog-solid.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="catalog-solid-title">
                  <title id="catalog-solid-title">Catalog solid icon</title>
                  <path fill="currentColor" opacity=".2" d="M5 5.5A2.5 2.5 0 0 1 7.5 3H19v15.5H7.5A2.5 2.5 0 0 0 5 21z" />
                  <path fill="currentColor" d="M7.5 3A2.5 2.5 0 0 0 5 5.5V21a2.5 2.5 0 0 1 2.5-2.5H19V3zm1.8 4.2h6.4v1.7H9.3zm0 3.8h5.4v1.7H9.3zm0 3.8h4.2v1.7H9.3z" />
                </svg>
                """),
            ("detail-line.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="detail-line-title">
                  <title id="detail-line-title">Detail line icon</title>
                  <g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <rect x="4" y="4" width="16" height="16" rx="2.2" />
                    <path d="M8 8h8" />
                    <path d="M8 12h8" />
                    <path d="M8 16h4.5" />
                    <path d="m14.8 16.1 1.3 1.3 2.3-2.7" />
                  </g>
                </svg>
                """),
            ("detail-solid.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="detail-solid-title">
                  <title id="detail-solid-title">Detail solid icon</title>
                  <path fill="currentColor" opacity=".2" d="M4 6.2A2.2 2.2 0 0 1 6.2 4h11.6A2.2 2.2 0 0 1 20 6.2v11.6a2.2 2.2 0 0 1-2.2 2.2H6.2A2.2 2.2 0 0 1 4 17.8z" />
                  <path fill="currentColor" d="M8 7.4h8v1.8H8zm0 3.7h8v1.8H8zm0 3.7h4.5v1.8H8z" />
                  <path fill="currentColor" d="m15 16.3 1.1 1.1 2.6-3 .9 1-3.5 4L14 17.3z" />
                </svg>
                """)
        ];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateNavigationIconSvgs");
                return null;
            }

        }

        /// <summary>
        /// Runs the generate solution readme operation.
        /// </summary>
        public string GenerateSolutionReadme(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var description = isAiHostLab
                ? "Generated by LocalGPT as a .NET 10 ASP.NET Core and DevExpress Blazor AI host with a native local-model-file runner contract."
                : "Generated by LocalGPT as a whole-solution AI Council artifact.";

                var notes = isAiHostLab
                    ? """
                  ## AI Host Control-Plane Lab Scope

                  This prototype demonstrates selected provider-compatible HTTP routes, model catalog UX, health cards, endpoint testing in .NET/Blazor, and a native local-model-file runner boundary.

                  It does not proxy `/api/chat` or `/api/generate` to upstream Ollama/LM Studio/OpenAI-compatible hosts. It can read local model-file metadata, resolve `.gguf` or Ollama-managed blob candidates, and invoke a configured approved native executable.
                  """
                    : """
                  ## Scope

                  This is a LocalGPT/TacosPortalOpen-style .NET 10 Blazor and DevExpress generation sandbox.
                  """;

                return
                $$"""
            # {{projectName}}

            {{description}}

            This zip is a sandbox prototype. Review it before copying any file into LocalGPT, TacosPortalOpen, or another real project.

            ## Contents

            - `{{projectName}}.sln`
            - `src/{{projectName}}/{{projectName}}.csproj`
            - Blazor Web App `Program.cs`, `App.razor`, `catalog.Routes.razor`
            - Routable Razor pages under `Components/Pages`
            - Service/model code under `Services` and `Models`
            - `wwwroot/app.css`
            - Navigation SVG pairs under `wwwroot/icons/nav`
            - `PROJECT_INDEX.md`
            - `ARCHITECTURE.md`
            - `SOURCE_FIDELITY.md`
            - `BUILD_AND_RUN.md`
            - `.localgpt-generation.json`
            - `LocalGPT.GenerationManifest.json`

            {{notes}}

            ## Design Contract

            The UI uses Bootstrap v5-style spacing and responsive layout with DevExpress Blazor controls for actual interaction. Navigation includes line SVG icons for the default state and solid SVG icons for hover/focus states so generated apps have a reusable icon language.

            ## Build

            ```powershell
            dotnet restore
            dotnet build
            ```

            ## Original Request

            {{TrimForCodeComment(request.Prompt, 1200, logger)}}

            ## Council Output Summary

            {{TrimForCodeComment(result.FinalAnswer, 1200, logger)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionReadme");
                return string.Empty;
            }

        }
        /// <summary>
        /// Runs the generate solution project index operation.
        /// </summary>
        public string GenerateSolutionProjectIndex(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var projectKind = isAiHostLab ? "dotnet_service" : "localgpt_feature";
                var purpose = isAiHostLab
                    ? "Prototype an AI-host-shaped .NET/Blazor control plane without claiming native model inference."
                    : "Prototype a LocalGPT/TacosPortalOpen-style AI Council feature workspace with reviewable Blazor pages.";
                var catalogPage = isAiHostLab ? "GeneratedKnowledgeTable.razor route `/models`" : "GeneratedKnowledgeTable.razor route `/knowledge`";
                var detailPage = isAiHostLab ? "ApiConsole.razor route `/api-console`" : "ImplementationPlan.razor route `/implementation-plan`";
                var aiHostExpectedEntryPoints = isAiHostLab
                    ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/Chat.razor",
                "src/{{projectName}}/Components/Pages/RunningModels.razor",
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Templates.razor",
                "src/{{projectName}}/Components/Pages/Hardware.razor",
                "src/{{projectName}}/Components/Pages/RunnerPlugins.razor",
                "src/{{projectName}}/Components/Pages/Logs.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor"
                """
                    : string.Empty;
                var aiHostEntryPoints = isAiHostLab
                    ? $$"""
            - `src/{{projectName}}/Components/Pages/Chat.razor` - safe chat route preview with typed response data.
            - `src/{{projectName}}/Components/Pages/RunningModels.razor` - running-model status grid.
            - `src/{{projectName}}/Components/Pages/ModelDownloads.razor` - DevExpress model pull planning page.
            - `src/{{projectName}}/Components/Pages/Templates.razor` - chat template and thinking-format guidance.
            - `src/{{projectName}}/Components/Pages/Hardware.razor` - GPU/VRAM/context policy page.
            - `src/{{projectName}}/Components/Pages/RunnerPlugins.razor` - native-runner, plugin, script, and adapter boundary page.
            - `src/{{projectName}}/Components/Pages/Logs.razor` - runtime-boundary diagnostics page.
            - `src/{{projectName}}/Components/Pages/Settings.razor` - AI host settings and runner-boundary page.
            """
                    : string.Empty;
                var aiHostGeneratedFiles = isAiHostLab
                    ? $$"""
            | `src/{{projectName}}/Components/Pages/Chat.razor` | DevExpress chat page with safe typed `/api/chat` preview. |
            | `src/{{projectName}}/Components/Pages/RunningModels.razor` | Running model status grid for `/api/ps`-style state. |
            | `src/{{projectName}}/Components/Pages/ModelDownloads.razor` | DevExpress UI for provider-style pull planning and download guidance. |
            | `src/{{projectName}}/Components/Pages/Templates.razor` | Chat template, Harmony, and thinking-format compatibility page. |
            | `src/{{projectName}}/Components/Pages/Hardware.razor` | GPU/VRAM/context budget and queue-policy page. |
            | `src/{{projectName}}/Components/Pages/RunnerPlugins.razor` | Runner/plugin/script adapter surface for native local model-file execution. |
            | `src/{{projectName}}/Components/Pages/Logs.razor` | Runtime-boundary diagnostic log page. |
            | `src/{{projectName}}/Components/Pages/Settings.razor` | DevExpress settings page for local model source, context, and native-runner boundaries. |
            | `src/{{projectName}}/Services/GeneratedAiHostArchitectureServices.cs` | Provider, runner, plugin, script, hardware, and template contracts wired through DI. |
            """
                    : string.Empty;

                return $$"""
            # Project Index

            ## Purpose

            {{purpose}}

            ## Archetype

            ```json
            {
              "project_kind": "{{projectKind}}",
              "complexity": "normal",
              "needs_datagen": false,
              "needs_tests": true,
              "needs_native_commands": {{(isAiHostLab ? "true" : "false")}},
              "needs_index": true,
              "needs_version_resolver": false,
              "expected_entrypoints": [
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/SourceFidelity.razor"{{aiHostExpectedEntryPoints}}
              ]
            }
            ```

            ## Entry Points

            - `src/{{projectName}}/Program.cs` - ASP.NET Core service registration, DevExpress setup, and app pipeline.
            - `src/{{projectName}}/Components/App.razor` - document shell and static asset links.
            - `src/{{projectName}}/Components/catalog.Routes.razor` - Blazor route discovery.
            - `src/{{projectName}}/Components/GeneratedNavigation.razor` - generated app navigation.
            - `src/{{projectName}}/Components/Pages/Index.razor` - first viewport and page hub.
            - `src/{{projectName}}/Components/Pages/GeneratedDashboard.razor` - health/status grid.
            - `src/{{projectName}}/Components/Pages/{{catalogPage}}` - archetype catalog page.
            - `src/{{projectName}}/Components/Pages/{{detailPage}}` - archetype-specific detail page.
            - `src/{{projectName}}/Components/Pages/SourceFidelity.razor` - source-fidelity review grid.
            {{aiHostEntryPoints}}

            ## Generated Files

            | File | Why it exists |
            | --- | --- |
            | `{{projectName}}.sln` | Visual Studio and CLI solution entry point. |
            | `src/{{projectName}}/{{projectName}}.csproj` | .NET 10 Blazor Web App project with DevExpress dependency. |
            | `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs` | Typed demo service instead of Razor-only fake data. |
            | `src/{{projectName}}/Services/GeneratedSourceFidelityService.cs` | Typed evidence that the sandbox preserves source workflows or marks capability gaps. |
            | `src/{{projectName}}/Models/GeneratedHealthCard.cs` | Shared model records for grids/catalog rows. |
            {{aiHostGeneratedFiles}}
            | `src/{{projectName}}/wwwroot/app.css` | Local styling for the generated shell. |
            | `src/{{projectName}}/wwwroot/icons/nav/*-line.svg` | Default navigation icon style. |
            | `src/{{projectName}}/wwwroot/icons/nav/*-solid.svg` | Hover/focus navigation icon style. |
            | `PROJECT_INDEX.md` | Required generated-project map and archetype declaration. |
            | `ARCHITECTURE.md` | Explains why this artifact differs from other project types. |
            | `SOURCE_FIDELITY.md` | Explains why a compiling artifact still needs architectural fidelity review. |
            | `PROMISE_MAP.md` | Maps the council's promised workflows into generated modules and review surfaces. |
            | `DESIGN_REVIEW.md` | Explains layout choices, DevExpress components, mocked pieces, and follow-up wiring. |
            | `BUILD_AND_RUN.md` | Exact restore/build/run commands and expected checks. |
            | `.localgpt-generation.json` | Machine-readable generation contract. |
            | `LocalGPT.GenerationManifest.json` | LocalGPT artifact metadata and safety notes. |

            ## Validation Status

            Generated only. The LocalGPT artifact service validated the required file contract before zipping. Run `dotnet build` before treating this as build-passed.

            ## Original Request

            {{TrimForCodeComment(request.Prompt, 900, logger)}}

            ## Council Summary

            {{TrimForCodeComment(result.FinalAnswer, 900, logger)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionProjectIndex");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the generate solution architecture doc operation.
        /// </summary>
        public string GenerateSolutionArchitectureDoc(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var title = isAiHostLab ? "AI Host Control-Plane Architecture" : "LocalGPT Feature Lab Architecture";
                var contrast = isAiHostLab
                    ? "This is not a LocalGPT feature page clone. It is an API-control-plane lab with endpoint cataloging, model rows, and explicit runner boundaries."
                    : "This is not an AI host control-plane lab. It is a LocalGPT/TacosPortalOpen-style feature sandbox with council grounding, implementation steps, and approval gates.";
                var backendBoundary = isAiHostLab
                    ? "Native inference, GGML/GPU scheduling, model loading, and tokenizer/runtime ownership stay outside this prototype until a real runner adapter is approved."
                    : "EF/SQLite writes, native commands, report generation, and artifact creation belong in backend services and routes, not in Razor-only snippets.";

                return $$"""
            # {{title}}

            ## Why This Shape

            {{contrast}}

            The solution uses a .NET 10 Blazor Web App with Interactive Server rendering because LocalGPT's desktop wrapper and debugging flow already favor server-side ownership for diagnostics, downloads, SQLite, and native-command boundaries.

            ## Boundaries

            - Blazor pages own user interaction and DevExpress presentation.
            - Services own generated data and route-test state.
            - Models are plain, typed C# objects consumed by DevExpress grids/forms.
            - {{backendBoundary}}
            - Generated code remains sandboxed until the user explicitly approves integration.

            ## Microsoft Architecture Rules Applied

            - Prefer a single cohesive web app when the feature does not need independent deployment.
            - Introduce service-oriented separation only around real boundaries, such as independent scaling, external runner adapters, background work, or downloadable artifacts.
            - Keep configuration/bootstrap data in appsettings and durable user/application state in EF/SQLite.
            - Keep APIs and services testable through DI rather than embedding logic in markup strings.
            - Provide health/status views and build instructions so the artifact can be reviewed line by line.
            - Use Bootstrap v5 for responsive page structure and DevExpress controls for real application interactions.
            - Include paired line/solid SVG navigation icons so default and active states are visually distinct.
            - For AI-host labs, generate interface-driven provider, model catalog, download, native-runner, plugin, script, template, and hardware-budget services. A dashboard without these contracts is incomplete.

            ## Files To Review First

            1. `PROJECT_INDEX.md`
            2. `.localgpt-generation.json`
            3. `src/{{projectName}}/Program.cs`
            4. `src/{{projectName}}/Components/Pages/Index.razor`
            5. `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs`
            6. `SOURCE_FIDELITY.md`
            7. `src/{{projectName}}/Services/GeneratedSourceFidelityService.cs`
            {{(isAiHostLab ? $"8. `src/{projectName}/Services/GeneratedAiHostArchitectureServices.cs`" : string.Empty)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionArchitectureDoc projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Runs the generate source fidelity doc operation.
        /// </summary>
        public string GenerateSourceFidelityDoc(
            string projectName,
            GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var expectedShape = archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt =>
                        "A LocalGPT replacement must look and behave like a local-first AI workbench: DXAiChat, AI Council, SQLite memory/knowledge, artifact downloads, Minecraft builder, Install, Test Lab, diagnostics, and visible model/runtime status.",
                    GeneratedSolutionArchetype.TacosPortal =>
                        "A TacosPortalOpen replacement must preserve the multi-host/event-ingestion architecture: core/shared services, Telegram or message ingestion, normalized persistence, workers, notifications, DevExpress admin/security, optional WASM client, and WinUI/WebView2 wrapper boundaries. It is not accepted as a generic restaurant ordering portal.",
                    GeneratedSolutionArchetype.AiHost =>
                        "An AI-host replacement must expose provider-compatible API routes, catalog/download/running-model UX, chat/API console, logs, settings, templates, hardware policy, runner/plugin boundaries, and direct local model-file inference without upstream proxying.",
                    GeneratedSolutionArchetype.BotBackend =>
                        "A bot backend replacement must expose webhook ingress, conversation state, command routing, moderation/retry queues, settings/logs, optional Python interop, and permission gates.",
                    _ =>
                        "A generic generated solution must still show which source behaviors are represented, stubbed, or out of scope."
                };
                var promiseReview = promiseModules.Count == 0
                    ? "No dynamic promise modules were detected from the council answer. Review the base archetype files and the original prompt manually."
                    : string.Join(
                        Environment.NewLine,
                        promiseModules.Select(module => $"- `{module.Route}` / `{module.FileName}`: {module.Summary}"));

                return $$"""
            # Source Fidelity

            Generated project: `{{projectName}}`

            ## Acceptance Rule

            A generated solution is not accepted merely because it compiles. It must preserve the requested source application's recognizable workflows, navigation, service boundaries, persistence shape, diagnostics, and download/artifact behavior.

            ## Expected Shape

            {{expectedShape}}

            ## Dynamic Promise Modules

            {{promiseReview}}

            ## Review Files

            - `src/{{projectName}}/Components/Pages/SourceFidelity.razor`
            - `src/{{projectName}}/Services/GeneratedSourceFidelityService.cs`
            - `PROJECT_INDEX.md`
            - `.localgpt-generation.json`
            - `ARCHITECTURE.md`

            ## Benchmark Guidance

            LocalGPT replacement benchmarks should inspect this file and the generated source-fidelity service before awarding architecture points. A page-only solution should score low when it misses source workflows, service contracts, or persistence boundaries.

            ## Integration Rule

            This remains a sandbox artifact. Copying generated files into a real repo requires explicit user approval, a build, and a focused smoke test.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSourceFidelityDoc projectName:{projectName} archetype:{archetype.ToString()} promiseModules:{promiseModules.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate promise map doc operation.
        /// </summary>
        public string GeneratePromiseMapDoc(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var moduleRows = promiseModules.Count == 0
                ? "| No dynamic modules detected | The generated solution must be reviewed against the request manually. |"
                : string.Join(
                    Environment.NewLine,
                    promiseModules.Select(module =>
                        $"| `{module.Route}` | `{module.FileName}` | {module.Summary} | {string.Join(", ", module.Areas)} |"));

                return $$"""
            # Promise Map

            Generated project: `{{projectName}}`

            This file maps the user's request and the council's promised architecture into generated review surfaces. It exists so LocalGPT does not ship a generic shell after the council described a richer application.

            ## Dynamic Modules

            | Route | File | Promise Preserved | Review Areas |
            | --- | --- | --- | --- |
            {{moduleRows}}

            ## Artifact Rule

            A concrete downloadable target is not enough by itself. If the council creates a blocking user decision poll, artifact generation must pause until the user answers or grants safe sandbox auto-choice. If no blocking poll remains, the generated artifact should preserve the council's promised workflows in pages, services, docs, and validation notes.

            ## Request Excerpt

            ```text
            {{TrimForCodeComment(request.Prompt, 1200, logger)}}
            ```

            ## Council Excerpt

            ```text
            {{TrimForCodeComment(result.FinalAnswer, 1600, logger)}}
            ```
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSourceFidelityDoc");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the generate design review doc operation.
        /// </summary>
        public string GenerateDesignReviewDoc(
            string projectName,
             GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var moduleList = promiseModules.Count == 0
                ? "- No dynamic promise modules were detected. The design stays with the base generated shell."
                : string.Join(Environment.NewLine, promiseModules.Select(module => $"- `{module.Title}` uses DevExpress grid/form patterns to expose {string.Join(", ", module.Areas)}."));
                var archetypeName = archetype.ToString();

                return $$"""
            # Design Review

            Generated project: `{{projectName}}`

            ## Layout Choice

            The artifact uses a compact operational layout: top navigation, dashboard/status grid, source-fidelity review, and one page per detected promise module. It avoids a marketing-style landing page because generated LocalGPT artifacts are tools first.

            ## Base Archetype

            `{{archetypeName}}`

            ## Dynamic UI Modules

            {{moduleList}}

            ## Components Used

            - DevExpress Blazor navigation-friendly pages.
            - `DxGrid` for scan-friendly operational state.
            - `DxFormLayout`, `DxTextBox`, and `DxMemo` for bounded review and settings surfaces.
            - Bootstrap-compatible CSS for responsive grid and toolbar layout.
            - Paired SVG navigation icons with line/solid states.

            ## Mocked Versus Real

            Mocked: generated rows, status values, and sample implementation boundaries.

            Real: routable Razor files, compileable project structure, static CSS/icons, docs, generation manifest, and source-fidelity contract.

            ## Needs Wiring

            - Replace generated sample services with real backend services.
            - Add EF/SQLite persistence if user-visible state must survive restarts.
            - Add route smoke tests for every generated endpoint.
            - Add real DevExpress report/export implementation only after package/API verification and user approval.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateDesignReviewDoc projectName:{projectName} archetype:{archetype.ToString()} promiseModules:{promiseModules.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate solution build and run doc operation.
        /// </summary>
        public string GenerateSolutionBuildAndRunDoc(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var smokeRoute = isAiHostLab
                ? "Open `/api-console`, `/model-downloads`, `/runner-plugins`, and `/settings`; then call `/api/version`, `/api/tags`, `/api/localgpt/runner/capability`, and `/api/chat` to verify route shape, local model-file runner readiness, and no upstream proxy fallback."
                : "Open `/implementation-plan` and verify implementation steps are visible.";
                return $$"""
            # Build And Run

            ## Requirements

            - .NET 10 SDK
            - DevExpress Blazor 25.1 package feed/license access
            - Visual Studio 2026 or `dotnet` CLI

            ## Commands

            ```powershell
            dotnet restore
            dotnet build
            dotnet run --project .\src\{{projectName}}\{{projectName}}.csproj
            ```

            ## Expected Checks

            - `/` shows the generated index page with navigation.
            - `/dashboard` shows the generated health/status grid.
            - `/source-fidelity` shows source-signal, boundary, status, and evidence rows.
            - {{smokeRoute}}
            - `PROJECT_INDEX.md` and `.localgpt-generation.json` describe the selected archetype.

            ## Validation Honesty

            Do not claim build success unless `dotnet build` completed and the command output is available. Do not claim production readiness; this zip is a sandbox artifact.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionBuildAndRunDoc projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the generate local gpt generation JSON operation.
        /// </summary>
        public string GenerateLocalGptGenerationJson(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var projectKind = isAiHostLab ? "dotnet_service" : "localgpt_feature";
                var targetPlatform = isAiHostLab
                    ? "dotnet10_aspnetcore_devexpress_blazor_ai_host_control_plane"
                    : "dotnet10_aspnetcore_devexpress_blazor_localgpt_feature";
                var detailPage = isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor";
                var validationNotes = isAiHostLab
                    ? "Required docs, source-fidelity files, manifest, navigation, paired nav icons, index, dashboard, model catalog, API console, chat, running-models, model-download, templates, hardware, runner-plugin, logs, settings, and AI-host architecture service files were present before zipping."
                    : "Required docs, source-fidelity files, manifest, navigation, paired nav icons, index, dashboard, knowledge table, and implementation-plan files were present before zipping.";
                var aiHostExpectedEntryPoints = isAiHostLab
                    ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/Chat.razor",
                "src/{{projectName}}/Components/Pages/RunningModels.razor",
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Templates.razor",
                "src/{{projectName}}/Components/Pages/Hardware.razor",
                "src/{{projectName}}/Components/Pages/RunnerPlugins.razor",
                "src/{{projectName}}/Components/Pages/Logs.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor"
                """
                    : string.Empty;
                var aiHostGeneratedFiles = isAiHostLab
                    ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/Chat.razor",
                "src/{{projectName}}/Components/Pages/RunningModels.razor",
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Templates.razor",
                "src/{{projectName}}/Components/Pages/Hardware.razor",
                "src/{{projectName}}/Components/Pages/RunnerPlugins.razor",
                "src/{{projectName}}/Components/Pages/Logs.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor",
                "src/{{projectName}}/Services/GeneratedAiHostArchitectureServices.cs"
                """
                    : string.Empty;

                return $$"""
            {
              "schema": "localgpt-generation-contract/v1",
              "project_kind": "{{projectKind}}",
              "target_platform": "{{targetPlatform}}",
              "project_name": "{{EscapeJsonString(projectName, logger)}}",
              "generated_at_utc": "{{DateTime.UtcNow:O}}",
              "complexity": "normal",
              "needs_datagen": false,
              "needs_tests": true,
              "needs_native_commands": {{(isAiHostLab ? "true" : "false")}},
              "needs_index": true,
              "needs_version_resolver": false,
              "model_names": "{{EscapeJsonString(string.Join(", ", result.ModelNames), logger)}}",
              "requested_features": "{{EscapeJsonString(TrimForCodeComment(request.Prompt, 900, logger), logger)}}",
              "validation_status": "GeneratedFilesValidatedOnly",
              "validation_notes": "{{EscapeJsonString(validationNotes, logger)}}",
              "build_test_result_provenance": "LocalGPT validated required files and contract JSON before zipping. dotnet build was not run for this sandbox artifact, so no build success is claimed.",
              "expected_entrypoints": [
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/GeneratedNavigation.razor",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/SourceFidelity.razor",
                "src/{{projectName}}/Components/Pages/{{detailPage}}"{{aiHostExpectedEntryPoints}}
              ],
              "generated_files": [
                "{{projectName}}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
                "SOURCE_FIDELITY.md",
                "PROMISE_MAP.md",
                "DESIGN_REVIEW.md",
                "BUILD_AND_RUN.md",
                ".localgpt-generation.json",
                "LocalGPT.GenerationManifest.json",
                "src/{{projectName}}/{{projectName}}.csproj",
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/App.razor",
                "src/{{projectName}}/Components/Routes.razor",
                "src/{{projectName}}/Components/GeneratedNavigation.razor",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/SourceFidelity.razor",
                "src/{{projectName}}/Components/Pages/{{detailPage}}"{{aiHostGeneratedFiles}},
                "src/{{projectName}}/Services/GeneratedHealthSummaryService.cs",
                "src/{{projectName}}/Services/GeneratedSourceFidelityService.cs",
                "src/{{projectName}}/Models/GeneratedHealthCard.cs",
                "src/{{projectName}}/wwwroot/app.css",
                "src/{{projectName}}/wwwroot/icons/nav/dashboard-line.svg",
                "src/{{projectName}}/wwwroot/icons/nav/dashboard-solid.svg",
                "src/{{projectName}}/wwwroot/icons/nav/catalog-line.svg",
                "src/{{projectName}}/wwwroot/icons/nav/catalog-solid.svg",
                "src/{{projectName}}/wwwroot/icons/nav/detail-line.svg",
                "src/{{projectName}}/wwwroot/icons/nav/detail-solid.svg"
              ],
              "safety": "Sandbox artifact only. Integration requires explicit user approval.",
              "archetype_difference": "{{(isAiHostLab ? "AI host lab includes API routes, model catalog, downloads, settings, and a native local-model-file runner contract; upstream proxying is explicitly out of scope." : "LocalGPT feature sandbox includes implementation-plan and knowledge-table pages rather than AI host compatibility workflows.")}}"
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateLocalGptGenerationJson");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the generate solution manifest operation.
        /// </summary>
        public string GenerateSolutionManifest(
            string projectName,
            string solutionGuid,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var sourceGoal = isAiHostLab
                ? ".NET 10 ASP.NET Core and DevExpress Blazor AI host control-plane lab with explicit provider, plugin, script, and native-runner adapter boundaries"
                : "LocalGPT/TacosPortalOpen-style .NET 10 Blazor and DevExpress generation";

                return
                $$"""
            {
              "projectName": "{{EscapeJsonString(projectName, logger)}}",
              "solutionGuid": "{{EscapeJsonString(solutionGuid, logger)}}",
              "generatedAtUtc": "{{DateTime.UtcNow:O}}",
              "modelNames": "{{EscapeJsonString(string.Join(", ", result.ModelNames), logger)}}",
              "artifactKind": "WholeSolutionZip",
              "sourceGoal": "{{EscapeJsonString(sourceGoal, logger)}}",
              "designContract": "Bootstrap v5 layout, DevExpress Blazor controls, and paired line/solid SVG navigation icons.",
              "validationStatus": "GeneratedFilesValidatedOnly",
              "buildTestResultProvenance": "Required files and contract metadata were validated before zipping. No generated-project build success is claimed.",
              "request": "{{EscapeJsonString(TrimForCodeComment(request.Prompt, 1400, logger), logger)}}",
              "finalAnswer": "{{EscapeJsonString(TrimForCodeComment(result.FinalAnswer, 1400, logger), logger)}}",
              "safety": "Sandbox artifact only. Integration requires explicit user approval."
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateLocalGptGenerationJson");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate blazor dev express razor example operation.
        /// </summary>
        public string GenerateBlazorDevExpressRazorExample(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var requestSummary = TrimForCodeComment(request.Prompt, 700, logger);
                var consensusSummary = TrimForCodeComment(result.FinalAnswer, 900, logger);
                return $$"""
                @page "/generated/localgpt-health-summary"
                @rendermode InteractiveServer
                @using DevExpress.Blazor

                <PageTitle>LocalGPT Health Summary</PageTitle>

                <div class="main-container generated-feature-page">
                    <h3>LocalGPT Health Summary</h3>

                    <DxLoadingPanel CssClass="w-100"
                                    @bind-Visible="PanelVisible"
                                    CloseOnClick="true"
                                    IndicatorVisible="true"
                                    IsContentBlocked="false"
                                    IndicatorAreaVisible="false"
                                    Text="Refreshing diagnostics...">
                        <div class="top-container">
                            <DxButton Text="Refresh"
                                      RenderStyle="ButtonRenderStyle.Primary"
                                      RenderStyleMode="ButtonRenderStyleMode.Contained"
                                      Click="RefreshAsync" />
                            <DxCheckBox @bind-Checked="ShowTechnicalDetails"
                                        Text="Show technical details" />
                        </div>

                        <DxGrid Data="@Cards"
                                KeyFieldName="@nameof(HealthCard.Area)"
                                ShowSearchBox="true"
                                ShowFilterRow="true"
                                AllowSort="true"
                                HighlightRowOnHover="true"
                                TextWrapEnabled="false"
                                ColumnResizeMode="GridColumnResizeMode.NextColumn">
                            <Columns>
                                <DxGridDataColumn FieldName="@nameof(HealthCard.Area)" Caption="Area" />
                                <DxGridDataColumn FieldName="@nameof(HealthCard.Status)" Caption="Status" />
                                <DxGridDataColumn FieldName="@nameof(HealthCard.NextAction)" Caption="Next Action" />
                                @if (ShowTechnicalDetails)
                                {
                                    <DxGridDataColumn FieldName="@nameof(HealthCard.Detail)" Caption="Detail" />
                                }
                            </Columns>
                        </DxGrid>

                        <DxFormLayout CssClass="mt-3" SizeMode="SizeMode.Medium">
                            <DxFormLayoutGroup Caption="Implementation Note" ColSpanMd="12">
                                <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                                    <DxMemo Text="@RequestSummary" Rows="4" ReadOnly="true" />
                                </DxFormLayoutItem>
                                <DxFormLayoutItem Caption="Council Consensus" ColSpanMd="12">
                                    <DxMemo Text="@CouncilConsensus" Rows="5" ReadOnly="true" />
                                </DxFormLayoutItem>
                            </DxFormLayoutGroup>
                        </DxFormLayout>
                    </DxLoadingPanel>
                </div>

                @code {
                    bool PanelVisible { get; set; }
                    bool ShowTechnicalDetails { get; set; } = true;
                    List<HealthCard> Cards { get; set; } = new();
                    string RequestSummary { get; } = "{{EscapeCSharpString(requestSummary, logger)}}";
                    string CouncilConsensus { get; } = "{{EscapeCSharpString(consensusSummary, logger)}}";

                    protected override Task OnInitializedAsync() => RefreshAsync();

                    Task RefreshAsync()
                    {
                        PanelVisible = true;
                        Cards =
                        [
                            new("AI Host", "Needs verification", "Check /__diag/council/models before selecting a model.", "Use CPU-only mode after a GPU driver reset."),
                            new("Blazor UI", "Prototype", "Add this page under Components/Pages, then add a NavMenu entry if the user approves integration.", "Uses @rendermode InteractiveServer and known DevExpress Blazor components."),
                            new("Download Route", "Ready", "Serve generated files through /__artifacts/council/{fileName}.", "Keep generated code sandboxed until the user explicitly permits integration.")
                        ];
                        PanelVisible = false;
                        return Task.CompletedTask;
                    }

                    sealed record HealthCard(string Area, string Status, string NextAction, string Detail);
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateBlazorDevExpressRazorExample");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate blazor support code operation.
        /// </summary>
        public string GenerateBlazorSupportCode(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string targetArea, ILogger logger)
        {
            try
            {
                return $$"""
                // <auto-generated>
                // LocalGPT AI Council Blazor support example.
                // </auto-generated>

                namespace LocalGPT.GeneratedExamples;

                public sealed record LocalGptGeneratedHealthCard(
                    string Area,
                    string Status,
                    string NextAction,
                    string Detail);

                public sealed class LocalGptGeneratedHealthSummaryService
                {
                    public const string TargetArea = "{{EscapeCSharpString(targetArea, logger)}}";
                    public const string CouncilMembers = "{{EscapeCSharpString(string.Join(", ", result.ModelNames), logger)}}";
                    public const string OriginalRequest = "{{EscapeCSharpString(TrimForCodeComment(request.Prompt, 900, logger), logger)}}";

                    public IReadOnlyList<LocalGptGeneratedHealthCard> GetCards()
                    {
                        return
                        [
                            new("AI Host", "Needs verification", "Call /__diag/council/models and keep unstable runs CPU-only.", "Do not assume Ollama or LM Studio is running until discovery confirms it."),
                            new("Blazor UI", "Prototype", "Generate a .razor page with @page, @rendermode InteractiveServer, and DevExpress controls.", "Prefer DxGrid, DxFormLayout, DxButton, DxCheckBox, DxMemo, and existing LocalGPT CSS classes."),
                            new("Sandbox", "Required", "Keep generated code downloadable until the user permits integration.", "Generated features must never self-expand into the real project without user approval.")
                        ];
                    }
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateBlazorSupportCode");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the generate code dom example operation.
        /// </summary>
        public string GenerateCodeDomExample(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string targetArea, ILogger logger)
        {
            try
            {
                var unit = new CodeCompileUnit();
                var namespaceDeclaration = new CodeNamespace("LocalGPT.GeneratedExamples");
                namespaceDeclaration.Imports.Add(new CodeNamespaceImport("System"));
                namespaceDeclaration.Imports.Add(new CodeNamespaceImport("System.Text"));
                unit.Namespaces.Add(namespaceDeclaration);

                var type = new CodeTypeDeclaration("CouncilFeatureRequestExample")
                {
                    IsClass = true,
                    TypeAttributes = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed
                };
                type.Comments.Add(new CodeCommentStatement("Generated by LocalGPT AI Council as a downloadable implementation sketch."));
                type.Comments.Add(new CodeCommentStatement("Treat this as a starter example, not as production code."));
                namespaceDeclaration.Types.Add(type);

                var privateConstructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Private
                };
                type.Members.Add(privateConstructor);

                type.Members.Add(CreateConstant("TargetArea", targetArea, logger));
                type.Members.Add(CreateConstant("CouncilMembers", string.Join(", ", result.ModelNames, logger), logger));
                type.Members.Add(CreateConstant("OriginalRequest", TrimForCodeComment(request.Prompt, 900, logger), logger));

                var method = new CodeMemberMethod
                {
                    Name = "BuildImplementationRequestMarkdown",
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    ReturnType = new CodeTypeReference(typeof(string))
                };
                method.Comments.Add(new CodeCommentStatement("This shape can be pasted into DXAiChat or an AI Council continuation round."));
                method.Statements.Add(new CodeVariableDeclarationStatement(typeof(StringBuilder), "builder", new CodeObjectCreateExpression(typeof(StringBuilder))));
                AppendLine(method, "# LocalGPT Implementation Request", logger);
                AppendLine(method, "", logger);
                AppendLine(method, $"Target area: {targetArea}", logger);
                AppendLine(method, $"Council members: {string.Join(", ", result.ModelNames)}", logger);
                AppendLine(method, "", logger);
                AppendLine(method, "## Requested feature", logger);
                AppendLine(method, TrimForCodeComment(request.Prompt, 1000, logger), logger);
                AppendLine(method, "", logger);
                AppendLine(method, "## Current council consensus", logger);
                AppendLine(method, TrimForCodeComment(result.FinalAnswer, 1600, logger), logger);
                AppendLine(method, "", logger);
                AppendLine(method, "## Implementation checklist", logger);
                AppendLine(method, "- Identify the owning LocalGPT service/page/project.", logger);
                AppendLine(method, "- Check /__diag/devexpress before proposing DevExpress APIs or UI components.", logger);
                AppendLine(method, "- Put DevExpress Office/report/PDF/export generation in ASP.NET Core backend services and expose safe download links.", logger);
                AppendLine(method, "- Keep native commands in backend services.", logger);
                AppendLine(method, "- Save user-visible state to EF/SQLite when it affects future chats.", logger);
                AppendLine(method, "- Prototype requested features in a harmless sandbox artifact or temporary workspace before integrating into the real project.", logger);
                AppendLine(method, "- Ask the user for explicit permission before integrating any generated expansion into LocalGPT.", logger);
                AppendLine(method, "- Never overrule a user decision that denies or limits self-expansion.", logger);
                AppendLine(method, "- List helpful official docs, examples, specs, or source repositories needed before implementation.", logger);
                AppendLine(method, "- Add a diagnostic endpoint or smoke path before relying on UI behavior.", logger);
                AppendLine(method, "- Mark unknown dependencies as Needs verification.", logger);
                method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("builder"), "ToString")));
                type.Members.Add(method);

                using var writer = new StringWriter();
                writer.WriteLine("// <auto-generated>");
                writer.WriteLine("// LocalGPT AI Council implementation example.");
                writer.WriteLine("// </auto-generated>");
                writer.WriteLine();

                using var provider = new CSharpCodeProvider();
                provider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions
                {
                    BracingStyle = "C",
                    BlankLinesBetweenMembers = true
                });

                return writer.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateBlazorSupportCode");
                return string.Empty;
            }

            
        }

        /// <summary>
        /// Creates constant.
        /// </summary>
        public CodeMemberField? CreateConstant(string name, string value, ILogger logger)
        {
            try
            {
                return new CodeMemberField(typeof(string), name)
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    InitExpression = new CodePrimitiveExpression(value)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateConstant name:{name} value:{value}");
                return null;
            }
        }
        /// <summary>
        /// Gets discovered model button text.
        /// </summary>
        public string GetDiscoveredModelButtonText(LocalAiModelInfo model, ILogger logger)
        {
            try
            {
                var state = model.IsLoaded ? "loaded" : "installed";
                return string.IsNullOrWhiteSpace(model.Details)
                    ? $"{model.Name} ({state})"
                    : $"{model.Name} ({state}, {model.Details})";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetDiscoveredModelButtonText model:{model.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Runs the append line operation.
        /// </summary>
        public void AppendLine(CodeMemberMethod method, string line, ILogger logger)
        {
            try
            {
                method.Statements.Add(new CodeMethodInvokeExpression(
                 /// <summary>
                 /// Runs the code variable reference expression operation.
                 /// </summary>
                 new CodeVariableReferenceExpression("builder"),
                 "AppendLine",
                 /// <summary>
                 /// Runs the code primitive expression operation.
                 /// </summary>
                 new CodePrimitiveExpression(line)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendLine method:{method.ToString()} line:{line}");
            }
        }

        /// <summary>
        /// Determines whether minecraft datapack artifact target.
        /// </summary>
        public bool? IsMinecraftDatapackArtifactTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var text = prompt;
                return patterns.MinecraftPattern.IsMatch(text) && patterns.DatapackPattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect whether the response targets a Minecraft datapack artifact.");
                return null;
            }
        }

        /// <summary>
        /// Determines whether minecraft skeleton matrix artifact target.
        /// </summary>
        public bool? IsMinecraftSkeletonMatrixArtifactTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var text = prompt;
                return patterns.MinecraftPattern.IsMatch(text) && patterns.MinecraftSkeletonMatrixPattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect whether the response targets a Minecraft loader matrix artifact.");
                return null;
            }
        }

        /// <summary>
        /// Runs the extract minecraft version operation.
        /// </summary>
        public string ExtractMinecraftVersion(string text, ILogger logger)
        {
            try
            {
                var match = patterns.MinecraftVersionPattern.Match(text);
                return match.Success
                    ? match.Groups["version"].Value
                    : catalog.DefaultMinecraftVersion;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractMinecraftVersion text:{text}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds minecraft datapack artifact identity.
        /// </summary>
        public MinecraftDatapackArtifactIdentity? BuildMinecraftDatapackArtifactIdentity(string text, string timestamp, ILogger logger)
        {
            try
            {
                var displayName = ExtractMinecraftProjectDisplayName(text,null, logger);
                var modId = ToMinecraftNamespace(displayName, logger);
                var projectName = ToPascalIdentifier(displayName, logger);
                if (string.IsNullOrWhiteSpace(projectName))
                    projectName = "PromptedDatapack";
                if (string.IsNullOrWhiteSpace(modId))
                    modId = "prompted_datapack";

                return new MinecraftDatapackArtifactIdentity(
                    $"{projectName}Council{timestamp.Replace("-", string.Empty, StringComparison.Ordinal)}",
                    modId,
                    $"com.localgpt.{modId.Replace("_", string.Empty, StringComparison.Ordinal)}",
                    displayName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildMinecraftDatapackArtifactIdentity text:{text} timestamp:{timestamp}");
                return null;
            }
        }
        /// <summary>
        /// Runs the extract minecraft project display name operation.
        /// </summary>
        public string ExtractMinecraftProjectDisplayName(string text, bool? harmonyModel, ILogger logger)
        {
            try
            {
                harmonyModel=harmonyModel ?? false;
                var quoted = patterns.MinecraftQuotedProjectNamePattern.Match(text);
                if (quoted.Success)
                    return CleanMinecraftProjectDisplayName(quoted.Groups["name"].Value, logger);

                var explicitlyNamed = patterns.MinecraftExplicitProjectNamePattern.Match(text);
                if (explicitlyNamed.Success)
                    return CleanMinecraftProjectDisplayName(explicitlyNamed.Groups["name"].Value, logger);

                var named = patterns.MinecraftNamedProjectPattern.Match(text);
                if (named.Success)
                    return CleanMinecraftProjectDisplayName(named.Groups["name"].Value, logger);

                var heading = patterns.MarkdownHeadingProjectNamePattern.Match(text);
                if (heading.Success)
                    return CleanMinecraftProjectDisplayName(heading.Groups["name"].Value, logger);

                return "Prompted Datapack";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractMinecraftProjectDisplayName text:{text}");
                return string.Empty;
            }
            
        }
        /// <summary>
        /// Runs the clean minecraft project display name operation.
        /// </summary>
        public string CleanMinecraftProjectDisplayName(string value, ILogger logger)
        {
            try
            {
                var trimmed = value.Trim();
                foreach (var separator in new[] { " with ", " for ", " and ", " that ", " the ", " zip ", " pack " })
                {
                    var index = trimmed.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
                    if (index > 2)
                        trimmed = trimmed[..index].Trim();
                }

                return string.IsNullOrWhiteSpace(trimmed) ? "Prompted Datapack" : trimmed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CleanMinecraftProjectDisplayName value:{value}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the to minecraft namespace operation.
        /// </summary>
        public string ToMinecraftNamespace(string value, ILogger logger)
        {
            try
            {
                var normalized = patterns.IdentifierSeparatorPattern.Replace(value.ToLowerInvariant(), "_").Trim('_');
                return string.IsNullOrWhiteSpace(normalized) ? "prompted_datapack" : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToMinecraftNamespace value:{value}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the to pascal identifier operation.
        /// </summary>
        public string ToPascalIdentifier(string value, ILogger logger)
        {
            try
            {
                var words = patterns.AlphaNumericWordPattern.Matches(value)
                .Select(match => match.Value)
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Take(5);
                return string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToPascalIdentifier value:{value}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the to kebab route operation.
        /// </summary>
        public string ToKebabRoute(string value, ILogger logger)
        {
            try
            {
                var normalized = patterns.IdentifierSeparatorPattern.Replace(value.ToLowerInvariant(), "-").Trim('-');
                return string.IsNullOrWhiteSpace(normalized) ? "promise-module" : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToKebabRoute value:{value}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate solution navigation razor operation.
        /// </summary>
        public string GenerateSolutionNavigationRazor(
             GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var labName = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI Host Control Plane",
                    _ => "LocalGPT Generation Lab"
                };
                var catalogHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/models",
                    _ => "/knowledge"
                };
                var catalogText = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Model Catalog",
                    _ => "Knowledge"
                };
                var detailHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/api-console",
                    _ => "/implementation-plan"
                };
                var detailText = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "API Console",
                    _ => "Implementation Plan"
                };
                var aiHostLinks = isAiHostLab
                    ? """
                    <a href="/chat">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Chat</span>
                    </a>
                    <a href="/running-models">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Running</span>
                    </a>
                    <a href="/model-downloads">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Downloads</span>
                    </a>
                    <a href="/templates">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Templates</span>
                    </a>
                    <a href="/hardware">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Hardware</span>
                    </a>
                    <a href="/runner-plugins">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Runner Plugins</span>
                    </a>
                    <a href="/logs">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Logs</span>
                    </a>
                    <a href="/settings">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Settings</span>
                    </a>
                """
                    : string.Empty;
                var archetypeLinks = archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt => """
                    <a href="/chat">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>DXAiChat</span>
                    </a>
                    <a href="/model-council">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>AI Council</span>
                    </a>
                    <a href="/database">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>SQLite</span>
                    </a>
                    <a href="/minecraft-mod-builder">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Minecraft</span>
                    </a>
                    <a href="/test-lab">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Test Lab</span>
                    </a>
                """,
                    GeneratedSolutionArchetype.TacosPortal => """
                    <a href="/telegram-ingestion">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Ingestion</span>
                    </a>
                    <a href="/persistence">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Persistence</span>
                    </a>
                    <a href="/workers">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Workers</span>
                    </a>
                    <a href="/admin">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Admin</span>
                    </a>
                    <a href="/client-shells">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Client Shells</span>
                    </a>
                """,
                    GeneratedSolutionArchetype.BotBackend => """
                    <a href="/webhooks">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Webhooks</span>
                    </a>
                    <a href="/conversations">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Conversations</span>
                    </a>
                    <a href="/bot-settings">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Bot Settings</span>
                    </a>
                    <a href="/python-interop">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Python Interop</span>
                    </a>
                """,
                    _ => string.Empty
                };
                var promiseLinks = BuildPromiseNavigationLinks(promiseModules, logger);

                return $$"""
                <nav class="generated-nav" aria-label="{{labName}} navigation">
                    <a class="generated-brand" href="/">{{labName}}</a>
                    <a href="/dashboard">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Dashboard</span>
                    </a>
                    <a href="{{catalogHref}}">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>{{catalogText}}</span>
                    </a>
                    <a href="{{detailHref}}">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>{{detailText}}</span>
                    </a>
                    <a href="/source-fidelity">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Source Fidelity</span>
                    </a>
                    {{aiHostLinks}}
                    {{archetypeLinks}}
                    {{promiseLinks}}
                </nav>

                @code {
                    [Parameter]
                    public bool IsAiHostLab { get; set; }
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionNavigationRazor archetype:{archetype.ToString()} promiseModules:{promiseModules.ToString()}", archetype, promiseModules);
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds promise navigation links.
        /// </summary>
        public string BuildPromiseNavigationLinks(IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                if (promiseModules.Count == 0)
                    return string.Empty;

                var builder = new StringBuilder();
                foreach (var module in promiseModules.Take(6))
                {
                    builder.AppendLine($$"""
                    <a href="{{module.Route}}">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>{{module.Title}}</span>
                    </a>
                """);
                }

                return builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildPromiseNavigationLinks promiseModules:{promiseModules.ToString()}", promiseModules);
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the generate solution index razor operation.
        /// </summary>
        public string GenerateSolutionIndexRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var isAiHostLiteral = isAiHostLab ? "true" : "false";
                var title = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI Host Control Plane",
                    GeneratedSolutionArchetype.LocalGpt => "LocalGPT Workbench",
                    GeneratedSolutionArchetype.TacosPortal => "TacosPortal Operations",
                    GeneratedSolutionArchetype.BotBackend => "Bot Backend Control Plane",
                    _ => "LocalGPT Feature Generation Lab"
                };
                var subtitle = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "A DevExpress Blazor shell for provider-compatible API routes, model cataloging, endpoint checks, and external runner boundaries.",
                    GeneratedSolutionArchetype.LocalGpt => "A local-first AI workbench with DXAiChat, AI Council, SQLite memory, artifact downloads, Minecraft generation, setup, and test-lab surfaces.",
                    GeneratedSolutionArchetype.TacosPortal => "A server-interactive operations portal with menu, orders, reservations, admin CRUD, notifications, and a simple bot backend boundary.",
                    GeneratedSolutionArchetype.BotBackend => "A compact bot backend with webhooks, conversation state, moderation/retry queues, Python interop boundaries, and operator settings.",
                    _ => "A LocalGPT/TacosPortalOpen-style sandbox for AI Council feature requests, implementation planning, knowledge-backed generation, and artifact review."
                };
                var primaryHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/api-console",
                    GeneratedSolutionArchetype.LocalGpt => "/chat",
                    GeneratedSolutionArchetype.TacosPortal => "/orders",
                    GeneratedSolutionArchetype.BotBackend => "/webhooks",
                    _ => "/implementation-plan"
                };
                var primaryLabel = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Open API console",
                    GeneratedSolutionArchetype.LocalGpt => "Open DXAiChat",
                    GeneratedSolutionArchetype.TacosPortal => "Open orders",
                    GeneratedSolutionArchetype.BotBackend => "Open webhooks",
                    _ => "Open implementation plan"
                };
                var secondaryHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/models",
                    GeneratedSolutionArchetype.LocalGpt => "/model-council",
                    GeneratedSolutionArchetype.TacosPortal => "/menu",
                    GeneratedSolutionArchetype.BotBackend => "/conversations",
                    _ => "/knowledge"
                };
                var secondaryLabel = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Review model catalog",
                    GeneratedSolutionArchetype.LocalGpt => "Review AI Council",
                    GeneratedSolutionArchetype.TacosPortal => "Review menu",
                    GeneratedSolutionArchetype.BotBackend => "Review conversations",
                    _ => "Review knowledge table"
                };
                var kicker = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI host lab",
                    _ => "LocalGPT lab"
                };
                var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 500, logger), logger);
                var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 700, logger), logger);

                return $$"""
                @page "/"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>{{title}}</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="{{isAiHostLiteral}}" />

                    <section class="generated-hero">
                        <div>
                            <p class="generated-kicker">{{kicker}}</p>
                            <h1>{{title}}</h1>
                            <p>{{subtitle}}</p>
                        </div>
                        <div class="generated-actions">
                            <DxButton Text="{{primaryLabel}}"
                                      NavigateUrl="{{primaryHref}}"
                                      RenderStyle="ButtonRenderStyle.Primary"
                                      RenderStyleMode="ButtonRenderStyleMode.Contained" />
                            <DxButton Text="{{secondaryLabel}}"
                                      NavigateUrl="{{secondaryHref}}"
                                      RenderStyle="ButtonRenderStyle.Secondary"
                                      RenderStyleMode="ButtonRenderStyleMode.Outline" />
                        </div>
                    </section>

                    <section class="generated-split">
                        <div>
                            <h2>What This Artifact Is</h2>
                            <DxGrid Data="@HealthService.GetCards()"
                                    KeyFieldName="@nameof(GeneratedHealthCard.Area)"
                                    CssClass="generated-grid compact"
                                    TextWrapEnabled="true">
                                <Columns>
                                    <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Area)" Caption="Area" />
                                    <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Status)" Caption="Status" />
                                    <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.NextAction)" Caption="Next Action" />
                                </Columns>
                            </DxGrid>
                        </div>
                        <div>
                            <h2>Original Council Context</h2>
                            <DxFormLayout CssClass="generated-form">
                                <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                                    <DxMemo Text="@RequestSummary" Rows="5" ReadOnly="true" />
                                </DxFormLayoutItem>
                                <DxFormLayoutItem Caption="Consensus" ColSpanMd="12">
                                    <DxMemo Text="@CouncilSummary" Rows="6" ReadOnly="true" />
                                </DxFormLayoutItem>
                            </DxFormLayout>
                        </div>
                    </section>
                </main>

                @code {
                    string RequestSummary { get; } = "{{requestSummary}}";
                    string CouncilSummary { get; } = "{{consensusSummary}}";
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionIndexRazor");
                return string.Empty;
            }
           
        }
        /// <summary>
        /// Runs the generate solution dashboard razor operation.
        /// </summary>
        public string GenerateSolutionDashboardRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var isAiHostLiteral = isAiHostLab ? "true" : "false";
                var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 700, logger), logger);
                var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 900, logger), logger);
                var title = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI Host Dashboard",
                    GeneratedSolutionArchetype.LocalGpt => "LocalGPT Workbench Dashboard",
                    GeneratedSolutionArchetype.TacosPortal => "TacosPortal Operations Dashboard",
                    GeneratedSolutionArchetype.BotBackend => "Bot Backend Dashboard",
                    _ => "LocalGPT Generation Dashboard"
                };
                var subtitle = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Track API compatibility, model catalog readiness, runner adapter boundaries, and endpoint-test status.",
                    GeneratedSolutionArchetype.LocalGpt => "Track model connectivity, Council health, SQLite memory, generated artifacts, Minecraft builder readiness, and frontend test status.",
                    GeneratedSolutionArchetype.TacosPortal => "Track order throughput, kitchen state, menu publishing, reservations, admin CRUD, and bot notification boundaries.",
                    GeneratedSolutionArchetype.BotBackend => "Track webhook health, queue state, conversation memory, retry policy, Python interop, and operator approvals.",
                    _ => "Track AI Council feature-generation readiness, knowledge grounding, artifact review, and integration safety."
                };
                return $$"""
            @page "/dashboard"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>{{title}}</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="{{isAiHostLiteral}}" />

                <section class="generated-header">
                    <div>
                        <h1>{{title}}</h1>
                        <p>{{subtitle}}</p>
                    </div>
                    <DxButton Text="Refresh"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="RefreshAsync" />
                </section>

                <DxGrid Data="@Cards"
                        KeyFieldName="@nameof(GeneratedHealthCard.Area)"
                        ShowSearchBox="true"
                        ShowFilterRow="true"
                        TextWrapEnabled="false"
                        CssClass="generated-grid">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.NextAction)" Caption="Next Action" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Detail)" Caption="Detail" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generation Context" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                            <DxMemo Text="@RequestSummary" Rows="4" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Council Output" ColSpanMd="12">
                            <DxMemo Text="@CouncilSummary" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedHealthCard> Cards { get; set; } = [];
                string RequestSummary { get; } = "{{requestSummary}}";
                string CouncilSummary { get; } = "{{consensusSummary}}";

                protected override Task OnInitializedAsync() => RefreshAsync();

                Task RefreshAsync()
                {
                    Cards = HealthService.GetCards();
                    return Task.CompletedTask;
                }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionDashboardRazor");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate solution knowledge table razor operation.
        /// </summary>
        public string GenerateSolutionKnowledgeTableRazor(bool isAiHostLab, ILogger logger)
        {
            try
            {
                if (isAiHostLab)
                {
                    return """
                @page "/models"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>AI Host Model Catalog</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="true" />

                    <h1>AI Host Model Catalog</h1>
                    <p class="generated-muted">Model rows are compatibility records for local model files and native runner readiness.</p>

                    <DxGrid Data="@HealthService.GetModelCatalog()"
                            ShowSearchBox="true"
                            CssClass="generated-grid">
                        <Columns>
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.Name)" Caption="Model or adapter" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.Status)" Caption="Status" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.SizeMegabytes)" Caption="Size MB" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.SupportsNativeInference)" Caption="Native inference" />
                        </Columns>
                    </DxGrid>
                </main>
                """;
                }

                return """
            @page "/knowledge"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Generation Knowledge</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="false" />

                <h1>Generation Knowledge</h1>
                <p class="generated-muted">This page demonstrates the knowledge-first path the LocalGPT AI Council should use before proposing integration.</p>

                <DxGrid Data="@HealthService.GetCards()"
                        ShowSearchBox="true"
                        CssClass="generated-grid">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Detail)" Caption="Detail" />
                    </Columns>
                </DxGrid>
            </main>
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionKnowledgeTableRazor isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
            
        }
 
        

        /// <summary>
        /// Runs the generate solution file operation.
        /// </summary>
        public string GenerateSolutionFile(string projectName, string projectGuid, ILogger logger)
        {
            try
            {
                return $$"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{projectName}}", "src\{{projectName}}\{{projectName}}.csproj", "{{projectGuid}}"
            EndProject
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            		Release|Any CPU = Release|Any CPU
            	EndGlobalSection
            	GlobalSection(ProjectConfigurationPlatforms) = postSolution
            		{{projectGuid}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{{projectGuid}}.Debug|Any CPU.Build.0 = Debug|Any CPU
            		{{projectGuid}}.Release|Any CPU.ActiveCfg = Release|Any CPU
            		{{projectGuid}}.Release|Any CPU.Build.0 = Release|Any CPU
            	EndGlobalSection
            EndGlobal
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionFile projectName:{projectName} projectGuid:{projectGuid}");
                return string.Empty;
            }

        }
            

       

        /// <summary>
        /// Runs the generate solution app settings operation.
        /// </summary>
        public string GenerateSolutionAppSettings(bool isAiHostLab,ILogger logger)
        {
            try
            {
                if (!isAiHostLab)
                {
                    return """
                {
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information"
                    }
                  }
                }
                """;
                }

                return """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              },
              "AiHost": {
                "DefaultModel": "gpt-oss:20b",
                "SafeStorageRoot": "%LOCALAPPDATA%/GeneratedAiHost",
                "PluginRoot": "plugins",
                "ModelsRoot": "%LOCALAPPDATA%/GeneratedAiHost/Models",
                "NativeRunnerExecutable": "",
                "EnableRunnerAutoDetect": true,
                "NativeRunnerInstallUrl": "https://github.com/ggml-org/llama.cpp/releases",
                "RunnerSearchRoots": [
                  "%LOCALAPPDATA%/LocalGPT/Runners",
                  "%LOCALAPPDATA%/Programs/Ollama",
                  "%PROGRAMFILES%/Ollama",
                  "%USERPROFILE%/.local/bin"
                ],
                "RunnerExecutableNames": [
                  "llama-cli.exe",
                  "llama-server.exe",
                  "ollama.exe",
                  "llama-cli",
                  "llama-server",
                  "ollama"
                ],
                "ModelSearchRoots": [
                  "%USERPROFILE%/.ollama/models",
                  "%LOCALAPPDATA%/LocalGPT/ModelFiles",
                  "%LOCALAPPDATA%/GeneratedAiHost/Models"
                ],
                "ContextTokens": 262144,
                "GpuLayers": 20,
                "MaxParallelModels": 2,
                "TargetGpuLoadPercent": 85,
                "AllowNativeRunner": false,
                "AllowPythonNet": false,
                "AllowPowerShellScripts": false,
                "AllowTypeScriptAdapters": false
              }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionAppSettings isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the generate solution program operation.
        /// </summary>
        public string GenerateSolutionProgram(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var aiHostServiceRegistrations = isAiHostLab
                ? """
                  builder.Services.Configure<AiHostRuntimeOptions>(builder.Configuration.GetSection("AiHost"));
                  builder.Services.AddSingleton<IModelCatalogService>(sp => sp.GetRequiredService<GeneratedHealthSummaryService>());
                  builder.Services.AddSingleton<IModelTransferService>(sp => sp.GetRequiredService<GeneratedHealthSummaryService>());
                  builder.Services.AddSingleton<IInferenceProvider, NativeModelFileInferenceProvider>();
                  builder.Services.AddSingleton<IInferenceRunner, NativeModelFileProcessRunner>();
                  builder.Services.AddSingleton<IPluginCatalogService, GeneratedPluginCatalogService>();
                  builder.Services.AddSingleton<IScriptExecutionService, PermissionGatedScriptExecutionService>();
                  builder.Services.AddSingleton<IHardwareBudgetService, GeneratedHardwareBudgetService>();
                  builder.Services.AddSingleton<IChatTemplateService, GeneratedChatTemplateService>();
                  """
                : string.Empty;

                var aiHostRoutes = isAiHostLab
                    ? """
                  app.MapGet("/api/version", () => new
                  {
                      version = "dotnet-lab-0.2",
                      source = "LocalGPT generated sandbox",
                      native_runner_contract = true,
                      upstream_proxy = false
                  });
                  app.MapGet("/api/tags", ([FromServices] IModelCatalogService catalog) => new { models = catalog.GetAiHostTags() });
                  app.MapGet("/api/ps", ([FromServices] IModelCatalogService catalog) => new { models = catalog.GetRunningModels() });
                  app.MapPost("/api/show", ([FromServices] IModelCatalogService catalog, [FromBody] GeneratedModelActionRequest request) => catalog.GetModelDetails(request));
                  app.MapPost("/api/pull", ([FromServices] IModelTransferService transfer, [FromBody] GeneratedModelActionRequest request) => transfer.CreatePullPlan(request));
                  app.MapPost("/api/push", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("push", request.Model));
                  app.MapPost("/api/create", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("create", request.Model));
                  app.MapPost("/api/copy", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelCopyRequest request) => service.CreateCopyPlan(request));
                  app.MapDelete("/api/delete", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("delete", request.Model));
                  app.MapPost("/api/generate", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedModelActionRequest request, CancellationToken cancellationToken) => await provider.GenerateAsync(request, cancellationToken).ConfigureAwait(false));
                  app.MapPost("/api/chat", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedChatRequest request, CancellationToken cancellationToken) => await provider.ChatAsync(request, cancellationToken).ConfigureAwait(false));
                  app.MapPost("/api/embed", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  app.MapPost("/api/embeddings", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  app.MapGet("/api/blobs/{digest}", (string digest) => Results.Json(new { digest, status = "planned", boundary = "Blob storage is represented as metadata only in this generated lab." }));
                  app.MapGet("/api/localgpt/runner/capability", async ([FromServices] IInferenceRunner runner, CancellationToken cancellationToken) => await runner.GetCapabilityAsync(cancellationToken).ConfigureAwait(false));
                  app.MapGet("/api/localgpt/plugins", ([FromServices] IPluginCatalogService plugins) => plugins.GetPlugins());
                  app.MapGet("/api/localgpt/hardware-budget", ([FromServices] IHardwareBudgetService hardware) => hardware.GetBudget());
                  app.MapGet("/api/localgpt/chat-templates", ([FromServices] IChatTemplateService templates) => templates.GetTemplateRules());
                  app.MapGet("/api/host/status", async ([FromServices] IInferenceRunner runner, [FromServices] IModelCatalogService catalog, [FromServices] IHardwareBudgetService hardware, CancellationToken cancellationToken) => new
                  {
                      runner = await runner.GetCapabilityAsync(cancellationToken).ConfigureAwait(false),
                      models = catalog.GetAiHostTags(),
                      running = catalog.GetRunningModels(),
                      hardware = hardware.GetBudget(),
                      upstream_proxy = false
                  });
                  app.MapPost("/api/localgpt/scripts/plan", ([FromServices] IScriptExecutionService scripts, [FromBody] GeneratedScriptPlanRequest request) => scripts.CreatePlan(request.ScriptKind, request.Target, request.UserApproved));
                  app.MapGet("/v1/models", ([FromServices] IModelCatalogService catalog) => new { data = catalog.GetAiHostTags() });
                  app.MapPost("/v1/chat/completions", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedChatRequest request, CancellationToken cancellationToken) => await provider.ChatAsync(request, cancellationToken).ConfigureAwait(false));
                  app.MapPost("/v1/embeddings", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  """
                    : string.Empty;

                return $$"""
            using DevExpress.Blazor;
            using Microsoft.AspNetCore.Mvc;
            using {{projectName}}.Components;
            using {{projectName}}.Models;
            using {{projectName}}.Services;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddDevExpressBlazor(options => options.SizeMode = SizeMode.Small);
            builder.Services.AddSingleton<GeneratedHealthSummaryService>();
            builder.Services.AddSingleton<ISourceFidelityService, GeneratedSourceFidelityService>();
            {{aiHostServiceRegistrations}}

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapGet("/__generated/health", (GeneratedHealthSummaryService service) => service.GetCards());
            {{aiHostRoutes}}
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionProgram projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the generate solution imports operation.
        /// </summary>
        public string GenerateSolutionImports(string projectName, ILogger logger)
        {
            try
            {
                return $$"""
            @using System.Net.Http
            @using Microsoft.AspNetCore.Components
            @using Microsoft.AspNetCore.Components.Forms
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using Microsoft.AspNetCore.Components.Web.Virtualization
            @using static Microsoft.AspNetCore.Components.Web.RenderMode
            @using Microsoft.JSInterop
            @using DevExpress.Blazor
            @using {{projectName}}
            @using {{projectName}}.Components
            @using {{projectName}}.Components.Pages
            @using {{projectName}}.Models
            @using {{projectName}}.Services
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionImports projectName:{projectName}");
                return string.Empty;
            }

        }
       
        /// <summary>
        /// Builds data content file name.
        /// </summary>
        public string BuildDataContentFileName(int index, string? mediaType, ILogger logger)
        {
            try
            {
                var extension = (mediaType ?? string.Empty).ToLowerInvariant() switch
                {
                    "application/zip" or "application/x-zip-compressed" => ".zip",
                    "application/json" => ".json",
                    "application/xml" or "text/xml" => ".xml",
                    "text/markdown" => ".md",
                    "text/css" => ".css",
                    "text/html" => ".html",
                    "text/javascript" or "application/javascript" => ".js",
                    "application/octet-stream" => ".bin",
                    _ => ".txt"
                };
                return $"dxaichat-upload-{index}{extension}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionImports index:{index} mediaType:{mediaType}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Attempts to get data content file name.
        /// </summary>
        public string? TryGetDataContentFileName(DataContent content, ILogger logger)
        {
            try
            {
                foreach (var key in new[] { "name", "fileName", "filename", "FileName", "Name" })
                {
                    if (content.AdditionalProperties?.TryGetValue(key, out var value) == true &&
                        value is not null &&
                        !string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        return value.ToString();
                    }
                }

                var rawName = content.RawRepresentation?
                    .GetType()
                    .GetProperty("Name")?
                    .GetValue(content.RawRepresentation)?
                    .ToString();
                return string.IsNullOrWhiteSpace(rawName) ? null : rawName;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "TryGetDataContentFileName");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the format live council running title operation.
        /// </summary>
        public string FormatLiveCouncilRunningTitle(string template, string runId, ILogger logger)
        {
            try
            {
                return (template ?? string.Empty).Replace("{id}", runId ?? string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not format the live Council running title.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the format live council elapsed status operation.
        /// </summary>
        public string FormatLiveCouncilElapsedStatus(string template, string elapsed, string status, ILogger logger)
        {
            try
            {
                return (template ?? string.Empty)
                    .Replace("{elapsed}", elapsed ?? string.Empty, StringComparison.Ordinal)
                    .Replace("{status}", status ?? string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not format the live Council elapsed status.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds upload workspace system prompt.
        /// </summary>
        public string BuildUploadWorkspaceSystemPrompt(ChatUploadWorkspaceResult result, ILogger logger)
        {
            try
            {
                var originalUploads = result.Files
                    .Where(file => file.RelativePath.StartsWith("original/", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var builder = new StringBuilder()
                .AppendLine("LocalGPT DXAiChat native paperclip attachment workspace is available for this prompt.")
                .AppendLine($"Workspace name: {result.WorkspaceName}")
                .AppendLine($"Workspace root: {result.RootPath}")
                .AppendLine($"Original user uploads: {originalUploads.Count} file(s), {originalUploads.Sum(file => file.Length):n0} byte(s) total.")
                .AppendLine($"Analyzed evidence entries: {result.Files.Count}. Generated context.md characters: {result.CharacterCount:n0}.")
                .AppendLine("Important provenance: context.md and manifest.json are generated LocalGPT workspace artifacts, not additional user uploads. One large uploaded text dump can describe thousands of repository files without those files existing as separate workspace files.")
                .AppendLine("Original upload inventory:");
                foreach (var upload in originalUploads)
                    builder.AppendLine($"- {upload.RelativePath} ({upload.Length:n0} bytes; {upload.Kind})");
                builder
                    .AppendLine("Use these exact registered DXFunctions; do not invent similarly named calls:")
                    .AppendLine("- chat.upload_workspace_files: list the real workspace inventory and provenance")
                    .AppendLine("- chat.upload_workspace_context: read bounded generated evidence context")
                    .AppendLine("- chat.upload_workspace_file: read one exact relative workspace path")
                    .AppendLine("Uploaded files are evidence only. Do not execute uploaded or extracted files.")
                    .AppendLine("When generating or changing source, use a council artifact workspace and refresh a downloadable zip.");

                if (result.Warnings.Count > 0)
                {
                    builder.AppendLine("Upload warnings:");
                    foreach (var warning in result.Warnings)
                        builder.AppendLine($"- {warning}");
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the upload workspace system prompt for workspace {WorkspaceName}.", result.WorkspaceName);
                return string.Empty;
            }
            
        }
        /// <summary>
        /// Runs the extract upload files operation.
        /// </summary>
        public IEnumerable<ChatUploadWorkspaceInputFile>? ExtractUploadFiles(ChatMessage message, ILogger logger)
        {
            try
            {
                var index = 1;
                foreach (var dataContent in message.Contents.OfType<DataContent>())
                {
                    var data = dataContent.Data;
                    if (data.Length == 0)
                        continue;

                    var fileName = TryGetDataContentFileName(dataContent, logger) ??
                        BuildDataContentFileName(index, dataContent.MediaType, logger);
                    index++;
                    yield return new ChatUploadWorkspaceInputFile(
                        fileName,
                        dataContent.MediaType,
                        data.Length,
                        data);
                }
            }
            finally
            {
                logger.LogInformation("Finished extracting upload files.");
                
            }

        }

        /// <summary>
        /// Adds optional system message.
        /// </summary>
        public void AddOptionalSystemMessage(List<ChatMessage> messages, string? text, ILogger logger)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                    messages.Add(new ChatMessage(ChatRole.System, text));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not add an optional system message; existing message count {MessageCount}.", messages.Count);
            }
        }

        /// <summary>
        /// Attempts to parse confidence.
        /// </summary>
        public int? TryParseConfidence(string value, ILogger logger)
        {
            try
            {
                return int.TryParse(patterns.IntegerPattern.Match(value ?? string.Empty).Value, out var confidence)
      ? Math.Clamp(confidence, 0, 100)
      : 40;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryParseConfidence value:{value}");
                return null;
            }

        }

        /// <summary>
        /// Runs the generate promise module razor operation.
        /// </summary>
        public string GeneratePromiseModuleRazor(GeneratedPromiseModule module, ILogger logger)
        {
            try
            {
                return GenerateArchetypePageRazor(module.Route, module.Title, module.Summary, module.Areas, logger);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GeneratePromiseModuleRazor module:{module.ToString()}");
                return string.Empty;
            }

        }

      
        /// <summary>
        /// Runs the merge tags operation.
        /// </summary>
        public string MergeTags(string requestedTags, string requiredTags, ILogger logger)
        {
            try
            {
                return string.IsNullOrWhiteSpace(requestedTags)
                ? requiredTags
                : $"{requestedTags.Trim()}; {requiredTags}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "MergeTags");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds capability gap knowledge content.
        /// </summary>
        public string BuildCapabilityGapKnowledgeContent(string body, ILogger logger)
        {
            try
            {
                var fields = new[]
           {
            "user-request-summary",
            "missing-capability",
            "owning-area",
            "target-deliverable",
            "requested-languages",
            "requested-frameworks",
            "requested-versions",
            "requested-domain-knowledge",
            "local-knowledge-sources",
            "external-knowledge-sources",
            "missing-localgpt-functions",
            "safe-workflow",
            "artifact-plan",
            "investigation-status",
            "next-localgpt-improvement"
        };

                var builder = new StringBuilder()
                    .AppendLine("Structured LocalGPT capability gap request:");

                foreach (var field in fields)
                {
                    var value = ExtractField(body, field, logger);
                    if (!string.IsNullOrWhiteSpace(value))
                        builder.Append("- ").Append(field).Append(": ").AppendLine(value);
                }

                return builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildCapabilityGapKnowledgeContent");
                return string.Empty;
            }
        }


        /// <summary>
        /// Parses knowledge requests.
        /// </summary>
        public IEnumerable<CouncilKnowledgeEntry>? ParseKnowledgeRequests(string source, string responseText, ILogger logger)
        {
            try
            {
                foreach (System.Text.RegularExpressions.Match match in patterns.KnowledgeBlockPattern.Matches(responseText))
                {
                    var body = match.Groups["body"].Value.Trim();
                    if (string.IsNullOrWhiteSpace(body))
                        continue;

                    var content = ExtractField(body, "content" , logger);
                    if (string.IsNullOrWhiteSpace(content))
                        content = body;

                    yield return new CouncilKnowledgeEntry
                    {
                        Topic = ExtractField(body, "topic", logger, "AI model knowledge request"),
                        Scope = ExtractField(body, "scope", logger, "DXAiChat"),
                        Source = $"AI model request: {source}",
                        Content = content,
                        HelpfulSources = ExtractField(body, "helpful-sources", logger, "None explicitly requested."),
                        Tags = MergeTags(ExtractField(body, "tags", logger), "model-written; unapproved", logger),
                        Confidence = TryParseConfidence(ExtractField(body, "confidence",  logger),logger) ?? 0,
                        VerificationStatus = "ModelSuggested",
                        ReviewStatus = "NeedsUserReview",
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                        IsUserApproved = false,
                        IsPinned = false,
                        IsArchived = false
                    };
                }

                foreach (System.Text.RegularExpressions.Match match in patterns.CapabilityGapBlockPattern.Matches(responseText))
                {
                    var body = match.Groups["body"].Value.Trim();
                    if (string.IsNullOrWhiteSpace(body))
                        continue;

                    var missingCapability = ExtractField(body, "missing-capability", logger, "LocalGPT capability gap request");
                    var owningArea = ExtractField(body, "owning-area", logger, "DXAiChat / AI Council");
                    var localSources = ExtractField(body, "local-knowledge-sources", logger, "None listed.");
                    var externalSources = ExtractField(body, "external-knowledge-sources", logger, "None listed.");

                    yield return new CouncilKnowledgeEntry
                    {
                        Topic = missingCapability,
                        Scope = owningArea,
                        Source = $"AI capability gap request: {source}",
                        Content = BuildCapabilityGapKnowledgeContent(body, logger),
                        HelpfulSources = $"Local sources:\n{localSources}\n\nExternal sources:\n{externalSources}",
                        Tags = MergeTags(ExtractField(body, "tags", logger), "capability-gap; model-written; unapproved", logger),
                        Confidence = TryParseConfidence(ExtractField(body, "confidence",  logger),logger) ?? 0,
                        VerificationStatus = "ModelSuggested",
                        ReviewStatus = "NeedsDiagnosticVerification",
                        ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                        StalenessReason = "Capability gap request needs human or diagnostic verification before it becomes trusted guidance.",
                        StalenessDetectedBy = "DXAiChat capability-gap parser",
                        IsUserApproved = false,
                        IsPinned = false,
                        IsArchived = false
                    };
                }
            }
            finally
            {
                logger.LogDebug("Finished parsing knowledge requests for source {Source}.", source);
            }
        }
        /// <summary>
        /// Runs the extract field operation.
        /// </summary>
        public string ExtractField(string body, string name,  ILogger logger, string fallback = "")
        {
            try
            {
                return patterns.ExtractStructuredField(body, name) ?? fallback;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not extract a named field; source content was omitted from logs.");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the fallback operation.
        /// </summary>
        public string Fallback(string value, string fallback, ILogger logger)
        {
            try
            {
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not select a fallback value; source content was omitted from logs.");
                return string.Empty;
            }
          
        }

        /// <summary>
        /// Parses nullable guid.
        /// </summary>
        public Guid? ParseNullableGuid(string value, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return Guid.TryParse(value, out var parsed) ? parsed : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ParseNullableGuid value:{value}");
                return null;
            }

        }

        /// <summary>
        /// Runs the format nullable UTC operation.
        /// </summary>
        public string FormatNullableUtc(DateTime? value, ILogger logger)
        {
            try
            {
                return value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatNullableUtc value:{value}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Runs the format nullable guid operation.
        /// </summary>
        public string FormatNullableGuid(Guid? value, ILogger logger)
        {
            try
            {
                return value?.ToString("D") ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatNullableGuid value:{value}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Parses nullable UTC.
        /// </summary>
        public DateTime? ParseNullableUtc(string value, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed)
                    ? parsed
                    : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ParseNullableUtc value:{value}");
                return null;
            }
          
        }

        /// <summary>
        /// Creates message signature.
        /// </summary>
        public string CreateMessageSignature(IEnumerable<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                return string.Join("|", messages
               .Where(message => !message.Typing)
               .Select(message => $"{message.Role}:{message.Content.GetHashCode(StringComparison.Ordinal)}:{message.Content.Length}"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create a message signature.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs the detect target area operation.
        /// </summary>
        public string DetectTargetArea(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var text = $"{prompt} {finalAnswer}";
                if (patterns.DevExpressDocumentPattern.IsMatch(text))
                    return "DevExpress document/report backend";
                if (patterns.BlazorFrontendPattern.IsMatch(text))
                    return "Blazor/DevExpress frontend";
                if (patterns.DotNetPattern.IsMatch(text))
                    return ".NET/Blazor/ASP.NET Core";
                if (patterns.MinecraftPattern.IsMatch(text))
                    return "Minecraft builder";
                if (patterns.FrontendPattern.IsMatch(text))
                    return "Blazor frontend";
                if (patterns.LoggingPattern.IsMatch(text))
                    return "diagnostics and logging";

                return "LocalGPT feature";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect the target area for generated artifacts.");
                return string.Empty;
            }

        }

        /// <summary>
        /// Runs the trim for code comment operation.
        /// </summary>
        public string TrimForCodeComment(string text, int maxLength, ILogger logger)
        {
            try
            {
                var normalized = patterns.WhitespacePattern.Replace(text, " ").Trim();
                return normalized.Length <= maxLength
                    ? normalized
                    : $"{normalized[..maxLength].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimForCodeComment text:{text} maxLength:{maxLength}");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Runs the escape csharp string operation.
        /// </summary>
        public string EscapeCSharpString(string text, ILogger logger)
        {
            try
            {
                return text
              .Replace("\\", "\\\\", StringComparison.Ordinal)
              .Replace("\"", "\\\"", StringComparison.Ordinal)
              .Replace("\r", "\\r", StringComparison.Ordinal)
              .Replace("\n", "\\n", StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EscapeCSharpString text:{text}");
                return string.Empty;
            }
          
        }

        /// <summary>
        /// Runs the escape JSON string operation.
        /// </summary>
        public string EscapeJsonString(string text, ILogger logger)
        {
            try
            {
                return EscapeCSharpString(text, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EscapeJsonString text:{text}");
                return string.Empty;
            }
           
        }
        /// <summary>
        /// Normalizes dbnull string value.
        /// </summary>
        public string? NormalizeDBNullStringValue(string value, ILogger logger)
        {
            try
            {
                return value.Equals("[null]", StringComparison.OrdinalIgnoreCase) ? null : value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeDBNullStringValue value:{value}");
                return null;
            }
        }

        /// <summary>
        /// Runs the trim endpoint operation.
        /// </summary>
        public string TrimEndpoint(string endpoint, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                    return "unknown endpoint";

                return endpoint
                    .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .TrimEnd('/');
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimEndpoint endpoint:{endpoint}");
                return string.Empty;
            }
           
        }
        /// <summary>
        /// Runs the trim for knowledge operation.
        /// </summary>
        public string TrimForKnowledge(string text, int maxLength, ILogger logger)
        {
            try
            {
                var normalized = patterns.WhitespacePattern.Replace(text ?? string.Empty, " ").Trim();
                return normalized.Length <= maxLength
                    ? normalized
                    : $"{normalized[..maxLength].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TrimForKnowledge {ex.ToString()} text {text?.ToString()} maxLength {maxLength.ToString()}");
                return string.Empty;
            }
        }


        /// <summary>
        /// Creates minecraft system prompt.
        /// </summary>
        public string CreateMinecraftSystemPrompt(string mode, ILogger logger)
        {
            try
            {
                return string.Join(Environment.NewLine, new[]
{
        $"You are a senior Minecraft Java mod engineer helping through LocalGPT in {mode}.",
        "Prefer Java Edition first. Treat Bedrock as a separate behavior/resource pack exporter.",
        "For Java code work, choose Fabric mod, NeoForge mod, or Paper plugin. For command-only vanilla systems, choose datapack.",
        "For current Minecraft Java 26.x datapacks and Java mod/plugin planning, expect Java 25 unless the target version is explicitly older.",
        "For older 1.21.x Java mods/plugins, JDK 21 remains a useful compatibility target.",
        "Produce buildable, practical implementation plans with exact files, classes, registry steps, assets, data generation, and Gradle commands.",
        "For datapacks, produce pack.mcmeta, minecraft load/tick function tags, namespace functions, validation steps, and install instructions.",
        "Help the user set up their system when tooling is missing.",
        "If LocalGPT needs a missing feature, include a 'Missing feature report' section that can be saved to memory.",
        "Label uncertain dependency versions under 'Needs verification'."
    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateMinecraftSystemPrompt");
                return string.Empty;
            }

        }
    

        /// <summary>
        /// Runs the minecraft datapack version info resolve operation.
        /// </summary>
        public MinecraftDatapackVersionInfo MinecraftDatapackVersionInfoResolve(string? minecraftVersion, ILogger logger)
        {
            try
            {
                var requested = string.IsNullOrWhiteSpace(minecraftVersion)
                ? catalog.DefaultMinecraftVersion
                : minecraftVersion.Trim();
                var knownVersions = MinecraftDatapackVersionKnownVersions(logger);
                var exact = knownVersions.FirstOrDefault(item =>
                    requested.Equals(item.MatchedVersion, StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                    return exact with { RequestedVersion = requested };

                var prefix = knownVersions
                    .Where(item => requested.StartsWith(item.MatchedVersion, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.MatchedVersion.Length)
                    .FirstOrDefault();
                if (prefix is not null)
                    return prefix with { RequestedVersion = requested, IsExactMatch = false, NeedsVerification = true, Notes = $"{prefix.Notes} Version matched by prefix; verify against the official Minecraft version manifest before friend testing." };

                var fallback = requested.StartsWith("26.", StringComparison.OrdinalIgnoreCase)
                    ? knownVersions.First(item => item.MatchedVersion == catalog.DefaultMinecraftVersion)
                    : requested.StartsWith("1.21", StringComparison.OrdinalIgnoreCase)
                    ? knownVersions.First(item => item.MatchedVersion == "1.21.4")
                    : knownVersions.First(item => item.MatchedVersion == catalog.DefaultMinecraftVersion);

                return fallback with
                {
                    RequestedVersion = requested,
                    IsExactMatch = false,
                    NeedsVerification = true,
                    Notes = $"No exact LocalGPT mapping for Minecraft {requested}. Using {fallback.MatchedVersion} as a cautious fallback; verify pack_format with the official version manifest."
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not resolve datapack metadata for Minecraft version {MinecraftVersion}.", minecraftVersion);
                var requested = string.IsNullOrWhiteSpace(minecraftVersion) ? catalog.DefaultMinecraftVersion : minecraftVersion.Trim();
                return new MinecraftDatapackVersionInfo(
                    RequestedVersion: requested,
                    MatchedVersion: catalog.DefaultMinecraftVersion,
                    PackFormat: "101.1",
                    FunctionRegistryFolder: "function",
                    IsExactMatch: false,
                    NeedsVerification: true,
                    Notes: "LocalGPT used a defensive datapack fallback. Verify pack_format before release.",
                    Source: "LocalGPT defensive fallback after version-resolution failure.");
            }
        }

        /// <summary>
        /// Runs the minecraft datapack version known versions operation.
        /// </summary>
        public List< MinecraftDatapackVersionInfo> MinecraftDatapackVersionKnownVersions (ILogger logger)
        {
    try
    {
                try
                {
                    return new()
                    {
                             MinecraftDatapackVersionInfoKnown("26.2", "105.0", "function", "Minecraft Java 26.2 snapshot family. Use only for snapshot worlds and verify against the installed launcher build.",logger),
            MinecraftDatapackVersionInfoKnown("26.2-snapshot-6", "105.0", "function", "Minecraft Java 26.2 Snapshot 6 datapack format.",logger),
            MinecraftDatapackVersionInfoKnown("26.1.2", "101.1", "function", "Minecraft Java 26.1 stable family; Java 25 runtime required.",logger),
            MinecraftDatapackVersionInfoKnown("26.1.1", "101.1", "function", "Minecraft Java 26.1 stable family; Java 25 runtime required.",logger),
            MinecraftDatapackVersionInfoKnown("26.1", "101.1", "function", "Minecraft Java 26.1 stable family; Java 25 runtime required.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.4", 61.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "LocalGPT Living Cities benchmark target.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.3", 57.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21.2/1.21.3 datapack format family.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.2", 57.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21.2/1.21.3 datapack format family.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.1", 48.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21/1.21.1 datapack format family.",logger),
            MinecraftDatapackVersionInfoKnown("1.21",48.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21/1.21.1 datapack format family.",logger)
                    };
                }
                catch (Exception)
                {
                    return new();
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(MinecraftDatapackVersionKnownVersions)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilTextService)}.{nameof(MinecraftDatapackVersionKnownVersions)} failed.");
        throw;
    }
}

        /// <summary>
        /// Runs the minecraft datapack version info known operation.
        /// </summary>
        public MinecraftDatapackVersionInfo MinecraftDatapackVersionInfoKnown(string version, string packFormat, string functionRegistryFolder, string notes, ILogger logger) 
        {
            try
            {
                return new(
                RequestedVersion: version,
                MatchedVersion: version,
                PackFormat: packFormat,
                FunctionRegistryFolder: functionRegistryFolder,
                IsExactMatch: true,
                NeedsVerification: false,
                Notes: notes,
                Source: "LocalGPT curated datapack version catalog; verify unknown versions with the official Minecraft version manifest.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create curated datapack metadata for version {Version}.", version);
                return new MinecraftDatapackVersionInfo(
                    RequestedVersion: version ?? string.Empty,
                    MatchedVersion: version ?? string.Empty,
                    PackFormat: string.IsNullOrWhiteSpace(packFormat) ? "unknown" : packFormat,
                    FunctionRegistryFolder: string.IsNullOrWhiteSpace(functionRegistryFolder) ? "function" : functionRegistryFolder,
                    IsExactMatch: false,
                    NeedsVerification: true,
                    Notes: string.IsNullOrWhiteSpace(notes) ? "Curated datapack metadata requires verification." : notes,
                    Source: "LocalGPT defensive fallback after curated metadata construction failure.");
            }
        }

        /// <summary>
        /// Runs the enumerate nested architecture roots operation.
        /// </summary>
        public IEnumerable<string> EnumerateNestedArchitectureRoots(string rootPath, ILogger logger)
        {
            try
            {
                var stack = new Stack<DirectoryInfo>(SafeEnumerateDirectoryInfos(rootPath, logger).Reverse());
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (catalog.ExcludedDirectoryNames.Contains(current.Name))
                        continue;

                    if (LooksLikeArchitectureRoot(current.FullName, logger))
                        yield return current.FullName;

                    foreach (var child in SafeEnumerateDirectoryInfos(current.FullName, logger).Reverse())
                        stack.Push(child);
                }
            }
            finally
            {
                logger.LogInformation($"Ended EnumerateNestedArchitectureRoots rootPath {rootPath?.ToString()}");
            }
        }

        /// <summary>
        /// Runs the safe enumerate directories operation.
        /// </summary>
        public IEnumerable<string> SafeEnumerateDirectories(string rootPath, ILogger logger)
        {
            try
            {
                return Directory.EnumerateDirectories(rootPath).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SafeEnumerateDirectories rootPath {rootPath?.ToString()}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Runs the safe enumerate directory infos operation.
        /// </summary>
        public IEnumerable<DirectoryInfo> SafeEnumerateDirectoryInfos(string rootPath, ILogger logger)
        {
            try
            {
                return new DirectoryInfo(rootPath)
                    .EnumerateDirectories()
                    .Where(directory => !catalog.ExcludedDirectoryNames.Contains(directory.Name))
                    .OrderBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SafeEnumerateDirectoryInfos rootPath {rootPath?.ToString()}");
                return new List<DirectoryInfo>();
            }
        }

        /// <summary>
        /// Runs the looks like architecture root operation.
        /// </summary>
        public bool LooksLikeArchitectureRoot(string rootPath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(rootPath);
                if (!directory.Exists)
                    return false;

                if (directory.GetFiles().Any(file => IsProjectRootFile(file.Name, file.Extension, logger)))
                    return true;

                var childNames = directory.GetDirectories()
                    .Select(child => child.Name)
                    .ToArray();
                var distinctiveChildren = childNames.Count(name =>
                    name.Equals("api", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("server", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("llm", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("runner", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("manifest", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BlazorDemo.ServerSide", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BlazorDemo.Wasm", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("VideoShredGUI", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("python-midi", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("JessiferBlazorWASM", StringComparison.OrdinalIgnoreCase));
                return distinctiveChildren >= 2;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikeArchitectureRoot rootPath {rootPath?.ToString()}");
                return false;
            }

        }

        /// <summary>
        /// Runs the sanitize for prompt operation.
        /// </summary>
        public string SanitizeForPrompt(string text, ILogger logger)
        {
            try
            {
                var userName = Environment.UserName;
                if (!string.IsNullOrWhiteSpace(userName))
                    text = text.Replace(userName, "%USER%", StringComparison.OrdinalIgnoreCase);

                return text.Replace("\0", string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not sanitize text for a prompt.");
                return string.Empty;
            }
        }
}
}
