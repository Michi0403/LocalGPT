using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Polls only user-enabled Remote Control connectors whose explicitly configured intervals are due.</summary>
/// <param name="scopeFactory">Scope factory used to resolve scoped connector services for each polling pass.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
/// <param name="runtimePolicy">Database-backed runtime-policy service that supplies configurable operational parameters for this component.</param>
public sealed class RemoteControlPollingHostedService(
    IServiceScopeFactory scopeFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<RemoteControlPollingHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Stores the internal scan interval state used by <see cref="RemoteControlPollingHostedService"/> while executing its surrounding workflow.
    /// </summary>
    private TimeSpan ScanInterval => TimeSpan.FromSeconds(runtimePolicy.GetJson<ServiceTimingRuntimeParameters>(LocalGptRuntimeValue.ServiceTimingRuntimeParametersJson).RemoteControlPollingSeconds);

    /// <summary>
    /// Performs execute as part of the remote control polling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(ScanInterval);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunPassAsync(stoppingToken).ConfigureAwait(false);
                if (!await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) break;
            }
        }
        catch (OperationCanceledException exception) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "Remote Control polling hosted service stopped with application cancellation.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control polling hosted service terminated unexpectedly.");
            throw;
        }
    }

    /// <summary>
    /// Performs run pass as part of the remote control polling service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunPassAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var connectors = scope.ServiceProvider.GetRequiredService<IRemoteControlConnectorService>();
            var due = await connectors.ListDueForPollingAsync(DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
            foreach (var connector in due)
            {
                try
                {
                    await connectors.PullAsync(connector.Key, runPipelines: true, automaticInvocation: true, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Automatic Remote Control poll failed for connector {ConnectorKey}; endpoint and payload were omitted.", connector.Key);
                }
            }
            if (due.Count > 0) logger.LogDebug("Completed Remote Control polling pass for {ConnectorCount} due connector(s).", due.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control polling pass failed.");
        }
    }
}
