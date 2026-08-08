namespace LocalGPT.Services;

/// <summary>
/// Serializes DX function catalog synchronization and policy writes across all scoped catalog-service instances.
/// </summary>
public sealed class DxAiFunctionCatalogSynchronizationGate
{
    private readonly SemaphoreSlim gate = new(1, 1);

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
