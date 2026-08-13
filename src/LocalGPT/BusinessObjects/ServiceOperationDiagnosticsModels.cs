namespace LocalGPT.BusinessObjects;

/// <summary>
/// Mutable aggregate for one service-operation diagnostics batch. The object is data only;
/// batching and logging behavior remain owned by the diagnostics proxy.
/// </summary>
public sealed class ServiceOperationBatch
{
    /// <summary>
    /// Gets the sync root value that forms part of the service operation batch state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="ServiceOperationBatch"/>.</value>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets or sets the count that quantifies the associated service operation batch data.
    /// </summary>
    /// <value>The count value exposed by <see cref="ServiceOperationBatch"/>.</value>
    public long Count { get; set; }
    /// <summary>
    /// Gets or sets the total elapsed milliseconds value that forms part of the service operation batch state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The total elapsed milliseconds value exposed by <see cref="ServiceOperationBatch"/>.</value>
    public long TotalElapsedMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the maximum elapsed milliseconds value that forms part of the service operation batch state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The maximum elapsed milliseconds value exposed by <see cref="ServiceOperationBatch"/>.</value>
    public long MaximumElapsedMilliseconds { get; set; }
    /// <summary>
    /// Gets or sets the started at UTC associated with this service operation batch state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The started at UTC value exposed by <see cref="ServiceOperationBatch"/>.</value>
    public DateTimeOffset StartedAtUtc { get; set; }
}

/// <summary>
/// Represents a service operation batch snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Count">Count value supplied to the service operation batch snapshot operation and used when producing its result.</param>
/// <param name="TotalElapsedMilliseconds">Total elapsed milliseconds value supplied to the service operation batch snapshot operation and used when producing its result.</param>
/// <param name="MaximumElapsedMilliseconds">Maximum elapsed milliseconds value supplied to the service operation batch snapshot operation and used when producing its result.</param>
/// <param name="StartedAtUtc">Started at utc value supplied to the service operation batch snapshot operation and used when producing its result.</param>
/// <param name="EndedAtUtc">Ended at utc value supplied to the service operation batch snapshot operation and used when producing its result.</param>
public sealed record ServiceOperationBatchSnapshot(
    long Count,
    long TotalElapsedMilliseconds,
    long MaximumElapsedMilliseconds,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc);
