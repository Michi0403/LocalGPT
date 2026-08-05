namespace LocalGPT.BusinessObjects;

/// <summary>
/// Mutable aggregate for one service-operation diagnostics batch. The object is data only;
/// batching and logging behavior remain owned by the diagnostics proxy.
/// </summary>
public sealed class ServiceOperationBatch
{
    public object SyncRoot { get; } = new();
    public long Count { get; set; }
    public long TotalElapsedMilliseconds { get; set; }
    public long MaximumElapsedMilliseconds { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
}

public sealed record ServiceOperationBatchSnapshot(
    long Count,
    long TotalElapsedMilliseconds,
    long MaximumElapsedMilliseconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc);
