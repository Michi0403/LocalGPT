using System.Security.Cryptography.X509Certificates;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Lists supported network certificate key size values.
/// </summary>
public enum NetworkCertificateKeySize
{
    Rsa2048 = 2048,
    Rsa3072 = 3072,
    Rsa4096 = 4096
}

/// <summary>
/// Lists supported network certificate hash values.
/// </summary>
public enum NetworkCertificateHash
{
    Sha256,
    Sha384,
    Sha512
}

/// <summary>
/// Represents a network certificate create request.
/// </summary>
public sealed class NetworkCertificateCreateRequest
{
    /// <summary>
    /// Gets or sets common name.
    /// </summary>
    public string CommonName { get; set; } = Environment.MachineName;
    /// <summary>
    /// Gets or sets subject alternative names.
    /// </summary>
    public string SubjectAlternativeNames { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets validity days.
    /// </summary>
    public int ValidityDays { get; set; } = 825;
    /// <summary>
    /// Gets or sets key size.
    /// </summary>
    public NetworkCertificateKeySize KeySize { get; set; } = NetworkCertificateKeySize.Rsa2048;
    /// <summary>
    /// Gets or sets hash.
    /// </summary>
    public NetworkCertificateHash Hash { get; set; } = NetworkCertificateHash.Sha256;
    /// <summary>
    /// Gets or sets output path.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets install to store.
    /// </summary>
    public bool InstallToStore { get; set; }
    /// <summary>
    /// Gets or sets store location.
    /// </summary>
    public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;
    /// <summary>
    /// Gets or sets store name.
    /// </summary>
    public StoreName StoreName { get; set; } = StoreName.My;
}

/// <summary>
/// Represents a network certificate create result.
/// </summary>
public sealed record NetworkCertificateCreateResult(
    string PfxPath,
    string Thumbprint,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    IReadOnlyList<string> SubjectAlternativeNames,
    string StoreDescription);
