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
        if (ownsTarget && target is IDisposable disposable)
            disposable.Dispose();
        target = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
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
        var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == methodName && candidate.IsGenericMethodDefinition);
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
        try
        {
            await foreach (var item in source.ConfigureAwait(false))
                yield return item;
           
        }
        finally
        {
            stopwatch.Stop();
            LogCompleted(currentLogger, operation, stopwatch.ElapsedMilliseconds);
        }
      
    }

    private void LogStarted(ILogger currentLogger, string operation)
    {
        if (development)
            currentLogger.LogInformation("Service operation {Operation} started; arguments and payload content were omitted.", operation);
        else
            currentLogger.LogTrace("Service operation {Operation} started; arguments and payload content were omitted.", operation);
    }

    private void LogCompleted(ILogger currentLogger, string operation, long elapsedMilliseconds)
    {
        if (development)
            currentLogger.LogInformation("Service operation {Operation} completed in {ElapsedMilliseconds} ms.", operation, elapsedMilliseconds);
        else
            currentLogger.LogTrace("Service operation {Operation} completed in {ElapsedMilliseconds} ms.", operation, elapsedMilliseconds);
    }

    private void LogCancellation(ILogger currentLogger, string operation, long elapsedMilliseconds, OperationCanceledException exception)
    {
        // Cancellation is expected control flow. It is emitted only by Debug builds,
        // at Information rather than LogLevel.Debug.
#if DEBUG
        currentLogger.LogInformation(exception, "Service operation {Operation} was cancelled after {ElapsedMilliseconds} ms in a Debug build.", operation, elapsedMilliseconds);
#endif
    }

    private void LogFailure(ILogger currentLogger, string operation, long elapsedMilliseconds, Exception exception)
    {
        if (exception is OperationCanceledException cancellation)
        {
            LogCancellation(currentLogger, operation, elapsedMilliseconds, cancellation);
            return;
        }

        currentLogger.LogError(
            exception,
            "Service operation {Operation} failed after {ElapsedMilliseconds} ms; arguments, return values and payload content were omitted.",
            operation,
            elapsedMilliseconds);
    }
}
