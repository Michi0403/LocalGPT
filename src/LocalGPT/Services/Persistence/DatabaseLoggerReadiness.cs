using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// One-way startup gate shared by database initialization and the database logger worker.
/// </summary>
public sealed class DatabaseLoggerReadiness : IDatabaseLoggerReadiness
{
    /// <summary>
    /// Stores the internal ready state used by <see cref="DatabaseLoggerReadiness"/> while executing its surrounding workflow.
    /// </summary>
    private readonly TaskCompletionSource<bool> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets a value indicating whether ready applies to the database logger readiness state.
    /// </summary>
    /// <value>The is ready value exposed by <see cref="DatabaseLoggerReadiness"/>.</value>
    public bool IsReady => ready.Task.IsCompletedSuccessfully;

    /// <summary>
    /// Performs wait until ready for <see cref="DatabaseLoggerReadiness"/>, keeping the operation consistent with the state and invariants of the surrounding database logger readiness workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            if (IsReady)
                return Task.CompletedTask;

            return ready.Task.WaitAsync(cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method DatabaseLoggerReadiness.WaitUntilReadyAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs mark ready for <see cref="DatabaseLoggerReadiness"/>, keeping the operation consistent with the state and invariants of the surrounding database logger readiness workflow.
    /// </summary>
    public void MarkReady() {
    try
    {
        ready.TrySetResult(true);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method DatabaseLoggerReadiness.MarkReady failed: {__serviceMethodException}");
        throw;
    }
}
}
