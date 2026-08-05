using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.OneWire;

public sealed class OneWireDispatchContextFactory(
    ILogger<OneWireDispatchContextFactory> logger) : IOneWireDispatchContextFactory
{
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
