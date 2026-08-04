# LocalGPT AI Council and PublisherStudio organic 1-Wire blueprint — implementation review 2.1

This document separates the implemented source contract from longer-term transport and hardware plans. The term **organic 1-Wire** means this project's transport-neutral application protocol; it is not limited to the Dallas/Maxim physical 1-Wire bus.

## 1. Project knowledge and revision context

**Implemented:** `ProjectOrganicContext` stores installer state/path, compiler identifiers, system commands, knowledge references, project/file regex references, debug paths, nullable build success, last Council activity, required organic capabilities and external organ plugins. The context is user-confirmed and stored as a project artifact. Runtime capability persistence is a derived searchable cache; failure to refresh it no longer terminates the Council because the live DI registry remains authoritative.

**Prepared, not complete:** richer per-file revision entities and automated compiler/MS Docs ingestion can be added without changing the wire protocol. Current regex and knowledge tables already provide the base, but not every field in the conceptual blueprint is a dedicated SQL column.

## 2. Generated .NET organ-plugin architecture contract

**Implemented as Council guidance and review contracts:** generated integrations are expected to use interface-first services, dependency injection, bounded lifetimes, structured logging, explicit catch/log boundaries, user-reviewed source and no automatic installation/execution. Logging removal is explicitly rejected as cleanup.

**Prepared:** a future generator template can materialize the exact Backend/BusinessObject/DomainDom folder and namespace convention for every external organ project. The current system enforces principles and review rather than one mandatory physical solution layout for all project types.

## 3. Organic 1-Wire protocol and transports

**Implemented:** one RID-neutral NuGet protocol authority in LocalGPT; PublisherStudio consumes the package only. Envelopes support correlation IDs, reply IDs, execution modes, work-order keys, capability/skill/organ metadata, user confirmation, target interaction requirements, integrity hash/CRC, signatures, encrypted payload capsule and bounded messages. TCP and HTTP/JSON adapters use the same envelope and approval lifecycle. Transport enum values reserve MQTT, UART, SPI and custom adapters.

**Custom add-on flow:** an ESP32 or gateway can read the HTTP profile, implement the JSON envelope, publish a bounded capability directory, preserve one correlation ID through approval and polling, and remain unavailable for sensitive work until the user grants trust and permissions. UART/SPI/MQTT framing and device-specific certificate storage are adapter responsibilities, not hardcoded into the shared DTO.

## 4. Runtime identity, MFA and encryption

**Implemented source contract:** each application creates a random secret file at runtime only. The user can create, rotate or delete it from the frontend; doing so resets trust. A short-lived signed public pairing ticket and an Authenticator-compatible TOTP URI can be generated explicitly. PublisherStudio renders both as QR codes. Imported public tickets are verified, local MFA is checked, and trust validity is stored per peer. Reciprocal trust enables ECDH + HKDF-SHA256 key derivation, AES-GCM protection of sensitive body fields and ECDSA signatures.

**Important boundary:** source contracts and static tests are complete here, but an actual two-process encrypted exchange and Authenticator scan must still be runtime-tested on the maintainer's .NET machines. The design does not claim that a secret file alone replaces TLS for an Internet-exposed endpoint. HTTP adapters should normally be served through HTTPS, especially for an ESP32 gateway.

## 5. Council teaching and role routing

**Implemented:** every Council preflight synchronizes DX functions and organic skills, reads connected peer capabilities, and teaches all members the exact capability key, input contract, output contract, security contract, organic use case and suggested roles. Members are told to preserve correlation IDs, wait through `ApprovalRequired`, and continue only from the matching result. OCR capabilities recommend OCR/vision members such as a configured DeepSeek OCR model.

## 6. Human-reviewed text round trip

**Implemented source flow:** LocalGPT's dedicated `publisher.text.feedback.request` DX function requires current LocalGPT approval. PublisherStudio receives `publisher.text.edit.request`, applies its own permission/approval policy, opens the bounded text editor, lets the user edit, saves through the same correlation, closes the editor and returns a work result. LocalGPT's spooler updates the exact queued tool result for the active Council/chat context.

The returned text is not forcibly typed into the chat input while streaming. It becomes the result of the current tool call, which is safer and still lets the Council continue with the user's exact response.

## 7. Screenshot and screen recording

**Implemented source flow:** dedicated LocalGPT functions request PublisherStudio capture/recording. These two capabilities are permanently non-persistable: even an AlwaysAllow permission is overridden to `AskEveryTime`. A current LocalGPT decision, a current PublisherStudio approval and the browser's fresh `getDisplayMedia` prompt are required for every request. Recordings are bounded to 15 seconds and protocol size limits.

## 8. Picture Studio OCR

**Implemented source flow:** the LocalGPT AI ribbon appears only when the connected peer advertises `localgpt.vision.ocr`. Picture Studio renders the current canvas, sends the bounded image through the same correlation, waits through LocalGPT approval, calls a configured local Ollama-compatible OCR/vision model, shows the recognized text for review and can insert it as an editable text layer. No server file path is accepted.

## 9. Approved HTML/DIV/document content

**Implemented source flow:** LocalGPT can request bounded content through `publisher.website.content.request`. PublisherStudio returns only content reviewed in its frontend, with format/source metadata and truncation state. It does not automatically fetch arbitrary URLs. The result is generic wire content and can later be handed to another compatible organic add-on without depending on PublisherStudio's internal classes.

## 10. Discovery, capability buttons and default settings

**Implemented:** LocalGPT broadcasts a compact advertisement every five seconds to the standard broadcast address and additionally sends a loopback beacon. PublisherStudio listens on the same default discovery port. Full capability data moves over the connection rather than UDP. UI state is event-driven from discovery/connection changes; management refresh is bounded and not a one-second button poll. AI/OCR controls appear only when the relevant negotiated capability is available. Discovery never equals trust or permission.

## 11. Council heartbeat

1. User request or PublisherStudio Council prompt.
2. Mandatory member readiness and connected-capability teaching.
3. Expert preparation and evidence/regex/project inspection.
4. Leader synthesis and bounded workflow plan.
5. Main Council round with role-appropriate DX/organic functions.
6. Target-side permission and, when required, browser interaction.
7. Same-correlation work result returned into the next Council heartbeat.
8. Final human review/authorization before consequential completion.

## 12. Current verification boundary

Static source contracts verify project/package authority, deterministic build ordering, runtime-secret lifecycle, correlation ownership, capture consent, OCR wiring, HTTP adapters, capability teaching, logging integrity and PublisherStudio UI contracts. The maintainer still needs to perform the actual .NET builds, two-application runtime pairing, browser capture, Authenticator and hardware/gateway tests. Those runtime results should become release evidence rather than being inferred from source alone.
