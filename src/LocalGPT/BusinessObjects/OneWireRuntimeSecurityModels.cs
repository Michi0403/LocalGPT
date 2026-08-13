namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an one wire runtime secret file application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal sealed class OneWireRuntimeSecretFile
{
    /// <summary>
    /// Gets or sets the schema version value that forms part of the one wire runtime secret file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The schema version value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public int SchemaVersion { get; set; }
    /// <summary>
    /// Gets or sets the stable peer identifier used to identify or correlate this one wire runtime secret file instance with related application state.
    /// </summary>
    /// <value>The peer identifier value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the created UTC associated with this one wire runtime secret file state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created UTC value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public DateTimeOffset CreatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the rotated UTC associated with this one wire runtime secret file state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The rotated UTC value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public DateTimeOffset? RotatedUtc { get; set; }
    /// <summary>
    /// Gets or sets the root secret value that forms part of the one wire runtime secret file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The root secret value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string RootSecret { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key identifier used to identify or correlate this one wire runtime secret file instance with related application state.
    /// </summary>
    /// <value>The key identifier value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string KeyId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fingerprint value that forms part of the one wire runtime secret file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fingerprint value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string Fingerprint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key agreement private key used to identify or correlate this one wire runtime secret file instance with related application state.
    /// </summary>
    /// <value>The key agreement private key value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string KeyAgreementPrivateKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable key agreement public key used to identify or correlate this one wire runtime secret file instance with related application state.
    /// </summary>
    /// <value>The key agreement public key value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string KeyAgreementPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable signing private key used to identify or correlate this one wire runtime secret file instance with related application state.
    /// </summary>
    /// <value>The signing private key value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string SigningPrivateKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable signing public key used to identify or correlate this one wire runtime secret file instance with related application state.
    /// </summary>
    /// <value>The signing public key value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string SigningPublicKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the MFA seed value that forms part of the one wire runtime secret file state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The MFA seed value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public string MfaSeed { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the trusted peers collection maintained or exposed by this one wire runtime secret file instance for downstream processing.
    /// </summary>
    /// <value>The trusted peers value exposed by <see cref="OneWireRuntimeSecretFile"/>.</value>
    public List<OneWireTrustedPeerDescriptor> TrustedPeers { get; set; } = [];
}
