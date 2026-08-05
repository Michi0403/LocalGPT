namespace LocalGPT.Services;

/// <summary>
/// Serializes DX function catalog synchronization and policy writes across all scoped catalog-service instances.
/// </summary>
public sealed class DxAiFunctionCatalogSynchronizationGate
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public Task WaitAsync(CancellationToken cancellationToken) => gate.WaitAsync(cancellationToken);

    public void Release() => gate.Release();
}
