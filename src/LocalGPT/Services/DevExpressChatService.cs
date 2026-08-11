using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.Extensions.AI;
using System.Net;

namespace LocalGPT.Services
{
    /// <summary>
    /// Provides dev express chat service operations.
    /// </summary>
    public sealed class DevExpressChatService(
        CouncilTextService text,
        IDxAiFunctionRegistry functionRegistry,
        LocalGptCatalogService catalog,
        ILogger<DevExpressChatService> logger)
    {
        private readonly CouncilTextService _text = text;
        private readonly IDxAiFunctionRegistry _functionRegistry = functionRegistry;
        private readonly LocalGptCatalogService _catalog = catalog;
        private readonly ILogger<DevExpressChatService> _logger = logger;

        /// <summary>
        /// Stores functions.
        /// </summary>
        public readonly DxaichatFunctionInfo[] Functions =
    [
        new(
            "minecraft.datapack_version",
            "GET",
            "/__diag/minecraft/datapack-version?minecraftVersion=26.1",
            "Resolve the datapack pack_format and singular/plural function folder convention for a Minecraft Java version.",
            "minecraftVersion: optional, defaults to current LocalGPT Java target 26.1.",
            "Read-only. Unknown versions are marked NeedsVerification instead of guessed as fact."),
        new(
            "minecraft.dependency_version",
            "GET",
            "/__diag/minecraft/dependency-version?loader=datapack&minecraftVersion=26.1",
            "Resolve curated Fabric, NeoForge, Paper, Java, Gradle, or datapack dependency versions before generating a workspace.",
            "loader: fabric, neoforge, paper, datapack, or bedrock. minecraftVersion/javaVersion/gradleVersion are optional.",
            "Read-only. Fallback mappings are marked NeedsVerification and should be checked against official sources before release."),
        new(
            "minecraft.workspace_smoke",
            "GET",
            "/__diag/minecraft/workspace-smoke?loader=datapack",
            "Generate a small Minecraft workspace for datapack, Fabric, NeoForge, or Paper smoke testing.",
            "loader: datapack, fabric, neoforge, or paper.",
            "Creates files under LocalAppData/LocalGPT/MinecraftModWorkspaces; does not launch Minecraft.",
            false,
            false),
        new(
            "minecraft.datapack_benchmark",
            "GET",
            "/__diag/minecraft/datapack-benchmark?minecraftVersion=26.1",
            "Generate, validate, zip, and save a compact council-knowledge entry for a current Java datapack benchmark.",
            "minecraftVersion: optional, defaults to current LocalGPT Java target 26.1. Use 1.21.4 only for legacy comparison.",
            "Runs the generated local build-local.ps1 validator; does not copy into a game world or run /reload.",
            false,
            false),
        new(
            "council.models",
            "GET",
            "/__diag/council/models",
            "List configured and installed Ollama council model candidates.",
            "No parameters.",
            "Read-only model discovery."),
        new(
            "council.benchmark_plan",
            "GET",
            "/__diag/council/benchmark-plan",
            "Return a hardware-safe benchmark matrix for testing which local model or council combination is best for .NET/DevExpress enterprise generation.",
            "No parameters.",
            "Read-only. Uses installed/configured model discovery and does not call Ollama generation."),
        new(
            "localgpt.learn_base_import",
            "GET",
            "/__diag/learn-base/import?maxProjects=40&saveToKnowledge=true",
            "Import compact architecture fingerprints from the user-selected learn-base folder into CouncilKnowledgeEntries.",
            "rootPath should be an explicit user-selected local folder; use localgpt.path.roots/localgpt.path.browse when discovery is needed; maxProjects optional; saveToKnowledge optional.",
            "Reads local source fingerprints and skips raw binaries/build folders. It teaches architecture, protocols, host wiring, libraries, and interop patterns rather than names or branding.",
            false,
            false),
        new(
            "localgpt.engineering_benchmark",
            "GET",
            "/__diag/benchmark/engineering?taskSet=engineering&importLearnBaseFirst=false&saveToKnowledge=true",
            "Run the five-task personal engineering benchmark with honest lane scoring for raw Ollama, LocalGPT artifacts, cloud assistant, and manual expected output.",
            "taskSet optional: engineering, replacement, or all. importLearnBaseFirst/saveToKnowledge/validateBuildableArtifacts/maxBuildArtifacts are optional.",
            "Deterministic LocalGPT artifact lane can run without GPU. Raw Ollama and cloud lanes are marked NotRun until real transcripts are supplied.",
            false,
            false),
        new(
            "localgpt.replacement_benchmark",
            "GET",
            "/__diag/benchmark/engineering?taskSet=replacement&validateBuildableArtifacts=true&maxBuildArtifacts=4&saveToKnowledge=true",
            "Benchmark LocalGPT-style, TacosPortalOpen-style, local-model-file AI-host, and simple bot-backend replacement solution generation with downloadable artifacts and optional dotnet build checks.",
            "validateBuildableArtifacts optional, defaults false unless this route sets it true. maxBuildArtifacts limits build checks.",
            "Runs deterministic artifact generation and dotnet build checks for generated .NET solution zips. Does not call local Ollama models.",
            false,
            false),
        new(
            "council.development_feedback_talk",
            "GET",
            "/__diag/council/development-feedback-talk?maxOutputTokens=2048&maxContextTokens=32768&maxRounds=0",
            "Run a compact minimum-two-member AI Council feedback talk about LocalGPT and the AI Council development, missing features, replacement benchmarks, and needed knowledge/functions. Consider LocalGPT Repository Sourcecode in the SQLite Database for that Purpose.",
            "modelNames optional comma-separated list. maxOutputTokens/maxContextTokens/maxRounds/ollamaNumGpu are optional.",
            "Calls local Ollama through the Council service and requires an explicit current user action.",
            false,
            false),
        new(
            "localgpt.sqlite.tables",
            "GET",
            "/__diag/sqlite/tables",
            "List LocalGPT SQLite tables, row counts, primary keys, and editable table metadata before reading or editing database state. Contains bounded diagnostic, knowledge, memory, and configuration data for user-directed review.",
            "No parameters.",
            "Read-only. Use this before requesting a specific table."),
        new(
            "localgpt.sqlite.table",
            "GET",
            "/__diag/sqlite/table/CouncilKnowledgeEntries?take=50",
            "Read a bounded preview of one LocalGPT SQLite table, such as CouncilKnowledgeEntries, ApplicationLogs, ChatConversations, ChatMessages, ModelThoughts, NativeCommandLogs, or settings tables.",
            "tableName in route; take optional, defaults to 100.",
            "Read-only bounded table preview. Do not request huge dumps; prefer targeted table names and small take values."),
        new(
            "localgpt.memory",
            "GET",
            "/__diag/memory",
            "Inspect saved DXAiChat/council conversations and recent model thoughts for compact continuation context. Inspect, if still existing, all regarding Workspaces to the conversations and topics, consider Knowledgebase and SQLite-Database Memory",
            "No parameters.",
            "Read-only and bounded to recent memory entries."),
        new(
            "localgpt.logs",
            "GET",
            "/__diag/logs?minimumLevel=Information&take=100",
            "Inspect recent application warnings/errors from the SQLite database logger so the council can react to Ollama, Java, static asset, SQLite, or deployment issues.",
            "minimumLevel: required, Microsoft.Extensions.Logging.LogLevel => Trace, Debug, Information, Warning, Error, Critical, None; take optional, defaults to 100.",
            "Read-only. Logs may contain local paths and diagnostic text; summarize rather than dumping everything."),
        new(
            "localgpt.knowledge",
            "GET",
            "/__diag/knowledge?includeArchived=false&take=50",
            "Read the editable council knowledge database with verification/approval markers.",
            "includeArchived optional; take optional.",
            "Read-only. Treat UserVerified/SourceBacked entries as stronger than ModelSuggested or unapproved entries."),
        new(
            "localgpt.blazor_devexpress_guidance",
            "GET",
            "/__diag/blazor-devexpress-guidance",
            "Return compact LocalGPT/TacosPortalOpen guidance for real .NET 10 Blazor, DevExpress, Bootstrap v5 layout, template starting points, and navigation SVG icon styles.",
            "No parameters.",
            "Read-only. Use this before generating Razor pages or blaming missing DevExpress/.NET context."),
        new(
            "localgpt.frontend_design_guidance",
            "GET",
            "/__diag/frontend-design-guidance",
            "Return LocalGPT's compiled frontend design pattern library for social, commerce, admin, AI-tool, media, Bootstrap, DevExpress/custom Razor components, Windows/Fluent principles, services, and accessibility checks.",
            "No parameters.",
            "Read-only. Use this before generating a frontend from a screenshot, goal app, broad UI request, or product archetype."),
        new(
            "localgpt.dotnet_sample_curriculum",
            "GET",
            "/__diag/dotnet-sample-curriculum",
            "Return official Microsoft/dotnet sample and Learn curriculum guidance for C#, .NET, ASP.NET Core, Blazor, EF, DevOps, architecture, and technician troubleshooting.",
            "No parameters.",
            "Read-only. Use this before generating whole .NET solutions, training plans, backend services, or CI/release advice."),
        new(
            "localgpt.ai_host_rebuild_guidance",
            "GET",
            "/__diag/ai-host-rebuild-guidance",
            "Return source-backed guidance for generating a local AI host .NET/ASP.NET Core/DevExpress Blazor app with routes, model catalog, chat, downloads, settings, logs, direct local model-file native runner interfaces, Python.NET/PowerShell boundaries, and capability gaps.",
            "No parameters.",
            "Read-only. Use this before saying a local AI host .NET rebuild is too large or before producing a generic dashboard."),
        new(
            "localgpt.ollama_compatible_smoke",
            "GET",
            "/__diag/ollama-compatible-smoke?endpoint=http://127.0.0.1:11434&model=gpt-oss:20b&numGpu=0",
            "Call an Ollama-compatible route surface through LocalGPT's own OllamaThinkingChatClient to prove LocalGPT can use a generated .NET AI host URL; acceptance requires the generated host to execute local model files directly, not proxy upstream Ollama.",
            "endpoint/model required for generated-host tests. prompt/maxOutputTokens/numGpu are optional.",
            "Calls a local or configured model endpoint and therefore requires an explicit current user action.",
            false,
            false),
        new(
            "localgpt.frontend_test_guidance",
            "GET",
            "/__diag/frontend-test-guidance",
            "Return source-backed guidance for LocalGPT Test Lab, deterministic frontend/API smoke tests, Selenium WebView2 automation, and optional Python.NET browser automation.",
            "No parameters.",
            "Read-only. Use this before claiming DXAiChat, WebView2, or artifact download behavior was tested."),
        new(
            "localgpt.capability_gap_contract",
            "GET",
            "/__diag/capability-gap-contract",
            "Return the structured contract for reporting missing LocalGPT functions, source knowledge, language/framework/version needs, local/external source requests, and the next downloadable artifact plan.",
            "No parameters.",
            "Read-only. Use this whenever the user says the council lacks capability or when a model needs investigation before reliable generation."),
        new(
            "council.artifact_smoke",
            "GET",
            "/__diag/council/artifact-smoke?target=blazor",
            "Generate a deterministic sandbox artifact bundle without calling Ollama, useful for testing .razor/.cs/.dll and whole-solution zip downloads.",
            "target: optional, defaults to blazor. Use target=solution for a zipped .NET 10 Blazor/DevExpress solution, target=datapack for a prompt-driven Minecraft datapack zip, target=loader-matrix for distinct Fabric/Paper/NeoForge skeletons, or target=ai-host for a local AI host .NET/DevExpress app with native local-model-file runner contracts. Optional prompt/finalAnswer query values replay a council promise so PROMISE_MAP/DESIGN_REVIEW fidelity can be tested without loading Ollama. target=ollama is only a backwards-compatible alias.",
            "Writes files under LocalAppData/LocalGPT/CouncilArtifacts; does not integrate generated code into the project.",
            false,
            false),
        new(
            "council.artifact_workspaces",
            "GET",
            "/__diag/artifact-workspaces",
            "List generated council artifact workspaces, the current LocalGPT base URL, artifact root, latest workspace, and bounded source-edit routes.",
            "take optional, defaults to 20.",
            "Read-only. Use this before giving download links or editing generated files so paths and host URLs are real."),
        new(
            "council.artifact_workspace_files",
            "GET",
            "/__diag/artifact-workspace/{workspaceName}/files",
            "List editable text/source files inside a generated artifact workspace.",
            "workspaceName from council.artifact_workspaces; take optional.",
            "Read-only. Only source/docs text files are listed; binaries and build outputs are excluded."),
        new(
            "council.artifact_workspace_file",
            "GET/POST",
            "/__diag/artifact-workspace/{workspaceName}/file",
            "Read or save one generated source/docs file by relative path so the AI and user can iterate before zipping.",
            "GET query: path=relative/path. POST JSON: relativePath, content.",
            "Writes only text-like files inside the selected generated workspace. Do not launch generated programs, scripts, installers, or solutions; present a user-approved action with system-impact summary instead.",
            false,
            false),
        new(
            "council.artifact_workspace_zip",
            "GET",
            "/__diag/artifact-workspace/{workspaceName}/zip",
            "Refresh the downloadable zip from the current generated source workspace after edits.",
            "workspaceName from council.artifact_workspaces.",
            "Creates a zip under CouncilArtifacts and returns /__artifacts/council/ download links. Zipping is separate from editing and never executes generated code.",
            false,
            false),
        new(
            "chat.upload_workspaces",
            "GET",
            "/__diag/chat-upload-workspaces",
            "List per-prompt DXAiChat upload workspaces, including the latest uploaded files, root path, and read-only routes.",
            "take optional, defaults to 20.",
            "Read-only. Use this when a user attached files with the DXAiChat native paperclip attachment control."),
        new(
            "chat.upload_workspace_files",
            "GET",
            "/__diag/chat-upload-workspace/{workspaceName}/files",
            "List original uploaded files, safely extracted zip entries, generated context.md, and manifest.json for one chat upload workspace.",
            "workspaceName from chat.upload_workspaces; take optional.",
            "Read-only. Files may include source, docs, zip entries, PDBs, DLLs, and other binaries; binaries are not executed."),
        new(
            "chat.upload_workspace_context",
            "GET",
            "/__diag/chat-upload-workspace/{workspaceName}/context",
            "Read the bounded Markdown context generated from uploaded files for the current prompt.",
            "workspaceName from chat.upload_workspaces; maxCharacters optional.",
            "Read-only. Prefer this compact context over asking the user to paste a whole archive."),
        new(
            "chat.upload_workspace_file",
            "GET",
            "/__diag/chat-upload-workspace/{workspaceName}/file",
            "Read one uploaded or extracted file by relative path, with text decoding or bounded printable binary/PDB strings.",
            "GET query: path=relative/path. maxCharacters optional.",
            "Read-only. Never execute uploaded binaries, scripts, installers, generated apps, or extracted commands."),
        new(
            "localgpt.projects",
            "GET",
            "/api/projects",
            "List user-created LocalGPT project records with purpose, recorded path text, version, status, and topic/version counts.",
            "includeArchived optional and false by default.",
            "Read-only metadata. A recorded path is context only and does not authorize file access, execution, or Git operations."),
        new(
            "localgpt.project",
            "GET",
            "/api/projects/{projectId}",
            "Read one user-created LocalGPT project and its approved topics and version history.",
            "projectId: required GUID from localgpt.projects.",
            "Read-only metadata. Do not access the recorded path or link knowledge without a separate current user action."),
        new(
            "council.run",
            "POST",
            "/__diag/council",
            "Run the LocalGPT AI Council backend with an explicit MultiModelCouncilRequest. Run only the current user-supplied council request.",
            "JSON body: model names, prompt, token limits, CPU/GPU options, and artifact flags.",
            "Runs model inference and may save memory. It requires the user's current request; artifact creation and project linking require separate confirmation flags.",
            false,
            false)
    ];

        /// <summary>
        /// Gets functions.
        /// </summary>
        public IReadOnlyList<DxaichatFunctionInfo> GetFunctions()
        {
    try
    {
                var functions = Functions
                    .Concat(_functionRegistry.GetFunctions())
                    .GroupBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Last())
                    .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _logger.LogDebug("Built LocalGPT function briefing catalog with {FunctionCount} route and DI function descriptor(s).", functions.Count);
                return functions;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(DevExpressChatService)}.{nameof(GetFunctions)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(DevExpressChatService)}.{nameof(GetFunctions)} failed.");
        throw;
    }
}

        /// <summary>
        /// Builds prompt briefing.
        /// </summary>
        public string BuildPromptBriefing()
        {
    try
    {
                return string.Join(Environment.NewLine, GetFunctions()
                    .Where(function => function.AvailableToAi)
                    .Select(function =>
                    {
                        var directInvocation = function.SupportsDirectInvocation ? "supported" : "route-specific";
                        var automaticUse = function.RequiresHumanConfirmation && function.SupportsDeferredApprovalRequest
                            ? "exact approval request may be queued; execution deferred"
                            : function.SupportsAutomaticInvocation
                                ? function.IsCoordinationOnly ? "coordination-only supported" : "read-only supported"
                                : "not allowed";
                        var confirmation = function.RequiresHumanConfirmation
                            ? "required and one-use"
                            : function.IsCoordinationOnly
                                ? "not required for bounded feedback/guidance coordination"
                                : "not required for this read-only operation";
                        return $"- {function.Name}: {function.Method} {function.Route} — {function.Purpose} " +
                            $"Parameters: {function.Parameters} Safety: {function.SafetyNotes} " +
                            $"Direct invocation: {directInvocation}; automatic use: {automaticUse}; " +
                            $"fresh confirmation: {confirmation}.";
                    }));
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(DevExpressChatService)}.{nameof(BuildPromptBriefing)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(DevExpressChatService)}.{nameof(BuildPromptBriefing)} failed.");
        throw;
    }
}
        /// <summary>
        /// Builds title.
        /// </summary>
        public string BuildTitle(IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                var firstUserMessage = messages.FirstOrDefault(message => message.Role == ChatMessageRole.User)?.Content
                ?? messages.First().Content;
                var title = _catalog.WhitespacePattern.Replace(_text.StripThinking(firstUserMessage,logger), " ").Trim();

                if (string.IsNullOrWhiteSpace(title))
                    return "New conversation";

                return title.Length <= 90 ? title : $"{title[..87].TrimEnd()}...";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not build a conversation title from {MessageCount} message(s).", messages.Count);
                return string.Empty;
            }
        }
        /// <summary>
        /// Ensures visible council prompt.
        /// </summary>
        public List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
    ChatMemoryConversation conversation,
    List<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                if (messages.Count == 0 ||
               messages.Any(message => message.Role == ChatMessageRole.User && !string.IsNullOrWhiteSpace(message.Content)))
                {
                    return messages;
                }

                if (!IsCouncilConversation(conversation, messages,logger))
                    return messages;

                var prompt = TryExtractPromptFromAssistantMessages(messages, logger)
                    ?? _text.TryRecoverPromptFromTitle(conversation.Title, logger);
                if (string.IsNullOrWhiteSpace(prompt))
                    return messages;

                messages.Insert(0, new BlazorChatMessage(
                    ChatRole.User,
                    prompt,
                    /// <summary>
                    /// Runs the list operation.
                    /// </summary>
                    new List<AIChatUploadFileInfo>()));
                return messages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not ensure a visible council prompt for conversation {ConversationId}; message count {MessageCount}.", conversation.Id, messages.Count);
                return new();
            }
        }
        /// <summary>
        /// Runs the to role name operation.
        /// </summary>
        public string ToRoleName(ChatMessageRole role, ILogger logger)
        {
            try
            {
                return role switch
                {
                    ChatMessageRole.Assistant => "assistant",
                    ChatMessageRole.System => "system",
                    ChatMessageRole.Error => "error",
                    _ => "user"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToRoleName role {role.ToString()}");
                return string.Empty;
            }
        }
        /// <summary>
        /// Attempts to extract prompt from assistant messages.
        /// </summary>
        public string? TryExtractPromptFromAssistantMessages(IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                foreach (var message in messages)
                {
                    var content = WebUtility.HtmlDecode(message.Content);
                    var promptSection = _text.TryFindCouncilPromptSection(content, logger);
                    if (!string.IsNullOrWhiteSpace(promptSection))
                    {
                        var fencedPrompt = _catalog.CouncilPromptFencePattern.Match(promptSection);
                        if (fencedPrompt.Success)
                            return _text.NormalizeRecoveredPrompt(fencedPrompt.Groups["prompt"].Value, logger);
                    }

                    var requestBlock = _catalog.CouncilRequestBlockPattern.Match(content);
                    if (requestBlock.Success)
                        return _text.NormalizeRecoveredPrompt(requestBlock.Groups["prompt"].Value, logger);
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not extract a council prompt from {MessageCount} assistant message(s).", messages.Count);
                return null;
            }
        }
        /// <summary>
        /// Runs the to blazor chat message operation.
        /// </summary>
        public BlazorChatMessage? ToBlazorChatMessage(ChatMemoryMessage message, ILogger logger)
        {
            try
            {
                return new BlazorChatMessage(new ChatRole(message.Role), message.Content, new List<AIChatUploadFileInfo>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not convert a chat message; message content was omitted from logs.");
                return null;
            }
        }
        /// <summary>
        /// Determines whether council conversation.
        /// </summary>
        public bool IsCouncilConversation(
    ChatMemoryConversation conversation,
    IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                return conversation.ProviderName.Contains("AI Council", StringComparison.OrdinalIgnoreCase) ||
                conversation.Title.Contains("AI Council request", StringComparison.OrdinalIgnoreCase) ||
                conversation.Title.Contains("Council members:", StringComparison.OrdinalIgnoreCase) ||
                messages.Any(message => message.Content.Contains("Council members:", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify conversation {ConversationId} as a council conversation; message count {MessageCount}.", conversation.Id, messages.Count);
                return new();
            }
        }
    }
}
