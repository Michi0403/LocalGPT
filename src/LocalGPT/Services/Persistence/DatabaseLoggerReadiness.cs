using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// One-way startup gate shared by database initialization and the database logger worker.
/// </summary>
public sealed class DatabaseLoggerReadiness : IDatabaseLoggerReadiness
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly TaskCompletionSource<bool> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets or sets is ready.
    /// </summary>
    public bool IsReady => ready.Task.IsCompletedSuccessfully;

    /// <summary>
    /// Runs the wait until ready async operation.
    /// </summary>
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
    /// Runs the mark ready operation.
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
