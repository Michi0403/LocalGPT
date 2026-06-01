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
            var fileName = $"council-feature-example-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{result.RunId:N}.cs";
            var path = Path.Combine(ArtifactRoot, fileName);
            var source = GenerateCodeDomExample(request, result, targetArea);

            await File.WriteAllTextAsync(path, source, cancellationToken);
            logger.LogInformation("Wrote council implementation example artifact to {Path}", path);

            var artifacts = new List<CouncilArtifact>
            {
                new CouncilArtifact
                {
                    Name = fileName,
                    Kind = "CodeDOM C# example",
                    FilePath = path,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(fileName)}",
                    Summary = $"Generated starter example for {targetArea} implementation ideas."
                }
            };

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

        private static string TrimForCodeComment(string text, int maxLength)
        {
            var normalized = WhitespacePattern().Replace(text, " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        [GeneratedRegex("(devexpress|richedit|pdfviewer|pivot|report|xtrareport|office|docx|xlsx|pdf export|spreadsheet|document generation)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DevExpressDocumentPattern();

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
