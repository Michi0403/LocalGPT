using System.Security.Cryptography.X509Certificates;

namespace LocalGPT.BusinessObjects;

public enum NetworkCertificateKeySize
{
    Rsa2048 = 2048,
    Rsa3072 = 3072,
    Rsa4096 = 4096
}

public enum NetworkCertificateHash
{
    Sha256,
    Sha384,
    Sha512
}

public sealed class NetworkCertificateCreateRequest
{
    public string CommonName { get; set; } = Environment.MachineName;
    public string SubjectAlternativeNames { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 825;
    public NetworkCertificateKeySize KeySize { get; set; } = NetworkCertificateKeySize.Rsa2048;
    public NetworkCertificateHash Hash { get; set; } = NetworkCertificateHash.Sha256;
    public string OutputPath { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool InstallToStore { get; set; }
    public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;
    public StoreName StoreName { get; set; } = StoreName.My;
}

public sealed record NetworkCertificateCreateResult(
    string PfxPath,
    string Thumbprint,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    IReadOnlyList<string> SubjectAlternativeNames,
    string StoreDescription);
