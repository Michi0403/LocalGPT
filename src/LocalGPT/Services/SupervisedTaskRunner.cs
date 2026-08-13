using System.Collections.Concurrent;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Observes intentionally concurrent work so exceptions cannot become unobserved Tasks.
/// The runner is process-scoped, but every operation must carry its owning component/service name.
/// </summary>
/// <param name="serviceActivity">Service activity service dependency used by the supervised task runner workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SupervisedTaskRunner(
    IServiceActivityService serviceActivity,
    ILogger<SupervisedTaskRunner> logger) : ISupervisedTaskRunner
{
    /// <summary>
    /// Stores the in-memory active tasks collection maintained internally by <see cref="SupervisedTaskRunner"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<long, Task> activeTasks = new();
    /// <summary>
    /// Stores the internal next task identifier state used by <see cref="SupervisedTaskRunner"/> while executing its surrounding workflow.
    /// </summary>
    private long nextTaskId;

    /// <summary>
    /// Gets the active task count that quantifies the associated supervised task runner data.
    /// </summary>
    /// <value>The active task count value exposed by <see cref="SupervisedTaskRunner"/>.</value>
    public int ActiveTaskCount => activeTasks.Count;

    /// <summary>
    /// Performs run for <see cref="SupervisedTaskRunner"/>, keeping the operation consistent with the state and invariants of the surrounding supervised task runner workflow.
    /// </summary>
    /// <param name="owner">Owner value supplied to the supervised task runner operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the supervised task runner operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
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
    /// Performs observe for <see cref="SupervisedTaskRunner"/>, keeping the operation consistent with the state and invariants of the surrounding supervised task runner workflow.
    /// </summary>
    /// <param name="taskId">Identifier of the task to use for this operation.</param>
    /// <param name="owner">Owner value supplied to the supervised task runner operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the supervised task runner operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
