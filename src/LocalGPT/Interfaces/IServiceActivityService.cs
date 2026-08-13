namespace LocalGPT.Interfaces;

/// <summary>
/// Records bounded, sanitized service-operation state for LocalGPT short-term context.
/// The executor never swallows failures: cancellation and exceptions are recorded and rethrown.
/// </summary>
public interface IServiceActivityService
{
    /// <summary>
    /// Performs record information as part of the service activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="serviceName">Service name value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the service activity operation and used when producing its result.</param>
    void RecordInformation(string serviceName, string operation, string summary, string? route = null);
    /// <summary>
    /// Performs record warning as part of the service activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="serviceName">Service name value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the service activity operation and used when producing its result.</param>
    void RecordWarning(string serviceName, string operation, string summary, string? route = null);
    /// <summary>
    /// Performs record failure as part of the service activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="serviceName">Service name value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the service activity operation and used when producing its result.</param>
    void RecordFailure(string serviceName, string operation, Exception exception, string? route = null);

    /// <summary>
    /// Performs run as part of the service activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="serviceName">Service name value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="successSummary">Success summary value supplied to the service activity operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RunAsync(
        string serviceName,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        string? successSummary = null);

    /// <summary>
    /// Performs run as part of the service activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="IServiceActivityService"/>.</typeparam>
    /// <param name="serviceName">Service name value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the service activity operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="successSummary">Success summary value supplied to the service activity operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    Task<T> RunAsync<T>(
        string serviceName,
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        string? successSummary = null);
}
