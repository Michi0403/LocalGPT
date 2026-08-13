using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Represents an one wire transport security policy application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireTransportSecurityPolicy(ILogger<OneWireTransportSecurityPolicy> logger) : IOneWireTransportSecurityPolicy
{
    /// <summary>
    /// Performs requires protected transport for <see cref="OneWireTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport security policy workflow.
    /// </summary>
    /// <param name="messageType">Message type value supplied to the one wire transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool RequiresProtectedTransport(OneWireMessageType messageType)
    {
        try
        {
            var requiresProtection = messageType is not (
                OneWireMessageType.Hello or
                OneWireMessageType.HelloAck or
                OneWireMessageType.LinkRequest or
                OneWireMessageType.LinkStatus or
                OneWireMessageType.SecurityProfileRequest or
                OneWireMessageType.SecurityProfileResponse or
                OneWireMessageType.MfaChallenge or
                OneWireMessageType.MfaProof or
                OneWireMessageType.TrustEstablished or
                OneWireMessageType.TrustRevoked or
                OneWireMessageType.Ping or
                OneWireMessageType.Pong or
                OneWireMessageType.Error);
            logger.LogTrace($"Resolved protected-transport requirement for 1-Wire message type {messageType}: {requiresProtection}.");
            return requiresProtection;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve the protected-transport requirement for 1-Wire message type {messageType}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether protected for <see cref="OneWireTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport security policy workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire transport security policy operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsProtected(OneWireEnvelope envelope)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(envelope);
            var isProtected = envelope.SecurityMode is OneWireSecurityMode.Signed or OneWireSecurityMode.EncryptedAndSigned;
            logger.LogTrace($"Resolved 1-Wire envelope protection state for message {envelope.MessageId}: {isProtected}.");
            return isProtected;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve the 1-Wire envelope protection state: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether loopback for <see cref="OneWireTransportSecurityPolicy"/>, keeping the operation consistent with the state and invariants of the surrounding one wire transport security policy workflow.
    /// </summary>
    /// <param name="address">P address dependency used by the one wire transport security policy workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsLoopback(IPAddress? address)
    {
        try
        {
            var isLoopback = address is not null && IPAddress.IsLoopback(address);
            logger.LogTrace($"Resolved loopback state for address {address?.ToString() ?? "<none>"}: {isLoopback}.");
            return isLoopback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve loopback state for address {address?.ToString() ?? "<none>"}: {exception.Message}");
            throw;
        }
    }
}
