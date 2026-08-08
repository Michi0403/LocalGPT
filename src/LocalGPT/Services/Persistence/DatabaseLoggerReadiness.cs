using LocalGPT.Interfaces;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// One-way startup gate shared by database initialization and the database logger worker.
/// </summary>
public sealed class DatabaseLoggerReadiness : IDatabaseLoggerReadiness
{
    private readonly TaskCompletionSource<bool> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => ready.Task.IsCompletedSuccessfully;

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
