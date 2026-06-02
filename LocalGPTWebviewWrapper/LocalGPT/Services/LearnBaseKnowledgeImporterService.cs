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
            ".resx"
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

            var projectDirectories = Directory.EnumerateDirectories(rootPath)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(request.MaxProjects, 1, 100))
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
            var combined = string.Join("\n", textSamples);

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
            var content = new StringBuilder()
                .AppendLine("This entry is about reusable architecture and wiring patterns, not about copying names or branding.")
                .AppendLine($"Architecture fingerprint source label: {summary.Name}")
                .AppendLine($"Architecture signals: {summary.Architecture}")
                .AppendLine($"Protocols/components: {summary.ProtocolsAndComponents}")
                .AppendLine($"Target frameworks: {Fallback(summary.TargetFrameworks, "none detected")}")
                .AppendLine($"Package references: {Fallback(summary.PackageReferences, "none detected")}")
                .AppendLine($"Important files: {summary.ImportantFiles}")
                .AppendLine($"Source files counted: {summary.SourceFileCount}; binary/build artifacts counted but not stored: {summary.BinaryFileCount}.")
                .AppendLine("Generation guidance: learn host shapes, protocols, libraries, service boundaries, and solution setup. Do not preserve project names unless the user explicitly asks.")
                .AppendLine("Ask for a poll when the user has not selected monolith vs microservice, Blazor vs non-Blazor frontend, DevExpress Web API/security, Python interop, or data persistence style.")
                .ToString();

            return new CouncilKnowledgeEntry
            {
                Id = CreateStableGuid($"learn-base|{summary.SourcePath}"),
                Topic = $"Selected learn-base architecture fingerprint: {summary.Architecture}",
                Scope = "Selected local project learn-base",
                Source = $"Local learn-base scan: {summary.SourcePath}",
                Content = content,
                HelpfulSources = "Local user-selected source folder C:\\tmpselectedcodexlearnbaseforlocalgpt. Import stores compact fingerprints only; inspect source directly before copying exact code.",
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

        private static bool IsImportantFile(string fileName, string extension)
        {
            return extension is ".sln" or ".csproj" or ".razor" or ".xaml" or ".py" or ".json" or ".sql" or ".md" ||
                fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Startup.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("App.razor", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) ||
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

        [GeneratedRegex("(?i)\\b(fuck|shit|bitch|cunt|dick|pussy|whore|slut)\\b")]
        private static partial Regex SensitiveNamePattern();
    }
}
