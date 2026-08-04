# Organic 1-Wire and security

## Protocol meaning

LocalGPT's organic 1-Wire is a transport-neutral application protocol for cooperation between LocalGPT, PublisherStudio-style peers, approved add-ons, and device gateways. It may travel over TCP, HTTP/JSON, or another bounded transport. It is not the physical Dallas/Maxim 1-Wire protocol, though a gateway may bridge to physical hardware.

## Identity

Each application creates its own local identity material at runtime. Private keys, shared secrets, certificates, MFA seeds, and trusted-peer databases are not committed to source or embedded in releases.

Identity creation, regeneration, deletion, and peer trust are visible user operations. Read-only program directories fall back to the user's application-data location.

## Connection flow

A safe peer flow is:

1. discover a peer without auto-trusting it;
2. user selects the peer;
3. exchange and display identity information;
4. user confirms the intended relationship;
5. establish the protected session;
6. advertise bounded capabilities;
7. submit work through the spooler;
8. require fresh approval for protected operations.

A sender-supplied `UserConfirmed=true` flag is data, not proof of local approval.

## Envelope and replay protection

Protected messages use an envelope with stable identifiers, timestamps/nonces, target capability, payload, and authentication material. The receiver validates identity, freshness, replay policy, capability, and local approval before dispatch.

Replay records are durable where needed and bounded by retention policy. Invalid envelopes fail without exposing secrets or executing partial work.

## Capability routing

The capability catalog maps protocol operations to application services. It prevents transport handlers from reflecting arbitrary public methods or constructing unreviewed commands.

Capabilities may include reviewed text exchange, screen capture requests, website/document content, embedded wiring proposals, OCR, or plugin work results. Each capability has its own parameters and approval behavior.

## Work spooler

Incoming work enters a queue with peer identity, capability, payload summary, status, and decision state. A hosted processor executes only work that has passed validation and approval.

## Security invariants

- no trust on discovery alone;
- no credential reuse across unrelated endpoints;
- no arbitrary reflection execution;
- no uploaded executable execution;
- no automatic filesystem write outside approved workspace;
- no actuator/flash/native command without the matching approval;
- no secret content in logs or documentation;
- no model response treated as authority.

The protocol remains useful because the boundaries are explicit, not because every peer is assumed friendly. 🐾
