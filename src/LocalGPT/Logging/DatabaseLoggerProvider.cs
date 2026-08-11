using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Enums;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace LocalGPT.Logging
{
    /// <summary>
    /// Provides database logger provider operations.
    /// </summary>
    public sealed class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly string[] ExcludedCategoryPrefixes =
        [
            "LocalGPT.Logging.DatabaseLogger",
            "Microsoft.EntityFrameworkCore"
        ];

        private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
        private readonly IOptionsMonitor<DatabaseLoggerCoreOptions> options;
        private readonly IDatabaseLoggerReadiness databaseLoggerReadiness;
        private readonly Channel<ApplicationLogEntry> channel;
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly CancellationTokenSource stop = new();
        private readonly Task processingTask;

        /// <summary>
        /// Runs the database logger provider operation.
        /// </summary>
        public DatabaseLoggerProvider(
            IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
            IOptionsMonitor<DatabaseLoggerCoreOptions> options,
            IDatabaseLoggerReadiness databaseLoggerReadiness)
        {
            this.dbContextFactory = dbContextFactory;
            this.options = options;
            this.databaseLoggerReadiness = databaseLoggerReadiness;
            var queueLength = Math.Clamp(options.CurrentValue.MaxQueueLength, 100, 50000);
            channel = Channel.CreateBounded<ApplicationLogEntry>(new BoundedChannelOptions(queueLength)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            processingTask = Task.Run(ProcessQueueAsync);
        }

        /// <summary>
        /// Creates logger.
        /// </summary>
        public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, this);

        /// <summary>
        /// Determines whether enabled.
        /// </summary>
        internal bool IsEnabled(string categoryName, LogLevel logLevel)
        {
            var current = options.CurrentValue;
            if (current.CoreLogLevel == CoreLogLevel.None || logLevel == LogLevel.None)
                return false;

            if (ExcludedCategoryPrefixes.Any(prefix => categoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return false;

            return (int)logLevel >= (int)current.CoreLogLevel;
        }

        /// <summary>
        /// Runs the enqueue operation.
        /// </summary>
        internal void Enqueue(ApplicationLogEntry entry)
        {
            _ = channel.Writer.TryWrite(entry);
        }

        /// <summary>
        /// Runs the process queue async operation.
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            try
            {
                // Startup logs remain in the bounded channel until migrations and deterministic seeds
                // have released SQLite. This prevents the logger from racing ApplicationLogs creation
                // or taking a write lock while the initialization hosted service is saving seed data.
                await databaseLoggerReadiness.WaitUntilReadyAsync(stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var batch = new List<ApplicationLogEntry>();
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    if (!await channel.Reader.WaitToReadAsync(stop.Token).ConfigureAwait(false))
                        break;

                    DrainBatch(batch);
                    if (batch.Count < Math.Clamp(options.CurrentValue.BatchSize, 1, 500))
                        await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(options.CurrentValue.FlushIntervalSeconds, 1, 30)), stop.Token).ConfigureAwait(false);

                    DrainBatch(batch);
                    await FlushAsync(batch, stop.Token).ConfigureAwait(false);
                    batch.Clear();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database logger background worker failed: {ex.Message}");
                    batch.Clear();
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stop.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            try
            {
                DrainBatch(batch, maxItems: 1000);
                await FlushAsync(batch, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database logger final flush failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs the drain batch operation.
        /// </summary>
        private void DrainBatch(List<ApplicationLogEntry> batch, int? maxItems = null)
        {
            var maxBatchSize = maxItems ?? Math.Clamp(options.CurrentValue.BatchSize, 1, 500);
            while (batch.Count < maxBatchSize && channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }
        }

        /// <summary>
        /// Runs the flush async operation.
        /// </summary>
        private async Task FlushAsync(List<ApplicationLogEntry> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
                return;

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.ApplicationLogs.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            //await PruneOldLogsAsync(db, cancellationToken);
        }

        //private async Task PruneOldLogsAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
        //{
        //    var retentionDays = options.CurrentValue.RetentionDays;
        //    if (retentionDays <= 0)
        //        return;

        //    var cutoff = DateTime.UtcNow.AddDays(-Math.Clamp(retentionDays, 1, 3660));
        //    await db.ApplicationLogs
        //        .Where(log => log.TimestampUtc < cutoff)
        //        .ExecuteDeleteAsync(cancellationToken);
        //}

        /// <summary>
        /// Runs the dispose operation.
        /// </summary>
        public void Dispose()
        {
            channel.Writer.TryComplete();
            if (!databaseLoggerReadiness.IsReady)
                stop.Cancel();
            try
            {
                if (!processingTask.Wait(TimeSpan.FromSeconds(2)))
                {
                    stop.Cancel();
                    _ = processingTask.Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch
            {
                stop.Cancel();
            }
            finally
            {
                stop.Cancel();
                stop.Dispose();
            }
        }
    }
}
