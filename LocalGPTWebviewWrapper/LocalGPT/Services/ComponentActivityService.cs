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
public sealed class ComponentActivityService(ILogger<ComponentActivityService> logger) :
    IComponentActivityService,
    IServiceActivityService
{
    private const int Capacity = 192;
    private const int MaxSummaryCharacters = 320;
    private readonly ConcurrentQueue<ComponentActivitySnapshot> entries = new();

    public void RecordNavigation(string route) =>
        RecordInformation("Router", "Navigation", "The user opened a LocalGPT route.", NormalizeRoute(route));

    public void RecordInformation(string component, string operation, string summary, string? route = null) =>
        Enqueue(component, operation, "Information", summary, route);

    public void RecordWarning(string component, string operation, string summary, string? route = null) =>
        Enqueue(component, operation, "Warning", summary, route);

    public void RecordFailure(string component, string operation, Exception exception, string? route = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Enqueue(
            component,
            operation,
            "Error",
            $"{exception.GetType().Name}: the operation failed; sensitive details remain only in application logs.",
            route);
    }

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

    public IReadOnlyList<ComponentActivitySnapshot> GetRecent(int take = 20) =>
        entries.Reverse().Take(Math.Clamp(take, 1, Capacity)).Reverse().ToList();

    public string BuildBriefing(int take = 12)
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

    private void Enqueue(string component, string operation, string status, string summary, string? route)
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

    private string Normalize(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized[..Math.Min(normalized.Length, MaxSummaryCharacters)];
    }

    private string? NormalizeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return null;
        if (!Uri.TryCreate(route, UriKind.RelativeOrAbsolute, out var parsed))
            return "unknown";
        var value = parsed.IsAbsoluteUri ? parsed.AbsolutePath : route.Split('?', '#')[0];
        return Normalize(value, "/");
    }
}
