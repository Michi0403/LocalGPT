using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LocalGPT.Services;

/// <summary>Creates explicit user-requested self-signed PFX certificates for the optional LocalGPT LAN/VPN Kestrel endpoint.</summary>
public sealed class NetworkCertificateService(ILogger<NetworkCertificateService> logger) : INetworkCertificateService
{
    public NetworkCertificateCreateRequest CreateDefaultRequest()
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "certificates");
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

            var hash = request.Hash switch
            {
                NetworkCertificateHash.Sha384 => HashAlgorithmName.SHA384,
                NetworkCertificateHash.Sha512 => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA256
            };

            using var rsa = RSA.Create((int)request.KeySize);
            var certificateRequest = new CertificateRequest(
                $"CN={EscapeDistinguishedNameValue(request.CommonName.Trim())}",
                rsa,
                hash,
                RSASignaturePadding.Pkcs1);
            certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                true));
            certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1", "TLS Web Server Authentication") },
                false));

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
