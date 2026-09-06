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
    /// Loads core as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="createWhenMissing">Value indicating whether create when missing should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire runtime secret file produced by the operation.</returns>
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
    /// Persists core as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Attempts to restrict secret permissions as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the one wire runtime security operation and used when producing its result.</param>
    private void TryRestrictSecretPermissions(string path)
    {
        try
        {
            secretFileProtection.RestrictToCurrentUser(path);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(OneWireRuntimeSecurityService), nameof(TryRestrictSecretPermissions), exception);
            throw;
        }
    }

    /// <summary>
    /// Resolves secret path as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveSecretPath()
    {
    try
    {
            if (!string.IsNullOrWhiteSpace(resolvedPath)) return resolvedPath;
            var portable = Path.Combine(AppContext.BaseDirectory, "security", "onewire-secret.json");
            if (File.Exists(portable) && CanWriteDirectory(Path.GetDirectoryName(portable)!))
            {
                logger.LogInformation("Preserving the existing portable LocalGPT 1-Wire secret at {SecretPath}.", portable);
                return resolvedPath = portable;
            }

            var preferred = LocalGptApplicationDataPaths.ResolveUserPath("Security",
                "onewire-secret.json");
            if (CanWriteDirectory(Path.GetDirectoryName(preferred)!))
                return resolvedPath = preferred;

            if (CanWriteDirectory(Path.GetDirectoryName(portable)!))
            {
                logger.LogWarning("The LocalGPT per-user data directory is not writable; falling back to the portable program directory for the runtime 1-Wire secret: {SecretPath}.", portable);
                return resolvedPath = portable;
            }

            throw new UnauthorizedAccessException("LocalGPT cannot create a writable runtime 1-Wire secret directory in either per-user application data or the portable program directory.");
    
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
    /// Determines whether write directory as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="directory">Directory value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Creates secret as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="createdUtc">Created utc value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <param name="rotatedUtc">Rotated utc value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The one wire runtime secret file produced by the operation.</returns>
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
    /// Validates secret as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the one wire runtime security operation and used when producing its result.</param>
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
    /// Creates status as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The one wire runtime security status produced by the operation.</returns>
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
    /// Creates public descriptor as part of the one wire runtime security service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="file">File value supplied to the one wire runtime security operation and used when producing its result.</param>
    /// <returns>The one wire security descriptor produced by the operation.</returns>
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

}
