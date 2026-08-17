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
    /// Validates pairing ticket as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ticket">Ticket value supplied to the one wire runtime security operation and used when producing its result.</param>
    private void ValidatePairingTicket(OneWirePairingTicket ticket)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(ticket);
            if (!string.Equals(ticket.Scheme, "onewire-pair-v1", StringComparison.Ordinal) || ticket.ExpiresUtc <= DateTimeOffset.UtcNow ||
                string.IsNullOrWhiteSpace(ticket.PeerId) || string.IsNullOrWhiteSpace(ticket.KeyAgreementPublicKey) ||
                string.IsNullOrWhiteSpace(ticket.SigningPublicKey) || string.IsNullOrWhiteSpace(ticket.Signature))
                throw new InvalidDataException("The 1-Wire pairing ticket is incomplete or expired.");
            var agreement = Convert.FromBase64String(ticket.KeyAgreementPublicKey);
            var signing = Convert.FromBase64String(ticket.SigningPublicKey);
            var expectedFingerprint = Convert.ToHexString(SHA256.HashData([.. agreement, .. signing]));
            if (!string.Equals(expectedFingerprint, ticket.Fingerprint, StringComparison.OrdinalIgnoreCase))
                throw new CryptographicException("The pairing-ticket fingerprint does not match its public keys.");
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(signing, out _);
            if (!verifier.VerifyData(BuildTicketBytes(ticket), Convert.FromBase64String(ticket.Signature), HashAlgorithmName.SHA256))
                throw new CryptographicException("The pairing ticket signature is invalid.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(ValidatePairingTicket)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(ValidatePairingTicket)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds ticket bytes as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="ticket">Ticket value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] BuildTicketBytes(OneWirePairingTicket ticket) {
    try
    {
        return JsonSerializer.SerializeToUtf8Bytes(new
    {
        ticket.Scheme, ticket.PeerId, ticket.DisplayName, ticket.Application, ticket.ProtocolVersion, ticket.KeyId,
        ticket.Fingerprint, ticket.KeyAgreementPublicKey, ticket.SigningPublicKey, ticket.CreatedUtc, ticket.ExpiresUtc, ticket.Nonce
    });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(BuildTicketBytes)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(BuildTicketBytes)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds signature bytes as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] BuildSignatureBytes(OneWireEnvelope envelope) {
    try
    {
        return JsonSerializer.SerializeToUtf8Bytes(new
    {
        envelope.ProtocolVersion, envelope.MessageId, envelope.CorrelationId, envelope.ReplyToMessageId, envelope.MessageType,
        envelope.SourcePeerId, envelope.TargetPeerId, envelope.CreatedUtc, envelope.ExpiresUtc, envelope.Sequence,
        envelope.ExecutionMode, envelope.Controller, envelope.Method, envelope.Route, envelope.CapabilityKey,
        Organs = envelope.Organs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        Skills = envelope.Skills.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        envelope.EncryptedPayload, envelope.SecurityMode, envelope.SecurityKeyId, envelope.EncryptionNonce,
        envelope.AuthenticationTag, envelope.UserConfirmed, envelope.ApprovalMode, envelope.WorkOrderKey, envelope.NotBeforeUtc,
        envelope.RequiresHumanInteractionOnTargetSystem, envelope.RequiresAutomatedInteractionOnTargetSystem, envelope.InteractionKind
    });
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(BuildSignatureBytes)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(BuildSignatureBytes)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds associated data as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] BuildAssociatedData(OneWireEnvelope envelope) {
    try
    {
        return Encoding.UTF8.GetBytes(
        $"{envelope.ProtocolVersion}|{envelope.MessageId:N}|{envelope.CorrelationId:N}|{envelope.SourcePeerId}|{envelope.TargetPeerId}|{envelope.MessageType}|{envelope.CapabilityKey}");
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(BuildAssociatedData)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(BuildAssociatedData)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs derive peer key as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="peer">Peer value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="sourcePeerId">Identifier of the source peer to use for this operation.</param>
    /// <param name="targetPeerId">Identifier of the target peer to use for this operation.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] DerivePeerKey(OneWireRuntimeSecretFile file, OneWireTrustedPeerDescriptor peer, string sourcePeerId, string targetPeerId)
    {
    try
    {
            using var local = ECDiffieHellman.Create();
            local.ImportPkcs8PrivateKey(Convert.FromBase64String(file.KeyAgreementPrivateKey), out _);
            using var remote = ECDiffieHellman.Create();
            remote.ImportSubjectPublicKeyInfo(Convert.FromBase64String(peer.KeyAgreementPublicKey), out _);
            var shared = local.DeriveKeyMaterial(remote.PublicKey);
            var orderedPeers = new[] { sourcePeerId, targetPeerId }
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var orderedFingerprints = new[] { file.Fingerprint, peer.Fingerprint }
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var context = $"LocalGPT-Organic-OneWire-v2.1|{string.Join("|", orderedPeers)}|{string.Join("|", orderedFingerprints)}";
            var salt = SHA256.HashData(Encoding.UTF8.GetBytes("LocalGPT-Organic-OneWire-HKDF-SHA256-v1|" + string.Join("|", orderedFingerprints)));
            return HkdfSha256(shared, salt, Encoding.UTF8.GetBytes(context), 32);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(DerivePeerKey)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(DerivePeerKey)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs hkdf SHA-256 as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="inputKeyMaterial">Input key material value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="salt">Salt value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="info">Info value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="outputLength">Output length value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] HkdfSha256(ReadOnlySpan<byte> inputKeyMaterial, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info, int outputLength)
    {
    try
    {
            if (outputLength <= 0 || outputLength > 255 * 32) throw new ArgumentOutOfRangeException(nameof(outputLength));
            byte[] pseudoRandomKey;
            using (var extract = new HMACSHA256(salt.ToArray()))
                pseudoRandomKey = extract.ComputeHash(inputKeyMaterial.ToArray());

            var output = new byte[outputLength];
            var previous = Array.Empty<byte>();
            var written = 0;
            byte counter = 1;
            using var expand = new HMACSHA256(pseudoRandomKey);
            while (written < outputLength)
            {
                var blockInput = new byte[previous.Length + info.Length + 1];
                previous.CopyTo(blockInput, 0);
                info.CopyTo(blockInput.AsSpan(previous.Length));
                blockInput[^1] = counter++;
                previous = expand.ComputeHash(blockInput);
                var count = Math.Min(previous.Length, outputLength - written);
                previous.AsSpan(0, count).CopyTo(output.AsSpan(written));
                written += count;
            }
            CryptographicOperations.ZeroMemory(pseudoRandomKey);
            CryptographicOperations.ZeroMemory(previous);
            return output;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(HkdfSha256)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(HkdfSha256)} failed.");
        throw;
    }
}

}
