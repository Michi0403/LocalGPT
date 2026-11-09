namespace LocalGPT.BusinessObjects;

/// <summary>Names the payload capture modes of the 1-Wire traffic journal.</summary>
public sealed class OneWireTrafficPayloadModes
{
    /// <summary>Captures the complete decrypted payload.</summary>
    public const string Full = "Full";
    /// <summary>Captures the decrypted payload with secret-like properties masked.</summary>
    public const string Redacted = "Redacted";
    /// <summary>Captures no payload, only packet metadata.</summary>
    public const string None = "None";
}
