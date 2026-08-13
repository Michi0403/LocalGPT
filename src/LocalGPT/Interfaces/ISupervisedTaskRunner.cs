namespace LocalGPT.Interfaces;

/// <summary>
/// Starts intentionally non-blocking work while observing completion, cancellation, and failure.
/// Use this instead of discarding a Task returned by an asynchronous method.
/// </summary>
public interface ISupervisedTaskRunner
{
    /// <summary>
    /// Gets the active task count that quantifies the associated supervised task runner data.
    /// </summary>
    /// <value>The active task count value exposed by <see cref="ISupervisedTaskRunner"/>.</value>
    int ActiveTaskCount { get; }

    /// <summary>
    /// Performs run for <see cref="ISupervisedTaskRunner"/>, keeping the operation consistent with the state and invariants of the surrounding supervised task runner workflow.
    /// </summary>
    /// <param name="owner">Owner value supplied to the supervised task runner operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the supervised task runner operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    void Run(
        string owner,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
