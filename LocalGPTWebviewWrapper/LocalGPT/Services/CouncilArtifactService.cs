using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
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

            Directory.CreateDirectory(ArtifactRoot);

            var targetArea = DetectTargetArea(request.Prompt, result.FinalAnswer);
            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var artifacts = new List<CouncilArtifact>();

            if (IsMinecraftSkeletonMatrixArtifactTarget(request.Prompt, result.FinalAnswer))
            {
                artifacts.AddRange(await CreateMinecraftSkeletonMatrixArtifactsAsync(request, result, timestamp, cancellationToken));
                return artifacts;
            }

            if (IsMinecraftDatapackArtifactTarget(request.Prompt, result.FinalAnswer))
            {
                artifacts.AddRange(await CreateMinecraftDatapackArtifactsAsync(request, result, timestamp, cancellationToken));
                return artifacts;
            }

            if (IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea))
            {
                var razorFileName = $"council-feature-page-{timestamp}-{result.RunId:N}.razor";
                var razorPath = Path.Combine(ArtifactRoot, razorFileName);
                var razorSource = GenerateBlazorDevExpressRazorExample(request, result);
                await File.WriteAllTextAsync(razorPath, razorSource, cancellationToken);
                logger.LogInformation("Wrote council Blazor Razor artifact to {Path}", razorPath);
                artifacts.Add(new CouncilArtifact
                {
                    Name = razorFileName,
                    Kind = "Blazor/DevExpress Razor component",
                    FilePath = razorPath,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(razorFileName)}",
                    Summary = "Generated server-interactive Razor page using DevExpress controls and LocalGPT/TacosPortal-style patterns."
                });

                targetArea = "Blazor/DevExpress frontend";
            }

            var fileName = $"council-feature-example-{timestamp}-{result.RunId:N}.cs";
            var path = Path.Combine(ArtifactRoot, fileName);
            var source = IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea)
                ? GenerateBlazorSupportCode(request, result, targetArea)
                : GenerateCodeDomExample(request, result, targetArea);

            await File.WriteAllTextAsync(path, source, cancellationToken);
            logger.LogInformation("Wrote council implementation example artifact to {Path}", path);

            artifacts.Add(new CouncilArtifact
            {
                Name = fileName,
                Kind = IsBlazorFrontendTarget(request.Prompt, result.FinalAnswer, targetArea)
                    ? "Compileable .NET support code for the Razor artifact"
                    : "CodeDOM C# example",
                FilePath = path,
                DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(fileName)}",
                Summary = $"Generated starter example for {targetArea} implementation ideas."
            });

            var dllArtifact = await TryCreateDllArtifactAsync(fileName, source, targetArea, cancellationToken);
            if (dllArtifact is not null)
                artifacts.Add(dllArtifact);

            if (IsWholeSolutionTarget(request.Prompt, result.FinalAnswer))
                artifacts.Add(await CreateSolutionZipArtifactAsync(request, result, timestamp, cancellationToken));

            return artifacts;
        }

        private async Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftDatapackArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            var text = $"{request.Prompt} {result.FinalAnswer}";
            var minecraftVersion = ExtractMinecraftVersion(text);
            var identity = BuildMinecraftDatapackArtifactIdentity(text, timestamp);
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
                Description = TrimForCodeComment(request.Prompt, 1800)
            };

            var workspace = await minecraftWorkspaceService.CreateWorkspaceAsync(requestModel, cancellationToken);
            ValidateGeneratedDatapackWorkspace(workspace.RootPath);

            var runSuffix = result.RunId.ToString("N")[..8];
            var zipName = $"{identity.ModId}-datapack-{timestamp}-{runSuffix}.zip";
            var zipPath = Path.Combine(ArtifactRoot, zipName);
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                AddFileToZip(archive, Path.Combine(workspace.RootPath, "pack.mcmeta"), "pack.mcmeta");
                AddDirectoryToZip(archive, workspace.RootPath, Path.Combine(workspace.RootPath, "data"));
                AddDirectoryToZip(archive, workspace.RootPath, Path.Combine(workspace.RootPath, "docs"));
                AddFileToZip(archive, workspace.ReadmePath, Path.GetFileName(workspace.ReadmePath));
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
                    Summary = $"Generated {identity.DisplayName} datapack for Minecraft {minecraftVersion}. Zip root contains pack.mcmeta and data/ directly."
                }
            ];
        }

        private async Task<IReadOnlyList<CouncilArtifact>> CreateMinecraftSkeletonMatrixArtifactsAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            var text = $"{request.Prompt} {result.FinalAnswer}";
            var minecraftVersion = ExtractMinecraftVersion(text);
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

                CopyDirectory(workspace.RootPath, Path.Combine(matrixRoot, loader.Item1.ToLowerInvariant()));
            }

            await WriteTextAsync(Path.Combine(matrixRoot, "PROJECT_INDEX.md"), $"""
                # Minecraft Loader Matrix

                Prompt:
                {TrimForCodeComment(request.Prompt, 1200)}

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
                    Summary = "Generated separate Fabric, Paper, and NeoForge skeleton workspaces so loader-specific files cannot be mixed silently."
                }
            ];
        }

        private async Task<CouncilArtifact> CreateSolutionZipArtifactAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string timestamp,
            CancellationToken cancellationToken)
        {
            var archetype = DetectSolutionArchetype(request.Prompt, result.FinalAnswer);
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
            var promiseModules = ExtractDynamicPromiseModules(request, result);

            if (Directory.Exists(solutionRoot))
                Directory.Delete(solutionRoot, recursive: true);

            Directory.CreateDirectory(pagesRoot);
            Directory.CreateDirectory(servicesRoot);
            Directory.CreateDirectory(modelsRoot);
            Directory.CreateDirectory(wwwroot);
            Directory.CreateDirectory(navIconsRoot);

            var solutionGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();

            await WriteTextAsync(Path.Combine(solutionRoot, $"{projectName}.sln"), GenerateSolutionFile(projectName, projectGuid), cancellationToken);
            await WriteTextAsync(Path.Combine(projectRoot, $"{projectName}.csproj"), GenerateSolutionProjectFile(), cancellationToken);
            await WriteTextAsync(Path.Combine(projectRoot, "Program.cs"), GenerateSolutionProgram(projectName, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(projectRoot, "_Imports.razor"), GenerateSolutionImports(projectName), cancellationToken);
            await WriteTextAsync(Path.Combine(projectRoot, "appsettings.json"), GenerateSolutionAppSettings(isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(componentsRoot, "App.razor"), GenerateSolutionAppRazor(), cancellationToken);
            await WriteTextAsync(Path.Combine(componentsRoot, "Routes.razor"), GenerateSolutionRoutesRazor(), cancellationToken);
            await WriteTextAsync(Path.Combine(componentsRoot, "GeneratedNavigation.razor"), GenerateSolutionNavigationRazor(archetype, promiseModules), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "Index.razor"), GenerateSolutionIndexRazor(request, result, archetype), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "GeneratedDashboard.razor"), GenerateSolutionDashboardRazor(request, result, archetype), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "GeneratedKnowledgeTable.razor"), GenerateSolutionKnowledgeTableRazor(isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "SourceFidelity.razor"), GenerateSourceFidelityRazor(), cancellationToken);
            await WriteTextAsync(
                Path.Combine(pagesRoot, isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                GenerateSolutionDetailRazor(request, result, isAiHostLab),
                cancellationToken);

            foreach (var page in GenerateArchetypePages(archetype))
                await WriteTextAsync(Path.Combine(pagesRoot, page.FileName), page.Source, cancellationToken);
            foreach (var module in promiseModules)
                await WriteTextAsync(Path.Combine(pagesRoot, module.FileName), GeneratePromiseModuleRazor(module), cancellationToken);

            if (isAiHostLab)
            {
                await WriteTextAsync(Path.Combine(pagesRoot, "Chat.razor"), GenerateAiHostChatRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "RunningModels.razor"), GenerateAiHostRunningModelsRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "ModelDownloads.razor"), GenerateAiHostModelDownloadsRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "Templates.razor"), GenerateAiHostTemplatesRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "Hardware.razor"), GenerateAiHostHardwareRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "RunnerPlugins.razor"), GenerateAiHostRunnerPluginsRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "Logs.razor"), GenerateAiHostLogsRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "Settings.razor"), GenerateAiHostSettingsRazor(), cancellationToken);
            }

            await WriteTextAsync(Path.Combine(servicesRoot, "GeneratedHealthSummaryService.cs"), GenerateSolutionService(projectName, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(servicesRoot, "GeneratedSourceFidelityService.cs"), GenerateSourceFidelityService(projectName, archetype), cancellationToken);
            if (isAiHostLab)
                await WriteTextAsync(Path.Combine(servicesRoot, "GeneratedAiHostArchitectureServices.cs"), GenerateAiHostArchitectureServices(projectName), cancellationToken);

            await WriteTextAsync(Path.Combine(modelsRoot, "GeneratedHealthCard.cs"), GenerateSolutionModel(projectName), cancellationToken);
            await WriteTextAsync(Path.Combine(wwwroot, "app.css"), GenerateSolutionCss(), cancellationToken);
            foreach (var icon in GenerateNavigationIconSvgs())
                await WriteTextAsync(Path.Combine(navIconsRoot, icon.FileName), icon.Svg, cancellationToken);

            await WriteTextAsync(Path.Combine(solutionRoot, "README.md"), GenerateSolutionReadme(projectName, request, result, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "PROJECT_INDEX.md"), GenerateSolutionProjectIndex(projectName, request, result, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "ARCHITECTURE.md"), GenerateSolutionArchitectureDoc(projectName, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "SOURCE_FIDELITY.md"), GenerateSourceFidelityDoc(projectName, archetype, promiseModules), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "PROMISE_MAP.md"), GeneratePromiseMapDoc(projectName, request, result, promiseModules), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "DESIGN_REVIEW.md"), GenerateDesignReviewDoc(projectName, archetype, promiseModules), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "BUILD_AND_RUN.md"), GenerateSolutionBuildAndRunDoc(projectName, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, ".localgpt-generation.json"), GenerateLocalGptGenerationJson(projectName, request, result, isAiHostLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), GenerateSolutionManifest(projectName, solutionGuid, request, result, isAiHostLab), cancellationToken);
            ValidateSolutionArtifactContract(solutionRoot, projectName, isAiHostLab);

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
                Summary = "Generated whole-solution artifact with .sln, .csproj, Razor pages, CSS, service/model code, README, and manifest."
            };
        }

        private async Task<CouncilArtifact?> TryCreateDllArtifactAsync(
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
                    Summary = $"Compiled sandbox assembly for {targetArea} implementation ideas."
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

        private static Task WriteTextAsync(string path, string content, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path has no directory: {path}"));
            return File.WriteAllTextAsync(path, content, cancellationToken);
        }

        private static string GenerateSolutionFile(string projectName, string projectGuid) =>
            $$"""
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

        private static string GenerateSolutionProjectFile() =>
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="DevExpress.Blazor" Version="25.1.*" />
              </ItemGroup>
            </Project>
            """;

        private static string GenerateSolutionAppSettings(bool isAiHostLab)
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
                "NativeRunnerExecutable": "",
                "ModelSearchRoots": [
                  "%USERPROFILE%/.ollama/models",
                  "%LOCALAPPDATA%/LocalGPT/ModelFiles",
                  "%LOCALAPPDATA%/GeneratedAiHost/Models"
                ],
                "ContextTokens": 32768,
                "GpuLayers": 20,
                "AllowNativeRunner": true,
                "AllowPythonNet": false,
                "AllowPowerShellScripts": false,
                "AllowTypeScriptAdapters": false
              }
            }
            """;
        }

        private static string GenerateSolutionProgram(string projectName, bool isAiHostLab)
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
                  app.MapPost("/api/generate", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedModelActionRequest request, CancellationToken cancellationToken) => await provider.GenerateAsync(request, cancellationToken));
                  app.MapPost("/api/chat", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedChatRequest request, CancellationToken cancellationToken) => await provider.ChatAsync(request, cancellationToken));
                  app.MapPost("/api/embed", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  app.MapPost("/api/embeddings", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  app.MapGet("/api/blobs/{digest}", (string digest) => Results.Json(new { digest, status = "planned", boundary = "Blob storage is represented as metadata only in this generated lab." }));
                  app.MapGet("/api/localgpt/runner/capability", async ([FromServices] IInferenceRunner runner, CancellationToken cancellationToken) => await runner.GetCapabilityAsync(cancellationToken));
                  app.MapGet("/api/localgpt/plugins", ([FromServices] IPluginCatalogService plugins) => plugins.GetPlugins());
                  app.MapGet("/api/localgpt/hardware-budget", ([FromServices] IHardwareBudgetService hardware) => hardware.GetBudget());
                  app.MapGet("/api/localgpt/chat-templates", ([FromServices] IChatTemplateService templates) => templates.GetTemplateRules());
                  app.MapPost("/api/localgpt/scripts/plan", ([FromServices] IScriptExecutionService scripts, [FromBody] GeneratedScriptPlanRequest request) => scripts.CreatePlan(request.ScriptKind, request.Target, request.UserApproved));
                  app.MapGet("/v1/models", ([FromServices] IModelCatalogService catalog) => new { data = catalog.GetAiHostTags() });
                  app.MapPost("/v1/chat/completions", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedChatRequest request, CancellationToken cancellationToken) => await provider.ChatAsync(request, cancellationToken));
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

        private static string GenerateSolutionImports(string projectName) =>
            $$"""
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

        private static string GenerateSolutionAppRazor() =>
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <base href="/" />
                <link href="_content/DevExpress.Blazor.Themes/blazing-berry.bs5.css" rel="stylesheet" />
                <link href="app.css" rel="stylesheet" />
                <HeadOutlet @rendermode="InteractiveServer" />
            </head>
            <body>
                <Routes @rendermode="InteractiveServer" />
                <script src="_framework/blazor.web.js"></script>
            </body>
            </html>
            """;

        private static string GenerateSolutionRoutesRazor() =>
            """
            <Router AppAssembly="@typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="@routeData" />
                    <FocusOnNavigate RouteData="@routeData" Selector="h1" />
                </Found>
                <NotFound>
                    <PageTitle>Not Found</PageTitle>
                    <p role="alert">This generated LocalGPT route was not found.</p>
                </NotFound>
            </Router>
            """;

        private static string GenerateSolutionNavigationRazor(
            GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules)
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
            var promiseLinks = BuildPromiseNavigationLinks(promiseModules);

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

        private static string BuildPromiseNavigationLinks(IReadOnlyList<GeneratedPromiseModule> promiseModules)
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

        private static string GenerateSolutionIndexRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            GeneratedSolutionArchetype archetype)
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
            var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 500));
            var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 700));

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

        private static string GenerateSolutionDashboardRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            GeneratedSolutionArchetype archetype)
        {
            var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
            var isAiHostLiteral = isAiHostLab ? "true" : "false";
            var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 700));
            var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 900));
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

        private static string GenerateSolutionKnowledgeTableRazor(bool isAiHostLab)
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

        private static string GenerateSourceFidelityRazor() =>
            """
            @page "/source-fidelity"
            @rendermode InteractiveServer
            @inject ISourceFidelityService FidelityService

            <PageTitle>Source Fidelity</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation />

                <section class="generated-header">
                    <div>
                        <h1>Source Fidelity</h1>
                        <p>Checks whether this generated solution represents the requested source architecture instead of only compiling.</p>
                    </div>
                </section>

                <DxGrid Data="@Rows"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        ShowFilterRow="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.SourceSignal)" Caption="Source Signal" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.GeneratedBoundary)" Caption="Generated Boundary" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedSourceFidelityRequirement.Evidence)" Caption="Evidence" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Review rule" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Acceptance" ColSpanMd="12">
                            <DxMemo Text="@ReviewRule" Rows="4" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedSourceFidelityRequirement> Rows { get; set; } = [];
                string ReviewRule { get; } =
                    "A generated replacement is not accepted just because it builds. It must preserve the source application's recognizable workflows, service boundaries, persistence shape, navigation, diagnostics, and artifact/download behavior.";

                protected override void OnInitialized()
                {
                    Rows = FidelityService.GetRequirements();
                }
            }
            """;

        private static IReadOnlyList<GeneratedArchetypePage> GenerateArchetypePages(GeneratedSolutionArchetype archetype)
        {
            return archetype switch
            {
                GeneratedSolutionArchetype.LocalGpt =>
                [
                    ArchetypePage("Chat.razor", "/chat", "DXAiChat", "Chat surface with model routing, uploads, artifact links, visible progress, and memory-aware continuation.", ["Model selection", "Council mode", "File context", "Artifact downloads"]),
                    ArchetypePage("ModelCouncil.razor", "/model-council", "AI Council", "Multi-model review surface for feedback talks, polls, missing features, source requests, and implementation artifacts.", ["Minimum two members", "Sequential scheduling", "Poll gate", "Feedback log"]),
                    ArchetypePage("Database.razor", "/database", "SQLite Database", "Editable operational memory for chats, thoughts, logs, knowledge, benchmark scores, and approval markers.", ["CouncilKnowledgeEntries", "ChatMessages", "ApplicationLogs", "BenchmarkResults"]),
                    ArchetypePage("MinecraftModBuilder.razor", "/minecraft-mod-builder", "Minecraft Mod Builder", "Workspace generator for datapacks, Fabric, Paper, NeoForge, Java/Gradle setup, validation, and downloads.", ["Datapack zip", "Loader matrix", "Version resolver", "Validation script"]),
                    ArchetypePage("TestLab.razor", "/test-lab", "Test Lab", "Frontend-accessible diagnostics for API smoke checks, benchmark routes, artifact downloads, and WebView2 workflows.", ["Health", "DXAiFunctions", "Replacement benchmark", "Council feedback"]),
                    ArchetypePage("Install.razor", "/install", "Install", "Model host discovery, Ollama/LM Studio status, model pull planning, runtime checks, and setup guidance.", ["Ollama status", "LM Studio status", "Model downloads", "Java/.NET checks"])
                ],
                GeneratedSolutionArchetype.TacosPortal =>
                [
                    ArchetypePage("TelegramIngestion.razor", "/telegram-ingestion", "Telegram Ingestion", "Event-ingestion boundary with update handling, command routing, idempotency, retries, and sanitized bot service wiring.", ["Update handler", "Command router", "Idempotency", "Retry queue"]),
                    ArchetypePage("Persistence.razor", "/persistence", "Persistence", "Normalized domain persistence with EF/SQLite or provider-specific backend, explicit DTO/service boundaries, and migration notes.", ["Business objects", "DbContext", "DTO boundaries", "Migration safety"]),
                    ArchetypePage("Workers.razor", "/workers", "Workers", "Hosted/background worker view for polling, notification dispatch, API synchronization, and operational diagnostics.", ["Hosted services", "Polling", "Notifications", "Diagnostics"]),
                    ArchetypePage("Admin.razor", "/admin", "Admin", "DevExpress CRUD/admin workbench with roles, audit log, validation, custom security, and operational settings.", ["Users", "Roles", "Audit", "Settings"]),
                    ArchetypePage("ClientShells.razor", "/client-shells", "Client Shells", "Host map for Blazor server, optional WASM client, WinUI/WebView2 wrapper, package boundaries, and debug/deploy notes.", ["Server host", "WASM client", "WinUI/WebView2", "Package diagnostics"])
                ],
                GeneratedSolutionArchetype.BotBackend =>
                [
                    ArchetypePage("Webhooks.razor", "/webhooks", "Webhooks", "Inbound message and event receiver surface with validation, idempotency, and retry diagnostics.", ["Ingress", "Signature check", "Idempotency", "Dead letters"]),
                    ArchetypePage("Conversations.razor", "/conversations", "Conversations", "Conversation-state workbench with memory, moderation, handoff, and compact transcript review.", ["Memory", "Moderation", "Handoff", "Transcript"]),
                    ArchetypePage("BotSettings.razor", "/bot-settings", "Bot Settings", "Provider-neutral bot configuration with secrets stored outside the generated code and visible safety gates.", ["Provider", "Token source", "Allowed commands", "Rate limit"]),
                    ArchetypePage("PythonInterop.razor", "/python-interop", "Python Interop", "Optional Python.NET or process-adapter boundary for transcription, translation, media, or model tooling.", ["Python.NET", "Process adapter", "Safe directory", "User approval"])
                ],
                _ => []
            };
        }

        private static IReadOnlyList<GeneratedPromiseModule> ExtractDynamicPromiseModules(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result)
        {
            var text = $"{request.Prompt} {result.FinalAnswer}";
            var modules = new List<GeneratedPromiseModule>();

            void AddIf(bool condition, string title, string summary, IReadOnlyList<string> areas)
            {
                if (!condition || modules.Any(module => module.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
                    return;

                var route = "/" + ToKebabRoute(title);
                var fileName = $"{ToPascalIdentifier(title)}.razor";
                modules.Add(new GeneratedPromiseModule(fileName, route, title, summary, areas));
            }

            AddIf(
                DevExpressDocumentPattern().IsMatch(text) || ExportFormatPattern().IsMatch(text),
                "Document Exports",
                "Promise-derived surface for report, Office, PDF, spreadsheet, presentation, and document export work owned by backend services.",
                ["Report template", "Format mapping", "Backend service", "Download route"]);
            AddIf(
                text.Contains("FileDownloadController", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("download link", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("download route", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("safe download", StringComparison.OrdinalIgnoreCase),
                "Download Center",
                "Promise-derived surface for generated files, MIME types, safe HTTP GET links, checksums, expiry, and user-visible artifact status.",
                ["Generated files", "HTTP GET", "Checksum", "Expiry"]);
            AddIf(
                text.Contains("DxAiFunctions", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("IAIInferenceProvider", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("/api/inference", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("AI prompt", StringComparison.OrdinalIgnoreCase),
                "AI Prompt Flow",
                "Promise-derived surface for prompt-to-plan workflows, model/provider calls, generated briefs, and Needs verification notes.",
                ["Prompt", "Provider call", "Generated brief", "Verification"]);
            AddIf(
                text.Contains("IModelCatalogService", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("model catalog", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("LM Studio", StringComparison.OrdinalIgnoreCase),
                "Model Host Status",
                "Promise-derived surface for local model/provider inventory, host reachability, selected model, and runtime status.",
                ["Provider", "Model catalog", "Reachability", "Runtime status"]);
            AddIf(
                text.Contains("SQLite", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("EF/", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("persist", StringComparison.OrdinalIgnoreCase),
                "Persistence",
                "Promise-derived surface for database state, DTO projection, migration safety, audit records, and user-approved knowledge.",
                ["EF/SQLite", "DTOs", "Migration safety", "Audit"]);
            AddIf(
                text.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("DxGrid", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("DxFormLayout", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Blazor", StringComparison.OrdinalIgnoreCase),
                "DevExpress UI",
                "Promise-derived surface for DevExpress Blazor controls, layout, navigation, forms, grids, and frontend verification.",
                ["Navigation", "Grid", "Form", "Frontend smoke"]);
            AddIf(
                text.Contains("API endpoint", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("controller", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("/api/", StringComparison.OrdinalIgnoreCase),
                "API Contracts",
                "Promise-derived surface for backend routes, request/response DTOs, validation, errors, and smoke-test calls.",
                ["Routes", "DTOs", "Validation", "Smoke tests"]);

            return modules.Take(8).ToList();
        }

        private static string GeneratePromiseModuleRazor(GeneratedPromiseModule module) =>
            GenerateArchetypePageRazor(module.Route, module.Title, module.Summary, module.Areas);

        private static GeneratedArchetypePage ArchetypePage(
            string fileName,
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas)
        {
            return new GeneratedArchetypePage(
                fileName,
                GenerateArchetypePageRazor(route, title, summary, areas));
        }

        private static string GenerateArchetypePageRazor(
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas)
        {
            var rows = string.Join(
                "," + Environment.NewLine + "            ",
                areas.Select((area, index) => $$"""new("{{EscapeCSharpString(area)}}", "{{(index == 0 ? "Ready" : "Planned")}}", "{{EscapeCSharpString(BuildArchetypeNextAction(area))}}")"""));

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

                private sealed record GeneratedArchetypeRow(string Area, string Status, string NextAction);
            }
            """;
        }

        private static string BuildArchetypeNextAction(string area)
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

        private static string GenerateSolutionDetailRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab)
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

            var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 650));
            var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 800));
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

        private static string GenerateAiHostChatRazor() =>
            """
            @page "/chat"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Chat</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Chat</h1>
                        <p>Exercise the chat route shape through the generated local model-file runner boundary.</p>
                    </div>
                    <DxButton Text="Send runner chat"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="SendStubChat" />
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Chat request" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model" ColSpanMd="4">
                            <DxTextBox @bind-Text="Model" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Prompt" ColSpanMd="8">
                            <DxMemo @bind-Text="Prompt" Rows="3" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Transcript" ColSpanMd="12">
                            <DxMemo Text="@Transcript" Rows="8" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                string Model { get; set; } = "gpt-oss:20b";
                string Prompt { get; set; } = "Explain the generated AI host control-plane route boundaries.";
                string Transcript { get; set; } = "Click Send runner chat to preview a safe /api/chat response.";

                void SendStubChat()
                {
                    Transcript = HealthService.CreateChatTranscript(Model, Prompt);
                }
            }
            """;

        private static string GenerateAiHostRunningModelsRazor() =>
            """
            @page "/running-models"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Running Models</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Running Models</h1>
                        <p>Mirror a local AI host's running-model view as a control-plane status page.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRunningModels()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Name)" Caption="Model" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.ModifiedAt)" Caption="Started" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Size)" Caption="Size" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedAiHostModelTag.Digest)" Caption="Digest" />
                    </Columns>
                </DxGrid>
            </main>
            """;

        private static string GenerateAiHostModelDownloadsRazor() =>
            """
            @page "/model-downloads"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Model Downloads</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Model Downloads</h1>
                        <p>Plan model-file downloads with explicit target paths and user approval.</p>
                    </div>
                    <DxButton Text="Create pull plan"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="CreatePullPlan" />
                </section>

                <DxGrid Data="@HealthService.GetDownloadCandidates()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.Name)" Caption="Model" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SourceType)" Caption="Source" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SourceUrl)" Caption="Catalog URL" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.RecommendedFor)" Caption="Recommended For" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.DownloadRoute)" Caption="Route" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedModelDownloadCandidate.SafetyNote)" Caption="Safety Note" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Selected pull request" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model" ColSpanMd="6">
                            <DxTextBox Text="@SelectedModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Streaming" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="StreamProgress" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Generated plan" ColSpanMd="12">
                            <DxMemo Text="@PullPlanText" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                string SelectedModel { get; set; } = "gpt-oss:20b";
                bool StreamProgress { get; set; }
                string PullPlanText { get; set; } = "Click Create pull plan to preview a safe /api/pull response.";

                void CreatePullPlan()
                {
                    var plan = HealthService.CreatePullPlan(new GeneratedModelActionRequest
                    {
                        Model = SelectedModel,
                        Stream = StreamProgress
                    });
                    PullPlanText = $"{plan.Route} for {plan.Model}: {plan.Status}. {plan.Detail}";
                }
            }
            """;

        private static string GenerateAiHostTemplatesRazor() =>
            """
            @page "/templates"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Chat Templates</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Chat Templates</h1>
                        <p>Track model-specific prompt templates, thinking markers, and compatibility adapters as first-class control-plane data.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetTemplateRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Format" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Detector" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Boundary" />
                    </Columns>
                </DxGrid>
            </main>
            """;

        private static string GenerateAiHostHardwareRazor() =>
            """
            @page "/hardware"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Hardware Budget</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Hardware Budget</h1>
                        <p>Represent GPU, CPU, context, queue, and throttling rules before heavy native runner jobs are allowed.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetHardwareBudgetRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Budget" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Policy" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Reason" />
                    </Columns>
                </DxGrid>
            </main>
            """;

        private static string GenerateAiHostRunnerPluginsRazor() =>
            """
            @page "/runner-plugins"
            @rendermode InteractiveServer
            @inject IPluginCatalogService PluginCatalog
            @inject IInferenceRunner Runner
            @inject IHardwareBudgetService HardwareBudget
            @inject IChatTemplateService ChatTemplates

            <PageTitle>Runner Plugins</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Runner Plugins</h1>
                        <p>Show native-runner boundaries, optional catalog/provider adapters, Python.NET, PowerShell, and managed inference paths as explicit architecture contracts.</p>
                    </div>
                    <DxButton Text="Refresh capability"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="RefreshCapabilityAsync" />
                </section>

                <div class="generated-status-strip">
                    <article>
                        <strong>Native inference</strong>
                        <span>@(Capability?.NativeInferenceImplemented == true ? "Implemented" : "Capability gap")</span>
                    </article>
                    <article>
                        <strong>GPU target</strong>
                        <span>@Budget.TargetGpuLoadPercent% sustained</span>
                    </article>
                    <article>
                        <strong>Parallel models</strong>
                        <span>@Budget.MaxParallelModels</span>
                    </article>
                </div>

                <DxGrid Data="@PluginCatalog.GetPlugins()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Id)" Caption="Plugin Id" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.DisplayName)" Caption="Name" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Contract)" Caption="Contract" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Approved)" Caption="Approved" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Notes)" Caption="Notes" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Runner capability" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Runner kind" ColSpanMd="4">
                            <DxTextBox Text="@Runner.RunnerKind" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Supported formats" ColSpanMd="8">
                            <DxTextBox Text="@SupportedFormatsText" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Missing capability" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.MissingCapability ?? "Capability not loaded yet.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Next milestone" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.NextMilestone ?? "Click Refresh capability.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>

                <DxGrid Data="@ChatTemplates.GetTemplateRules()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Name)" Caption="Template" />
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Rule)" Caption="Rule" />
                    </Columns>
                </DxGrid>
            </main>

            @code {
                RunnerCapabilityReport? Capability { get; set; }
                HardwareBudgetSnapshot Budget { get; set; } = new(85, 20, 2048, 1, "Sequential by default.");
                string SupportedFormatsText => Capability is null ? string.Empty : string.Join(", ", Capability.SupportedFormats);

                protected override async Task OnInitializedAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }

                async Task RefreshCapabilityAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }
            }
            """;

        private static string GenerateAiHostLogsRazor() =>
            """
            @page "/logs"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Logs</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Logs</h1>
                        <p>Surface control-plane diagnostics and runtime-boundary notes where users can inspect them.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRuntimeLogRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Level" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Message" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Action" />
                    </Columns>
                </DxGrid>
            </main>
            """;

        private static string GenerateAiHostSettingsRazor() =>
            """
            @page "/settings"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Settings</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Settings</h1>
                        <p>Configuration is shown as safe generated defaults. Real persistence should be added through backend services and EF/SQLite after user approval.</p>
                    </div>
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generated Runtime Profile" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model Source" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.BaseUri" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Default Model" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.DefaultModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Keep Alive" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.KeepAlive" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Context Tokens" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.ContextTokens.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="GPU Layers" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.GpuLayers.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Native Runner Attached" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="NativeRunnerAttached" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Pull Planning Enabled" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="AllowPullPlanning" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Settings Summary" ColSpanMd="12">
                            <DxMemo Text="@HealthService.BuildSettingsSummary()" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                GeneratedAiHostSettings LabSettings { get; set; } = new();
                bool NativeRunnerAttached { get; set; }
                bool AllowPullPlanning { get; set; }

                protected override void OnInitialized()
                {
                    LabSettings = HealthService.GetSettings();
                    NativeRunnerAttached = LabSettings.NativeRunnerAttached;
                    AllowPullPlanning = LabSettings.AllowPullPlanning;
                }
            }
            """;

        private static string GenerateSolutionService(string projectName, bool isAiHostLab)
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

                private static string NormalizeModel(string? model)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? "gpt-oss:20b"
                        : model.Trim();
                }
            }
            """;
        }

        private static string GenerateSourceFidelityService(string projectName, GeneratedSolutionArchetype archetype)
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

        private static string GenerateAiHostArchitectureServices(string projectName) =>
            $$"""
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
                public bool AllowNativeRunner { get; set; } = true;
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
                    return await runner.InferAsync(request, cancellationToken);
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
                    var response = await runner.InferAsync(chat, cancellationToken);
                    return new
                    {
                        model = response.Model,
                        created_at = response.CreatedAt,
                        response = response.Message.Content,
                        done = response.Done,
                        upstream_proxy = false
                    };
                }

                private static string NormalizeModel(string? model, string fallbackModel)
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
                        await process.WaitForExitAsync(timeout.Token);
                        var output = await outputTask;
                        var error = await errorTask;
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

                private static string? ResolveDirectGguf(string root, string model)
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

                private static string? ResolveOllamaManagedBlob(string root, string model)
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

                private static IReadOnlyList<string> BuildRunnerArguments(string modelPath, string prompt, GeneratedRequestOptions? requestOptions)
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

                private static string BuildPrompt(GeneratedChatRequest request)
                {
                    var builder = new StringBuilder();
                    foreach (var message in request.Messages.Where(message => !string.IsNullOrWhiteSpace(message.Content)))
                        builder.Append(message.Role ?? "user").Append(": ").AppendLine(message.Content);
                    if (builder.Length == 0)
                        builder.AppendLine("user: Hello");
                    builder.Append("assistant: ");
                    return builder.ToString();
                }

                private static (string Name, string Tag) SplitModelName(string model)
                {
                    var parts = model.Split(':', 2, StringSplitOptions.TrimEntries);
                    return (parts[0], parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "latest");
                }

                private static string NormalizeModel(string? model, string fallbackModel)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? fallbackModel
                        : model.Trim();
                }

                private static string ExpandPath(string? path)
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

                private static GeneratedChatResponse BuildStatusResponse(string model, string message)
                {
                    return new GeneratedChatResponse(
                        model,
                        DateTimeOffset.UtcNow,
                        new GeneratedChatMessage("assistant", message),
                        true);
                }

                private static void TryKill(Process process)
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

        private static string GenerateSolutionModel(string projectName) =>
            $$"""
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

        private static string GenerateSolutionCss() =>
            """
            :root {
                color-scheme: light;
                font-family: "Segoe UI", Arial, sans-serif;
            }

            body {
                margin: 0;
                background: #f7f8fa;
                color: #1f2937;
            }

            .generated-shell {
                max-width: 1180px;
                margin: 0 auto;
                padding: 32px;
            }

            .generated-nav {
                display: flex;
                align-items: center;
                gap: 16px;
                margin-bottom: 24px;
                padding-bottom: 14px;
                border-bottom: 1px solid #d9dee7;
            }

            .generated-nav a {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                color: #384252;
                text-decoration: none;
                font-weight: 600;
            }

            .generated-nav a:hover,
            .generated-nav a:focus-visible {
                color: #0b5cab;
            }

            .generated-nav .generated-brand {
                margin-right: auto;
                color: #172033;
                font-weight: 700;
            }

            .generated-nav-icon {
                width: 18px;
                height: 18px;
                flex: 0 0 18px;
            }

            .generated-nav-icon-solid {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-line,
            .generated-nav a:focus-visible .generated-nav-icon-line {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-solid,
            .generated-nav a:focus-visible .generated-nav-icon-solid {
                display: inline-block;
            }

            .generated-hero {
                display: grid;
                grid-template-columns: minmax(0, 1fr) auto;
                gap: 20px;
                align-items: end;
                padding: 28px 0 24px;
            }

            .generated-hero h1 {
                margin: 0;
                font-size: 34px;
                line-height: 1.1;
            }

            .generated-hero p {
                max-width: 760px;
                color: #536173;
            }

            .generated-kicker {
                margin: 0 0 8px;
                color: #0f766e;
                font-weight: 700;
                text-transform: uppercase;
                letter-spacing: 0;
            }

            .generated-actions {
                display: flex;
                gap: 10px;
                flex-wrap: wrap;
                justify-content: flex-end;
            }

            .generated-split {
                display: grid;
                grid-template-columns: minmax(0, 1fr) minmax(320px, 0.8fr);
                gap: 24px;
                align-items: start;
            }

            .generated-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 16px;
                margin-bottom: 20px;
            }

            .generated-header h1 {
                margin: 0;
                font-size: 28px;
            }

            .generated-header p,
            .generated-muted {
                margin: 6px 0 0;
                color: #5f6b7a;
            }

            .generated-grid,
            .generated-form {
                margin-top: 18px;
            }

            .generated-note {
                margin-top: 22px;
            }

            .generated-code {
                overflow: auto;
                padding: 16px;
                border: 1px solid #d9dee7;
                background: #ffffff;
                border-radius: 6px;
            }

            @media (max-width: 860px) {
                .generated-shell {
                    padding: 20px;
                }

                .generated-hero,
                .generated-split {
                    grid-template-columns: 1fr;
                }

                .generated-actions {
                    justify-content: flex-start;
                }
            }
            """;

        private static IReadOnlyList<(string FileName, string Svg)> GenerateNavigationIconSvgs() =>
        [
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

        private static string GenerateSolutionReadme(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab)
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
            - Blazor Web App `Program.cs`, `App.razor`, `Routes.razor`
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

            {{TrimForCodeComment(request.Prompt, 1200)}}

            ## Council Output Summary

            {{TrimForCodeComment(result.FinalAnswer, 1200)}}
            """;
        }

        private static string GenerateSolutionProjectIndex(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab)
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
            - `src/{{projectName}}/Components/Routes.razor` - Blazor route discovery.
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

            {{TrimForCodeComment(request.Prompt, 900)}}

            ## Council Summary

            {{TrimForCodeComment(result.FinalAnswer, 900)}}
            """;
        }

        private static string GenerateSolutionArchitectureDoc(string projectName, bool isAiHostLab)
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

        private static string GenerateSourceFidelityDoc(
            string projectName,
            GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules)
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

        private static string GeneratePromiseMapDoc(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            IReadOnlyList<GeneratedPromiseModule> promiseModules)
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
            {{TrimForCodeComment(request.Prompt, 1200)}}
            ```

            ## Council Excerpt

            ```text
            {{TrimForCodeComment(result.FinalAnswer, 1600)}}
            ```
            """;
        }

        private static string GenerateDesignReviewDoc(
            string projectName,
            GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules)
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

        private static string GenerateSolutionBuildAndRunDoc(string projectName, bool isAiHostLab)
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

        private static string GenerateLocalGptGenerationJson(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab)
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
              "project_name": "{{EscapeJsonString(projectName)}}",
              "generated_at_utc": "{{DateTime.UtcNow:O}}",
              "complexity": "normal",
              "needs_datagen": false,
              "needs_tests": true,
              "needs_native_commands": {{(isAiHostLab ? "true" : "false")}},
              "needs_index": true,
              "needs_version_resolver": false,
              "model_names": "{{EscapeJsonString(string.Join(", ", result.ModelNames))}}",
              "requested_features": "{{EscapeJsonString(TrimForCodeComment(request.Prompt, 900))}}",
              "validation_status": "GeneratedOnlyContractValidated",
              "validation_notes": "{{EscapeJsonString(validationNotes)}}",
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

        private static string GenerateSolutionManifest(
            string projectName,
            string solutionGuid,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab)
        {
            var sourceGoal = isAiHostLab
                ? ".NET 10 ASP.NET Core and DevExpress Blazor AI host control-plane lab with explicit provider, plugin, script, and native-runner adapter boundaries"
                : "LocalGPT/TacosPortalOpen-style .NET 10 Blazor and DevExpress generation";

            return
            $$"""
            {
              "projectName": "{{EscapeJsonString(projectName)}}",
              "solutionGuid": "{{EscapeJsonString(solutionGuid)}}",
              "generatedAtUtc": "{{DateTime.UtcNow:O}}",
              "modelNames": "{{EscapeJsonString(string.Join(", ", result.ModelNames))}}",
              "artifactKind": "WholeSolutionZip",
              "sourceGoal": "{{EscapeJsonString(sourceGoal)}}",
              "designContract": "Bootstrap v5 layout, DevExpress Blazor controls, and paired line/solid SVG navigation icons.",
              "validationStatus": "GeneratedOnlyContractValidated",
              "buildTestResultProvenance": "Required files and contract metadata were validated before zipping. No generated-project build success is claimed.",
              "request": "{{EscapeJsonString(TrimForCodeComment(request.Prompt, 1400))}}",
              "finalAnswer": "{{EscapeJsonString(TrimForCodeComment(result.FinalAnswer, 1400))}}",
              "safety": "Sandbox artifact only. Integration requires explicit user approval."
            }
            """;
        }

        private static string GenerateBlazorDevExpressRazorExample(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result)
        {
            var requestSummary = TrimForCodeComment(request.Prompt, 700);
            var consensusSummary = TrimForCodeComment(result.FinalAnswer, 900);
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
                    string RequestSummary { get; } = "{{EscapeCSharpString(requestSummary)}}";
                    string CouncilConsensus { get; } = "{{EscapeCSharpString(consensusSummary)}}";

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

        private static string GenerateBlazorSupportCode(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string targetArea)
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
                    public const string TargetArea = "{{EscapeCSharpString(targetArea)}}";
                    public const string CouncilMembers = "{{EscapeCSharpString(string.Join(", ", result.ModelNames))}}";
                    public const string OriginalRequest = "{{EscapeCSharpString(TrimForCodeComment(request.Prompt, 900))}}";

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

        private static string GenerateCodeDomExample(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            string targetArea)
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

            type.Members.Add(CreateConstant("TargetArea", targetArea));
            type.Members.Add(CreateConstant("CouncilMembers", string.Join(", ", result.ModelNames)));
            type.Members.Add(CreateConstant("OriginalRequest", TrimForCodeComment(request.Prompt, 900)));

            var method = new CodeMemberMethod
            {
                Name = "BuildImplementationRequestMarkdown",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(string))
            };
            method.Comments.Add(new CodeCommentStatement("This shape can be pasted into DXAiChat or an AI Council continuation round."));
            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(StringBuilder), "builder", new CodeObjectCreateExpression(typeof(StringBuilder))));
            AppendLine(method, "# LocalGPT Implementation Request");
            AppendLine(method, "");
            AppendLine(method, $"Target area: {targetArea}");
            AppendLine(method, $"Council members: {string.Join(", ", result.ModelNames)}");
            AppendLine(method, "");
            AppendLine(method, "## Requested feature");
            AppendLine(method, TrimForCodeComment(request.Prompt, 1000));
            AppendLine(method, "");
            AppendLine(method, "## Current council consensus");
            AppendLine(method, TrimForCodeComment(result.FinalAnswer, 1600));
            AppendLine(method, "");
            AppendLine(method, "## Implementation checklist");
            AppendLine(method, "- Identify the owning LocalGPT service/page/project.");
            AppendLine(method, "- Check /__diag/devexpress before proposing DevExpress APIs or UI components.");
            AppendLine(method, "- Put DevExpress Office/report/PDF/export generation in ASP.NET Core backend services and expose safe download links.");
            AppendLine(method, "- Keep native commands in backend services.");
            AppendLine(method, "- Save user-visible state to EF/SQLite when it affects future chats.");
            AppendLine(method, "- Prototype requested features in a harmless sandbox artifact or temporary workspace before integrating into the real project.");
            AppendLine(method, "- Ask the user for explicit permission before integrating any generated expansion into LocalGPT.");
            AppendLine(method, "- Never overrule a user decision that denies or limits self-expansion.");
            AppendLine(method, "- List helpful official docs, examples, specs, or source repositories needed before implementation.");
            AppendLine(method, "- Add a diagnostic endpoint or smoke path before relying on UI behavior.");
            AppendLine(method, "- Mark unknown dependencies as Needs verification.");
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

        private static CodeMemberField CreateConstant(string name, string value)
        {
            return new CodeMemberField(typeof(string), name)
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Const,
                InitExpression = new CodePrimitiveExpression(value)
            };
        }

        private static void AppendLine(CodeMemberMethod method, string line)
        {
            method.Statements.Add(new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression("builder"),
                "AppendLine",
                new CodePrimitiveExpression(line)));
        }

        private static bool IsMinecraftDatapackArtifactTarget(string prompt, string finalAnswer)
        {
            var text = prompt;
            return MinecraftPattern().IsMatch(text) && DatapackPattern().IsMatch(text);
        }

        private static bool IsMinecraftSkeletonMatrixArtifactTarget(string prompt, string finalAnswer)
        {
            var text = prompt;
            return MinecraftPattern().IsMatch(text) && MinecraftSkeletonMatrixPattern().IsMatch(text);
        }

        private static string ExtractMinecraftVersion(string text)
        {
            var match = MinecraftVersionPattern().Match(text);
            return match.Success
                ? match.Groups["version"].Value
                : MinecraftDatapackVersionCatalog.DefaultMinecraftVersion;
        }

        private static MinecraftDatapackArtifactIdentity BuildMinecraftDatapackArtifactIdentity(string text, string timestamp)
        {
            var displayName = ExtractMinecraftProjectDisplayName(text);
            var modId = ToMinecraftNamespace(displayName);
            var projectName = ToPascalIdentifier(displayName);
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

        private static string ExtractMinecraftProjectDisplayName(string text)
        {
            var quoted = Regex.Match(text, "\"(?<name>[A-Z][A-Za-z0-9 _-]{2,60})\"");
            if (quoted.Success)
                return CleanMinecraftProjectDisplayName(quoted.Groups["name"].Value);

            var explicitlyNamed = Regex.Match(text, @"(?i)(?:called|named|titled)\s+(?<name>[A-Z][A-Za-z0-9 _-]{2,60})");
            if (explicitlyNamed.Success)
                return CleanMinecraftProjectDisplayName(explicitlyNamed.Groups["name"].Value);

            var named = Regex.Match(
                text,
                @"(?i)(?:datapack|data pack|modpack|minecraft project|minecraft mod)\s+(?:called|named|for|about)?\s*(?<name>[A-Z][A-Za-z0-9 _-]{2,60})");
            if (named.Success)
                return CleanMinecraftProjectDisplayName(named.Groups["name"].Value);

            var heading = Regex.Match(text, @"(?m)^#\s+(?<name>[A-Za-z0-9 _-]{3,60})");
            if (heading.Success)
                return CleanMinecraftProjectDisplayName(heading.Groups["name"].Value);

            return "Prompted Datapack";
        }

        private static string CleanMinecraftProjectDisplayName(string value)
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

        private static string ToMinecraftNamespace(string value)
        {
            var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            return string.IsNullOrWhiteSpace(normalized) ? "prompted_datapack" : normalized;
        }

        private static string ToPascalIdentifier(string value)
        {
            var words = Regex.Matches(value, "[A-Za-z0-9]+")
                .Select(match => match.Value)
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Take(5);
            return string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }

        private static string ToKebabRoute(string value)
        {
            var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "promise-module" : normalized;
        }

        private static void ValidateGeneratedDatapackWorkspace(string rootPath)
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
                if (LeadingSlashCommandPattern().IsMatch(content))
                    throw new InvalidOperationException($"Generated function contains a leading slash command: {Path.GetRelativePath(rootPath, functionFile)}");
                if (RootStorageRemovePattern().IsMatch(content))
                    throw new InvalidOperationException($"Generated function uses data remove storage root syntax: {Path.GetRelativePath(rootPath, functionFile)}");
                if (MalformedStorageTargetPattern().IsMatch(content))
                    throw new InvalidOperationException($"Generated function appears to put an NBT path into the storage id instead of after it: {Path.GetRelativePath(rootPath, functionFile)}");
            }
        }

        private static void AddDirectoryToZip(ZipArchive archive, string rootPath, string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
                AddFileToZip(archive, filePath, entryName);
            }
        }

        private static void AddFileToZip(ZipArchive archive, string filePath, string entryName)
        {
            if (!File.Exists(filePath))
                return;

            archive.CreateEntryFromFile(filePath, entryName.Replace('\\', '/'), CompressionLevel.SmallestSize);
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
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

        private static string DetectTargetArea(string prompt, string finalAnswer)
        {
            var text = $"{prompt} {finalAnswer}";
            if (DevExpressDocumentPattern().IsMatch(text))
                return "DevExpress document/report backend";
            if (BlazorFrontendPattern().IsMatch(text))
                return "Blazor/DevExpress frontend";
            if (DotNetPattern().IsMatch(text))
                return ".NET/Blazor/ASP.NET Core";
            if (MinecraftPattern().IsMatch(text))
                return "Minecraft builder";
            if (FrontendPattern().IsMatch(text))
                return "Blazor frontend";
            if (LoggingPattern().IsMatch(text))
                return "diagnostics and logging";

            return "LocalGPT feature";
        }

        private static bool IsBlazorFrontendTarget(string prompt, string finalAnswer, string targetArea)
        {
            return targetArea.Contains("Blazor/DevExpress frontend", StringComparison.OrdinalIgnoreCase) ||
                BlazorFrontendPattern().IsMatch($"{prompt} {finalAnswer}");
        }

        private static bool IsWholeSolutionTarget(string prompt, string finalAnswer)
        {
            var text = $"{prompt} {finalAnswer}";
            return WholeSolutionPattern().IsMatch(text) || IsAiHostExperimentTarget(prompt, finalAnswer);
        }

        private static bool IsAiHostExperimentTarget(string prompt, string finalAnswer)
        {
            return AiHostExperimentPattern().IsMatch($"{prompt} {finalAnswer}");
        }

        private static GeneratedSolutionArchetype DetectSolutionArchetype(string prompt, string finalAnswer)
        {
            var text = $"{prompt} {finalAnswer}";
            if (AiHostExperimentPattern().IsMatch(text))
                return GeneratedSolutionArchetype.AiHost;
            if (LocalGptReplacementPattern().IsMatch(text))
                return GeneratedSolutionArchetype.LocalGpt;
            if (TacosPortalPattern().IsMatch(text))
                return GeneratedSolutionArchetype.TacosPortal;
            if (BotBackendPattern().IsMatch(text))
                return GeneratedSolutionArchetype.BotBackend;

            return GeneratedSolutionArchetype.Generic;
        }

        private static string TrimForCodeComment(string text, int maxLength)
        {
            var normalized = WhitespacePattern().Replace(text, " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static string EscapeCSharpString(string text)
        {
            return text
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);
        }

        private static string EscapeJsonString(string text)
        {
            return EscapeCSharpString(text);
        }

        private static void ValidateSolutionArtifactContract(string solutionRoot, string projectName, bool isAiHostLab)
        {
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

            ValidateGenerationContractJson(Path.Combine(solutionRoot, ".localgpt-generation.json"));
            ValidateGenerationManifestJson(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"));
        }

        private static void ValidateGenerationContractJson(string path)
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
                RequireJsonProperty(root, property, path);

            RequireNonEmptyJsonArray(root, "expected_entrypoints", path);
            RequireNonEmptyJsonArray(root, "generated_files", path);
        }

        private static void ValidateGenerationManifestJson(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            RequireJsonProperty(root, "artifactKind", path);
            RequireJsonProperty(root, "sourceGoal", path);
            RequireJsonProperty(root, "validationStatus", path);
            RequireJsonProperty(root, "buildTestResultProvenance", path);
        }

        private static void RequireJsonProperty(JsonElement root, string propertyName, string path)
        {
            if (!root.TryGetProperty(propertyName, out _))
                throw new InvalidOperationException($"Generated contract {Path.GetFileName(path)} is missing {propertyName}.");
        }

        private static void RequireNonEmptyJsonArray(JsonElement root, string propertyName, string path)
        {
            RequireJsonProperty(root, propertyName, path);
            var property = root.GetProperty(propertyName);
            if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    $"Generated contract {Path.GetFileName(path)} must include a non-empty {propertyName} array.");
            }
        }

        [GeneratedRegex("(devexpress|richedit|pdfviewer|pivot|report|xtrareport|office|docx|xlsx|pdf export|spreadsheet|document generation)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DevExpressDocumentPattern();

        [GeneratedRegex("(\\.xlsx|xlsx|excel|\\.pptx|pptx|powerpoint|\\.pdf|pdf|\\.docx|docx|word|export format|file generation)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ExportFormatPattern();

        [GeneratedRegex("(blazor|razor|component|page|dxgrid|dxformlayout|dxbutton|dxmemo|dxtextbox|dxcombobox|dxaichat|devexpress blazor|interactive(server|webassembly|auto))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex BlazorFrontendPattern();

        [GeneratedRegex("(dotnet|\\.net|aspnet|asp\\.net|blazor|c#|codedom|entityframework|sqlite|winui|webview2)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DotNetPattern();

        [GeneratedRegex("(minecraft|fabric|neoforge|paper|datapack|gradle|java)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MinecraftPattern();

        [GeneratedRegex("(datapack|data pack|pack\\.mcmeta|mcfunction|living cities)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DatapackPattern();

        [GeneratedRegex("(fabric.*paper.*neoforge|neoforge.*paper.*fabric|loader.*matrix|skeleton.*distinction|project skeleton distinction)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MinecraftSkeletonMatrixPattern();

        [GeneratedRegex("(?<!\\d)(?<version>(?:1\\.\\d{1,2}|26\\.\\d)(?:\\.\\d{1,2})?(?:-snapshot-\\d+)?)(?!\\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MinecraftVersionPattern();

        [GeneratedRegex("(?m)^\\s*/", RegexOptions.CultureInvariant)]
        private static partial Regex LeadingSlashCommandPattern();

        [GeneratedRegex("\\bdata\\s+remove\\s+storage\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RootStorageRemovePattern();

        [GeneratedRegex("\\bstore\\s+result\\s+storage\\s+[a-z0-9_.-]+:[a-z0-9_/-]+\\.[a-z0-9_.-]+\\s+(?:byte|short|int|long|float|double)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MalformedStorageTargetPattern();

        [GeneratedRegex("(frontend|razor|devexpress|dxaichat|css|javascript)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex FrontendPattern();

        [GeneratedRegex("(whole solution|full solution|entire solution|solution zip|project zip|\\.sln|\\.csproj|all source files|tacosportalopen|localgpt|whole ai host|ai host dotnet|local ai host|whole ollama|ollama dotnet|ollama \\.net)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex WholeSolutionPattern();

        [GeneratedRegex("(ai host|local ai host|model host|chat host|ollama).*(dotnet|\\.net|blazor|devexpress|aspnet|asp\\.net)|(dotnet|\\.net|blazor|devexpress|aspnet|asp\\.net).*(ai host|local ai host|model host|chat host|ollama)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AiHostExperimentPattern();

        [GeneratedRegex("(localgpt|local gpt|dxaichat|ai council|minecraft mod builder|sqlite memory|test lab)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex LocalGptReplacementPattern();

        [GeneratedRegex("(tacosportalopen|tacos portal|restaurant portal|orders.*menu|menu.*orders|reservation|kitchen queue)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex TacosPortalPattern();

        [GeneratedRegex("(bot backend|telegram bot|botapi|webhook|conversation state|python\\.net|whisper|translator bot)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex BotBackendPattern();

        [GeneratedRegex("(log|logger|diagnostic|error|warning|telemetry)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex LoggingPattern();

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();

        private sealed record MinecraftDatapackArtifactIdentity(
            string ProjectName,
            string ModId,
            string PackageName,
            string DisplayName);

        private enum GeneratedSolutionArchetype
        {
            Generic,
            LocalGpt,
            TacosPortal,
            BotBackend,
            AiHost
        }

        private sealed record GeneratedArchetypePage(string FileName, string Source);

        private sealed record GeneratedPromiseModule(
            string FileName,
            string Route,
            string Title,
            string Summary,
            IReadOnlyList<string> Areas);
    }
}
