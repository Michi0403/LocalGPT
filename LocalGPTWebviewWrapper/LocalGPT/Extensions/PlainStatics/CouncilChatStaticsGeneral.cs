using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using System.IO.Compression;
using System.Text.Json;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class CouncilChatStaticsGeneral
    {

        public static bool IsSupportedOllamaMode(string mode) =>
            mode.Equals(GlobalVariableSlopCollectionToRemove.OllamaModeAutoGpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(GlobalVariableSlopCollectionToRemove.OllamaModeSafeCpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(GlobalVariableSlopCollectionToRemove.OllamaModeLimitedGpu, StringComparison.OrdinalIgnoreCase);
        public static bool IsBlazorFrontendTarget(string prompt, string finalAnswer, string targetArea)
        {
            return targetArea.Contains("Blazor/DevExpress frontend", StringComparison.OrdinalIgnoreCase) ||
                GlobalVariableSlopCollectionToRemove.BlazorFrontendPattern().IsMatch($"{prompt} {finalAnswer}");
        }

        public static bool IsWholeSolutionTarget(string prompt, string finalAnswer)
        {
            return GlobalVariableSlopCollectionToRemove.WholeSolutionPattern().IsMatch(prompt) || IsAiHostExperimentTarget(prompt, finalAnswer);
        }

        public static bool IsAiHostExperimentTarget(string prompt, string finalAnswer)
        {
            return GlobalVariableSlopCollectionToRemove.AiHostExperimentPattern().IsMatch(prompt);
        }

        public static bool IsAdviceOnlyPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            return GlobalVariableSlopCollectionToRemove.AdviceOnlyPromptPattern().IsMatch(prompt) &&
                !GlobalVariableSlopCollectionToRemove.ExplicitArtifactCreationCommandPattern().IsMatch(prompt);
        }

        public static GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype DetectSolutionArchetype(string prompt, string finalAnswer)
        {
            if (GlobalVariableSlopCollectionToRemove.AiHostExperimentPattern().IsMatch(prompt))
                return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.AiHost;
            if (GlobalVariableSlopCollectionToRemove.LocalGptReplacementPattern().IsMatch(prompt))
                return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt;
            if (GlobalVariableSlopCollectionToRemove.TacosPortalPattern().IsMatch(prompt))
                return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.TacosPortal;
            if (GlobalVariableSlopCollectionToRemove.BotBackendPattern().IsMatch(prompt))
                return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.BotBackend;

            return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.Generic;
        }


        public static void ValidateGeneratedDatapackWorkspace(string rootPath)
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
                if (GlobalVariableSlopCollectionToRemove.LeadingSlashCommandPattern().IsMatch(content))
                    throw new InvalidOperationException($"Generated function contains a leading slash command: {Path.GetRelativePath(rootPath, functionFile)}");
                if (GlobalVariableSlopCollectionToRemove.RootStorageRemovePattern().IsMatch(content))
                    throw new InvalidOperationException($"Generated function uses data remove storage root syntax: {Path.GetRelativePath(rootPath, functionFile)}");
                if (GlobalVariableSlopCollectionToRemove.MalformedStorageTargetPattern().IsMatch(content))
                    throw new InvalidOperationException($"Generated function appears to put an NBT path into the storage id instead of after it: {Path.GetRelativePath(rootPath, functionFile)}");
            }
        }

        public static void AddDirectoryToZip(ZipArchive archive, string rootPath, string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
                AddFileToZip(archive, filePath, entryName);
            }
        }

        public static void AddFileToZip(ZipArchive archive, string filePath, string entryName)
        {
            if (!File.Exists(filePath))
                return;

            archive.CreateEntryFromFile(filePath, entryName.Replace('\\', '/'), CompressionLevel.SmallestSize);
        }

        public static void CopyDirectory(string sourceDirectory, string destinationDirectory)
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

        public static string GeneratePromiseModuleRazor(GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule module) =>
            CouncilChatStringFunctions. GenerateArchetypePageRazor(module.Route, module.Title, module.Summary, module.Areas);

        public static GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage ArchetypePage(
            string fileName,
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas)
        {
            return new GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage(
                fileName,
                CouncilChatStringFunctions.GenerateArchetypePageRazor(route, title, summary, areas));
        }
        public static Task WriteTextAsync(string path, string content, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path has no directory: {path}"));
            return File.WriteAllTextAsync(path, content, cancellationToken);
        }

        public static IReadOnlyList<GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage> GenerateArchetypePages(GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype archetype)
        {
            return archetype switch
            {
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt =>
                [
                    ArchetypePage("Chat.razor", "/chat", "DXAiChat", "Chat surface with model routing, uploads, artifact links, visible progress, and memory-aware continuation.", ["Model selection", "Council mode", "File context", "Artifact downloads"]),
                    ArchetypePage("ModelCouncil.razor", "/model-council", "AI Council", "Multi-model review surface for feedback talks, polls, missing features, source requests, and implementation artifacts.", ["Minimum two members", "Sequential scheduling", "Poll gate", "Feedback log"]),
                    ArchetypePage("Database.razor", "/database", "SQLite Database", "Editable operational memory for chats, thoughts, logs, knowledge, benchmark scores, and approval markers.", ["CouncilKnowledgeEntries", "ChatMessages", "ApplicationLogs", "BenchmarkResults"]),
                    ArchetypePage("MinecraftModBuilder.razor", "/minecraft-mod-builder", "Minecraft Mod Builder", "Workspace generator for datapacks, Fabric, Paper, NeoForge, Java/Gradle setup, validation, and downloads.", ["Datapack zip", "Loader matrix", "Version resolver", "Validation script"]),
                    ArchetypePage("TestLab.razor", "/test-lab", "Test Lab", "Frontend-accessible diagnostics for API smoke checks, benchmark routes, artifact downloads, and WebView2 workflows.", ["Health", "DXAiFunctions", "Replacement benchmark", "Council feedback"]),
                    ArchetypePage("Install.razor", "/install", "Install", "Model host discovery, Ollama/LM Studio status, model pull planning, runtime checks, and setup guidance.", ["Ollama status", "LM Studio status", "Model downloads", "Java/.NET checks"])
                ],
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.TacosPortal =>
                [
                    ArchetypePage("TelegramIngestion.razor", "/telegram-ingestion", "Telegram Ingestion", "Event-ingestion boundary with update handling, command routing, idempotency, retries, and sanitized bot service wiring.", ["Update handler", "Command router", "Idempotency", "Retry queue"]),
                    ArchetypePage("Persistence.razor", "/persistence", "Persistence", "Normalized domain persistence with EF/SQLite or provider-specific backend, explicit DTO/service boundaries, and migration notes.", ["Business objects", "DbContext", "DTO boundaries", "Migration safety"]),
                    ArchetypePage("Workers.razor", "/workers", "Workers", "Hosted/background worker view for polling, notification dispatch, API synchronization, and operational diagnostics.", ["Hosted services", "Polling", "Notifications", "Diagnostics"]),
                    ArchetypePage("Admin.razor", "/admin", "Admin", "DevExpress CRUD/admin workbench with roles, audit log, validation, custom security, and operational settings.", ["Users", "Roles", "Audit", "Settings"]),
                    ArchetypePage("ClientShells.razor", "/client-shells", "Client Shells", "Host map for Blazor server, optional WASM client, WinUI/WebView2 wrapper, package boundaries, and debug/deploy notes.", ["Server host", "WASM client", "WinUI/WebView2", "Package diagnostics"])
                ],
                GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.BotBackend =>
                [
                    ArchetypePage("Webhooks.razor", "/webhooks", "Webhooks", "Inbound message and event receiver surface with validation, idempotency, and retry diagnostics.", ["Ingress", "Signature check", "Idempotency", "Dead letters"]),
                    ArchetypePage("Conversations.razor", "/conversations", "Conversations", "Conversation-state workbench with memory, moderation, handoff, and compact transcript review.", ["Memory", "Moderation", "Handoff", "Transcript"]),
                    ArchetypePage("BotSettings.razor", "/bot-settings", "Bot Settings", "Provider-neutral bot configuration with secrets stored outside the generated code and visible safety gates.", ["Provider", "Token source", "Allowed commands", "Rate limit"]),
                    ArchetypePage("PythonInterop.razor", "/python-interop", "Python Interop", "Optional Python.NET or process-adapter boundary for transcription, translation, media, or model tooling.", ["Python.NET", "Process adapter", "Safe directory", "User approval"])
                ],
                _ => []
            };
        }
        public static IReadOnlyList<GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule> ExtractDynamicPromiseModules(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result)
        {
            var text = $"{request.Prompt} {result.FinalAnswer}";
            var modules = new List<GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule>();

            void AddIf(bool condition, string title, string summary, IReadOnlyList<string> areas)
            {
                if (!condition || modules.Any(module => module.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
                    return;

                var route = "/" + CouncilChatStringFunctions. ToKebabRoute(title);
                var fileName = $"{CouncilChatStringFunctions.ToPascalIdentifier(title)}.razor";
                modules.Add(new GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule(fileName, route, title, summary, areas));
            }

            AddIf(
                GlobalVariableSlopCollectionToRemove.DevExpressDocumentPattern().IsMatch(text) || GlobalVariableSlopCollectionToRemove.ExportFormatPattern().IsMatch(text),
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

        public static List<CouncilKnowledgeEntry> SortKnowledgeEntries(IEnumerable<CouncilKnowledgeEntry> entries)
        {
            return entries
                .OrderBy(CouncilChatStaticsGeneral.KnowledgeReviewPriority)
                .ThenByDescending(entry => entry.IsPinned)
                .ThenByDescending(entry => entry.IsUserApproved)
                .ThenByDescending(entry => entry.UpdatedAtUtc)
                .ToList();
        }
        public static CouncilKnowledgeEntry CreateEmptyKnowledgeEntry()
        {
            return new CouncilKnowledgeEntry
            {
                Topic = "New LocalGPT knowledge",
                Scope = "AI Council",
                Source = "Manual database editor",
                HelpfulSources = "None yet.",
                Tags = "manual; council",
                Confidence = 60,
                VerificationStatus = "UserVerified",
                ReviewStatus = "Current",
                LastVerifiedAtUtc = DateTime.UtcNow,
                IsUserApproved = true
            };
        }
        public static CouncilKnowledgeEntry CopyKnowledgeEntry(CouncilKnowledgeEntry entry)
        {
            return new CouncilKnowledgeEntry
            {
                Id = entry.Id,
                CreatedAtUtc = entry.CreatedAtUtc,
                UpdatedAtUtc = entry.UpdatedAtUtc,
                Topic = entry.Topic,
                Scope = entry.Scope,
                Content = entry.Content,
                Source = entry.Source,
                HelpfulSources = entry.HelpfulSources,
                Tags = entry.Tags,
                Confidence = entry.Confidence,
                VerificationStatus = entry.VerificationStatus,
                ReviewStatus = entry.ReviewStatus,
                ExpiresAtUtc = entry.ExpiresAtUtc,
                LastVerifiedAtUtc = entry.LastVerifiedAtUtc,
                LastUsedAtUtc = entry.LastUsedAtUtc,
                SupersededByKnowledgeId = entry.SupersededByKnowledgeId,
                StalenessReason = entry.StalenessReason,
                StalenessDetectedAtUtc = entry.StalenessDetectedAtUtc,
                StalenessDetectedBy = entry.StalenessDetectedBy,
                SourceHash = entry.SourceHash,
                SourceDateUtc = entry.SourceDateUtc,
                IsUserApproved = entry.IsUserApproved,
                IsPinned = entry.IsPinned,
                IsArchived = entry.IsArchived
            };
        }

        public static string BuildKnowledgeReviewSummary(IReadOnlyCollection<CouncilKnowledgeEntry> entries)
        {
            if (entries.Count == 0)
                return "No knowledge notes loaded yet.";

            var needsAttention = entries.Count(entry => entry.ReviewStatus is "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired");
            var trusted = entries.Count(entry => entry.ReviewStatus == "Current" && entry.IsUserApproved);
            return $"{needsAttention} note(s) need attention. {trusted} user-approved current note(s) can guide the council.";
        }

        public static int GetCouncilModelLoadPriority(string modelName)
        {
            if (modelName.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (modelName.Contains("deepseek-r1:8b", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (modelName.Contains("gemma", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (modelName.Contains("qwen", StringComparison.OrdinalIgnoreCase))
                return 3;

            return 10;
        }

        public static int KnowledgeReviewPriority(CouncilKnowledgeEntry entry)
        {
            return entry.ReviewStatus switch
            {
                "NeedsUserReview" => 0,
                "NeedsSourceRefresh" => 1,
                "NeedsDiagnosticVerification" => 2,
                "Expired" => 3,
                "Superseded" => 4,
                "Deprecated" => 5,
                "Current" => 6,
                "Archived" => 7,
                _ => 8
            };
        }

        public static bool IsDynamicSession(ChatClientSession session) =>
            session.Name.StartsWith(GlobalVariableSlopCollectionToRemove.DetectedOllamaSessionPrefix, StringComparison.OrdinalIgnoreCase) ||
            session.Name.Equals(GlobalVariableSlopCollectionToRemove.CouncilSessionName, StringComparison.OrdinalIgnoreCase);

        public static IEnumerable<string> OrderCouncilModelsForLoad(IEnumerable<string> modelNames)
        {
            return modelNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetCouncilModelLoadPriority)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
        }
        public static string BuildDynamicSessionName(MultiModelCouncilModelCandidate candidate) =>
      $"{GlobalVariableSlopCollectionToRemove.DetectedOllamaSessionPrefix}{candidate.ModelName} @ {CouncilChatStringFunctions.TrimEndpoint(candidate.Endpoint)}";

        public static string BuildCandidateLabel(MultiModelCouncilModelCandidate candidate) =>
            $"{candidate.ModelName} @ {CouncilChatStringFunctions.TrimEndpoint(candidate.Endpoint)}";

        public static string BuildCandidateTitle(MultiModelCouncilModelCandidate candidate)
        {
            var details = string.IsNullOrWhiteSpace(candidate.Details)
                ? "No model details reported."
                : candidate.Details;
            return $"{candidate.Provider} at {candidate.Endpoint}. {details}";
        }


        public static GlobalVariableSlopCollectionToRemove.ArtifactContractReport ValidateSolutionArtifactContract(
            string solutionRoot,
            string projectName,
            GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype archetype)
        {
            var isAiHostLab = archetype == GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.AiHost;
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

            if (isAiHostLab)
            {
                ValidateAiHostArtifactContract(solutionRoot, projectName);
                return new GlobalVariableSlopCollectionToRemove.ArtifactContractReport(
                    "Source-contract prototype",
                    "AI-host source contract validated",
                    [
                        "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable",
                        "AI-host endpoint and native-runner source markers exist"
                    ],
                    ["No model-file runtime execution proof was produced", "No generated-project build proof was produced"],
                    "AI-host routes, settings, navigation, and native-runner source markers were checked before zipping; runtime behavior is still unproven.");
            }

            if (archetype == GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt)
            {
                return new GlobalVariableSlopCollectionToRemove.ArtifactContractReport(
                    "Static LocalGPT-style prototype",
                    "Missing LocalGPT runtime contract",
                    [
                        "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable"
                    ],
                    [
                        "DXAiChat runtime wiring is not proven",
                        "AI Council execution is not proven",
                        "SQLite memory persistence is not proven",
                        "Artifact route behavior is not proven"
                    ],
                    "LocalGPT-like source files were generated, but the artifact must not be treated as a working LocalGPT replacement.");
            }

            return new GlobalVariableSlopCollectionToRemove.ArtifactContractReport(
                "Generated solution prototype",
                "Generated files validated",
                [
                    "Required generated file set exists",
                    "Generation contract JSON is parseable",
                    "Generation manifest JSON is parseable"
                ],
                ["No generated-project build proof was produced", "No runtime UI proof was produced"],
                "Required files and metadata were checked before zipping; build and runtime behavior are unproven.");
        }

        public static void ValidateAiHostArtifactContract(string solutionRoot, string projectName)
        {
            var projectRoot = Path.Combine(solutionRoot, "src", projectName);
            var programPath = Path.Combine(projectRoot, "Program.cs");
            var architectureServicePath = Path.Combine(projectRoot, "Services", "GeneratedAiHostArchitectureServices.cs");
            var appSettingsPath = Path.Combine(projectRoot, "appsettings.json");
            var navigationPath = Path.Combine(projectRoot, "Components", "GeneratedNavigation.razor");

            var program = File.ReadAllText(programPath);
            var architectureService = File.ReadAllText(architectureServicePath);
            var appSettings = File.ReadAllText(appSettingsPath);
            var navigation = File.ReadAllText(navigationPath);

            var requiredRoutes = new[]
            {
                "/api/version",
                "/api/tags",
                "/api/ps",
                "/api/generate",
                "/api/chat"
            };

            foreach (var route in requiredRoutes)
            {
                if (!program.Contains(route, StringComparison.Ordinal))
                    throw new InvalidOperationException($"AI host artifact Program.cs is missing required route {route}.");
            }

            var requiredProgramTokens = new[]
            {
                "IInferenceProvider",
                "NativeModelFileInferenceProvider",
                "IInferenceRunner",
                "NativeModelFileProcessRunner",
                "upstream_proxy = false"
            };

            foreach (var token in requiredProgramTokens)
            {
                if (!program.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"AI host artifact Program.cs is missing required implementation token {token}.");
            }

            var requiredServiceTokens = new[]
            {
                "AiHostRuntimeOptions",
                "NativeModelFileProcessRunner",
                "NativeRunnerExecutable",
                "No upstream proxy fallback is used",
                "ProcessStartInfo"
            };

            foreach (var token in requiredServiceTokens)
            {
                if (!architectureService.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"AI host architecture service is missing required implementation token {token}.");
            }

            var requiredSettingTokens = new[]
            {
                "\"DefaultModel\"",
                "\"NativeRunnerExecutable\"",
                "\"ModelSearchRoots\"",
                "\"ContextTokens\"",
                "\"GpuLayers\""
            };

            foreach (var token in requiredSettingTokens)
            {
                if (!appSettings.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"AI host appsettings.json is missing required setting {token}.");
            }

            var requiredNavigationRoutes = new[]
            {
                "/chat",
                "/models",
                "/api-console",
                "/downloads",
                "/settings"
            };

            foreach (var route in requiredNavigationRoutes)
            {
                if (!navigation.Contains(route, StringComparison.Ordinal))
                    throw new InvalidOperationException($"AI host navigation is missing required route {route}.");
            }
        }

        public static void ValidateGenerationContractJson(string path)
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

        public static void ValidateGenerationManifestJson(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            RequireJsonProperty(root, "artifactKind", path);
            RequireJsonProperty(root, "sourceGoal", path);
            RequireJsonProperty(root, "validationStatus", path);
            RequireJsonProperty(root, "buildTestResultProvenance", path);
        }

        public static void RequireJsonProperty(JsonElement root, string propertyName, string path)
        {
            if (!root.TryGetProperty(propertyName, out _))
                throw new InvalidOperationException($"Generated contract {Path.GetFileName(path)} is missing {propertyName}.");
        }

        public static void RequireNonEmptyJsonArray(JsonElement root, string propertyName, string path)
        {
            RequireJsonProperty(root, propertyName, path);
            var property = root.GetProperty(propertyName);
            if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    $"Generated contract {Path.GetFileName(path)} must include a non-empty {propertyName} array.");
            }
        }

    }
}
