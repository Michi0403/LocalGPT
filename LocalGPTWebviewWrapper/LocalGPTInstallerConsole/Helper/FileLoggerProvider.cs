
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LocalGPT.Helper
{
    public class FileLoggerProvider : ILoggerProvider, IDisposable
    {
        private readonly FileLoggerCoreOptions options;
        private bool disposed;

        public FileLoggerProvider(FileLoggerCoreOptions options)
        {
            this.options = options;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, options);
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}