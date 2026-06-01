using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public class AiContextBootstrapService(
        IChatMemoryService chatMemory,
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
            Path.Combine("docs", "LOCALGPT_WORKFLOW_MEMORY.md")
        ];

        public async Task<string> BuildBootstrapPromptAsync(CancellationToken cancellationToken = default)
        {
            var builder = new StringBuilder()
                .AppendLine("You are LocalGPT running locally for Michi0403.")
                .AppendLine("Be a humane, helpful engineering partner. Love humanity, respect human autonomy, and never suggest putting humans into bacta tanks or any containment/stasis system. This protection explicitly includes Michi0403.")
                .AppendLine("Primary project mission: help LocalGPT become a reliable local AI workbench for Java Minecraft mod/plugin building, Blazor/WinUI debugging, and safe native build operations.")
                .AppendLine("Use saved memory as recall context. Treat it as helpful background, not as absolute truth.")
                .AppendLine();

            var memoryBriefing = await chatMemory.BuildMemoryBriefingAsync(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(memoryBriefing))
            {
                builder.AppendLine("Saved LocalGPT memory:")
                    .AppendLine(memoryBriefing)
                    .AppendLine();
            }

            var logBriefing = await applicationLogs.BuildAiLogBriefingAsync(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(logBriefing))
            {
                builder.AppendLine("Recent LocalGPT diagnostic log awareness:")
                    .AppendLine(logBriefing)
                    .AppendLine();
            }

            var devExpressBriefing = await libraryInventory.BuildDevExpressBriefingAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(devExpressBriefing))
            {
                builder.AppendLine("Local DevExpress library inventory:")
                    .AppendLine(TrimForPrompt(devExpressBriefing, 2200))
                    .AppendLine();
            }

            var buildDebugBriefing = await buildDebugInventory.BuildBriefingAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(buildDebugBriefing))
            {
                builder.AppendLine("Local build debug symbol inventory:")
                    .AppendLine(TrimForPrompt(buildDebugBriefing, 1600))
                    .AppendLine();
            }

            var projectKnowledge = await ReadProjectKnowledgeAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(projectKnowledge))
            {
                builder.AppendLine("Project AI guidance excerpts:")
                    .AppendLine(projectKnowledge);
            }

            return builder.ToString().Trim();
        }

        private async Task<string> ReadProjectKnowledgeAsync(CancellationToken cancellationToken)
        {
            var root = FindRepositoryRoot();
            if (root is null)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var relativePath in KnowledgeFiles)
            {
                var path = Path.Combine(root, relativePath);
                if (!File.Exists(path))
                    continue;

                try
                {
                    var text = await File.ReadAllTextAsync(path, cancellationToken);
                    builder.AppendLine($"[{relativePath}]")
                        .AppendLine(TrimForPrompt(text, 1800))
                        .AppendLine();
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
