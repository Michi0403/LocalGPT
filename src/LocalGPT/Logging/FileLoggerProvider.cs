using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LocalGPT.Logging
{
    /// <summary>
    /// Provides file logger provider operations.
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider, IDisposable
    {
        private readonly IOptionsMonitor<FileLoggerCoreOptions> options;
        private bool disposed;

        /// <summary>
        /// Runs the file logger provider operation.
        /// </summary>
        public FileLoggerProvider(IOptionsMonitor<FileLoggerCoreOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// Creates logger.
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, options);
        }

        /// <summary>
        /// Runs the dispose operation.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {

                }


                disposed = true;
            }
        }

        /// <summary>
        /// Runs the dispose operation.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}