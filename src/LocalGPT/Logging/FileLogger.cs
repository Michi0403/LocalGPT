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
        /// Stores the internal real path state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly string _realPath;
        /// <summary>
        /// Stores the internal options state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly FileLoggerCoreOptions _options;
        /// <summary>
        /// Stores the internal log queue state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly BlockingCollection<string> _logQueue = new();
        /// <summary>
        /// Stores the internal logging thread state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly Thread _loggingThread;
        /// <summary>
        /// Stores the internal disposed state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private bool _disposed = false;
        /// <summary>
        /// Stores the internal null scope state used by <see cref="FileLogger"/> while executing its surrounding workflow.
        /// </summary>
        private readonly LoggerNullScope nullScope = new();

        /// <summary>
        /// Initializes a new <see cref="FileLogger"/> instance and captures the dependencies or initial state required by its file logger workflow.
        /// </summary>
        /// <param name="categoryName">Category name value supplied to the file logger operation and used when producing its result.</param>
        /// <param name="optionsSnapshot">File logger core options dependency used by the file logger workflow to provide the corresponding application capability.</param>
        public FileLogger(string categoryName, IOptionsMonitor<FileLoggerCoreOptions> optionsSnapshot)
        {
            _options = optionsSnapshot.CurrentValue;
            _realPath = string.IsNullOrWhiteSpace(_options.FilePath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "LocalGPT.log")
                : _options.FilePath;


            _loggingThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "FileLoggerBackgroundThread"
            };
            _loggingThread.Start();
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
            {
                return;
            }

            var sb = new StringBuilder();
            _ = sb.Append(DateTime.UtcNow.ToString("O"))
              .Append(" [Machine: ").Append(Environment.MachineName).Append("]")
              .Append(" [Level: ").Append(logLevel).Append("] ")
              .Append(formatter(state, exception));

            if (exception != null)
            {
                _ = sb.AppendLine().Append("Exception: ").Append(exception);
            }

            var message = sb.ToString();

            try
            {

                if (!_logQueue.IsAddingCompleted)
                {
                    _logQueue.Add(message);
                }
            }
            catch (InvalidOperationException)
            {

            }
        }

        /// <summary>
        /// Processes log queue for <see cref="FileLogger"/>, keeping the operation consistent with the state and invariants of the surrounding file logger workflow.
        /// </summary>
        private void ProcessLogQueue()
        {
            try
            {
                foreach (var message in _logQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(_realPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            _ = Directory.CreateDirectory(dir);
                        }
                        try
                        {
                            File.AppendAllText(_realPath, message + Environment.NewLine);
                        }
                        catch (System.IO.IOException ex)
                        {
                            Console.WriteLine($"Warning Logger couldn't access log file: {ex.Message}");
                        }
                      
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to write log to file: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging background thread crashed: {ex.Message}");
            }
        }

        /// <summary>
        /// Releases resources owned by <see cref="FileLogger"/> and leaves the file logger workflow in a safely disposed state.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;


            _logQueue.CompleteAdding();


            _loggingThread.Join();

            _logQueue.Dispose();
        }

    }
}
