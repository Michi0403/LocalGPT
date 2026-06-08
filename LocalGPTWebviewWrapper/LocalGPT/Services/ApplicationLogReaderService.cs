using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Extensions.PlainStatics.CouncilData.Data;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Services
{
    public partial class ApplicationLogReaderService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory) : IApplicationLogReaderService
    {
        public string DatabasePath => EfChatMemoryService.GetDefaultDatabasePath();

        public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await ApplicationLogSchema.EnsureCreatedAsync(db, cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationLogSummary>> GetRecentAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 20, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await ApplicationLogSchema.EnsureCreatedAsync(db, cancellationToken);

            return await db.ApplicationLogs
                .AsNoTracking()
                .Where(log => log.LogLevelValue >= (int)minimumLevel)
                .OrderByDescending(log => log.TimestampUtc)
                .Take(Math.Clamp(take, 1, 200))
                .Select(log => new ApplicationLogSummary(
                    log.Id,
                    log.TimestampUtc,
                    log.Level,
                    log.LogLevelValue,
                    log.Category,
                    log.EventId,
                    log.EventName,
                    log.Message,
                    log.Exception))
                .ToListAsync(cancellationToken);
        }

        public async Task<string> BuildAiLogBriefingAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 8, CancellationToken cancellationToken = default)
        {
            var logs = await GetRecentAsync(minimumLevel, take, cancellationToken);
            if (logs.Count == 0)
                return string.Empty;

            var builder = new StringBuilder()
                .AppendLine("Recent LocalGPT warnings/errors from SQLite application log:");

            foreach (var log in logs.OrderBy(log => log.TimestampUtc))
            {
                builder
                    .Append("- ")
                    .Append(log.TimestampUtc.ToString("u"))
                    .Append(" [")
                    .Append(log.Level)
                    .Append("] ")
                    .Append(log.Category)
                    .Append(": ")
                    .AppendLine(CouncilChatStringFunctions. TrimForPrompt(log.Message, 320));

                if (!string.IsNullOrWhiteSpace(log.Exception))
                    builder.AppendLine($"  Exception: {CouncilChatStringFunctions.TrimForPrompt(log.Exception, 320)}");
            }

            builder.AppendLine("If these logs mention missing Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, package registration, or model setup, explain the likely local fix to the user and mark uncertain details as Needs verification.");
            return builder.ToString().Trim();
        }
    }
}
