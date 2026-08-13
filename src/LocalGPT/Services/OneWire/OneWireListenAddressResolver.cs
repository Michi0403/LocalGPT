using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Resolves one wire listen address choices from the available runtime state and returns the application-appropriate result to callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireListenAddressResolver(
    ILogger<OneWireListenAddressResolver> logger) : IOneWireListenAddressResolver
{
    /// <summary>
    /// Performs resolve for <see cref="OneWireListenAddressResolver"/>, keeping the operation consistent with the state and invariants of the surrounding one wire listen address workflow.
    /// </summary>
    /// <param name="configured">Configured value supplied to the one wire listen address operation and used when producing its result.</param>
    /// <returns>The IP address produced by the operation.</returns>
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
