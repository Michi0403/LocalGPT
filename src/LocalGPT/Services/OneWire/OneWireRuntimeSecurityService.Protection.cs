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
    /// Performs protect outgoing as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task ProtectOutgoingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false);
            var peer = file?.TrustedPeers.FirstOrDefault(candidate => IsCurrentTrust(candidate, envelope.TargetPeerId));
            if (file is null || peer is null || IsSecurityBootstrap(envelope.MessageType)) return;

            var sensitive = new OneWireSensitivePayload
            {
                Properties = envelope.Properties,
                InteractionValueJson = envelope.InteractionValueJson,
                InteractionValueContentType = envelope.InteractionValueContentType,
                WorkflowJson = envelope.WorkflowJson
            };
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(sensitive, jsonOptions);
            var key = DerivePeerKey(file, peer, envelope.SourcePeerId, envelope.TargetPeerId);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using (var aes = new AesGcm(key, tag.Length))
                aes.Encrypt(nonce, plaintext, ciphertext, tag, BuildAssociatedData(envelope));

            envelope.Properties = null;
            envelope.InteractionValueJson = null;
            envelope.WorkflowJson = string.Empty;
            envelope.EncryptedPayload = Convert.ToBase64String(ciphertext);
            envelope.EncryptionNonce = Convert.ToBase64String(nonce);
            envelope.AuthenticationTag = Convert.ToBase64String(tag);
            envelope.SecurityMode = OneWireSecurityMode.EncryptedAndSigned;
            envelope.SecurityKeyId = file.KeyId;
            using var signing = ECDsa.Create();
            signing.ImportPkcs8PrivateKey(Convert.FromBase64String(file.SigningPrivateKey), out _);
            envelope.Signature = Convert.ToBase64String(signing.SignData(BuildSignatureBytes(envelope), HashAlgorithmName.SHA256));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            logger.LogError(ex, "Could not protect outgoing LocalGPT 1-Wire message {MessageId} for {PeerId}.", envelope.MessageId, envelope.TargetPeerId);
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Performs unprotect incoming as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task UnprotectIncomingAsync(OneWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.SecurityMode == OneWireSecurityMode.None) return;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false)
                ?? throw new CryptographicException("A secured 1-Wire message arrived before the local runtime secret was created.");
            var peer = file.TrustedPeers.FirstOrDefault(candidate => IsCurrentTrust(candidate, envelope.SourcePeerId))
                ?? throw new CryptographicException("The secured 1-Wire message came from an untrusted or expired peer.");
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String(peer.SigningPublicKey), out _);
            if (string.IsNullOrWhiteSpace(envelope.Signature) || !verifier.VerifyData(BuildSignatureBytes(envelope), Convert.FromBase64String(envelope.Signature), HashAlgorithmName.SHA256))
                throw new CryptographicException("The 1-Wire signature is invalid.");
            if (envelope.SecurityMode != OneWireSecurityMode.EncryptedAndSigned) return;

            var ciphertext = Convert.FromBase64String(envelope.EncryptedPayload ?? throw new CryptographicException("EncryptedPayload is missing."));
            var nonce = Convert.FromBase64String(envelope.EncryptionNonce ?? throw new CryptographicException("EncryptionNonce is missing."));
            var tag = Convert.FromBase64String(envelope.AuthenticationTag ?? throw new CryptographicException("AuthenticationTag is missing."));
            var plaintext = new byte[ciphertext.Length];
            var key = DerivePeerKey(file, peer, envelope.SourcePeerId, envelope.TargetPeerId);
            using (var aes = new AesGcm(key, tag.Length))
                aes.Decrypt(nonce, ciphertext, tag, plaintext, BuildAssociatedData(envelope));
            var sensitive = JsonSerializer.Deserialize<OneWireSensitivePayload>(plaintext, jsonOptions)
                ?? throw new JsonException("The decrypted 1-Wire payload is empty.");
            envelope.Properties = sensitive.Properties;
            envelope.InteractionValueJson = sensitive.InteractionValueJson;
            envelope.InteractionValueContentType = sensitive.InteractionValueContentType;
            envelope.WorkflowJson = sensitive.WorkflowJson;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            logger.LogError(ex, "Could not verify or decrypt incoming LocalGPT 1-Wire message {MessageId} from {PeerId}.", envelope.MessageId, envelope.SourcePeerId);
            throw;
        }
        finally { gate.Release(); }
    }

}
