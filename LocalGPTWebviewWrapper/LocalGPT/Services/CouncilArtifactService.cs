using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
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

            return artifacts;
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

        [GeneratedRegex("(log|logger|diagnostic|error|warning|telemetry)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex LoggingPattern();

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();
    }
}
