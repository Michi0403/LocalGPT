using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Helper;

/// <summary>
/// Persists LocalGPT setup diagnostics to a durable per-user transcript so installer and startup failures remain available after the console closes.
/// </summary>
internal sealed class SetupFileLoggerProvider : ILoggerProvider
{
    /// <summary>Stores the absolute durable setup transcript path shared by loggers created by this provider.</summary>
    private readonly string logPath;
    /// <summary>Serializes append operations so concurrent setup diagnostics do not interleave in the transcript.</summary>
    private readonly object sync = new();

    /// <summary>Initializes the provider with the absolute setup transcript path.</summary>
    /// <param name="logPath">Absolute path of the setup transcript.</param>
    public SetupFileLoggerProvider(string logPath)
    {
        this.logPath = Path.GetFullPath(logPath);
        var directory = Path.GetDirectoryName(this.logPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        File.AppendAllText(this.logPath, $"{Environment.NewLine}===== LocalGPT setup session {DateTimeOffset.UtcNow:O} ====={Environment.NewLine}", Encoding.UTF8);
    }

    /// <summary>Creates a logger that appends setup events to the shared transcript.</summary>
    /// <param name="categoryName">Logging category name.</param>
    /// <returns>A logger bound to the shared setup transcript.</returns>
    public ILogger CreateLogger(string categoryName) => new SetupFileLogger(categoryName, logPath, sync);

    /// <summary>Releases provider resources. The provider owns no long-lived stream.</summary>
    public void Dispose()
    {
    }

    /// <summary>Writes formatted setup events synchronously so crash diagnostics are not lost.</summary>
    /// <param name="categoryName">Logging category attached to each persisted event.</param>
    /// <param name="logPath">Absolute transcript path shared by the provider.</param>
    /// <param name="sync">Synchronization object shared across setup loggers.</param>
    private sealed class SetupFileLogger(string categoryName, string logPath, object sync) : ILogger
    {
        /// <summary>Returns no scope object because the durable setup transcript records events without ambient scope state.</summary>
        /// <typeparam name="TState">Scope state type.</typeparam>
        /// <param name="state">Scope state supplied by the caller.</param>
        /// <returns>An inert disposable scope.</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Reports whether the supplied level should be persisted.</summary>
        /// <param name="logLevel">Severity being evaluated.</param>
        /// <returns>True for every non-None severity.</returns>
        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        /// <summary>Formats and appends one installer event, including its full exception when present.</summary>
        /// <typeparam name="TState">Event state type.</typeparam>
        /// <param name="logLevel">Event severity.</param>
        /// <param name="eventId">Event identifier.</param>
        /// <param name="state">Event state.</param>
        /// <param name="exception">Optional event exception.</param>
        /// <param name="formatter">Formatter used to render the event message.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var builder = new StringBuilder()
                .Append(DateTimeOffset.UtcNow.ToString("O"))
                .Append(" [").Append(logLevel).Append("] ")
                .Append(categoryName).Append(" - ")
                .Append(formatter(state, exception));
            if (exception is not null)
                builder.AppendLine().Append(exception);
            lock (sync)
                File.AppendAllText(logPath, builder.AppendLine().ToString(), Encoding.UTF8);
        }
    }
}
