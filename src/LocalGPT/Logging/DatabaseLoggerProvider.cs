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
    /// Provides database logger data or behavior to callers while hiding the underlying acquisition and configuration details.
    /// </summary>
    public sealed class DatabaseLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Stores the internal excluded category prefixes state used by <see cref="DatabaseLoggerProvider"/> while executing its surrounding workflow.
        /// </summary>
        private readonly string[] ExcludedCategoryPrefixes =
        [
            "LocalGPT.Logging.DatabaseLogger",
            "Microsoft.EntityFrameworkCore"
        ];

        /// <summary>
        /// Stores the database context factory dependency used by <see cref="DatabaseLoggerProvider"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
        /// <summary>
        /// Stores the options monitor dependency used by <see cref="DatabaseLoggerProvider"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IOptionsMonitor<DatabaseLoggerCoreOptions> options;
        /// <summary>
        /// Stores the database logger readiness dependency used by <see cref="DatabaseLoggerProvider"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IDatabaseLoggerReadiness databaseLoggerReadiness;
        /// <summary>
        /// Stores the internal channel state used by <see cref="DatabaseLoggerProvider"/> while executing its surrounding workflow.
        /// </summary>
        private readonly Channel<ApplicationLogEntry> channel;
        /// <summary>
        /// Stores the cancellation source used by <see cref="DatabaseLoggerProvider"/> to stop its current background or asynchronous operation.
        /// </summary>
        private readonly CancellationTokenSource stop = new();
        /// <summary>
        /// Stores the internal processing task state used by <see cref="DatabaseLoggerProvider"/> while executing its surrounding workflow.
        /// </summary>
        private readonly Task processingTask;

        /// <summary>
        /// Initializes a new <see cref="DatabaseLoggerProvider"/> instance and captures the dependencies or initial state required by its database logger workflow.
        /// </summary>
        /// <param name="dbContextFactory">Local gpt memory database context dependency used by the database logger workflow to provide the corresponding application capability.</param>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        /// <param name="databaseLoggerReadiness">Database logger readiness dependency used by the database logger workflow to provide the corresponding application capability.</param>
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
        /// Creates logger for <see cref="DatabaseLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <param name="categoryName">Category name value supplied to the database logger operation and used when producing its result.</param>
        /// <returns>The i logger produced by the operation.</returns>
        public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, this);

        /// <summary>
        /// Determines whether enabled for <see cref="DatabaseLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <param name="categoryName">Category name value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="logLevel">Log level value supplied to the database logger operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
        /// Performs enqueue for <see cref="DatabaseLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <param name="entry">Entry value supplied to the database logger operation and used when producing its result.</param>
        internal void Enqueue(ApplicationLogEntry entry)
        {
            _ = channel.Writer.TryWrite(entry);
        }

        /// <summary>
        /// Processes queue for <see cref="DatabaseLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
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
        /// Performs drain batch for <see cref="DatabaseLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <param name="batch">Batch value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="maxItems">Max items value supplied to the database logger operation and used when producing its result.</param>
        private void DrainBatch(List<ApplicationLogEntry> batch, int? maxItems = null)
        {
            var maxBatchSize = maxItems ?? Math.Clamp(options.CurrentValue.BatchSize, 1, 500);
            while (batch.Count < maxBatchSize && channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }
        }

        /// <summary>
        /// Performs flush for <see cref="DatabaseLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <param name="batch">Batch value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task FlushAsync(List<ApplicationLogEntry> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
                return;

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
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
        /// Releases resources owned by <see cref="DatabaseLoggerProvider"/> and leaves the database logger workflow in a safely disposed state.
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
