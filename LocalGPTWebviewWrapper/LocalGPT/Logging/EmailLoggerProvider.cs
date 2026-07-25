using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LocalGPT.Logging;

public sealed class EmailLoggerProvider(IOptionsMonitor<EmailLoggerCoreOptions> options) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, EmailLogger> loggers =
        new(StringComparer.Ordinal);
    private int disposed;

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        return loggers.GetOrAdd(categoryName, name => new EmailLogger(name, options));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        foreach (var logger in loggers.Values)
            logger.Dispose();

        loggers.Clear();
    }
}
