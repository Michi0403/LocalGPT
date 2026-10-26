using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LocalGPT.Services;

/// <summary>Creates explicit user-requested self-signed PFX certificates for the optional LocalGPT LAN/VPN Kestrel endpoint.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class NetworkCertificateService(ILogger<NetworkCertificateService> logger) : INetworkCertificateService
{
    /// <summary>
    /// Creates default request as part of the network certificate service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The network certificate create request produced by the operation.</returns>
    public NetworkCertificateCreateRequest CreateDefaultRequest()
    {
        try
        {
            var directory = LocalGptApplicationDataPaths.ResolveUserPath("certificates");
            var sans = new List<string> { Environment.MachineName, "localhost", "127.0.0.1" };
            try
            {
                sans.AddRange(Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(address => !IPAddress.IsLoopback(address))
                    .Select(address => address.ToString()));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not enumerate local interface addresses for certificate defaults.");
            }

            return new NetworkCertificateCreateRequest
            {
                CommonName = Environment.MachineName,
                SubjectAlternativeNames = string.Join("; ", sans.Distinct(StringComparer.OrdinalIgnoreCase)),
                OutputPath = Path.Combine(directory, "localgpt-network.pfx")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create network certificate defaults.");
            throw;
        }
    }

    /// <summary>
    /// Performs create as part of the network certificate service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The network certificate create result produced by the operation.</returns>
    public async Task<NetworkCertificateCreateResult> CreateAsync(NetworkCertificateCreateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(request.CommonName)) throw new ArgumentException("A certificate common name is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.OutputPath)) throw new ArgumentException("A PFX output path is required.", nameof(request));
            if (request.ValidityDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(request), "Certificate validity must be between 1 and 3650 days.");

            var outputPath = Environment.ExpandEnvironmentVariables(request.OutputPath.Trim());
            if (!Path.IsPathRooted(outputPath)) outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            if (!Enum.IsDefined(request.KeySize) || !Enum.IsDefined(request.Hash))
                throw new ArgumentException("Select a supported RSA key size and hash algorithm.", nameof(request));
            var selectedKeyUsages = (request.KeyUsages ?? []).Distinct().ToArray();
            if (selectedKeyUsages.Length == 0 || selectedKeyUsages.Contains(X509KeyUsageFlags.None) ||
                selectedKeyUsages.Any(value => !Enum.IsDefined(value)))
                throw new ArgumentException("Select at least one individual X.509 key-usage flag.", nameof(request));
            var keyUsage = selectedKeyUsages.Aggregate(X509KeyUsageFlags.None, (flags, value) => flags | value);
            if ((keyUsage & (X509KeyUsageFlags.EncipherOnly | X509KeyUsageFlags.DecipherOnly)) != 0 &&
                (keyUsage & X509KeyUsageFlags.KeyAgreement) == 0)
                throw new ArgumentException("EncipherOnly/DecipherOnly require KeyAgreement.", nameof(request));
            if (request.IsCertificateAuthority != ((keyUsage & X509KeyUsageFlags.KeyCertSign) != 0))
                throw new ArgumentException("CA certificates require KeyCertSign; end-entity certificates must not have KeyCertSign.", nameof(request));
            if (!request.IsCertificateAuthority && (keyUsage & X509KeyUsageFlags.CrlSign) != 0)
                throw new ArgumentException("CRL signing requires the Certificate Authority option.", nameof(request));
            var selectedRoles = (request.ExtendedKeyUsages ?? []).Distinct().ToArray();
            if (selectedRoles.Any(role => !Enum.IsDefined(role)))
                throw new ArgumentException("Unknown extended-key-usage role.", nameof(request));
            if (!request.IsCertificateAuthority && selectedRoles.Length == 0)
                throw new ArgumentException("An end-entity certificate requires at least one extended-key-usage role.", nameof(request));
            if (request.IsCertificateAuthority && selectedRoles.Length > 0)
                throw new ArgumentException("Certificate Authority certificates cannot contain end-entity extended-key-usage roles.", nameof(request));
            if (request.RequireServerAuthentication &&
                (request.IsCertificateAuthority ||
                 !selectedRoles.Contains(NetworkCertificateExtendedKeyUsage.ServerAuthentication) ||
                 (keyUsage & (X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment)) == 0))
                throw new ArgumentException("TLS server certificates require Server Authentication EKU and a signing/encipherment key usage.", nameof(request));

            var hash = request.Hash switch
            {
                NetworkCertificateHash.Sha256 => HashAlgorithmName.SHA256,
                NetworkCertificateHash.Sha384 => HashAlgorithmName.SHA384,
                NetworkCertificateHash.Sha512 => HashAlgorithmName.SHA512,
                _ => throw new ArgumentOutOfRangeException(nameof(request), "Unsupported hash algorithm.")
            };

            using var rsa = RSA.Create((int)request.KeySize);
            var certificateRequest = new CertificateRequest(
                $"CN={EscapeDistinguishedNameValue(request.CommonName.Trim())}",
                rsa,
                hash,
                RSASignaturePadding.Pkcs1);
            certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(
                request.IsCertificateAuthority, request.IsCertificateAuthority, 0, true));
            certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));
            if (selectedRoles.Length > 0)
            {
                var extendedKeyUsages = new OidCollection();
                foreach (var role in selectedRoles)
                    extendedKeyUsages.Add(MapExtendedKeyUsage(role));
                certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(extendedKeyUsages, false));
            }

            var sanBuilder = new SubjectAlternativeNameBuilder();
            var sanValues = ParseSubjectAlternativeNames(request.SubjectAlternativeNames, request.CommonName);
            foreach (var value in sanValues)
            {
                if (IPAddress.TryParse(value, out var address)) sanBuilder.AddIpAddress(address);
                else sanBuilder.AddDnsName(value);
            }
            certificateRequest.CertificateExtensions.Add(sanBuilder.Build());

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
            var notAfter = notBefore.AddDays(request.ValidityDays);
            using var certificate = certificateRequest.CreateSelfSigned(notBefore, notAfter);
            // Fail before touching the previous PFX if the new certificate cannot serve HTTPS.
            // Kestrel requires the real Server Authentication OID, not a similar-looking OID.
            var serverAuthenticationOid = "1.3.6.1.5.5.7.3.1";
            if (request.RequireServerAuthentication && (!certificate.HasPrivateKey ||
                !certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>()
                    .Any(extension => extension.EnhancedKeyUsages.Cast<Oid>()
                        .Any(usage => string.Equals(usage.Value, serverAuthenticationOid, StringComparison.Ordinal)))))
            {
                throw new InvalidOperationException("Generated network certificate is not valid for TLS Server Authentication.");
            }
            var pfx = certificate.Export(X509ContentType.Pfx, request.Password ?? string.Empty);
            var temporaryPath = outputPath + ".tmp";
            await File.WriteAllBytesAsync(temporaryPath, pfx, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, outputPath, true);

            var storeDescription = "not installed in a certificate store";
            if (request.InstallToStore)
            {
                using var store = new X509Store(request.StoreName, request.StoreLocation);
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                storeDescription = $"{request.StoreLocation}/{request.StoreName}";
            }

            logger.LogInformation(
                "Created LocalGPT network certificate {Thumbprint} at {CertificatePath}; store={StoreDescription}.",
                certificate.Thumbprint, outputPath, storeDescription);
            return new NetworkCertificateCreateResult(
                outputPath,
                certificate.Thumbprint,
                notBefore,
                notAfter,
                sanValues,
                storeDescription);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create the LocalGPT network endpoint certificate.");
            throw;
        }
    }

    /// <summary>Resolves the standards-defined OID for a selected extended-key-usage role.</summary>
    /// <param name="role">The EKU role selected in the certificate request.</param>
    /// <returns>The exact OID representing the selected EKU role.</returns>
    private Oid MapExtendedKeyUsage(NetworkCertificateExtendedKeyUsage role) => role switch
    {
        NetworkCertificateExtendedKeyUsage.ServerAuthentication => new Oid("1.3.6.1.5.5.7.3.1", "TLS Web Server Authentication"),
        NetworkCertificateExtendedKeyUsage.ClientAuthentication => new Oid("1.3.6.1.5.5.7.3.2", "TLS Web Client Authentication"),
        NetworkCertificateExtendedKeyUsage.CodeSigning => new Oid("1.3.6.1.5.5.7.3.3", "Code Signing"),
        NetworkCertificateExtendedKeyUsage.EmailProtection => new Oid("1.3.6.1.5.5.7.3.4", "Email Protection"),
        NetworkCertificateExtendedKeyUsage.IpsecEndSystem => new Oid("1.3.6.1.5.5.7.3.5", "IPsec End System"),
        NetworkCertificateExtendedKeyUsage.IpsecTunnel => new Oid("1.3.6.1.5.5.7.3.6", "IPsec Tunnel"),
        NetworkCertificateExtendedKeyUsage.IpsecUser => new Oid("1.3.6.1.5.5.7.3.7", "IPsec User"),
        NetworkCertificateExtendedKeyUsage.TimeStamping => new Oid("1.3.6.1.5.5.7.3.8", "Time Stamping"),
        NetworkCertificateExtendedKeyUsage.OcspSigning => new Oid("1.3.6.1.5.5.7.3.9", "OCSP Signing"),
        NetworkCertificateExtendedKeyUsage.AnyExtendedKeyUsage => new Oid("2.5.29.37.0", "Any Extended Key Usage"),
        _ => throw new ArgumentOutOfRangeException(nameof(role), "Unsupported certificate role.")
    };

    /// <summary>
    /// Parses subject alternative names as part of the network certificate service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="text">Text value supplied to the network certificate operation and used when producing its result.</param>
    /// <param name="commonName">Common name value supplied to the network certificate operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> ParseSubjectAlternativeNames(string? text, string commonName)
    {
        try
        {
            var values = (text ?? string.Empty)
                .Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            if (!values.Contains(commonName, StringComparer.OrdinalIgnoreCase)) values.Insert(0, commonName);
            return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not parse certificate subject alternative names.");
            throw;
        }
    }

    /// <summary>
    /// Performs escape distinguished name value as part of the network certificate service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the network certificate operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string EscapeDistinguishedNameValue(string value)
    {
        try
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace(",", "\\,", StringComparison.Ordinal)
                .Replace("+", "\\+", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("<", "\\<", StringComparison.Ordinal)
                .Replace(">", "\\>", StringComparison.Ordinal)
                .Replace(";", "\\;", StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not escape certificate common-name value.");
            throw;
        }
    }
}
