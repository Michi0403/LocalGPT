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
        if (IsReady)
            return Task.CompletedTask;

        return ready.Task.WaitAsync(cancellationToken);
    }

    public void MarkReady() => ready.TrySetResult(true);
}
