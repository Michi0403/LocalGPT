using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
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
    /// Coordinates council runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
    /// <summary>Executes the build project summary operation.</summary>
        /// <summary>
        /// Builds project summary as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
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

                var read = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await using var configuredReadAsyncDisposal = read.ConfigureAwait(false);
                var write = File.Open(destination, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var configuredWriteAsyncDisposal = write.ConfigureAwait(false);
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
        /// Retrieves search roots as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
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
                var path = LocalGptApplicationDataPaths.ResolveUserPath("runtime",
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

    }
}
