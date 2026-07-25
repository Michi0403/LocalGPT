using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Logging
{
    public sealed class DatabaseLogger(string categoryName, DatabaseLoggerProvider provider) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(categoryName, logLevel);

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

        private string Trim(string value, int maxLength)
        {
            if (value.Length <= maxLength)
                return value;

            return $"{value[..Math.Max(0, maxLength - 3)]}...";
        }

        public sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
