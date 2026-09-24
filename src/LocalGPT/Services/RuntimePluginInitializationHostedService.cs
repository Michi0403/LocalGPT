using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Primes persisted runtime extension descriptors after host startup without loading executable code automatically.</summary>
/// <param name="plugins">Runtime plugin service dependency.</param>
/// <param name="logger">Logger used for startup diagnostics.</param>
public sealed class RuntimePluginInitializationHostedService(IRuntimePluginService plugins, ILogger<RuntimePluginInitializationHostedService> logger) : IHostedService
{
    /// <summary>Refreshes persisted descriptors at startup.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await plugins.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Runtime extension descriptor initialization failed; executable plugin code was not loaded.");
        }
    }

    /// <summary>No stop work is required; collectible plugin contexts are process-owned.</summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Runtime extension descriptor initialization hosted service stopped.");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Stopping runtime extension descriptor initialization failed.");
            throw;
        }
    }
}
