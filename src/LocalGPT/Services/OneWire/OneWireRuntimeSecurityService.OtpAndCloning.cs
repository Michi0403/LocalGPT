using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Coordinates one wire runtime security behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class OneWireRuntimeSecurityService
{
    /// <summary>
    /// Verifies totp as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="seedBase64">Seed base64 value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="code">Code value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool VerifyTotp(string seedBase64, string code)
    {
    try
    {
            var normalized = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
            if (normalized.Length != 6) return false;
            var seed = Convert.FromBase64String(seedBase64);
            var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpPeriodSeconds;
            for (var offset = -1; offset <= 1; offset++)
            {
                var bytes = BitConverter.GetBytes(counter + offset);
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                using var hmac = new HMACSHA1(seed);
                var hash = hmac.ComputeHash(bytes);
                var index = hash[^1] & 0x0F;
                var binary = ((hash[index] & 0x7F) << 24) | (hash[index + 1] << 16) | (hash[index + 2] << 8) | hash[index + 3];
                var expected = (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
                if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(normalized))) return true;
            }
            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(VerifyTotp)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(VerifyTotp)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs base32 encode as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="data">Data value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Base32Encode(ReadOnlySpan<byte> data)
    {
    try
    {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var output = new StringBuilder((data.Length * 8 + 4) / 5);
            var buffer = 0;
            var bits = 0;
            foreach (var value in data)
            {
                buffer = (buffer << 8) | value;
                bits += 8;
                while (bits >= 5)
                {
                    output.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) output.Append(alphabet[(buffer << (5 - bits)) & 31]);
            return output.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(Base32Encode)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(Base32Encode)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether current trust as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peer">Peer value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsCurrentTrust(OneWireTrustedPeerDescriptor peer, string peerId) {
    try
    {
        return string.Equals(peer.PeerId, peerId, StringComparison.OrdinalIgnoreCase) &&
        peer.TrustLevel >= OneWireTrustLevel.MfaVerified &&
        (peer.ValidUntilUtc is null || peer.ValidUntilUtc > DateTimeOffset.UtcNow) &&
        (peer.MfaVerifiedUntilUtc is null || peer.MfaVerifiedUntilUtc > DateTimeOffset.UtcNow);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(IsCurrentTrust)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(IsCurrentTrust)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether security bootstrap as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="type">Type value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsSecurityBootstrap(OneWireMessageType type) {
    try
    {
        return type is
        OneWireMessageType.Hello or OneWireMessageType.HelloAck or OneWireMessageType.LinkRequest or OneWireMessageType.LinkStatus or
        OneWireMessageType.SecurityProfileRequest or OneWireMessageType.SecurityProfileResponse or OneWireMessageType.MfaChallenge or
        OneWireMessageType.MfaProof or OneWireMessageType.TrustEstablished or OneWireMessageType.TrustRevoked;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(IsSecurityBootstrap)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(IsSecurityBootstrap)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clamp as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="minimum">Minimum value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The time span produced by the operation.</returns>
    private TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum) {
    try
    {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(Clamp)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(Clamp)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clone trusted peer as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peer">Peer value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The one wire trusted peer descriptor produced by the operation.</returns>
    private OneWireTrustedPeerDescriptor CloneTrustedPeer(OneWireTrustedPeerDescriptor peer) {
    try
    {
        return new()
    {
        PeerId = peer.PeerId, DisplayName = peer.DisplayName, Fingerprint = peer.Fingerprint,
        KeyAgreementPublicKey = peer.KeyAgreementPublicKey, SigningPublicKey = peer.SigningPublicKey,
        TrustLevel = peer.TrustLevel, TrustedUtc = peer.TrustedUtc, ValidUntilUtc = peer.ValidUntilUtc,
        MfaVerifiedUntilUtc = peer.MfaVerifiedUntilUtc
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CloneTrustedPeer)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CloneTrustedPeer)} failed.");
        throw;
    }
}


}
