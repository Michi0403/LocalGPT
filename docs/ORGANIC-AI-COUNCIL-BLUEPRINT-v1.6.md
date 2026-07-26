# Organic AI Council and 1-Wire blueprint — protocol 1.6

## User authority

LocalGPT and PublisherStudio are offline-first applications operated by a person at the frontend. A TCP connection, UDP discovery packet, model response, reflected service method, scheduler item, or sender-side `UserConfirmed` value is **not** local authorization.

The supported authority chain is:

1. PublisherStudio discovers LocalGPT but does not connect automatically.
2. The PublisherStudio user selects **Connect**.
3. LocalGPT keeps the transport unlinked and places the exact link request in its global Human Collaboration Inbox.
4. The LocalGPT frontend user approves or declines that link.
5. Each receiving application applies its own database/file-backed capability policy.
6. When the receiving policy requires confirmation or information, its own frontend presents the exact request and optional plain-text, rich-text, or JSON editor.
7. Only the approved envelope continues through the spooler. The entered value is returned on that envelope and correlation ID.

Saved policies such as `SameCapability`, `CurrentWorkOrder`, and `AlwaysAllow` are themselves explicit frontend choices. They do not replace the user as authority; they are the user's stored instructions.

## Configurable callable surface

LocalGPT synchronizes two kinds of callable entries into the database-backed DX Function Catalog:

- registered `IDxAiFunctionHandler` functions;
- discoverable public methods on registered LocalGPT service implementations.

The LocalGPT frontend controls, per entry:

- enabled state;
- visibility to ordinary AI chat;
- visibility to a linked 1-Wire application;
- permission for linked invocation;
- mandatory receiving-frontend confirmation;
- the receiving editor (`ConfirmationOnly`, `PlainText`, `RichText`, or `Json`);
- an optional allow-list of linked peer IDs.

Reflection is used for discovery and typed parameter binding, not as an authorization bypass. The selected catalog key resolves to one stored implementation/method signature. The receiver still checks link state, exposure, invocation policy, and frontend approval.

PublisherStudio applies the same principle to its organic capabilities. Its Organic Plugins view configures exposure, invocation, link requirement, confirmation mode, work-order scope, organ, and requested editor per LocalGPT peer and capability.

## Council heartbeat

A Council heartbeat is prepared and executed in these phases:

1. **Database and project preflight** — verify project, revision, branch, files, compiler/scientific knowledge, regex links, debug evidence, DX functions, organic organs, and model hardware routes. Missing facts become explicit user questions instead of invented facts.
2. **Regex and subject expert preparation** — project/file regex experts and domain experts collect bounded, directly linked evidence from project files, conversations, knowledge, logs, changelogs, compiler references, and debug metadata.
3. **Leader synthesis** — the selected leader translates the request from current state to desired state using the repository's actual domain/service/controller architecture and an UML-compatible activity/sequence plan.
4. **Organic main round** — every member contributes according to role, demonstrated strengths, available skills, hardware lane, and callable functions. A new member introduces itself and submits untrusted self-assessment evidence for later user review.
5. **Required organ interaction** — eye, hand, media, text-editor, or other external work is requested through 1-Wire. If the target frontend requires input or confirmation, the exact work pauses and resumes after that frontend responds.
6. **Heartbeat continuation and review** — returned evidence is added to the next heartbeat. Consequential project changes remain reviewable by the user.

## Hardware roads and spooler

Each model has its own CPU/GPU/accelerator support and minimum/maximum settings. Session sliders use 0–100% in 5% steps. A 30% session value means `model minimum + 30% of that model's own range`, not one shared absolute value.

Council and organic work is registered in spoolers with correlation IDs, status, scheduling metadata, sequential work-order gates, and rejoinable UI snapshots. Closing a browser circuit must not silently discard a running Council run. A full process/OS interruption can restore checkpoints, but cannot resurrect an inference call that was executing inside a terminated process; such a call must be marked interrupted and retried deliberately.

## Project and knowledge maintenance

Old databases are upgraded without replacing user-edited values. At minimum they receive the permanent **LocalGPT Core** and **Humanitarian Collaboration Workspace** projects plus missing system seeds.

Project context can reference installer information, compiler/toolchain facts, commands, knowledge entries, project and revision file regexes, debug artifacts, build status, last Council activity, and required external organs.

The regex database is a maintained BusinessObject domain, not a new static substitute. Built-in patterns are seeded idempotently, AI-proposed patterns remain reviewable, and regex/DX maintenance functions are made available to ordinary chat and Council according to the same catalog policy.

## Transport contract

The shared `LocalGPT.WireProtocolVersion` project is embedded in the LocalGPT repository and referenced by both applications. The envelope supports one-time, sequential, scheduled, and recurring work; capability/skill/UI/hardware negotiation; human and automated target interactions; a serialized interaction value; integrity hash and error check; nullable signature/encrypted-payload placeholders; and adapter interfaces intended for later TCP, UART, SPI, and MQTT transports.

Current working transport is TCP with UDP discovery. Production cryptographic identity, signing, encryption, and UART/SPI/MQTT adapters remain explicit future work and are not represented as complete.
