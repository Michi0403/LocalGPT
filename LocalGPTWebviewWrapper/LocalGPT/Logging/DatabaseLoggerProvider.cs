using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Enums;
using LocalGPT.Extensions.PlainStatics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace LocalGPT.Logging
{
    public sealed class DatabaseLoggerProvider(ILogger<DatabaseLoggerProvider> logger) : ILoggerProvider
    {
        private static readonly string[] ExcludedCategoryPrefixes =
        [
            "LocalGPT.Logging.DatabaseLogger",
            "Microsoft.EntityFrameworkCore"
        ];

        private readonly IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory;
        private readonly IOptionsMonitor<DatabaseLoggerCoreOptions> options;
        private readonly Channel<ApplicationLogEntry> channel;
        private readonly CancellationTokenSource stop = new();
        private readonly Task processingTask;

        public DatabaseLoggerProvider(ILogger<DatabaseLoggerProvider> logger,
            IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
            IOptionsMonitor<DatabaseLoggerCoreOptions> options) : this(logger)
        {
            this.dbContextFactory = dbContextFactory;
            this.options = options;
            var queueLength = Math.Clamp(options.CurrentValue.MaxQueueLength, 100, 50000);
            channel = Channel.CreateBounded<ApplicationLogEntry>(new BoundedChannelOptions(queueLength)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            processingTask = Task.Run(ProcessQueueAsync);
        }

        public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, this);

        internal bool IsEnabled(string categoryName, LogLevel logLevel)
        {
            var current = options.CurrentValue;
            if (current.CoreLogLevel == CoreLogLevel.None || logLevel == LogLevel.None)
                return false;

            if (ExcludedCategoryPrefixes.Any(prefix => categoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return false;

            return (int)logLevel >= (int)current.CoreLogLevel;
        }

        internal void Enqueue(ApplicationLogEntry entry)
        {
            _ = channel.Writer.TryWrite(entry);
        }

        private async Task ProcessQueueAsync()
        {
            var batch = new List<ApplicationLogEntry>();
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    if (!await channel.Reader.WaitToReadAsync(stop.Token))
                        break;

                    DrainBatch(batch);
                    if (batch.Count < Math.Clamp(options.CurrentValue.BatchSize, 1, 500))
                        await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(options.CurrentValue.FlushIntervalSeconds, 1, 30)), stop.Token);

                    DrainBatch(batch);
                    await FlushAsync(batch, stop.Token);
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
                        await Task.Delay(TimeSpan.FromSeconds(5), stop.Token);
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
                await FlushAsync(batch, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database logger final flush failed: {ex.Message}");
            }
        }

        private void DrainBatch(List<ApplicationLogEntry> batch, int? maxItems = null)
        {
            var maxBatchSize = maxItems ?? Math.Clamp(options.CurrentValue.BatchSize, 1, 500);
            while (batch.Count < maxBatchSize && channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }
        }

        private async Task FlushAsync(List<ApplicationLogEntry> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
                return;

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await SQLLiteTableFunctions.EnsureCreatedApplicationLogSchemaAsync(db,logger, cancellationToken);
            db.ApplicationLogs.AddRange(batch);
            await db.SaveChangesAsync(cancellationToken);
            await PruneOldLogsAsync(db, cancellationToken);
        }

        private async Task PruneOldLogsAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
        {
            var retentionDays = options.CurrentValue.RetentionDays;
            if (retentionDays <= 0)
                return;

            var cutoff = DateTime.UtcNow.AddDays(-Math.Clamp(retentionDays, 1, 3660));
            await db.ApplicationLogs
                .Where(log => log.TimestampUtc < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public void Dispose()
        {
            channel.Writer.TryComplete();
            stop.Cancel();
            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }

            stop.Dispose();
        }
    }
}
