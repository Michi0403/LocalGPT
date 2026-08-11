using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Logging
{
    /// <summary>
    /// Represents a database logger.
    /// </summary>
    public sealed class DatabaseLogger(string categoryName, DatabaseLoggerProvider provider) : ILogger
    {
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly LoggerNullScope nullScope = new();
        /// <summary>
        /// Runs the begin scope operation.
        /// </summary>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => nullScope;

        /// <summary>
        /// Determines whether enabled.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(categoryName, logLevel);

        /// <summary>
        /// Runs the log operation.
        /// </summary>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter?.Invoke(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
                return;

            provider.Enqueue(new ApplicationLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = logLevel.ToString(),
                LogLevelValue = (int)logLevel,
                Category = Trim(categoryName, 300),
                EventId = eventId.Id,
                EventName = string.IsNullOrWhiteSpace(eventId.Name) ? null : Trim(eventId.Name, 200),
                Message = message ?? string.Empty,
                Exception = exception?.ToString(),
                MachineName = Trim(Environment.MachineName, 120),
                ProcessId = Environment.ProcessId,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }

        /// <summary>
        /// Runs the trim operation.
        /// </summary>
        private string Trim(string value, int maxLength)
        {
            if (value.Length <= maxLength)
                return value;

            return $"{value[..Math.Max(0, maxLength - 3)]}...";
        }

    }
}
