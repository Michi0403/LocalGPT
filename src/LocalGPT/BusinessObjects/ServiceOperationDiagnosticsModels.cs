namespace LocalGPT.BusinessObjects;

/// <summary>
/// Mutable aggregate for one service-operation diagnostics batch. The object is data only;
/// batching and logging behavior remain owned by the diagnostics proxy.
/// </summary>
public sealed class ServiceOperationBatch
{
    /// <summary>
    /// Gets or sets sync root.
    /// </summary>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets count.
    /// </summary>
    public long Count { get; set; }
    /// <summary>
    /// Gets or sets total elapsed milliseconds.
    /// </summary>
    public long TotalElapsedMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets maximum elapsed milliseconds.
    /// </summary>
    public long MaximumElapsedMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets started at UTC.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }
}

/// <summary>
/// Represents a service operation batch snapshot.
/// </summary>
public sealed record ServiceOperationBatchSnapshot(
    long Count,
    long TotalElapsedMilliseconds,
    long MaximumElapsedMilliseconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc);
