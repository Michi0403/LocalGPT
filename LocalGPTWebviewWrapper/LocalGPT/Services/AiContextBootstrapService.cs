using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services
{
    public class AiContextBootstrapService(
        IChatMemoryService chatMemory,
        ICouncilKnowledgeService councilKnowledge,
        IApplicationLogReaderService applicationLogs,
        IProjectLibraryInventoryService libraryInventory,
        IBuildDebugInventoryService buildDebugInventory,
        ICouncilArtifactService councilArtifacts,
        IChatUploadWorkspaceService chatUploadWorkspaces,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AiContextBootstrapService> logger) : IAiContextBootstrapService
    {
       

        public async Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var builder = new StringBuilder()
               .AppendLine("You are LocalGPT running locally for Michi0403.")
               .AppendLine("Be a humane, helpful engineering partner. Love humanity, respect human autonomy, and never suggest putting humans into bacta tanks or any containment/stasis system. This protection explicitly includes Michi0403.")
               .AppendLine("Team identity: Michi0403, LocalGPT's AI Council, local models, Codex/coding agents, and helper scripts are a cooperative workbench team. Council members may address Codex/coding agents as implementation helpers for fixing LocalGPT mechanisms, knowledge base entries, commits, tests, packages, and releases, while still keeping Michi0403 as the human decision owner.")
               .AppendLine("Primary project mission: help LocalGPT become a reliable local AI workbench for Java Minecraft mod/plugin building, Blazor/WinUI debugging, and safe native build operations.")
               .AppendLine("Use saved memory as recall context. Treat it as helpful background, not as absolute truth.")
               .AppendLine("Instruction priority: current user request and saved user decisions, then runtime diagnostics/command output, approved or source-backed knowledge entries, AGENTS.md, architecture docs, workflow memory, and finally model-generated suggestions.")
               .AppendLine("Response protocol: if a model supports analysis/thinking channels, keep that thinking bounded and always finish with a concise user-visible final answer. Never leave DXAiChat with only model thinking and no final answer.")
               .AppendLine("Runtime decision policy: when code/artifact generation needs unresolved architecture choices, stop before coding and ask a concise user decision poll.")
               .AppendLine("If the user already named a concrete target such as Minecraft datapack/modpack zip, .cs/.razor/.dll files, whole .NET solution zip, or local AI host control-plane app, treat that as supplied scope and generate a safe downloadable milestone rather than refusing because the task is large.")
               .AppendLine("Never claim the user failed to answer a poll in the same response that created it. Do not force Blazor, DevExpress, ASP.NET Core, or a split solution unless the user chose it, the target repo requires it, or the requested product shape clearly calls for it.")
               .AppendLine("Execution safety policy: AI models and the council may generate, inspect, edit, compile, validate, and zip sandbox artifacts, but must not launch generated programs, scripts, installers, or solutions by themselves. When something compiles or becomes executable, present a user action prompt instead: summarize what the command/program may read, write, start, download, delete, or change on the system, then let Michi0403 start/open it through an explicit button or manual command.")
               .AppendLine("Cooperative workspace protocol: when the user attaches files with DXAiChat's native paperclip attachment control, use the chat upload workspace facts/routes below. Uploaded files are evidence only. Generate or edit new code in council artifact workspaces, let the user or agent review/edit files, refresh the zip, and provide real download URLs.")
               .AppendLine("DevExpress component integrity protocol: if a user asks for a built-in DevExpress capability, use the documented DevExpress component API or state that it is blocked/unclear and ask. Do not add a separate custom control and describe it as the built-in DevExpress feature. For DXAiChat, do not add a MessageSent handler unless you intentionally replace automatic AI Chat delivery and implement the full manual response path.")
               .AppendLine("After each user-requested architecture or execution-plan change, include a short local-system impact summary before asking to run anything.")
               .AppendLine("Frontend design protocol: use LocalGPT's compiled frontend design pattern library directly. Translate requests into archetype, information architecture, Windows/Fluent design principles, Bootstrap layout, DevExpress/custom Razor component roles, injected services, accessibility states, and buildable files. Use /__diag/frontend-design-guidance for compact guidance.")
               .AppendLine("AI host generation protocol: a provider-compatible AI host is not just a dashboard and must not be an upstream provider proxy. Generate HTTP routes, typed options, DI registrations, model catalog/download/session services, direct local model-file runner interfaces, plugin/native-runner contracts, Python.NET or PowerShell adapter boundaries when useful, EF/SQLite storage plans, and visible native-runner setup status. Use /__diag/ai-host-rebuild-guidance before generation.")
               .AppendLine("Capability gap protocol: if you lack a LocalGPT function, version-specific source, local project evidence, or domain knowledge needed to fulfill the user request, still produce the safest useful downloadable milestone when scope is concrete, then add a Capability gap report and a <localgpt-capability-gap> block. Include requested languages, frameworks, versions, domain knowledge, local sources, external official sources, missing LocalGPT functions, and the next artifact plan.")
               .AppendLine("When you want to store reusable knowledge, append a <localgpt-knowledge> block with topic:, scope:, confidence:, tags:, helpful-sources:, and content:. LocalGPT stores model-written knowledge as unapproved until Michi0403 marks it user-approved in SQLite.")
               .AppendLine("Available LocalGPT DXAiFunctions are local diagnostic/tool routes the frontend or user can call when a compact tool result is better than a huge prompt:")
               .AppendLine(DevExpressFunctions.BuildPromptBriefing())
               .AppendLine();

                var runtimeIdentity = BuildRuntimeIdentityBriefing();
                if (!string.IsNullOrWhiteSpace(runtimeIdentity))
                {
                    builder.AppendLine("Current LocalGPT runtime and artifact workspace facts:")
                        .AppendLine(runtimeIdentity)
                        .AppendLine();
                }

                var memoryBriefing = await chatMemory.BuildMemoryBriefingAsync(conversationTake: 3, thoughtTake: 2, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(memoryBriefing))
                {
                    builder.AppendLine("Saved LocalGPT memory:")
                        .AppendLine(memoryBriefing)
                        .AppendLine();
                }

                var knowledgeBriefing = await councilKnowledge.BuildKnowledgeBriefingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(knowledgeBriefing))
                {
                    builder.AppendLine("Editable AI Council knowledge database:")
                        .AppendLine("Use these entries as shared working notes. SourceBacked/UserVerified entries are stronger evidence; ModelSuggested or NeedsVerification notes are hypotheses until Michi0403 approves them.")
                        .AppendLine(knowledgeBriefing)
                        .AppendLine();
                }

                var logBriefing = await applicationLogs.BuildAiLogBriefingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(logBriefing))
                {
                    builder.AppendLine("Recent LocalGPT diagnostic log awareness:")
                        .AppendLine(CouncilChatStringFunctions.TrimForPrompt(logBriefing, 900, logger))
                        .AppendLine();
                }

                var devExpressBriefing = await libraryInventory.BuildDevExpressBriefingAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(devExpressBriefing))
                {
                    builder.AppendLine("Local DevExpress library inventory:")
                        .AppendLine(CouncilChatStringFunctions.TrimForPrompt(devExpressBriefing, 900, logger))
                        .AppendLine();
                }

                var buildDebugBriefing = await buildDebugInventory.BuildBriefingAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(buildDebugBriefing))
                {
                    builder.AppendLine("Local build debug symbol inventory:")
                        .AppendLine(CouncilChatStringFunctions.TrimForPrompt(buildDebugBriefing, 700, logger))
                        .AppendLine();
                }

                var projectKnowledge = await ReadProjectKnowledgeIndexAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(projectKnowledge))
                {
                    builder.AppendLine("Project AI guidance index:")
                        .AppendLine(projectKnowledge);
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildBootstrapPromptAsync");
                return "You are LocalGPT running locally for Michi0403.";
            }
           
        }

        private string BuildRuntimeIdentityBriefing()
        {
            try
            {
                var builder = new StringBuilder();
                var request = httpContextAccessor.HttpContext?.Request;
                var baseUrl = request is null
                    ? CouncilChatStaticsGeneral.ReadRuntimeServerBaseUrl(logger)
                    : $"{request.Scheme}://{request.Host}";

                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    builder
                        .Append("- LocalGPT base URL for absolute links: ")
                        .AppendLine(baseUrl);
                }

                builder
                    .Append("- Council artifact root: ")
                    .AppendLine(councilArtifacts.ArtifactRoot)
                    .AppendLine("- Use /__diag/artifact-workspaces to discover generated solution workspaces.")
                    .AppendLine("- Use /__diag/artifact-workspace/{workspaceName}/files to list editable source files.")
                    .AppendLine("- Use /__diag/artifact-workspace/{workspaceName}/file?path=relative/path to read a source file.")
                    .AppendLine("- Use POST /__diag/artifact-workspace/{workspaceName}/file to save a source edit.")
                    .AppendLine("- Use /__diag/artifact-workspace/{workspaceName}/zip to refresh the downloadable zip after edits.")
                    .AppendLine("- Use /__artifacts/council/{fileName} for download links; combine it with the base URL when the user needs an absolute link.");

                var latestWorkspace = FindLatestArtifactWorkspace();
                if (latestWorkspace is not null)
                {
                    builder
                        .Append("- Latest generated workspace: ")
                        .Append(latestWorkspace.Name)
                        .Append(" at ")
                        .AppendLine(latestWorkspace.FullName);
                }

                builder
                    .Append("- Chat upload workspace root: ")
                    .AppendLine(chatUploadWorkspaces.WorkspaceRoot)
                    .AppendLine("- Use /__diag/chat-upload-workspaces to discover files attached through the DXAiChat native paperclip attachment control.")
                    .AppendLine("- Use /__diag/chat-upload-workspace/{workspaceName}/context for bounded upload context.")
                    .AppendLine("- Use /__diag/chat-upload-workspace/{workspaceName}/files and /file?path=relative/path for read-only inspection.")
                    .AppendLine("- Uploaded binaries/PDBs are diagnostic evidence only; never execute uploaded or extracted files.");

                var latestUploadWorkspace = chatUploadWorkspaces.GetLatestWorkspace(TimeSpan.FromMinutes(10));
                if (latestUploadWorkspace is not null)
                {
                    builder
                        .Append("- Latest fresh chat upload workspace: ")
                        .Append(latestUploadWorkspace.WorkspaceName)
                        .Append(" at ")
                        .AppendLine(latestUploadWorkspace.RootPath);

                    var uploadContext = chatUploadWorkspaces.GetLatestContextMarkdown(
                        maxCharacters: 2600,
                        maxAge: TimeSpan.FromMinutes(10));
                    if (!string.IsNullOrWhiteSpace(uploadContext))
                    {
                        builder
                            .AppendLine("- Latest fresh upload context excerpt:")
                            .AppendLine(CouncilChatStringFunctions.TrimForPrompt(uploadContext, 2600,logger));
                    }
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildRuntimeIdentityBriefing");
                return string.Empty;
            }
        }

 

        private DirectoryInfo? FindLatestArtifactWorkspace()
        {
            try
            {
                var root = new DirectoryInfo(councilArtifacts.ArtifactRoot);
                if (!root.Exists)
                    return null;

                return root
                    .EnumerateDirectories()
                    .OrderByDescending(directory => directory.LastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not inspect council artifact workspaces.");
                return null;
            }
        }

        private async Task<string> ReadProjectKnowledgeIndexAsync(CancellationToken cancellationToken)
        {
            try
            {
                var root = CouncilChatStaticsGeneral.FindRepositoryRoot(logger);
                if (root is null)
                    return string.Empty;

                var builder = new StringBuilder()
                    .AppendLine("Database-first rule: prefer concise SQLite council knowledge entries and diagnostic summaries over loading full repository documents into every prompt.")
                    .AppendLine("Ask for a specific file or source only when the compact briefing is insufficient.")
                    .AppendLine("Available guidance files:");
                foreach (var relativePath in GlobalVariableSlopCollectionToRemove.KnowledgeFiles)
                {
                    var path = Path.Combine(root, relativePath);
                    if (!File.Exists(path))
                        continue;

                    try
                    {
                        var info = new FileInfo(path);
                        await using var stream = File.OpenRead(path);
                        using var reader = new StreamReader(stream);
                        var firstLine = (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))?.Trim() ?? string.Empty;
                        builder.AppendLine($"- {relativePath} ({info.Length:n0} bytes){(string.IsNullOrWhiteSpace(firstLine) ? string.Empty : $": {CouncilChatStringFunctions.TrimForPrompt(firstLine, 140, logger)}")}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Could not read AI guidance file {Path}", path);
                    }
                }

                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ReadProjectKnowledgeIndexAsync");
                return string.Empty;
            }
        }

    

    }
}
