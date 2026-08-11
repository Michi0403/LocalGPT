using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Logging
{
    /// <summary>
    /// Represents a file logger.
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _realPath;
        private readonly FileLoggerCoreOptions _options;
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly BlockingCollection<string> _logQueue = new();
        private readonly Thread _loggingThread;
        private bool _disposed = false;
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly LoggerNullScope nullScope = new();

        /// <summary>
        /// Runs the file logger operation.
        /// </summary>
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

        IDisposable ILogger.BeginScope<TState>(TState state)
        {

            return nullScope;
        }

        /// <summary>
        /// Determines whether enabled.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return (int)logLevel >= (int)_options.CoreLogLevel;
        }

        /// <summary>
        /// Runs the log operation.
        /// </summary>
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
        /// Runs the process log queue operation.
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
        /// Runs the dispose operation.
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