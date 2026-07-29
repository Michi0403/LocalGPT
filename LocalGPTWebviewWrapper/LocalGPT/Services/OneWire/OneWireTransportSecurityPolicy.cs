using LocalGPT.BusinessObjects;
using System.Net;

namespace LocalGPT.Services.OneWire;

internal static class OneWireTransportSecurityPolicy
{
    public static bool RequiresProtectedTransport(OneWireMessageType messageType) => messageType is not (
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

    public static bool IsProtected(OneWireEnvelope envelope) =>
        envelope.SecurityMode is OneWireSecurityMode.Signed or OneWireSecurityMode.EncryptedAndSigned;

    public static bool IsLoopback(IPAddress? address) => address is not null && IPAddress.IsLoopback(address);
}
