using LocalGPT.Services.Council;
using LocalGPT.Services.OneWire;
using LocalGPT.Services.Persistence;
using Microsoft.Extensions.Hosting;

namespace LocalGPT.Services;

/// <summary>
/// Starts LocalGPT's non-HTTP background workers only after the ASP.NET Core application has
/// published <see cref="IHostApplicationLifetime.ApplicationStarted"/>. This keeps database
/// initialization, catalog synchronization, remote-control polling, and 1-Wire work outside the
/// Kestrel listener's startup critical path while preserving their existing implementations.
/// </summary>
/// <param name="services">Root service provider used only after the listener has started.</param>
/// <param name="applicationLifetime">Application lifetime signals used to establish the post-listen boundary.</param>
/// <param name="logger">Logger used for startup and shutdown diagnostics.</param>
public sealed class LocalGptPostListenHostedServiceCoordinator(
    IServiceProvider services,
    IHostApplicationLifetime applicationLifetime,
    ILogger<LocalGptPostListenHostedServiceCoordinator> logger) : BackgroundService
{
    private readonly List<IHostedService> startedWorkers = [];

    /// <summary>
    /// Waits until Kestrel has completed host startup, then starts application background workers.
    /// None of the worker dependency graphs are resolved before <c>ApplicationStarted</c>.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token signaled when LocalGPT is stopping.</param>
    /// <returns>A task representing the coordinator lifetime.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!applicationLifetime.ApplicationStarted.IsCancellationRequested)
            {
                using var startupBoundary = CancellationTokenSource.CreateLinkedTokenSource(
                    applicationLifetime.ApplicationStarted,
                    stoppingToken);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, startupBoundary.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (applicationLifetime.ApplicationStarted.IsCancellationRequested)
                {
                    // ApplicationStarted is represented by a cancellation token. Reaching this branch
                    // is the successful post-listen hand-off, not an application cancellation.
                }
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            logger.LogInformation("LocalGPT web listener is online; starting post-listen application workers.");

            IHostedService[] workers =
            [
                services.GetRequiredService<DatabaseInitializationHostedService>(),
                services.GetRequiredService<RemoteControlPollingHostedService>(),
                services.GetRequiredService<RuntimeCapabilityDirectoryHostedService>(),
                services.GetRequiredService<DxAiFunctionCatalogHostedService>(),
                services.GetRequiredService<OneWireTcpHostedService>(),
                services.GetRequiredService<OneWireDiscoveryHostedService>(),
                services.GetRequiredService<OneWireCouncilApprovalProcessorHostedService>(),
                services.GetRequiredService<OneWireWorkProcessorHostedService>()
            ];

            foreach (var worker in workers)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await worker.StartAsync(stoppingToken).ConfigureAwait(false);
                startedWorkers.Add(worker);
                logger.LogInformation("Started post-listen worker {WorkerType}.", worker.GetType().Name);
            }

            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("LocalGPT post-listen worker coordinator stopped with application cancellation.");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "LocalGPT post-listen worker startup failed after the web listener was already online.");
        }
        finally
        {
            using var stopBudget = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            for (var index = startedWorkers.Count - 1; index >= 0; index--)
            {
                var worker = startedWorkers[index];
                try
                {
                    await worker.StopAsync(stopBudget.Token).ConfigureAwait(false);
                    logger.LogDebug("Stopped post-listen worker {WorkerType}.", worker.GetType().Name);
                }
                catch (OperationCanceledException exception) when (stopBudget.IsCancellationRequested)
                {
                    logger.LogWarning(exception, "Timed out while stopping post-listen worker {WorkerType}.", worker.GetType().Name);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Post-listen worker {WorkerType} failed during shutdown.", worker.GetType().Name);
                }
            }

            startedWorkers.Clear();
        }
    }
}
