using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LocalGPT.Logging
{
    public class EmailLoggerProvider(IOptionsMonitor<EmailLoggerCoreOptions> options) : ILoggerProvider
    {

        public ILogger CreateLogger(string categoryName)
        {
            return new EmailLogger(categoryName, options);
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
        private bool disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
