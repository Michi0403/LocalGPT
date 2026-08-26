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
public sealed partial class OneWireRuntimeSecurityService : IOneWireRuntimeSecurityService
{
    /// <summary>
    /// Stores the logger used by <see cref="OneWireRuntimeSecurityService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<OneWireRuntimeSecurityService> logger;
    /// <summary>Stores host-specific secret-file permission handling behind an injected boundary.</summary>
    private readonly IRuntimeSecretFileProtectionService secretFileProtection;

    /// <summary>Initializes the type with its dependency-injected collaborators.</summary>
    /// <param name="logger">Injected dependency used by OneWireRuntimeSecurityService.</param>
    public OneWireRuntimeSecurityService(
        IRuntimeSecretFileProtectionService secretFileProtection,
        ILogger<OneWireRuntimeSecurityService> logger)
    {
        this.secretFileProtection = secretFileProtection;
        this.logger = logger;
    }

    /// <summary>
    /// Defines the schema version constant used by <see cref="OneWireRuntimeSecurityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int SchemaVersion = 1;
    /// <summary>
    /// Defines the totp period seconds constant used by <see cref="OneWireRuntimeSecurityService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int TotpPeriodSeconds = 30;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="OneWireRuntimeSecurityService"/>.
    /// </summary>
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="OneWireRuntimeSecurityService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    /// <summary>
    /// Stores the internal cached state used by <see cref="OneWireRuntimeSecurityService"/> while executing its surrounding workflow.
    /// </summary>
    private OneWireRuntimeSecretFile? cached;
    /// <summary>
    /// Stores the internal resolved path state used by <see cref="OneWireRuntimeSecurityService"/> while executing its surrounding workflow.
    /// </summary>
    private string? resolvedPath;

    /// <summary>
    /// Retrieves status as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire runtime security status produced by the operation.</returns>
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
    /// Ensures created as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs regenerate as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Performs delete as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Retrieves public descriptor as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire security descriptor produced by the operation.</returns>
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
    /// Creates pairing ticket as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="lifetime">Lifetime value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire pairing ticket produced by the operation.</returns>
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
    /// Retrieves otp auth URI as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs establish trust as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs revoke trust as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Retrieves trusted peers as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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
}
