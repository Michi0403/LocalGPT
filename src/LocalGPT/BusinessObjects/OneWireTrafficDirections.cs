namespace LocalGPT.BusinessObjects;

/// <summary>Names the directions of a journaled 1-Wire packet.</summary>
public sealed class OneWireTrafficDirections
{
    /// <summary>A packet LocalGPT received.</summary>
    public const string Received = "Received";
    /// <summary>A packet LocalGPT sent.</summary>
    public const string Sent = "Sent";
}
