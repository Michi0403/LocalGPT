namespace LocalGPT.Services;

/// <summary>
/// Serializes DX function catalog synchronization and policy writes across all scoped catalog-service instances.
/// </summary>
public sealed class DxAiFunctionCatalogSynchronizationGate
{
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="DxAiFunctionCatalogSynchronizationGate"/>.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>
    /// Performs wait for <see cref="DxAiFunctionCatalogSynchronizationGate"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function catalog synchronization gate workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task WaitAsync(CancellationToken cancellationToken) {
    try
    {
        return gate.WaitAsync(cancellationToken);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method DxAiFunctionCatalogSynchronizationGate.WaitAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs release for <see cref="DxAiFunctionCatalogSynchronizationGate"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function catalog synchronization gate workflow.
    /// </summary>
    public void Release() {
    try
    {
        gate.Release();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method DxAiFunctionCatalogSynchronizationGate.Release failed: {__serviceMethodException}");
        throw;
    }
}
}
