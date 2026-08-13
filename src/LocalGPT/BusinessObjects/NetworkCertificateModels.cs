using System.Security.Cryptography.X509Certificates;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported network certificate key size values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum NetworkCertificateKeySize
{
    Rsa2048 = 2048,
    Rsa3072 = 3072,
    Rsa4096 = 4096
}

/// <summary>
/// Defines the supported network certificate hash values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum NetworkCertificateHash
{
    Sha256,
    Sha384,
    Sha512
}

/// <summary>
/// Represents the input contract for network certificate create, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class NetworkCertificateCreateRequest
{
    /// <summary>
    /// Gets or sets the common name value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The common name value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public string CommonName { get; set; } = Environment.MachineName;
    /// <summary>
    /// Gets or sets the subject alternative names value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The subject alternative names value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public string SubjectAlternativeNames { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the validity days value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The validity days value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public int ValidityDays { get; set; } = 825;
    /// <summary>
    /// Gets or sets the key size that quantifies the associated network certificate create data.
    /// </summary>
    /// <value>The key size value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public NetworkCertificateKeySize KeySize { get; set; } = NetworkCertificateKeySize.Rsa2048;
    /// <summary>
    /// Gets or sets the hash value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hash value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public NetworkCertificateHash Hash { get; set; } = NetworkCertificateHash.Sha256;
    /// <summary>
    /// Gets or sets the output path used by this network certificate create instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The output path value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public string OutputPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the password value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The password value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public string Password { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether install to store applies to the network certificate create state.
    /// </summary>
    /// <value>The install to store value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public bool InstallToStore { get; set; }
    /// <summary>
    /// Gets or sets the store location value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The store location value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;
    /// <summary>
    /// Gets or sets the store name value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The store name value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public StoreName StoreName { get; set; } = StoreName.My;
}

/// <summary>
/// Represents the outcome of network certificate create, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="PfxPath">Pfx path value supplied to the network certificate create operation and used when producing its result.</param>
/// <param name="Thumbprint">Thumbprint value supplied to the network certificate create operation and used when producing its result.</param>
/// <param name="NotBefore">Not before value supplied to the network certificate create operation and used when producing its result.</param>
/// <param name="NotAfter">Not after value supplied to the network certificate create operation and used when producing its result.</param>
/// <param name="SubjectAlternativeNames">String dependency used by the network certificate create workflow to provide the corresponding application capability.</param>
/// <param name="StoreDescription">Store description value supplied to the network certificate create operation and used when producing its result.</param>
public sealed record NetworkCertificateCreateResult(
    string PfxPath,
    string Thumbprint,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    IReadOnlyList<string> SubjectAlternativeNames,
    string StoreDescription);
