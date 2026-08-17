using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
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
    /// <summary>Executes the read guidance docs async operation.</summary>
        /// <summary>
        /// Reads guidance docs as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="env">Input value for env.</param>
        /// <param name="relativePaths">Input value for relativePaths.</param>
        /// <param name="fallbackBriefing">Input value for fallbackBriefing.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes with the operation result.</returns>
        public async Task<IResult> ReadGuidanceDocsAsync(
            IWebHostEnvironment env,
            IReadOnlyList<string> relativePaths,
            string fallbackBriefing,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var foundFiles = new List<object>();
                var briefing = new StringBuilder();

                foreach (var relativePath in relativePaths)
                {
                    var candidatePaths = new[]
                    {
                    Path.Combine(AppContext.BaseDirectory, relativePath),
                    Path.Combine(env.ContentRootPath, relativePath),
                    Path.Combine(Directory.GetCurrentDirectory(), relativePath)
                }.Distinct(StringComparer.OrdinalIgnoreCase);

                    var path = candidatePaths.FirstOrDefault(System.IO.File.Exists);
                    if (path is null)
                        continue;

                    var text = await System.IO.File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                    foundFiles.Add(new
                    {
                        RelativePath = relativePath.Replace('\\', '/'),
                        SourcePath = path
                    });

                    briefing
                        .Append("# ")
                        .AppendLine(Path.GetFileName(relativePath))
                        .AppendLine()
                        .AppendLine(text.Trim())
                        .AppendLine();
                }

                return Results.Ok(new
                {
                    GuidanceFiles = foundFiles,
                    Briefing = foundFiles.Count > 0
                        ? briefing.ToString().Trim()
                        : fallbackBriefing.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reading the approved guidance documents failed; guidance content and local paths were omitted from logs.");
                return Results.InternalServerError("The guidance documents could not be read. See local application logs for technical details.");
            }
        }
        /// <summary>Executes the limit prompt size operation.</summary>
        /// <param name="messages">Input value for messages.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="forcedMaxPromptCharacters">Input value for forcedMaxPromptCharacters.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<ChatMessage> LimitPromptSize(IReadOnlyList<ChatMessage> messages, ILogger logger, int? forcedMaxPromptCharacters = null)
        {
            try
            {
                var maxPromptCharacters = Math.Clamp(forcedMaxPromptCharacters ?? catalog.DefaultMaxPromptCharacters, 512, catalog.MaxPromptCharacters);
                if (messages.Sum(EstimateTextLength) <= maxPromptCharacters)
                    return messages;

                var result = new List<ChatMessage>();
                var usedCharacters = 0;
                var remainingSystemBudget = Math.Min(catalog.MaxBootstrapCharacters, Math.Max(maxPromptCharacters / 2, 0));

                foreach (var message in messages.Where(message => message.Role == ChatRole.System))
                {
                    var textInner = message.Text ?? string.Empty;
                    var budget = Math.Min(remainingSystemBudget, maxPromptCharacters - usedCharacters);
                    if (budget <= 0)
                        break;

                    var trimmed = text.TrimForPrompt(textInner, budget,logger, keepBothEnds: false);
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    result.Add(new ChatMessage(message.Role, trimmed));
                    usedCharacters += trimmed.Length;
                    remainingSystemBudget -= trimmed.Length;
                }

                var conversationMessages = messages
                    .Where(message => message.Role != ChatRole.System)
                    .ToList();
                var keptConversationMessages = new Stack<ChatMessage>();

                for (var index = conversationMessages.Count - 1; index >= 0; index--)
                {
                    var remainingBudget = maxPromptCharacters - usedCharacters;
                    if (remainingBudget <= 0)
                        break;

                    var message = conversationMessages[index];
                    var textInner = message.Text ?? string.Empty;
                    var messageBudget = Math.Min(catalog.MaxSingleConversationMessageCharacters, remainingBudget);
                    var trimmed =   text.TrimForPrompt(textInner, messageBudget, logger, keepBothEnds: true);
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    keptConversationMessages.Push(new ChatMessage(message.Role, trimmed));
                    usedCharacters += trimmed.Length;
                }

                result.AddRange(keptConversationMessages);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not limit prompt size to {ForcedMaxPromptCharacters} characters.",
                    forcedMaxPromptCharacters);
                return new List<ChatMessage>();
            }
        }

        /// <summary>Executes the estimate text length operation.</summary>
        /// <param name="message">Input value for message.</param>
        /// <returns>The operation result.</returns>
        public int EstimateTextLength(ChatMessage message) {
    try
    {
        return message.Text?.Length ?? 0;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(EstimateTextLength)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(CouncilRuntimeService)}.{nameof(EstimateTextLength)} failed.");
        throw;
    }
}

        /// <summary>Executes the try is supported ollama mode operation.</summary>
        /// <param name="mode">Input value for mode.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? TryIsSupportedOllamaMode(string mode,ILogger logger)
        {
            try
            {
                return mode.Equals(catalog.OllamaModeAutoGpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(catalog.OllamaModeSafeCpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(catalog.OllamaModeLimitedGpu, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsSupportedOllamaMode mode:{mode}");
                return null;
            }

        }
       
        /// <summary>Executes the is blazor frontend target operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="targetArea">Input value for targetArea.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsBlazorFrontendTarget(string prompt, string finalAnswer, string targetArea, ILogger logger)
        {
            try
            {
                return targetArea.Contains("Blazor/DevExpress frontend", StringComparison.OrdinalIgnoreCase) ||
               catalog.BlazorFrontendPattern.IsMatch($"{prompt} {finalAnswer}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the target area is Blazor/DevExpress: {TargetArea}.", targetArea);
                return null;
            }
           
        }

        /// <summary>Executes the is whole solution target operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsWholeSolutionTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var isAiHostExperimentTarget = IsAiHostExperimentTarget(prompt, finalAnswer, logger);
                bool isAiHostExperimentTargetBool = isAiHostExperimentTarget ?? false;
                return catalog.WholeSolutionPattern.IsMatch(prompt) || isAiHostExperimentTargetBool;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the response targets a whole solution.");
                return null;
            }
        }

        /// <summary>Executes the is ai host experiment target operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsAiHostExperimentTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {

                return catalog.AiHostExperimentPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the response targets an AI-host experiment.");
                return null;
            }
        }

        /// <summary>Executes the is advice only prompt operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool? IsAdviceOnlyPrompt(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.AdviceOnlyPromptPattern.IsMatch(prompt) &&
                    !catalog.ExplicitArtifactCreationCommandPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not determine whether the request is advice-only.");
                return null;
            }
        }

        /// <summary>Executes the detect solution archetype operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="finalAnswer">Input value for finalAnswer.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public GeneratedSolutionArchetype? DetectSolutionArchetype(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                if (catalog.AiHostExperimentPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.AiHost;
                if (catalog.LocalGptReplacementPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.LocalGpt;
                if (catalog.TacosPortalPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.TacosPortal;
                if (catalog.BotBackendPattern.IsMatch(prompt))
                    return GeneratedSolutionArchetype.BotBackend;

                return GeneratedSolutionArchetype.Generic;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect the requested solution archetype.");
                return null;
            }
        }


        /// <summary>Executes the add directory to zip operation.</summary>
        /// <param name="archive">Input value for archive.</param>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="directoryPath">Input value for directoryPath.</param>
        /// <param name="logger">Input value for logger.</param>
        public void AddDirectoryToZip(ZipArchive archive, string rootPath, string directoryPath, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return;

                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    var entryName = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
                    AddFileToZip(archive, filePath, entryName, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddDirectoryToZip archive:{archive.ToString()} rootPath:{rootPath} directoryPath:{directoryPath}", archive);

            }
        }

        /// <summary>Executes the add file to zip operation.</summary>
        /// <param name="archive">Input value for archive.</param>
        /// <param name="filePath">Input value for filePath.</param>
        /// <param name="entryName">Input value for entryName.</param>
        /// <param name="logger">Input value for logger.</param>
        public void AddFileToZip(ZipArchive archive, string filePath, string entryName, ILogger logger)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                archive.CreateEntryFromFile(filePath, entryName.Replace('\\', '/'), CompressionLevel.SmallestSize);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddFileToZip archive:{archive.ToString()} filePath:{filePath} entryName:{entryName}", archive);
            }
        }

        /// <summary>Executes the copy directory operation.</summary>
        /// <param name="sourceDirectory">Input value for sourceDirectory.</param>
        /// <param name="destinationDirectory">Input value for destinationDirectory.</param>
        /// <param name="logger">Input value for logger.</param>
        public void CopyDirectory(string sourceDirectory, string destinationDirectory, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyDirectory sourceDirectory:{sourceDirectory} destinationDirectory:{destinationDirectory}");
            }
           
        }

    

        /// <summary>Executes the archetype page operation.</summary>
        /// <param name="fileName">Input value for fileName.</param>
        /// <param name="route">Input value for route.</param>
        /// <param name="title">Input value for title.</param>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="areas">Input value for areas.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public GeneratedArchetypePage ArchetypePage(
            string fileName,
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas, ILogger logger)
        {
            try
            {
                return new GeneratedArchetypePage(
             fileName,
             text.GenerateArchetypePageRazor(route, title, summary, areas, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not generate archetype page {FileName} for route {Route}.", fileName, route);
                return new GeneratedArchetypePage(
                    string.IsNullOrWhiteSpace(fileName) ? "GeneratedPage.razor" : fileName,
                    $"@page \"{route}\"{Environment.NewLine}<PageTitle>{title}</PageTitle>{Environment.NewLine}<h1>{title}</h1>{Environment.NewLine}<p>Page generation failed. Review LocalGPT logs and regenerate this page.</p>");
            }
         
        }
        /// <summary>Executes the write text async operation.</summary>
        /// <param name="path">Input value for path.</param>
        /// <param name="content">Input value for content.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public Task WriteTextAsync(string path, string content, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path has no directory: {path}"));
                return File.WriteAllTextAsync(path, content, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteTextAsync");
                return Task.CompletedTask;
            }
        }

        /// <summary>Executes the generate archetype pages operation.</summary>
        /// <param name="archetype">Input value for archetype.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<GeneratedArchetypePage> GenerateArchetypePages(GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                return archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt =>
                    [
                        ArchetypePage("Chat.razor", "/chat", "DXAiChat", "Chat surface with model routing, uploads, artifact links, visible progress, and memory-aware continuation.", ["Model selection", "Council mode", "File context", "Artifact downloads"],logger),
                    ArchetypePage("ModelCouncil.razor", "/model-council", "AI Council", "Multi-model review surface for feedback talks, polls, missing features, source requests, and implementation artifacts.", ["Minimum two members", "Sequential scheduling", "Poll gate", "Feedback log"],logger),
                    ArchetypePage("Database.razor", "/database", "SQLite Database", "Editable operational memory for chats, thoughts, logs, knowledge, benchmark scores, and approval markers.", ["CouncilKnowledgeEntries", "ChatMessages", "ApplicationLogs", "BenchmarkResults"],logger),
                    ArchetypePage("MinecraftModBuilder.razor", "/minecraft-mod-builder", "Minecraft Mod Builder", "Workspace generator for datapacks, Fabric, Paper, NeoForge, Java/Gradle setup, validation, and downloads.", ["Datapack zip", "Loader matrix", "Version resolver", "Validation script"],logger),
                    ArchetypePage("TestLab.razor", "/test-lab", "Test Lab", "Frontend-accessible diagnostics for API smoke checks, benchmark routes, artifact downloads, and WebView2 workflows.", ["Health", "DXAiFunctions", "Replacement benchmark", "Council feedback"],logger),
                    ArchetypePage("Install.razor", "/install", "Install", "Model host discovery, Ollama/LM Studio status, model pull planning, runtime checks, and setup guidance.", ["Ollama status", "LM Studio status", "Model downloads", "Java/.NET checks"],logger)
                    ],
                    GeneratedSolutionArchetype.TacosPortal =>
                    [
                        ArchetypePage("TelegramIngestion.razor", "/telegram-ingestion", "Telegram Ingestion", "Event-ingestion boundary with update handling, command routing, idempotency, retries, and sanitized bot service wiring.", ["Update handler", "Command router", "Idempotency", "Retry queue"],logger),
                    ArchetypePage("Persistence.razor", "/persistence", "Persistence", "Normalized domain persistence with EF/SQLite or provider-specific backend, explicit DTO/service boundaries, and migration notes.", ["Business objects", "DbContext", "DTO boundaries", "Migration safety"],logger),
                    ArchetypePage("Workers.razor", "/workers", "Workers", "Hosted/background worker view for polling, notification dispatch, API synchronization, and operational diagnostics.", ["Hosted services", "Polling", "Notifications", "Diagnostics"],logger),
                    ArchetypePage("Admin.razor", "/admin", "Admin", "DevExpress CRUD/admin workbench with roles, audit log, validation, custom security, and operational settings.", ["Users", "Roles", "Audit", "Settings"],logger),
                    ArchetypePage("ClientShells.razor", "/client-shells", "Client Shells", "Host map for Blazor server, optional WASM client, WinUI/WebView2 wrapper, package boundaries, and debug/deploy notes.", ["Server host", "WASM client", "WinUI/WebView2", "Package diagnostics"],logger)
                    ],
                    GeneratedSolutionArchetype.BotBackend =>
                    [
                        ArchetypePage("Webhooks.razor", "/webhooks", "Webhooks", "Inbound message and event receiver surface with validation, idempotency, and retry diagnostics.", ["Ingress", "Signature check", "Idempotency", "Dead letters"],logger),
                    ArchetypePage("Conversations.razor", "/conversations", "Conversations", "Conversation-state workbench with memory, moderation, handoff, and compact transcript review.", ["Memory", "Moderation", "Handoff", "Transcript"],logger),
                    ArchetypePage("BotSettings.razor", "/bot-settings", "Bot Settings", "Provider-neutral bot configuration with secrets stored outside the generated code and visible safety gates.", ["Provider", "Token source", "Allowed commands", "Rate limit"],logger),
                    ArchetypePage("PythonInterop.razor", "/python-interop", "Python Interop", "Optional Python.NET or process-adapter boundary for transcription, translation, media, or model tooling.", ["Python.NET", "Process adapter", "Safe directory", "User approval"],logger)
                    ],
                    _ => []
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePages archetype:{archetype.ToString()}");
                return new List<GeneratedArchetypePage>();
            }
           
        }
        /// <summary>Executes the extract dynamic promise modules operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public IReadOnlyList<GeneratedPromiseModule> ExtractDynamicPromiseModules(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var textInner = $"{request.Prompt} {result.FinalAnswer}";
                var modules = new List<GeneratedPromiseModule>();

                void AddIf(bool condition, string title, string summary, IReadOnlyList<string> areas)
                {
                    if (!condition || modules.Any(module => module.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
                        return;

                    var route = "/" + text.ToKebabRoute(title,logger);
                    var fileName = $"{text.ToPascalIdentifier(title, logger)}.razor";
                    modules.Add(new GeneratedPromiseModule(fileName, route, title, summary, areas));
                }

                AddIf(
                    catalog.DevExpressDocumentPattern.IsMatch(textInner) || catalog.ExportFormatPattern.IsMatch(textInner),
                    "Document Exports",
                    "Promise-derived surface for report, Office, PDF, spreadsheet, presentation, and document export work owned by backend services.",
                    ["Report template", "Format mapping", "Backend service", "Download route"]);
                AddIf(
                    textInner.Contains("FileDownloadController", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("download link", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("download route", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("safe download", StringComparison.OrdinalIgnoreCase),
                    "Download Center",
                    "Promise-derived surface for generated files, MIME types, safe HTTP GET links, checksums, expiry, and user-visible artifact status.",
                    ["Generated files", "HTTP GET", "Checksum", "Expiry"]);
                AddIf(
                    textInner.Contains("DxAiFunctions", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("IAIInferenceProvider", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("/api/inference", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("AI prompt", StringComparison.OrdinalIgnoreCase),
                    "AI Prompt Flow",
                    "Promise-derived surface for prompt-to-plan workflows, model/provider calls, generated briefs, and Needs verification notes.",
                    ["Prompt", "Provider call", "Generated brief", "Verification"]);
                AddIf(
                    textInner.Contains("IModelCatalogService", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("model catalog", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("LM Studio", StringComparison.OrdinalIgnoreCase),
                    "Model Host Status",
                    "Promise-derived surface for local model/provider inventory, host reachability, selected model, and runtime status.",
                    ["Provider", "Model catalog", "Reachability", "Runtime status"]);
                AddIf(
                    textInner.Contains("SQLite", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("EF/", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("persist", StringComparison.OrdinalIgnoreCase),
                    "Persistence",
                    "Promise-derived surface for database state, DTO projection, migration safety, audit records, and user-approved knowledge.",
                    ["EF/SQLite", "DTOs", "Migration safety", "Audit"]);
                AddIf(
                    textInner.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("DxGrid", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("DxFormLayout", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("Blazor", StringComparison.OrdinalIgnoreCase),
                    "DevExpress UI",
                    "Promise-derived surface for DevExpress Blazor controls, layout, navigation, forms, grids, and frontend verification.",
                    ["Navigation", "Grid", "Form", "Frontend smoke"]);
                AddIf(
                    textInner.Contains("API endpoint", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("controller", StringComparison.OrdinalIgnoreCase) ||
                    textInner.Contains("/api/", StringComparison.OrdinalIgnoreCase),
                    "API Contracts",
                    "Promise-derived surface for backend routes, request/response DTOs, validation, errors, and smoke-test calls.",
                    ["Routes", "DTOs", "Validation", "Smoke tests"]);

                return modules.Take(8).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ExtractDynamicPromiseModules");
                return new List<GeneratedPromiseModule>();
            }
           
        }

    }
}
