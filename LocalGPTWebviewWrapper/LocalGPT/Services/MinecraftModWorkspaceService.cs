using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public partial class MinecraftModWorkspaceService(ILogger<MinecraftModWorkspaceService> logger) : IMinecraftModWorkspaceService
    {
        public string WorkspaceRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "MinecraftModWorkspaces");

        public async Task<MinecraftModWorkspace> CreateFabricWorkspaceAsync(MinecraftModBuildRequest request, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(WorkspaceRoot);

            var projectName = NormalizeName(request.ProjectName, "GeneratedMinecraftMod");
            var modId = NormalizeModId(request.ModId, "generated_mod");
            var packageName = NormalizePackageName(request.PackageName);
            var projectRoot = GetUniqueProjectPath(projectName);
            var mainClassName = ToPascalCase(modId) + "Mod";
            var packagePath = packageName.Replace('.', Path.DirectorySeparatorChar);
            var javaRoot = Path.Combine(projectRoot, "src", "main", "java", packagePath);
            var resourceRoot = Path.Combine(projectRoot, "src", "main", "resources");

            Directory.CreateDirectory(javaRoot);
            Directory.CreateDirectory(resourceRoot);

            var settingsPath = Path.Combine(projectRoot, "settings.gradle");
            var buildPath = Path.Combine(projectRoot, "build.gradle");
            var mainClassPath = Path.Combine(javaRoot, $"{mainClassName}.java");
            var metadataPath = Path.Combine(resourceRoot, "fabric.mod.json");
            var readmePath = Path.Combine(projectRoot, "LOCALGPT_MOD_BRIEF.md");

            await File.WriteAllTextAsync(settingsPath, CreateSettingsGradle(projectName), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(buildPath, CreateBuildGradle(request, modId), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(mainClassPath, CreateMainClass(packageName, mainClassName, modId), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(metadataPath, CreateFabricMetadata(request, packageName, mainClassName, modId), Encoding.UTF8, cancellationToken);
            await File.WriteAllTextAsync(readmePath, CreateBrief(request), Encoding.UTF8, cancellationToken);

            logger.LogInformation("Created Minecraft mod workspace at {ProjectRoot}", projectRoot);

            return new MinecraftModWorkspace
            {
                ProjectName = projectName,
                RootPath = projectRoot,
                MainClassPath = mainClassPath,
                MetadataPath = metadataPath
            };
        }

        public bool IsPathInsideWorkspaceRoot(string path)
        {
            var root = Path.GetFullPath(WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
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

            return parts.Length == 0 ? "com.localgpt.generatedmod" : string.Join(".", parts);
        }

        private static string ToPascalCase(string value)
        {
            var words = value.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join("", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }

        private static string CreateSettingsGradle(string projectName) =>
            $$"""
            pluginManagement {
                repositories {
                    maven { url = 'https://maven.fabricmc.net/' }
                    gradlePluginPortal()
                    mavenCentral()
                }
            }

            dependencyResolutionManagement {
                repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
                repositories {
                    maven { url = 'https://maven.fabricmc.net/' }
                    mavenCentral()
                }
            }

            rootProject.name = '{{projectName}}'
            """;

        private static string CreateBuildGradle(MinecraftModBuildRequest request, string modId) =>
            $$"""
            plugins {
                id 'fabric-loom' version '1.7-SNAPSHOT'
                id 'maven-publish'
            }

            version = '1.0.0'
            group = '{{NormalizePackageName(request.PackageName)}}'

            base {
                archivesName = '{{modId}}'
            }

            dependencies {
                minecraft 'com.mojang:minecraft:{{request.MinecraftVersion}}'
                mappings 'net.fabricmc:yarn:{{request.MinecraftVersion}}+build.1:v2'
                modImplementation 'net.fabricmc:fabric-loader:0.16.9'
                modImplementation 'net.fabricmc.fabric-api:fabric-api:0.110.0+{{request.MinecraftVersion}}'
            }

            java {
                toolchain {
                    languageVersion = JavaLanguageVersion.of(21)
                }
                withSourcesJar()
            }
            """;

        private static string CreateMainClass(string packageName, string mainClassName, string modId) =>
            $$"""
            package {{packageName}};

            import net.fabricmc.api.ModInitializer;
            import org.slf4j.Logger;
            import org.slf4j.LoggerFactory;

            public class {{mainClassName}} implements ModInitializer {
                public static final String MOD_ID = "{{modId}}";
                public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

                @Override
                public void onInitialize() {
                    LOGGER.info("LocalGPT generated mod loaded: {}", MOD_ID);
                }
            }
            """;

        private static string CreateFabricMetadata(MinecraftModBuildRequest request, string packageName, string mainClassName, string modId) =>
            $$"""
            {
              "schemaVersion": 1,
              "id": "{{modId}}",
              "version": "1.0.0",
              "name": "{{request.ProjectName}}",
              "description": "{{request.Description.Replace("\"", "\\\"")}}",
              "authors": [
                "LocalGPT"
              ],
              "environment": "*",
              "entrypoints": {
                "main": [
                  "{{packageName}}.{{mainClassName}}"
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

        private static string CreateBrief(MinecraftModBuildRequest request) =>
            $"""
            # LocalGPT Minecraft Mod Brief

            Loader: {request.Loader}
            Minecraft version: {request.MinecraftVersion}
            Package: {request.PackageName}

            ## User Request

            {request.Description}
            """;

        [GeneratedRegex("[^a-zA-Z0-9_.-]")]
        private static partial Regex NameCleaner();

        [GeneratedRegex("[^a-z0-9_]")]
        private static partial Regex ModIdCleaner();

        [GeneratedRegex("[^a-z0-9_]")]
        private static partial Regex PackagePartCleaner();
    }
}
