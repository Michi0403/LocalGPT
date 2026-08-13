using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Creates configured one wire dispatch context instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireDispatchContextFactory(
    ILogger<OneWireDispatchContextFactory> logger) : IOneWireDispatchContextFactory
{
    /// <summary>
    /// Creates internal using the configuration and dependencies owned by <see cref="OneWireDispatchContextFactory"/>.
    /// </summary>
    /// <param name="transport">Transport value supplied to the one wire dispatch context operation and used when producing its result.</param>
    /// <returns>The one wire dispatch context produced by the operation.</returns>
    public OneWireDispatchContext CreateInternal(string transport = "internal")
    {
        try
        {
            var context = new OneWireDispatchContext
            {
                AuthenticatedPeerId = "localgpt",
                ConnectionId = Guid.Empty,
                IsInternal = true,
                IsLoopback = true,
                Transport = transport ?? string.Empty
            };
            logger.LogTrace($"Created internal 1-Wire dispatch context for transport {context.Transport}.");
            return context;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not create an internal 1-Wire dispatch context for transport {transport}.");
            throw;
        }
    }

    /// <summary>
    /// Creates external using the configuration and dependencies owned by <see cref="OneWireDispatchContextFactory"/>.
    /// </summary>
    /// <param name="authenticatedPeerId">Identifier of the authenticated peer to use for this operation.</param>
    /// <param name="connectionId">Identifier of the connection to use for this operation.</param>
    /// <param name="isLoopback">Value indicating whether is loopback should apply to this operation.</param>
    /// <param name="transport">Transport value supplied to the one wire dispatch context operation and used when producing its result.</param>
    /// <returns>The one wire dispatch context produced by the operation.</returns>
    public OneWireDispatchContext CreateExternal(string authenticatedPeerId, Guid connectionId, bool isLoopback, string transport)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedPeerId);
            if (connectionId == Guid.Empty)
                throw new ArgumentException("A connection id is required.", nameof(connectionId));
            var context = new OneWireDispatchContext
            {
                AuthenticatedPeerId = authenticatedPeerId,
                ConnectionId = connectionId,
                IsInternal = false,
                IsLoopback = isLoopback,
                Transport = transport ?? string.Empty
            };
            logger.LogTrace($"Created external 1-Wire dispatch context for peer {authenticatedPeerId} on {transport}.");
            return context;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not create an external 1-Wire dispatch context for peer {authenticatedPeerId} on {transport}.");
            throw;
        }
    }
}
