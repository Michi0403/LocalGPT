namespace LocalGPT.BusinessObjects;

internal sealed class OneWireRuntimeSecretFile
{
    public int SchemaVersion { get; set; }
    public string PeerId { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? RotatedUtc { get; set; }
    public string RootSecret { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string KeyAgreementPrivateKey { get; set; } = string.Empty;
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    public string SigningPrivateKey { get; set; } = string.Empty;
    public string SigningPublicKey { get; set; } = string.Empty;
    public string MfaSeed { get; set; } = string.Empty;
    public List<OneWireTrustedPeerDescriptor> TrustedPeers { get; set; } = [];
}
