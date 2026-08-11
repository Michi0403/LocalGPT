namespace LocalGPT.Interfaces;

/// <summary>
/// Records bounded, sanitized service-operation state for LocalGPT short-term context.
/// The executor never swallows failures: cancellation and exceptions are recorded and rethrown.
/// </summary>
public interface IServiceActivityService
{
    /// <summary>
    /// Runs the record information operation.
    /// </summary>
    void RecordInformation(string serviceName, string operation, string summary, string? route = null);
    /// <summary>
    /// Runs the record warning operation.
    /// </summary>
    void RecordWarning(string serviceName, string operation, string summary, string? route = null);
    /// <summary>
    /// Runs the record failure operation.
    /// </summary>
    void RecordFailure(string serviceName, string operation, Exception exception, string? route = null);

    /// <summary>
    /// Runs the run async operation.
    /// </summary>
    Task RunAsync(
        string serviceName,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        string? successSummary = null);

    /// <summary>
    /// Runs the run async operation.
    /// </summary>
    Task<T> RunAsync<T>(
        string serviceName,
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        string? successSummary = null);
}
