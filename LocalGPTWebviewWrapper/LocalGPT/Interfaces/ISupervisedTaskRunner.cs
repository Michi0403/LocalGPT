namespace LocalGPT.Interfaces;

/// <summary>
/// Starts intentionally non-blocking work while observing completion, cancellation, and failure.
/// Use this instead of discarding a Task returned by an asynchronous method.
/// </summary>
public interface ISupervisedTaskRunner
{
    int ActiveTaskCount { get; }

    void Run(
        string owner,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
