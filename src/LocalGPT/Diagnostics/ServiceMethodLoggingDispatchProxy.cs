using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Diagnostics;

/// <summary>
/// Adds bounded operation diagnostics to LocalGPT interface services without logging arguments,
/// return values, prompts, source, database rows, credentials, or other payload content.
/// </summary>
public class ServiceMethodLoggingDispatchProxy : DispatchProxy
{
    /// <summary>
    /// Stores the internal target state used by <see cref="ServiceMethodLoggingDispatchProxy"/> while executing its surrounding workflow.
    /// </summary>
    private object? target;
    /// <summary>
    /// Stores the logger used by <see cref="ServiceMethodLoggingDispatchProxy"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private ILogger? logger;
    /// <summary>
    /// Stores the internal development state used by <see cref="ServiceMethodLoggingDispatchProxy"/> while executing its surrounding workflow.
    /// </summary>
    private bool development;
    /// <summary>
    /// Stores the in-memory operation batches collection maintained internally by <see cref="ServiceMethodLoggingDispatchProxy"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, ServiceOperationBatch> operationBatches = new(StringComparer.Ordinal);
    /// <summary>
    /// Stores the internal batch window state used by <see cref="ServiceMethodLoggingDispatchProxy"/> while executing its surrounding workflow.
    /// </summary>
    private readonly TimeSpan batchWindow = TimeSpan.FromSeconds(30);
    /// <summary>
    /// Gets the batch size that quantifies the associated service method logging dispatch proxy data.
    /// </summary>
    /// <value>The batch size value exposed by <see cref="ServiceMethodLoggingDispatchProxy"/>.</value>
    private int BatchSize => development ? 65_536 : 262_144;

    /// <summary>
    /// Performs initialize for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="serviceTarget">Service target value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="loggerFactory">Logger factory dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="isDevelopment">Value indicating whether is development should apply to this operation.</param>
    public void Initialize(object serviceTarget, ILoggerFactory loggerFactory, bool isDevelopment)
    {
        ILogger? initializationLogger = null;
        try
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            initializationLogger = loggerFactory.CreateLogger<ServiceMethodLoggingDispatchProxy>();
            target = serviceTarget ?? throw new ArgumentNullException(nameof(serviceTarget));
            logger = loggerFactory.CreateLogger(serviceTarget.GetType());
            development = isDevelopment;
            initializationLogger.LogDebug(
                "Initialized bounded service-method diagnostics for {ServiceImplementationType}; service arguments and payloads remain excluded from logs.",
                serviceTarget.GetType().FullName);
        }
        catch (Exception exception)
        {
            (initializationLogger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).LogError(
                exception,
                "Initializing a service-method diagnostics proxy failed; service arguments and payloads were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs invoke for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="targetMethod">Target method value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        var currentTarget = target ?? throw new InvalidOperationException("The service diagnostics proxy was not initialized.");
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

    /// <summary>
    /// Invokes generic observer for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="methodName">Method name value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="resultType">Result type value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="stopwatch">Stopwatch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
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

    /// <summary>
    /// Performs observe task for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="task">Task value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="stopwatch">Stopwatch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs observe task for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ServiceMethodLoggingDispatchProxy"/>.</typeparam>
    /// <param name="task">Task value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="stopwatch">Stopwatch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
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

    /// <summary>
    /// Performs observe value task for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="task">Task value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="stopwatch">Stopwatch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs observe value task for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ServiceMethodLoggingDispatchProxy"/>.</typeparam>
    /// <param name="task">Task value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="stopwatch">Stopwatch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
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

    /// <summary>
    /// Performs observe async enumerable for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ServiceMethodLoggingDispatchProxy"/>.</typeparam>
    /// <param name="source">T dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="stopwatch">Stopwatch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <returns>The i async enumerable t produced by the operation.</returns>
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

    /// <summary>
    /// Performs log started for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    private void LogStarted(ILogger currentLogger, string operation)
    {
        // Per-call diagnostics remain available at Trace, while the normal Development log receives
        // bounded aggregate summaries. This prevents hot property getters and formatting helpers from
        // monopolising the log pipeline without discarding call counts or timing information.
        currentLogger.LogTrace(
            "Service operation {Operation} started; arguments and payload content were omitted.",
            operation);
    }

    /// <summary>
    /// Performs log completed for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="elapsedMilliseconds">Elapsed milliseconds value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    private void LogCompleted(ILogger currentLogger, string operation, long elapsedMilliseconds)
    {
        currentLogger.LogTrace(
            "Service operation {Operation} completed in {ElapsedMilliseconds} ms.",
            operation,
            elapsedMilliseconds);

        RecordSuccessfulCall(currentLogger, operation, elapsedMilliseconds, forceFlush: false);
    }

    /// <summary>
    /// Performs log cancellation for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="elapsedMilliseconds">Elapsed milliseconds value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    private void LogCancellation(ILogger currentLogger, string operation, long elapsedMilliseconds, OperationCanceledException exception)
    {
        FlushBatch(currentLogger, operation);
        currentLogger.LogInformation(
            exception,
            "Service operation {Operation} was cancelled after {ElapsedMilliseconds} ms.",
            operation,
            elapsedMilliseconds);
    }

    /// <summary>
    /// Performs log failure for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="elapsedMilliseconds">Elapsed milliseconds value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
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

    /// <summary>
    /// Performs record successful call for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="elapsedMilliseconds">Elapsed milliseconds value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="forceFlush">Value indicating whether force flush should apply to this operation.</param>
    private void RecordSuccessfulCall(
        ILogger currentLogger,
        string operation,
        long elapsedMilliseconds,
        bool forceFlush)
    {
        var batch = operationBatches.GetOrAdd(operation, _ => new ServiceOperationBatch());
        ServiceOperationBatchSnapshot? snapshot = null;
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
                snapshot = new ServiceOperationBatchSnapshot(
                    batch.Count,
                    batch.TotalElapsedMilliseconds,
                    batch.MaximumElapsedMilliseconds,
                    batch.StartedAtUtc,
                    now);
                ResetBatch(batch);
            }
        }

        if (snapshot is not null)
            WriteBatch(currentLogger, operation, snapshot);
    }

    /// <summary>
    /// Performs flush batch for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    private void FlushBatch(ILogger currentLogger, string operation)
    {
        if (!operationBatches.TryGetValue(operation, out var batch))
            return;

        ServiceOperationBatchSnapshot? snapshot = null;
        lock (batch.SyncRoot)
        {
            if (batch.Count > 0)
            {
                var now = DateTimeOffset.UtcNow;
                snapshot = new ServiceOperationBatchSnapshot(
                    batch.Count,
                    batch.TotalElapsedMilliseconds,
                    batch.MaximumElapsedMilliseconds,
                    batch.StartedAtUtc,
                    now);
                ResetBatch(batch);
            }
        }

        if (snapshot is not null)
            WriteBatch(currentLogger, operation, snapshot);
    }

    /// <summary>
    /// Performs reset batch for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="batch">Batch value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    private void ResetBatch(ServiceOperationBatch batch)
    {
        batch.Count = 0;
        batch.TotalElapsedMilliseconds = 0;
        batch.MaximumElapsedMilliseconds = 0;
        batch.StartedAtUtc = default;
    }

    /// <summary>
    /// Writes batch for <see cref="ServiceMethodLoggingDispatchProxy"/>, keeping the operation consistent with the state and invariants of the surrounding service method logging dispatch proxy workflow.
    /// </summary>
    /// <param name="currentLogger">Logger dependency used by the service method logging dispatch proxy workflow to provide the corresponding application capability.</param>
    /// <param name="operation">Operation value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    /// <param name="snapshot">Snapshot value supplied to the service method logging dispatch proxy operation and used when producing its result.</param>
    private void WriteBatch(ILogger currentLogger, string operation, ServiceOperationBatchSnapshot snapshot)
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
}
