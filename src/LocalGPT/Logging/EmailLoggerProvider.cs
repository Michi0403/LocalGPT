using LocalGPT.BusinessObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LocalGPT.Logging;

/// <summary>
/// Provides email logger data or behavior to callers while hiding the underlying acquisition and configuration details.
/// </summary>
/// <param name="options">Options containing the caller-supplied values that control this operation.</param>
public sealed class EmailLoggerProvider(IOptionsMonitor<EmailLoggerCoreOptions> options) : ILoggerProvider
{
    /// <summary>
    /// Stores the in-memory loggers collection maintained internally by <see cref="EmailLoggerProvider"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, EmailLogger> loggers =
        new(StringComparer.Ordinal);
    /// <summary>
    /// Stores the internal disposed state used by <see cref="EmailLoggerProvider"/> while executing its surrounding workflow.
    /// </summary>
    private int disposed;

    /// <summary>
    /// Creates logger for <see cref="EmailLoggerProvider"/>, keeping the operation consistent with the state and invariants of the surrounding email logger workflow.
    /// </summary>
    /// <param name="categoryName">Category name value supplied to the email logger operation and used when producing its result.</param>
    /// <returns>The i logger produced by the operation.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(System.Threading.Volatile.Read(ref disposed) != 0, this);
        return loggers.GetOrAdd(categoryName, name => new EmailLogger(name, options));
    }

    /// <summary>
    /// Releases resources owned by <see cref="EmailLoggerProvider"/> and leaves the email logger workflow in a safely disposed state.
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
