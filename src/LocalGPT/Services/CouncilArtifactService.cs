using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.IO.Compression;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council artifact behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="minecraftWorkspaceService">Minecraft mod workspace service dependency used by the council artifact workflow to provide the corresponding application capability.</param>
    /// <param name="artifactBuildExecutor">Artifact build executor dependency used by the council artifact workflow to provide the corresponding application capability.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the council artifact workflow to provide the corresponding application capability.</param>
    /// <param name="councilText">Council text service dependency used by the council artifact workflow to provide the corresponding application capability.</param>
    /// <param name="catalog">Local gpt catalog service dependency used by the council artifact workflow to provide the corresponding application capability.</param>
    public partial class CouncilArtifactService(
        ILogger<CouncilArtifactService> logger,
        IMinecraftModWorkspaceService minecraftWorkspaceService,
        IArtifactBuildExecutor artifactBuildExecutor,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText,
        LocalGptCatalogService catalog) : ICouncilArtifactService
    {
        /// <summary>
        /// Gets the artifact root value that forms part of the council artifact state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The artifact root value exposed by <see cref="CouncilArtifactService"/>.</value>
        public string ArtifactRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "CouncilArtifacts");

        /// <summary>
        /// Creates implementation artifacts as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        public async Task<IReadOnlyList<CouncilArtifact>> CreateImplementationArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!request.GenerateImplementationArtifact)
                    return [];

                if (!request.UserConfirmedArtifactBuild)
                    throw new InvalidOperationException("Fresh human confirmation is required before generating implementation artifacts.");

                if (councilRuntime.IsAdviceOnlyPrompt(request.Prompt, logger) ?? false)
                {
                    logger.LogInformation("Skipped council artifact generation for advice-only prompt.");
                    return [];
                }

                Directory.CreateDirectory(ArtifactRoot);

                var targetArea = councilText.DetectTargetArea(request.Prompt, result.FinalAnswer, logger);
                var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
                var artifacts = new List<CouncilArtifact>();

                if (councilText.IsMinecraftSkeletonMatrixArtifactTarget(request.Prompt, result.FinalAnswer, logger) ?? false)
                {
                    artifacts.AddRange(await CreateMinecraftSkeletonMatrixArtifactsAsync(request, result, timestamp, cancellationToken).ConfigureAwait(false));
                    return artifacts;
                }

                if (councilText.IsMinecraftDatapackArtifactTarget(request.Prompt, result.FinalAnswer, logger) ?? false)
                {
                    artifacts.AddRange(await CreateMinecraftDatapackArtifactsAsync(request, result, timestamp, cancellationToken).ConfigureAwait(false));
                    return artifacts;
                }

                if (councilRuntime.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea, logger) ?? false)
                {
                    var razorFileName = $"council-feature-page-{timestamp}-{result.RunId:N}.razor";
                    var razorPath = Path.Combine(ArtifactRoot, razorFileName);
                    var razorSource = councilText.GenerateBlazorDevExpressRazorExample(request, result, logger);
                    await File.WriteAllTextAsync(razorPath, razorSource, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("Wrote council Blazor Razor artifact to {Path}", razorPath);
                    artifacts.Add(new CouncilArtifact
                    {
                        Name = razorFileName,
                        Kind = "Blazor/DevExpress Razor component",
                        FilePath = razorPath,
                        DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(razorFileName)}",
                        Summary = "Generated server-interactive Razor page using DevExpress controls and LocalGPT/TacosPortal-style patterns.",
                        QualityStatus = "Generated source only",
                        ContractStatus = "Razor file written",
                        ContractChecks = ["File exists after write"],
                        MissingRequirements = ["No Razor compile or runtime render proof was produced"]
                    });

                    targetArea = "Blazor/DevExpress frontend";
                }

                var fileName = $"council-feature-example-{timestamp}-{result.RunId:N}.cs";
                var path = Path.Combine(ArtifactRoot, fileName);
                var source = councilRuntime.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea, logger) ?? false
                    ? councilText.GenerateBlazorSupportCode(request, result, targetArea, logger)
                    : councilText.GenerateCodeDomExample(request, result, targetArea, logger);

                await File.WriteAllTextAsync(path, source, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Wrote council implementation example artifact to {Path}", path);

                artifacts.Add(new CouncilArtifact
                {
                    Name = fileName,
                    Kind = councilRuntime.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea, logger) ?? false
                        ? "Compileable .NET support code for the Razor artifact"
                        : "CodeDOM C# example",
                    FilePath = path,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(fileName)}",
                    Summary = $"Generated starter example for {targetArea} implementation ideas.",
                    QualityStatus = "Generated source only",
                    ContractStatus = "C# file written",
                    ContractChecks = ["File exists after write"],
                    MissingRequirements = ["No integration into the requested application was performed"]
                });

                var dllArtifact = await TryCreateDllArtifactAsync(
                    fileName,
                    source,
                    targetArea,
                    request.UserConfirmedArtifactBuild,
                    cancellationToken).ConfigureAwait(false);
                if (dllArtifact is not null)
                    artifacts.Add(dllArtifact);

                if (councilRuntime.IsWholeSolutionTarget(request.Prompt, result.FinalAnswer, logger) ?? false)
                    artifacts.Add(await CreateSolutionZipArtifactAsync(request, result, timestamp, cancellationToken).ConfigureAwait(false));

                return artifacts;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateImplementationArtifactsAsync");
                return new List<CouncilArtifact>();
            }
        }

        /// <summary>
        /// Creates minecraft datapack artifacts as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        public async Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftDatapackArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            try
            {
                var text = $"{request.Prompt} {result.FinalAnswer}";
                var minecraftVersion = councilText.ExtractMinecraftVersion(text, logger);
                var identity = councilText.BuildMinecraftDatapackArtifactIdentity(text, timestamp, logger) ?? new MinecraftDatapackArtifactIdentity(string.Empty, string.Empty, string.Empty, string.Empty);
                var requestModel = new MinecraftModBuildRequest
                {
                    ProjectName = identity.ProjectName,
                    ModId = identity.ModId,
                    PackageName = identity.PackageName,
                    MinecraftVersion = minecraftVersion,
                    Loader = "Datapack",
                    JavaVersion = minecraftVersion.StartsWith("26.", StringComparison.OrdinalIgnoreCase) ? "25" : "21",
                    GradleVersion = "8.14.2",
                    Ide = "Eclipse",
                    IncludeLivingCitiesStarter = false,
                    Description = councilText.TrimForCodeComment(request.Prompt, 1800, logger)
                };

                var workspace = await minecraftWorkspaceService.CreateWorkspaceAsync(requestModel, cancellationToken).ConfigureAwait(false);
                councilRuntime.ValidateGeneratedDatapackWorkspace(workspace.RootPath, logger);

                var runSuffix = result.RunId.ToString("N")[..8];
                var zipName = $"{identity.ModId}-datapack-{timestamp}-{runSuffix}.zip";
                var zipPath = Path.Combine(ArtifactRoot, zipName);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    councilRuntime.AddFileToZip(archive, Path.Combine(workspace.RootPath, "pack.mcmeta"), "pack.mcmeta", logger);
                    councilRuntime.AddDirectoryToZip(archive, workspace.RootPath, Path.Combine(workspace.RootPath, "data"), logger);
                    councilRuntime.AddDirectoryToZip(archive, workspace.RootPath, Path.Combine(workspace.RootPath, "docs"), logger);
                    councilRuntime.AddFileToZip(archive, workspace.ReadmePath, Path.GetFileName(workspace.ReadmePath), logger);
                }

                logger.LogInformation("Wrote council Minecraft datapack artifact to {Path}", zipPath);

                return
                [
                    new CouncilArtifact
                {
                    Name = zipName,
                    Kind = "Minecraft Java datapack zip",
                    FilePath = zipPath,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}",
                    Summary = $"Generated {identity.DisplayName} datapack for Minecraft {minecraftVersion}. Zip root contains pack.mcmeta and data/ directly.",
                    QualityStatus = "Generated datapack contract checked",
                    ContractStatus = "Datapack structure validated",
                    ContractChecks = ["pack.mcmeta exists", "data/minecraft/tags/functions/load.json exists", "data/minecraft/tags/functions/tick.json exists", "referenced mcfunction files exist"],
                    MissingRequirements = ["Minecraft runtime behavior is not proven by LocalGPT"]
                }
                ];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateMinecraftDatapackArtifactsAsync");
                return new List<CouncilArtifact>();
            }
        }

        /// <summary>
        /// Creates minecraft skeleton matrix artifacts as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        public async Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftSkeletonMatrixArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            try
            {
                var text = $"{request.Prompt} {result.FinalAnswer}";
                var minecraftVersion = councilText.ExtractMinecraftVersion(text, logger);
                var runSuffix = result.RunId.ToString("N")[..8];
                var matrixRoot = Path.Combine(ArtifactRoot, $"minecraft-loader-matrix-{timestamp}-{runSuffix}");
                if (Directory.Exists(matrixRoot))
                    Directory.Delete(matrixRoot, recursive: true);

                Directory.CreateDirectory(matrixRoot);

                var loaders = new[]
                {
                ("Fabric", "fabric_loader_matrix", "client/server Java mod; fabric.mod.json plus Fabric API dependency"),
                ("Paper", "paper_loader_matrix", "server-side plugin; plugin.yml plus Paper API dependency"),
                ("NeoForge", "neoforge_loader_matrix", "Forge-family Java mod; neoforge.mods.toml plus NeoForge dependency")
            };

                foreach (var loader in loaders)
                {
                    var workspace = await minecraftWorkspaceService.CreateWorkspaceAsync(new MinecraftModBuildRequest
                    {
                        ProjectName = $"{loader.Item1}Matrix{timestamp.Replace("-", string.Empty, StringComparison.Ordinal)[..8]}",
                        ModId = loader.Item2,
                        PackageName = $"com.localgpt.matrix.{loader.Item1.ToLowerInvariant()}",
                        MinecraftVersion = minecraftVersion,
                        Loader = loader.Item1,
                        JavaVersion = minecraftVersion.StartsWith("26.", StringComparison.OrdinalIgnoreCase) ? "25" : "21",
                        GradleVersion = "8.14.2",
                        Ide = "Eclipse",
                        IncludeLivingCitiesStarter = false,
                        Description = $"Generated for a benchmark that verifies Fabric, Paper, and NeoForge skeletons stay distinct: {loader.Item3}."
                    }, cancellationToken).ConfigureAwait(false);

                    councilRuntime.CopyDirectory(workspace.RootPath, Path.Combine(matrixRoot, loader.Item1.ToLowerInvariant()), logger);
                }

                await councilRuntime.WriteTextAsync(Path.Combine(matrixRoot, "PROJECT_INDEX.md"), $"""
                # Minecraft Loader Matrix

                Prompt:
                {councilText.TrimForCodeComment(request.Prompt, 1200, logger)}

                This artifact intentionally contains three different Java Edition skeleton families:

                - `fabric/`: Fabric mod skeleton with Fabric metadata/dependencies.
                - `paper/`: Paper plugin skeleton with plugin.yml and server plugin conventions.
                - `neoforge/`: NeoForge mod skeleton with NeoForge metadata/dependencies.

                Validation rule: do not reuse one loader's metadata files for another loader.
                Minecraft version: {minecraftVersion}
                Generated UTC: {DateTime.UtcNow:O}
                """, cancellationToken, logger).ConfigureAwait(false);

                var zipName = $"minecraft-loader-matrix-{timestamp}-{runSuffix}.zip";
                var zipPath = Path.Combine(ArtifactRoot, zipName);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(matrixRoot, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: true);
                logger.LogInformation("Wrote council Minecraft loader matrix artifact to {Path}", zipPath);

                return
                [
                    new CouncilArtifact
                {
                    Name = zipName,
                    Kind = "Minecraft Fabric/Paper/NeoForge skeleton matrix zip",
                    FilePath = zipPath,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}",
                    Summary = "Generated separate Fabric, Paper, and NeoForge skeleton workspaces so loader-specific files cannot be mixed silently.",
                    QualityStatus = "Generated skeletons only",
                    ContractStatus = "Loader family separation written",
                    ContractChecks = ["Fabric, Paper, and NeoForge folders were created"],
                    MissingRequirements = ["No Gradle build or Minecraft launch proof was produced"]
                }
                ];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateMinecraftSkeletonMatrixArtifactsAsync");
                return new List<CouncilArtifact>();
            }
            
        }

        /// <summary>
        /// Creates solution ZIP artifact as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The council artifact produced by the operation.</returns>
        public async Task<CouncilArtifact> CreateSolutionZipArtifactAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            try
            {
                var archetype = councilRuntime.DetectSolutionArchetype(request.Prompt, result.FinalAnswer, logger);
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var projectPrefix = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AiHostLab",
                    GeneratedSolutionArchetype.LocalGpt => "LocalGPTApp",
                    GeneratedSolutionArchetype.TacosPortal => "TacosPortal",
                    GeneratedSolutionArchetype.BotBackend => "BotBackend",
                    _ => "LocalGptLab"
                };
                var runSuffix = result.RunId.ToString("N")[..8];
                var compactTimestamp = timestamp.Replace("-", string.Empty, StringComparison.Ordinal);
                var projectName = $"{projectPrefix}{compactTimestamp[^6..]}";
                var solutionRoot = Path.Combine(ArtifactRoot, $"{projectName}-{runSuffix}");
                var projectRoot = Path.Combine(solutionRoot, "src", projectName);
                var componentsRoot = Path.Combine(projectRoot, "Components");
                var pagesRoot = Path.Combine(componentsRoot, "Pages");
                var servicesRoot = Path.Combine(projectRoot, "Services");
                var modelsRoot = Path.Combine(projectRoot, "Models");
                var wwwroot = Path.Combine(projectRoot, "wwwroot");
                var navIconsRoot = Path.Combine(wwwroot, "icons", "nav");
                var promiseModules = councilRuntime.ExtractDynamicPromiseModules(request, result, logger);

                if (Directory.Exists(solutionRoot))
                    Directory.Delete(solutionRoot, recursive: true);

                Directory.CreateDirectory(pagesRoot);
                Directory.CreateDirectory(servicesRoot);
                Directory.CreateDirectory(modelsRoot);
                Directory.CreateDirectory(wwwroot);
                Directory.CreateDirectory(navIconsRoot);

                var solutionGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
                var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();

                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, $"{projectName}.sln"), councilText.GenerateSolutionFile(projectName, projectGuid, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(projectRoot, $"{projectName}.csproj"), catalog.GenerateSolutionProjectFile, cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(projectRoot, "Program.cs"), councilText.GenerateSolutionProgram(projectName, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(projectRoot, "_Imports.razor"), councilText.GenerateSolutionImports(projectName, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(projectRoot, "appsettings.json"), councilText.GenerateSolutionAppSettings(isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(componentsRoot, "App.razor"), catalog.GenerateSolutionAppRazor, cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(componentsRoot, "Routes.razor"), catalog.GenerateSolutionRoutesRazor, cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(componentsRoot, "GeneratedNavigation.razor"), councilText.GenerateSolutionNavigationRazor(archetype ?? GeneratedSolutionArchetype.Generic, promiseModules, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "Index.razor"), councilText.GenerateSolutionIndexRazor(request, result, archetype ?? GeneratedSolutionArchetype.Generic, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "GeneratedDashboard.razor"), councilText.GenerateSolutionDashboardRazor(request, result, archetype ?? GeneratedSolutionArchetype.Generic, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "GeneratedKnowledgeTable.razor"), councilText.GenerateSolutionKnowledgeTableRazor(isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "SourceFidelity.razor"), catalog.GenerateSourceFidelityRazor, cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(
                    Path.Combine(pagesRoot, isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                    councilText.GenerateSolutionDetailRazor(request, result, isAiHostLab, logger),
                    cancellationToken, logger).ConfigureAwait(false);

                foreach (var page in councilRuntime.GenerateArchetypePages(archetype ?? GeneratedSolutionArchetype.Generic, logger))
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, page.FileName), page.Source, cancellationToken, logger).ConfigureAwait(false);
                foreach (var module in promiseModules)
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, module.FileName), councilText.GeneratePromiseModuleRazor(module, logger), cancellationToken, logger).ConfigureAwait(false);

                if (isAiHostLab)
                {
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "Chat.razor"), catalog.GenerateAiHostChatRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "RunningModels.razor"), catalog.GenerateAiHostRunningModelsRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "ModelDownloads.razor"), catalog.GenerateAiHostModelDownloadsRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "Templates.razor"), catalog.GenerateAiHostTemplatesRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "Hardware.razor"), catalog.GenerateAiHostHardwareRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "RunnerPlugins.razor"), catalog.GenerateAiHostRunnerPluginsRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "Logs.razor"), catalog.GenerateAiHostLogsRazor, cancellationToken, logger).ConfigureAwait(false);
                    await councilRuntime.WriteTextAsync(Path.Combine(pagesRoot, "Settings.razor"), catalog.GenerateAiHostSettingsRazor, cancellationToken, logger).ConfigureAwait(false);
                }

                await councilRuntime.WriteTextAsync(Path.Combine(servicesRoot, "GeneratedHealthSummaryService.cs"), councilText.GenerateSolutionService(projectName, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(servicesRoot, "GeneratedSourceFidelityService.cs"), councilText.GenerateSourceFidelityService(projectName, archetype ?? GeneratedSolutionArchetype.Generic, logger), cancellationToken, logger).ConfigureAwait(false);
                if (isAiHostLab)
                    await councilRuntime.WriteTextAsync(Path.Combine(servicesRoot, "GeneratedAiHostArchitectureServices.cs"), councilText.GenerateAiHostArchitectureServices(projectName, logger), cancellationToken, logger).ConfigureAwait(false);

                await councilRuntime.WriteTextAsync(Path.Combine(modelsRoot, "GeneratedHealthCard.cs"), councilText.GenerateSolutionModel(projectName, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(wwwroot, "app.css"), catalog.GenerateSolutionCss, cancellationToken, logger).ConfigureAwait(false);
                foreach (var icon in councilText.GenerateNavigationIconSvgs(logger) ?? new List<(string, string)>())
                    await councilRuntime.WriteTextAsync(Path.Combine(navIconsRoot, icon.FileName), icon.Svg, cancellationToken, logger).ConfigureAwait(false);

                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "README.md"), councilText.GenerateSolutionReadme(projectName, request, result, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "PROJECT_INDEX.md"), councilText.GenerateSolutionProjectIndex(projectName, request, result, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "ARCHITECTURE.md"), councilText.GenerateSolutionArchitectureDoc(projectName, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "SOURCE_FIDELITY.md"), councilText.GenerateSourceFidelityDoc(projectName, archetype ?? GeneratedSolutionArchetype.Generic, promiseModules, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "PROMISE_MAP.md"), councilText.GeneratePromiseMapDoc(projectName, request, result, promiseModules, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "DESIGN_REVIEW.md"), councilText.GenerateDesignReviewDoc(projectName, archetype ?? GeneratedSolutionArchetype.Generic, promiseModules, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "BUILD_AND_RUN.md"), councilText.GenerateSolutionBuildAndRunDoc(projectName, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, ".localgpt-generation.json"), councilText.GenerateLocalGptGenerationJson(projectName, request, result, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                await councilRuntime.WriteTextAsync(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), councilText.GenerateSolutionManifest(projectName, solutionGuid, request, result, isAiHostLab, logger), cancellationToken, logger).ConfigureAwait(false);
                var contract = councilRuntime.ValidateSolutionArtifactContract(solutionRoot, projectName, archetype ?? GeneratedSolutionArchetype.Generic, logger) ?? new ArtifactContractReport(string.Empty, string.Empty, new List<string>(), new List<string>(), string.Empty);

                var zipName = $"{projectName}-{runSuffix}.zip";
                var zipPath = Path.Combine(ArtifactRoot, zipName);
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(solutionRoot, zipPath, CompressionLevel.SmallestSize, includeBaseDirectory: true);
                logger.LogInformation("Wrote council whole-solution artifact to {Path}", zipPath);

                return new CouncilArtifact
                {
                    Name = zipName,
                    Kind = "Downloadable .NET 10 Blazor/DevExpress solution zip",
                    FilePath = zipPath,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(zipName)}",
                    Summary = $"Generated whole-solution artifact with .sln, .csproj, Razor pages, CSS, service/model code, README, and manifest. {contract.Summary}",
                    QualityStatus = contract.QualityStatus,
                    ContractStatus = contract.ContractStatus,
                    ContractChecks = contract.ContractChecks.ToList(),
                    MissingRequirements = contract.MissingRequirements.ToList()
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateSolutionZipArtifactAsync");
                throw;
            }
        }

        /// <summary>
        /// Attempts to create dll artifact as part of the council artifact service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="sourceFileName">Source file name value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="source">Source value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="targetArea">Target area value supplied to the council artifact operation and used when producing its result.</param>
        /// <param name="userConfirmedArtifactBuild">Value indicating whether user confirmed artifact build should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The council artifact produced by the operation.</returns>
        public async Task<CouncilArtifact?> TryCreateDllArtifactAsync(
            string sourceFileName,
            string source,
            string targetArea,
            bool userConfirmedArtifactBuild,
            CancellationToken cancellationToken)
        {
            try
            {
                var projectName = Path.GetFileNameWithoutExtension(sourceFileName);
                var projectDirectory = Path.Combine(ArtifactRoot, projectName);
                var outputDirectory = Path.Combine(projectDirectory, "bin");
                var projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
                var sourcePath = Path.Combine(projectDirectory, "CouncilFeatureRequestExample.cs");
                var dllName = $"{projectName}.dll";
                var dllPath = Path.Combine(ArtifactRoot, dllName);

                Directory.CreateDirectory(projectDirectory);
                Directory.CreateDirectory(outputDirectory);

                await File.WriteAllTextAsync(projectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <GenerateDocumentationFile>true</GenerateDocumentationFile>
                  </PropertyGroup>
                </Project>
                """, cancellationToken).ConfigureAwait(false);
                await File.WriteAllTextAsync(sourcePath, source, cancellationToken).ConfigureAwait(false);

                var build = await artifactBuildExecutor.BuildAsync(
                    projectPath,
                    projectDirectory,
                    "Release",
                    outputDirectory,
                    TimeSpan.FromSeconds(75),
                    cancellationToken,
                    userConfirmed: userConfirmedArtifactBuild).ConfigureAwait(false);

                if (!build.Succeeded)
                {
                    logger.LogInformation(
                        "Council DLL artifact was not produced. Build status: {BuildStatus}; exit code: {ExitCode}.",
                        build.Status,
                        build.ExitCode);
                    return null;
                }

                var builtDll = Path.Combine(outputDirectory, dllName);
                if (!File.Exists(builtDll))
                    return null;

                File.Copy(builtDll, dllPath, overwrite: true);
                logger.LogInformation("Wrote council DLL artifact to {Path}", dllPath);

                return new CouncilArtifact
                {
                    Name = dllName,
                    Kind = "Sandbox compiled .NET DLL",
                    FilePath = dllPath,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(dllName)}",
                    Summary = $"Compiled sandbox assembly for {targetArea} implementation ideas.",
                    QualityStatus = "Compiled sandbox DLL",
                    ContractStatus = "bounded artifact build succeeded for isolated support-code project",
                    ContractChecks = ["ArtifactBuildExecutor returned BuildPassed", "DLL exists after build"],
                    MissingRequirements = ["No application integration or runtime UI proof was produced"]
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create the bounded DLL artifact for source file {SourceFileName} and target area {TargetArea}.", sourceFileName, targetArea);
                return null;
            }
            
        }
    }
}
