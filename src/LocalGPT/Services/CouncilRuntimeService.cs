using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Owns deterministic Council prompt reconstruction, source/artifact generation helpers and shared runtime formatting operations.
    /// </summary>
    /// <param name="text">Provides maintained normalization and source-generation text operations.</param>
    /// <param name="catalog">Provides maintained runtime limits and defaults.</param>
    /// <param name="serviceLogger">Writes bounded runtime diagnostics.</param>
    [DocumentationUpdated("2.1.20")]
    public sealed class CouncilRuntimeService(CouncilTextService text, LocalGptCatalogService catalog, ILogger<CouncilRuntimeService> serviceLogger)
    {
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly ConcurrentDictionary<string, byte> ollamaModelsWithoutNativeToolMetadata = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Runs the ollama thinking chat client should skip native tools operation.
        /// </summary>
        public bool OllamaThinkingChatClientShouldSkipNativeTools(Uri? endpoint, string modelName, ILogger logger)
        {
            try
            {
                if (endpoint is null || string.IsNullOrWhiteSpace(modelName))
                    return false;
                var key = $"{endpoint.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                return ollamaModelsWithoutNativeToolMetadata.ContainsKey(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not inspect the process-local Ollama native-tool compatibility cache.");
                return false;
            }
        }

        /// <summary>
        /// Runs the ollama thinking chat client remember native tools rejected operation.
        /// </summary>
        public void OllamaThinkingChatClientRememberNativeToolsRejected(Uri? endpoint, string modelName, ILogger logger)
        {
            try
            {
                if (endpoint is null || string.IsNullOrWhiteSpace(modelName))
                    return;
                var key = $"{endpoint.GetLeftPart(UriPartial.Authority).TrimEnd('/')}|{modelName.Trim()}";
                ollamaModelsWithoutNativeToolMetadata[key] = 1;
                logger.LogInformation(
                    "Remembered for this LocalGPT process that Ollama model {Model} at {Endpoint} rejects native tool metadata; later requests will skip the known-incompatible metadata instead of repeating HTTP 400/501 probing.",
                    modelName,
                    endpoint.GetLeftPart(UriPartial.Authority));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not update the process-local Ollama native-tool compatibility cache for model {Model}.", modelName);
                throw;
            }
        }
        /// <summary>Creates Gradle properties for a Paper server plugin workspace.</summary>
        /// <param name="request">Minecraft build request containing target versions and description.</param>
        /// <param name="context">Normalized generated-project context.</param>
        /// <param name="logger">Writes bounded generation diagnostics.</param>
        /// <returns>The generated Gradle properties text, or an empty string when generation fails.</returns>
        public string CreatePaperGradleProperties(MinecraftModBuildRequest request, WorkspaceContext context, ILogger logger)
        {
            try
            {
                serviceLogger.LogTrace("Council runtime operation {Operation} started.", nameof(CreatePaperGradleProperties));
                var versions = ResolveDependencyVersions(request, logger, "Paper");
                return $$"""
            org.gradle.jvmargs=-Xmx2G
            org.gradle.daemon=false
            org.gradle.parallel=false

            paper_api_version={{versions.PaperApiVersion}}
            plugin_id={{context.ModId}}
            plugin_name={{context.ProjectName}}
            plugin_version=0.1.0
            plugin_main={{context.PackageName}}.{{context.MainClassName}}
            plugin_authors=Generated with LocalGPT
            plugin_description={{text.NormalizeDescription(request.Description)}}
            maven_group={{context.PackageName}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreatePaperGradleProperties");
                return string.Empty;
            }
        }

        /// <summary>Executes the create common gradle properties operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="context">Input value for context.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string CreateCommonGradleProperties(MinecraftModBuildRequest request, WorkspaceContext context, ILogger logger)
        {
            try
            {
                var versions = ResolveDependencyVersions(request, logger);
                var fabricApiVersion = versions.FabricApiVersion ?? ResolveMinecraftDependencyVersionInfo("Fabric", request.MinecraftVersion, logger, request.JavaVersion, request.GradleVersion)
                    .FabricApiVersion;
                var neoForgeVersion = versions.NeoForgeVersion ?? ResolveMinecraftDependencyVersionInfo("NeoForge", request.MinecraftVersion, logger, request.JavaVersion, request.GradleVersion)
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
            mod_authors=Generated with LocalGPT
            mod_description={{text.NormalizeDescription(request.Description)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateCommonGradleProperties");
                return string.Empty;
            }
        }
        /// <summary>Executes the resolve dependency versions operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="loaderOverride">Input value for loaderOverride.</param>
        /// <returns>The operation result.</returns>
        public MinecraftDependencyVersionInfo ResolveDependencyVersions(
          MinecraftModBuildRequest request, 
          ILogger logger,
          string? loaderOverride = null) 
        {
            try
            {
                return ResolveMinecraftDependencyVersionInfo(
              loaderOverride ?? request.Loader,
              request.MinecraftVersion, 
              logger,
              request.JavaVersion,
              request.GradleVersion);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ResolveDependencyVersions");
                return CreateFallbackDependencyVersionInfo(request, loaderOverride, ex.Message);
            }
        }
          

        /// <summary>Executes the create fallback dependency version info operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="loaderOverride">Input value for loaderOverride.</param>
        /// <param name="failureReason">Input value for failureReason.</param>
        /// <returns>The operation result.</returns>
        private MinecraftDependencyVersionInfo CreateFallbackDependencyVersionInfo(
            MinecraftModBuildRequest request,
            string? loaderOverride,
            string? failureReason)
        {
    try
    {
                var loader = string.IsNullOrWhiteSpace(loaderOverride) ? request.Loader : loaderOverride;
                var normalizedLoader = string.IsNullOrWhiteSpace(loader) ? "Fabric" : loader.Trim();
                var minecraftVersion = string.IsNullOrWhiteSpace(request.MinecraftVersion) ? catalog.DefaultMinecraftVersion : request.MinecraftVersion.Trim();
                var javaVersion = string.IsNullOrWhiteSpace(request.JavaVersion) ? catalog.DefaultJavaVersion : request.JavaVersion.Trim();
                var gradleVersion = string.IsNullOrWhiteSpace(request.GradleVersion) ? catalog.DefaultGradleVersion : request.GradleVersion.Trim();
                return new MinecraftDependencyVersionInfo(
                    Loader: normalizedLoader,
                    RequestedMinecraftVersion: minecraftVersion,
                    MatchedMinecraftVersion: minecraftVersion,
                    JavaVersion: javaVersion,
                    GradleVersion: gradleVersion,
                    "",
                    FabricApiVersion: null,
                    NeoForgeVersion: null,
                    PaperApiVersion: null,
                    DatapackPackFormat: normalizedLoader.Equals("Datapack", StringComparison.OrdinalIgnoreCase) ? "101.1" : null,
                    IsExactMatch: false,
                    NeedsVerification: true,
                    Notes: $"Dependency resolution failed and a conservative project-local fallback was used. {failureReason}".Trim(),
                    Source: "LocalGPT defensive fallback; verify versions with the official loader and Minecraft sources before release.");
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(CreateFallbackDependencyVersionInfo)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(CreateFallbackDependencyVersionInfo)} failed.");
        throw;
    }
}

        /// <summary>Executes the create build local script operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string CreateBuildLocalScript(MinecraftModBuildRequest request, ILogger logger)
        {
            try
            {
                var versions = ResolveDependencyVersions(request,logger);
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
                throw "JDK 21 was not found. Install Microsoft.OpenJDK.21 or run src\build\Setup-MinecraftModToolchain.ps1 -Install."
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

            throw "Gradle {{gradleVersion}} was not found. Run LocalGPT\build\Setup-MinecraftModToolchain.ps1 -InstallGradle."
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateBuildLocalScript");
                return string.Empty;
            }
        }
        /// <summary>Executes the create datapack mcmeta operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="context">Input value for context.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string CreateDatapackMcmeta(MinecraftModBuildRequest request, WorkspaceContext context, ILogger logger)
        {
            try
            {
                return $$"""
            {
              "pack": {
                "pack_format": {{text.GetPackFormatJsonValue(request.MinecraftVersion, logger)}},
                "description": "{{text.EscapeJson(context.ProjectName)}} - LocalGPT generated Living Cities datapack"
              }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateDatapackMcmeta");
                return string.Empty;
            }
        }
       
        /// <summary>Executes the write datapack function async operation.</summary>
        /// <param name="context">Input value for context.</param>
        /// <param name="functionPath">Input value for functionPath.</param>
        /// <param name="content">Input value for content.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task WriteDatapackFunctionAsync(
    WorkspaceContext context,
    string functionPath,
    string content,
    CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var normalizedPath = functionPath.Replace('/', Path.DirectorySeparatorChar);
                var path = Path.Combine(context.ProjectRoot, "data", context.ModId, "function", $"{normalizedPath}.mcfunction");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, content, catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteDatapackFunctionAsync");
            }
        }
        /// <summary>Executes the known dependency version info versions operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public List<CatalogEntry> KnownDependencyVersionInfoVersions (ILogger logger)
        {
            try
            {
                return new List<CatalogEntry>()
                {
                    new(
            MinecraftVersion: "26.1.2",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT datapack-first mapping for the Minecraft Java 26.1 stable family. Java mods/plugins need official Fabric, NeoForge, or Paper version checks before release."),
        new(
            MinecraftVersion: "26.1.1",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT datapack-first mapping for the Minecraft Java 26.1 stable family. Java mods/plugins need official Fabric, NeoForge, or Paper version checks before release."),
        new(
            MinecraftVersion: "26.1",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT datapack-first mapping for Minecraft Java 26.1. Java mods/plugins need official Fabric, NeoForge, or Paper version checks before release."),
        new(
            MinecraftVersion: "26.2-snapshot-6",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT snapshot datapack mapping for Minecraft Java 26.2 Snapshot 6. Use only for snapshot worlds and verify loader APIs before Java mod/plugin release."),
        new(
            MinecraftVersion: "26.2",
            FabricApiVersion: null,
            NeoForgeVersion: null,
            PaperApiVersion: null,
            JavaVersion: "25",
            Notes: "Curated LocalGPT snapshot datapack mapping for Minecraft Java 26.2. Use only for snapshot worlds and verify loader APIs before Java mod/plugin release."),
        new(
            MinecraftVersion: "1.21.4",
            FabricApiVersion: "0.116.9+1.21.4",
            NeoForgeVersion: "21.1.231",
            PaperApiVersion: "1.21.4-R0.1-SNAPSHOT",
            JavaVersion: "21",
            Notes: "Curated LocalGPT 1.21.4 mapping; NeoForge value is a cautious 1.21.x fallback and should be source-checked before release."),
        new(
            MinecraftVersion: "1.21.1",
            FabricApiVersion: "0.116.9+1.21.1",
            NeoForgeVersion: "21.1.231",
            PaperApiVersion: "1.21.1-R0.1-SNAPSHOT",
            JavaVersion: "21",
            Notes: "Curated LocalGPT 1.21.1 mapping used by Java workspace smoke tests.")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in KnownDependencyVersionInfoVersions");
                return new() ;
            }
        }
        /// <summary>Executes the resolve minecraft dependency version info operation.</summary>
        /// <param name="loader">Input value for loader.</param>
        /// <param name="minecraftVersion">Input value for minecraftVersion.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="javaVersion">Input value for javaVersion.</param>
        /// <param name="gradleVersion">Input value for gradleVersion.</param>
        /// <returns>The operation result.</returns>
        public MinecraftDependencyVersionInfo ResolveMinecraftDependencyVersionInfo(
    string? loader,
    string? minecraftVersion,
    ILogger logger,
    string? javaVersion = null,
    string? gradleVersion = null)
        {
            try
            {
                var normalizedLoader = text.NormalizeLoader(loader, logger);
                var requestedMinecraftVersion = string.IsNullOrWhiteSpace(minecraftVersion)
                    ? catalog.DefaultMinecraftVersion
                    : minecraftVersion.Trim();
                var requestedGradleVersion = string.IsNullOrWhiteSpace(gradleVersion)
                    ? catalog.DefaultGradleVersion
                    : gradleVersion.Trim();
                var knownDependencyVersions = KnownDependencyVersionInfoVersions(logger);
                var entry = knownDependencyVersions.FirstOrDefault(item =>
                    requestedMinecraftVersion.Equals(item.MinecraftVersion, StringComparison.OrdinalIgnoreCase));
                var exact = entry is not null;
                entry ??= knownDependencyVersions
                    .Where(item => requestedMinecraftVersion.StartsWith(item.MinecraftVersion, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.MinecraftVersion.Length)
                    .FirstOrDefault();
                entry ??= requestedMinecraftVersion.StartsWith("26.", StringComparison.OrdinalIgnoreCase)
                    ? knownDependencyVersions.First(item => item.MinecraftVersion == "26.1")
                    : requestedMinecraftVersion.StartsWith("1.21", StringComparison.OrdinalIgnoreCase)
                    ? knownDependencyVersions.First(item => item.MinecraftVersion == "1.21.4")
                    : knownDependencyVersions.First(item => item.MinecraftVersion == "26.1");

                var requestedJavaVersion = string.IsNullOrWhiteSpace(javaVersion)
                    ? entry.JavaVersion ?? catalog.DefaultJavaVersion
                    : javaVersion.Trim();

                var datapack = MinecraftDatapackVersionInfoResolve(requestedMinecraftVersion, logger);
                var needsVerification = !exact ||
                    datapack.NeedsVerification ||
                    (normalizedLoader is "Fabric" or "NeoForge" or "Paper") &&
                    (entry.FabricApiVersion is null || entry.NeoForgeVersion is null || entry.PaperApiVersion is null) ||
                    normalizedLoader.Equals("NeoForge", StringComparison.OrdinalIgnoreCase) && !requestedMinecraftVersion.Equals("1.21.1", StringComparison.OrdinalIgnoreCase);

                var notes = exact
                    ? entry.Notes
                    : $"No exact curated Java-loader mapping for Minecraft {requestedMinecraftVersion}; using {entry.MinecraftVersion} as a fallback. Verify loader/API versions against official Fabric, NeoForge, and Paper sources before release.";

                return new MinecraftDependencyVersionInfo(
                    Loader: normalizedLoader,
                    RequestedMinecraftVersion: requestedMinecraftVersion,
                    MatchedMinecraftVersion: entry.MinecraftVersion,
                    JavaVersion: requestedJavaVersion,
                    GradleVersion: requestedGradleVersion,
                    "",
                    FabricApiVersion: normalizedLoader is "Fabric" ? entry.FabricApiVersion : null,
                    NeoForgeVersion: normalizedLoader is "NeoForge" ? entry.NeoForgeVersion : null,
                    PaperApiVersion: normalizedLoader is "Paper" ? entry.PaperApiVersion : null,
                    DatapackPackFormat: normalizedLoader is "Datapack" ? datapack.PackFormat : null,
                    IsExactMatch: exact && !datapack.NeedsVerification,
                    NeedsVerification: needsVerification,
                    Notes: notes,
                    Source: "LocalGPT curated Minecraft dependency catalog. Verify unknown versions with official Fabric, NeoForge, Paper, Gradle, and Minecraft version sources.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not resolve Minecraft dependency versions for loader {Loader} and version {MinecraftVersion}.", loader, minecraftVersion);
                return CreateFallbackDependencyVersionInfo(
                    new MinecraftModBuildRequest
                    {
                        Loader = loader ?? "Fabric",
                        MinecraftVersion = minecraftVersion ?? catalog.DefaultMinecraftVersion,
                        JavaVersion = javaVersion ?? catalog.DefaultJavaVersion,
                        GradleVersion = gradleVersion ?? catalog.DefaultGradleVersion
                    },
                    loader,
                    ex.Message);
            }
        }

        /// <summary>Executes the minecraft datapack version info resolve operation.</summary>
        /// <param name="minecraftVersion">Input value for minecraftVersion.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
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
                    Notes: "LocalGPT could not resolve the requested datapack version and used a safe current fallback. Verify pack_format before release.",
                    Source: "LocalGPT defensive fallback after version-resolution failure.");
            }
        }
        /// <summary>Executes the minecraft datapack version known versions operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
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
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(MinecraftDatapackVersionKnownVersions)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(MinecraftDatapackVersionKnownVersions)} failed.");
        throw;
    }
}
        /// <summary>Executes the minecraft datapack version info known operation.</summary>
        /// <param name="version">Input value for version.</param>
        /// <param name="packFormat">Input value for packFormat.</param>
        /// <param name="functionRegistryFolder">Input value for functionRegistryFolder.</param>
        /// <param name="notes">Input value for notes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
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
        /// <summary>Executes the find repository root operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        private string? FindRepositoryRoot(ILogger<BuildDebugInventoryService> logger)
        {
            try
            {
                foreach (var start in new[]
 {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
                {
                    var directory = new DirectoryInfo(start);
                    while (directory is not null)
                    {
                        if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                            Directory.Exists(Path.Combine(directory.FullName, ".git")))
                        {
                            return directory.FullName;
                        }

                        directory = directory.Parent;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FindRepositoryRoot");
                return string.Empty;
            }
        }
        /// <summary>Executes the build manual expected lane operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public EngineeringBenchmarkLaneResult BuildManualExpectedLane(BenchmarkTaskDefinition task, ILogger logger)
        {
            try
            {
                var lane = new EngineeringBenchmarkLaneResult
                {
                    Lane = "D. manual expected output",
                    Status = "Reference",
                    ValidArchitectureScore = 10,
                    BuildabilityScore = 10,
                    MissingFilesScore = 10,
                    WrongPackagesTemplatesScore = 10,
                    TimeToUsableOutputScore = 0,
                    RepairPromptsScore = 10,
                    DownloadableArtifactScore = 0,
                    RepairPromptCount = 0,
                    Notes = task.ManualExpectedOutput
                };
                lane.TotalScore = SumScores(lane, logger);
                return lane;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the manual benchmark reference lane for task {TaskId}.", task?.Id);
                return new EngineeringBenchmarkLaneResult
                {
                    Lane = "D. manual expected output",
                    Status = "Error",
                    Notes = "The manual reference lane could not be prepared. Review LocalGPT logs."
                };
            }
        }
        /// <summary>Executes the not run lane operation.</summary>
        /// <param name="laneName">Input value for laneName.</param>
        /// <param name="notes">Input value for notes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public EngineeringBenchmarkLaneResult NotRunLane(string laneName, string notes, ILogger logger)
        {
            try
            {
                return new EngineeringBenchmarkLaneResult
                {
                    Lane = laneName,
                    Status = "NotRun",
                    Notes = notes
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create benchmark lane {LaneName}.", laneName);
                return new EngineeringBenchmarkLaneResult
                {
                    Lane = string.IsNullOrWhiteSpace(laneName) ? "Unnamed benchmark lane" : laneName,
                    Status = "Error",
                    Notes = string.IsNullOrWhiteSpace(notes) ? "The benchmark lane could not be prepared." : notes
                };
            }
        }
        /// <summary>Executes the score architecture operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="zipEntries">Input value for zipEntries.</param>
        /// <param name="artifacts">Input value for artifacts.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int ScoreArchitecture(
    BenchmarkTaskDefinition task,
    HashSet<string> zipEntries,
    IReadOnlyList<CouncilArtifact> artifacts, ILogger logger)
        {
            try
            {
                if (artifacts.Count == 0)
                    return 0;

                var hits = task.ArchitectureEvidence.Count(evidence =>
                    zipEntries.Any(entry => entry.Contains(evidence, StringComparison.OrdinalIgnoreCase)) ||
                    artifacts.Any(artifact => artifact.Summary.Contains(evidence, StringComparison.OrdinalIgnoreCase) ||
                        artifact.Kind.Contains(evidence, StringComparison.OrdinalIgnoreCase)));

                return task.ArchitectureEvidence.Count == 0
                    ? 7
                    : Math.Clamp(4 + hits * 2, 0, 10);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ScoreArchitecture task {task.ToString()} zipEntries {zipEntries.ToString()} artifacts {artifacts.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the score wrong template risk operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="zipEntries">Input value for zipEntries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int ScoreWrongTemplateRisk(BenchmarkTaskDefinition task, HashSet<string> zipEntries, ILogger logger)
        {
            try
            {
                if (task.WrongTemplateGuards.Count == 0)
                    return 8;

                var guardHits = task.WrongTemplateGuards.Count(guard => text.ContainsZipEntry(zipEntries, guard, logger));
                return guardHits == task.WrongTemplateGuards.Count ? 10 : Math.Max(0, 10 - (task.WrongTemplateGuards.Count - guardHits) * 3);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ScoreWrongTemplateRisk task {task.ToString()} zipEntries {zipEntries.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the score buildability operation.</summary>
        /// <param name="task">Input value for task.</param>
        /// <param name="artifacts">Input value for artifacts.</param>
        /// <param name="buildChecks">Input value for buildChecks.</param>
        /// <param name="validateBuildableArtifacts">Input value for validateBuildableArtifacts.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int ScoreBuildability(
      BenchmarkTaskDefinition task,
      IReadOnlyList<CouncilArtifact> artifacts,
      IReadOnlyList<EngineeringBenchmarkBuildCheck> buildChecks,
      bool validateBuildableArtifacts, ILogger logger)
        {
            try
            {
                if (artifacts.Count == 0)
                    return 0;

                if (!validateBuildableArtifacts)
                    return task.LocalGptBuildabilityScore;

                var dotnetChecks = buildChecks
                    .Where(check => !check.Status.Equals("NoSolution", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (dotnetChecks.Length == 0)
                    return task.LocalGptBuildabilityScore;

                return dotnetChecks.All(check => check.Status.Equals("BuildPassed", StringComparison.OrdinalIgnoreCase))
                    ? 10
                    : 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ScoreBuildability task {task.ToString()} artifacts {artifacts.ToString()} buildChecks {buildChecks.ToString()} validateBuildableArtifacts {validateBuildableArtifacts.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the read zip entries safe operation.</summary>
        /// <param name="artifact">Input value for artifact.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<string> ReadZipEntriesSafe(CouncilArtifact artifact, ILogger logger)
        {

            if (!File.Exists(artifact.FilePath))
                return [];

            try
            {
                using var archive = ZipFile.OpenRead(artifact.FilePath);
                return archive.Entries
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ReadZipEntriesSafe artifact {artifact.ToString()}");
                return new List<string>();
            }
        }
        /// <summary>Executes the sum scores operation.</summary>
        /// <param name="lane">Input value for lane.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int SumScores(EngineeringBenchmarkLaneResult lane, ILogger logger)
        {
            try
            {
                return lane.ValidArchitectureScore +
                lane.BuildabilityScore +
                lane.MissingFilesScore +
                lane.WrongPackagesTemplatesScore +
                lane.TimeToUsableOutputScore +
                lane.RepairPromptsScore +
                lane.DownloadableArtifactScore;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SumScores lane {lane.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the read small text operation.</summary>
        /// <param name="file">Input value for file.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string ReadSmallText(FileInfo file, ILogger logger)
        {
            try
            {
                return System.IO.File.ReadAllText(file.FullName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadSmallText file {file?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the add if operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="signals">Input value for signals.</param>
        /// <param name="label">Input value for label.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="needles">Input value for needles.</param>
        public void AddIf(string text, List<string> signals, string label, ILogger logger, params string[] needles)
        {
            try
            {
                if (needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase)))
                    signals.Add(label);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddIf text {text?.ToString()} signals {signals?.ToString()} label {label?.ToString()} needles {needles?.ToString()}");

            }
        }
        /// <summary>Executes the build windows dev docs entries operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<CouncilKnowledgeEntry> BuildWindowsDevDocsEntries(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles, ILogger logger)
        {
            try
            {
                var now = DateTime.UtcNow;
                var docfxSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "docfx", "metadata", "toc", "index", "authoring");
                var platformSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "windows-app-sdk", "winui", "webview2", "msix", "desktop");
                var supportSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "developer-mode", "dev-drive", "winget", "terminal", "arm64");
                var designSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, logger, "design", "accessibility", "navigation", "layout", "typography");
                var frontMatterCount = markdownFiles
                    .Take(800)
                    .Select(filter => ReadSmallText(filter, logger))
                    .Count(text => text.TrimStart().StartsWith("---", StringComparison.Ordinal));

                return StampSourceMetadata(
                    rootPath,
                    markdownFiles,
                    now,
                [
                    new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|docfx|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows developer docs DocFX and Microsoft Learn authoring",
                    Scope = "DocFX / developer documentation",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "The local Windows developer docs corpus uses Microsoft Learn/DocFX-style Markdown. " +
                        "Generation should preserve normal physical line breaks, front matter, title/description metadata, ms.topic/ms.date fields, relative links, includes, image references, and table/list readability. " +
                        "For docfx generation, produce docs that can be indexed by topic, source file, service boundary, build command, troubleshooting case, and related API/platform area. " +
                        "Do not paste full docs into prompts; summarize source maps and let LocalGPT retrieve narrow entries. " +
                        $"Sampled {markdownFiles.Count} markdown files; {frontMatterCount} of the first 800 looked like front-matter pages.",
                    HelpfulSources = docfxSamples,
                    Tags = "learn-base; windows-dev-docs; docfx; microsoft-learn; markdown; documentation; source-backed",
                    Confidence = 88,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|platform|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows app platform source map for LocalGPT generation",
                    Scope = "Windows app development",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "When generating Windows-capable .NET apps, use the Windows docs corpus as a compact source map for Windows App SDK, WinUI, WebView2, MSIX/package deployment, app lifecycle, desktop integration, and Windows desktop support boundaries. " +
                        "For LocalGPT-style apps, keep WebView2 wrappers thin, own Blazor/ASP.NET Core work in the backend, and document static assets, package/runtime dependencies, and deploy/debug differences. " +
                        "Generated projects should include health routes, package diagnostics, build/run docs, and clear user-facing setup checks.",
                    HelpfulSources = platformSamples,
                    Tags = "learn-base; windows; winui; windowsappsdk; webview2; msix; deployment; desktop; source-backed",
                    Confidence = 88,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|support|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows technician workflow for developer support",
                    Scope = "Windows support / operations",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "Use the Windows docs corpus to support developer-machine setup and troubleshooting: Developer Mode, Device Portal/discovery, winget, Windows Terminal, Dev Drive, PowerToys, Visual Studio/SDK/runtime checks, Arm64/Arm64EC/Arm64X compatibility, package logs, event logs, certificates, and deployment diagnostics. " +
                        "LocalGPT should present these as guided checks and repair scripts, not as vague advice. Mark actions that need admin rights, downloads, or package changes before running them.",
                    HelpfulSources = supportSamples,
                    Tags = "learn-base; windows-support; winget; terminal; dev-drive; arm64; diagnostics; technician; source-backed",
                    Confidence = 86,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|design|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Windows design and accessibility guidance for generated Blazor apps",
                    Scope = "Frontend design / accessibility",
                    Source = "Local learn-base docs corpus: windows-dev-docs-docs",
                    Content = "Use Windows design guidance as a source-backed supplement for generated Blazor/DevExpress apps: navigation clarity, command placement, typography, layout, iconography, accessibility, keyboard focus, density, status messages, and responsive behavior. " +
                        "Generated apps should be understandable without long instructional text, while still surfacing setup state, loading state, errors, empty states, and next actions.",
                    HelpfulSources = designSamples,
                    Tags = "learn-base; windows-design; accessibility; blazor; devexpress; ux; source-backed",
                    Confidence = 86,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                }
                ], logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildWindowsDevDocsEntries rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }

        }
        /// <summary>Executes the build dot net docs entries operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<CouncilKnowledgeEntry> BuildDotNetDocsEntries(
     string rootPath,
     IReadOnlyList<FileInfo> markdownFiles, ILogger logger)
        {
            try
            {
                var now = DateTime.UtcNow;
                var docfxSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "docfx", "toc", "index", "includes", "samples");
                var architectureSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "architecture", "microservices", "cloud-native", "modern-web-apps");
                var csharpSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "csharp", "language-reference", "compiler", "csharp-12", "language-versioning");
                var webSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "aspnet", "blazor", "web-api", "minimal-api", "dependency-injection");
                var dataSamples = BuildDocsPathSamples(rootPath, markdownFiles, logger, "entity-framework", "ef-core", "linq", "data", "serialization");
                var frontMatterCount = markdownFiles
                    .Take(1000)
                    .Select(filter => ReadSmallText(filter, logger))
                    .Count(text => text.TrimStart().StartsWith("---", StringComparison.Ordinal));

                return StampSourceMetadata(
                    rootPath,
                    markdownFiles,
                    now,
                [
                    new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|docfx|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Microsoft .NET docs corpus source map",
                    Scope = ".NET / Microsoft Learn / DocFX",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "The local Microsoft .NET docs corpus is a source-backed map for .NET, C#, compiler diagnostics, architecture, libraries, samples, and Microsoft Learn authoring. " +
                        "Do not paste the full corpus into model prompts. Store concise source maps in SQLite, retrieve narrow entries, and inspect exact files only when a generation task needs exact syntax or version detail. " +
                        "Generated docs should preserve front matter, relative links, includes, readable tables/lists, normal physical line breaks, and topic/source metadata. " +
                        $"Sampled {markdownFiles.Count} markdown files; {frontMatterCount} of the first 1000 looked like front-matter pages.",
                    HelpfulSources = docfxSamples,
                    Tags = "learn-base; dotnet-docs; microsoft-learn; docfx; markdown; source-backed",
                    Confidence = 90,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|architecture|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Modern .NET architecture guidance for generated solutions",
                    Scope = ".NET architecture / application generation",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use the Microsoft .NET architecture docs as the source map for generated enterprise solutions: layered or modular monoliths, microservices only when useful, cloud-native boundaries, service ownership, DI/options/logging/configuration, background services, API contracts, resiliency, data access, tests, deployment, and documentation. " +
                        "When a prompt asks for a whole app, generate projects, services, models, routes/pages, persistence, tests or smoke paths, README, and downloadable artifacts instead of only pages.",
                    HelpfulSources = architectureSamples,
                    Tags = "learn-base; dotnet; architecture; microservices; modular-monolith; aspnetcore; source-backed",
                    Confidence = 90,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|csharp-compiler|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "C# language and compiler source map for LocalGPT code generation",
                    Scope = "C# / compiler diagnostics / language versions",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use the C# language reference, language-version docs, compiler options, compiler messages, nullable reference type guidance, pattern matching, records, required members, primary constructors, collection expressions, interceptors where version-supported, generics, LINQ, async, attributes, XML docs, source generators, and analyzers as source-backed input for code generation and repair. " +
                        "If the requested feature depends on C# 12 or newer syntax, verify the target SDK/langversion and emit buildable code for that version instead of guessing. Compiler diagnostics win over model confidence.",
                    HelpfulSources = csharpSamples,
                    Tags = "learn-base; csharp; csharp12; compiler; diagnostics; langversion; roslyn; source-backed",
                    Confidence = 91,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|web-blazor|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "ASP.NET Core and Blazor source map for generated web apps",
                    Scope = "ASP.NET Core / Blazor",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use Microsoft docs as a source map for ASP.NET Core hosting, minimal APIs/controllers, routing, middleware, configuration, DI, authentication/authorization, SignalR where relevant, Blazor component rendering modes, forms, validation, static assets, file uploads/downloads, testing, publishing, and diagnostics. " +
                        "For LocalGPT-style apps, generate backend services and safe HTTP download routes for generated files; Blazor pages should present state, controls, and validation rather than owning privileged execution.",
                    HelpfulSources = webSamples,
                    Tags = "learn-base; aspnetcore; blazor; minimal-api; middleware; static-assets; source-backed",
                    Confidence = 88,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"dotnet-docs|data|{rootPath}",logger),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = ".NET data, LINQ, serialization, and EF source map",
                    Scope = ".NET data access / EF / serialization",
                    Source = "Local learn-base docs corpus: dotnet/docs-main",
                    Content = "Use Microsoft docs as a source map for LINQ, serialization, configuration binding, EF-style data modeling when present, migrations/schema evolution, nullable columns for populated databases, DTOs, validation, and database-backed application state. " +
                        "When DevExpress Web API/XAF/OData compatibility is requested, combine this with LocalGPT's DevExpress business-object guidance instead of inventing shadow properties or ambiguous relationships.",
                    HelpfulSources = dataSamples,
                    Tags = "learn-base; dotnet-data; linq; serialization; efcore; database; source-backed",
                    Confidence = 87,
                    VerificationStatus = "SourceBacked",
                    IsUserApproved = true,
                    IsPinned = true
                }
                ], logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDotNetDocsEntries rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }
        }
        /// <summary>Executes the to knowledge entry operation.</summary>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilKnowledgeEntry? ToKnowledgeEntry(LearnBaseProjectSummary summary, ILogger logger)
        {
            try
            {
                var sanitizedSourcePath = text.RedactSensitiveName(summary.SourcePath, logger);
                var now = DateTime.UtcNow;
                var content = new StringBuilder()
                    .AppendLine("This entry is about reusable architecture and wiring patterns, not about copying names or branding.")
                    .AppendLine("Learn the functionality, protocols, service boundaries, host wiring, and component usage. Treat source labels as evidence labels, not as target product names.")
                    .AppendLine($"Architecture fingerprint source label: {summary.Name}")
                    .AppendLine($"Sanitized source path label: {sanitizedSourcePath}")
                    .AppendLine($"Architecture signals: {summary.Architecture}")
                    .AppendLine($"Protocols/components: {summary.ProtocolsAndComponents}")
                    .AppendLine($"Target frameworks: {text.Fallback(summary.TargetFrameworks, "none detected", logger)}")
                    .AppendLine($"Package references: {text.Fallback(summary.PackageReferences, "none detected", logger)}")
                    .AppendLine($"Important files: {summary.ImportantFiles}")
                    .AppendLine($"Source files counted: {summary.SourceFileCount}; binary/build artifacts counted but not stored: {summary.BinaryFileCount}.")
                    .AppendLine("Generation guidance: learn host shapes, protocols, libraries, service boundaries, and solution setup. Do not preserve project names unless the user explicitly asks.")
                    .AppendLine("Ask for a poll when the user has not selected monolith vs microservice, Blazor vs non-Blazor frontend, DevExpress Web API/security, Python interop, or data persistence style.")
                    .AppendLine("Legacy offensive names are sanitized in knowledge records; preserve the technical pattern, not the wording.")
                    .ToString();

                return new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"learn-base|{summary.SourcePath}", logger),
                    Topic = $"Selected learn-base architecture fingerprint: {summary.Architecture}",
                    Scope = "Selected local project learn-base",
                    Source = $"Local learn-base scan: {sanitizedSourcePath}",
                    Content = content,
                    HelpfulSources = "The user-selected local learn-base source folder. Import stores compact fingerprints only; inspect the selected source directly before copying exact code. Legacy offensive names are sanitized before teaching.",
                    Tags = BuildTags(summary, logger),
                    Confidence = 78,
                    VerificationStatus = "SourceBacked",
                    ReviewStatus = "NeedsUserReview",
                    ExpiresAtUtc = now.AddDays(180),
                    LastVerifiedAtUtc = now,
                    SourceDateUtc = TryGetLatestSourceDateUtc(summary.SourcePath, logger),
                    SourceHash = ComputeSummaryHash(summary, logger),
                    StalenessReason = "Local project fingerprints should be approved or corrected by the user before being treated as durable generation guidance.",
                    StalenessDetectedBy = "Learn-base importer",
                    IsUserApproved = false,
                    IsPinned = summary.Name.Contains("Tacos", StringComparison.OrdinalIgnoreCase) ||
                        summary.Name.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                        summary.Name.Contains("Jezzifa", StringComparison.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToKnowledgeEntry summary {summary?.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the build windows docs path samples operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="needles">Input value for needles.</param>
        /// <returns>The operation result.</returns>
        public string BuildWindowsDocsPathSamples(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles,
            ILogger logger,
            params string[] needles)
        {
            try
            {
                var samples = BuildDocsPathSamples(rootPath, markdownFiles, logger, needles);
                if (!samples.StartsWith("No direct sample paths matched", StringComparison.OrdinalIgnoreCase))
                    return samples;

                return string.Join(
                    "\n",
                    markdownFiles
                        .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                        .Take(16)
                        .Select(file => "- " + text.RedactSensitiveName(Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/'), logger)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildWindowsDocsPathSamples rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()} needles {needles?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the stamp source metadata operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="now">Input value for now.</param>
        /// <param name="entries">Input value for entries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<CouncilKnowledgeEntry> StampSourceMetadata(
    string rootPath,
    IReadOnlyList<FileInfo> files,
    DateTime now,
    IReadOnlyList<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                var sourceDateUtc = files.Count == 0
               ? Directory.GetLastWriteTimeUtc(rootPath)
               : files.Max(file => file.LastWriteTimeUtc);
                var corpusHash = ComputeCorpusHash(rootPath, files, logger);
                foreach (var entry in entries)
                {
                    entry.ReviewStatus = "Current";
                    entry.LastVerifiedAtUtc = now;
                    entry.SourceDateUtc = sourceDateUtc;
                    entry.SourceHash = corpusHash;
                }

                return entries;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in StampSourceMetadata rootPath {rootPath?.ToString()} files {files?.ToString()} now {now.ToString()} entries {entries?.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }
        }
        /// <summary>Executes the enumerate documentation corpus candidates operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<string> EnumerateDocumentationCorpusCandidates(string rootPath, ILogger logger)
        {
            try
            {
                yield return rootPath;

                foreach (var child in SafeEnumerateDirectories(rootPath, logger))
                {
                    yield return child;

                    foreach (var grandChild in SafeEnumerateDirectories(child, logger))
                    {
                        var name = Path.GetFileName(grandChild);
                        if (name.Contains("docs", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("learn", StringComparison.OrdinalIgnoreCase))
                        {
                            yield return grandChild;
                        }
                    }
                }
            }
            finally
            {
                logger.LogInformation($"Ended EnumerateDocumentationCorpusCandidates rootPath {rootPath?.ToString()}");

            }
        }
        /// <summary>Executes the build project summary operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="projectDirectory">Input value for projectDirectory.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public LearnBaseProjectSummary? BuildProjectSummary(string rootPath, string projectDirectory, ILogger logger)
        {
            try
            {
                var files = EnumerateUsefulFiles(projectDirectory, logger).Take(1600).ToArray();
                return BuildProjectSummary(rootPath, projectDirectory, files, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in BuildProjectSummary rootPath {RootPath} projectDirectory {ProjectDirectory}", rootPath, projectDirectory);
                return null;
            }
        }

        /// <summary>Executes the build project summary operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="projectDirectory">Input value for projectDirectory.</param>
        /// <param name="selectedFiles">Input value for selectedFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public LearnBaseProjectSummary? BuildProjectSummary(
            string rootPath,
            string projectDirectory,
            IReadOnlyList<FileInfo> selectedFiles,
            ILogger logger)
        {
            try
            {
                var files = selectedFiles.Take(1600).ToArray();
                var sourceFiles = files
                    .Where(file => !catalog.BinaryExtensions.Contains(file.Extension))
                    .ToArray();
                var binaryCount = files.Count(file => catalog.BinaryExtensions.Contains(file.Extension));
                var textSamples = sourceFiles
                    .Where(file => file.Length is > 0 and < 256_000)
                    .Take(80)
                    .Select(filter => ReadSmallText(filter, logger))
                    .Where(content => !string.IsNullOrWhiteSpace(content))
                    .ToArray();
                var pathSamples = sourceFiles
                    .Take(700)
                    .Select(file => Path.GetRelativePath(projectDirectory, file.FullName).Replace('\\', '/'));
                var combined = string.Join("\n", textSamples.Concat(pathSamples));

                return new LearnBaseProjectSummary
                {
                    Name = text.RedactSensitiveName(Path.GetFileName(projectDirectory), logger),
                    SourcePath = projectDirectory,
                    SourceFileCount = sourceFiles.Length,
                    BinaryFileCount = binaryCount,
                    Architecture = InferArchitecture(projectDirectory, files, combined, logger),
                    ProtocolsAndComponents = InferProtocolsAndComponents(combined, files, logger),
                    TargetFrameworks = string.Join(", ", text.ExtractTargetFrameworks(combined, logger).Take(12)),
                    PackageReferences = string.Join(", ", text.ExtractPackageReferences(combined, logger).Take(24)),
                    ImportantFiles = BuildImportantFileList(rootPath, projectDirectory, sourceFiles, logger)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in BuildProjectSummary rootPath {RootPath} projectDirectory {ProjectDirectory}", rootPath, projectDirectory);
                return null;
            }
        }
        /// <summary>Executes the build docs path samples operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="markdownFiles">Input value for markdownFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="needles">Input value for needles.</param>
        /// <returns>The operation result.</returns>
        public string BuildDocsPathSamples(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles,
            ILogger logger,
            params string[] needles)
        {
            try
            {
                var matches = markdownFiles
              .Where(file =>
              {
                  var relative = Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/');
                  return needles.Any(needle => relative.Contains(needle, StringComparison.OrdinalIgnoreCase));
              })
              .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
              .Take(18)
              .Select(file => "- " + text.RedactSensitiveName(Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/'), logger))
              .ToArray();

                if (matches.Length > 0)
                    return string.Join("\n", matches);

                return "No direct sample paths matched: " + string.Join(", ", needles.Take(8));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDocsPathSamples rootPath {rootPath?.ToString()} markdownFiles {markdownFiles?.ToString()} needles {needles?.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>Executes the compute corpus hash operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string ComputeCorpusHash(string rootPath, IReadOnlyList<FileInfo> files, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine(Path.GetFileName(rootPath));

                foreach (var file in files.OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase).Take(2000))
                {
                    builder
                        .Append(Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/'))
                        .Append('|')
                        .Append(file.Length)
                        .Append('|')
                        .AppendLine(file.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ComputeCorpusHash rootPath {rootPath?.ToString()} files {files?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the build documentation corpus candidates operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<string> BuildDocumentationCorpusCandidates(string rootPath, ILogger logger)
        {
            try
            {
                var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var candidate in EnumerateDocumentationCorpusCandidates(rootPath, logger))
                {
                    if (Directory.Exists(candidate) && emitted.Add(Path.GetFullPath(candidate)))
                        yield return Path.GetFullPath(candidate);
                }
            }
            finally
            {
                logger.LogInformation("Ended BuildDocumentationCorpusCandidates rootPath {rootPath?.ToString()}");

            }
        }
        /// <summary>Executes the try get latest source date utc operation.</summary>
        /// <param name="sourcePath">Input value for sourcePath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public DateTime? TryGetLatestSourceDateUtc(string sourcePath, ILogger logger)
        {
            try
            {
                return Directory.Exists(sourcePath)
                    ? EnumerateUsefulFiles(sourcePath, logger).Take(2000).DefaultIfEmpty().Max(file => file?.LastWriteTimeUtc)
                    : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in TryGetLatestSourceDateUtc sourcePath {sourcePath?.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the enumerate useful files operation.</summary>
        /// <param name="directory">Input value for directory.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<FileInfo> EnumerateUsefulFiles(string directory, ILogger logger)
        {
            try
            {
                var stack = new Stack<DirectoryInfo>();
                stack.Push(new DirectoryInfo(directory));
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    DirectoryInfo[] subdirectories;
                    FileInfo[] files;
                    try
                    {
                        subdirectories = current.GetDirectories();
                        files = current.GetFiles();
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }

                    foreach (var subdirectory in subdirectories)
                    {
                        if (!catalog.ExcludedDirectoryNames.Contains(subdirectory.Name))
                            stack.Push(subdirectory);
                    }

                    foreach (var file in files)
                    {
                        if (catalog.SourceExtensions.Contains(file.Extension) || catalog.BinaryExtensions.Contains(file.Extension))
                            yield return file;
                    }
                }
            }
            finally
            {
                logger.LogInformation($"Ended  EnumerateUsefulFiles directory {directory?.ToString()}");

            }
        }
        /// <summary>Executes the compute summary hash operation.</summary>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string ComputeSummaryHash(LearnBaseProjectSummary summary, ILogger logger)
        {
            try
            {
                var material = string.Join(
               "\n",
               summary.Name,
               summary.SourcePath,
               summary.Architecture,
               summary.ProtocolsAndComponents,
               summary.TargetFrameworks,
               summary.PackageReferences,
               summary.ImportantFiles,
               summary.SourceFileCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ComputeSummaryHash summary {summary?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the infer architecture operation.</summary>
        /// <param name="projectDirectory">Input value for projectDirectory.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="combined">Input value for combined.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string InferArchitecture(string projectDirectory, IReadOnlyList<FileInfo> files, string combined, ILogger logger)
        {
            try
            {
                var signals = new List<string>();
                if (files.Any(file => file.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)))
                    signals.Add("Visual Studio solution");
                if (combined.Contains("DevExpress", StringComparison.OrdinalIgnoreCase))
                    signals.Add("DevExpress components");
                if (combined.Contains("DxGrid", StringComparison.OrdinalIgnoreCase))
                    signals.Add("DevExpress grid/forms");
                if (combined.Contains("@rendermode", StringComparison.OrdinalIgnoreCase) || combined.Contains("RazorComponents", StringComparison.OrdinalIgnoreCase))
                    signals.Add("server-interactive Blazor");
                if (combined.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) || combined.Contains("DbContext", StringComparison.OrdinalIgnoreCase))
                    signals.Add("EF Core/data model");
                if (combined.Contains("OData", StringComparison.OrdinalIgnoreCase))
                    signals.Add("OData/Web API");
                if (combined.Contains("XAF", StringComparison.OrdinalIgnoreCase) || combined.Contains("SecuritySystem", StringComparison.OrdinalIgnoreCase))
                    signals.Add("DevExpress security/business objects");
                if (combined.Contains("DevExpress.ExpressApp.Security", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("SecurityStrategy", StringComparison.OrdinalIgnoreCase))
                    signals.Add("DevExpress Security Web API backend");
                if (combined.Contains("Telegram", StringComparison.OrdinalIgnoreCase) || combined.Contains("Bot", StringComparison.OrdinalIgnoreCase))
                    signals.Add("bot/messaging integration");
                if (combined.Contains("Python.Runtime", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("PythonEngine", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("pythonnet", StringComparison.OrdinalIgnoreCase))
                    signals.Add("Python.NET C# and Python interop");
                if (files.Any(file => file.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase)))
                    signals.Add("JavaScript/Electron or web frontend");
                if (files.Any(file => file.Extension.Equals(".py", StringComparison.OrdinalIgnoreCase)))
                    signals.Add("Python integration");
                if (files.Count(file => file.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)) >= 3)
                    signals.Add("multi-project solution topology");
                if (combined.Contains("AddControllers", StringComparison.OrdinalIgnoreCase) &&
                    combined.Contains("MapRazorComponents", StringComparison.OrdinalIgnoreCase))
                    signals.Add("mixed ASP.NET Core API and Blazor host");
                if (combined.Contains("WebApplication.CreateBuilder", StringComparison.OrdinalIgnoreCase) &&
                    combined.Contains("BackgroundService", StringComparison.OrdinalIgnoreCase))
                    signals.Add("microservice/background worker host");
                if (combined.Contains("WinUI", StringComparison.OrdinalIgnoreCase) || combined.Contains("WindowsAppSDK", StringComparison.OrdinalIgnoreCase))
                    signals.Add("WinUI/Windows app");
                if (combined.Contains("Wasm", StringComparison.OrdinalIgnoreCase) || combined.Contains("BlazorWebAssembly", StringComparison.OrdinalIgnoreCase))
                    signals.Add("WebAssembly frontend");
                if (combined.Contains("IHostedService", StringComparison.OrdinalIgnoreCase) || combined.Contains("ServiceBase", StringComparison.OrdinalIgnoreCase))
                    signals.Add("service/background worker");
                if (files.Any(file => file.Extension.Equals(".go", StringComparison.OrdinalIgnoreCase)) ||
                    files.Any(file => file.Name.Equals("go.mod", StringComparison.OrdinalIgnoreCase)))
                    signals.Add("Go API/runtime source to translate into .NET control-plane patterns");
                if (combined.Contains("/api/generate", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("/api/chat", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("/api/tags", StringComparison.OrdinalIgnoreCase))
                    signals.Add("AI-host-compatible API route surface");
                if (combined.Contains("OpenAI", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("anthropic", StringComparison.OrdinalIgnoreCase))
                    signals.Add("AI provider compatibility adapters");
                if (combined.Contains("manifest", StringComparison.OrdinalIgnoreCase) &&
                    combined.Contains("model", StringComparison.OrdinalIgnoreCase))
                    signals.Add("model registry and manifest lifecycle");
                if (combined.Contains("llama", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("runner", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("kvcache", StringComparison.OrdinalIgnoreCase))
                    signals.Add("inference runner/runtime orchestration");
                if (combined.Contains("tokenizer", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("template", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("harmony", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("thinking", StringComparison.OrdinalIgnoreCase))
                    signals.Add("chat template tokenizer and thinking-format handling");
                if (combined.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("moviepy", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("pydub", StringComparison.OrdinalIgnoreCase))
                    signals.Add("media processing pipeline");
                if (combined.Contains("midi", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("wave", StringComparison.OrdinalIgnoreCase) ||
                    combined.Contains("audio", StringComparison.OrdinalIgnoreCase))
                    signals.Add("audio/MIDI processing");

                return signals.Count == 0
                    ? $"Mixed or legacy project under {text.RedactSensitiveName(Path.GetFileName(projectDirectory), logger)}"
                    : string.Join("; ", signals.Distinct(StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in InferArchitecture projectDirectory {projectDirectory?.ToString()} files {files?.ToString()} combined {combined?.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>Executes the infer protocols and components operation.</summary>
        /// <param name="combined">Input value for combined.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string InferProtocolsAndComponents(string combined, IReadOnlyList<FileInfo> files, ILogger logger)
        {
            try
            {
                var signals = new List<string>();
                AddIf(combined, signals, "HTTP API", logger, "HttpClient", "ControllerBase", "MapGet", "WebApplication");
                AddIf(combined, signals, "OData", logger, "OData", "EnableQuery");
                AddIf(combined, signals, "SQLite", logger, "Sqlite", "SQLite", "sqlite");
                AddIf(combined, signals, "SQL Server", logger, "SqlServer", "UseSqlServer");
                AddIf(combined, signals, "Telegram Bot API", logger, "Telegram", "BotClient");
                AddIf(combined, signals, "Whisper/audio", logger, "Whisper", "NAudio", "WaveIn");
                AddIf(combined, signals, "Python.NET interop", logger, "Python.Runtime", "PythonEngine", "Py.GIL", "pythonnet");
                AddIf(combined, signals, "DevExpress Blazor", logger, "DxGrid", "DxFormLayout", "AddDevExpressBlazor");
                AddIf(combined, signals, "DevExpress Web API/security", logger, "DevExpress.ExpressApp.Security", "SecurityStrategy", "AddXafWebApi", "UseMiddleTier");
                AddIf(combined, signals, "DevExpress reports/documents", logger, "XtraReport", "RichEdit", "PdfViewer", "Spreadsheet");
                AddIf(combined, signals, "MSIX/DesktopBridge", logger, "wapproj", "AppxPackage", "DesktopBridge");
                AddIf(combined, signals, "Authentication/authorization", logger, "AuthorizeView", "AddAuthentication", "Identity");
                AddIf(combined, signals, "multi-host ASP.NET/Blazor", logger, "MapRazorComponents", "MapControllers", "AddRazorPages", "AddServerSideBlazor");
                AddIf(combined, signals, "AI host API", logger, "/api/generate", "/api/chat", "/api/tags", "/api/pull", "/api/embed");
                AddIf(combined, signals, "OpenAI/Anthropic compatibility", logger, "OpenAI", "anthropic", "/v1/chat/completions", "/v1/models");
                AddIf(combined, signals, "model manifest/download lifecycle", logger, "manifest", "digest", "download", "transfer", "progress");
                AddIf(combined, signals, "inference runtime", logger, "llama", "runner", "kvcache", "tokenizer", "sampling");
                AddIf(combined, signals, "chat format parsing", logger, "template", "gotmpl", "harmony", "thinking");
                AddIf(combined, signals, "WebView/desktop tray shell", logger, "WebView2", "wintray", "notifyicon");
                AddIf(combined, signals, "DevExpress AI Chat/function calling", logger, "DxAIChat", "AI-Chat-FunctionCalling", "AI-Chat-GridFunctionCalling");
                AddIf(combined, signals, "DevExpress upload/file workflow", logger, "DxUpload", "DxFileInput", "ChunkUpload");
                AddIf(combined, signals, "DevExpress reporting/document workflow", logger, "AddDevExpressBlazorReporting", "RichEdit", "PdfViewer", "Spreadsheet");
                AddIf(combined, signals, "Python media tooling", logger, "ffmpeg", "moviepy", "pydub", "youtube", "midi", "wave");
                if (files.Any(file => file.Name.Equals("app.py", StringComparison.OrdinalIgnoreCase)))
                    signals.Add("Flask/Python web app");
                if (files.Any(file => file.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase)))
                    signals.Add("npm package frontend");

                return signals.Count == 0
                    ? "No dominant protocol/component detected"
                    : string.Join("; ", signals.Distinct(StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in InferProtocolsAndComponents combined {combined?.ToString()} files {files?.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>Executes the build important file list operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="projectDirectory">Input value for projectDirectory.</param>
        /// <param name="sourceFiles">Input value for sourceFiles.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildImportantFileList(string rootPath, string projectDirectory, IReadOnlyList<FileInfo> sourceFiles, ILogger logger)
        {
            try
            {
                var important = sourceFiles
             .Where(file => text.IsImportantFile(file.Name, file.Extension, logger))
             .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
             .Take(28)
             .Select(file => text.RedactSensitiveName(Path.GetRelativePath(projectDirectory, file.FullName).Replace('\\', '/'), logger));

                return string.Join("; ", important);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildImportantFileList rootPath {rootPath?.ToString()} projectDirectory {projectDirectory?.ToString()} sourceFiles {sourceFiles?.ToString()}");
                return string.Empty;
            }

        }
        /// <summary>Executes the enumerate nested architecture roots operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
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
        /// <summary>Executes the safe enumerate directories operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
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
        /// <summary>Executes the safe enumerate directory infos operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
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
        /// <summary>Executes the looks like windows dev docs root operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool LooksLikeWindowsDevDocsRoot(string rootPath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(rootPath);
                if (!directory.Exists)
                    return false;

                if (directory.Name.Equals("windows-dev-docs-docs", StringComparison.OrdinalIgnoreCase))
                    return true;

                try
                {
                    var childNames = directory.GetDirectories()
                        .Select(child => child.Name)
                        .ToArray();
                    return childNames.Any(name => name.Contains("windows", StringComparison.OrdinalIgnoreCase)) &&
                        childNames.Any(name => name.Contains("windows-app", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("uwp", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("design", StringComparison.OrdinalIgnoreCase));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogDebug(ex, "Could not inspect child directories while detecting a Windows documentation root at {RootPath}.", rootPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikeWindowsDevDocsRoot rootPath {rootPath?.ToString()}");
                return false;
            }
        }
        /// <summary>Executes the looks like dot net docs root operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool LooksLikeDotNetDocsRoot(string rootPath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(rootPath);
                if (!directory.Exists)
                    return false;

                var docsDirectory = Path.Combine(rootPath, "docs");
                if (!System.IO.File.Exists(Path.Combine(rootPath, "docfx.json")) || !Directory.Exists(docsDirectory))
                    return false;

                return Directory.Exists(Path.Combine(docsDirectory, "csharp")) ||
                    Directory.Exists(Path.Combine(docsDirectory, "core")) ||
                    Directory.Exists(Path.Combine(docsDirectory, "architecture")) ||
                    Directory.Exists(Path.Combine(docsDirectory, "standard"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in LooksLikeDotNetDocsRoot rootPath {rootPath?.ToString()}");
                return false;
            }
        }
        /// <summary>Executes the looks like architecture root operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool LooksLikeArchitectureRoot(string rootPath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(rootPath);
                if (!directory.Exists)
                    return false;

                if (directory.GetFiles().Any(file => text.IsProjectRootFile(file.Name, file.Extension, logger)))
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
        /// <summary>Executes the create stable guid operation.</summary>
        /// <param name="value">Input value for value.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public Guid CreateStableGuid(string value, ILogger logger)
        {
            try
            {
                var hash = MD5.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
                return new Guid(hash);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStableGuid value {value}");
                return Guid.Empty;
            }
        }
        /// <summary>Executes the build tags operation.</summary>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildTags(LearnBaseProjectSummary summary, ILogger logger)
        {
            try
            {
                var tags = new List<string> { "learn-base", "source-backed", "architecture-fingerprint" };
                foreach (var token in Regex.Split($"{summary.Architecture};{summary.ProtocolsAndComponents}", @"[^A-Za-z0-9]+"))
                {
                    if (token.Length is >= 3 and <= 28)
                        tags.Add(token.ToLowerInvariant());
                }

                return string.Join("; ", tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(24));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildTags summary {summary?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the build tasks operation.</summary>
        /// <param name="taskSet">Input value for taskSet.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<BenchmarkTaskDefinition> BuildTasks(string taskSet, ILogger logger)
        {
            try
            {
                var engineering = text.BuildEngineeringTasks();
                var replacements = text.BuildReplacementTasks();

                return taskSet switch
                {
                    "replacement" => replacements,
                    "all" => engineering.Concat(replacements).ToArray(),
                    _ => engineering
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildTasks taskSet {taskSet?.ToString()}");
                return new List<BenchmarkTaskDefinition>();
            }
        }
        /// <summary>Executes the get configured ollama providers operation.</summary>
        /// <param name="options">Input value for options.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<OllamaCoreOptions> GetConfiguredOllamaProviders(AICoreOptions options, ILogger<ChatClientFactory> logger)
        {
            try
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (options.OllamaCore is { Uri.Length: > 0, ModelName.Length: > 0 } primary)
                {
                    seen.Add($"{primary.Uri.TrimEnd('/')}|{primary.ModelName}");
                    yield return primary;
                }

                foreach (var ollama in options.OllamaCores.Where(o => !string.IsNullOrWhiteSpace(o.Uri) && !string.IsNullOrWhiteSpace(o.ModelName)))
                {
                    if (seen.Add($"{ollama.Uri.TrimEnd('/')}|{ollama.ModelName}"))
                        yield return ollama;
                }
            }
            finally
            {
                logger.LogInformation("Finished enumerating configured Ollama providers.");
            }

        }
        /// <summary>Executes the copy debug file async operation.</summary>
        /// <param name="file">Input value for file.</param>
        /// <param name="sourceArea">Input value for sourceArea.</param>
        /// <param name="captureRoot">Input value for captureRoot.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes with the operation result.</returns>
        public async Task<string> CopyDebugFileAsync(FileInfo file, string sourceArea, string captureRoot, CancellationToken cancellationToken, ILogger<BuildDebugInventoryService> logger)
        {
            try
            {
                var area = text.SanitizeFileName(sourceArea, logger);
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.FullName)))[..12];
                var destination = Path.Combine(captureRoot, $"{area}-{hash}-{file.Name}");

                await using var read = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await using var write = File.Open(destination, FileMode.Create, FileAccess.Write, FileShare.None);
                await read.CopyToAsync(write, cancellationToken).ConfigureAwait(false);
                return destination;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyDebugFileAsync file {file.ToString()} sourceArea {sourceArea?.ToString()} captureRoot {captureRoot?.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the has real api key operation.</summary>
        /// <param name="apiKey">Input value for apiKey.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool HasRealApiKey(string? apiKey, ILogger<ChatClientFactory> logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                    return false;

                var trimmed = apiKey.Trim();
                return trimmed != "---" && !trimmed.Equals("local-key", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API-key presence validation failed.");
                return false;
            }

        }
        /// <summary>
        /// Gets search roots.
        /// </summary>
        public IEnumerable<(string Area, string Path)> GetSearchRoots(ILogger<BuildDebugInventoryService> logger)
        {
            try
            {
                yield return ("runtime", AppContext.BaseDirectory);

                var root = FindRepositoryRoot(logger);
                if (root is null)
                    yield break;

                yield return ("LocalGPT bin", Path.Combine(root, "src", "LocalGPT", "bin"));
                yield return ("LocalGPT obj", Path.Combine(root, "src", "LocalGPT", "obj"));
                yield return ("WebView2 wrapper bin", Path.Combine(root, "src", "LocalGPTWebviewWrapper", "bin"));
                yield return ("WebView2 wrapper obj", Path.Combine(root, "src", "LocalGPTWebviewWrapper", "obj"));
                yield return ("MSIX package bin", Path.Combine(root, "src", "LocalGPTWebviewWrapper (Package)", "bin"));
                yield return ("MSIX package obj", Path.Combine(root, "src", "LocalGPTWebviewWrapper (Package)", "obj"));
            }
            finally
            {
                logger.LogInformation($"Finished GetSearchRoots");
            }
        }
        /// <summary>Executes the read runtime server base url operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string ReadRuntimeServerBaseUrl(ILogger logger)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LocalGPT",
                    "runtime",
                    "server.json");
                if (!File.Exists(path))
                    return string.Empty;

                using var json = JsonDocument.Parse(File.ReadAllText(path));
                return json.RootElement.TryGetProperty("BaseUrl", out var value)
                    ? value.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadRuntimeServerBaseUrl");
                return string.Empty;
            }
        }
        /// <summary>Executes the first text operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="values">Input value for values.</param>
        /// <returns>The operation result.</returns>
        public string FirstText(ILogger<AiConnectivityProbe> logger, params string?[] values)
        {
            try
            {

                return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FirstText values {values.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Reconstructs a bounded DXAiChat prompt without recursively feeding complete prior Council transcripts back into later runs.
        /// </summary>
        /// <param name="messages">Current DXAiChat message history.</param>
        /// <param name="logger">Writes bounded prompt-reconstruction diagnostics.</param>
        /// <returns>The compact Council prompt containing user turns and at most the latest cleaned assistant consensus.</returns>
        public string BuildPrompt(IEnumerable<ChatMessage> messages, ILogger logger)
        {
            try
            {
                var history = messages
                    .Where(message => message.Role != ChatRole.System && !string.IsNullOrWhiteSpace(message.Text))
                    .ToList();
                var latestAssistantIndex = history.FindLastIndex(message => message.Role == ChatRole.Assistant);
                var normalizedHistory = new List<(ChatRole Role, string Text)>();
                var seenUserTurns = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < history.Count; index++)
                {
                    var message = history[index];
                    if (message.Role == ChatRole.Assistant && index != latestAssistantIndex)
                        continue;

                    var normalized = NormalizeCouncilHistoryText(
                        message.Text ?? string.Empty,
                        message.Role == ChatRole.Assistant,
                        logger);
                    if (string.IsNullOrWhiteSpace(normalized))
                        continue;
                    if (message.Role == ChatRole.User && !seenUserTurns.Add(normalized))
                        continue;
                    normalizedHistory.Add((message.Role, normalized));
                }

                const int maximumUserTurns = 12;
                var retainedUserTurns = normalizedHistory.Count(item => item.Role == ChatRole.User);
                var usersToSkip = Math.Max(0, retainedUserTurns - maximumUserTurns);
                var builder = new StringBuilder()
                    .AppendLine("Answer this DXAiChat conversation as the LocalGPT AI Council.")
                    .AppendLine("Use the selected members, preserve user intent, and include a concise consensus.")
                    .AppendLine();
                foreach (var item in normalizedHistory)
                {
                    if (item.Role == ChatRole.User && usersToSkip > 0)
                    {
                        usersToSkip--;
                        continue;
                    }
                    builder
                        .Append(item.Role == ChatRole.Assistant ? "Previous assistant consensus" : "User")
                        .AppendLine(":")
                        .AppendLine(item.Text)
                        .AppendLine();
                }

                var prompt = builder.ToString().Trim();
                return prompt.Length <= catalog.MaxDxAiChatPromptCharacters
                    ? prompt
                    : prompt[^catalog.MaxDxAiChatPromptCharacters..];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the provider prompt from chat messages.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Removes LocalGPT-owned rendering panels and recursive Council wrapper text from one chat-history item.
        /// </summary>
        /// <param name="value">Raw stored chat text.</param>
        /// <param name="assistantMessage">Whether the item is an assistant response.</param>
        /// <param name="logger">Writes bounded normalization diagnostics.</param>
        /// <returns>Plain Markdown suitable for inclusion in a later Council prompt.</returns>
        private string NormalizeCouncilHistoryText(string value, bool assistantMessage, ILogger logger)
        {
            try
            {
                var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
                normalized = assistantMessage
                    ? ExtractLatestAssistantConsensus(normalized)
                    : ExtractInnermostUserRequest(normalized);
                normalized = StripLocalGptRenderingPanels(normalized);
                normalized = Regex.Replace(
                    normalized,
                    @"<!--localgpt-council-stream-complete:[a-f0-9]{32}-->|<p\s+class=""localgpt-stream-status""[^>]*>.*?</p>",
                    string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                normalized = WebUtility.HtmlDecode(normalized);
                normalized = RepairUnbalancedMarkdownFence(normalized);
                normalized = Regex.Replace(
                    normalized,
                    @"(?m)^[ \t]+$",
                    string.Empty,
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                normalized = Regex.Replace(
                    normalized,
                    @"\n{4,}",
                    "\n\n\n",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2)).Trim();

                const int maximumHistoryCharacters = 32_000;
                return normalized.Length <= maximumHistoryCharacters
                    ? normalized
                    : normalized[^maximumHistoryCharacters..];
            }
            catch (RegexMatchTimeoutException exception)
            {
                logger.LogWarning(exception, "Council chat-history cleanup timed out; a bounded plain-text fallback will be used.");
                var fallback = WebUtility.HtmlDecode(value).Trim();
                return fallback.Length <= 8_000 ? fallback : fallback[^8_000..];
            }
        }

        /// <summary>
        /// Removes a lone trailing fence or closes a lone opening fence so one history item cannot break later Markdown.
        /// </summary>
        /// <param name="value">Normalized history Markdown.</param>
        /// <returns>Markdown with balanced fenced-code delimiters.</returns>
        private string RepairUnbalancedMarkdownFence(string value)
        {
    try
    {
                var matches = Regex.Matches(
                    value,
                    @"(?m)^[ \t]*(?<fence>`{3,})[^\r\n]*$",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                if (matches.Count == 0 || matches.Count % 2 == 0)
                    return value;

                var finalMatch = matches[^1];
                if (string.IsNullOrWhiteSpace(value[(finalMatch.Index + finalMatch.Length)..]))
                    return value.Remove(finalMatch.Index, finalMatch.Length).TrimEnd();

                var openingFence = matches[0].Groups["fence"].Value;
                return value.TrimEnd() + Environment.NewLine + openingFence;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(RepairUnbalancedMarkdownFence)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(RepairUnbalancedMarkdownFence)} failed.");
        throw;
    }
}

        /// <summary>
        /// Extracts the last user-authored body when an older Council wrapper was accidentally persisted as user text.
        /// </summary>
        /// <param name="value">Stored user message text.</param>
        /// <returns>The innermost user request without generated Council wrapper headings.</returns>
        private string ExtractInnermostUserRequest(string value)
        {
    try
    {
                if (!value.Contains("AI Council request:", StringComparison.OrdinalIgnoreCase) &&
                    !value.Contains("Answer this DXAiChat conversation", StringComparison.OrdinalIgnoreCase))
                    return value;

                var matches = Regex.Matches(
                    value,
                    @"(?ms)^User:\s*(?<body>.*?)(?=^Previous assistant consensus:|^User:|\z)",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
                var candidate = matches
                    .Select(match => match.Groups["body"].Value.Trim())
                    .LastOrDefault(body => !string.IsNullOrWhiteSpace(body) &&
                        !body.StartsWith("AI Council request:", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(candidate))
                    return candidate;

                return Regex.Replace(
                    value,
                    @"(?im)^(?:AI Council request:|Council members:.*|Answer this DXAiChat conversation.*|Use the selected members.*)\s*$",
                    string.Empty,
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2)).Trim();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractInnermostUserRequest)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractInnermostUserRequest)} failed.");
        throw;
    }
}

        /// <summary>
        /// Keeps only the latest visible consensus/final-answer section from a stored assistant Council response.
        /// </summary>
        /// <param name="value">Stored assistant response text.</param>
        /// <returns>The latest final section or the original value when no known heading exists.</returns>
        private string ExtractLatestAssistantConsensus(string value)
        {
    try
    {
                var markers = new[]
                {
                    "\n## Consensus\n",
                    "\n## Final Answer\n",
                    "\n## Final council answer\n",
                    "\n### Final Answer\n"
                };
                var markerIndex = -1;
                var markerLength = 0;
                foreach (var marker in markers)
                {
                    var index = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (index <= markerIndex)
                        continue;
                    markerIndex = index;
                    markerLength = marker.Length;
                }

                var result = markerIndex >= 0 ? value[(markerIndex + markerLength)..] : value;
                var boundaries = new[]
                {
                    "\n## Continue Action",
                    "\n## Generated Artifact Links",
                    "\n## User Decision Poll",
                    "\n## User decision poll",
                    "\n## Artifacts"
                };
                var boundary = boundaries
                    .Select(item => result.IndexOf(item, StringComparison.OrdinalIgnoreCase))
                    .Where(index => index >= 0)
                    .DefaultIfEmpty(-1)
                    .Min();
                return (boundary >= 0 ? result[..boundary] : result).Trim();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractLatestAssistantConsensus)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(ExtractLatestAssistantConsensus)} failed.");
        throw;
    }
}

        /// <summary>
        /// Removes LocalGPT-owned details/pre panels so model thinking and live process markup never become later prompt content.
        /// </summary>
        /// <param name="value">Stored response text containing optional LocalGPT markup.</param>
        /// <returns>Response text without controlled rendering panels or orphaned panel tags.</returns>
        private string StripLocalGptRenderingPanels(string value)
        {
    try
    {
                var result = value;
                const string controlledPanelPattern = @"<details\s+class=""(?:model-thinking(?:\s+open)?|council-step(?:\s+council-live)?|council-prompt)""[^>]*>.*?</details>";
                for (var pass = 0; pass < 6; pass++)
                {
                    var cleaned = Regex.Replace(
                        result,
                        controlledPanelPattern,
                        string.Empty,
                        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(2));
                    if (string.Equals(cleaned, result, StringComparison.Ordinal))
                        break;
                    result = cleaned;
                }

                return Regex.Replace(
                    result,
                    @"</?(?:details|summary|pre)(?:\s[^>]*)?>",
                    string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(2));
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(StripLocalGptRenderingPanels)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(StripLocalGptRenderingPanels)} failed.");
        throw;
    }
}
        /// <summary>Executes the format step progress operation.</summary>
        /// <param name="step">Input value for step.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string FormatStepProgress(MultiModelCouncilStep step, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine()
                .AppendLine("<p class=\"localgpt-stream-status\"><em>")
                .Append(WebUtility.HtmlEncode($"{step.ModelName} finished {step.Phase} / {step.Role} in {step.DurationSeconds:n1}s. Step details were streamed above; final consensus appears below."))
                .AppendLine("</em></p>")
                .AppendLine();

                if (!string.IsNullOrWhiteSpace(step.Error))
                {
                    builder
                        .AppendLine($"<details class=\"council-step\" open>")
                        .Append("<summary>")
                        .Append(WebUtility.HtmlEncode($"{step.ModelName} error during {step.Phase}"))
                        .AppendLine("</summary>")
                        .AppendLine()
                        .AppendLine("**Error:**")
                        .AppendLine()
                        .AppendLine(step.Error.Trim())
                        .AppendLine()
                        .AppendLine("</details>")
                        .AppendLine();
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatStepProgress step {step.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>Executes the create update operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ChatResponseUpdate CreateUpdate(string text, ILogger logger)
        {
            try
            {

                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create an AI Council stream update.");
                return new ChatResponseUpdate(
                    ChatRole.Assistant,
                    [new TextContent("The AI Council produced an update that could not be rendered. Review LocalGPT logs.")]);
            }
        }
        /// <summary>Executes the build summary operation.</summary>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="length">Input value for length.</param>
        /// <param name="kind">Input value for kind.</param>
        /// <param name="includedInPrompt">Input value for includedInPrompt.</param>
        /// <param name="note">Input value for note.</param>
        /// <param name="excerpt">Input value for excerpt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public AnalyzedUploadFile? BuildSummary(
    string relativePath,
    long length,
    string kind,
    bool includedInPrompt,
    string note,
    string excerpt, ILogger logger)
        {
            try
            {
                return new AnalyzedUploadFile(
                /// <summary>
                /// Runs the chat upload workspace file summary operation.
                /// </summary>
                new ChatUploadWorkspaceFileSummary(
                    relativePath,
                    kind,
                    length,
                    DateTime.UtcNow,
                    includedInPrompt,
                    note),
                text.TrimForPrompt(excerpt, catalog.MaxExcerptCharactersPerFile, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the source summary for {RelativePath}; length {Length}; kind {Kind}; included {IncludedInPrompt}.", relativePath, length, kind, includedInPrompt);
                return null;
            }
        }

        /// <summary>Executes the build binary summary operation.</summary>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="length">Input value for length.</param>
        /// <param name="kind">Input value for kind.</param>
        /// <param name="includedInPrompt">Input value for includedInPrompt.</param>
        /// <param name="note">Input value for note.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public AnalyzedUploadFile? BuildBinarySummary(
            string relativePath,
            long length,
            string kind,
            bool includedInPrompt,
            string note, ILogger logger)
        {
            try
            {
                return BuildSummary(relativePath, length, kind, includedInPrompt, note, string.Empty, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build the binary summary for {RelativePath}; length {Length}; kind {Kind}; included {IncludedInPrompt}.", relativePath, length, kind, includedInPrompt);
                return null;
            }
        }


        /// <summary>Executes the sanitize for prompt operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
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
        /// <summary>Executes the analyze bytes operation.</summary>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="bytes">Input value for bytes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public AnalyzedUploadFile? AnalyzeBytes(string relativePath, byte[] bytes, ILogger logger)
        {
            try
            {
                if (IsZip(relativePath, logger))
                {
                    return BuildBinarySummary(
                        relativePath,
                        bytes.Length,
                        "zip",
                        false,
                        "Zip file saved as uploaded. Extracted safe entries are represented separately.", logger);
                }

                var isText = IsTextLike(relativePath, logger) || LooksLikeText(bytes, logger);
                if (isText)
                {
                    var texti = text.DecodeText(bytes, logger);
                    return BuildSummary(relativePath, bytes.Length, "text", true, "Text excerpt included.", texti, logger);
                }

                var extension = Path.GetExtension(relativePath);
                if (catalog.BinaryDiagnosticExtensions.Contains(extension))
                {
                    var strings = text.ExtractPrintableStrings(bytes, catalog.MaxBinaryStringCharacters, logger);
                    var note = extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase)
                        ? "PDB/debug file summarized with printable strings only."
                        : "Binary file summarized with printable strings only.";
                    return BuildSummary(relativePath, bytes.Length, "binary-strings", true, note, strings, logger);
                }

                return BuildBinarySummary(
                    relativePath,
                    bytes.Length,
                    "binary",
                    false,
                    "Binary file saved but not included in prompt context.", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AnalyzeBytes relativePath: {relativePath.ToString()} bytes: {bytes.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the build context markdown operation.</summary>
        /// <param name="workspaceName">Input value for workspaceName.</param>
        /// <param name="root">Input value for root.</param>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="analyzedFiles">Input value for analyzedFiles.</param>
        /// <param name="warnings">Input value for warnings.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildContextMarkdown(
            string workspaceName,
            string root,
            string prompt,
            IReadOnlyList<AnalyzedUploadFile> analyzedFiles,
            IReadOnlyList<string> warnings, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
              .AppendLine("# LocalGPT Chat Upload Workspace")
              .AppendLine()
              .AppendLine($"Workspace: `{workspaceName}`")
              .AppendLine($"Root path: `{root}`")
              .AppendLine($"Created UTC: {DateTimeOffset.UtcNow:O}")
              .AppendLine()
              .AppendLine("## Prompt")
              .AppendLine(text.TrimForPrompt(prompt, 4_000, logger))
              .AppendLine()
              .AppendLine("## AI workflow instructions")
              .AppendLine("- Use this workspace as uploaded user evidence for the current DXAiChat prompt.")
              .AppendLine("- Read files through chat.upload_workspace_* DXAiFunctions instead of asking for huge pasted context.")
              .AppendLine("- Zips are extracted safely; skipped entries are listed as warnings.")
              .AppendLine("- PDB, DLL, EXE, WASM, and other binaries are never executed; only bounded printable strings are shown.")
              .AppendLine("- Generated or edited code belongs in a council artifact workspace, then a refreshed zip download.")
              .AppendLine();

                if (warnings.Count > 0)
                {
                    builder.AppendLine("## Warnings");
                    foreach (var warning in warnings)
                        builder.AppendLine($"- {warning}");
                    builder.AppendLine();
                }

                builder.AppendLine("## Files");
                foreach (var file in analyzedFiles.Select(file => file.Summary))
                {
                    builder
                        .Append("- ")
                        .Append(file.RelativePath)
                        .Append(" (")
                        .Append(file.Kind)
                        .Append(", ")
                        .Append(file.Length)
                        .Append(" bytes): ")
                        .AppendLine(file.Note);
                }

                builder.AppendLine();
                builder.AppendLine("## Extracted context");

                var remainingCharacters = catalog.MaxContextCharacters - builder.Length;
                foreach (var file in analyzedFiles.Where(file => file.Summary.IncludedInPrompt))
                {
                    if (remainingCharacters <= 0)
                        break;

                    var excerpt = text.TrimForPrompt(file.Excerpt, Math.Min(catalog.MaxExcerptCharactersPerFile, remainingCharacters), logger);
                    if (string.IsNullOrWhiteSpace(excerpt))
                        continue;

                    var section = new StringBuilder()
                        .AppendLine()
                        .AppendLine($"### {file.Summary.RelativePath}")
                        .AppendLine($"Kind: {file.Summary.Kind}. {file.Summary.Note}")
                        .AppendLine()
                        .AppendLine("```text")
                        .AppendLine(excerpt)
                        .AppendLine("```")
                        .ToString();

                    if (section.Length > remainingCharacters)
                        section = text.TrimForPrompt(section, remainingCharacters, logger);

                    builder.Append(section);
                    remainingCharacters -= section.Length;
                }

                return text.TrimForPrompt(builder.ToString(), catalog.MaxContextCharacters, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build upload-workspace context Markdown for {WorkspaceName}.", workspaceName);
                return string.Empty;
            }

        }

        /// <summary>Executes the resolve workspace file operation.</summary>
        /// <param name="workspace">Input value for workspace.</param>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? ResolveWorkspaceFile(string workspace, string relativePath, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalized))
                    return null;

                var root = Path.GetFullPath(workspace);
                var file = Path.GetFullPath(Path.Combine(root, normalized));
                return IsInsideRoot(root, file, logger) ? file : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error ResolveWorkspaceFile workspace {workspace} relativePath {relativePath}");
                return null;
            }
        }

        /// <summary>Executes the is inside root operation.</summary>
        /// <param name="root">Input value for root.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsInsideRoot(string root, string path, ILogger logger)
        {
            try
            {
                var normalizedRoot = Path.GetFullPath(root)
      .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
      Path.DirectorySeparatorChar;
                var normalizedPath = Path.GetFullPath(path);
                return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsInsideRoot root {root} path {path}");
                return false;
            }
        }

        /// <summary>Executes the build workspace name operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="files">Input value for files.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildWorkspaceName(
            string prompt,
            IReadOnlyList<ChatUploadWorkspaceInputFile> files, ILogger logger)
        {
            try
            {
                var source = files.FirstOrDefault()?.Name;
                if (string.IsNullOrWhiteSpace(source))
                    source = prompt;

                var slug = Regex.Replace(source ?? "prompt", "[^A-Za-z0-9]+", "-")
                    .Trim('-')
                    .ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(slug))
                    slug = "prompt";
                if (slug.Length > 24)
                    slug = slug[..24].Trim('-');

                var suffix = Guid.NewGuid().ToString("N")[..8];
                return $"chat-upload-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{slug}-{suffix}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build an upload-workspace name.");
                return string.Empty;
            }
        }

        /// <summary>Executes the is zip operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsZip(string path, ILogger logger)
        {
            try
            {
                return Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsZip path {path}");
                return false;
            }
        }


        /// <summary>Executes the is text like operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsTextLike(string path, ILogger logger)
        {
            try
            {
                return catalog.TextExtensions.Contains(Path.GetExtension(path));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsTextLike path {path}");
                return false;
            }
        }

        /// <summary>Executes the determine file kind operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string DetermineFileKind(string path, ILogger logger)
        {
            try
            {
                if (IsZip(path, logger))
                    return "zip";
                if (IsTextLike(path, logger))
                    return "text";
                return catalog.BinaryDiagnosticExtensions.Contains(Path.GetExtension(path))
                    ? "binary-diagnostic"
                    : "binary";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error DetermineFileKind path {path}");
                return string.Empty;
            }
        }

        /// <summary>Executes the looks like text operation.</summary>
        /// <param name="bytes">Input value for bytes.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool LooksLikeText(byte[] bytes, ILogger logger)
        {
            try
            {
                if (bytes.Length == 0)
                    return true;

                var sampleLength = Math.Min(bytes.Length, 8192);
                var controlCount = 0;
                for (var i = 0; i < sampleLength; i++)
                {
                    var value = bytes[i];
                    if (value == 0)
                        return false;

                    if (value < 9 || (value > 13 && value < 32))
                        controlCount++;
                }

                return controlCount <= sampleLength / 20;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error LooksLikeText bytes {bytes.ToString()}");
                return false;
            }

        }
        /// <summary>Executes the multi model council service build log markdown operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string MultiModelCouncilServiceBuildLogMarkdown(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine($"# AI Council {result.RunId}")
                .AppendLine()
                .AppendLine($"Started: {result.StartedAtUtc:u}")
                .AppendLine($"Completed: {result.CompletedAtUtc:u}")
                .AppendLine($"Models: {string.Join(", ", result.ModelNames)}")
                .AppendLine(result.KnowledgeEntryId is Guid knowledgeId ? $"Knowledge entry: {knowledgeId}" : "Knowledge entry: not saved")
                .AppendLine()
                .AppendLine("## Original Prompt / User Request Audit")
                .AppendLine()
                .AppendLine("This is the exact prompt LocalGPT sent into the AI Council, including the reconstructed DXAiChat conversation when the run came from the chat window.")
                .AppendLine()
                .AppendLine(result.Prompt)
                .AppendLine();

                if (result.ContinuedFromConversationId is Guid continuedFrom)
                {
                    builder
                        .AppendLine("## Continued Conversation")
                        .AppendLine()
                        .AppendLine($"Conversation: {continuedFrom}")
                        .AppendLine($"Title: {result.ContinuedFromTitle ?? "Unknown"}")
                        .AppendLine();
                }

                if (result.Warnings.Count > 0)
                {
                    builder.AppendLine("## Warnings").AppendLine();
                    foreach (var warning in result.Warnings)
                        builder.AppendLine($"- {warning}");
                    builder.AppendLine();
                }

                builder.AppendLine("## Transcript").AppendLine();
                foreach (var step in result.Steps.OrderBy(step => step.SortOrder))
                {
                    builder
                        .AppendLine($"### {step.Phase}: {step.ModelName}")
                        .AppendLine()
                        .AppendLine($"Role: {step.Role}")
                        .AppendLine($"Council members: {string.Join(", ", step.CouncilMembers)}")
                        .AppendLine($"Round: {step.Round}")
                        .AppendLine($"Duration: {step.DurationSeconds:0.0}s")
                        .AppendLine();

                    if (!string.IsNullOrWhiteSpace(step.Thinking))
                        builder.AppendLine("#### Visible model thinking").AppendLine().AppendLine(step.Thinking).AppendLine();

                    builder.AppendLine(step.VisibleContent).AppendLine();
                }

                if (result.UserPoll is not null)
                {
                    builder.AppendLine("## User Decision Poll").AppendLine().AppendLine(text.MultiModelCouncilServiceBuildPollMarkdown(result.UserPoll, logger)).AppendLine();
                }

                builder.AppendLine("## Final Answer").AppendLine().AppendLine(result.FinalAnswer).AppendLine();

                if (result.Artifacts.Count > 0)
                {
                    builder.AppendLine("## Artifacts").AppendLine().AppendLine(text.MultiModelCouncilServiceBuildArtifactsMarkdown(result.Artifacts, logger)).AppendLine();
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildLogMarkdown");
                return string.Empty;
            }
        }

        /// <summary>Executes the multi model council service build user poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildUserPoll(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var failedModels = result.Steps
              .Where(step => !string.IsNullOrWhiteSpace(step.Error))
              .Select(step => step.ModelName)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();

                var promptLooksFrustrated = MultiModelCouncilServiceIsFrustratedPrompt(result.Prompt, logger);
                var needsAiHostSetupDecision = MultiModelCouncilServiceNeedsAiHostSetupDecision(result, logger);
                var needsImplementationPathDecision = MultiModelCouncilServiceNeedsImplementationPathDecision(result, logger);

                if (failedModels.Count == 0 && !promptLooksFrustrated && !needsAiHostSetupDecision && !needsImplementationPathDecision)
                    return null;

                if (promptLooksFrustrated)
                    return MultiModelCouncilServiceBuildFrustrationPoll(result, failedModels, logger);

                if (needsAiHostSetupDecision)
                    return MultiModelCouncilServiceBuildAiHostSetupPoll(result, failedModels, logger);

                if (needsImplementationPathDecision)
                    return MultiModelCouncilServiceBuildImplementationPathPoll(result, failedModels, logger);

                if (failedModels.Count == 0)
                    return null;

                var reason = $"The council could not fully sync because these participant(s) failed or were unavailable: {string.Join(", ", failedModels)}.";

                var options = new List<CouncilUserPollOption>();
                if (failedModels.Count > 0)
                {
                    options.Add(new CouncilUserPollOption
                    {
                        Label = "Exclude faulty members",
                        FollowUpPrompt = $"Exclude these council member(s) from the next round unless the user re-adds them: {string.Join(", ", failedModels)}. Continue with the remaining selected models, preserve the prior transcript, and clearly note that the exclusion was user-confirmed."
                    });
                }

                options.AddRange(
                [
                    new CouncilUserPollOption
                {
                    Label = "Wait and retry missing models",
                    FollowUpPrompt = "Wait until all requested Ollama models are installed and visible, then rerun the same council prompt. Every participant must read the prior transcript, acknowledge the user selected retry, and produce updated proposals."
                },
                new CouncilUserPollOption
                {
                    Label = "Proceed with available models",
                    FollowUpPrompt = "Continue with only the currently available models. Every participant must acknowledge which models were unavailable and avoid claiming absent models agreed."
                },
                new CouncilUserPollOption
                {
                    Label = "Ask a shorter tie-break question",
                    FollowUpPrompt = "Ask the user one focused follow-up question that resolves the blocked decision, then rerun the council using the user's answer as binding context."
                }
                ]);

                return new CouncilUserPoll
                {
                    Question = "How should the AI Council continue so every model stays aligned with your decision?",
                    Reason = reason,
                    Options = options
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildUserPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service build implementation path poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="failedModels">Input value for failedModels.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildImplementationPathPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
        {
            try
            {
                var missingModelNote = failedModels.Count > 0
               ? $" Some participant(s) also failed or were unavailable: {string.Join(", ", failedModels)}."
               : string.Empty;

                return new CouncilUserPoll
                {
                    Question = "Which implementation path should the AI Council use before it generates code or files?",
                    Reason = "This looks like a development request with more than one reasonable architecture path. " +
                        $"The council should ask for your direction instead of choosing unclear scope on its own.{missingModelNote}",
                    Options =
                    [
                        new CouncilUserPollOption
                    {
                        Label = "Ask architecture first",
                        FollowUpPrompt = "Stop generation and ask the user for the minimum missing architecture decisions. Include target platform/runtime, language/framework, UI stack if any, data/persistence model, solution shape, deployment target, and expected downloadable artifacts. Do not generate files until the user answers."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Sandbox prototype first",
                        FollowUpPrompt = "Use a harmless sandbox artifact or temporary workspace first. Generate downloadable example files only after the user confirms the architecture, name the smoke tests, and do not integrate changes into the real project until the user approves the prototype direction."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Use repository default",
                        FollowUpPrompt = "Use the target repository's existing architecture and libraries. If the repo is LocalGPT, prefer .NET 10, ASP.NET Core/Blazor Server InteractiveServer, DevExpress Blazor where suitable, EF/SQLite for persistent app state, backend services for native/file operations, and safe download routes. If a different repo is targeted, inspect that repo before choosing."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Target-specific stack",
                        FollowUpPrompt = "Do not force LocalGPT's Blazor/DevExpress defaults. Choose the stack that matches the requested product: datapack for vanilla Minecraft data/commands, Fabric/NeoForge/Paper for Java mod/plugin work, ASP.NET Core API for service work, WebView2/WinUI for Windows desktop wrapper work, CLI/tooling for automation, or another explicit user-chosen target."
                    }
                    ]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildImplementationPathPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service build ai host setup poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="failedModels">Input value for failedModels.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildAiHostSetupPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
        {
            try
            {

                var missingModelNote = failedModels.Count > 0
                    ? $" Some participant(s) also failed or were unavailable: {string.Join(", ", failedModels)}."
                    : string.Empty;

                return new CouncilUserPoll
                {
                    Question = "Which native model-runner setup should the AI host artifact use next?",
                    Reason = "The council generated the sandbox AI-host artifact, but local model execution still needs concrete setup choices. " +
                        "This is not a missing-model problem; it is the runner and model-file contract that must be selected before real inference can be proven." +
                        missingModelNote,
                    Options =
                    [
                        new CouncilUserPollOption
                    {
                        Label = "Use llama.cpp GGUF",
                        FollowUpPrompt = "Continue the same generated AI-host workspace using a user-approved llama.cpp style runner executable boundary and GGUF model files. Add settings for NativeRunnerExecutable, ModelSearchRoots, context size, GPU/layer policy, and per-model session scheduling. Keep no upstream AI-host proxy fallback."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Use Python.NET runner",
                        FollowUpPrompt = "Continue the same generated AI-host workspace with a Python.NET runner boundary. Require user-approved Python runtime path, PYTHONNET_PYDLL, package list, model roots, and a safe backend service contract. Keep the UI in .NET/DevExpress and do not execute unapproved Python code."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Keep setup-needed",
                        FollowUpPrompt = "Keep the artifact buildable and explicit with Setup Needed banners, no proxy fallback, provider-compatible API routes, SQLite settings, and clear user instructions. Do not pretend native inference works until a runner executable and compatible model-file format are supplied."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Custom runner contract",
                        FollowUpPrompt = "Ask the user for a custom native runner executable, model-file format, arguments, streaming protocol, cancellation behavior, and hardware policy, then continue the generated workspace with those exact choices."
                    }
                    ]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildAiHostSetupPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service build frustration poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="failedModels">Input value for failedModels.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildFrustrationPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
        {
            try
            {
                var missingModelNote = failedModels.Count > 0
               ? $" Some participant(s) also failed or were unavailable: {string.Join(", ", failedModels)}."
               : string.Empty;

                return new CouncilUserPoll
                {
                    Question = "Which technical recovery path should the AI Council use for the next round?",
                    Reason = $"The request sounds frustrated or blocked. The council should pause, stay kind to the user and to each other, and ask for a concrete recovery choice instead of guessing.{missingModelNote}",
                    Options =
                    [
                        new CouncilUserPollOption
                    {
                        Label = "Stabilize first",
                        FollowUpPrompt = "Treat the user's frustration as a signal to stabilize the system first. Ask the models to produce a minimal reproduction checklist, current failure symptoms, logs to inspect, and the smallest safe next command. Document any missing LocalGPT feature as a database memory item."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Implement missing feature",
                        FollowUpPrompt = "Ask the models to identify the missing LocalGPT feature causing the user's frustration, propose the smallest implementation, and document the requested feature plus rationale in SQLite memory before coding."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Reduce scope",
                        FollowUpPrompt = "Ask the models to reduce the task to the safest next milestone, name what will not be attempted yet, and document blocked or missing features in SQLite memory for later council rounds."
                    }
                    ]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildFrustrationPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service is frustrated prompt operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceIsFrustratedPrompt(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                var currentUserText = MultiModelCouncilServiceCurrentUserTextForSignalClassification(prompt, logger);
                var phraseMarkers = new[]
                {
                    "does not work",
                    "doesn't work",
                    "geht nicht",
                    "funktioniert nicht"
                };
                if (phraseMarkers.Any(marker => currentUserText.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    return true;

                var wordMarkers = new[]
                {
                    "angry",
                    "mad",
                    "frustrated",
                    "annoyed",
                    "upset",
                    "broken",
                    "stuck",
                    "wtf",
                    "fuck",
                    "shit",
                    "wütend",
                    "wuetend",
                    "sauer",
                    "frustriert",
                    "nervt",
                    "kaputt",
                    "scheisse",
                    "scheiße"
                };

                return wordMarkers.Any(marker => System.Text.RegularExpressions.Regex.IsMatch(
                    currentUserText,
                    $@"(?<![\p{{L}}\p{{N}}_]){System.Text.RegularExpressions.Regex.Escape(marker)}(?![\p{{L}}\p{{N}}_])",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify prompt frustration state.");
                return false;
            }

        }

        /// <summary>
        /// Runs the multi model council service current user text for signal classification operation.
        /// </summary>
        private string MultiModelCouncilServiceCurrentUserTextForSignalClassification(string prompt, ILogger logger)
        {
            try
            {
                var currentUserText = prompt;
                var userMarkerIndex = prompt.LastIndexOf("\nUser:", StringComparison.OrdinalIgnoreCase);
                if (userMarkerIndex >= 0)
                    currentUserText = prompt[(userMarkerIndex + "\nUser:".Length)..];

                var priorTranscriptIndex = currentUserText.IndexOf("\nPrior transcript:", StringComparison.OrdinalIgnoreCase);
                if (priorTranscriptIndex >= 0)
                    currentUserText = currentUserText[..priorTranscriptIndex];

                return currentUserText.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not isolate the latest user text for council signal classification.");
                return prompt;
            }
        }

        /// <summary>Executes the multi model council service needs implementation path decision operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceNeedsImplementationPathDecision(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (!MultiModelCouncilServiceIsDevelopmentRequest(result.Prompt, logger))
                    return false;

                if (MultiModelCouncilServiceHasExplicitArtifactIntent(result.Prompt, logger))
                    return false;

                var text = result.Prompt;
                if (catalog.ImplementationDecisionPattern.IsMatch(text))
                    return true;

                var areaHits = MultiModelCouncilServiceCountImplementationAreaHits(text, logger);
                return areaHits >= 3 && catalog.ImplementationChoicePattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "NeedsImplementationPathDecision");
                return false;
            }

        }

        /// <summary>Executes the multi model council service needs ai host setup decision operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceNeedsAiHostSetupDecision(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var text = result.Prompt;
                if (!catalog.AiHostSetupPattern.IsMatch(text))
                    return false;

                return text.Contains("setup needed", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("native runner executable", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("runner path", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model-file format", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model file format", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "NeedsAiHostSetupDecision");
                return false;
            }
        }

        /// <summary>Executes the multi model council service is development request operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceIsDevelopmentRequest(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.DevelopmentRequestPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify a development request.");
                return false;
            }
        }

        /// <summary>Executes the multi model council service has explicit artifact intent operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceHasExplicitArtifactIntent(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.ExplicitArtifactIntentPattern.IsMatch(prompt) ||
                    catalog.ConcreteMinecraftArtifactPattern.IsMatch(prompt) ||
                    catalog.ConcreteDotNetArtifactPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify explicit artifact intent.");
                return false;
            }
        }


        /// <summary>Executes the multi model council service requires user decision before artifacts operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceRequiresUserDecisionBeforeArtifacts(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (MultiModelCouncilServiceUserGrantedSafeSandboxChoice(result.Prompt, logger) || MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(result.Prompt, logger))
                    return false;

                var text = $"{result.Prompt} {result.FinalAnswer}";
                return catalog.BlockingArtifactDecisionPattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "RequiresUserDecisionBeforeArtifacts");
                return false;
            }

        }

        /// <summary>Executes the multi model council service user granted safe sandbox choice operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceUserGrantedSafeSandboxChoice(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.SafeSandboxConsentPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify sandbox consent.");
                return false;
            }

        }

        /// <summary>Executes the multi model council service should generate safe sandbox artifact without blocking operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                if (catalog.ExplicitDoNotGenerateUntilUserDecisionPattern.IsMatch(prompt))
                    return false;

                return MultiModelCouncilServiceHasExplicitArtifactIntent(prompt, logger) ||
                    catalog.DeveloperExecutionIntentPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify safe sandbox artifact generation intent.");
                return false;
            }

        }

        /// <summary>Executes the multi model council service count implementation area hits operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int MultiModelCouncilServiceCountImplementationAreaHits(string text, ILogger logger)
        {
            try
            {
                var hits = 0;
                var areas = new[]
                {
                "backend",
                "frontend",
                "blazor",
                "razor",
                "devexpress",
                "database",
                "sqlite",
                "entityframework",
                "ef",
                "service",
                "api",
                "endpoint",
                "winui",
                "webview2",
                "minecraft",
                "datapack",
                "fabric",
                "neoforge",
                "paper",
                "artifact",
                "download"
            };

                foreach (var area in areas)
                {
                    if (text.Contains(area, StringComparison.OrdinalIgnoreCase))
                        hits++;
                }

                return hits;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CountImplementationAreaHits text {text?.ToString()}");
                return -1;
            }

        }
        /// <summary>Executes the ollama thinking chat client ensure success or throw async operation.</summary>
        /// <param name="response">Input value for response.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task OllamaThinkingChatClientEnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                if (response.IsSuccessStatusCode)
                    return;

                var body = await OllamaThinkingChatClientReadErrorBodyAsync(response, cancellationToken, logger).ConfigureAwait(false);
                var message = string.IsNullOrWhiteSpace(body)
                    ? $"Ollama returned {(int)response.StatusCode} {response.StatusCode}."
                    : $"Ollama returned {(int)response.StatusCode} {response.StatusCode}: {body}";
                throw new HttpRequestException(message, null, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ollama HTTP response validation failed with status {StatusCode}.", response.StatusCode);

            }
        }

        /// <summary>Executes the ollama thinking chat client read error body async operation.</summary>
        /// <param name="response">Input value for response.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes with the operation result.</returns>
        public async Task<string> OllamaThinkingChatClientReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(body))
                    return string.Empty;

                return body.Length <= 4000 ? body.Trim() : body[..4000].Trim() + "...";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not read an Ollama error response body with status {StatusCode}.", response.StatusCode);
                return string.Empty;
            }
        }

        /// <summary>Executes the ollama thinking chat client create streaming update operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ChatResponseUpdate? OllamaThinkingChatClientCreateStreamingUpdate(string text, ILogger logger)
        {
            try
            {
                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingUpdate text {text.ToString()}");
                return null;
            }
        }


        /// <summary>
        /// Determines whether local gpt streaming status update.
        /// </summary>
        public bool IsLocalGptStreamingStatusUpdate(string? text, ILogger logger)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(text)
                    && text.Contains("class=\"localgpt-stream-status\"", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify a LocalGPT streaming status update.");
                return false;
            }
        }

        /// <summary>Executes the ollama thinking chat client create streaming status update operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ChatResponseUpdate? OllamaThinkingChatClientCreateStreamingStatusUpdate(string text, ILogger logger)
        {
            try
            {
                return OllamaThinkingChatClientCreateStreamingUpdate($"<p class=\"localgpt-stream-status\"><em>{WebUtility.HtmlEncode(text)}</em></p>\n\n", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingStatusUpdate text {text.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the append dev express imports async operation.</summary>
        /// <param name="builder">Input value for builder.</param>
        /// <param name="root">Input value for root.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task AppendDevExpressImportsAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var importsPath = Path.Combine(root, "src", "LocalGPT", "Components", "_Imports.razor");
                if (!File.Exists(importsPath))
                    return;

                var text = await File.ReadAllTextAsync(importsPath, cancellationToken).ConfigureAwait(false);
                var imports = catalog.DevExpressImportPattern
                    .Matches(text)
                    .Select(match => match.Groups["namespace"].Value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();

                if (imports.Count == 0)
                    return;

                builder.AppendLine("- Imported DevExpress namespaces in Blazor:");
                foreach (var item in imports)
                    builder.AppendLine($"  - {item}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendDevExpressImportsAsync builder {builder.ToString()} root {root.ToString()}");
            }

        }

        /// <summary>Executes the append dev express registrations async operation.</summary>
        /// <param name="builder">Input value for builder.</param>
        /// <param name="root">Input value for root.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task AppendDevExpressRegistrationsAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var programPath = Path.Combine(root, "src", "LocalGPT", "Program.cs");
                if (!File.Exists(programPath))
                    return;

                var text = await File.ReadAllTextAsync(programPath, cancellationToken).ConfigureAwait(false);
                var registrations = catalog.DevExpressRegistrationPattern
                    .Matches(text)
                    .Select(match => match.Value.TrimEnd('('))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();

                if (registrations.Count == 0)
                    return;

                builder.AppendLine("- DevExpress services registered in ASP.NET Core:");
                foreach (var registration in registrations)
                    builder.AppendLine($"  - {registration}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendDevExpressRegistrationsAsync builder {builder.ToString()} root {root.ToString()}");
            }
            
        }

        /// <summary>Executes the append loaded dev express assemblies operation.</summary>
        /// <param name="builder">Input value for builder.</param>
        /// <param name="logger">Input value for logger.</param>
        public void AppendLoadedDevExpressAssemblies(StringBuilder builder, ILogger logger)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName())
                .Where(name => name.Name?.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(name => name.Name)
                .Take(30)
                .ToList();

                if (assemblies.Count == 0)
                    return;

                builder.AppendLine("- Loaded DevExpress assemblies:");
                foreach (var assembly in assemblies)
                    builder.AppendLine($"  - {assembly.Name} {assembly.Version}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendLoadedDevExpressAssemblies builder {builder.ToString()}");
            }
            
        }

        /// <summary>Executes the find repository root operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? FindRepositoryRoot(ILogger logger)
        {
            try
            {
                foreach (var start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
                {
                    var directory = new DirectoryInfo(start);
                    while (directory is not null)
                    {
                        if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                            Directory.Exists(Path.Combine(directory.FullName, ".git")))
                        {
                            return directory.FullName;
                        }

                        directory = directory.Parent;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FindRepositoryRoot");
                return null;
            }
            
        }
        /// <summary>Executes the create chat upload smoke zip operation.</summary>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public byte[] CreateChatUploadSmokeZip( ILogger logger)
        {
            try
            {
                using var memory = new MemoryStream();
                using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
                {
                    WriteZipEntry(archive, "WeatherHost/WeatherHost.sln", """
                    Microsoft Visual Studio Solution File, Format Version 12.00
                    # Visual Studio Version 17
                    Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WeatherHost", "src\WeatherHost\WeatherHost.csproj", "{11111111-1111-1111-1111-111111111111}"
                    EndProject
                    Global
                    EndGlobal
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/WeatherHost.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk.Web">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                      </PropertyGroup>
                    </Project>
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Program.cs", """
                    using WeatherHost.Services;

                    var builder = WebApplication.CreateBuilder(args);
                    builder.Services.AddRazorPages();
                    builder.Services.AddServerSideBlazor();
                    builder.Services.AddScoped<WeatherForecastService>();

                    var app = builder.Build();
                    app.MapGet("/api/weather", (WeatherForecastService service) => service.GetForecasts());
                    app.MapBlazorHub();
                    app.MapFallbackToPage("/_Host");
                    app.Run();
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Services/WeatherForecastService.cs", """
                    namespace WeatherHost.Services;

                    public sealed class WeatherForecastService
                    {
                        public IReadOnlyList<WeatherForecast> GetForecasts() =>
                        [
                            new(DateOnly.FromDateTime(DateTime.Today), 21, "Clear"),
                            new(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), 18, "Rain"),
                            new(DateOnly.FromDateTime(DateTime.Today.AddDays(2)), 24, "Sunny")
                        ];
                    }

                    public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string Summary);
                    """, logger);
                    WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Pages/Index.razor", """
                    @page "/"
                    @inject WeatherHost.Services.WeatherForecastService Weather

                    <h1>Weather Host</h1>

                    <ul>
                        @foreach (var item in Weather.GetForecasts())
                        {
                            <li>@item.Date: @item.TemperatureC C, @item.Summary</li>
                        }
                    </ul>
                    """, logger);
                }

                return memory.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"CreateChatUploadSmokeZip");
                return new byte[0];
            }
        }
        /// <summary>Executes the write zip entry operation.</summary>
        /// <param name="archive">Input value for archive.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="content">Input value for content.</param>
        /// <param name="logger">Input value for logger.</param>
        public void WriteZipEntry(ZipArchive archive, string path, string content, ILogger logger)
        {
            try
            {
                var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content.Replace("                    ", string.Empty, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteZipEntry");
            }
        }

        /// <summary>Executes the get request base url operation.</summary>
        /// <param name="httpContext">Input value for httpContext.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string GetRequestBaseUrl(HttpContext httpContext, ILogger logger)
        {
            try
            {
                var request = httpContext.Request;
                return $"{request.Scheme}://{request.Host}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GetRequestBaseUrl");
                return string.Empty;
            }
        }


        /// <summary>Executes the enumerate artifact workspaces operation.</summary>
        /// <param name="artifactRoot">Input value for artifactRoot.</param>
        /// <param name="take">Input value for take.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<ArtifactWorkspaceSummary> EnumerateArtifactWorkspaces(string artifactRoot, int take, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(artifactRoot))
                    return [];

                return Directory
                    .EnumerateDirectories(artifactRoot)
                    .Select(path => BuildArtifactWorkspaceSummary(artifactRoot, path, logger))
                    .Where(summary => summary is not null)
                    .Cast<ArtifactWorkspaceSummary>()
                    .OrderByDescending(summary => summary.LastWriteTimeUtc)
                    .Take(Math.Clamp(take, 1, 100))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EnumerateArtifactWorkspaces artifactRoot {artifactRoot.ToString()} take {take.ToString()}");
                return new List<ArtifactWorkspaceSummary>();
            }
        }
        /// <summary>Executes the datapack reference comparison missing operation.</summary>
        /// <param name="generatedZipPath">Input value for generatedZipPath.</param>
        /// <param name="referenceZipPath">Input value for referenceZipPath.</param>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public DatapackReferenceComparison? DatapackReferenceComparisonMissing(string generatedZipPath, string referenceZipPath, string summary, ILogger logger)
        {
            try
            {
               return new(
                    GeneratedZipPath: generatedZipPath,
                    ReferenceZipPath: referenceZipPath,
                    ReferenceExists: System.IO.File.Exists(referenceZipPath),
                    GeneratedFileCount: 0,
                    GeneratedFunctionFileCount: 0,
                    GeneratedPlaceholderCount: 0,
                    ReferenceFileCount: 0,
                    ReferenceFunctionFileCount: 0,
                    ReferencePlaceholderCount: 0,
                    GeneratedHasRootPackMcmeta: false,
                    ReferenceHasRootPackMcmeta: false,
                    ReferenceHasNestedPackMcmeta: false,
                    GeneratedHasLoadTag: false,
                    GeneratedHasTickTag: false,
                    ReferenceHasLoadTag: false,
                    ReferenceHasTickTag: false,
                    CriticalFileCount: 0,
                    PreservedCriticalFileCount: 0,
                    PreservedCriticalFiles: [],
                    ReferencePlaceholderSamples: [],
                    Summary: summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Missing generatedZipPath {generatedZipPath.ToString()} referenceZipPath {referenceZipPath.ToString()} summary {summary.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the build datapack reference comparison operation.</summary>
        /// <param name="workspaceRoot">Input value for workspaceRoot.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public DatapackReferenceComparison? BuildDatapackReferenceComparison(string workspaceRoot, ILogger logger)
        {
            try
            {
                var generatedZip = Directory.Exists(Path.Combine(workspaceRoot, "build"))
      ? Directory.GetFiles(Path.Combine(workspaceRoot, "build"), "*.zip").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty
      : string.Empty;
                var referenceZip = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "living_cities.zip");

                if (string.IsNullOrWhiteSpace(generatedZip) || !System.IO.File.Exists(generatedZip))
                {
                    return DatapackReferenceComparisonMissing(
                        generatedZip,
                        referenceZip,
                        "Generated benchmark zip was not found.", logger);
                }

                if (!System.IO.File.Exists(referenceZip))
                {
                    return DatapackReferenceComparisonMissing(
                        generatedZip,
                        referenceZip,
                        "Reference living_cities.zip was not found in Downloads.", logger);
                }

                var generatedEntries = ReadZipFileEntries(generatedZip, logger);
                var referenceEntries = ReadZipFileEntries(referenceZip, logger);
                var normalizedReferenceEntries = referenceEntries
                    .Select(filter => NormalizeReferenceDatapackEntry(filter, logger))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();

                var generatedSet = generatedEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var normalizedReferenceSet = normalizedReferenceEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var criticalFiles = new[]
                {
                "pack.mcmeta",
                "data/minecraft/tags/function/load.json",
                "data/minecraft/tags/function/tick.json",
                "data/living_cities/function/core/load.mcfunction",
                "data/living_cities/function/core/tick.mcfunction",
                "data/living_cities/function/city/create.mcfunction",
                "data/living_cities/function/citizens/register.mcfunction",
                "data/living_cities/function/ui/status.mcfunction"
            };
                var preservedCriticalFiles = criticalFiles
                    .Where(file => generatedSet.Contains(file) && normalizedReferenceSet.Contains(file))
                    .ToArray();
                var generatedPlaceholders = generatedEntries
                    .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var referencePlaceholders = referenceEntries
                    .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var summary = string.Join(" ", new[]
                {
                $"Generated zip has {generatedEntries.Length} files and {generatedEntries.Count(filter => IsMcFunctionPath(filter,logger))} functions.",
                $"Reference zip has {referenceEntries.Length} files and {referenceEntries.Count(filter => IsMcFunctionPath(filter,logger))} real functions plus {referencePlaceholders.Length} placeholders.",
                "Generated zip has root pack.mcmeta/load/tick tags; reference keeps those under a top-level folder, so it is useful as a design benchmark but less install-ready as a zip."
            });

                return new DatapackReferenceComparison(
                    GeneratedZipPath: generatedZip,
                    ReferenceZipPath: referenceZip,
                    ReferenceExists: true,
                    GeneratedFileCount: generatedEntries.Length,
                    GeneratedFunctionFileCount: generatedEntries.Count(filter => IsMcFunctionPath(filter, logger)),
                    GeneratedPlaceholderCount: generatedPlaceholders.Length,
                    ReferenceFileCount: referenceEntries.Length,
                    ReferenceFunctionFileCount: referenceEntries.Count(filter => IsMcFunctionPath(filter, logger)),
                    ReferencePlaceholderCount: referencePlaceholders.Length,
                    GeneratedHasRootPackMcmeta: generatedSet.Contains("pack.mcmeta"),
                    ReferenceHasRootPackMcmeta: referenceEntries.Contains("pack.mcmeta", StringComparer.OrdinalIgnoreCase),
                    ReferenceHasNestedPackMcmeta: normalizedReferenceSet.Contains("pack.mcmeta"),
                    GeneratedHasLoadTag: generatedSet.Contains("data/minecraft/tags/function/load.json"),
                    GeneratedHasTickTag: generatedSet.Contains("data/minecraft/tags/function/tick.json"),
                    ReferenceHasLoadTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/load.json"),
                    ReferenceHasTickTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/tick.json"),
                    CriticalFileCount: criticalFiles.Length,
                    PreservedCriticalFileCount: preservedCriticalFiles.Length,
                    PreservedCriticalFiles: preservedCriticalFiles,
                    ReferencePlaceholderSamples: referencePlaceholders.Take(12).ToArray(),
                    Summary: summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDatapackReferenceComparison {ex.ToString()} workspaceRoot {workspaceRoot?.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the read zip file entries operation.</summary>
        /// <param name="zipPath">Input value for zipPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string[] ReadZipFileEntries(string zipPath, ILogger logger)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                return archive.Entries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadZipFileEntries {ex.ToString()} zipPath {zipPath?.ToString()}");
                return new string[0];
            }

        }

        /// <summary>Executes the normalize reference datapack entry operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string NormalizeReferenceDatapackEntry(string entry, ILogger logger)
        {
            try
            {
                var normalized = entry.Replace('\\', '/').TrimStart('/');
                const string nestedPrefix = "living_cities/";
                return normalized.StartsWith(nestedPrefix, StringComparison.OrdinalIgnoreCase)
                    ? normalized[nestedPrefix.Length..]
                    : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeReferenceDatapackEntry {ex.ToString()} entry {entry?.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>Executes the is mc function path operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsMcFunctionPath(string entry, ILogger logger)
        {
            try
            {
                return entry.EndsWith(".mcfunction", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsMcFunctionPath {ex.ToString()} entry {entry?.ToString()}");
                return false;
            }
        }
        /// <summary>Executes the build artifact workspace summary operation.</summary>
        /// <param name="artifactRoot">Input value for artifactRoot.</param>
        /// <param name="workspacePath">Input value for workspacePath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ArtifactWorkspaceSummary? BuildArtifactWorkspaceSummary(string artifactRoot, string workspacePath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(workspacePath);
                var files = EnumerateWorkspaceTextFiles(workspacePath, 500,logger);
                var zipNames = Directory
                    .EnumerateFiles(artifactRoot, "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Where(name => name!.StartsWith(directory.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name!)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new ArtifactWorkspaceSummary(
                    directory.Name,
                    directory.FullName,
                    directory.LastWriteTimeUtc,
                    files.Count,
                    files.Count(file => file.RelativePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)),
                    files.Count(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)),
                    zipNames);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"BuildArtifactWorkspaceSummary artifactRoot {artifactRoot.ToString()} workspacePath {workspacePath.ToString()}");
                return null;
            }

        }
        /// <summary>Executes the enumerate workspace text files operation.</summary>
        /// <param name="workspaceRoot">Input value for workspaceRoot.</param>
        /// <param name="take">Input value for take.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public List<ArtifactWorkspaceFileSummary> EnumerateWorkspaceTextFiles(string workspaceRoot, int take, ILogger logger)
        {
            try
            {
                 if (!Directory.Exists(workspaceRoot))
                return [];

            return Directory
                .EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
                .Where(file => IsSupportedArtifactTextFile(file,logger))
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new ArtifactWorkspaceFileSummary(
                        text.ToForwardSlash(Path.GetRelativePath(workspaceRoot, path),logger),
                        info.Length,
                        info.LastWriteTimeUtc);
                })
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(take, 1, 1000))
                .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EnumerateWorkspaceTextFiles workspaceRoot {workspaceRoot.ToString()} take {take.ToString()}");
                return new();
            }
           
        }
        /// <summary>Executes the resolve artifact workspace operation.</summary>
        /// <param name="artifactRoot">Input value for artifactRoot.</param>
        /// <param name="workspaceName">Input value for workspaceName.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? ResolveArtifactWorkspace(string artifactRoot, string workspaceName, ILogger logger)
        {
            try
            {
                var safeName = Path.GetFileName(workspaceName);
                if (!string.Equals(workspaceName, safeName, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(safeName))
                {
                    return null;
                }

                var root = Path.GetFullPath(artifactRoot);
                var path = Path.GetFullPath(Path.Combine(root, safeName));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !Directory.Exists(path))
                {
                    return null;
                }

                return path;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ResolveWorkspaceTextFile artifactRoot {artifactRoot.ToString()} workspaceName {workspaceName.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the resolve workspace text file operation.</summary>
        /// <param name="workspaceRoot">Input value for workspaceRoot.</param>
        /// <param name="relativePath">Input value for relativePath.</param>
        /// <param name="allowMissing">Input value for allowMissing.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string? ResolveWorkspaceTextFile(string workspaceRoot, string relativePath, bool allowMissing, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalizedRelativePath))
                    return null;

                var root = Path.GetFullPath(workspaceRoot);
                var path = Path.GetFullPath(Path.Combine(root, normalizedRelativePath));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !IsSupportedArtifactTextFile(path, logger))
                {
                    return null;
                }

                return allowMissing || System.IO.File.Exists(path) ? path : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ResolveWorkspaceTextFile workspaceRoot {workspaceRoot.ToString()} relativePath {relativePath.ToString()} allowMissing {allowMissing.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the is supported artifact text file operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsSupportedArtifactTextFile(string path, ILogger logger)
        {
            try
            {
                var extension = Path.GetExtension(path);
                return catalog.ArtifactTextExtensions.Contains(extension);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"IsSupportedArtifactTextFile path {path.ToString()}");
                return false;
            }
        }
        /// <summary>Executes the read guidance docs async operation.</summary>
        /// <param name="env">Input value for env.</param>
        /// <param name="relativePaths">Input value for relativePaths.</param>
        /// <param name="fallbackBriefing">Input value for fallbackBriefing.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes with the operation result.</returns>
        public async Task<IResult> ReadGuidanceDocsAsync(
            IWebHostEnvironment env,
            IReadOnlyList<string> relativePaths,
            string fallbackBriefing,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var foundFiles = new List<object>();
                var briefing = new StringBuilder();

                foreach (var relativePath in relativePaths)
                {
                    var candidatePaths = new[]
                    {
                    Path.Combine(AppContext.BaseDirectory, relativePath),
                    Path.Combine(env.ContentRootPath, relativePath),
                    Path.Combine(Directory.GetCurrentDirectory(), relativePath)
                }.Distinct(StringComparer.OrdinalIgnoreCase);

                    var path = candidatePaths.FirstOrDefault(System.IO.File.Exists);
                    if (path is null)
                        continue;

                    var text = await System.IO.File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                    foundFiles.Add(new
                    {
                        RelativePath = relativePath.Replace('\\', '/'),
                        SourcePath = path
                    });

                    briefing
                        .Append("# ")
                        .AppendLine(Path.GetFileName(relativePath))
                        .AppendLine()
                        .AppendLine(text.Trim())
                        .AppendLine();
                }

                return Results.Ok(new
                {
                    GuidanceFiles = foundFiles,
                    Briefing = foundFiles.Count > 0
                        ? briefing.ToString().Trim()
                        : fallbackBriefing.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reading the approved guidance documents failed; guidance content and local paths were omitted from logs.");
                return Results.InternalServerError("The guidance documents could not be read. See local application logs for technical details.");
            }
        }
        /// <summary>Executes the limit prompt size operation.</summary>
        /// <param name="messages">Input value for messages.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="forcedMaxPromptCharacters">Input value for forcedMaxPromptCharacters.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<ChatMessage> LimitPromptSize(IReadOnlyList<ChatMessage> messages, ILogger logger, int? forcedMaxPromptCharacters = null)
        {
            try
            {
                var maxPromptCharacters = Math.Clamp(forcedMaxPromptCharacters ?? catalog.DefaultMaxPromptCharacters, 512, catalog.MaxPromptCharacters);
                if (messages.Sum(EstimateTextLength) <= maxPromptCharacters)
                    return messages;

                var result = new List<ChatMessage>();
                var usedCharacters = 0;
                var remainingSystemBudget = Math.Min(catalog.MaxBootstrapCharacters, Math.Max(maxPromptCharacters / 2, 0));

                foreach (var message in messages.Where(message => message.Role == ChatRole.System))
                {
                    var textInner = message.Text ?? string.Empty;
                    var budget = Math.Min(remainingSystemBudget, maxPromptCharacters - usedCharacters);
                    if (budget <= 0)
                        break;

                    var trimmed = text.TrimForPrompt(textInner, budget,logger, keepBothEnds: false);
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    result.Add(new ChatMessage(message.Role, trimmed));
                    usedCharacters += trimmed.Length;
                    remainingSystemBudget -= trimmed.Length;
                }

                var conversationMessages = messages
                    .Where(message => message.Role != ChatRole.System)
                    .ToList();
                var keptConversationMessages = new Stack<ChatMessage>();

                for (var index = conversationMessages.Count - 1; index >= 0; index--)
                {
                    var remainingBudget = maxPromptCharacters - usedCharacters;
                    if (remainingBudget <= 0)
                        break;

                    var message = conversationMessages[index];
                    var textInner = message.Text ?? string.Empty;
                    var messageBudget = Math.Min(catalog.MaxSingleConversationMessageCharacters, remainingBudget);
                    var trimmed =   text.TrimForPrompt(textInner, messageBudget, logger, keepBothEnds: true);
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    keptConversationMessages.Push(new ChatMessage(message.Role, trimmed));
                    usedCharacters += trimmed.Length;
                }

                result.AddRange(keptConversationMessages);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not limit prompt size to {ForcedMaxPromptCharacters} characters.",
                    forcedMaxPromptCharacters);
                return new List<ChatMessage>();
            }
        }

        /// <summary>Executes the estimate text length operation.</summary>
        /// <param name="message">Input value for message.</param>
        /// <returns>The operation result.</returns>
        public int EstimateTextLength(ChatMessage message) {
    try
    {
        return message.Text?.Length ?? 0;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(EstimateTextLength)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(EstimateTextLength)} failed.");
        throw;
    }
}

        /// <summary>Executes the try is supported ollama mode operation.</summary>
        /// <param name="mode">Input value for mode.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? TryIsSupportedOllamaMode(string mode,ILogger logger)
        {
            try
            {
                return mode.Equals(catalog.OllamaModeAutoGpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(catalog.OllamaModeSafeCpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(catalog.OllamaModeLimitedGpu, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsSupportedOllamaMode mode:{mode}");
                return null;
            }

        }
       
        /// <summary>Executes the is blazor frontend target operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="targetArea">Input value for targetArea.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsBlazorFrontendTarget(string prompt, string finalAnswer, string targetArea, ILogger logger)
        {
            try
            {
                return targetArea.Contains("Blazor/DevExpress frontend", StringComparison.OrdinalIgnoreCase) ||
               catalog.BlazorFrontendPattern.IsMatch($"{prompt} {finalAnswer}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the target area is Blazor/DevExpress: {TargetArea}.", targetArea);
                return null;
            }
           
        }

        /// <summary>Executes the is whole solution target operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsWholeSolutionTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var isAiHostExperimentTarget = IsAiHostExperimentTarget(prompt, finalAnswer, logger);
                bool isAiHostExperimentTargetBool = isAiHostExperimentTarget ?? false;
                return catalog.WholeSolutionPattern.IsMatch(prompt) || isAiHostExperimentTargetBool;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the response targets a whole solution.");
                return null;
            }
        }

        /// <summary>Executes the is ai host experiment target operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsAiHostExperimentTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {

                return catalog.AiHostExperimentPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the response targets an AI-host experiment.");
                return null;
            }
        }

        /// <summary>Executes the is advice only prompt operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsAdviceOnlyPrompt(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.AdviceOnlyPromptPattern.IsMatch(prompt) &&
                    !catalog.ExplicitArtifactCreationCommandPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the request is advice-only.");
                return null;
            }
        }

        /// <summary>Executes the detect solution archetype operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public GeneratedSolutionArchetype? DetectSolutionArchetype(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                if (catalog.AiHostExperimentPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.AiHost;
                if (catalog.LocalGptReplacementPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.LocalGpt;
                if (catalog.TacosPortalPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.TacosPortal;
                if (catalog.BotBackendPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.BotBackend;

                return GeneratedSolutionArchetype.Generic;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect the requested solution archetype.");
                return null;
            }
        }


        /// <summary>Executes the validate generated datapack workspace operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateGeneratedDatapackWorkspace(string rootPath, ILogger logger)
        {
            try
            {
                var packPath = Path.Combine(rootPath, "pack.mcmeta");
                var dataPath = Path.Combine(rootPath, "data");
                if (!File.Exists(packPath))
                    throw new InvalidOperationException("Generated datapack is missing root pack.mcmeta.");
                if (!Directory.Exists(dataPath))
                    throw new InvalidOperationException("Generated datapack is missing root data folder.");

                JsonDocument.Parse(File.ReadAllText(packPath));
                foreach (var tagPath in Directory.GetFiles(Path.Combine(dataPath, "minecraft", "tags", "function"), "*.json"))
                    JsonDocument.Parse(File.ReadAllText(tagPath));

                var nestedPack = Directory
                    .EnumerateDirectories(rootPath)
                    .Select(directory => Path.Combine(directory, "pack.mcmeta"))
                    .FirstOrDefault(File.Exists);
                if (nestedPack is not null)
                    throw new InvalidOperationException("Generated datapack has a nested wrapper folder containing pack.mcmeta.");

                var pluralFunctionsFolder = Directory
                    .EnumerateDirectories(dataPath, "functions", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (pluralFunctionsFolder is not null)
                    throw new InvalidOperationException("Generated datapack contains legacy plural functions folder; Minecraft 1.21+ uses function.");

                var txtPlaceholder = Directory
                    .EnumerateFiles(dataPath, "*.mcfunction.txt", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (txtPlaceholder is not null)
                    throw new InvalidOperationException("Generated datapack contains .mcfunction.txt placeholder files.");

                foreach (var functionFile in Directory.EnumerateFiles(dataPath, "*.mcfunction", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(functionFile);
                    if (catalog.LeadingSlashCommandPattern.IsMatch(content))
                        throw new InvalidOperationException($"Generated function contains a leading slash command: {Path.GetRelativePath(rootPath, functionFile)}");
                    if (catalog.RootStorageRemovePattern.IsMatch(content))
                        throw new InvalidOperationException($"Generated function uses data remove storage root syntax: {Path.GetRelativePath(rootPath, functionFile)}");
                    if (catalog.MalformedStorageTargetPattern.IsMatch(content))
                        throw new InvalidOperationException($"Generated function appears to put an NBT path into the storage id instead of after it: {Path.GetRelativePath(rootPath, functionFile)}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGeneratedDatapackWorkspace rootPath:{rootPath}");
             
            }
           
        }

        /// <summary>Executes the add directory to zip operation.</summary>
        /// <param name="archive">Input value for archive.</param>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="directoryPath">Input value for directoryPath.</param>
        /// <param name="logger">Input value for logger.</param>
        public void AddDirectoryToZip(ZipArchive archive, string rootPath, string directoryPath, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return;

                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    var entryName = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
                    AddFileToZip(archive, filePath, entryName, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddDirectoryToZip archive:{archive.ToString()} rootPath:{rootPath} directoryPath:{directoryPath}", archive);

            }
        }

        /// <summary>Executes the add file to zip operation.</summary>
        /// <param name="archive">Input value for archive.</param>
        /// <param name="filePath">Input value for filePath.</param>
        /// <param name="entryName">Input value for entryName.</param>
        /// <param name="logger">Input value for logger.</param>
        public void AddFileToZip(ZipArchive archive, string filePath, string entryName, ILogger logger)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                archive.CreateEntryFromFile(filePath, entryName.Replace('\\', '/'), CompressionLevel.SmallestSize);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddFileToZip archive:{archive.ToString()} filePath:{filePath} entryName:{entryName}", archive);
            }
        }

        /// <summary>Executes the copy directory operation.</summary>
        /// <param name="sourceDirectory">Input value for sourceDirectory.</param>
        /// <param name="destinationDirectory">Input value for destinationDirectory.</param>
        /// <param name="logger">Input value for logger.</param>
        public void CopyDirectory(string sourceDirectory, string destinationDirectory, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(destinationDirectory);
                foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
                    Directory.CreateDirectory(Path.Combine(destinationDirectory, relativeDirectory));
                }

                foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativeFile = Path.GetRelativePath(sourceDirectory, file);
                    var destinationFile = Path.Combine(destinationDirectory, relativeFile);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                    File.Copy(file, destinationFile, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyDirectory sourceDirectory:{sourceDirectory} destinationDirectory:{destinationDirectory}");
            }
           
        }

    

        /// <summary>Executes the archetype page operation.</summary>
        /// <param name="fileName">Input value for fileName.</param>
        /// <param name="route">Input value for route.</param>
        /// <param name="title">Input value for title.</param>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="areas">Input value for areas.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public GeneratedArchetypePage ArchetypePage(
            string fileName,
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas, ILogger logger)
        {
            try
            {
                return new GeneratedArchetypePage(
             fileName,
             text.GenerateArchetypePageRazor(route, title, summary, areas, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not generate archetype page {FileName} for route {Route}.", fileName, route);
                return new GeneratedArchetypePage(
                    string.IsNullOrWhiteSpace(fileName) ? "GeneratedPage.razor" : fileName,
                    $"@page \"{route}\"{Environment.NewLine}<PageTitle>{title}</PageTitle>{Environment.NewLine}<h1>{title}</h1>{Environment.NewLine}<p>Page generation failed. Review LocalGPT logs and regenerate this page.</p>");
            }
         
        }
        /// <summary>Executes the write text async operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="content">Input value for content.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public Task WriteTextAsync(string path, string content, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path has no directory: {path}"));
                return File.WriteAllTextAsync(path, content, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteTextAsync");
                return Task.CompletedTask;
            }
        }

        /// <summary>Executes the generate archetype pages operation.</summary>
        /// <param name="archetype">Input value for archetype.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<GeneratedArchetypePage> GenerateArchetypePages(GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                return archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt =>
                    [
                        ArchetypePage("Chat.razor", "/chat", "DXAiChat", "Chat surface with model routing, uploads, artifact links, visible progress, and memory-aware continuation.", ["Model selection", "Council mode", "File context", "Artifact downloads"],logger),
                    ArchetypePage("ModelCouncil.razor", "/model-council", "AI Council", "Multi-model review surface for feedback talks, polls, missing features, source requests, and implementation artifacts.", ["Minimum two members", "Sequential scheduling", "Poll gate", "Feedback log"],logger),
                    ArchetypePage("Database.razor", "/database", "SQLite Database", "Editable operational memory for chats, thoughts, logs, knowledge, benchmark scores, and approval markers.", ["CouncilKnowledgeEntries", "ChatMessages", "ApplicationLogs", "BenchmarkResults"],logger),
                    ArchetypePage("MinecraftModBuilder.razor", "/minecraft-mod-builder", "Minecraft Mod Builder", "Workspace generator for datapacks, Fabric, Paper, NeoForge, Java/Gradle setup, validation, and downloads.", ["Datapack zip", "Loader matrix", "Version resolver", "Validation script"],logger),
                    ArchetypePage("TestLab.razor", "/test-lab", "Test Lab", "Frontend-accessible diagnostics for API smoke checks, benchmark routes, artifact downloads, and WebView2 workflows.", ["Health", "DXAiFunctions", "Replacement benchmark", "Council feedback"],logger),
                    ArchetypePage("Install.razor", "/install", "Install", "Model host discovery, Ollama/LM Studio status, model pull planning, runtime checks, and setup guidance.", ["Ollama status", "LM Studio status", "Model downloads", "Java/.NET checks"],logger)
                    ],
                    GeneratedSolutionArchetype.TacosPortal =>
                    [
                        ArchetypePage("TelegramIngestion.razor", "/telegram-ingestion", "Telegram Ingestion", "Event-ingestion boundary with update handling, command routing, idempotency, retries, and sanitized bot service wiring.", ["Update handler", "Command router", "Idempotency", "Retry queue"],logger),
                    ArchetypePage("Persistence.razor", "/persistence", "Persistence", "Normalized domain persistence with EF/SQLite or provider-specific backend, explicit DTO/service boundaries, and migration notes.", ["Business objects", "DbContext", "DTO boundaries", "Migration safety"],logger),
                    ArchetypePage("Workers.razor", "/workers", "Workers", "Hosted/background worker view for polling, notification dispatch, API synchronization, and operational diagnostics.", ["Hosted services", "Polling", "Notifications", "Diagnostics"],logger),
                    ArchetypePage("Admin.razor", "/admin", "Admin", "DevExpress CRUD/admin workbench with roles, audit log, validation, custom security, and operational settings.", ["Users", "Roles", "Audit", "Settings"],logger),
                    ArchetypePage("ClientShells.razor", "/client-shells", "Client Shells", "Host map for Blazor server, optional WASM client, WinUI/WebView2 wrapper, package boundaries, and debug/deploy notes.", ["Server host", "WASM client", "WinUI/WebView2", "Package diagnostics"],logger)
                    ],
                    GeneratedSolutionArchetype.BotBackend =>
                    [
                        ArchetypePage("Webhooks.razor", "/webhooks", "Webhooks", "Inbound message and event receiver surface with validation, idempotency, and retry diagnostics.", ["Ingress", "Signature check", "Idempotency", "Dead letters"],logger),
                    ArchetypePage("Conversations.razor", "/conversations", "Conversations", "Conversation-state workbench with memory, moderation, handoff, and compact transcript review.", ["Memory", "Moderation", "Handoff", "Transcript"],logger),
                    ArchetypePage("BotSettings.razor", "/bot-settings", "Bot Settings", "Provider-neutral bot configuration with secrets stored outside the generated code and visible safety gates.", ["Provider", "Token source", "Allowed commands", "Rate limit"],logger),
                    ArchetypePage("PythonInterop.razor", "/python-interop", "Python Interop", "Optional Python.NET or process-adapter boundary for transcription, translation, media, or model tooling.", ["Python.NET", "Process adapter", "Safe directory", "User approval"],logger)
                    ],
                    _ => []
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePages archetype:{archetype.ToString()}");
                return new List<GeneratedArchetypePage>();
            }
           
        }
        /// <summary>Executes the extract dynamic promise modules operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<GeneratedPromiseModule> ExtractDynamicPromiseModules(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var textInner = $"{request.Prompt} {result.FinalAnswer}";
                var modules = new List<GeneratedPromiseModule>();

                void AddIf(bool condition, string title, string summary, IReadOnlyList<string> areas)
                {
                    if (!condition || modules.Any(module => module.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
                        return;

                    var route = "/" + text.ToKebabRoute(title,logger);
                    var fileName = $"{text.ToPascalIdentifier(title, logger)}.razor";
                    modules.Add(new GeneratedPromiseModule(fileName, route, title, summary, areas));
                }

                AddIf(
                    catalog.DevExpressDocumentPattern.IsMatch(textInner) || catalog.ExportFormatPattern.IsMatch(textInner),
                    "Document Exports",
                    "Promise-derived surface for report, Office, PDF, spreadsheet, presentation, and document export work owned by backend services.",
                    ["Report template", "Format mapping", "Backend service", "Download route"]);
                AddIf(
                    textInner.Contains("FileDownloadController", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("download link", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("download route", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("safe download", StringComparison.OrdinalIgnoreCase),
                    "Download Center",
                    "Promise-derived surface for generated files, MIME types, safe HTTP GET links, checksums, expiry, and user-visible artifact status.",
                    ["Generated files", "HTTP GET", "Checksum", "Expiry"]);
                AddIf(
                    textInner.Contains("DxAiFunctions", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("IAIInferenceProvider", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("/api/inference", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("AI prompt", StringComparison.OrdinalIgnoreCase),
                    "AI Prompt Flow",
                    "Promise-derived surface for prompt-to-plan workflows, model/provider calls, generated briefs, and Needs verification notes.",
                    ["Prompt", "Provider call", "Generated brief", "Verification"]);
                AddIf(
                    textInner.Contains("IModelCatalogService", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("model catalog", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("LM Studio", StringComparison.OrdinalIgnoreCase),
                    "Model Host Status",
                    "Promise-derived surface for local model/provider inventory, host reachability, selected model, and runtime status.",
                    ["Provider", "Model catalog", "Reachability", "Runtime status"]);
                AddIf(
                    textInner.Contains("SQLite", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("EF/", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("persist", StringComparison.OrdinalIgnoreCase),
                    "Persistence",
                    "Promise-derived surface for database state, DTO projection, migration safety, audit records, and user-approved knowledge.",
                    ["EF/SQLite", "DTOs", "Migration safety", "Audit"]);
                AddIf(
                    textInner.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("DxGrid", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("DxFormLayout", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("Blazor", StringComparison.OrdinalIgnoreCase),
                    "DevExpress UI",
                    "Promise-derived surface for DevExpress Blazor controls, layout, navigation, forms, grids, and frontend verification.",
                    ["Navigation", "Grid", "Form", "Frontend smoke"]);
                AddIf(
                    textInner.Contains("API endpoint", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("controller", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("/api/", StringComparison.OrdinalIgnoreCase),
                    "API Contracts",
                    "Promise-derived surface for backend routes, request/response DTOs, validation, errors, and smoke-test calls.",
                    ["Routes", "DTOs", "Validation", "Smoke tests"]);

                return modules.Take(8).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ExtractDynamicPromiseModules");
                return new List<GeneratedPromiseModule>();
            }
           
        }

        /// <summary>Executes the sort knowledge entries operation.</summary>
        /// <param name="entries">Input value for entries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public List<CouncilKnowledgeEntry> SortKnowledgeEntries(IEnumerable<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                return entries
             .OrderBy(filter => KnowledgeReviewPriority(filter, logger))
             .ThenByDescending(entry => entry.IsPinned)
             .ThenByDescending(entry => entry.IsUserApproved)
             .ThenByDescending(entry => entry.UpdatedAtUtc)
             .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SortKnowledgeEntries entries:{entries.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }

        }
        /// <summary>Executes the copy knowledge entry operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public CouncilKnowledgeEntry? CopyKnowledgeEntry(CouncilKnowledgeEntry entry,ILogger logger)
        {
            try
            {
                return new CouncilKnowledgeEntry
                {
                    Id = entry.Id,
                    CreatedAtUtc = entry.CreatedAtUtc,
                    UpdatedAtUtc = entry.UpdatedAtUtc,
                    Topic = entry.Topic,
                    Scope = entry.Scope,
                    Content = entry.Content,
                    Source = entry.Source,
                    HelpfulSources = entry.HelpfulSources,
                    Tags = entry.Tags,
                    Confidence = entry.Confidence,
                    VerificationStatus = entry.VerificationStatus,
                    ReviewStatus = entry.ReviewStatus,
                    ExpiresAtUtc = entry.ExpiresAtUtc,
                    LastVerifiedAtUtc = entry.LastVerifiedAtUtc,
                    LastUsedAtUtc = entry.LastUsedAtUtc,
                    SupersededByKnowledgeId = entry.SupersededByKnowledgeId,
                    StalenessReason = entry.StalenessReason,
                    StalenessDetectedAtUtc = entry.StalenessDetectedAtUtc,
                    StalenessDetectedBy = entry.StalenessDetectedBy,
                    SourceHash = entry.SourceHash,
                    SourceDateUtc = entry.SourceDateUtc,
                    IsUserApproved = entry.IsUserApproved,
                    IsPinned = entry.IsPinned,
                    IsArchived = entry.IsArchived
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyKnowledgeEntry entry:{entry.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the build knowledge review summary operation.</summary>
        /// <param name="entries">Input value for entries.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildKnowledgeReviewSummary(IReadOnlyCollection<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                if (entries.Count == 0)
                    return "No knowledge notes loaded yet.";

                var needsAttention = entries.Count(entry => entry.ReviewStatus is "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired");
                var trusted = entries.Count(entry => entry.ReviewStatus == "Current" && entry.IsUserApproved);
                return $"{needsAttention} note(s) need attention. {trusted} user-approved current note(s) can guide the council.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildKnowledgeReviewSummary entries:{entries.ToString()}");
                return string.Empty;
            }
            
        }

        /// <summary>Executes the get council model load priority randomisator operation.</summary>
        /// <param name="maxPriority">Input value for maxPriority.</param>
        /// <param name="modelName">Input value for modelName.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int? GetCouncilModelLoadPriorityRandomisator(int maxPriority, string modelName, ILogger logger)
        {
            try
            {
                var random = new Random();
                int randomNumber = random.Next(maxPriority);
                logger.LogInformation($"GetCouncilModelLoadPriorityRandomisator modelName:{modelName.ToString()} returning value..{randomNumber}");
                return randomNumber;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetCouncilModelLoadPriorityRandomisator modelName:{modelName.ToString()} maxPriority:{maxPriority}");
                return null;
            }
            
        }

        /// <summary>Executes the knowledge review priority operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int? KnowledgeReviewPriority(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                return entry.ReviewStatus switch
                {
                    "NeedsUserReview" => 0,
                    "NeedsSourceRefresh" => 1,
                    "NeedsDiagnosticVerification" => 2,
                    "Expired" => 3,
                    "Superseded" => 4,
                    "Deprecated" => 5,
                    "Current" => 6,
                    "Archived" => 7,
                    _ => 8
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in KnowledgeReviewPriority entry:{entry.ToString()}");
                return null;
            }
           
        }

        /// <summary>Executes the is dynamic session operation.</summary>
        /// <param name="session">Input value for session.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsDynamicSession(ChatClientSession session, ILogger logger)
        {
            try
            {
                return session.Name.StartsWith(catalog.DetectedOllamaSessionPrefix, StringComparison.OrdinalIgnoreCase) ||
            session.Name.Equals(catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsDynamicSession sessionm:{session.ToString()}");
                return null;
            }

        }
       

        /// <summary>Executes the order council models for load operation.</summary>
        /// <param name="modelNames">Input value for modelNames.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The resulting sequence.</returns>
        public IEnumerable<string>? OrderCouncilModelsForLoad(IEnumerable<string> modelNames, ILogger logger)
        {
            try
            {
                return modelNames
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(filter =>  GetCouncilModelLoadPriorityRandomisator(modelNames.Count(), filter,logger) ?? 0)
               .ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in OrderCouncilModelsForLoad modelNames:{modelNames.ToString()}");
                return null;
            }

        }
        /// <summary>Executes the build dynamic session name operation.</summary>
        /// <param name="candidate">Input value for candidate.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildDynamicSessionName(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                return $"{catalog.DetectedOllamaSessionPrefix}{candidate.SelectionKey}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDynamicSessionName candidate:{candidate.ToString()}");
                return string.Empty;
            }

        }
  

        /// <summary>Executes the build candidate label operation.</summary>
        /// <param name="candidate">Input value for candidate.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildCandidateLabel(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                return $"{candidate.ModelName} @ {text.TrimEndpoint(candidate.Endpoint,logger)}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCandidateLabel candidate:{candidate.ToString()}");
                return string.Empty;
            }

        }
           

        /// <summary>Executes the build candidate title operation.</summary>
        /// <param name="candidate">Input value for candidate.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string BuildCandidateTitle(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                var details = string.IsNullOrWhiteSpace(candidate.Details)
          ? "No model details reported."
          : candidate.Details;
                return $"{candidate.Provider} at {candidate.Endpoint}. {details}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCandidateTitle candidate:{candidate.ToString()}");
                return string.Empty;
            }

      
        }


        /// <summary>Executes the validate solution artifact contract operation.</summary>
        /// <param name="solutionRoot">Input value for solutionRoot.</param>
        /// <param name="projectName">Input value for projectName.</param>
        /// <param name="archetype">Input value for archetype.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public ArtifactContractReport? ValidateSolutionArtifactContract(
            string solutionRoot,
            string projectName,
            GeneratedSolutionArchetype archetype,ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var requiredFiles = new List<string>
            {
                $"{projectName}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
                "SOURCE_FIDELITY.md",
                "BUILD_AND_RUN.md",
                ".localgpt-generation.json",
                "LocalGPT.GenerationManifest.json",
                Path.Combine("src", projectName, $"{projectName}.csproj"),
                Path.Combine("src", projectName, "Program.cs"),
                Path.Combine("src", projectName, "Components", "GeneratedNavigation.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "Index.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedDashboard.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedKnowledgeTable.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "SourceFidelity.razor"),
                Path.Combine("src", projectName, "Components", "Pages", isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                Path.Combine("src", projectName, "Services", "GeneratedHealthSummaryService.cs"),
                Path.Combine("src", projectName, "Services", "GeneratedSourceFidelityService.cs"),
                Path.Combine("src", projectName, "Models", "GeneratedHealthCard.cs"),
                Path.Combine("src", projectName, "wwwroot", "app.css"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-solid.svg")
            };

                if (isAiHostLab)
                {
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Chat.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "RunningModels.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "ModelDownloads.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Templates.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Hardware.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "RunnerPlugins.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Logs.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Settings.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Services", "GeneratedAiHostArchitectureServices.cs"));
                }

                var missing = requiredFiles
                    .Where(relativePath => !File.Exists(Path.Combine(solutionRoot, relativePath)))
                    .ToArray();

                if (missing.Length > 0)
                    throw new InvalidOperationException($"Generated solution artifact is missing required files: {string.Join(", ", missing)}");

                ValidateGenerationContractJson(Path.Combine(solutionRoot, ".localgpt-generation.json"), logger);
                ValidateGenerationManifestJson(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), logger);

                if (isAiHostLab)
                {
                    ValidateAiHostArtifactContract(solutionRoot, projectName, logger);
                    return new ArtifactContractReport(
                        "Source-contract prototype",
                        "AI-host source contract validated",
                        [
                            "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable",
                        "AI-host endpoint and native-runner source markers exist"
                        ],
                        ["No model-file runtime execution proof was produced", "No generated-project build proof was produced"],
                        "AI-host routes, settings, navigation, and native-runner source markers were checked before zipping; runtime behavior is still unproven.");
                }

                if (archetype == GeneratedSolutionArchetype.LocalGpt)
                {
                    return new ArtifactContractReport(
                        "Static LocalGPT-style prototype",
                        "Missing LocalGPT runtime contract",
                        [
                            "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable"
                        ],
                        [
                            "DXAiChat runtime wiring is not proven",
                        "AI Council execution is not proven",
                        "SQLite memory persistence is not proven",
                        "Artifact route behavior is not proven"
                        ],
                        "LocalGPT-like source files were generated, but the artifact must not be treated as a working LocalGPT replacement.");
                }

                return new ArtifactContractReport(
                    "Generated solution prototype",
                    "Generated files validated",
                    [
                        "Required generated file set exists",
                    "Generation contract JSON is parseable",
                    "Generation manifest JSON is parseable"
                    ],
                    ["No generated-project build proof was produced", "No runtime UI proof was produced"],
                    "Required files and metadata were checked before zipping; build and runtime behavior are unproven.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateSolutionArtifactContract solutionRoot:{solutionRoot.ToString()} projectName:{projectName.ToString()} archetype:{archetype.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the validate ai host artifact contract operation.</summary>
        /// <param name="solutionRoot">Input value for solutionRoot.</param>
        /// <param name="projectName">Input value for projectName.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateAiHostArtifactContract(string solutionRoot, string projectName, ILogger logger)
        {
            try
            {
                var projectRoot = Path.Combine(solutionRoot, "src", projectName);
                var programPath = Path.Combine(projectRoot, "Program.cs");
                var architectureServicePath = Path.Combine(projectRoot, "Services", "GeneratedAiHostArchitectureServices.cs");
                var appSettingsPath = Path.Combine(projectRoot, "appsettings.json");
                var navigationPath = Path.Combine(projectRoot, "Components", "GeneratedNavigation.razor");

                var program = File.ReadAllText(programPath);
                var architectureService = File.ReadAllText(architectureServicePath);
                var appSettings = File.ReadAllText(appSettingsPath);
                var navigation = File.ReadAllText(navigationPath);

                var requiredRoutes = new[]
                {
                "/api/version",
                "/api/tags",
                "/api/ps",
                "/api/generate",
                "/api/chat"
            };

                foreach (var route in requiredRoutes)
                {
                    if (!program.Contains(route, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host artifact Program.cs is missing required route {route}.");
                }

                var requiredProgramTokens = new[]
                {
                "IInferenceProvider",
                "NativeModelFileInferenceProvider",
                "IInferenceRunner",
                "NativeModelFileProcessRunner",
                "upstream_proxy = false"
            };

                foreach (var token in requiredProgramTokens)
                {
                    if (!program.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host artifact Program.cs is missing required implementation token {token}.");
                }

                var requiredServiceTokens = new[]
                {
                "AiHostRuntimeOptions",
                "NativeModelFileProcessRunner",
                "NativeRunnerExecutable",
                "No upstream proxy fallback is used",
                "ProcessStartInfo"
            };

                foreach (var token in requiredServiceTokens)
                {
                    if (!architectureService.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host architecture service is missing required implementation token {token}.");
                }

                var requiredSettingTokens = new[]
                {
                "\"DefaultModel\"",
                "\"NativeRunnerExecutable\"",
                "\"ModelSearchRoots\"",
                "\"ContextTokens\"",
                "\"GpuLayers\""
            };

                foreach (var token in requiredSettingTokens)
                {
                    if (!appSettings.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host appsettings.json is missing required setting {token}.");
                }

                var requiredNavigationRoutes = new[]
                {
                "/chat",
                "/models",
                "/api-console",
                "/downloads",
                "/settings"
            };

                foreach (var route in requiredNavigationRoutes)
                {
                    if (!navigation.Contains(route, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host navigation is missing required route {route}.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateAiHostArtifactContract solutionRoot:{solutionRoot.ToString()} projectName:{projectName.ToString()}");
             
            }
        }

        /// <summary>Executes the validate generation contract json operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateGenerationContractJson(string path, ILogger logger)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                var requiredProperties = new[]
                {
                "schema",
                "project_kind",
                "target_platform",
                "complexity",
                "needs_datagen",
                "needs_tests",
                "needs_native_commands",
                "needs_index",
                "needs_version_resolver",
                "expected_entrypoints",
                "generated_files",
                "validation_status",
                "build_test_result_provenance"
            };

                foreach (var property in requiredProperties)
                    RequireJsonProperty(root, property, path, logger);

                RequireNonEmptyJsonArray(root, "expected_entrypoints", path, logger);
                RequireNonEmptyJsonArray(root, "generated_files", path, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGenerationContractJson path:{path.ToString()}");

            }
           
        }

        /// <summary>Executes the validate generation manifest json operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateGenerationManifestJson(string path, ILogger logger)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                RequireJsonProperty(root, "artifactKind", path, logger);
                RequireJsonProperty(root, "sourceGoal", path, logger);
                RequireJsonProperty(root, "validationStatus", path, logger);
                RequireJsonProperty(root, "buildTestResultProvenance", path, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGenerationManifestJson path:{path.ToString()}");

            }
       
        }

        /// <summary>Executes the require json property operation.</summary>
        /// <param name="root">Input value for root.</param>
        /// <param name="propertyName">Input value for propertyName.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void RequireJsonProperty(JsonElement root, string propertyName, string path, ILogger logger)
        {
            try
            {
                if (!root.TryGetProperty(propertyName, out _))
                    throw new InvalidOperationException($"Generated contract {Path.GetFileName(path)} is missing {propertyName}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequireJsonProperty root:{root.ToString()} propertyName:{propertyName.ToString()} path:{path.ToString()}");

            }
            
        }

        /// <summary>Executes the require non empty json array operation.</summary>
        /// <param name="root">Input value for root.</param>
        /// <param name="propertyName">Input value for propertyName.</param>
        /// <param name="path">Input value for path.</param>
        /// <param name="logger">Input value for logger.</param>
        public void RequireNonEmptyJsonArray(JsonElement root, string propertyName, string path, ILogger logger)
        {
            try
            {
                RequireJsonProperty(root, propertyName, path, logger);
                var property = root.GetProperty(propertyName);
                if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException(
                        $"Generated contract {Path.GetFileName(path)} must include a non-empty {propertyName} array.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequireNonEmptyJsonArray root:{root.ToString()} propertyName:{propertyName.ToString()} path:{path.ToString()}");

            }
        }

    }
}
