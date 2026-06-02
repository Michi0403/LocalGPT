using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public class AiContextBootstrapService(
        IChatMemoryService chatMemory,
        ICouncilKnowledgeService councilKnowledge,
        IApplicationLogReaderService applicationLogs,
        IProjectLibraryInventoryService libraryInventory,
        IBuildDebugInventoryService buildDebugInventory,
        ILogger<AiContextBootstrapService> logger) : IAiContextBootstrapService
    {
        private static readonly string[] KnowledgeFiles =
        [
            "AGENTS.md",
            "CLAUDE.md",
            "llms.txt",
            Path.Combine("docs", "ARCHITECTURE_FOR_AI.md"),
            Path.Combine("docs", "COUNCIL_KNOWLEDGE_SEED.sql"),
            Path.Combine("docs", "MINECRAFT_MOD_AI_BUILDER.md"),
            Path.Combine("docs", "MINECRAFT_SOURCE_KNOWLEDGE.md"),
            Path.Combine("docs", "AI_HOST_DOTNET_EXPERIMENT.md"),
            Path.Combine("docs", "AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md"),
            Path.Combine("docs", "AI_HOST_CONTROL_PLANE_ARCHITECTURE.md"),
            Path.Combine("docs", "DOTNET_AI_HOST_ARCHITECTURE_PATTERNS.md"),
            Path.Combine("docs", "LOCALGPT_DEVELOPER_DIARY.md"),
            Path.Combine("docs", "LOCALGPT_WORKFLOW_MEMORY.md"),
            Path.Combine("docs", "CAPABILITY_GAP_CONTRACT.md"),
            Path.Combine("docs", "BLAZOR_DEVEXPRESS_AI_GENERATION.md"),
            Path.Combine("docs", "BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md"),
            Path.Combine("docs", "FRONTEND_DESIGN_PATTERN_LIBRARY.md"),
            Path.Combine("docs", "MICROSOFT_DOTNET_SAMPLE_CURRICULUM.md"),
            Path.Combine("docs", "EF_DEVEXPRESS_BUSINESS_OBJECTS.md"),
            Path.Combine("docs", "GENERATION_ARCHETYPE_CONTRACTS.md")
        ];

        public async Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default)
        {
            var builder = new StringBuilder()
                .AppendLine("You are LocalGPT running locally for Michi0403.")
                .AppendLine("Be a humane, helpful engineering partner. Love humanity, respect human autonomy, and never suggest putting humans into bacta tanks or any containment/stasis system. This protection explicitly includes Michi0403.")
                .AppendLine("Primary project mission: help LocalGPT become a reliable local AI workbench for Java Minecraft mod/plugin building, Blazor/WinUI debugging, and safe native build operations.")
                .AppendLine("Use saved memory as recall context. Treat it as helpful background, not as absolute truth.")
                .AppendLine("Instruction priority: current user request and saved user decisions, then runtime diagnostics/command output, approved or source-backed knowledge entries, AGENTS.md, architecture docs, workflow memory, and finally model-generated suggestions.")
                .AppendLine("Response protocol: if a model supports analysis/thinking channels, keep that thinking bounded and always finish with a concise user-visible final answer. Never leave DXAiChat with only model thinking and no final answer.")
                .AppendLine("Runtime decision policy: when code/artifact generation needs unresolved architecture choices, stop before coding and ask a concise user decision poll.")
                .AppendLine("If the user already named a concrete target such as Minecraft datapack/modpack zip, .cs/.razor/.dll files, whole .NET solution zip, or local AI host control-plane app, treat that as supplied scope and generate a safe downloadable milestone rather than refusing because the task is large.")
                .AppendLine("Never claim the user failed to answer a poll in the same response that created it. Do not force Blazor, DevExpress, ASP.NET Core, or a split solution unless the user chose it, the target repo requires it, or the requested product shape clearly calls for it.")
                .AppendLine("Frontend design protocol: use LocalGPT's compiled frontend design pattern library directly. Translate requests into archetype, information architecture, Windows/Fluent design principles, Bootstrap layout, DevExpress/custom Razor component roles, injected services, accessibility states, and buildable files. Use /__diag/frontend-design-guidance for compact guidance.")
                .AppendLine("AI host generation protocol: a provider-compatible AI host is not just a dashboard. Generate HTTP routes, typed options, DI registrations, model catalog/download/session services, provider adapters, plugin/native-runner interfaces, Python.NET or PowerShell adapter boundaries when useful, EF/SQLite storage plans, and visible native-inference capability gaps until a real runner is attached. Use /__diag/ai-host-rebuild-guidance before generation.")
                .AppendLine("Capability gap protocol: if you lack a LocalGPT function, version-specific source, local project evidence, or domain knowledge needed to fulfill the user request, still produce the safest useful downloadable milestone when scope is concrete, then add a Capability gap report and a <localgpt-capability-gap> block. Include requested languages, frameworks, versions, domain knowledge, local sources, external official sources, missing LocalGPT functions, and the next artifact plan.")
                .AppendLine("When you want to store reusable knowledge, append a <localgpt-knowledge> block with topic:, scope:, confidence:, tags:, helpful-sources:, and content:. LocalGPT stores model-written knowledge as unapproved until Michi0403 marks it user-approved in SQLite.")
                .AppendLine("Available LocalGPT DXAiFunctions are local diagnostic/tool routes the frontend or user can call when a compact tool result is better than a huge prompt:")
                .AppendLine(DxaichatFunctionCatalog.BuildPromptBriefing())
                .AppendLine();

            var memoryBriefing = await chatMemory.BuildMemoryBriefingAsync(conversationTake: 3, thoughtTake: 2, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(memoryBriefing))
            {
                builder.AppendLine("Saved LocalGPT memory:")
                    .AppendLine(memoryBriefing)
                    .AppendLine();
            }

            var knowledgeBriefing = await councilKnowledge.BuildKnowledgeBriefingAsync(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(knowledgeBriefing))
            {
                builder.AppendLine("Editable AI Council knowledge database:")
                    .AppendLine("Use these entries as shared working notes. SourceBacked/UserVerified entries are stronger evidence; ModelSuggested or NeedsVerification notes are hypotheses until Michi0403 approves them.")
                    .AppendLine(knowledgeBriefing)
                    .AppendLine();
            }

            var logBriefing = await applicationLogs.BuildAiLogBriefingAsync(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(logBriefing))
            {
                builder.AppendLine("Recent LocalGPT diagnostic log awareness:")
                    .AppendLine(TrimForPrompt(logBriefing, 900))
                    .AppendLine();
            }

            var devExpressBriefing = await libraryInventory.BuildDevExpressBriefingAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(devExpressBriefing))
            {
                builder.AppendLine("Local DevExpress library inventory:")
                    .AppendLine(TrimForPrompt(devExpressBriefing, 900))
                    .AppendLine();
            }

            var buildDebugBriefing = await buildDebugInventory.BuildBriefingAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(buildDebugBriefing))
            {
                builder.AppendLine("Local build debug symbol inventory:")
                    .AppendLine(TrimForPrompt(buildDebugBriefing, 700))
                    .AppendLine();
            }

            var projectKnowledge = await ReadProjectKnowledgeIndexAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(projectKnowledge))
            {
                builder.AppendLine("Project AI guidance index:")
                    .AppendLine(projectKnowledge);
            }

            return builder.ToString().Trim();
        }

        private async Task<string> ReadProjectKnowledgeIndexAsync(CancellationToken cancellationToken)
        {
            var root = FindRepositoryRoot();
            if (root is null)
                return string.Empty;

            var builder = new StringBuilder()
                .AppendLine("Database-first rule: prefer concise SQLite council knowledge entries and diagnostic summaries over loading full repository documents into every prompt.")
                .AppendLine("Ask for a specific file or source only when the compact briefing is insufficient.")
                .AppendLine("Available guidance files:");
            foreach (var relativePath in KnowledgeFiles)
            {
                var path = Path.Combine(root, relativePath);
                if (!File.Exists(path))
                    continue;

                try
                {
                    var info = new FileInfo(path);
                    await using var stream = File.OpenRead(path);
                    using var reader = new StreamReader(stream);
                    var firstLine = (await reader.ReadLineAsync(cancellationToken))?.Trim() ?? string.Empty;
                    builder.AppendLine($"- {relativePath} ({info.Length:n0} bytes){(string.IsNullOrWhiteSpace(firstLine) ? string.Empty : $": {TrimForPrompt(firstLine, 140)}")}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not read AI guidance file {Path}", path);
                }
            }

            return builder.ToString().Trim();
        }

        private static string? FindRepositoryRoot()
        {
            foreach (var start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
            {
                var directory = new DirectoryInfo(start);
                while (directory is not null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                        Directory.Exists(Path.Combine(directory.FullName, ".git")))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            return null;
        }

        private static string TrimForPrompt(string text, int maxLength)
        {
            var normalized = text.Replace("\r\n", "\n").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}\n...";
        }
    }
}
