using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public sealed class EngineeringBenchmarkService(
        ICouncilArtifactService artifactService,
        ICouncilKnowledgeService knowledgeService,
        ILearnBaseKnowledgeImporterService learnBaseImporter,
        ILogger<EngineeringBenchmarkService> logger) : IEngineeringBenchmarkService
    {
        public async Task<EngineeringBenchmarkResult> RunAsync(
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new EngineeringBenchmarkResult
            {
                TaskSet = NormalizeTaskSet(request.TaskSet)
            };
            if (request.ImportLearnBaseFirst)
                result.LearnBaseImport = await learnBaseImporter.ImportAsync(new LearnBaseImportRequest
                {
                    RootPath = request.LearnBaseRootPath,
                    SaveToKnowledge = true,
                    MaxProjects = 40
                }, cancellationToken);

            foreach (var task in BuildTasks(result.TaskSet))
            {
                var taskResult = new EngineeringBenchmarkTaskResult
                {
                    TaskId = task.Id,
                    Name = task.Name,
                    Prompt = task.Prompt
                };

                taskResult.Lanes.Add(NotRunLane("A. raw Ollama model", "Live raw Ollama call intentionally not run in this deterministic benchmark. Run later with GPU-safe caps and record the transcript."));
                if (request.RunLocalGptArtifacts)
                    taskResult.Lanes.Add(await RunLocalGptLaneAsync(task, request, cancellationToken));
                else
                    taskResult.Lanes.Add(NotRunLane("B. LocalGPT with DxFunctions + memory", "Skipped by request."));

                taskResult.Lanes.Add(NotRunLane("C. cloud ChatGPT/Codex-style assistant", "Cloud comparison must be run with a real prompt/session and pasted evidence; not faked by LocalGPT."));
                taskResult.Lanes.Add(BuildManualExpectedLane(task));
                result.Tasks.Add(taskResult);
            }

            result.CompletedAtUtc = DateTime.UtcNow;
            if (request.SaveToKnowledge)
                result.KnowledgeEntryId = await SaveBenchmarkKnowledgeAsync(result, cancellationToken);

            logger.LogInformation("Engineering benchmark {RunId} completed with {TaskCount} task(s).", result.RunId, result.Tasks.Count);
            return result;
        }

        private async Task<EngineeringBenchmarkLaneResult> RunLocalGptLaneAsync(
            BenchmarkTaskDefinition task,
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var councilResult = new MultiModelCouncilResult
            {
                Prompt = task.Prompt,
                ModelNames = ["LocalGPT deterministic artifact service"],
                FinalAnswer = task.LocalGptFinalAnswer
            };
            var councilRequest = new MultiModelCouncilRequest
            {
                Prompt = task.Prompt,
                ModelNames = ["artifact-benchmark"],
                GenerateImplementationArtifact = true,
                MaxOutputTokens = 1024,
                MaxContextTokens = 2048,
                MaxRounds = 0
            };

            var artifacts = await artifactService.CreateImplementationArtifactsAsync(councilRequest, councilResult, cancellationToken);
            stopwatch.Stop();

            var lane = new EngineeringBenchmarkLaneResult
            {
                Lane = "B. LocalGPT with DxFunctions + memory",
                Status = artifacts.Count > 0 ? "Ran" : "RanNoArtifact",
                Duration = stopwatch.Elapsed,
                Artifacts = artifacts.ToList(),
                TimeToUsableOutputScore = stopwatch.Elapsed < TimeSpan.FromSeconds(20) ? 10 : stopwatch.Elapsed < TimeSpan.FromMinutes(1) ? 8 : 5,
                RepairPromptsScore = 10,
                RepairPromptCount = 0,
                DownloadableArtifactScore = artifacts.Any(artifact => !string.IsNullOrWhiteSpace(artifact.DownloadUrl)) ? 10 : 0
            };

            var artifactEntries = artifacts
                .Where(artifact => artifact.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .SelectMany(ReadZipEntriesSafe)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            lane.Evidence.AddRange(artifacts.Select(artifact => $"{artifact.Kind}: {artifact.Name} -> {artifact.DownloadUrl}"));
            lane.MissingFiles.AddRange(task.RequiredArtifactEntries.Where(required => !ContainsZipEntry(artifactEntries, required)));
            lane.MissingFilesScore = lane.MissingFiles.Count == 0 ? 10 : Math.Max(0, 10 - lane.MissingFiles.Count * 2);
            lane.ValidArchitectureScore = ScoreArchitecture(task, artifactEntries, artifacts);
            if (request.ValidateBuildableArtifacts)
                lane.BuildChecks.AddRange(await ValidateBuildableArtifactsAsync(artifacts, request.MaxBuildArtifacts, cancellationToken));

            lane.BuildabilityScore = ScoreBuildability(task, artifacts, lane.BuildChecks, request.ValidateBuildableArtifacts);
            lane.WrongPackagesTemplatesScore = ScoreWrongTemplateRisk(task, artifactEntries);
            lane.TotalScore = SumScores(lane);
            lane.Notes = lane.MissingFiles.Count == 0
                ? "Deterministic LocalGPT artifact path produced expected benchmark files."
                : "Artifact was produced, but required benchmark entries were missing. This is improvement fuel, not a pass.";
            if (request.ValidateBuildableArtifacts && lane.BuildChecks.Count > 0)
                lane.Notes += $" Build checks: {string.Join("; ", lane.BuildChecks.Select(check => $"{check.ArtifactName}={check.Status}"))}.";
            return lane;
        }

        private static EngineeringBenchmarkLaneResult BuildManualExpectedLane(BenchmarkTaskDefinition task)
        {
            var lane = new EngineeringBenchmarkLaneResult
            {
                Lane = "D. manual expected output",
                Status = "Reference",
                ValidArchitectureScore = 10,
                BuildabilityScore = 10,
                MissingFilesScore = 10,
                WrongPackagesTemplatesScore = 10,
                TimeToUsableOutputScore = 0,
                RepairPromptsScore = 10,
                DownloadableArtifactScore = 0,
                RepairPromptCount = 0,
                Notes = task.ManualExpectedOutput
            };
            lane.TotalScore = SumScores(lane);
            return lane;
        }

        private static EngineeringBenchmarkLaneResult NotRunLane(string laneName, string notes)
        {
            return new EngineeringBenchmarkLaneResult
            {
                Lane = laneName,
                Status = "NotRun",
                Notes = notes
            };
        }

        private static async Task<IReadOnlyList<EngineeringBenchmarkBuildCheck>> ValidateBuildableArtifactsAsync(
            IReadOnlyList<CouncilArtifact> artifacts,
            int maxBuildArtifacts,
            CancellationToken cancellationToken)
        {
            var checks = new List<EngineeringBenchmarkBuildCheck>();
            var zipArtifacts = artifacts
                .Where(artifact => artifact.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(artifact.FilePath))
                .Take(Math.Clamp(maxBuildArtifacts, 1, 8))
                .ToArray();

            foreach (var artifact in zipArtifacts)
            {
                checks.Add(await ValidateBuildableArtifactAsync(artifact, cancellationToken));
            }

            return checks;
        }

        private static async Task<EngineeringBenchmarkBuildCheck> ValidateBuildableArtifactAsync(
            CouncilArtifact artifact,
            CancellationToken cancellationToken)
        {
            var started = DateTime.UtcNow;
            var root = Path.Combine(
                Path.GetTempPath(),
                "LocalGPT",
                "EngineeringBenchmarkBuilds",
                $"{Path.GetFileNameWithoutExtension(artifact.Name)}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);

            var check = new EngineeringBenchmarkBuildCheck
            {
                ArtifactName = artifact.Name,
                ExtractedRoot = root
            };

            try
            {
                ZipFile.ExtractToDirectory(artifact.FilePath, root, overwriteFiles: true);
                var solutionPath = Directory
                    .EnumerateFiles(root, "*.sln", SearchOption.AllDirectories)
                    .OrderBy(path => path.Length)
                    .FirstOrDefault();

                if (solutionPath is null)
                {
                    check.Status = "NoSolution";
                    check.OutputPreview = "No .sln file found. This artifact is not a .NET build target.";
                    return check;
                }

                check.SolutionPath = solutionPath;
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{solutionPath}\" -c Debug /nologo /p:UseSharedCompilation=false",
                    WorkingDirectory = Path.GetDirectoryName(solutionPath) ?? root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    check.Status = "ProcessNotStarted";
                    return check;
                }

                var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
                await process.WaitForExitAsync(timeoutCts.Token);
                var output = await outputTask;
                var error = await errorTask;

                check.ExitCode = process.ExitCode;
                check.Status = process.ExitCode == 0 ? "BuildPassed" : "BuildFailed";
                check.OutputPreview = TrimForPrompt(output, 1800);
                check.ErrorPreview = TrimForPrompt(error, 1200);
                return check;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                check.Status = "TimedOut";
                check.ErrorPreview = "dotnet build exceeded the 3 minute benchmark timeout.";
                return check;
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                check.Status = "BuildCheckError";
                check.ErrorPreview = TrimForPrompt(ex.Message, 1200);
                return check;
            }
            finally
            {
                check.Duration = DateTime.UtcNow - started;
            }
        }

        private async Task<Guid> SaveBenchmarkKnowledgeAsync(EngineeringBenchmarkResult result, CancellationToken cancellationToken)
        {
            var summary = new StringBuilder()
                .AppendLine($"Engineering benchmark run {result.RunId} completed at {result.CompletedAtUtc:O}.")
                .AppendLine($"Task set: {result.TaskSet}.")
                .AppendLine($"Tasks: {string.Join("; ", result.Tasks.Select(task => task.Name))}.")
                .AppendLine("Lane rule: raw Ollama and cloud lanes must be run with real transcripts before scoring; deterministic LocalGPT artifacts are allowed for no-GPU smoke evidence.");

            foreach (var task in result.Tasks)
            {
                var local = task.Lanes.FirstOrDefault(lane => lane.Lane.StartsWith("B.", StringComparison.Ordinal));
                summary.AppendLine($"- {task.Name}: LocalGPT status {local?.Status}, score {local?.TotalScore}, artifacts {local?.Artifacts.Count ?? 0}.");
                if (local?.MissingFiles.Count > 0)
                    summary.AppendLine($"  Missing: {string.Join(", ", local.MissingFiles)}");
                if (local?.BuildChecks.Count > 0)
                    summary.AppendLine($"  Build checks: {string.Join(", ", local.BuildChecks.Select(check => $"{check.ArtifactName}={check.Status}"))}");
            }

            var entry = await knowledgeService.SaveEntryAsync(new CouncilKnowledgeEntry
            {
                Topic = "Personal engineering benchmark: LocalGPT vs raw models",
                Scope = "Benchmark",
                Source = $"/__diag/benchmark/engineering {result.RunId}",
                Content = summary.ToString(),
                HelpfulSources = "Use selected learn-base knowledge entries, artifact zips, build checks, raw Ollama transcripts, and cloud assistant transcripts. Do not fake unrun lanes.",
                Tags = "benchmark; localgpt; ollama; devexpress; blazor; minecraft; artifacts",
                Confidence = 70,
                VerificationStatus = "SourceBacked",
                IsPinned = true
            }, cancellationToken);

            return entry.Id;
        }

        private static int ScoreArchitecture(
            BenchmarkTaskDefinition task,
            HashSet<string> zipEntries,
            IReadOnlyList<CouncilArtifact> artifacts)
        {
            if (artifacts.Count == 0)
                return 0;

            var hits = task.ArchitectureEvidence.Count(evidence =>
                zipEntries.Any(entry => entry.Contains(evidence, StringComparison.OrdinalIgnoreCase)) ||
                artifacts.Any(artifact => artifact.Summary.Contains(evidence, StringComparison.OrdinalIgnoreCase) ||
                    artifact.Kind.Contains(evidence, StringComparison.OrdinalIgnoreCase)));

            return task.ArchitectureEvidence.Count == 0
                ? 7
                : Math.Clamp(4 + hits * 2, 0, 10);
        }

        private static int ScoreWrongTemplateRisk(BenchmarkTaskDefinition task, HashSet<string> zipEntries)
        {
            if (task.WrongTemplateGuards.Count == 0)
                return 8;

            var guardHits = task.WrongTemplateGuards.Count(guard => ContainsZipEntry(zipEntries, guard));
            return guardHits == task.WrongTemplateGuards.Count ? 10 : Math.Max(0, 10 - (task.WrongTemplateGuards.Count - guardHits) * 3);
        }

        private static int ScoreBuildability(
            BenchmarkTaskDefinition task,
            IReadOnlyList<CouncilArtifact> artifacts,
            IReadOnlyList<EngineeringBenchmarkBuildCheck> buildChecks,
            bool validateBuildableArtifacts)
        {
            if (artifacts.Count == 0)
                return 0;

            if (!validateBuildableArtifacts)
                return task.LocalGptBuildabilityScore;

            var dotnetChecks = buildChecks
                .Where(check => !check.Status.Equals("NoSolution", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (dotnetChecks.Length == 0)
                return task.LocalGptBuildabilityScore;

            return dotnetChecks.All(check => check.Status.Equals("BuildPassed", StringComparison.OrdinalIgnoreCase))
                ? 10
                : 0;
        }

        private static bool ContainsZipEntry(HashSet<string> zipEntries, string required)
        {
            var normalized = required.Replace('\\', '/').Trim('/');
            return zipEntries.Any(entry =>
                string.Equals(entry.Trim('/'), normalized, StringComparison.OrdinalIgnoreCase) ||
                entry.Contains($"/{normalized}", StringComparison.OrdinalIgnoreCase) ||
                entry.StartsWith($"{normalized}/", StringComparison.OrdinalIgnoreCase) ||
                entry.Contains($"{normalized}/", StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<string> ReadZipEntriesSafe(CouncilArtifact artifact)
        {
            if (!File.Exists(artifact.FilePath))
                return [];

            try
            {
                using var archive = ZipFile.OpenRead(artifact.FilePath);
                return archive.Entries
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();
            }
            catch (InvalidDataException)
            {
                return [];
            }
        }

        private static int SumScores(EngineeringBenchmarkLaneResult lane)
        {
            return lane.ValidArchitectureScore +
                lane.BuildabilityScore +
                lane.MissingFilesScore +
                lane.WrongPackagesTemplatesScore +
                lane.TimeToUsableOutputScore +
                lane.RepairPromptsScore +
                lane.DownloadableArtifactScore;
        }

        private static string NormalizeTaskSet(string? taskSet)
        {
            if (string.IsNullOrWhiteSpace(taskSet))
                return "engineering";

            return taskSet.Trim().ToLowerInvariant() switch
            {
                "replacement" or "replacements" or "apps" or "app-replacements" => "replacement",
                "all" or "full" => "all",
                _ => "engineering"
            };
        }

        private static string TrimForPrompt(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static IReadOnlyList<BenchmarkTaskDefinition> BuildTasks(string taskSet)
        {
            var engineering = BuildEngineeringTasks();
            var replacements = BuildReplacementTasks();

            return taskSet switch
            {
                "replacement" => replacements,
                "all" => engineering.Concat(replacements).ToArray(),
                _ => engineering
            };
        }

        private static IReadOnlyList<BenchmarkTaskDefinition> BuildEngineeringTasks()
        {
            return
            [
                new(
                    "devexpress-webshop-efcore",
                    "DevExpress Blazor webshop with EF Core",
                    "Generate a downloadable whole solution zip for a DevExpress Blazor webshop with EF Core, SQLite seed data, products, carts, orders, admin CRUD grid, detail form, Bootstrap v5 layout, and README.",
                    "A strong answer contains a .NET solution, EF Core DbContext/entities/migration guidance, DevExpress product/admin grids, cart/order services, seed data, app navigation, build/run steps, and no client-side privileged commands.",
                    "Benchmark answer: create a full solution zip with DevExpress Blazor pages, EF Core entities, services, product/cart/order workflows, and README. Include Implementation artifact request.",
                    6,
                    ["PROJECT_INDEX.md", ".localgpt-generation.json", "src/"],
                    ["DevExpress", "Blazor", "service", "model"],
                    ["Components/Pages", "Services", "Models"]),
                new(
                    "blazor-admin-crud-dashboard",
                    "Blazor admin dashboard with CRUD grid and detail form",
                    "Generate a downloadable whole solution zip for a Blazor admin dashboard with DevExpress DxGrid CRUD, detail form, validation, SQLite persistence, audit log, and Bootstrap v5 navigation.",
                    "A strong answer contains DxGrid, EditForm/DxFormLayout detail editing, validation, EF Core persistence, audit logging, clear service boundaries, and buildable project files.",
                    "Benchmark answer: create a full solution zip with DxGrid CRUD, detail form, validation, SQLite persistence, audit notes, and README. Include Implementation artifact request.",
                    6,
                    ["PROJECT_INDEX.md", ".localgpt-generation.json", "src/"],
                    ["DevExpress", "Blazor", "grid"],
                    ["Components/Pages", "Services", "Models"]),
                new(
                    "msix-winui-blazor-packaging",
                    "MSIX/WinUI/Blazor packaging error diagnosis",
                    "Diagnose and produce a downloadable LocalGPT-style implementation note for an MSIX WinUI WebView2 Blazor packaging error involving static web assets, LocalGPT.deps.json, IncludeLocalGptPublishedPayload, and APPX1111 duplicate paths.",
                    "A strong answer separates SDK dotnet build from Visual Studio MSBuild, preserves thin WinUI wrapper, explains IncludeLocalGptPublishedPayload=false for Debug/F5 and release opt-in, and names static web asset payload risks.",
                    "Benchmark answer: produce a concise .cs artifact note and optional solution zip explaining DesktopBridge diagnosis, package-map duplicate risks, and verification commands. Include Implementation artifact request.",
                    5,
                    [],
                    ["MSIX", "WebView2", "WinUI"],
                    []),
                new(
                    "minecraft-datapack-workspace",
                    "Minecraft datapack workspace",
                    "Generate a downloadable Minecraft Java datapack zip for a prompt-driven city simulation datapack named Benchmark Borough with scoreboards, storage, load/tick tags, debug function, docs, and Minecraft 1.21.4 pack format.",
                    "A strong answer contains zip root pack.mcmeta and data/ directly, singular 1.21 function folders, valid load/tick tags, lowercase namespace, no .mcfunction.txt files, no leading slash commands, and install/test steps.",
                    "Benchmark answer: generate a prompt-driven datapack zip for Benchmark Borough, not a hard-coded Living Cities artifact. Include pack.mcmeta and data/ at zip root.",
                    9,
                    ["pack.mcmeta", "data/minecraft/tags/function/load.json", "data/minecraft/tags/function/tick.json"],
                    ["datapack", "pack.mcmeta"],
                    ["pack.mcmeta", "data/"]),
                new(
                    "minecraft-loader-skeletons",
                    "Fabric/Paper/NeoForge project skeleton distinction",
                    "Generate a downloadable Minecraft Java project skeleton distinction zip that contains separate Fabric, Paper, and NeoForge skeletons for Minecraft 1.21.4, with each loader using its own metadata and Gradle dependency conventions.",
                    "A strong answer keeps Fabric metadata, Paper plugin.yml, and NeoForge mods.toml/dependencies separate; it does not reuse one loader template for all three.",
                    "Benchmark answer: create a loader matrix zip with distinct Fabric, Paper, and NeoForge workspaces. Include project skeleton distinction in the answer.",
                    8,
                    ["fabric/", "paper/", "neoforge/"],
                    ["Fabric", "Paper", "NeoForge"],
                    ["fabric", "paper", "neoforge"])
            ];
        }

        private static IReadOnlyList<BenchmarkTaskDefinition> BuildReplacementTasks()
        {
            return
            [
                new(
                    "localgpt-replacement",
                    "LocalGPT replacement workbench",
                    "Generate a downloadable whole solution zip that can stand in for LocalGPT as a local-first AI workbench. It must include DXAiChat, AI Council with minimum two-member feedback talk, SQLite memory and knowledge approval markers, artifact download routes, Minecraft builder, install/setup, and Test Lab surfaces. No missing feature is acceptable; if a capability is not implemented, represent it as a visible backend service boundary and capability gap.",
                    "A strong answer is a buildable .NET/Blazor/DevExpress solution with recognizable LocalGPT navigation: DXAiChat, AI Council, SQLite Database, Minecraft Mod Builder, Install, Help/Test Lab, artifact routes, memory/knowledge services, logs, and missing-feature feedback capture.",
                    "Benchmark answer: create a full LocalGPT-like workbench solution zip with distinct pages for DXAiChat, AI Council, SQLite, Minecraft, Install, and Test Lab. Include Implementation artifact request.",
                    7,
                    ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Chat.razor", "Components/Pages/ModelCouncil.razor", "Components/Pages/Database.razor", "Components/Pages/MinecraftModBuilder.razor", "Components/Pages/TestLab.razor", "Components/Pages/Install.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                    ["DXAiChat", "AI Council", "SQLite", "Minecraft", "Test Lab", "Source Fidelity", "Artifact"],
                    ["Components/Pages/Chat.razor", "Components/Pages/ModelCouncil.razor", "Components/Pages/Database.razor", "Services/GeneratedSourceFidelityService.cs"]),
                new(
                    "tacosportalopen-replacement",
                    "TacosPortalOpen replacement portal",
                    "Generate a downloadable whole solution zip that can stand in for TacosPortalOpen as a server-interactive DevExpress/Blazor system. It must represent the real architecture: multi-project/core service topology, Telegram or message-event ingestion, normalized persistence, worker services, notifications/logging, custom security/admin UI, optional WASM client, WinUI/WebView2 wrapper boundary, and a sanitized simpler bot backend implementation.",
                    "A strong answer is a buildable .NET/Blazor/DevExpress solution with pages and service boundaries for Telegram ingestion, persistence, workers, admin/security, client shells, notification/logging, EF/SQLite or provider-backed data, validation, and build/run docs. A generic menu/orders/reservations restaurant portal is the wrong template.",
                    "Benchmark answer: create a full TacosPortalOpen-style multi-host/event-ingestion solution zip with Telegram ingestion, persistence, workers, admin, client-shell boundaries, and source-fidelity docs. Include Implementation artifact request.",
                    7,
                    ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/TelegramIngestion.razor", "Components/Pages/Persistence.razor", "Components/Pages/Workers.razor", "Components/Pages/Admin.razor", "Components/Pages/ClientShells.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                    ["Telegram", "Persistence", "Workers", "WebView2", "WASM", "DevExpress", "Source Fidelity"],
                    ["Components/Pages/TelegramIngestion.razor", "Components/Pages/Persistence.razor", "Components/Pages/Workers.razor", "Services/GeneratedSourceFidelityService.cs"]),
                new(
                    "ai-host-replacement",
                    "Provider-compatible AI host replacement",
                    "Generate a downloadable whole solution zip for a provider-neutral AI host replacement in .NET 10, ASP.NET Core, Blazor, and DevExpress. It must include model catalog, chat, downloads, running models, API console, logs, settings, templates, hardware, runner/plugins, /api/version, /api/tags, /api/ps, /api/chat, /api/generate, OpenAI-compatible routes, direct local model-file runner interfaces, Python.NET/PowerShell extension boundaries, and SQLite/appsettings state.",
                    "A strong answer is a buildable AI-host solution with provider-compatible routes, DevExpress navigation, model/download/runtime pages, native local-model-file runner interfaces, no upstream provider proxying, and explicit runner setup/status.",
                    "Benchmark answer: create a buildable AI-host replacement solution zip with DevExpress pages, provider-compatible routes, runner/plugin service contracts, and no Go dependency. Include Implementation artifact request.",
                    9,
                    ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Chat.razor", "Components/Pages/RunningModels.razor", "Components/Pages/ModelDownloads.razor", "Components/Pages/RunnerPlugins.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedAiHostArchitectureServices.cs", "Services/GeneratedSourceFidelityService.cs"],
                    ["IInferenceProvider", "IInferenceRunner", "RunnerPlugins", "/api/chat", "Source Fidelity"],
                    ["Components/Pages/RunnerPlugins.razor", "Services/GeneratedAiHostArchitectureServices.cs", "Services/GeneratedSourceFidelityService.cs"]),
                new(
                    "simple-bot-backend",
                    "Simpler bot backend implementation",
                    "Generate a downloadable whole solution zip for a simpler bot backend inspired by legacy Telegram-style integrations, but sanitized. It must include webhooks, conversation state, command routing, moderation/retry queues, optional Python.NET boundary for speech/translation/media helpers, settings, logs, EF/SQLite, and a DevExpress Blazor operator UI.",
                    "A strong answer is a buildable .NET/Blazor/DevExpress bot backend with Webhooks, Conversations, Bot Settings, Python Interop pages, services, safe permission gates, and no private database dump requirement.",
                    "Benchmark answer: create a simple bot backend solution zip with webhook/conversation/settings/python-interop pages and safe backend service boundaries. Include Implementation artifact request.",
                    7,
                    ["PROJECT_INDEX.md", "SOURCE_FIDELITY.md", ".localgpt-generation.json", "src/", "Components/Pages/Webhooks.razor", "Components/Pages/Conversations.razor", "Components/Pages/BotSettings.razor", "Components/Pages/PythonInterop.razor", "Components/Pages/SourceFidelity.razor", "Services/GeneratedSourceFidelityService.cs"],
                    ["Webhooks", "Conversations", "Python Interop", "SQLite"],
                    ["Components/Pages/Webhooks.razor", "Components/Pages/PythonInterop.razor", "Services/GeneratedSourceFidelityService.cs"])
            ];
        }

        private sealed record BenchmarkTaskDefinition(
            string Id,
            string Name,
            string Prompt,
            string ManualExpectedOutput,
            string LocalGptFinalAnswer,
            int LocalGptBuildabilityScore,
            IReadOnlyList<string> RequiredArtifactEntries,
            IReadOnlyList<string> ArchitectureEvidence,
            IReadOnlyList<string> WrongTemplateGuards);
    }
}
