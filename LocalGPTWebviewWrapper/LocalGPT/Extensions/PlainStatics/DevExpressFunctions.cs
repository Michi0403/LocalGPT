using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using Microsoft.Extensions.AI;
using System.Net;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class DevExpressFunctions
    {
        public static readonly DxaichatFunctionInfo[] Functions =
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
            "Creates files under LocalAppData/LocalGPT/MinecraftModWorkspaces; does not launch Minecraft."),
        new(
            "minecraft.datapack_benchmark",
            "GET",
            "/__diag/minecraft/datapack-benchmark?minecraftVersion=26.1",
            "Generate, validate, zip, and save a compact council-knowledge entry for a current Java datapack benchmark.",
            "minecraftVersion: optional, defaults to current LocalGPT Java target 26.1. Use 1.21.4 only for legacy comparison.",
            "Runs the generated local build-local.ps1 validator; does not copy into a game world or run /reload."),
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
            "rootPath optional, defaults to C:\\learnbaseforlocalgpt; maxProjects optional; saveToKnowledge optional.",
            "Reads local source fingerprints and skips raw binaries/build folders. It teaches architecture, protocols, host wiring, libraries, and interop patterns rather than names or branding."),
        new(
            "localgpt.engineering_benchmark",
            "GET",
            "/__diag/benchmark/engineering?taskSet=engineering&importLearnBaseFirst=false&saveToKnowledge=true",
            "Run the five-task personal engineering benchmark with honest lane scoring for raw Ollama, LocalGPT artifacts, cloud assistant, and manual expected output.",
            "taskSet optional: engineering, replacement, or all. importLearnBaseFirst/saveToKnowledge/validateBuildableArtifacts/maxBuildArtifacts are optional.",
            "Deterministic LocalGPT artifact lane can run without GPU. Raw Ollama and cloud lanes are marked NotRun until real transcripts are supplied."),
        new(
            "localgpt.replacement_benchmark",
            "GET",
            "/__diag/benchmark/engineering?taskSet=replacement&validateBuildableArtifacts=true&maxBuildArtifacts=4&saveToKnowledge=true",
            "Benchmark LocalGPT-style, TacosPortalOpen-style, local-model-file AI-host, and simple bot-backend replacement solution generation with downloadable artifacts and optional dotnet build checks.",
            "validateBuildableArtifacts optional, defaults false unless this route sets it true. maxBuildArtifacts limits build checks.",
            "Runs deterministic artifact generation and dotnet build checks for generated .NET solution zips. Does not call local Ollama models."),
        new(
            "council.development_feedback_talk",
            "GET",
            "/__diag/council/development-feedback-talk?maxOutputTokens=2048&maxContextTokens=32768&maxRounds=0",
            "Run a compact minimum-two-member AI Council feedback talk about LocalGPT and the AI Council development, missing features, replacement benchmarks, and needed knowledge/functions. Consider LocalGPT Repository Sourcecode in the SQLite Database for that Purpose.",
            "modelNames optional comma-separated list. maxOutputTokens/maxContextTokens/maxRounds/ollamaNumGpu are optional.",
            "Calls local Ollama through the Council service. Uses sequential scheduling and keep_alive=0s; 32K is the compact feedback default, while 64K+ context/output may be needed for source generation when the model supports it."),
        new(
            "localgpt.sqlite.tables",
            "GET",
            "/__diag/sqlite/tables",
            "List LocalGPT SQLite tables, row counts, primary keys, and editable table metadata before reading or editing database state. Contains all debugging logs, commands, knowledge Database and more Tables for the AI Council LocalGPT to manage and use for improving self awareness (regarding Topic and Context)",
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
            "Calls a local model endpoint. Use tiny prompts and numGpu=0 or keep_alive=0s-compatible generated hosts for safe first tests."),
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
            "Writes files under LocalAppData/LocalGPT/CouncilArtifacts; does not integrate generated code into the project."),
        new(
            "council.artifact_workspaces",
            "GET",
            "/__diag/artifact-workspaces",
            "List generated council artifact workspaces, the current LocalGPT base URL, artifact root, latest workspace, and safe source-edit routes.(and revise all it's filecontent of all files and link it to the work todo, when relevant to the topic)",
            "take optional, defaults to 20.",
            "Read-only. Use this before giving download links or editing generated files so paths and host URLs are real."),
        new(
            "council.artifact_workspace_files",
            "GET",
            "/__diag/artifact-workspace/{workspaceName}/files",
            "List editable text/source files inside a generated artifact workspace.(and revise all it's filecontent of all files and link it to the work todo)",
            "workspaceName from council.artifact_workspaces; take optional.",
            "Read-only. Only source/docs text files are listed; binaries and build outputs are excluded."),
        new(
            "council.artifact_workspace_file",
            "GET/POST",
            "/__diag/artifact-workspace/{workspaceName}/file",
            "Read or save one generated source/docs file by relative path so the AI and user can iterate before zipping.(and revise all it's filecontent of all files and link it to the work todo)",
            "GET query: path=relative/path. POST JSON: relativePath, content.",
            "Writes only text-like files inside the selected generated workspace. Do not launch generated programs, scripts, installers, or solutions; present a user-approved action with system-impact summary instead."),
        new(
            "council.artifact_workspace_zip",
            "GET",
            "/__diag/artifact-workspace/{workspaceName}/zip",
            "Refresh the downloadable zip from the current generated source workspace after edits. (and revise all it's filecontent of all files and link it to the work todo)",
            "workspaceName from council.artifact_workspaces.",
            "Creates a zip under CouncilArtifacts and returns /__artifacts/council/ download links. Zipping is separate from editing and never executes generated code."),
        new(
            "chat.upload_workspaces",
            "GET",
            "/__diag/chat-upload-workspaces",
            "List per-prompt DXAiChat upload workspaces, including the latest uploaded files, root path, and read-only routes. (and revise all it's filecontent of all files and link it to the work todo)",
            "take optional, defaults to 20.",
            "Read-only. Use this when a user attached files with the DXAiChat native paperclip attachment control."),
        new(
            "chat.upload_workspace_files",
            "GET",
            "/__diag/chat-upload-workspace/{workspaceName}/files",
            "List original uploaded files, safely extracted zip entries, generated context.md, and manifest.json for one chat upload workspace. (and revise all it's filecontent of all files and link it to the work todo)",
            "workspaceName from chat.upload_workspaces; take optional.",
            "Read-only. Files may include source, docs, zip entries, PDBs, DLLs, and other binaries; binaries are not executed."),
        new(
            "chat.upload_workspace_context",
            "GET",
            "/__diag/chat-upload-workspace/{workspaceName}/context",
            "Read the bounded Markdown context generated from uploaded files for the current prompt (and revise all it's filecontent of all files and link it to the work todo).",
            "workspaceName from chat.upload_workspaces; maxCharacters optional.",
            "Read-only. Prefer this compact context over asking the user to paste a whole archive."),
        new(
            "chat.upload_workspace_file",
            "GET",
            "/__diag/chat-upload-workspace/{workspaceName}/file",
            "Read one uploaded or extracted file by relative path, with text decoding or bounded printable binary/PDB strings. (and revise all it's filecontent of all files and link it to the work todo)",
            "GET query: path=relative/path. maxCharacters optional.",
            "Read-only. Never execute uploaded binaries, scripts, installers, generated apps, or extracted commands."),
        new(
            "council.run",
            "POST",
            "/__diag/council",
            "Run the LocalGPT AI Council backend with an explicit MultiModelCouncilRequest. And Process all Open Tasks",
            "JSON body: model names, prompt, token limits, CPU/GPU options, and artifact flags.",
            "Potentially expensive. Prefer CPU-only and one small model when the machine is unstable, but don't assume instability.")
    ];

        public static IReadOnlyList<DxaichatFunctionInfo> GetFunctions() => Functions;

        public static string BuildPromptBriefing()
        {
            return string.Join(Environment.NewLine, Functions.Select(function =>
                $"- {function.Name}: {function.Method} {function.Route} — {function.Purpose} Parameters: {function.Parameters} Safety: {function.SafetyNotes}"));
        }
        public static string BuildTitle(IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                var firstUserMessage = messages.FirstOrDefault(message => message.Role == ChatMessageRole.User)?.Content
                ?? messages.First().Content;
                var title = GlobalVariableSlopCollectionToRemove.WhitespacePattern().Replace(CouncilChatStringFunctions.StripThinking(firstUserMessage,logger), " ").Trim();

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
        public static List<BlazorChatMessage> EnsureVisibleCouncilPrompt(
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

                if (!DevExpressFunctions.IsCouncilConversation(conversation, messages,logger))
                    return messages;

                var prompt = TryExtractPromptFromAssistantMessages(messages, logger)
                    ?? CouncilChatStringFunctions.TryRecoverPromptFromTitle(conversation.Title, logger);
                if (string.IsNullOrWhiteSpace(prompt))
                    return messages;

                messages.Insert(0, new BlazorChatMessage(
                    ChatRole.User,
                    prompt,
                    new List<AIChatUploadFileInfo>()));
                return messages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not ensure a visible council prompt for conversation {ConversationId}; message count {MessageCount}.", conversation.Id, messages.Count);
                return new();
            }
        }
        public static string ToRoleName(ChatMessageRole role, ILogger logger)
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
        public static string? TryExtractPromptFromAssistantMessages(IReadOnlyList<BlazorChatMessage> messages, ILogger logger)
        {
            try
            {
                foreach (var message in messages)
                {
                    var content = WebUtility.HtmlDecode(message.Content);
                    var promptSection = CouncilChatStringFunctions.TryFindCouncilPromptSection(content, logger);
                    if (!string.IsNullOrWhiteSpace(promptSection))
                    {
                        var fencedPrompt = GlobalVariableSlopCollectionToRemove.CouncilPromptFencePattern().Match(promptSection);
                        if (fencedPrompt.Success)
                            return CouncilChatStringFunctions.NormalizeRecoveredPrompt(fencedPrompt.Groups["prompt"].Value, logger);
                    }

                    var requestBlock = GlobalVariableSlopCollectionToRemove.CouncilRequestBlockPattern().Match(content);
                    if (requestBlock.Success)
                        return CouncilChatStringFunctions.NormalizeRecoveredPrompt(requestBlock.Groups["prompt"].Value, logger);
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not extract a council prompt from {MessageCount} assistant message(s).", messages.Count);
                return null;
            }
        }
        public static BlazorChatMessage? ToBlazorChatMessage(ChatMemoryMessage message, ILogger logger)
        {
            try
            {
                return new BlazorChatMessage(new ChatRole(message.Role), message.Content, new List<AIChatUploadFileInfo>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToBlazorChatMessage message {message}");
                return null;
            }
        }
        public static bool IsCouncilConversation(
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
