using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Provides one wire listen address resolver operations.
/// </summary>
public sealed class OneWireListenAddressResolver(
    ILogger<OneWireListenAddressResolver> logger) : IOneWireListenAddressResolver
{
    /// <summary>
    /// Runs the resolve operation.
    /// </summary>
    public IPAddress Resolve(OneWireOptions configured)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(configured);
            if (!configured.EnableLanTransport)
                return IPAddress.Loopback;
            if (IPAddress.TryParse(configured.ListenAddress, out var address))
                return address;
            logger.LogWarning($"Invalid configured 1-Wire listen address '{configured.ListenAddress}'; falling back to loopback.");
            return IPAddress.Loopback;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not resolve the configured 1-Wire listen address.");
            throw;
        }
    }
}
