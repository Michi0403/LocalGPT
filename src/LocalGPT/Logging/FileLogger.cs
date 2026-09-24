using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Logging
{
    /// <summary>
    /// Represents a file logger application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        /// <summary>
        /// Stores the internal options state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly FileLoggerCoreOptions _options;
        /// <summary>
        /// Stores the shared file sink used by every category created by the provider so only one writer owns the log file.
        /// </summary>
        private readonly FileLoggerSink _sink;
        /// <summary>
        /// Stores the internal null scope state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly LoggerNullScope nullScope = new();

        /// <summary>
        /// Initializes a new <see cref="FileLogger"/> instance and captures the dependencies or initial state required by its file logger workflow.
        /// </summary>
        /// <param name="categoryName">Category name value supplied to the file logger operation and used when producing its result.</param>
        /// <param name="optionsSnapshot">File logger core options dependency used by the file logger workflow to provide the corresponding application capability.</param>
        /// <param name="sink">Provider-owned shared sink that serializes writes to the configured log file.</param>
        internal FileLogger(string categoryName, IOptionsMonitor<FileLoggerCoreOptions> optionsSnapshot, FileLoggerSink sink)
        {
            _ = categoryName;
            _options = optionsSnapshot.CurrentValue;
            _sink = sink;
        }

        /// <summary>
        /// Performs begin scope for <see cref="FileLogger"/>, keeping the operation consistent with the state and invariants of the surrounding file logger workflow.
        /// </summary>
        /// <typeparam name="TState">Type used for t state values handled by <see cref="FileLogger"/>.</typeparam>
        /// <param name="state">State value supplied to the file logger operation and used when producing its result.</param>
        /// <returns>The i disposable i logger produced by the operation.</returns>
        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return nullScope;
        }

        /// <summary>
        /// Determines whether enabled for <see cref="FileLogger"/>, keeping the operation consistent with the state and invariants of the surrounding file logger workflow.
        /// </summary>
        /// <param name="logLevel">Log level value supplied to the file logger operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return (int)logLevel >= (int)_options.CoreLogLevel;
        }

        /// <summary>
        /// Performs log for <see cref="FileLogger"/>, keeping the operation consistent with the state and invariants of the surrounding file logger workflow.
        /// </summary>
        /// <typeparam name="TState">Type used for t state values handled by <see cref="FileLogger"/>.</typeparam>
        /// <param name="logLevel">Log level value supplied to the file logger operation and used when producing its result.</param>
        /// <param name="eventId">Identifier of the event to use for this operation.</param>
        /// <param name="state">State value supplied to the file logger operation and used when producing its result.</param>
        /// <param name="exception">Exception value supplied to the file logger operation and used when producing its result.</param>
        /// <param name="formatter">Formatter value supplied to the file logger operation and used when producing its result.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || formatter == null)
                return;

            var sb = new StringBuilder();
            _ = sb.Append(DateTime.UtcNow.ToString("O"))
              .Append(" [Machine: ").Append(Environment.MachineName).Append("]")
              .Append(" [Level: ").Append(logLevel).Append("] ")
              .Append(formatter(state, exception));

            if (exception != null)
                _ = sb.AppendLine().Append("Exception: ").Append(exception);

            _sink.Enqueue(sb.ToString());
        }

        /// <summary>
        /// Releases resources owned by <see cref="FileLogger"/>. The provider owns the shared sink lifetime.
        /// </summary>
        public void Dispose()
        {
        }
    }

    /// <summary>Serializes LocalGPT file-log writes through one self-healing writer shared by every logging category.</summary>
    internal sealed class FileLoggerSink : IDisposable
    {
        private readonly string _realPath;
        private readonly string _fallbackPath;
        private readonly BlockingCollection<string> _logQueue = new();
        private readonly Thread _loggingThread;
        private bool _disposed;
        private DateTime _lastFailureNoticeUtc = DateTime.MinValue;

        /// <summary>Creates the provider-owned file sink and starts its single background writer.</summary>
        public FileLoggerSink(FileLoggerCoreOptions options)
        {
            _realPath = ResolveLogPath(options);
            _fallbackPath = Path.Combine(Path.GetTempPath(), "LocalGPT", "LocalGPT-fallback.log");
            _loggingThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "FileLoggerBackgroundThread"
            };
            _loggingThread.Start();
        }

        /// <summary>Queues one fully formatted entry without allowing logger failures to escape into application code.</summary>
        public void Enqueue(string message)
        {
            if (_disposed || _logQueue.IsAddingCompleted)
                return;
            try
            {
                _logQueue.Add(message);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>Drains the shared queue through one open append stream and automatically reopens it after transient I/O failures.</summary>
        private void ProcessLogQueue()
        {
            StreamWriter? writer = null;
            try
            {
                foreach (var message in _logQueue.GetConsumingEnumerable())
                {
                    var written = false;
                    for (var attempt = 0; attempt < 3 && !written; attempt++)
                    {
                        try
                        {
                            writer ??= OpenWriter(_realPath);
                            writer.WriteLine(message);
                            writer.Flush();
                            written = true;
                        }
                        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObjectDisposedException)
                        {
                            writer?.Dispose();
                            writer = null;
                            ReportWriteFailure(exception, _realPath);
                            Thread.Sleep(25 * (attempt + 1));
                        }
                    }

                    if (!written)
                        WriteFallback(message);
                }
            }
            catch (Exception exception)
            {
                ReportWriteFailure(exception, _realPath);
                while (_logQueue.TryTake(out var remaining))
                    WriteFallback(remaining);
            }
            finally
            {
                writer?.Dispose();
            }
        }

        /// <summary>Opens a resilient append writer while allowing diagnostics tools to inspect or copy the live file.</summary>
        private StreamWriter OpenWriter(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            return new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        /// <summary>Writes an emergency copy when the configured destination remains unavailable after retries.</summary>
        private void WriteFallback(string message)
        {
            try
            {
                using var writer = OpenWriter(_fallbackPath);
                writer.WriteLine(message);
                writer.Flush();
            }
            catch (Exception exception)
            {
                ReportWriteFailure(exception, _fallbackPath);
            }
        }

        /// <summary>Throttles console diagnostics so a file-system problem cannot create a second logging flood.</summary>
        private void ReportWriteFailure(Exception exception, string path)
        {
            var now = DateTime.UtcNow;
            if (now - _lastFailureNoticeUtc < TimeSpan.FromSeconds(30))
                return;
            _lastFailureNoticeUtc = now;
            Console.Error.WriteLine($"LocalGPT file logger could not write '{path}' and will retry/fall back without stopping application logging: {exception.Message}");
        }

        /// <summary>Resolves the file logger target without consulting the process current directory.</summary>
        private string ResolveLogPath(FileLoggerCoreOptions options)
        {
            try
            {
                var defaultPath = LocalGptApplicationDataPaths.ResolveUserPath("LocalGPT.log");
                var configured = options.FilePath?.Trim();
                if (string.IsNullOrWhiteSpace(configured))
                    return defaultPath;

                if (!Path.IsPathRooted(configured))
                    return LocalGptApplicationDataPaths.ResolveUserPath(configured);

                var fullConfigured = Path.GetFullPath(configured);
                var applicationRoot = Path.GetFullPath(AppContext.BaseDirectory);
                var relativeToApplication = Path.GetRelativePath(applicationRoot, fullConfigured);
                var outsideApplication = relativeToApplication.Equals("..", StringComparison.Ordinal)
                    || relativeToApplication.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.IsPathRooted(relativeToApplication);
                return outsideApplication ? fullConfigured : defaultPath;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return Path.Combine(Path.GetTempPath(), "LocalGPT", "LocalGPT.log");
            }
        }

        /// <summary>Stops intake, drains queued entries, and releases the shared writer thread.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _logQueue.CompleteAdding();
            if (_loggingThread.Join(TimeSpan.FromSeconds(5)))
                _logQueue.Dispose();
            else
                Console.Error.WriteLine("LocalGPT file logger is still draining queued entries during shutdown.");
        }
    }
}
