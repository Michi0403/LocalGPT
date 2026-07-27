# Organic 1-Wire runtime identity, MFA trust, encryption and HTTP/JSON

## Runtime-generated identity

No private key, certificate, MFA seed or trusted-peer database is compiled into either application or committed to Git. LocalGPT and PublisherStudio each generate their own random `security/onewire-secret.json` at runtime after the user selects **Create identity**. If the program directory is read-only, the service falls back to the current user's local application-data directory.

The frontend can create, regenerate or delete this file. Regeneration or deletion intentionally invalidates previous encrypted trust and is the supported way to reset a compromised or unwanted relationship. The applications never display, transmit or log private keys.

## Pairing and MFA

Each side can explicitly generate:

- an Authenticator-compatible TOTP enrollment URI;
- a short-lived, signed public pairing ticket containing public keys and a fingerprint only;
- a QR representation in PublisherStudio's organic security panel.

The user imports the remote public ticket, enters the current **local** Authenticator code and selects a trust validity. Pairing is reciprocal: repeat it in the other application. Trust validity and the remote public keys are stored in the local runtime secret file. TOTP follows RFC 6238 semantics with a 30-second period and a small clock-drift window.

## Secured payload

After reciprocal trust exists, the sender derives a peer-specific key from runtime ECDH public-key material and HKDF-SHA256 and protects only the sensitive body (`Properties`, interaction value and workflow) with AES-GCM. Public routing metadata remains visible so bounded dispatch and diagnostics remain possible. The protected envelope is also signed with ECDSA. Payloads remain subject to the protocol's 8 MiB ceiling; large media should use bounded data URLs only for small evidence or a separately approved content reference.

## Custom add-ons and ESP32 gateways

The JSON envelope is transport-neutral. A user-built add-on can implement TCP, HTTP/JSON, MQTT, UART, SPI or another adapter while retaining the same IDs, capability keys, approvals and work-status lifecycle.

LocalGPT profile:

```text
GET /api/onewire/http-json/profile
POST /api/onewire/http-json
GET /api/onewire/http-json/work/{correlationId}
```

PublisherStudio profile:

```text
GET /api/organic/onewire/http-json/profile
POST /api/organic/onewire/http-json
GET /api/organic/onewire/http-json/work/{correlationId}
```

A small ESP32 should normally use an HTTPS-capable gateway or its own TLS-enabled HTTP client/server, hardcode only the public protocol schema and capability identifiers, create its device secret at first boot, and keep the private identity in protected flash/NVS rather than firmware source. It may start with unsigned/untrusted discovery, but sensitive calls must remain disabled until the human establishes trust and grants the relevant frontend permission.

Minimal flow:

1. Read `profile` and verify protocol compatibility.
2. Advertise or import the device's public pairing ticket.
3. Complete explicit MFA/trust setup in the responsible frontend.
4. POST one sealed `Invoke` envelope.
5. Handle `ApprovalRequired` or `WorkAccepted` without retrying under a new correlation ID.
6. Poll the work URL with the same `CorrelationId` until `WorkResult`, `Declined`, `Failed` or `Cancelled`.

## Human interaction guarantees

- Text review and approved HTML/DIV/document content use the exact request correlation ID.
- Screenshot and screen recording always require a fresh LocalGPT decision, a fresh PublisherStudio decision and the browser's current `getDisplayMedia` prompt. A saved permission rule cannot bypass these prompts.
- OCR requests from Picture Studio require the PublisherStudio user action and LocalGPT approval. Recognized text is reviewed before insertion as an editable layer.
- Discovery only announces identity and endpoints. It never grants trust, permission or execution authority.
