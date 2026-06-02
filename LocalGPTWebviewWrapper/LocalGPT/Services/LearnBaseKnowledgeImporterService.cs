using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public sealed partial class LearnBaseKnowledgeImporterService(
        ICouncilKnowledgeService knowledgeService,
        ILogger<LearnBaseKnowledgeImporterService> logger) : ILearnBaseKnowledgeImporterService
    {
        private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".vs",
            ".idea",
            "bin",
            "obj",
            "node_modules",
            "packages",
            ".venv",
            "__pycache__",
            ".gradle",
            ".mypy_cache",
            ".pytest_cache",
            "build",
            "dist",
            "publish",
            "AppPackages"
        };

        private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll",
            ".exe",
            ".pdb",
            ".msi",
            ".pfx",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".ico",
            ".db",
            ".sqlite",
            ".sqlite3",
            ".zip",
            ".nupkg"
        };

        private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".sln",
            ".razor",
            ".xaml",
            ".json",
            ".xml",
            ".py",
            ".js",
            ".ts",
            ".html",
            ".css",
            ".sql",
            ".md",
            ".yml",
            ".yaml",
            ".ps1",
            ".props",
            ".targets",
            ".config",
            ".resx",
            ".mdx",
            ".go",
            ".mod",
            ".sum",
            ".proto",
            ".toml",
            ".ini",
            ".cmake",
            ".sh",
            ".bat",
            ".cmd",
            ".gotmpl",
            ".tmpl"
        };

        public async Task<LearnBaseImportResult> ImportAsync(
            LearnBaseImportRequest request,
            CancellationToken cancellationToken = default)
        {
            var rootPath = string.IsNullOrWhiteSpace(request.RootPath)
                ? @"C:\tmpselectedcodexlearnbaseforlocalgpt"
                : request.RootPath.Trim();
            var result = new LearnBaseImportResult { RootPath = rootPath };

            if (!Directory.Exists(rootPath))
            {
                result.Warnings.Add($"Learn-base root was not found: {rootPath}");
                return result;
            }

            await knowledgeService.EnsureCreatedAsync(cancellationToken);
            if (LooksLikeWindowsDevDocsRoot(rootPath))
                await ImportWindowsDevDocsCorpusAsync(rootPath, request, result, cancellationToken);

            var projectDirectories = BuildImportDirectories(rootPath, Math.Clamp(request.MaxProjects, 1, 120))
                .ToArray();

            foreach (var projectDirectory in projectDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var summary = BuildProjectSummary(rootPath, projectDirectory);
                    if (request.SaveToKnowledge)
                    {
                        var entry = await knowledgeService.SaveEntryAsync(ToKnowledgeEntry(summary), cancellationToken);
                        summary.KnowledgeEntryId = entry.Id;
                        result.SavedKnowledgeCount++;
                    }

                    result.Projects.Add(summary);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    var name = Path.GetFileName(projectDirectory);
                    result.Warnings.Add($"Could not scan {name}: {ex.Message}");
                    logger.LogWarning(ex, "Could not import learn-base project {ProjectDirectory}.", projectDirectory);
                }
            }

            result.ProjectCount = result.Projects.Count;
            return result;
        }

        private async Task ImportWindowsDevDocsCorpusAsync(
            string rootPath,
            LearnBaseImportRequest request,
            LearnBaseImportResult result,
            CancellationToken cancellationToken)
        {
            var markdownFiles = EnumerateUsefulFiles(rootPath)
                .Where(file => file.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                .Take(6000)
                .ToArray();
            if (markdownFiles.Length == 0)
                return;

            foreach (var entry in BuildWindowsDevDocsEntries(rootPath, markdownFiles))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Guid? knowledgeEntryId = null;
                if (request.SaveToKnowledge)
                {
                    var saved = await knowledgeService.SaveEntryAsync(entry, cancellationToken);
                    knowledgeEntryId = saved.Id;
                    result.SavedKnowledgeCount++;
                }

                result.Projects.Add(new LearnBaseProjectSummary
                {
                    Name = entry.Topic,
                    SourcePath = rootPath,
                    Architecture = "Windows developer docs corpus; DocFX/Microsoft Learn authoring; Windows app platform; deployment/support/design guidance",
                    ProtocolsAndComponents = "DocFX; Microsoft Learn markdown; Windows App SDK; WinUI; WebView2; MSIX; winget; Terminal; Dev Drive; PowerToys; Arm64; accessibility",
                    TargetFrameworks = "Documentation corpus, not a compiled project",
                    PackageReferences = "none",
                    ImportantFiles = entry.HelpfulSources,
                    SourceFileCount = markdownFiles.Length,
                    BinaryFileCount = 0,
                    KnowledgeEntryId = knowledgeEntryId
                });
            }
        }

        private static IReadOnlyList<CouncilKnowledgeEntry> BuildWindowsDevDocsEntries(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles)
        {
            var now = DateTime.UtcNow;
            var docfxSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, "docfx", "metadata", "toc", "index", "authoring");
            var platformSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, "windows-app-sdk", "winui", "webview2", "msix", "desktop");
            var supportSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, "developer-mode", "dev-drive", "winget", "terminal", "arm64");
            var designSamples = BuildWindowsDocsPathSamples(rootPath, markdownFiles, "design", "accessibility", "navigation", "layout", "typography");
            var frontMatterCount = markdownFiles
                .Take(800)
                .Select(ReadSmallText)
                .Count(text => text.TrimStart().StartsWith("---", StringComparison.Ordinal));

            return
            [
                new CouncilKnowledgeEntry
                {
                    Id = CreateStableGuid($"windows-dev-docs|docfx|{rootPath}"),
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
                    Id = CreateStableGuid($"windows-dev-docs|platform|{rootPath}"),
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
                    Id = CreateStableGuid($"windows-dev-docs|support|{rootPath}"),
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
                    Id = CreateStableGuid($"windows-dev-docs|design|{rootPath}"),
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
            ];
        }

        private static string BuildWindowsDocsPathSamples(
            string rootPath,
            IReadOnlyList<FileInfo> markdownFiles,
            params string[] needles)
        {
            var matches = markdownFiles
                .Where(file =>
                {
                    var relative = Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/');
                    return needles.Any(needle => relative.Contains(needle, StringComparison.OrdinalIgnoreCase));
                })
                .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                .Take(16)
                .Select(file => "- " + RedactSensitiveName(Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/')))
                .ToArray();

            if (matches.Length > 0)
                return string.Join("\n", matches);

            return string.Join(
                "\n",
                markdownFiles
                    .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                    .Take(16)
                    .Select(file => "- " + RedactSensitiveName(Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/'))));
        }

        private static LearnBaseProjectSummary BuildProjectSummary(string rootPath, string projectDirectory)
        {
            var files = EnumerateUsefulFiles(projectDirectory).Take(1600).ToArray();
            var sourceFiles = files
                .Where(file => SourceExtensions.Contains(file.Extension))
                .ToArray();
            var binaryCount = files.Count(file => BinaryExtensions.Contains(file.Extension));
            var textSamples = sourceFiles
                .Where(file => file.Length is > 0 and < 256_000)
                .Take(80)
                .Select(ReadSmallText)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();
            var pathSamples = sourceFiles
                .Take(700)
                .Select(file => Path.GetRelativePath(projectDirectory, file.FullName).Replace('\\', '/'));
            var combined = string.Join("\n", textSamples.Concat(pathSamples));

            var summary = new LearnBaseProjectSummary
            {
                Name = RedactSensitiveName(Path.GetFileName(projectDirectory)),
                SourcePath = projectDirectory,
                SourceFileCount = sourceFiles.Length,
                BinaryFileCount = binaryCount,
                Architecture = InferArchitecture(projectDirectory, files, combined),
                ProtocolsAndComponents = InferProtocolsAndComponents(combined, files),
                TargetFrameworks = string.Join(", ", ExtractDistinct(combined, TargetFrameworkPattern()).Take(12)),
                PackageReferences = string.Join(", ", ExtractDistinct(combined, PackageReferencePattern()).Take(24)),
                ImportantFiles = BuildImportantFileList(rootPath, projectDirectory, sourceFiles)
            };

            return summary;
        }

        private static IEnumerable<FileInfo> EnumerateUsefulFiles(string directory)
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
                    if (!ExcludedDirectoryNames.Contains(subdirectory.Name))
                        stack.Push(subdirectory);
                }

                foreach (var file in files)
                {
                    if (SourceExtensions.Contains(file.Extension) || BinaryExtensions.Contains(file.Extension))
                        yield return file;
                }
            }
        }

        private static CouncilKnowledgeEntry ToKnowledgeEntry(LearnBaseProjectSummary summary)
        {
            var sanitizedSourcePath = RedactSensitiveName(summary.SourcePath);
            var content = new StringBuilder()
                .AppendLine("This entry is about reusable architecture and wiring patterns, not about copying names or branding.")
                .AppendLine("Learn the functionality, protocols, service boundaries, host wiring, and component usage. Treat source labels as evidence labels, not as target product names.")
                .AppendLine($"Architecture fingerprint source label: {summary.Name}")
                .AppendLine($"Sanitized source path label: {sanitizedSourcePath}")
                .AppendLine($"Architecture signals: {summary.Architecture}")
                .AppendLine($"Protocols/components: {summary.ProtocolsAndComponents}")
                .AppendLine($"Target frameworks: {Fallback(summary.TargetFrameworks, "none detected")}")
                .AppendLine($"Package references: {Fallback(summary.PackageReferences, "none detected")}")
                .AppendLine($"Important files: {summary.ImportantFiles}")
                .AppendLine($"Source files counted: {summary.SourceFileCount}; binary/build artifacts counted but not stored: {summary.BinaryFileCount}.")
                .AppendLine("Generation guidance: learn host shapes, protocols, libraries, service boundaries, and solution setup. Do not preserve project names unless the user explicitly asks.")
                .AppendLine("Ask for a poll when the user has not selected monolith vs microservice, Blazor vs non-Blazor frontend, DevExpress Web API/security, Python interop, or data persistence style.")
                .AppendLine("Legacy offensive names are sanitized in knowledge records; preserve the technical pattern, not the wording.")
                .ToString();

            return new CouncilKnowledgeEntry
            {
                Id = CreateStableGuid($"learn-base|{summary.SourcePath}"),
                Topic = $"Selected learn-base architecture fingerprint: {summary.Architecture}",
                Scope = "Selected local project learn-base",
                Source = $"Local learn-base scan: {sanitizedSourcePath}",
                Content = content,
                HelpfulSources = "Local user-selected source folder C:\\tmpselectedcodexlearnbaseforlocalgpt. Import stores compact fingerprints only; inspect source directly before copying exact code. Legacy offensive names are sanitized before teaching.",
                Tags = BuildTags(summary),
                Confidence = 78,
                VerificationStatus = "SourceBacked",
                IsUserApproved = false,
                IsPinned = summary.Name.Contains("Tacos", StringComparison.OrdinalIgnoreCase) ||
                    summary.Name.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                    summary.Name.Contains("Jezzifa", StringComparison.OrdinalIgnoreCase)
            };
        }

        private static string InferArchitecture(string projectDirectory, IReadOnlyList<FileInfo> files, string combined)
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
                ? $"Mixed or legacy project under {RedactSensitiveName(Path.GetFileName(projectDirectory))}"
                : string.Join("; ", signals.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string InferProtocolsAndComponents(string combined, IReadOnlyList<FileInfo> files)
        {
            var signals = new List<string>();
            AddIf(combined, signals, "HTTP API", "HttpClient", "ControllerBase", "MapGet", "WebApplication");
            AddIf(combined, signals, "OData", "OData", "EnableQuery");
            AddIf(combined, signals, "SQLite", "Sqlite", "SQLite", "sqlite");
            AddIf(combined, signals, "SQL Server", "SqlServer", "UseSqlServer");
            AddIf(combined, signals, "Telegram Bot API", "Telegram", "BotClient");
            AddIf(combined, signals, "Whisper/audio", "Whisper", "NAudio", "WaveIn");
            AddIf(combined, signals, "Python.NET interop", "Python.Runtime", "PythonEngine", "Py.GIL", "pythonnet");
            AddIf(combined, signals, "DevExpress Blazor", "DxGrid", "DxFormLayout", "AddDevExpressBlazor");
            AddIf(combined, signals, "DevExpress Web API/security", "DevExpress.ExpressApp.Security", "SecurityStrategy", "AddXafWebApi", "UseMiddleTier");
            AddIf(combined, signals, "DevExpress reports/documents", "XtraReport", "RichEdit", "PdfViewer", "Spreadsheet");
            AddIf(combined, signals, "MSIX/DesktopBridge", "wapproj", "AppxPackage", "DesktopBridge");
            AddIf(combined, signals, "Authentication/authorization", "AuthorizeView", "AddAuthentication", "Identity");
            AddIf(combined, signals, "multi-host ASP.NET/Blazor", "MapRazorComponents", "MapControllers", "AddRazorPages", "AddServerSideBlazor");
            AddIf(combined, signals, "AI host API", "/api/generate", "/api/chat", "/api/tags", "/api/pull", "/api/embed");
            AddIf(combined, signals, "OpenAI/Anthropic compatibility", "OpenAI", "anthropic", "/v1/chat/completions", "/v1/models");
            AddIf(combined, signals, "model manifest/download lifecycle", "manifest", "digest", "download", "transfer", "progress");
            AddIf(combined, signals, "inference runtime", "llama", "runner", "kvcache", "tokenizer", "sampling");
            AddIf(combined, signals, "chat format parsing", "template", "gotmpl", "harmony", "thinking");
            AddIf(combined, signals, "WebView/desktop tray shell", "WebView2", "wintray", "notifyicon");
            AddIf(combined, signals, "DevExpress AI Chat/function calling", "DxAIChat", "AI-Chat-FunctionCalling", "AI-Chat-GridFunctionCalling");
            AddIf(combined, signals, "DevExpress upload/file workflow", "DxUpload", "DxFileInput", "ChunkUpload");
            AddIf(combined, signals, "DevExpress reporting/document workflow", "AddDevExpressBlazorReporting", "RichEdit", "PdfViewer", "Spreadsheet");
            AddIf(combined, signals, "Python media tooling", "ffmpeg", "moviepy", "pydub", "youtube", "midi", "wave");
            if (files.Any(file => file.Name.Equals("app.py", StringComparison.OrdinalIgnoreCase)))
                signals.Add("Flask/Python web app");
            if (files.Any(file => file.Name.Equals("package.json", StringComparison.OrdinalIgnoreCase)))
                signals.Add("npm package frontend");

            return signals.Count == 0
                ? "No dominant protocol/component detected"
                : string.Join("; ", signals.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static void AddIf(string text, List<string> signals, string label, params string[] needles)
        {
            if (needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase)))
                signals.Add(label);
        }

        private static string BuildImportantFileList(string rootPath, string projectDirectory, IReadOnlyList<FileInfo> sourceFiles)
        {
            var important = sourceFiles
                .Where(file => IsImportantFile(file.Name, file.Extension))
                .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                .Take(28)
                .Select(file => RedactSensitiveName(Path.GetRelativePath(projectDirectory, file.FullName).Replace('\\', '/')));

            return string.Join("; ", important);
        }

        private static IEnumerable<string> BuildImportDirectories(string rootPath, int maxProjects)
        {
            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var directory in EnumerateImportDirectoryCandidates(rootPath))
            {
                if (emitted.Count >= maxProjects)
                    yield break;

                var directoryName = Path.GetFileName(directory);
                if (ExcludedDirectoryNames.Contains(directoryName) || !emitted.Add(directory))
                    continue;

                yield return directory;
            }
        }

        private static IEnumerable<string> EnumerateImportDirectoryCandidates(string rootPath)
        {
            if (LooksLikeArchitectureRoot(rootPath))
                yield return rootPath;

            foreach (var directory in SafeEnumerateDirectories(rootPath))
                yield return directory;

            foreach (var directory in EnumerateNestedArchitectureRoots(rootPath))
                yield return directory;
        }

        private static IEnumerable<string> EnumerateNestedArchitectureRoots(string rootPath)
        {
            var stack = new Stack<DirectoryInfo>(SafeEnumerateDirectoryInfos(rootPath).Reverse());
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (ExcludedDirectoryNames.Contains(current.Name))
                    continue;

                if (LooksLikeArchitectureRoot(current.FullName))
                    yield return current.FullName;

                foreach (var child in SafeEnumerateDirectoryInfos(current.FullName).Reverse())
                    stack.Push(child);
            }
        }

        private static IEnumerable<string> SafeEnumerateDirectories(string rootPath)
        {
            try
            {
                return Directory.EnumerateDirectories(rootPath).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        private static IEnumerable<DirectoryInfo> SafeEnumerateDirectoryInfos(string rootPath)
        {
            try
            {
                return new DirectoryInfo(rootPath)
                    .EnumerateDirectories()
                    .Where(directory => !ExcludedDirectoryNames.Contains(directory.Name))
                    .OrderBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        private static bool LooksLikeWindowsDevDocsRoot(string rootPath)
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
                return false;
            }
        }

        private static bool LooksLikeArchitectureRoot(string rootPath)
        {
            var directory = new DirectoryInfo(rootPath);
            if (!directory.Exists)
                return false;

            try
            {
                if (directory.GetFiles().Any(file => IsProjectRootFile(file.Name, file.Extension)))
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool IsImportantFile(string fileName, string extension)
        {
            return IsProjectRootFile(fileName, extension) ||
                extension is ".razor" or ".xaml" or ".py" or ".json" or ".sql" or ".md" or ".mdx" or ".go" or ".gotmpl" ||
                fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Startup.", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("App.razor", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("_Imports.razor", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProjectRootFile(string fileName, string extension)
        {
            return extension is ".sln" or ".csproj" or ".fsproj" or ".vbproj" ||
                fileName.Equals("go.mod", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("go.sum", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("CMakeLists.txt", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> ExtractDistinct(string text, Regex pattern)
        {
            return pattern.Matches(text)
                .Select(match => match.Groups["value"].Value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string ReadSmallText(FileInfo file)
        {
            try
            {
                return File.ReadAllText(file.FullName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        private static string RedactSensitiveName(string value)
        {
            return SensitiveNamePattern().Replace(value, "[redacted-name]");
        }

        private static string BuildTags(LearnBaseProjectSummary summary)
        {
            var tags = new List<string> { "learn-base", "source-backed", "architecture-fingerprint" };
            foreach (var token in Regex.Split($"{summary.Architecture};{summary.ProtocolsAndComponents}", @"[^A-Za-z0-9]+"))
            {
                if (token.Length is >= 3 and <= 28)
                    tags.Add(token.ToLowerInvariant());
            }

            return string.Join("; ", tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(24));
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static Guid CreateStableGuid(string value)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
            return new Guid(hash);
        }

        [GeneratedRegex("<TargetFrameworks?>(?<value>[^<]+)</TargetFrameworks?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex TargetFrameworkPattern();

        [GeneratedRegex("<PackageReference\\s+Include=\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex PackageReferencePattern();

        [GeneratedRegex("(?i)(fuck|shit|bitch|cunt|dick|pussy|whore|slut|porn|xxx)")]
        private static partial Regex SensitiveNamePattern();
    }
}
