using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported network certificate key size values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum NetworkCertificateKeySize
{
    /// <summary>
    /// Selects the rsa2048 option for <see cref="NetworkCertificateKeySize"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Rsa2048 = 2048,
    /// <summary>
    /// Selects the rsa3072 option for <see cref="NetworkCertificateKeySize"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Rsa3072 = 3072,
    /// <summary>
    /// Selects the rsa4096 option for <see cref="NetworkCertificateKeySize"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Rsa4096 = 4096
}

/// <summary>
/// Defines the supported network certificate hash values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum NetworkCertificateHash
{
    /// <summary>
    /// Selects the SHA-256 option for <see cref="NetworkCertificateHash"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sha256,
    /// <summary>
    /// Selects the sha384 option for <see cref="NetworkCertificateHash"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sha384,
    /// <summary>
    /// Selects the sha512 option for <see cref="NetworkCertificateHash"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sha512
}

/// <summary>
/// Names interoperable X.509 extended-key-usage roles that can be combined for a generated certificate.
/// </summary>
public enum NetworkCertificateExtendedKeyUsage
{
    /// <summary>Authenticates an HTTPS/TLS server.</summary>
    ServerAuthentication,
    /// <summary>Authenticates a TLS client.</summary>
    ClientAuthentication,
    /// <summary>Signs executable code.</summary>
    CodeSigning,
    /// <summary>Protects S/MIME email messages.</summary>
    EmailProtection,
    /// <summary>Signs trusted timestamps.</summary>
    TimeStamping,
    /// <summary>Signs OCSP responses.</summary>
    OcspSigning,
    /// <summary>Authenticates an IPsec end system.</summary>
    IpsecEndSystem,
    /// <summary>Authenticates an IPsec tunnel.</summary>
    IpsecTunnel,
    /// <summary>Authenticates an IPsec user.</summary>
    IpsecUser,
    /// <summary>Allows any purpose. Explicit selection is required; never a default.</summary>
    AnyExtendedKeyUsage
}

/// <summary>
/// Represents the input contract for network certificate create, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class NetworkCertificateCreateRequest : IValidatableObject
{
    /// <summary>
    /// Gets or sets the common name value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The common name value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    [Required(ErrorMessage = "Enter a certificate common name.")]
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
    [Range(1, 3650, ErrorMessage = "Validity must be between 1 and 3650 days.")]
    public int ValidityDays { get; set; } = 825;
    /// <summary>
    /// Gets or sets the key size that quantifies the associated network certificate create data.
    /// </summary>
    /// <value>The key size value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public NetworkCertificateKeySize KeySize { get; set; } = NetworkCertificateKeySize.Rsa3072;
    /// <summary>
    /// Gets or sets the hash value that forms part of the network certificate create state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The hash value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    public NetworkCertificateHash Hash { get; set; } = NetworkCertificateHash.Sha256;
    /// <summary>Gets or sets the independently selectable X.509 key-usage flags.</summary>
    /// <value>Flags combined in the generated Key Usage extension.</value>
    public IEnumerable<X509KeyUsageFlags> KeyUsages { get; set; } =
        [X509KeyUsageFlags.DigitalSignature, X509KeyUsageFlags.KeyEncipherment];
    /// <summary>Gets or sets the independently selectable extended-key-usage roles.</summary>
    /// <value>Roles combined in the generated EKU extension.</value>
    [CertificateExtendedKeyUsageSelection]
    public IEnumerable<NetworkCertificateExtendedKeyUsage> ExtendedKeyUsages { get; set; } =
        [NetworkCertificateExtendedKeyUsage.ServerAuthentication];
    /// <summary>Gets or sets whether to issue a CA certificate instead of an end-entity certificate.</summary>
    /// <value>True only when the user intentionally generates a certificate authority.</value>
    public bool IsCertificateAuthority { get; set; }
    /// <summary>Gets or sets whether this request must be usable as a Kestrel TLS server certificate.</summary>
    /// <value>True when the generated certificate is to be assigned to the network listener.</value>
    public bool RequireServerAuthentication { get; set; } = true;
    /// <summary>
    /// Gets or sets the output path used by this network certificate create instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The output path value exposed by <see cref="NetworkCertificateCreateRequest"/>.</value>
    [Required(ErrorMessage = "Choose the output PFX path.")]
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

    /// <summary>
    /// Validates coupled certificate-purpose selections before a DevExpress form can submit.
    /// External callers must also pass the service's independent security checks.
    /// </summary>
    /// <param name="validationContext">The active Blazor EditForm validation context.</param>
    /// <returns>Field-associated errors for invalid certificate combinations.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(KeySize) || !Enum.IsDefined(Hash))
            yield return new ValidationResult("Select a supported RSA key size and hash algorithm.", [nameof(KeySize), nameof(Hash)]);

        var selectedKeyUsages = (KeyUsages ?? []).Distinct().ToArray();
        if (selectedKeyUsages.Length == 0 || selectedKeyUsages.Contains(X509KeyUsageFlags.None) ||
            selectedKeyUsages.Any(value => !Enum.IsDefined(value)))
            yield return new ValidationResult("Select one or more individual X.509 key usages.", [nameof(KeyUsages)]);

        var keyUsage = selectedKeyUsages.Aggregate(X509KeyUsageFlags.None, (combined, current) => combined | current);
        if ((keyUsage & (X509KeyUsageFlags.EncipherOnly | X509KeyUsageFlags.DecipherOnly)) != 0 &&
            (keyUsage & X509KeyUsageFlags.KeyAgreement) == 0)
            yield return new ValidationResult("EncipherOnly and DecipherOnly require KeyAgreement.", [nameof(KeyUsages)]);

        if (IsCertificateAuthority && (keyUsage & X509KeyUsageFlags.KeyCertSign) == 0)
            yield return new ValidationResult("Certificate authorities must select KeyCertSign.", [nameof(IsCertificateAuthority), nameof(KeyUsages)]);
        if (!IsCertificateAuthority && (keyUsage & (X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign)) != 0)
            yield return new ValidationResult("KeyCertSign and CrlSign require Certificate Authority mode.", [nameof(IsCertificateAuthority), nameof(KeyUsages)]);

        var selectedRoles = (ExtendedKeyUsages ?? []).Distinct().ToArray();

        if (RequireServerAuthentication && IsCertificateAuthority)
            yield return new ValidationResult("An HTTPS listener needs an end-entity certificate, not a Certificate Authority.", [nameof(IsCertificateAuthority), nameof(RequireServerAuthentication)]);
        if (RequireServerAuthentication && !selectedRoles.Contains(NetworkCertificateExtendedKeyUsage.ServerAuthentication))
            yield return new ValidationResult("HTTPS requires the ServerAuthentication EKU.", [nameof(ExtendedKeyUsages), nameof(RequireServerAuthentication)]);
        if (RequireServerAuthentication &&
            (keyUsage & (X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment)) == 0)
            yield return new ValidationResult("HTTPS requires DigitalSignature or KeyEncipherment.", [nameof(KeyUsages), nameof(RequireServerAuthentication)]);
    }
}

/// <summary>
/// Validates the extended-key-usage selection against CA mode as a field-level DataAnnotation.
/// DevExpress can show this error beside the bound DxListBox before form submission.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CertificateExtendedKeyUsageSelectionAttribute : ValidationAttribute
{
    /// <summary>
    /// Validates end-entity EKUs against the current certificate purpose and supported roles.
    /// </summary>
    /// <param name="value">Selected EKU roles supplied by the bound list editor.</param>
    /// <param name="validationContext">Context containing the full certificate request.</param>
    /// <returns>A field-scoped validation error, or success for a supported combination.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var field = validationContext.MemberName ?? nameof(NetworkCertificateCreateRequest.ExtendedKeyUsages);
        if (validationContext.ObjectInstance is not NetworkCertificateCreateRequest request)
            return new ValidationResult("The certificate request is not available for validation.", [field]);

        var roles = (value as IEnumerable<NetworkCertificateExtendedKeyUsage> ?? []).Distinct().ToArray();
        if (roles.Any(role => !Enum.IsDefined(role)))
            return new ValidationResult("Select only supported Extended Key Usage roles.", [field]);
        if (request.IsCertificateAuthority && roles.Length > 0)
            return new ValidationResult("A Certificate Authority cannot contain end-entity EKUs. Clear all Extended Key Usage selections.", [field]);
        if (!request.IsCertificateAuthority && roles.Length == 0)
            return new ValidationResult("Select at least one Extended Key Usage for an end-entity certificate.", [field]);
        return ValidationResult.Success;
    }
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
