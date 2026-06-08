using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.CSharp;
namespace LocalGPT.Services
{
    public partial class CouncilArtifactService(
        ILogger<CouncilArtifactService> logger,
        IMinecraftModWorkspaceService minecraftWorkspaceService) : ICouncilArtifactService
    {
        public string ArtifactRoot { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalGPT",
            "CouncilArtifacts");

        public async Task<IReadOnlyList<CouncilArtifact>> CreateImplementationArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            CancellationToken cancellationToken = default)
        {
            if (!request.GenerateImplementationArtifact)
                return [];

            if ( CouncilChatStaticsGeneral. IsAdviceOnlyPrompt(request.Prompt))
            {
                logger.LogInformation("Skipped council artifact generation for advice-only prompt.");
                return [];
            }

            Directory.CreateDirectory(ArtifactRoot);

            var targetArea = CouncilChatStringFunctions.DetectTargetArea(request.Prompt, result.FinalAnswer, logger);
            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var artifacts = new List<CouncilArtifact>();

            if (CouncilChatStringFunctions.IsMinecraftSkeletonMatrixArtifactTarget(request.Prompt, result.FinalAnswer, logger) ?? false)
            {
                artifacts.AddRange(await CreateMinecraftSkeletonMatrixArtifactsAsync(request, result, timestamp, cancellationToken));
                return artifacts;
            }

            if (CouncilChatStringFunctions.IsMinecraftDatapackArtifactTarget(request.Prompt, result.FinalAnswer, logger) ?? false)
            {
                artifacts.AddRange(await CreateMinecraftDatapackArtifactsAsync(request, result, timestamp, cancellationToken));
                return artifacts;
            }

            if (CouncilChatStaticsGeneral.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea))
            {
                var razorFileName = $"council-feature-page-{timestamp}-{result.RunId:N}.razor";
                var razorPath = Path.Combine(ArtifactRoot, razorFileName);
                var razorSource = CouncilChatStringFunctions.GenerateBlazorDevExpressRazorExample(request, result, logger);
                await File.WriteAllTextAsync(razorPath, razorSource, cancellationToken);
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
            var source = CouncilChatStaticsGeneral.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea)
                ? CouncilChatStringFunctions.GenerateBlazorSupportCode(request, result, targetArea, logger)
                : CouncilChatStringFunctions.GenerateCodeDomExample(request, result, targetArea, logger);

            await File.WriteAllTextAsync(path, source, cancellationToken);
            logger.LogInformation("Wrote council implementation example artifact to {Path}", path);

            artifacts.Add(new CouncilArtifact
            {
                Name = fileName,
                Kind = CouncilChatStaticsGeneral.IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea)
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

            var dllArtifact = await TryCreateDllArtifactAsync(fileName, source, targetArea, cancellationToken);
            if (dllArtifact is not null)
                artifacts.Add(dllArtifact);

            if (CouncilChatStaticsGeneral.IsWholeSolutionTarget(request.Prompt, result.FinalAnswer))
                artifacts.Add(await CreateSolutionZipArtifactAsync(request, result, timestamp, cancellationToken));

            return artifacts;
        }

        public async Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftDatapackArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            var text = $"{request.Prompt} {result.FinalAnswer}";
            var minecraftVersion = CouncilChatStringFunctions.ExtractMinecraftVersion(text, logger);
            var identity = CouncilChatStringFunctions.BuildMinecraftDatapackArtifactIdentity(text, timestamp, logger);
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
                Description = CouncilChatStringFunctions.TrimForCodeComment(request.Prompt, 1800, logger)
            };

            var workspace = await minecraftWorkspaceService.CreateWorkspaceAsync(requestModel, cancellationToken);
            CouncilChatStaticsGeneral.ValidateGeneratedDatapackWorkspace(workspace.RootPath);

            var runSuffix = result.RunId.ToString("N")[..8];
            var zipName = $"{identity.ModId}-datapack-{timestamp}-{runSuffix}.zip";
            var zipPath = Path.Combine(ArtifactRoot, zipName);
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                CouncilChatStaticsGeneral.AddFileToZip(archive, Path.Combine(workspace.RootPath, "pack.mcmeta"), "pack.mcmeta");
                CouncilChatStaticsGeneral.AddDirectoryToZip(archive, workspace.RootPath, Path.Combine(workspace.RootPath, "data"));
                CouncilChatStaticsGeneral.AddDirectoryToZip(archive, workspace.RootPath, Path.Combine(workspace.RootPath, "docs"));
                CouncilChatStaticsGeneral.AddFileToZip(archive, workspace.ReadmePath, Path.GetFileName(workspace.ReadmePath));
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

        public async Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftSkeletonMatrixArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            var text = $"{request.Prompt} {result.FinalAnswer}";
            var minecraftVersion = CouncilChatStringFunctions.ExtractMinecraftVersion(text, logger);
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
                }, cancellationToken);

                CouncilChatStaticsGeneral.CopyDirectory(workspace.RootPath, Path.Combine(matrixRoot, loader.Item1.ToLowerInvariant()));
            }

            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(matrixRoot, "PROJECT_INDEX.md"), $"""
                # Minecraft Loader Matrix

                Prompt:
                { CouncilChatStringFunctions.TrimForCodeComment(request.Prompt, 1200, logger)}

                This artifact intentionally contains three different Java Edition skeleton families:

                - `fabric/`: Fabric mod skeleton with Fabric metadata/dependencies.
                - `paper/`: Paper plugin skeleton with plugin.yml and server plugin conventions.
                - `neoforge/`: NeoForge mod skeleton with NeoForge metadata/dependencies.

                Validation rule: do not reuse one loader's metadata files for another loader.
                Minecraft version: {minecraftVersion}
                Generated UTC: {DateTime.UtcNow:O}
                """, cancellationToken);

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

        public async Task<CouncilArtifact> CreateSolutionZipArtifactAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            var archetype = CouncilChatStaticsGeneral. DetectSolutionArchetype(request.Prompt, result.FinalAnswer);
            var isAiHostLab = archetype == GlobalVariableSlopCollectionToRemove. GeneratedSolutionArchetype.AiHost;
            var projectPrefix = archetype switch
            {
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.AiHost => "AiHostLab",
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt => "LocalGPTApp",
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.TacosPortal => "TacosPortal",
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.BotBackend => "BotBackend",
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
            var promiseModules = CouncilChatStaticsGeneral.ExtractDynamicPromiseModules(request, result);

            if (Directory.Exists(solutionRoot))
                Directory.Delete(solutionRoot, recursive: true);

            Directory.CreateDirectory(pagesRoot);
            Directory.CreateDirectory(servicesRoot);
            Directory.CreateDirectory(modelsRoot);
            Directory.CreateDirectory(wwwroot);
            Directory.CreateDirectory(navIconsRoot);

            var solutionGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();

            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, $"{projectName}.sln"), CouncilChatStringFunctions.GenerateSolutionFile(projectName, projectGuid, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(projectRoot, $"{projectName}.csproj"), GlobalVariableSlopCollectionToRemove.GenerateSolutionProjectFile, cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(projectRoot, "Program.cs"), CouncilChatStringFunctions.GenerateSolutionProgram(projectName, isAiHostLab,logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(projectRoot, "_Imports.razor"), CouncilChatStringFunctions.GenerateSolutionImports(projectName, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(projectRoot, "appsettings.json"), CouncilChatStringFunctions.GenerateSolutionAppSettings(isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(componentsRoot, "App.razor"), GlobalVariableSlopCollectionToRemove.GenerateSolutionAppRazor, cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(componentsRoot, "Routes.razor"), GlobalVariableSlopCollectionToRemove.GenerateSolutionRoutesRazor, cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(componentsRoot, "GeneratedNavigation.razor"), CouncilChatStringFunctions.GenerateSolutionNavigationRazor(archetype, promiseModules, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "Index.razor"), CouncilChatStringFunctions.GenerateSolutionIndexRazor(request, result, archetype, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "GeneratedDashboard.razor"), CouncilChatStringFunctions.GenerateSolutionDashboardRazor(request, result, archetype, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "GeneratedKnowledgeTable.razor"), CouncilChatStringFunctions.GenerateSolutionKnowledgeTableRazor(isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "SourceFidelity.razor"), GlobalVariableSlopCollectionToRemove.GenerateSourceFidelityRazor, cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(
                Path.Combine(pagesRoot, isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                CouncilChatStringFunctions.GenerateSolutionDetailRazor(request, result, isAiHostLab,logger),
                cancellationToken);

            foreach (var page in CouncilChatStaticsGeneral. GenerateArchetypePages(archetype))
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, page.FileName), page.Source, cancellationToken);
            foreach (var module in promiseModules)
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, module.FileName), CouncilChatStringFunctions.GeneratePromiseModuleRazor(module,logger), cancellationToken);

            if (isAiHostLab)
            {
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "Chat.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostChatRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "RunningModels.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostRunningModelsRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "ModelDownloads.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostModelDownloadsRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "Templates.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostTemplatesRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "Hardware.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostHardwareRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "RunnerPlugins.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostRunnerPluginsRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "Logs.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostLogsRazor, cancellationToken);
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(pagesRoot, "Settings.razor"), GlobalVariableSlopCollectionToRemove.GenerateAiHostSettingsRazor, cancellationToken);
            }

            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(servicesRoot, "GeneratedHealthSummaryService.cs"), CouncilChatStringFunctions.GenerateSolutionService(projectName, isAiHostLab,logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(servicesRoot, "GeneratedSourceFidelityService.cs"), CouncilChatStringFunctions.GenerateSourceFidelityService(projectName, archetype, logger), cancellationToken);
            if (isAiHostLab)
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(servicesRoot, "GeneratedAiHostArchitectureServices.cs"), CouncilChatStringFunctions.GenerateAiHostArchitectureServices(projectName, logger), cancellationToken);

            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(modelsRoot, "GeneratedHealthCard.cs"), CouncilChatStringFunctions.GenerateSolutionModel(projectName, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(wwwroot, "app.css"), GlobalVariableSlopCollectionToRemove.GenerateSolutionCss, cancellationToken);
            foreach (var icon in CouncilChatStringFunctions.GenerateNavigationIconSvgs(logger)?? new List<(string,string)>() )
                await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(navIconsRoot, icon.FileName), icon.Svg, cancellationToken);

            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "README.md"), CouncilChatStringFunctions.GenerateSolutionReadme(projectName, request, result, isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "PROJECT_INDEX.md"), CouncilChatStringFunctions.GenerateSolutionProjectIndex(projectName, request, result, isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "ARCHITECTURE.md"), CouncilChatStringFunctions.GenerateSolutionArchitectureDoc(projectName, isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "SOURCE_FIDELITY.md"), CouncilChatStringFunctions.GenerateSourceFidelityDoc(projectName, archetype, promiseModules, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "PROMISE_MAP.md"), CouncilChatStringFunctions.GeneratePromiseMapDoc(projectName, request, result, promiseModules, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "DESIGN_REVIEW.md"), CouncilChatStringFunctions.GenerateDesignReviewDoc(projectName, archetype, promiseModules, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "BUILD_AND_RUN.md"), CouncilChatStringFunctions.GenerateSolutionBuildAndRunDoc(projectName, isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, ".localgpt-generation.json"), CouncilChatStringFunctions.GenerateLocalGptGenerationJson(projectName, request, result, isAiHostLab, logger), cancellationToken);
            await CouncilChatStaticsGeneral.WriteTextAsync(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), CouncilChatStringFunctions.GenerateSolutionManifest(projectName, solutionGuid, request, result, isAiHostLab, logger), cancellationToken);
            var contract = CouncilChatStaticsGeneral.ValidateSolutionArtifactContract(solutionRoot, projectName, archetype);

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

        public async Task<CouncilArtifact?> TryCreateDllArtifactAsync(
            string sourceFileName,
            string source,
            string targetArea,
            CancellationToken cancellationToken)
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
                """, cancellationToken);
            await File.WriteAllTextAsync(sourcePath, source, cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\" -c Release -o \"{outputDirectory}\" /nologo /p:UseSharedCompilation=false",
                WorkingDirectory = projectDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(75));

                using var process = Process.Start(startInfo);
                if (process is null)
                    return null;

                var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
                await process.WaitForExitAsync(timeoutCts.Token);
                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    logger.LogWarning(
                        "Council DLL artifact build failed with exit code {ExitCode}. Output: {Output} Error: {Error}",
                        process.ExitCode,
                        output,
                        error);
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
                    ContractStatus = "dotnet build succeeded for isolated support-code project",
                    ContractChecks = ["dotnet build exited successfully", "DLL exists after build"],
                    MissingRequirements = ["No application integration or runtime UI proof was produced"]
                };
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Timed out while building council DLL artifact.");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not build council DLL artifact.");
                return null;
            }
        }
    }
}