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
        }

        public async Task<IReadOnlyList<CouncilKnowledgeEntry>> GetEntriesAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);

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
