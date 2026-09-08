using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace LocalGPT.Logging
{
    /// <summary>
    /// Provides file logger data or behavior to callers while hiding the underlying acquisition and configuration details.
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider, IDisposable
    {
        /// <summary>
        /// Stores the options monitor dependency used by <see cref="FileLoggerProvider"/> to delegate that application responsibility to its owning collaborator.
        /// </summary>
        private readonly IOptionsMonitor<FileLoggerCoreOptions> options;
        /// <summary>
        /// Stores the internal disposed state used by <see cref="FileLoggerProvider"/> while executing its surrounding workflow.
        /// </summary>
        private bool disposed;
        /// <summary>Tracks whether the provider already reported a logger-construction failure to avoid repeating the same console warning for every category.</summary>
        private int creationFailureReported;

        /// <summary>
        /// Initializes a new <see cref="FileLoggerProvider"/> instance and captures the dependencies or initial state required by its file logger workflow.
        /// </summary>
        /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
        public FileLoggerProvider(IOptionsMonitor<FileLoggerCoreOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// Creates logger for <see cref="FileLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding file logger workflow.
        /// </summary>
        /// <param name="categoryName">Category name value supplied to the file logger operation and used when producing its result.</param>
        /// <returns>The i logger produced by the operation.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            try
            {
                return new FileLogger(categoryName, options);
            }
            catch (Exception exception)
            {
                if (Interlocked.Exchange(ref creationFailureReported, 1) == 0)
                    Console.Error.WriteLine($"LocalGPT file logging was disabled because its target could not be initialized: {exception.Message}");
                return NullLogger.Instance;
            }
        }

        /// <summary>
        /// Releases resources owned by <see cref="FileLoggerProvider"/> and leaves the file logger workflow in a safely disposed state.
        /// </summary>
        /// <param name="disposing">Value indicating whether disposing should apply to this operation.</param>
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
        /// Releases resources owned by <see cref="FileLoggerProvider"/> and leaves the file logger workflow in a safely disposed state.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
