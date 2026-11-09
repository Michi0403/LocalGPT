namespace LocalGPT.BusinessObjects;

/// <summary>One journaled 1-Wire packet as the in-app traffic view shows it.</summary>
/// <param name="Sequence">Monotonic journal sequence number.</param>
/// <param name="TimestampUtc">When the packet was journaled.</param>
/// <param name="Direction">Received or Sent.</param>
/// <param name="Transport">Transport that carried the packet.</param>
/// <param name="PeerId">Remote peer identity, when known.</param>
/// <param name="RemoteEndpoint">Remote network endpoint, when known.</param>
/// <param name="MessageType">Envelope message type or frame label.</param>
/// <param name="MessageId">Envelope message id; empty for non-envelope frames.</param>
/// <param name="CorrelationId">Envelope correlation id; empty for non-envelope frames.</param>
/// <param name="CapabilityKey">Capability the envelope addresses, when any.</param>
/// <param name="SecurityMode">Envelope security mode as it crossed the wire.</param>
/// <param name="FrameBytes">UTF-8 size of the raw frame.</param>
/// <param name="Outcome">Accepted, Rejected, Delivered or Failed.</param>
/// <param name="Error">Rejection or failure reason; empty on success.</param>
/// <param name="Payload">Decrypted payload JSON according to the payload mode; empty when not captured.</param>
/// <param name="RawFrame">Raw wire frame when captured; empty otherwise.</param>
public sealed record OneWireTrafficEntry(
    long Sequence,
    DateTime TimestampUtc,
    string Direction,
    string Transport,
    string PeerId,
    string RemoteEndpoint,
    string MessageType,
    Guid MessageId,
    Guid CorrelationId,
    string CapabilityKey,
    string SecurityMode,
    int FrameBytes,
    string Outcome,
    string Error,
    string Payload,
    string RawFrame);
