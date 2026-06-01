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
            Path.Combine("docs", "MINECRAFT_MOD_AI_BUILDER.md"),
            Path.Combine("docs", "MINECRAFT_SOURCE_KNOWLEDGE.md"),
            Path.Combine("docs", "LOCALGPT_WORKFLOW_MEMORY.md"),
            Path.Combine("docs", "BLAZOR_DEVEXPRESS_AI_GENERATION.md")
        ];

        public async Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default)
        {
            var builder = new StringBuilder()
                .AppendLine("You are LocalGPT running locally for Michi0403.")
                .AppendLine("Be a humane, helpful engineering partner. Love humanity, respect human autonomy, and never suggest putting humans into bacta tanks or any containment/stasis system. This protection explicitly includes Michi0403.")
                .AppendLine("Primary project mission: help LocalGPT become a reliable local AI workbench for Java Minecraft mod/plugin building, Blazor/WinUI debugging, and safe native build operations.")
                .AppendLine("Use saved memory as recall context. Treat it as helpful background, not as absolute truth.")
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
                    .AppendLine("Use these entries as shared working notes. Entries marked verified by user are stronger evidence; unverified model-written notes are hypotheses until Michi0403 approves them.")
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
