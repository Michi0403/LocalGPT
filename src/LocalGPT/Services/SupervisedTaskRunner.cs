using System.Collections.Concurrent;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Observes intentionally concurrent work so exceptions cannot become unobserved Tasks.
/// The runner is process-scoped, but every operation must carry its owning component/service name.
/// </summary>
public sealed class SupervisedTaskRunner(
    IServiceActivityService serviceActivity,
    ILogger<SupervisedTaskRunner> logger) : ISupervisedTaskRunner
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<long, Task> activeTasks = new();
    private long nextTaskId;

    /// <summary>
    /// Gets or sets active task count.
    /// </summary>
    public int ActiveTaskCount => activeTasks.Count;

    /// <summary>
    /// Runs the run operation.
    /// </summary>
    public void Run(
        string owner,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);
            ArgumentNullException.ThrowIfNull(action);

            var taskId = Interlocked.Increment(ref nextTaskId);
            // A component can call Run from the Blazor renderer synchronization context. Starting the
            // observer through Task.Run keeps intentionally concurrent service/network work from
            // executing its synchronous prefix on the renderer and freezing the entire circuit.
            var task = Task.Run(
                () => ObserveAsync(taskId, owner, operation, action, cancellationToken),
                CancellationToken.None);
            if (!activeTasks.TryAdd(taskId, task))
                throw new InvalidOperationException($"Could not track supervised task {taskId}.");

            task.GetAwaiter().OnCompleted(() => activeTasks.TryRemove(taskId, out _));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SupervisedTaskRunner)}.{nameof(Run)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SupervisedTaskRunner)}.{nameof(Run)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the observe async operation.
    /// </summary>
    private async Task ObserveAsync(
        long taskId,
        string owner,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        try
        {
            await serviceActivity
                .RunAsync(owner, operation, action, cancellationToken, "The supervised operation completed.")
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // IServiceActivityService already recorded and logged the supervised cancellation.
        }
        catch (Exception)
        {
            logger.LogDebug(
                "Supervised task {TaskId} for {Owner}/{Operation} was observed after its failure was recorded.",
                taskId,
                owner,
                operation);
        }
    }
}
