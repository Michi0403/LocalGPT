# LocalGPT 2.1.14 — Embedded workbench contracts and workspace environments

## Chat-first ESP32 and Arduino planning

- Adds an `embedded-firmware-wiring` Council preset for bounded GPIO, sensor, electrical, firmware and telemetry planning.
- Adds source-controlled board/pin profiles for a classic ESP32 review profile, an Arduino Uno R3 review profile and an intentionally blocked generic ESP32 family placeholder.
- Adds transport-neutral protocol descriptors for digital GPIO, ADC, PWM, physical 1-Wire, I²C, SPI, UART, CAN, RS-485, serial JSON lines, HTTP JSON, MQTT, LocalGPT logical 1-Wire, organic peers and custom protocols.
- Adds deterministic wiring-graph validation for pin ownership, board-profile matching, voltage, output-to-output connections, ground paths, shared buses, boot straps and reserved/input-only pins.
- Generates a deliberately small Arduino sketch, `platformio.ini`, wiring review, transport contracts, plan JSON and an optional wiring-canvas draft. Artifact creation is approval-gated and still does not compile or flash.
- Advises a follow-up learning round using the exact board revision, measured calibration ranges, physical addresses, captured telemetry, compiler versions and boot/reset evidence.

## Telemetry and logical 1-Wire boundary

- Embedded firmware emits a bounded `localgpt.embedded.telemetry.v1` edge packet. It does not contain LocalGPT trust secrets.
- A trusted local gateway can validate that packet and map it to the `embedded.sensor.telemetry.publish` DXFunction through the existing linked, authenticated and replay-protected logical 1-Wire execution path.
- Adds bounded in-memory recent telemetry for Chat/debug inspection. Raw telemetry is not silently written to the Council knowledge database.
- Untrusted edge ingress records device sequence values for diagnostics but deliberately leaves reboot-safe replay enforcement to the authenticated gateway or protected LocalGPT peer.
- Physical Dallas/Maxim 1-Wire remains one optional sensor bus and is explicitly separated from LocalGPT's logical 1-Wire protocol.

## Service, controller, DXFunction and organic contracts

- Adds embedded board catalog, wiring, planning, artifact, telemetry bridge and telemetry ingress services.
- Adds `/api/embedded` controller methods for catalogs, boards, protocols, PublisherStudio workbench contracts, wiring drafts/validation, firmware planning/artifacts and telemetry preview/ingress.
- Adds Chat-visible DXFunctions for the embedded catalog, wiring draft/validation, firmware plan/artifacts, edge telemetry preview, logical 1-Wire envelope preview and telemetry publish.
- Adds a PublisherStudio organic request capability for a future clickable pin/wiring editor. The shared draft already carries canvas coordinates, OpenSCAD part keys, wire styles and signal-animation keys.
- Extends the PublisherStudio source-controlled organic manifest with future wiring validation, signal animation and OpenSCAD part-preview capability/controller descriptors.

## Installer learning sources and parser

- Extends the recommended source list with official Arduino documentation/API/CLI repositories, official Espressif Arduino/ESP-IDF repositories and PlatformIO Core.
- Writes an explicit bounded `localgpt-learning-source.json` only for embedded documentation/toolchain repositories.
- Parses approved documentation, Arduino sketches, C/C++ headers/sources and embedded build configuration through include/exclude regexes into compact source maps and representative signatures instead of copying full repositories into knowledge entries.

## Workspace environments and compiler safety

- Extends every project workspace with environment kind/root, preferred compiler, build arguments, environment variables, expected subdirectories, expected-structure regex and Council-maintainable access-policy regex rules.
- Adds approved/warning/danger permission assessment for missing or overly broad roots, read/write availability, path escapes, structure mismatches, expected entries and compiler state.
- Adds PlatformIO and Arduino CLI discovery/version probes and embedded source/build-file classification.
- Build verification can inherit the workspace compiler, arguments and environment, and now refuses execution when the workspace has not been assessed, retains danger findings, or lacks persisted proof of both read and write access. Editing a workspace resets the assessment so stale permission results cannot authorize a later build.
- Adds Razor controls and badges directly to Project Maintenance rather than introducing a disconnected second workbench model.

## Legacy-source boundary

The supplied legacy Tasmota, Raspberry, Python.NET, simulation and workbench projects were used only to understand configuration, message, build and visual-wiring flows. Vendored Fritzing/Qt/Boost/libgit2 content, binaries, generated output and legacy application code are not copied into LocalGPT.

## Versioning

- LocalGPT application/runtime/organic advertisement: `2.1.14`.
- Council team seed version: `15`.
- The separately versioned LocalGPT logical 1-Wire protocol remains `2.1`.
