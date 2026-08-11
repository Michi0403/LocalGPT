using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Services
{
    /// <summary>
    /// Provides application log reader service operations.
    /// </summary>
    public partial class ApplicationLogReaderService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseInitializationService databaseInitializer,
        LocalGptDatabaseOptions databaseOptions,
        ILogger<ApplicationLogReaderService> logger,
        CouncilTextService councilText) : IApplicationLogReaderService
    {
        /// <summary>
        /// Gets or sets database path.
        /// </summary>
        public string DatabasePath => databaseOptions.DatabasePath;
        /// <summary>
        /// Gets recent async.
        /// </summary>
        public async Task<IReadOnlyList<ApplicationLogSummary>> GetRecentAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

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
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetRecentAsync minimumLevel {minimumLevel} take {take}");
                return new List<ApplicationLogSummary>();
            }
           
        }

        /// <summary>
        /// Builds ai log briefing async.
        /// </summary>
        public async Task<string> BuildAiLogBriefingAsync(LogLevel minimumLevel = LogLevel.Warning, int take = 8, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await GetRecentAsync(minimumLevel, take, cancellationToken).ConfigureAwait(false);
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
                        .AppendLine(councilText.TrimForPrompt(log.Message, 320, logger));

                    if (!string.IsNullOrWhiteSpace(log.Exception))
                        builder.AppendLine($"  Exception: {councilText.TrimForPrompt(log.Exception, 320, logger)}");
                }

                builder.AppendLine("If these logs mention missing Java, Gradle, Minecraft, Ollama, WebView2, DevExpress, package registration, or model setup, explain the likely local fix to the user and mark uncertain details as Needs verification.");
                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"BuildAiLogBriefingAsync {minimumLevel} take {take}");
                return string.Empty;
            }
        }
    }
}
