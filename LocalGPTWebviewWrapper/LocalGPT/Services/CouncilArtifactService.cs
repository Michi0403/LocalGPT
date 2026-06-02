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
                JavaVersion = "21",
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
                    ProjectName = $"{loader.Item1}LoaderMatrix{timestamp.Replace("-", string.Empty, StringComparison.Ordinal)}",
                    ModId = loader.Item2,
                    PackageName = $"com.localgpt.matrix.{loader.Item1.ToLowerInvariant()}",
                    MinecraftVersion = minecraftVersion,
                    Loader = loader.Item1,
                    JavaVersion = "21",
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
            var isOllamaLab = IsOllamaDotNetExperimentTarget(request.Prompt, result.FinalAnswer);
            var projectPrefix = isOllamaLab ? "OllamaDotNetLab" : "LocalGptLab";
            var runSuffix = result.RunId.ToString("N")[..8];
            var projectName = $"{projectPrefix}{timestamp.Replace("-", string.Empty, StringComparison.Ordinal)}";
            var solutionRoot = Path.Combine(ArtifactRoot, $"{projectName}-{runSuffix}");
            var projectRoot = Path.Combine(solutionRoot, "src", projectName);
            var componentsRoot = Path.Combine(projectRoot, "Components");
            var pagesRoot = Path.Combine(componentsRoot, "Pages");
            var servicesRoot = Path.Combine(projectRoot, "Services");
            var modelsRoot = Path.Combine(projectRoot, "Models");
            var wwwroot = Path.Combine(projectRoot, "wwwroot");
            var navIconsRoot = Path.Combine(wwwroot, "icons", "nav");

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
            await WriteTextAsync(Path.Combine(projectRoot, "Program.cs"), GenerateSolutionProgram(projectName, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(projectRoot, "_Imports.razor"), GenerateSolutionImports(projectName), cancellationToken);
            await WriteTextAsync(Path.Combine(projectRoot, "appsettings.json"), "{\n  \"Logging\": {\n    \"LogLevel\": {\n      \"Default\": \"Information\"\n    }\n  }\n}\n", cancellationToken);
            await WriteTextAsync(Path.Combine(componentsRoot, "App.razor"), GenerateSolutionAppRazor(), cancellationToken);
            await WriteTextAsync(Path.Combine(componentsRoot, "Routes.razor"), GenerateSolutionRoutesRazor(), cancellationToken);
            await WriteTextAsync(Path.Combine(componentsRoot, "GeneratedNavigation.razor"), GenerateSolutionNavigationRazor(isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "Index.razor"), GenerateSolutionIndexRazor(request, result, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "GeneratedDashboard.razor"), GenerateSolutionDashboardRazor(request, result, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(pagesRoot, "GeneratedKnowledgeTable.razor"), GenerateSolutionKnowledgeTableRazor(isOllamaLab), cancellationToken);
            await WriteTextAsync(
                Path.Combine(pagesRoot, isOllamaLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                GenerateSolutionDetailRazor(request, result, isOllamaLab),
                cancellationToken);
            if (isOllamaLab)
            {
                await WriteTextAsync(Path.Combine(pagesRoot, "ModelDownloads.razor"), GenerateOllamaModelDownloadsRazor(), cancellationToken);
                await WriteTextAsync(Path.Combine(pagesRoot, "Settings.razor"), GenerateOllamaSettingsRazor(), cancellationToken);
            }

            await WriteTextAsync(Path.Combine(servicesRoot, "GeneratedHealthSummaryService.cs"), GenerateSolutionService(projectName, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(modelsRoot, "GeneratedHealthCard.cs"), GenerateSolutionModel(projectName), cancellationToken);
            await WriteTextAsync(Path.Combine(wwwroot, "app.css"), GenerateSolutionCss(), cancellationToken);
            foreach (var icon in GenerateNavigationIconSvgs())
                await WriteTextAsync(Path.Combine(navIconsRoot, icon.FileName), icon.Svg, cancellationToken);

            await WriteTextAsync(Path.Combine(solutionRoot, "README.md"), GenerateSolutionReadme(projectName, request, result, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "PROJECT_INDEX.md"), GenerateSolutionProjectIndex(projectName, request, result, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "ARCHITECTURE.md"), GenerateSolutionArchitectureDoc(projectName, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "BUILD_AND_RUN.md"), GenerateSolutionBuildAndRunDoc(projectName, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, ".localgpt-generation.json"), GenerateLocalGptGenerationJson(projectName, request, result, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), GenerateSolutionManifest(projectName, solutionGuid, request, result, isOllamaLab), cancellationToken);
            ValidateSolutionArtifactContract(solutionRoot, projectName, isOllamaLab);

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

        private static string GenerateSolutionProgram(string projectName, bool isOllamaLab)
        {
            var ollamaRoutes = isOllamaLab
                ? """
                  app.MapGet("/api/version", () => new
                  {
                      version = "dotnet-lab-0.2",
                      source = "LocalGPT generated sandbox",
                      native_inference = false
                  });
                  app.MapGet("/api/tags", ([FromServices] GeneratedHealthSummaryService service) => new { models = service.GetOllamaTags() });
                  app.MapGet("/api/ps", ([FromServices] GeneratedHealthSummaryService service) => new { models = service.GetRunningModels() });
                  app.MapPost("/api/show", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.GetModelDetails(request));
                  app.MapPost("/api/pull", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreatePullPlan(request));
                  app.MapPost("/api/push", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("push", request.Model));
                  app.MapPost("/api/create", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("create", request.Model));
                  app.MapPost("/api/copy", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelCopyRequest request) => service.CreateCopyPlan(request));
                  app.MapDelete("/api/delete", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("delete", request.Model));
                  app.MapPost("/api/generate", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateGenerateResponse(request));
                  app.MapPost("/api/chat", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedChatRequest request) => service.CreateChatResponse(request));
                  app.MapPost("/api/embed", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
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

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapGet("/__generated/health", (GeneratedHealthSummaryService service) => service.GetCards());
            {{ollamaRoutes}}
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

        private static string GenerateSolutionNavigationRazor(bool isOllamaLab)
        {
            var labName = isOllamaLab ? "Ollama .NET Lab" : "LocalGPT Generation Lab";
            var catalogHref = isOllamaLab ? "/models" : "/knowledge";
            var catalogText = isOllamaLab ? "Model Catalog" : "Knowledge";
            var detailHref = isOllamaLab ? "/api-console" : "/implementation-plan";
            var detailText = isOllamaLab ? "API Console" : "Implementation Plan";
            var ollamaLinks = isOllamaLab
                ? """
                    <a href="/model-downloads">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Downloads</span>
                    </a>
                    <a href="/settings">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Settings</span>
                    </a>
                """
                : string.Empty;

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
                    {{ollamaLinks}}
                </nav>

                @code {
                    [Parameter]
                    public bool IsOllamaLab { get; set; }
                }
                """;
        }

        private static string GenerateSolutionIndexRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isOllamaLab)
        {
            var isOllamaLiteral = isOllamaLab ? "true" : "false";
            var title = isOllamaLab ? "Ollama-Compatible .NET Control Plane" : "LocalGPT Feature Generation Lab";
            var subtitle = isOllamaLab
                ? "A DevExpress Blazor shell for Ollama-style API routes, model cataloging, endpoint checks, and external runner boundaries."
                : "A LocalGPT/TacosPortalOpen-style sandbox for AI Council feature requests, implementation planning, knowledge-backed generation, and artifact review.";
            var primaryHref = isOllamaLab ? "/api-console" : "/implementation-plan";
            var primaryLabel = isOllamaLab ? "Open API console" : "Open implementation plan";
            var secondaryHref = isOllamaLab ? "/models" : "/knowledge";
            var secondaryLabel = isOllamaLab ? "Review model catalog" : "Review knowledge table";
            var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 500));
            var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 700));

            return $$"""
                @page "/"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>{{title}}</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsOllamaLab="{{isOllamaLiteral}}" />

                    <section class="generated-hero">
                        <div>
                            <p class="generated-kicker">{{(isOllamaLab ? "Ollama lab" : "LocalGPT lab")}}</p>
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
            bool isOllamaLab)
        {
            var isOllamaLiteral = isOllamaLab ? "true" : "false";
            var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 700));
            var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 900));
            var title = isOllamaLab ? "Ollama Runtime Dashboard" : "LocalGPT Generation Dashboard";
            var subtitle = isOllamaLab
                ? "Track API compatibility, model catalog readiness, runner adapter boundaries, and endpoint-test status."
                : "Track AI Council feature-generation readiness, knowledge grounding, artifact review, and integration safety.";
            return $$"""
            @page "/dashboard"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>{{title}}</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsOllamaLab="{{isOllamaLiteral}}" />

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

        private static string GenerateSolutionKnowledgeTableRazor(bool isOllamaLab)
        {
            if (isOllamaLab)
            {
                return """
                @page "/models"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>Ollama .NET Lab Catalog</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsOllamaLab="true" />

                    <h1>Ollama .NET Lab Catalog</h1>
                    <p class="generated-muted">Model rows are compatibility records for the .NET control-plane lab. They are not proof of native inference.</p>

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
                <GeneratedNavigation IsOllamaLab="false" />

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

        private static string GenerateSolutionDetailRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isOllamaLab)
        {
            if (isOllamaLab)
            {
                return """
                @page "/api-console"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>Ollama API Console</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsOllamaLab="true" />

                    <section class="generated-header">
                        <div>
                            <h1>Ollama API Console</h1>
                            <p>Selected Ollama-style endpoints are shown as .NET route stubs. Native model execution still belongs behind an approved runner adapter.</p>
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
                        <h2>Non-Inference Generate Stub</h2>
                        <pre class="generated-code">POST /api/generate
                {
                  "model": "dotnet-lab-stub",
                  "prompt": "Hello"
                }

                Response:
                {
                  "response": "This .NET lab does not implement native inference.",
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
                    <GeneratedNavigation IsOllamaLab="false" />

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

        private static string GenerateOllamaModelDownloadsRazor() =>
            """
            @page "/model-downloads"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Model Downloads</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsOllamaLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Model Downloads</h1>
                        <p>Plan Ollama-style pull operations without claiming ownership of model binaries or native runner behavior.</p>
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

        private static string GenerateOllamaSettingsRazor() =>
            """
            @page "/settings"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Ollama Lab Settings</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsOllamaLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Ollama Lab Settings</h1>
                        <p>Configuration is shown as safe generated defaults. Real persistence should be added through backend services and EF/SQLite after user approval.</p>
                    </div>
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generated Runtime Profile" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Base URI" ColSpanMd="6">
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
                GeneratedOllamaSettings LabSettings { get; set; } = new();
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

        private static string GenerateSolutionService(string projectName, bool isOllamaLab)
        {
            var cards = isOllamaLab
                ? """
                          new("REST API Shell", "Prototype", "Map version, tags, ps, show, pull, push, create, copy, delete, generate, chat, and embed stubs.", "This mirrors Ollama API route families without claiming native inference."),
                          new("Model Catalog", "SourceBacked", "Represent model names, tags, details, download candidates, and runner status in .NET models.", "Model file ownership stays outside the lab until a real backend is approved."),
                          new("Native Runner", "Not Implemented", "Attach or build a real inference backend before claiming Ollama replacement behavior.", "Ollama relies on native GGML/GPU runner paths, CMake payloads, and hardware-specific backends."),
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

            var endpoints = isOllamaLab
                ? """
                          new("GET", "/api/version", "Return a compact Ollama-style version document.", "Safe pure .NET response."),
                          new("GET", "/api/tags", "Return model catalog rows shaped like Ollama tags.", "Catalog only; no model file ownership implied."),
                          new("GET", "/api/ps", "Return currently loaded model rows for runner-status UI.", "Stubbed; no native runner session is owned."),
                          new("POST", "/api/show", "Return model metadata, parameters, template, and details.", "Source-shaped but generated data only."),
                          new("POST", "/api/pull", "Return a safe model-download plan.", "Does not download model binaries without a real adapter."),
                          new("POST", "/api/push", "Return a registry-upload plan.", "No registry credentials or upload path included."),
                          new("POST", "/api/create", "Return a model-create plan.", "No Modelfile build happens in this sandbox."),
                          new("POST", "/api/copy", "Return a model-copy plan.", "No local blob mutation happens."),
                          new("DELETE", "/api/delete", "Return a model-delete plan.", "No file deletion happens."),
                          new("POST", "/api/generate", "Return a deterministic non-inference response for UI/API plumbing tests.", "Must attach a real runner before claiming generation."),
                          new("POST", "/api/chat", "Return a deterministic chat response.", "No token generation or context cache is implemented."),
                          new("POST", "/api/embed", "Return a tiny deterministic vector.", "Not a real embedding model.")
                  """
                : """
                          new("1", "Backend service", "Create the durable service and data model first.", "Build and test before UI integration."),
                          new("2", "Blazor page", "Add a routable Razor page with DevExpress controls and navigation.", "Review against LocalGPT/TacosPortalOpen patterns."),
                          new("3", "SQLite knowledge", "Persist decisions, logs, and generated artifacts as approved or unverified.", "User approval decides trust state."),
                          new("4", "Artifact download", "Expose generated files through safe HTTP download routes.", "No binary blobs inside chat messages."),
                          new("5", "Frontend smoke", "Exercise the generated workflow like a user in WebView2.", "Do not rely only on backend APIs.")
                  """;

            return $$"""
            using {{projectName}}.Models;

            namespace {{projectName}}.Services;

            /// <summary>
            /// Provides deterministic health cards for the generated LocalGPT sandbox solution.
            /// </summary>
            public sealed class GeneratedHealthSummaryService
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
                /// Returns sample model entries for compatibility UI and API stub responses.
                /// </summary>
                public IReadOnlyList<GeneratedModelCard> GetModelCatalog()
                {
                    return
                    [
                        new("gpt-oss:20b", "External Ollama model candidate", 0, false),
                        new("qwen3-coder:30b", "External Ollama model candidate", 0, false),
                        new("dotnet-lab-stub:latest", "API shell only", 0, false),
                        new("external-runner-adapter:planned", "Requires real native or external inference backend", 0, false)
                    ];
                }

                /// <summary>
                /// Returns Ollama-style local model rows for the /api/tags route.
                /// </summary>
                public IReadOnlyList<GeneratedOllamaModelTag> GetOllamaTags()
                {
                    return
                    [
                        new("gpt-oss:20b", "gpt-oss", "20B", "Q4_K_M", 0),
                        new("qwen3-coder:30b", "qwen", "30B", "Q4_K_M", 0),
                        new("dotnet-lab-stub:latest", "generated", "0B", "none", 0)
                    ];
                }

                /// <summary>
                /// Returns the model rows shown as currently loaded by the generated /api/ps route.
                /// </summary>
                public IReadOnlyList<GeneratedOllamaModelTag> GetRunningModels()
                {
                    return [new("dotnet-lab-stub:latest", "generated", "0B", "none", 0)];
                }

                /// <summary>
                /// Returns model metadata shaped like Ollama's show route.
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
                        details = new GeneratedOllamaModelDetails("gguf", "generated", "0B", "none"),
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
                        new("gpt-oss:20b", "LocalGPT debugging and balanced reasoning", "/api/pull", "Pull only when GPU/VRAM policy allows it."),
                        new("gemma3:27b", "Longer general review and writing", "/api/pull", "Use one model at a time on 24 GB VRAM."),
                        new("qwen3-coder:30b", "Code review and larger code-generation tests", "/api/pull", "Prefer CPU or reduced GPU layers after driver instability."),
                        new("deepseek-r1:8b", "Small reasoning checks", "/api/pull", "May spend short budgets on thinking.")
                    ];
                }

                /// <summary>
                /// Returns generated runtime settings for the settings page.
                /// </summary>
                public GeneratedOllamaSettings GetSettings()
                {
                    return new GeneratedOllamaSettings
                    {
                        BaseUri = "http://127.0.0.1:11434",
                        DefaultModel = "gpt-oss:20b",
                        KeepAlive = "0s",
                        ContextTokens = 2048,
                        GpuLayers = 20,
                        NativeRunnerAttached = false,
                        AllowPullPlanning = true
                    };
                }

                /// <summary>
                /// Builds the settings summary shown in the generated settings page.
                /// </summary>
                public string BuildSettingsSummary()
                {
                    var settings = GetSettings();
                    return $"Base URI: {settings.BaseUri}\nDefault model: {settings.DefaultModel}\n" +
                        $"Context tokens: {settings.ContextTokens}\nGPU layers: {settings.GpuLayers}\n" +
                        "Native inference is not implemented in this generated lab.";
                }

                /// <summary>
                /// Creates a safe model-pull plan without downloading model files.
                /// </summary>
                public GeneratedOllamaOperation CreatePullPlan(GeneratedModelActionRequest request)
                {
                    return new GeneratedOllamaOperation(
                        "planned",
                        NormalizeModel(request.Model),
                        "/api/pull",
                        true,
                        "This response mirrors Ollama pull progress shape but does not download model binaries.");
                }

                /// <summary>
                /// Creates a safe non-mutating operation response for registry and model-management routes.
                /// </summary>
                public GeneratedOllamaOperation CreateOperation(string operation, string? model)
                {
                    return new GeneratedOllamaOperation(
                        "planned",
                        NormalizeModel(model),
                        $"/api/{operation}",
                        true,
                        $"The generated lab records a {operation} plan but does not mutate model storage.");
                }

                /// <summary>
                /// Creates a safe copy plan for the /api/copy route.
                /// </summary>
                public GeneratedOllamaOperation CreateCopyPlan(GeneratedModelCopyRequest request)
                {
                    var from = NormalizeModel(request.Source);
                    var to = NormalizeModel(request.Destination);
                    return new GeneratedOllamaOperation(
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
                        response = "This .NET lab does not implement native inference. Attach a real runner before claiming Ollama replacement behavior.",
                        done = true
                    };
                }

                /// <summary>
                /// Creates a deterministic non-inference chat response.
                /// </summary>
                public object CreateChatResponse(GeneratedChatRequest request)
                {
                    return new
                    {
                        model = NormalizeModel(request.Model),
                        created_at = DateTimeOffset.UtcNow,
                        message = new GeneratedChatMessage("assistant", "Generated lab response only. No native Ollama runner is attached."),
                        done = true
                    };
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
                        ? "dotnet-lab-stub:latest"
                        : model.Trim();
                }
            }
            """;
        }

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
            /// Describes one model or adapter row in the generated Ollama .NET lab.
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
            /// Describes one Ollama-style model row returned by generated catalog routes.
            /// </summary>
            public sealed class GeneratedOllamaModelTag
            {
                /// <summary>
                /// Creates a generated Ollama-style model row.
                /// </summary>
                public GeneratedOllamaModelTag(
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
                    Details = new GeneratedOllamaModelDetails("gguf", family, parameterSize, quantizationLevel);
                }

                /// <summary>
                /// Gets the legacy Ollama model name field.
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
                public GeneratedOllamaModelDetails Details { get; }
            }

            /// <summary>
            /// Describes generated Ollama-style model details.
            /// </summary>
            public sealed class GeneratedOllamaModelDetails
            {
                /// <summary>
                /// Creates generated model details.
                /// </summary>
                public GeneratedOllamaModelDetails(
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
            /// Represents a generated Ollama action request.
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
            }

            /// <summary>
            /// Represents a generated Ollama copy request.
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
            /// Represents a generated Ollama chat request.
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
            }

            /// <summary>
            /// Represents a generated Ollama chat message.
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
            /// Describes a generated model download planning row.
            /// </summary>
            public sealed class GeneratedModelDownloadCandidate
            {
                /// <summary>
                /// Creates a generated download candidate.
                /// </summary>
                public GeneratedModelDownloadCandidate(
                    string name,
                    string recommendedFor,
                    string downloadRoute,
                    string safetyNote)
                {
                    Name = name;
                    RecommendedFor = recommendedFor;
                    DownloadRoute = downloadRoute;
                    SafetyNote = safetyNote;
                }

                /// <summary>
                /// Gets the candidate model name.
                /// </summary>
                public string Name { get; }

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
            /// Holds generated Ollama lab settings shown in the DevExpress form.
            /// </summary>
            public sealed class GeneratedOllamaSettings
            {
                /// <summary>
                /// Gets or sets the external Ollama base URI.
                /// </summary>
                public string BaseUri { get; set; } = "http://localhost:11434";

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
            /// Describes a generated Ollama-compatible operation result.
            /// </summary>
            public sealed class GeneratedOllamaOperation
            {
                /// <summary>
                /// Creates a generated operation result.
                /// </summary>
                public GeneratedOllamaOperation(
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
            bool isOllamaLab)
        {
            var description = isOllamaLab
                ? "Generated by LocalGPT as an Ollama-inspired .NET 10 ASP.NET Core and DevExpress Blazor control-plane lab. Native inference is intentionally stubbed."
                : "Generated by LocalGPT as a whole-solution AI Council artifact.";

            var notes = isOllamaLab
                ? """
                  ## Ollama .NET Lab Scope

                  This prototype can demonstrate selected Ollama-style HTTP routes, model catalog UX, health cards, and endpoint testing in .NET/Blazor.

                  It does not replace Ollama's native runner, GGML/GPU backend, model loader, CMake/native build stack, or hardware-specific inference paths. Attach a real backend before calling it an Ollama replacement.
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
            bool isOllamaLab)
        {
            var projectKind = isOllamaLab ? "dotnet_service" : "localgpt_feature";
            var purpose = isOllamaLab
                ? "Prototype an Ollama-shaped .NET/Blazor control plane without claiming native model inference."
                : "Prototype a LocalGPT/TacosPortalOpen-style AI Council feature workspace with reviewable Blazor pages.";
            var catalogPage = isOllamaLab ? "GeneratedKnowledgeTable.razor route `/models`" : "GeneratedKnowledgeTable.razor route `/knowledge`";
            var detailPage = isOllamaLab ? "ApiConsole.razor route `/api-console`" : "ImplementationPlan.razor route `/implementation-plan`";
            var ollamaExpectedEntryPoints = isOllamaLab
                ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor"
                """
                : string.Empty;
            var ollamaEntryPoints = isOllamaLab
                ? $$"""
            - `src/{{projectName}}/Components/Pages/ModelDownloads.razor` - DevExpress model pull planning page.
            - `src/{{projectName}}/Components/Pages/Settings.razor` - Ollama lab settings and runner-boundary page.
            """
                : string.Empty;
            var ollamaGeneratedFiles = isOllamaLab
                ? $$"""
            | `src/{{projectName}}/Components/Pages/ModelDownloads.razor` | DevExpress UI for Ollama-style pull planning and download guidance. |
            | `src/{{projectName}}/Components/Pages/Settings.razor` | DevExpress settings page for external Ollama URI, context, and native-runner boundaries. |
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
              "needs_native_commands": {{(isOllamaLab ? "true" : "false")}},
              "needs_index": true,
              "needs_version_resolver": false,
              "expected_entrypoints": [
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor"{{ollamaExpectedEntryPoints}}
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
            {{ollamaEntryPoints}}

            ## Generated Files

            | File | Why it exists |
            | --- | --- |
            | `{{projectName}}.sln` | Visual Studio and CLI solution entry point. |
            | `src/{{projectName}}/{{projectName}}.csproj` | .NET 10 Blazor Web App project with DevExpress dependency. |
            | `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs` | Typed demo service instead of Razor-only fake data. |
            | `src/{{projectName}}/Models/GeneratedHealthCard.cs` | Shared model records for grids/catalog rows. |
            {{ollamaGeneratedFiles}}
            | `src/{{projectName}}/wwwroot/app.css` | Local styling for the generated shell. |
            | `src/{{projectName}}/wwwroot/icons/nav/*-line.svg` | Default navigation icon style. |
            | `src/{{projectName}}/wwwroot/icons/nav/*-solid.svg` | Hover/focus navigation icon style. |
            | `PROJECT_INDEX.md` | Required generated-project map and archetype declaration. |
            | `ARCHITECTURE.md` | Explains why this artifact differs from other project types. |
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

        private static string GenerateSolutionArchitectureDoc(string projectName, bool isOllamaLab)
        {
            var title = isOllamaLab ? "Ollama .NET Lab Architecture" : "LocalGPT Feature Lab Architecture";
            var contrast = isOllamaLab
                ? "This is not a LocalGPT feature page clone. It is an API-control-plane lab with endpoint cataloging, model rows, and explicit runner boundaries."
                : "This is not an Ollama replacement lab. It is a LocalGPT/TacosPortalOpen-style feature sandbox with council grounding, implementation steps, and approval gates.";
            var backendBoundary = isOllamaLab
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

            ## Files To Review First

            1. `PROJECT_INDEX.md`
            2. `.localgpt-generation.json`
            3. `src/{{projectName}}/Program.cs`
            4. `src/{{projectName}}/Components/Pages/Index.razor`
            5. `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs`
            """;
        }

        private static string GenerateSolutionBuildAndRunDoc(string projectName, bool isOllamaLab)
        {
            var smokeRoute = isOllamaLab
                ? "Open `/api-console`, `/model-downloads`, and `/settings`; then call `/api/version`, `/api/tags`, and `/api/chat` to verify the control-plane routes."
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
            bool isOllamaLab)
        {
            var projectKind = isOllamaLab ? "dotnet_service" : "localgpt_feature";
            var targetPlatform = isOllamaLab
                ? "dotnet10_aspnetcore_devexpress_blazor_ollama_control_plane"
                : "dotnet10_aspnetcore_devexpress_blazor_localgpt_feature";
            var detailPage = isOllamaLab ? "ApiConsole.razor" : "ImplementationPlan.razor";
            var validationNotes = isOllamaLab
                ? "Required docs, manifest, navigation, paired nav icons, index, dashboard, model catalog, API console, model-download, and settings files were present before zipping."
                : "Required docs, manifest, navigation, paired nav icons, index, dashboard, knowledge table, and implementation-plan files were present before zipping.";
            var ollamaExpectedEntryPoints = isOllamaLab
                ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor"
                """
                : string.Empty;
            var ollamaGeneratedFiles = isOllamaLab
                ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor"
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
              "needs_native_commands": {{(isOllamaLab ? "true" : "false")}},
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
                "src/{{projectName}}/Components/Pages/{{detailPage}}"{{ollamaExpectedEntryPoints}}
              ],
              "generated_files": [
                "{{projectName}}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
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
                "src/{{projectName}}/Components/Pages/{{detailPage}}"{{ollamaGeneratedFiles}},
                "src/{{projectName}}/Services/GeneratedHealthSummaryService.cs",
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
              "archetype_difference": "{{(isOllamaLab ? "Ollama lab includes API route stubs, model catalog, downloads, and settings; native inference remains explicitly out of scope." : "LocalGPT feature sandbox includes implementation-plan and knowledge-table pages rather than Ollama API compatibility workflows.")}}"
            }
            """;
        }

        private static string GenerateSolutionManifest(
            string projectName,
            string solutionGuid,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isOllamaLab)
        {
            var sourceGoal = isOllamaLab
                ? "Ollama-inspired .NET 10 ASP.NET Core and DevExpress Blazor control-plane lab with native inference stubbed"
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
            var text = $"{prompt} {finalAnswer}";
            return MinecraftPattern().IsMatch(text) && DatapackPattern().IsMatch(text);
        }

        private static bool IsMinecraftSkeletonMatrixArtifactTarget(string prompt, string finalAnswer)
        {
            var text = $"{prompt} {finalAnswer}";
            return MinecraftPattern().IsMatch(text) && MinecraftSkeletonMatrixPattern().IsMatch(text);
        }

        private static string ExtractMinecraftVersion(string text)
        {
            var match = MinecraftVersionPattern().Match(text);
            return match.Success ? match.Groups["version"].Value : "1.21.4";
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
            return WholeSolutionPattern().IsMatch(text) || IsOllamaDotNetExperimentTarget(prompt, finalAnswer);
        }

        private static bool IsOllamaDotNetExperimentTarget(string prompt, string finalAnswer)
        {
            return OllamaDotNetExperimentPattern().IsMatch($"{prompt} {finalAnswer}");
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

        private static void ValidateSolutionArtifactContract(string solutionRoot, string projectName, bool isOllamaLab)
        {
            var requiredFiles = new List<string>
            {
                $"{projectName}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
                "BUILD_AND_RUN.md",
                ".localgpt-generation.json",
                "LocalGPT.GenerationManifest.json",
                Path.Combine("src", projectName, $"{projectName}.csproj"),
                Path.Combine("src", projectName, "Program.cs"),
                Path.Combine("src", projectName, "Components", "GeneratedNavigation.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "Index.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedDashboard.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedKnowledgeTable.razor"),
                Path.Combine("src", projectName, "Components", "Pages", isOllamaLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                Path.Combine("src", projectName, "Services", "GeneratedHealthSummaryService.cs"),
                Path.Combine("src", projectName, "Models", "GeneratedHealthCard.cs"),
                Path.Combine("src", projectName, "wwwroot", "app.css"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-solid.svg")
            };

            if (isOllamaLab)
            {
                requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "ModelDownloads.razor"));
                requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Settings.razor"));
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

        [GeneratedRegex("(?<!\\d)(?<version>1\\.\\d{1,2}(?:\\.\\d{1,2})?)(?!\\d)", RegexOptions.CultureInvariant)]
        private static partial Regex MinecraftVersionPattern();

        [GeneratedRegex("(?m)^\\s*/", RegexOptions.CultureInvariant)]
        private static partial Regex LeadingSlashCommandPattern();

        [GeneratedRegex("\\bdata\\s+remove\\s+storage\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RootStorageRemovePattern();

        [GeneratedRegex("(frontend|razor|devexpress|dxaichat|css|javascript)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex FrontendPattern();

        [GeneratedRegex("(whole solution|full solution|entire solution|solution zip|project zip|\\.sln|\\.csproj|all source files|tacosportalopen|localgpt|whole ollama|ollama dotnet|ollama \\.net)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex WholeSolutionPattern();

        [GeneratedRegex("(ollama).*(dotnet|\\.net|blazor|devexpress|aspnet|asp\\.net)|(dotnet|\\.net|blazor|devexpress|aspnet|asp\\.net).*(ollama)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex OllamaDotNetExperimentPattern();

        [GeneratedRegex("(log|logger|diagnostic|error|warning|telemetry)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex LoggingPattern();

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();

        private sealed record MinecraftDatapackArtifactIdentity(
            string ProjectName,
            string ModId,
            string PackageName,
            string DisplayName);
    }
}
