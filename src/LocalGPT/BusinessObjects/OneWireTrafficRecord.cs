namespace LocalGPT.BusinessObjects;

/// <summary>Describes one 1-Wire packet as a transport hands it to the traffic journal.</summary>
public sealed class OneWireTrafficRecord
{
    /// <summary>Gets or sets whether the packet was received or sent.</summary>
    /// <value>One of the <see cref="OneWireTrafficDirections"/> values.</value>
    public string Direction { get; set; } = OneWireTrafficDirections.Received;
    /// <summary>Gets or sets the transport the packet used.</summary>
    /// <value>One of the <see cref="OneWireTrafficTransports"/> values.</value>
    public string Transport { get; set; } = string.Empty;
    /// <summary>Gets or sets the remote peer identity, when it is known.</summary>
    /// <value>The peer id; empty before a peer identified itself.</value>
    public string PeerId { get; set; } = string.Empty;
    /// <summary>Gets or sets the remote network endpoint.</summary>
    /// <value>The address and port, or empty when unknown.</value>
    public string RemoteEndpoint { get; set; } = string.Empty;
    /// <summary>Gets or sets the frame exactly as it crossed the wire.</summary>
    /// <value>The raw JSON frame; encrypted content stays encrypted here.</value>
    public string RawFrame { get; set; } = string.Empty;
    /// <summary>Gets or sets the envelope after decryption (received) or before protection (sent), when one was parsed.</summary>
    /// <value>The envelope, or <see langword="null"/> when the frame could not be parsed or is not an envelope.</value>
    public OneWireEnvelope? Envelope { get; set; }
    /// <summary>Gets or sets an already captured plaintext JSON view, used instead of serializing <see cref="Envelope"/>.</summary>
    /// <value>The plaintext JSON; empty to serialize the envelope.</value>
    public string PlaintextJson { get; set; } = string.Empty;
    /// <summary>Gets or sets the message type shown for frames that are not envelopes, such as discovery beacons.</summary>
    /// <value>The message type label; empty to use the envelope's type.</value>
    public string MessageType { get; set; } = string.Empty;
    /// <summary>Gets or sets why the packet was rejected or could not be sent.</summary>
    /// <value>The error message; empty for a successful packet.</value>
    public string Error { get; set; } = string.Empty;
}
