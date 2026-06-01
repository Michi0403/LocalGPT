using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Data;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    public partial class CouncilKnowledgeService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        ILogger<CouncilKnowledgeService> logger) : ICouncilKnowledgeService
    {
        public string DatabasePath => EfChatMemoryService.GetDefaultDatabasePath();

        public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedBuiltInKnowledgeAsync(db, cancellationToken);
        }

        public async Task<IReadOnlyList<CouncilKnowledgeEntry>> GetEntriesAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedBuiltInKnowledgeAsync(db, cancellationToken);

            var query = db.CouncilKnowledgeEntries.AsNoTracking();
            if (!includeArchived)
                query = query.Where(entry => !entry.IsArchived);

            return await query
                .OrderByDescending(entry => entry.IsPinned)
                .ThenByDescending(entry => entry.IsUserApproved)
                .ThenByDescending(entry => entry.UpdatedAtUtc)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync(cancellationToken);
        }

        public async Task<CouncilKnowledgeEntry> SaveEntryAsync(CouncilKnowledgeEntry entry, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedBuiltInKnowledgeAsync(db, cancellationToken);

            var now = DateTime.UtcNow;
            var existing = await db.CouncilKnowledgeEntries.SingleOrDefaultAsync(item => item.Id == entry.Id, cancellationToken);
            if (existing is null)
            {
                entry.CreatedAtUtc = entry.CreatedAtUtc == default ? now : entry.CreatedAtUtc;
                entry.UpdatedAtUtc = now;
                Normalize(entry);
                db.CouncilKnowledgeEntries.Add(entry);
            }
            else
            {
                existing.Topic = entry.Topic;
                existing.Scope = entry.Scope;
                existing.Content = entry.Content;
                existing.Source = entry.Source;
                existing.HelpfulSources = entry.HelpfulSources;
                existing.Tags = entry.Tags;
                existing.Confidence = entry.Confidence;
                existing.IsUserApproved = entry.IsUserApproved;
                existing.IsPinned = entry.IsPinned;
                existing.IsArchived = entry.IsArchived;
                existing.UpdatedAtUtc = now;
                Normalize(existing);
                entry = existing;
            }

            await db.SaveChangesAsync(cancellationToken);
            return entry;
        }

        public async Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedBuiltInKnowledgeAsync(db, cancellationToken);
            var entry = await db.CouncilKnowledgeEntries.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (entry is null)
                return;

            db.CouncilKnowledgeEntries.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> SaveFromCouncilRunAsync(MultiModelCouncilResult result, CancellationToken cancellationToken = default)
        {
            var nonSubstantive = IsNonSubstantiveCouncilKnowledge(result);
            var entry = new CouncilKnowledgeEntry
            {
                Topic = BuildTopic(result.Prompt),
                Scope = "AI Council",
                Source = $"AI Council {result.RunId}",
                Content = BuildCouncilKnowledgeContent(result),
                HelpfulSources = ExtractHelpfulSources(result.FinalAnswer),
                Tags = BuildTags(result, nonSubstantive),
                Confidence = nonSubstantive ? 20 : result.Warnings.Count == 0 ? 75 : 55,
                IsUserApproved = false,
                IsPinned = result.UserPoll is not null && !nonSubstantive,
                IsArchived = nonSubstantive
            };

            await SaveEntryAsync(entry, cancellationToken);
            logger.LogInformation("Saved council knowledge entry {KnowledgeEntryId} for council run {RunId}.", entry.Id, result.RunId);
            return entry.Id;
        }

        public async Task<string> BuildKnowledgeBriefingAsync(int take = 8, CancellationToken cancellationToken = default)
        {
            var entries = await GetEntriesAsync(includeArchived: false, take, cancellationToken);
            if (entries.Count == 0)
                return string.Empty;

            var builder = new StringBuilder()
                .AppendLine("AI Council maintained knowledge database:");

            var briefingEntries = entries
                .Where(entry => !LooksLikeNonSubstantiveContent(entry.Content))
                .OrderByDescending(entry => entry.IsUserApproved)
                .GroupBy(entry => $"{entry.Scope}|{entry.Topic}|{entry.Source}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());

            foreach (var entry in briefingEntries)
            {
                var trust = entry.IsUserApproved
                    ? "verified by user"
                    : "unverified model-written note; treat as hypothesis until user approves";
                builder
                    .Append("- ")
                    .Append(entry.Topic)
                    .Append(" [")
                    .Append(entry.Scope)
                    .Append(", ")
                    .Append(trust)
                    .Append(", confidence ")
                    .Append(entry.Confidence)
                    .Append("%]: ")
                    .AppendLine(TrimForPrompt(entry.Content, 420));

                if (!string.IsNullOrWhiteSpace(entry.HelpfulSources))
                    builder.AppendLine($"  Helpful sources requested: {TrimForPrompt(entry.HelpfulSources, 240)}");
            }

            return builder.ToString().Trim();
        }

        private static void Normalize(CouncilKnowledgeEntry entry)
        {
            entry.Topic = TrimOrFallback(entry.Topic, 240, "Untitled knowledge entry");
            entry.Scope = TrimOrFallback(entry.Scope, 120, "AI Council");
            entry.Source = TrimOrFallback(entry.Source, 240, "Manual");
            entry.Tags = Trim(entry.Tags, 400);
            entry.Confidence = Math.Clamp(entry.Confidence, 0, 100);
        }

        private static async Task SeedBuiltInKnowledgeAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
        {
            const string seedSource = "LocalGPT built-in seed";
            var existingSeedIds = await db.CouncilKnowledgeEntries
                .Where(entry => entry.Source == seedSource)
                .Select(entry => entry.Id)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var entries = new[]
            {
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("157ea50f-093d-43dc-b7f6-546d74d8ad22"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "LocalGPT Blazor/DevExpress page generation rules",
                    Scope = "Blazor frontend",
                    Source = seedSource,
                    Content = "When generating LocalGPT UI, produce real .razor artifacts instead of C# classes that only build strings. Use @page, @rendermode InteractiveServer, @code blocks, dependency injection, and existing project styling such as main-container/top-container. Prefer known DevExpress Blazor components already used in this project: DxButton, DxCheckBox, DxComboBox, DxTextBox, DxMemo, DxSpinEdit, DxGrid, DxGridDataColumn, DxFormLayout, DxFormLayoutGroup, DxFormLayoutItem, DxLoadingPanel, DxMenu, DxGridLayout, and DXAiChat. Keep native commands and generated files in backend services with safe download routes.",
                    HelpfulSources = "- GET /__diag/devexpress for local DevExpress package/import/service inventory.\n- Local project pages: Components/Pages/Chat.razor, Database.razor, Install.razor, ModelCouncil.razor.\n- TacosPortalOpen sample zip inspected locally for server-interactive Razor + DevExpress patterns.",
                    Tags = "seed; blazor; devexpress; razor; dxaichat; artifacts",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("dcf82c98-e535-453e-86c6-484a2795c140"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "TacosPortalOpen server-interactive architecture sample",
                    Scope = "Blazor frontend",
                    Source = seedSource,
                    Content = "TacosPortalOpen is a useful local architecture sample for Michi0403-style Blazor work. Relevant server-side patterns include Routes.razor with AuthorizeRouteView, pages using @rendermode InteractiveServer or new InteractiveServerRenderMode(prerender: true/false), AuthorizeView for protected UI, ToastWrapper/INotificationService for user feedback, and DevExpress DxGrid edit forms with EditFormTemplate + DxFormLayout. Treat the sample as architecture guidance, not code to copy blindly into LocalGPT.",
                    HelpfulSources = "- User-provided C:/Users/micha/Downloads/TacosPortalOpen-main.zip.\n- Inspected files: TacosPortal/Components/App.razor, Routes.razor, Pages/Index.razor, Pages/Admin/RoleAdministration.razor, Pages/GenericEditGrid.razor, Startup.cs.",
                    Tags = "seed; tacosportalopen; blazor; interactive-server; devexpress",
                    Confidence = 85,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("243e82e1-9f6d-4b8c-abd8-75e7cda0c776"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Official docs and missing-source behavior",
                    Scope = "AI Council",
                    Source = seedSource,
                    Content = "If the council needs GitHub repository details, DevExpress APIs, .NET/Blazor version behavior, or official syntax rules and the local diagnostics do not provide enough evidence, it must say exactly which source is needed under Helpful sources requested or Missing feature report. Do not blame the user or hallucinate APIs. Prefer compact diagnostics first: /__diag/devexpress, /__diag/dxaichat-functions, /__diag/build-debug-files, /__diag/logs, and SQLite knowledge entries. Mark claims as Needs verification until the source or local package inventory confirms them.",
                    HelpfulSources = "- Official Microsoft Learn .NET/ASP.NET Core/Blazor docs when internet access is allowed.\n- DevExpress official Blazor docs matching the installed package version.\n- GitHub repository source files or local extracted zips supplied by Michi0403.",
                    Tags = "seed; sources; github; dotnet; devexpress; needs-verification",
                    Confidence = 95,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("46d5a4c1-c873-4285-b5e7-d2c58eb5b6be"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Minecraft Java mod and plugin source map",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "Use this source map before generating Java Minecraft workspaces. Classic Forge uses the Forge MDK: download the MDK, extract it into an empty directory, import/open the Gradle project in Eclipse or IntelliJ, build with gradlew build, and test with generated run configs or gradlew runClient/runServer. Fabric builds with ./gradlew build or ./gradlew.bat build; use the shortest jar in build/libs for distribution and make sure the terminal/IDE Java version matches the project. Paper is the server-side plugin path for users who do not want a modded client; include plugin.yml and use Paper's plugin project setup guidance. Use Gradle Java toolchains or explicit IDE Gradle JVM settings to avoid inconsistent JDK behavior. Java syntax should be grounded in the Java Language Specification/JDK docs; Microsoft OpenJDK is a supported JDK distribution, not a separate Java syntax.",
                    HelpfulSources = "- Forge getting started: https://docs.minecraftforge.net/en/latest/gettingstarted/\n- NeoForge getting started: https://docs.neoforged.net/docs/gettingstarted/\n- Fabric building a mod: https://docs.fabricmc.net/develop/getting-started/building-a-mod\n- Paper getting started: https://docs.papermc.io/paper/dev/getting-started/\n- Gradle JVM toolchains: https://docs.gradle.org/current/userguide/toolchains.html\n- Oracle JDK 21 documentation: https://docs.oracle.com/en/java/javase/21/",
                    Tags = "seed; minecraft; forge; fabric; neoforge; paper; gradle; java; sources",
                    Confidence = 92,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("75c6cc67-958d-4b66-bfb2-f8f98eccd0c4"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Minecraft datapack generation source rules",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "For vanilla Java datapacks, generate a zip/folder whose root contains pack.mcmeta and data/. The data folder contains namespaces; function entry points for modern 1.21-style generated packs should use singular folders such as data/<namespace>/function and data/minecraft/tags/function. Add data/minecraft/tags/function/load.json and tick.json to call namespace functions; minecraft:load runs after /reload or server load, and minecraft:tick runs each tick, so tick functions must stay tiny and delegate scheduled aggregate work. pack_format is required and version-sensitive; LocalGPT should use its datapack version catalog or source-check the target version before claiming compatibility. supported_formats and overlays exist for multi-format packs, but basic generated starters should keep one target version unless the user asks for overlays.",
                    HelpfulSources = "- Minecraft Wiki data pack structure and pack.mcmeta: https://minecraft.wiki/w/Data_pack\n- Minecraft Wiki Java function tags: https://minecraft.wiki/w/Function_tag_(Java_Edition)\n- Minecraft Java snapshot 23w31a pack metadata supported_formats/overlays: https://feedback.minecraft.net/hc/en-us/articles/18619031671821-Minecraft-Java-Edition-Snapshot-23w31a\n- Minecraft Wiki pack_format table: https://minecraft.wiki/w/Pack_format",
                    Tags = "seed; minecraft; datapack; pack.mcmeta; function-tags; pack-format; sources",
                    Confidence = 88,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("1b84f98e-3c07-4f8b-ac3d-2a42bd9fb0c5"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Living Cities datapack benchmark acceptance",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "Use /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4 as the low-context Living Cities datapack benchmark. A useful result must generate real .mcfunction files, no .mcfunction.txt placeholders, pack.mcmeta, minecraft load/tick function tags, namespace functions, JSON validation, function-reference validation, and a zip under build/. Compare against the friend's early living_cities.zip for preserved traits: namespace living_cities, core/load and core/tick entry points, scoreboards for year/population/food/security/prestige/birth year, storage areas for city/chronicle/personalities, and a town hall/admin workflow. Do not tell the user it was game-tested until /reload and in-game commands were actually run in Minecraft.",
                    HelpfulSources = "- Local route: GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4\n- User-provided benchmark: C:/Users/micha/Downloads/living_cities.zip\n- User-provided design prompt: C:/Users/micha/Downloads/message (1).txt",
                    Tags = "seed; minecraft; datapack; living-cities; benchmark; validation",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                }
            };

            foreach (var entry in entries)
                Normalize(entry);

            var missingEntries = entries
                .Where(entry => !existingSeedIds.Contains(entry.Id))
                .ToArray();

            if (missingEntries.Length == 0)
                return;

            try
            {
                db.CouncilKnowledgeEntries.AddRange(missingEntries);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another startup request may have inserted the same stable seed IDs first.
            }
        }

        private static string BuildCouncilKnowledgeContent(MultiModelCouncilResult result)
        {
            var builder = new StringBuilder()
                .AppendLine($"Council members: {string.Join(", ", result.ModelNames)}")
                .AppendLine($"Prompt: {TrimForPrompt(result.Prompt, 900)}")
                .AppendLine()
                .AppendLine("Final answer:")
                .AppendLine(TrimForPrompt(result.FinalAnswer, 2400));

            if (result.Warnings.Count > 0)
            {
                builder.AppendLine().AppendLine("Warnings:");
                foreach (var warning in result.Warnings.Take(10))
                    builder.AppendLine($"- {warning}");
            }

            if (result.UserPoll is not null)
            {
                builder.AppendLine().AppendLine("User decision poll:");
                builder.AppendLine(result.UserPoll.Question);
                foreach (var option in result.UserPoll.Options)
                    builder.AppendLine($"- {option.Label}: {option.FollowUpPrompt}");
            }

            return builder.ToString().Trim();
        }

        private static string BuildTopic(string prompt)
        {
            var normalized = WhitespacePattern().Replace(prompt, " ").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return "AI Council run";

            return normalized.Length <= 120 ? normalized : $"{normalized[..117].TrimEnd()}...";
        }

        private static string BuildTags(MultiModelCouncilResult result, bool nonSubstantive)
        {
            var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "council",
                "auto"
            };

            foreach (var model in result.ModelNames)
                tags.Add(model);
            if (result.Artifacts.Count > 0)
                tags.Add("artifact");
            if (result.UserPoll is not null)
                tags.Add("poll");
            if (nonSubstantive)
            {
                tags.Add("non-substantive");
                tags.Add("thinking-only");
            }

            return string.Join("; ", tags);
        }

        private static bool IsNonSubstantiveCouncilKnowledge(MultiModelCouncilResult result)
        {
            if (result.UserPoll is not null)
                return false;

            return LooksLikeNonSubstantiveContent(result.FinalAnswer);
        }

        private static bool LooksLikeNonSubstantiveContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return true;

            return content.Contains("returned thinking but no final visible answer", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("did not return a visible answer", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("did not return a substantive consensus answer", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractHelpfulSources(string text)
        {
            var matches = HelpfulSourceLinePattern()
                .Matches(text)
                .Select(match => match.Groups["line"].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            return matches.Count == 0
                ? "None explicitly requested."
                : string.Join(Environment.NewLine, matches.Select(item => $"- {item}"));
        }

        private static string TrimForPrompt(string text, int maxLength)
        {
            var normalized = WhitespacePattern().Replace(text, " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static string TrimOrFallback(string value, int maxLength, string fallback)
        {
            var trimmed = Trim(value, maxLength);
            return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
        }

        private static string Trim(string value, int maxLength)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..maxLength].TrimEnd()}";
        }

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();

        [GeneratedRegex("(?im)^\\s*(?:[-*]\\s*)?(?<line>(?:helpful sources?|source request|needed sources?|references?|docs?|documentation|official docs?|examples?|sample projects?|spec(?:ification)?s?|tutorials?)\\s*[:\\-].+)$", RegexOptions.CultureInvariant)]
        private static partial Regex HelpfulSourceLinePattern();
    }
}
