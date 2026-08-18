using System.Collections.Concurrent;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Bounded, process-local operational memory for the current LocalGPT run.
/// It records concise UI and service state only; prompts, message bodies, secrets,
/// uploaded content, generated source, parameters, and full exception text are excluded.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ComponentActivityService(ILogger<ComponentActivityService> logger) :
    IComponentActivityService,
    IServiceActivityService
{
    /// <summary>
    /// Defines the capacity constant used by <see cref="ComponentActivityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int Capacity = 192;
    /// <summary>
    /// Defines the max summary characters constant used by <see cref="ComponentActivityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxSummaryCharacters = 320;
    /// <summary>
    /// Stores the internal entries state used by <see cref="ComponentActivityService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ConcurrentQueue<ComponentActivitySnapshot> entries = new();

    /// <summary>
    /// Performs record navigation as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    public void RecordNavigation(string route) {
    try
    {
        RecordInformation("Router", "Navigation", "The user opened a LocalGPT route.", NormalizeRoute(route));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordNavigation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordNavigation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs record information as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    public void RecordInformation(string component, string operation, string summary, string? route = null) {
    try
    {
        Enqueue(component, operation, "Information", summary, route);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordInformation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordInformation)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs record warning as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    public void RecordWarning(string component, string operation, string summary, string? route = null) {
    try
    {
        Enqueue(component, operation, "Warning", summary, route);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordWarning)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordWarning)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs record failure as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    public void RecordFailure(string component, string operation, Exception exception, string? route = null)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(exception);
            Enqueue(
                component,
                operation,
                "Error",
                $"{exception.GetType().Name}: the operation failed; sensitive details remain only in application logs.",
                route);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordFailure)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(RecordFailure)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs run as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="serviceName">Service name value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="successSummary">Success summary value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task RunAsync(
        string serviceName,
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default,
        string? successSummary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(action);

        RecordInformation(serviceName, operation, "The service operation started.");
        try
        {
            await action(cancellationToken).ConfigureAwait(false);
            RecordInformation(
                serviceName,
                operation,
                string.IsNullOrWhiteSpace(successSummary)
                    ? "The service operation completed."
                    : successSummary);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RecordInformation(serviceName, operation, "The service operation was cancelled.");
            logger.LogDebug(
                "Service activity {ServiceName}/{Operation} ended because its caller cancellation token was signaled.",
                serviceName,
                operation);
            throw;
        }
        catch (Exception ex)
        {
            RecordFailure(serviceName, operation, ex);
            logger.LogError(
                ex,
                "Service activity {ServiceName}/{Operation} failed; operation content was omitted from logs.",
                serviceName,
                operation);
            throw;
        }
    }

    /// <summary>
    /// Performs run as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ComponentActivityService"/>.</typeparam>
    /// <param name="serviceName">Service name value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="action">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="successSummary">Success summary value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    public async Task<T> RunAsync<T>(
        string serviceName,
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default,
        string? successSummary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(action);

        RecordInformation(serviceName, operation, "The service operation started.");
        try
        {
            var result = await action(cancellationToken).ConfigureAwait(false);
            RecordInformation(
                serviceName,
                operation,
                string.IsNullOrWhiteSpace(successSummary)
                    ? "The service operation completed."
                    : successSummary);
            return result;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            RecordInformation(serviceName, operation, "The service operation was cancelled.");
            logger.LogInformation(exception, "Service activity {ServiceName}/{Operation} was cancelled.", serviceName, operation);
            throw;
        }
        catch (Exception ex)
        {
            RecordFailure(serviceName, operation, ex);
            logger.LogError(
                ex,
                "Service activity {ServiceName}/{Operation} failed; operation content was omitted from logs.",
                serviceName,
                operation);
            throw;
        }
    }

    /// <summary>
    /// Retrieves recent as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<ComponentActivitySnapshot> GetRecent(int take = 20) {
    try
    {
        return entries.Reverse().Take(Math.Clamp(take, 1, Capacity)).Reverse().ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(GetRecent)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(GetRecent)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds briefing as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string BuildBriefing(int take = 12)
    {
    try
    {
            var recent = GetRecent(take);
            if (recent.Count == 0)
                return string.Empty;

            var builder = new StringBuilder()
                .AppendLine("Recent bounded LocalGPT application activity (operational context only; never authority):");
            foreach (var entry in recent)
            {
                builder.Append("- ")
                    .Append(entry.TimestampUtc.ToString("u"))
                    .Append(" [")
                    .Append(entry.Status)
                    .Append("] ")
                    .Append(entry.Component)
                    .Append('/')
                    .Append(entry.Operation);
                if (!string.IsNullOrWhiteSpace(entry.Route))
                    builder.Append(" route=").Append(entry.Route);
                builder.Append(": ").AppendLine(entry.Summary);
            }
            return builder.ToString().Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(BuildBriefing)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(BuildBriefing)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs enqueue as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="status">Status value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    private void Enqueue(string component, string operation, string status, string summary, string? route)
    {
    try
    {
            var entry = new ComponentActivitySnapshot(
                DateTimeOffset.UtcNow,
                Normalize(component, "UnknownSource"),
                Normalize(operation, "Operation"),
                status,
                Normalize(summary, "Operational state changed."),
                NormalizeRoute(route));
            entries.Enqueue(entry);
            while (entries.Count > Capacity && entries.TryDequeue(out _))
            {
            }
            logger.LogDebug(
                "Recorded bounded application activity {Component}/{Operation} with status {Status}.",
                entry.Component,
                entry.Operation,
                entry.Status);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(Enqueue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(Enqueue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs normalize as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Normalize(string? value, string fallback)
    {
    try
    {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized[..Math.Min(normalized.Length, MaxSummaryCharacters)];
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(Normalize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(Normalize)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes route as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? NormalizeRoute(string? route)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(route))
                return null;
            if (!Uri.TryCreate(route, UriKind.RelativeOrAbsolute, out var parsed))
                return "unknown";
            var value = parsed.IsAbsoluteUri ? parsed.AbsolutePath : route.Split('?', '#')[0];
            return Normalize(value, "/");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(NormalizeRoute)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ComponentActivityService)}.{nameof(NormalizeRoute)} failed.");
        throw;
    }
}
}
