namespace LocalGPT.BusinessObjects;

/// <summary>Names the outcomes of a journaled 1-Wire packet.</summary>
public sealed class OneWireTrafficOutcomes
{
    /// <summary>A received packet passed validation and decryption.</summary>
    public const string Accepted = "Accepted";
    /// <summary>A received packet was rejected (validation, decryption, identity or size).</summary>
    public const string Rejected = "Rejected";
    /// <summary>A packet was written to the wire.</summary>
    public const string Delivered = "Delivered";
    /// <summary>Sending a packet failed before or while it was written.</summary>
    public const string Failed = "Failed";
}
