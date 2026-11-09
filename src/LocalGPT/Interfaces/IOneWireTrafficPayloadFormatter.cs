using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Applies the traffic journal's payload mode (Full, Redacted, None) and length bounds to captured packet content.</summary>
public interface IOneWireTrafficPayloadFormatter
{
    /// <summary>Formats a captured plaintext payload according to the payload mode.</summary>
    /// <param name="plaintextJson">The captured plaintext JSON; may be empty.</param>
    /// <param name="options">The traffic options in effect.</param>
    /// <returns>The full payload, the payload with secret-like properties masked, or empty for mode None.</returns>
    string Format(string plaintextJson, OneWireTrafficOptions options);

    /// <summary>Bounds text to a maximum length and marks the cut.</summary>
    /// <param name="text">The text to bound.</param>
    /// <param name="maximumCharacters">The maximum length; 0 or less keeps the text unchanged.</param>
    /// <returns>The bounded text.</returns>
    string Bound(string text, int maximumCharacters);
}
