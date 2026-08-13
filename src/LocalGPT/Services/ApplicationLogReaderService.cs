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
    /// Coordinates application log reader behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="dbContextFactory">Local gpt memory database context dependency used by the application log reader workflow to provide the corresponding application capability.</param>
    /// <param name="databaseInitializer">Database initialization service dependency used by the application log reader workflow to provide the corresponding application capability.</param>
    /// <param name="databaseOptions">Database options value supplied to the application log reader operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="councilText">Council text service dependency used by the application log reader workflow to provide the corresponding application capability.</param>
    public partial class ApplicationLogReaderService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        IDatabaseInitializationService databaseInitializer,
        LocalGptDatabaseOptions databaseOptions,
        ILogger<ApplicationLogReaderService> logger,
        CouncilTextService councilText) : IApplicationLogReaderService
    {
        /// <summary>
        /// Gets the database path used by this application log reader instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The database path value exposed by <see cref="ApplicationLogReaderService"/>.</value>
        public string DatabasePath => databaseOptions.DatabasePath;
        /// <summary>
        /// Retrieves recent as part of the application log reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="minimumLevel">Minimum level value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
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
        /// Builds AI log briefing as part of the application log reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="minimumLevel">Minimum level value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the application log reader operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
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
