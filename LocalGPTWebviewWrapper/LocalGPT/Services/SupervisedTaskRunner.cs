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
    private readonly ConcurrentDictionary<long, Task> activeTasks = new();
    private long nextTaskId;

    public int ActiveTaskCount => activeTasks.Count;

    public void Run(
        string owner,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(action);

        var taskId = Interlocked.Increment(ref nextTaskId);
        var task = ObserveAsync(taskId, owner, operation, action, cancellationToken);
        if (!activeTasks.TryAdd(taskId, task))
            throw new InvalidOperationException($"Could not track supervised task {taskId}.");

        task.GetAwaiter().OnCompleted(() => activeTasks.TryRemove(taskId, out _));
    }

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
