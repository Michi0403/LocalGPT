using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LocalGPT.Diagnostics;

/// <summary>
/// Adds bounded operation diagnostics to LocalGPT interface services without logging arguments,
/// return values, prompts, source, database rows, credentials, or other payload content.
/// </summary>
public class ServiceMethodLoggingDispatchProxy : DispatchProxy, IDisposable, IAsyncDisposable
{
    private object? target;
    private ILogger? logger;
    private bool development;
    private bool ownsTarget;
    private int disposed;
    private readonly ConcurrentDictionary<string, OperationBatch> operationBatches = new(StringComparer.Ordinal);
    private readonly TimeSpan batchWindow = TimeSpan.FromSeconds(2);
    private int BatchSize => development ? 32 : 128;

    public void Initialize(object serviceTarget, ILoggerFactory loggerFactory, bool isDevelopment, bool ownsServiceTarget)
    {
        target = serviceTarget ?? throw new ArgumentNullException(nameof(serviceTarget));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        logger = loggerFactory.CreateLogger(serviceTarget.GetType());
        development = isDevelopment;
        ownsTarget = ownsServiceTarget;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        var currentTarget = target ?? throw new ObjectDisposedException(nameof(ServiceMethodLoggingDispatchProxy));
        var currentLogger = logger ?? throw new InvalidOperationException("The service diagnostics proxy was not initialized.");
        var operation = $"{currentTarget.GetType().Name}.{targetMethod.Name}";
        var stopwatch = Stopwatch.StartNew();
        LogStarted(currentLogger, operation);

        object? result;
        try
        {
            result = targetMethod.Invoke(currentTarget, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            stopwatch.Stop();
            LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception.InnerException);
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }

        var returnType = targetMethod.ReturnType;
        if (returnType == typeof(Task))
            return ObserveTaskAsync((Task)result!, currentLogger, operation, stopwatch);
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            return InvokeGenericObserver(nameof(ObserveTaskAsync), returnType.GenericTypeArguments[0], result!, currentLogger, operation, stopwatch);
        if (returnType == typeof(ValueTask))
            return new ValueTask(ObserveValueTaskAsync((ValueTask)result!, currentLogger, operation, stopwatch));
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            return InvokeGenericObserver(nameof(ObserveValueTaskAsync), returnType.GenericTypeArguments[0], result!, currentLogger, operation, stopwatch);
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            return InvokeGenericObserver(nameof(ObserveAsyncEnumerable), returnType.GenericTypeArguments[0], result!, currentLogger, operation, stopwatch);

        stopwatch.Stop();
        LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
        return result;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        FlushAllBatches();
        if (ownsTarget && target is IDisposable disposable)
            disposable.Dispose();
        target = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        FlushAllBatches();
        if (ownsTarget && target is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else if (ownsTarget && target is IDisposable disposable)
            disposable.Dispose();
        target = null;
    }

    private object InvokeGenericObserver(
        string methodName,
        Type resultType,
        object result,
        ILogger currentLogger,
        string operation,
        Stopwatch stopwatch)
    {
        // DispatchProxy creates a generated subclass at runtime. GetType() therefore points to
        // generatedProxy_N, whose reflection surface does not include private methods declared
        // on this base type. Resolve observers from their actual declaring type instead.
        var method = typeof(ServiceMethodLoggingDispatchProxy)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Single(candidate =>
                candidate.Name == methodName
                && candidate.IsGenericMethodDefinition
                && candidate.GetGenericArguments().Length == 1);
        return method.MakeGenericMethod(resultType)
            .Invoke(this, [result, currentLogger, operation, stopwatch])!;
    }

    private async Task ObserveTaskAsync(Task task, ILogger currentLogger, string operation, Stopwatch stopwatch)
    {
        try
        {
            await task.ConfigureAwait(false);
            stopwatch.Stop();
            LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException exception)
        {
            stopwatch.Stop();
            LogCancellation(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
    }

    private async Task<T> ObserveTaskAsync<T>(Task<T> task, ILogger currentLogger, string operation, Stopwatch stopwatch)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            stopwatch.Stop();
            LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            stopwatch.Stop();
            LogCancellation(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
    }

    private async Task ObserveValueTaskAsync(ValueTask task, ILogger currentLogger, string operation, Stopwatch stopwatch)
    {
        try
        {
            await task.ConfigureAwait(false);
            stopwatch.Stop();
            LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException exception)
        {
            stopwatch.Stop();
            LogCancellation(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
    }

    private async ValueTask<T> ObserveValueTaskAsync<T>(ValueTask<T> task, ILogger currentLogger, string operation, Stopwatch stopwatch)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            stopwatch.Stop();
            LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            stopwatch.Stop();
            LogCancellation(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
            throw;
        }
    }

    private async IAsyncEnumerable<T> ObserveAsyncEnumerable<T>(
        IAsyncEnumerable<T> source,
        ILogger currentLogger,
        string operation,
        Stopwatch stopwatch)
    {
        var terminalEventLogged = false;
        var enumerator = source.GetAsyncEnumerator();

        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    stopwatch.Stop();
                    LogCancellation(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
                    terminalEventLogged = true;
                    throw;
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();
                    LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
                    terminalEventLogged = true;
                    throw;
                }

                if (!hasNext)
                    yield break;

                yield return enumerator.Current;
            }
        }
        finally
        {
            try
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                if (!terminalEventLogged)
                {
                    stopwatch.Stop();
                    LogCancellation(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
                    terminalEventLogged = true;
                    throw;
                }
            }
            catch (Exception exception)
            {
                if (!terminalEventLogged)
                {
                    stopwatch.Stop();
                    LogFailure(currentLogger, operation, stopwatch.ElapsedMilliseconds, exception);
                    terminalEventLogged = true;
                    throw;
                }

                currentLogger.LogWarning(
                    exception,
                    "Service operation {Operation} also failed while disposing its asynchronous enumerator.",
                    operation);
            }

            if (!terminalEventLogged)
            {
                stopwatch.Stop();
                LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private void LogStarted(ILogger currentLogger, string operation)
    {
        // Per-call diagnostics remain available at Trace, while the normal Development log receives
        // bounded aggregate summaries. This prevents hot property getters and formatting helpers from
        // monopolising the log pipeline without discarding call counts or timing information.
        currentLogger.LogTrace(
            "Service operation {Operation} started; arguments and payload content were omitted.",
            operation);
    }

    private void LogCompleted(ILogger currentLogger, string operation, long elapsedMilliseconds)
    {
        currentLogger.LogTrace(
            "Service operation {Operation} completed in {ElapsedMilliseconds} ms.",
            operation,
            elapsedMilliseconds);

        RecordSuccessfulCall(currentLogger, operation, elapsedMilliseconds, forceFlush: false);
    }

    private void LogCancellation(ILogger currentLogger, string operation, long elapsedMilliseconds, OperationCanceledException exception)
    {
        FlushBatch(currentLogger, operation);
        currentLogger.LogInformation(
            exception,
            "Service operation {Operation} was cancelled after {ElapsedMilliseconds} ms.",
            operation,
            elapsedMilliseconds);
    }

    private void LogFailure(ILogger currentLogger, string operation, long elapsedMilliseconds, Exception exception)
    {
        if (exception is OperationCanceledException cancellation)
        {
            LogCancellation(currentLogger, operation, elapsedMilliseconds, cancellation);
            return;
        }

        FlushBatch(currentLogger, operation);
        currentLogger.LogError(
            exception,
            "Service operation {Operation} failed after {ElapsedMilliseconds} ms; arguments, return values and payload content were omitted.",
            operation,
            elapsedMilliseconds);
    }

    private void RecordSuccessfulCall(
        ILogger currentLogger,
        string operation,
        long elapsedMilliseconds,
        bool forceFlush)
    {
        var batch = operationBatches.GetOrAdd(operation, _ => new OperationBatch());
        OperationBatchSnapshot? snapshot = null;
        lock (batch.SyncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            if (batch.Count == 0)
                batch.StartedAtUtc = now;

            batch.Count++;
            batch.TotalElapsedMilliseconds += elapsedMilliseconds;
            batch.MaximumElapsedMilliseconds = Math.Max(batch.MaximumElapsedMilliseconds, elapsedMilliseconds);

            if (forceFlush || batch.Count >= BatchSize || now - batch.StartedAtUtc >= batchWindow)
            {
                snapshot = new OperationBatchSnapshot(
                    batch.Count,
                    batch.TotalElapsedMilliseconds,
                    batch.MaximumElapsedMilliseconds,
                    batch.StartedAtUtc,
                    now);
                batch.Reset();
            }
        }

        if (snapshot is not null)
            WriteBatch(currentLogger, operation, snapshot);
    }

    private void FlushAllBatches()
    {
        var currentLogger = logger;
        if (currentLogger is null) return;
        foreach (var operation in operationBatches.Keys)
            FlushBatch(currentLogger, operation);
    }

    private void FlushBatch(ILogger currentLogger, string operation)
    {
        if (!operationBatches.TryGetValue(operation, out var batch))
            return;

        OperationBatchSnapshot? snapshot = null;
        lock (batch.SyncRoot)
        {
            if (batch.Count > 0)
            {
                var now = DateTimeOffset.UtcNow;
                snapshot = new OperationBatchSnapshot(
                    batch.Count,
                    batch.TotalElapsedMilliseconds,
                    batch.MaximumElapsedMilliseconds,
                    batch.StartedAtUtc,
                    now);
                batch.Reset();
            }
        }

        if (snapshot is not null)
            WriteBatch(currentLogger, operation, snapshot);
    }

    private void WriteBatch(ILogger currentLogger, string operation, OperationBatchSnapshot snapshot)
    {
        var average = snapshot.Count == 0
            ? 0d
            : (double)snapshot.TotalElapsedMilliseconds / snapshot.Count;
        currentLogger.LogInformation(
            "Service operation batch {Operation}: {InvocationCount} successful call(s) in {BatchDurationMilliseconds} ms; aggregate {TotalElapsedMilliseconds} ms, average {AverageElapsedMilliseconds:F2} ms, maximum {MaximumElapsedMilliseconds} ms. Arguments and payload content were omitted.",
            operation,
            snapshot.Count,
            Math.Max(0, (long)(snapshot.EndedAtUtc - snapshot.StartedAtUtc).TotalMilliseconds),
            snapshot.TotalElapsedMilliseconds,
            average,
            snapshot.MaximumElapsedMilliseconds);
    }

    private sealed class OperationBatch
    {
        public object SyncRoot { get; } = new();
        public int Count { get; set; }
        public long TotalElapsedMilliseconds { get; set; }
        public long MaximumElapsedMilliseconds { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }

        public void Reset()
        {
            Count = 0;
            TotalElapsedMilliseconds = 0;
            MaximumElapsedMilliseconds = 0;
            StartedAtUtc = default;
        }
    }

    private sealed record OperationBatchSnapshot(
        int Count,
        long TotalElapsedMilliseconds,
        long MaximumElapsedMilliseconds,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset EndedAtUtc);

}
