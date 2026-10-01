using LocalGPT.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LocalGPT.Services;

/// <summary>
/// Resolves DXAI function handlers lazily from the current request or Blazor circuit scope.
/// </summary>
/// <param name="serviceProvider">The active scoped service provider supplied by Microsoft dependency injection.</param>
/// <param name="logger">Logger used to record scoped handler-resolution diagnostics.</param>
public sealed class DxAiFunctionHandlerResolver(
    IServiceProvider serviceProvider,
    ILogger<DxAiFunctionHandlerResolver> logger)
{
    /// <summary>
    /// Resolves the handler collection from the same active scope that owns the central DXAI registry.
    /// </summary>
    /// <returns>The handlers registered in the current scope.</returns>
    public IEnumerable<IDxAiFunctionHandler> Resolve()
    {
        try
        {
            var handlers = serviceProvider.GetServices<IDxAiFunctionHandler>();
            logger.LogDebug("Resolved the scoped DXAIFunction handler collection lazily from the active DI scope.");
            return handlers;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the scoped DXAIFunction handler collection failed.");
            throw;
        }
    }
}
