using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Logging
{
    /// <summary>
    /// Represents a database logger application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="categoryName">Category name value supplied to the database logger operation and used when producing its result.</param>
    /// <param name="provider">Database logger provider dependency used by the database logger workflow to provide the corresponding application capability.</param>
    public sealed class DatabaseLogger(string categoryName, DatabaseLoggerProvider provider) : ILogger
    {
        /// <summary>
        /// Stores the internal null scope state used by <see cref="DatabaseLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly LoggerNullScope nullScope = new();
        /// <summary>
        /// Performs begin scope for <see cref="DatabaseLogger"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <typeparam name="TState">Type used for t state values handled by <see cref="DatabaseLogger"/>.</typeparam>
        /// <param name="state">State value supplied to the database logger operation and used when producing its result.</param>
        /// <returns>The i disposable produced by the operation.</returns>
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => nullScope;

        /// <summary>
        /// Determines whether enabled for <see cref="DatabaseLogger"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <returns>The bool is enabled log level log level provider produced by the operation.</returns>
        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(categoryName, logLevel);

        /// <summary>
        /// Performs log for <see cref="DatabaseLogger"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <typeparam name="TState">Type used for t state values handled by <see cref="DatabaseLogger"/>.</typeparam>
        /// <param name="logLevel">Log level value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="eventId">Identifier of the event to use for this operation.</param>
        /// <param name="state">State value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="exception">Exception value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="formatter">Formatter value supplied to the database logger operation and used when producing its result.</param>
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
        /// Performs trim for <see cref="DatabaseLogger"/>, keeping the operation consistent with the state and invariants of the surrounding database logger workflow.
        /// </summary>
        /// <param name="value">Value value supplied to the database logger operation and used when producing its result.</param>
        /// <param name="maxLength">Max length value supplied to the database logger operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string Trim(string value, int maxLength)
        {
            if (value.Length <= maxLength)
                return value;

            return $"{value[..Math.Max(0, maxLength - 3)]}...";
        }

    }
}
