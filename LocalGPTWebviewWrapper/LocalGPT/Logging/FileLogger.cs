using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace LocalGPT.Logging
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _realPath;
        private readonly FileLoggerCoreOptions _options;
        private readonly BlockingCollection<string> _logQueue = new();
        private readonly Thread _loggingThread;
        private bool _disposed = false;

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

            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (int)logLevel >= (int)_options.CoreLogLevel;
        }

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
                        File.AppendAllText(_realPath, message + Environment.NewLine);
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;


            _logQueue.CompleteAdding();


            _loggingThread.Join();

            _logQueue.Dispose();
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}