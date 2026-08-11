using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LocalGPT.Logging;

/// <summary>
/// Provides email logger provider operations.
/// </summary>
public sealed class EmailLoggerProvider(IOptionsMonitor<EmailLoggerCoreOptions> options) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, EmailLogger> loggers =
        new(StringComparer.Ordinal);
    private int disposed;

    /// <summary>
    /// Creates logger.
    /// </summary>
    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(System.Threading.Volatile.Read(ref disposed) != 0, this);
        return loggers.GetOrAdd(categoryName, name => new EmailLogger(name, options));
    }

    /// <summary>
    /// Runs the dispose operation.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        foreach (var logger in loggers.Values)
            logger.Dispose();

        loggers.Clear();
    }
}
