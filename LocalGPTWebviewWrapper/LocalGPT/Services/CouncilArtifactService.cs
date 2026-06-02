using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.CSharp;

namespace LocalGPT.Services
{
    public partial class CouncilArtifactService(ILogger<CouncilArtifactService> logger) : ICouncilArtifactService
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

            if (Directory.Exists(solutionRoot))
                Directory.Delete(solutionRoot, recursive: true);

            Directory.CreateDirectory(pagesRoot);
            Directory.CreateDirectory(servicesRoot);
            Directory.CreateDirectory(modelsRoot);
            Directory.CreateDirectory(wwwroot);

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
            await WriteTextAsync(Path.Combine(servicesRoot, "GeneratedHealthSummaryService.cs"), GenerateSolutionService(projectName, isOllamaLab), cancellationToken);
            await WriteTextAsync(Path.Combine(modelsRoot, "GeneratedHealthCard.cs"), GenerateSolutionModel(projectName), cancellationToken);
            await WriteTextAsync(Path.Combine(wwwroot, "app.css"), GenerateSolutionCss(), cancellationToken);
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
                  app.MapGet("/api/version", () => new { version = "dotnet-lab-0.1", source = "LocalGPT generated sandbox" });
                  app.MapGet("/api/tags", (GeneratedHealthSummaryService service) => new { models = service.GetModelCatalog() });
                  app.MapPost("/api/generate", () => Results.Json(new
                  {
                      model = "dotnet-lab-stub",
                      created_at = DateTimeOffset.UtcNow,
                      response = "This .NET lab does not implement native inference. Attach a real runner before claiming Ollama replacement behavior.",
                      done = true
                  }));
                  """
                : string.Empty;

            return $$"""
            using DevExpress.Blazor;
            using {{projectName}}.Components;
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

            return $$"""
                <nav class="generated-nav" aria-label="{{labName}} navigation">
                    <a class="generated-brand" href="/">{{labName}}</a>
                    <a href="/dashboard">Dashboard</a>
                    <a href="{{catalogHref}}">{{catalogText}}</a>
                    <a href="{{detailHref}}">{{detailText}}</a>
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

        private static string GenerateSolutionService(string projectName, bool isOllamaLab)
        {
            var cards = isOllamaLab
                ? """
                          new("REST API Shell", "Prototype", "Map /api/version, /api/tags, and a non-inference /api/generate stub.", "This mirrors selected Ollama API shapes without claiming native inference."),
                          new("Model Catalog", "SourceBacked", "Represent model names, tags, sizes, and runner status in .NET models.", "The inspected Ollama source has api/model/manifest concerns that should become typed C# models."),
                          new("Native Runner", "Not Implemented", "Attach or build a real inference backend before claiming Ollama replacement behavior.", "Ollama relies on native GGML/GPU runner paths, CMake payloads, and hardware-specific backends."),
                          new("Endpoint Console", "Ready", "Expose API compatibility docs, request examples, and adapter status in the UI.", "This makes the lab recognizably Ollama-shaped without pretending to run models."),
                          new("DevExpress UI", "Ready", "Use grids/forms for model inventory, compatibility notes, logs, and endpoint tests.", "This is the realistic Blazor/DevExpress value of the experiment.")
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
                          new("POST", "/api/generate", "Return a deterministic non-inference response for UI/API plumbing tests.", "Must attach a real runner before claiming generation."),
                          new("POST", "/api/show", "Future route for model metadata and manifest details.", "Needs verified model manifest adapter.")
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
                /// Returns routes, steps, or boundaries shown by the generated detail page.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetEndpointCatalog()
                {
                    return
                    [
                {{endpoints}}
                    ];
                }
            }
            """;
        }

        private static string GenerateSolutionModel(string projectName) =>
            $$"""
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
                color: #384252;
                text-decoration: none;
                font-weight: 600;
            }

            .generated-nav .generated-brand {
                margin-right: auto;
                color: #172033;
                font-weight: 700;
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
            - `PROJECT_INDEX.md`
            - `ARCHITECTURE.md`
            - `BUILD_AND_RUN.md`
            - `.localgpt-generation.json`
            - `LocalGPT.GenerationManifest.json`

            {{notes}}

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
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor"
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

            ## Generated Files

            | File | Why it exists |
            | --- | --- |
            | `{{projectName}}.sln` | Visual Studio and CLI solution entry point. |
            | `src/{{projectName}}/{{projectName}}.csproj` | .NET 10 Blazor Web App project with DevExpress dependency. |
            | `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs` | Typed demo service instead of Razor-only fake data. |
            | `src/{{projectName}}/Models/GeneratedHealthCard.cs` | Shared model records for grids/catalog rows. |
            | `src/{{projectName}}/wwwroot/app.css` | Local styling for the generated shell. |
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
            var smokeRoute = isOllamaLab ? "Open `/api-console` and verify endpoint rows are visible." : "Open `/implementation-plan` and verify implementation steps are visible.";
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
            var detailPage = isOllamaLab ? "ApiConsole.razor" : "ImplementationPlan.razor";
            var validationNotes = isOllamaLab
                ? "Required docs, manifest, navigation, index, dashboard, model catalog, and API console files were present before zipping."
                : "Required docs, manifest, navigation, index, dashboard, knowledge table, and implementation-plan files were present before zipping.";

            return $$"""
            {
              "schema": "localgpt-generation-contract/v1",
              "project_kind": "{{projectKind}}",
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
              "expected_entrypoints": [
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/GeneratedNavigation.razor",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/{{detailPage}}"
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
                "src/{{projectName}}/Components/Pages/{{detailPage}}",
                "src/{{projectName}}/Services/GeneratedHealthSummaryService.cs",
                "src/{{projectName}}/Models/GeneratedHealthCard.cs",
                "src/{{projectName}}/wwwroot/app.css"
              ],
              "safety": "Sandbox artifact only. Integration requires explicit user approval."
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
            var requiredFiles = new[]
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
                Path.Combine("src", projectName, "wwwroot", "app.css")
            };

            var missing = requiredFiles
                .Where(relativePath => !File.Exists(Path.Combine(solutionRoot, relativePath)))
                .ToArray();

            if (missing.Length > 0)
                throw new InvalidOperationException($"Generated solution artifact is missing required files: {string.Join(", ", missing)}");
        }

        [GeneratedRegex("(devexpress|richedit|pdfviewer|pivot|report|xtrareport|office|docx|xlsx|pdf export|spreadsheet|document generation)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DevExpressDocumentPattern();

        [GeneratedRegex("(blazor|razor|component|page|dxgrid|dxformlayout|dxbutton|dxmemo|dxtextbox|dxcombobox|dxaichat|devexpress blazor|interactive(server|webassembly|auto))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex BlazorFrontendPattern();

        [GeneratedRegex("(dotnet|\\.net|aspnet|asp\\.net|blazor|c#|codedom|entityframework|sqlite|winui|webview2)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DotNetPattern();

        [GeneratedRegex("(minecraft|fabric|neoforge|paper|datapack|gradle|java)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex MinecraftPattern();

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
    }
}
