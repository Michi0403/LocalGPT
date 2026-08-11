namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an one wire runtime secret file.
/// </summary>
internal sealed class OneWireRuntimeSecretFile
{
    /// <summary>
    /// Gets or sets schema version.
    /// </summary>
    public int SchemaVersion { get; set; }
    /// <summary>
    /// Gets or sets peer identifier.
    /// </summary>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets created UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets rotated UTC.
    /// </summary>
    public DateTimeOffset? RotatedUtc { get; set; }
    /// <summary>
    /// Gets or sets root secret.
    /// </summary>
    public string RootSecret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fingerprint.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key agreement private key.
    /// </summary>
    public string KeyAgreementPrivateKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets key agreement public key.
    /// </summary>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signing private key.
    /// </summary>
    public string SigningPrivateKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets signing public key.
    /// </summary>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets mfa seed.
    /// </summary>
    public string MfaSeed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets trusted peers.
    /// </summary>
    public List<OneWireTrustedPeerDescriptor> TrustedPeers { get; set; } = [];
}
