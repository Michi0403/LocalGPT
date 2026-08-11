using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services.OneWire;

/// <summary>
/// Owns the runtime-generated LocalGPT 1-Wire identity. Private keys and the MFA seed are created only at runtime,
/// are never compiled into the application, and can be discarded from the frontend to reset every trusted link.
/// </summary>
public sealed class OneWireRuntimeSecurityService(
    ILogger<OneWireRuntimeSecurityService> logger) : IOneWireRuntimeSecurityService
{
    private const int SchemaVersion = 1;
    private const int TotpPeriodSeconds = 30;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    private OneWireRuntimeSecretFile? cached;
    private string? resolvedPath;

    /// <summary>
    /// Gets status async.
    /// </summary>
    public async Task<OneWireRuntimeSecurityStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false);
            return file is null
                ? new OneWireRuntimeSecurityStatus
                {
                    HasSecret = false,
                    SecretPath = ResolveSecretPath(),
                    Warning = "No runtime 1-Wire secret exists. Connections remain discoverable, but encrypted trusted transport is unavailable until the user creates one."
                }
                : CreateStatus(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            logger.LogError(ex, "Could not read the LocalGPT runtime 1-Wire security status.");
            return new OneWireRuntimeSecurityStatus { HasSecret = false, SecretPath = ResolveSecretPath(), Warning = ex.Message };
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Ensures created async.
    /// </summary>
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _ = await LoadCoreAsync(createWhenMissing: true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            logger.LogError(ex, "Could not create the LocalGPT runtime 1-Wire secret.");
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Runs the regenerate async operation.
    /// </summary>
    public async Task RegenerateAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var previous = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false);
            var replacement = CreateSecret(previous?.CreatedUtc ?? DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            await PersistCoreAsync(replacement, cancellationToken).ConfigureAwait(false);
            cached = replacement;
            logger.LogWarning("Regenerated the LocalGPT runtime 1-Wire secret. All previously trusted peers are intentionally invalidated.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            logger.LogError(ex, "Could not regenerate the LocalGPT runtime 1-Wire secret.");
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Deletes async.
    /// </summary>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = ResolveSecretPath();
            if (File.Exists(path)) File.Delete(path);
            cached = null;
            logger.LogWarning("Deleted the LocalGPT runtime 1-Wire secret. The local identity and every trusted peer binding were reset.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not delete the LocalGPT runtime 1-Wire secret.");
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Gets public descriptor async.
    /// </summary>
    public async Task<OneWireSecurityDescriptor> GetPublicDescriptorAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false);
            return file is null ? new OneWireSecurityDescriptor() : CreatePublicDescriptor(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            logger.LogWarning(ex, "Could not expose the LocalGPT public 1-Wire security descriptor.");
            return new OneWireSecurityDescriptor();
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Creates pairing ticket async.
    /// </summary>
    public async Task<OneWirePairingTicket> CreatePairingTicketAsync(TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: true, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The runtime security file could not be created.");
            var descriptor = CreatePublicDescriptor(file);
            var ticket = new OneWirePairingTicket
            {
                PeerId = "localgpt",
                DisplayName = "LocalGPT",
                Application = "LocalGPT",
                ProtocolVersion = OneWireProtocol.Version,
                KeyId = descriptor.KeyId,
                Fingerprint = descriptor.Fingerprint,
                KeyAgreementPublicKey = descriptor.KeyAgreementPublicKey,
                SigningPublicKey = descriptor.SigningPublicKey,
                CreatedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(Clamp(lifetime, TimeSpan.FromMinutes(2), TimeSpan.FromDays(1))),
                Nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            };
            using var signing = ECDsa.Create();
            signing.ImportPkcs8PrivateKey(Convert.FromBase64String(file.SigningPrivateKey), out _);
            ticket.Signature = Convert.ToBase64String(signing.SignData(BuildTicketBytes(ticket), HashAlgorithmName.SHA256));
            logger.LogInformation("Created a LocalGPT 1-Wire public pairing ticket valid until {ExpiresUtc}.", ticket.ExpiresUtc);
            return ticket;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            logger.LogError(ex, "Could not create a LocalGPT 1-Wire pairing ticket.");
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Gets otp auth URI async.
    /// </summary>
    public async Task<string> GetOtpAuthUriAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: true, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The runtime security file could not be created.");
            var label = Uri.EscapeDataString($"LocalGPT:{Environment.MachineName}");
            var issuer = Uri.EscapeDataString("LocalGPT 1-Wire");
            return $"otpauth://totp/{label}?secret={Base32Encode(Convert.FromBase64String(file.MfaSeed))}&issuer={issuer}&algorithm=SHA1&digits=6&period={TotpPeriodSeconds}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException)
        {
            logger.LogError(ex, "Could not create the LocalGPT 1-Wire Authenticator URI.");
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Runs the establish trust async operation.
    /// </summary>
    public async Task<bool> EstablishTrustAsync(OneWireTrustEstablishmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: true, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The runtime security file could not be created.");
            ValidatePairingTicket(request.Ticket);
            if (!VerifyTotp(file.MfaSeed, request.MfaCode))
            {
                logger.LogWarning("Rejected a LocalGPT 1-Wire trust request for peer {PeerId}: the local MFA code was invalid.", request.Ticket.PeerId);
                return false;
            }

            var validity = TimeSpan.FromMinutes(Math.Clamp(request.ValidForMinutes, 5, 525600));
            var trusted = new OneWireTrustedPeerDescriptor
            {
                PeerId = request.Ticket.PeerId,
                DisplayName = request.Ticket.DisplayName,
                Fingerprint = request.Ticket.Fingerprint,
                KeyAgreementPublicKey = request.Ticket.KeyAgreementPublicKey,
                SigningPublicKey = request.Ticket.SigningPublicKey,
                TrustLevel = OneWireTrustLevel.MfaVerified,
                TrustedUtc = DateTimeOffset.UtcNow,
                ValidUntilUtc = DateTimeOffset.UtcNow.Add(validity),
                MfaVerifiedUntilUtc = DateTimeOffset.UtcNow.Add(validity)
            };
            file.TrustedPeers.RemoveAll(peer => string.Equals(peer.PeerId, trusted.PeerId, StringComparison.OrdinalIgnoreCase));
            file.TrustedPeers.Add(trusted);
            await PersistCoreAsync(file, cancellationToken).ConfigureAwait(false);
            cached = file;
            logger.LogInformation("Established MFA-verified LocalGPT 1-Wire trust for peer {PeerId} until {ValidUntilUtc}.", trusted.PeerId, trusted.ValidUntilUtc);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException or InvalidDataException)
        {
            logger.LogError(ex, "Could not establish LocalGPT 1-Wire trust for peer {PeerId}.", request.Ticket?.PeerId);
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Runs the revoke trust async operation.
    /// </summary>
    public async Task<bool> RevokeTrustAsync(string peerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false);
            if (file is null) return false;
            var removed = file.TrustedPeers.RemoveAll(peer => string.Equals(peer.PeerId, peerId, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                await PersistCoreAsync(file, cancellationToken).ConfigureAwait(false);
                cached = file;
                logger.LogWarning("Revoked LocalGPT 1-Wire trust for peer {PeerId}.", peerId);
            }
            return removed;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            logger.LogError(ex, "Could not revoke LocalGPT 1-Wire trust for peer {PeerId}.", peerId);
            throw;
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Gets trusted peers async.
    /// </summary>
    public async Task<IReadOnlyList<OneWireTrustedPeerDescriptor>> GetTrustedPeersAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = await LoadCoreAsync(createWhenMissing: false, cancellationToken).ConfigureAwait(false);
            return file?.TrustedPeers.Select(CloneTrustedPeer).OrderBy(peer => peer.DisplayName).ToList() ?? [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException)
        {
            logger.LogWarning(ex, "Could not read LocalGPT trusted 1-Wire peers.");
            return [];
        }
        finally { gate.Release(); }
    }

    /// <summary>
    /// Runs the protect outgoing async operation.
    /// </summary>
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
    /// Runs the unprotect incoming async operation.
    /// </summary>
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

    /// <summary>
    /// Loads core async.
    /// </summary>
    private async Task<OneWireRuntimeSecretFile?> LoadCoreAsync(bool createWhenMissing, CancellationToken cancellationToken)
    {
    try
    {
            if (cached is not null) return cached;
            var path = ResolveSecretPath();
            if (!File.Exists(path))
            {
                if (!createWhenMissing) return null;
                var created = CreateSecret(DateTimeOffset.UtcNow, null);
                await PersistCoreAsync(created, cancellationToken).ConfigureAwait(false);
                cached = created;
                logger.LogInformation("Created the LocalGPT runtime 1-Wire secret at {SecretPath}.", path);
                return created;
            }
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var file = JsonSerializer.Deserialize<OneWireRuntimeSecretFile>(json, jsonOptions)
                ?? throw new JsonException("The LocalGPT 1-Wire secret file is empty.");
            ValidateSecret(file);
            cached = file;
            return file;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(LoadCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(LoadCoreAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the persist core async operation.
    /// </summary>
    private async Task PersistCoreAsync(OneWireRuntimeSecretFile file, CancellationToken cancellationToken)
    {
    try
    {
            ValidateSecret(file);
            var path = ResolveSecretPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(file, jsonOptions), cancellationToken).ConfigureAwait(false);
            TryRestrictSecretPermissions(temporary);
            File.Move(temporary, path, overwrite: true);
            TryRestrictSecretPermissions(path);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(PersistCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(PersistCoreAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to restrict secret permissions.
    /// </summary>
    private void TryRestrictSecretPermissions(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            logger.LogWarning(ex, "Could not restrict 1-Wire secret file permissions at {SecretPath}; private material was not logged.", path);
            // Persistence still succeeds on filesystems that do not support Unix modes; the frontend shows the path
            // so the owner can apply platform-specific ACLs. Never write private material to logs.
        }
    }

    /// <summary>
    /// Resolves secret path.
    /// </summary>
    private string ResolveSecretPath()
    {
    try
    {
            if (!string.IsNullOrWhiteSpace(resolvedPath)) return resolvedPath;
            var preferred = Path.Combine(AppContext.BaseDirectory, "security", "onewire-secret.json");
            if (CanWriteDirectory(Path.GetDirectoryName(preferred)!))
                return resolvedPath = preferred;
            var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "Security", "onewire-secret.json");
            logger.LogWarning("The LocalGPT program directory is not writable; the runtime 1-Wire secret will use {SecretPath}.", fallback);
            return resolvedPath = fallback;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(ResolveSecretPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(ResolveSecretPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether write directory.
    /// </summary>
    private bool CanWriteDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var test = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllBytes(test, []);
            File.Delete(test);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Directory write probe failed for {DirectoryPath}.", directory);
            return false;
        }
    }

    /// <summary>
    /// Creates secret.
    /// </summary>
    private OneWireRuntimeSecretFile CreateSecret(DateTimeOffset createdUtc, DateTimeOffset? rotatedUtc)
    {
    try
    {
            using var agreement = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var signing = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var agreementPublic = agreement.ExportSubjectPublicKeyInfo();
            var signingPublic = signing.ExportSubjectPublicKeyInfo();
            var fingerprint = Convert.ToHexString(SHA256.HashData([.. agreementPublic, .. signingPublic]));
            return new OneWireRuntimeSecretFile
            {
                SchemaVersion = SchemaVersion,
                PeerId = "localgpt",
                CreatedUtc = createdUtc,
                RotatedUtc = rotatedUtc,
                RootSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                KeyAgreementPrivateKey = Convert.ToBase64String(agreement.ExportPkcs8PrivateKey()),
                KeyAgreementPublicKey = Convert.ToBase64String(agreementPublic),
                SigningPrivateKey = Convert.ToBase64String(signing.ExportPkcs8PrivateKey()),
                SigningPublicKey = Convert.ToBase64String(signingPublic),
                MfaSeed = Convert.ToBase64String(RandomNumberGenerator.GetBytes(20)),
                KeyId = fingerprint[..16],
                Fingerprint = fingerprint
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CreateSecret)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CreateSecret)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates secret.
    /// </summary>
    private void ValidateSecret(OneWireRuntimeSecretFile file)
    {
    try
    {
            if (file.SchemaVersion != SchemaVersion || string.IsNullOrWhiteSpace(file.RootSecret) ||
                string.IsNullOrWhiteSpace(file.KeyAgreementPrivateKey) || string.IsNullOrWhiteSpace(file.SigningPrivateKey) ||
                string.IsNullOrWhiteSpace(file.MfaSeed) || string.IsNullOrWhiteSpace(file.Fingerprint))
                throw new CryptographicException("The runtime 1-Wire secret file is incomplete or uses an unsupported schema.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(ValidateSecret)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(ValidateSecret)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates status.
    /// </summary>
    private OneWireRuntimeSecurityStatus CreateStatus(OneWireRuntimeSecretFile file) {
    try
    {
        return new()
    {
        HasSecret = true,
        SecretPath = ResolveSecretPath(),
        KeyId = file.KeyId,
        Fingerprint = file.Fingerprint,
        CreatedUtc = file.CreatedUtc,
        RotatedUtc = file.RotatedUtc,
        TrustedPeerCount = file.TrustedPeers.Count(peer => IsCurrentTrust(peer, peer.PeerId)),
        MfaEnrolled = !string.IsNullOrWhiteSpace(file.MfaSeed)
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CreateStatus)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CreateStatus)} failed.");
        throw;
    }
}

    /// <summary>
    /// Creates public descriptor.
    /// </summary>
    private OneWireSecurityDescriptor CreatePublicDescriptor(OneWireRuntimeSecretFile file) {
    try
    {
        return new()
    {
        HasRuntimeSecret = true,
        KeyId = file.KeyId,
        Fingerprint = file.Fingerprint,
        KeyAgreementPublicKey = file.KeyAgreementPublicKey,
        SigningPublicKey = file.SigningPublicKey
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CreatePublicDescriptor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OneWireRuntimeSecurityService)}.{nameof(CreatePublicDescriptor)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates pairing ticket.
    /// </summary>
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
    /// Builds ticket bytes.
    /// </summary>
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
    /// Builds signature bytes.
    /// </summary>
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
    /// Builds associated data.
    /// </summary>
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
    /// Runs the derive peer key operation.
    /// </summary>
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
    /// Runs the hkdf sha256 operation.
    /// </summary>
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

    /// <summary>
    /// Runs the verify totp operation.
    /// </summary>
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
    /// Runs the base32 encode operation.
    /// </summary>
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
    /// Determines whether current trust.
    /// </summary>
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
    /// Determines whether security bootstrap.
    /// </summary>
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
    /// Runs the clamp operation.
    /// </summary>
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
    /// Runs the clone trusted peer operation.
    /// </summary>
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
