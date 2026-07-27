using System.Diagnostics;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LocalGPT.Diagnostics;

/// <summary>
/// Applies structured diagnostics to every MVC controller action without requiring
/// each controller to repeat request lifecycle logging. Client disconnects remain
/// cancellations; unexpected failures are logged and recorded in bounded activity.
/// </summary>
public sealed class ControllerRequestLoggingFilter(
    ILogger<ControllerRequestLoggingFilter> logger,
    IComponentActivityService componentActivity) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var stopwatch = Stopwatch.StartNew();
        var routeValues = context.ActionDescriptor.RouteValues;
        var controller = GetRouteValue(routeValues, "controller", "unknown");
        var action = GetRouteValue(routeValues, "action", "unknown");
        var operation = $"{controller}.{action}";
        var request = context.HttpContext.Request;
        var routePath = request.Path.Value ?? "/";

        logger.LogInformation(
            "Controller action {Controller}.{Action} started for {Method} {Path}.",
            controller,
            action,
            request.Method,
            routePath);
        componentActivity.RecordInformation(
            nameof(ControllerRequestLoggingFilter),
            operation,
            "The controller request started.",
            routePath);

        try
        {
            var executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                logger.LogError(
                    executed.Exception,
                    "Controller action {Controller}.{Action} failed after {ElapsedMilliseconds} ms.",
                    controller,
                    action,
                    stopwatch.ElapsedMilliseconds);
                componentActivity.RecordFailure(
                    nameof(ControllerRequestLoggingFilter),
                    operation,
                    executed.Exception,
                    routePath);
                return;
            }

            logger.LogInformation(
                "Controller action {Controller}.{Action} completed with status {StatusCode} after {ElapsedMilliseconds} ms.",
                controller,
                action,
                context.HttpContext.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            componentActivity.RecordInformation(
                nameof(ControllerRequestLoggingFilter),
                operation,
                "The controller request completed.",
                routePath);
        }
        catch (OperationCanceledException exception) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                exception,
                "Controller action {Controller}.{Action} was cancelled because the client disconnected.",
                controller,
                action);
            componentActivity.RecordWarning(
                nameof(ControllerRequestLoggingFilter),
                operation,
                "The controller request was cancelled because the client disconnected.",
                routePath);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Controller action {Controller}.{Action} threw after {ElapsedMilliseconds} ms.",
                controller,
                action,
                stopwatch.ElapsedMilliseconds);
            componentActivity.RecordFailure(
                nameof(ControllerRequestLoggingFilter),
                operation,
                exception,
                routePath);
            throw;
        }
    }

    private static string GetRouteValue(
        IDictionary<string, string?> routeValues,
        string key,
        string fallback)
    {
        return routeValues.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}
