# LocalGPT v0.1.8 — workspace proof and frontend authority

## Build and source closure

- Retains every maintained file from the v0.1.7 Council-spooler workspace; the generated preservation manifest records hashes for every baseline file and reports zero missing baseline paths.
- Keeps `OneWireModelSelfAssessment` in the shared `LocalGPT.WireProtocolVersion` project and the LocalGPT interfaces/services reference that shared type.
- Keeps all `System.Threading.Volatile` calls explicitly qualified and preserves installer/bootstrap port compatibility.
- Removes raw/interpolated JSON prompt hazards through the previously completed non-interpolated Council prompt construction.
- Updates LocalGPT application, wrapper, installer, runtime advertisement, permanent project feed, and custom version to 0.1.8.

## User-controlled DX function and service catalog

- Adds a database-backed catalog that synchronizes registered `IDxAiFunctionHandler` functions and public methods on registered LocalGPT services at boot.
- Preserves user policy while descriptor metadata is refreshed.
- Adds typed public-service invocation by exact stored catalog key, implementation, method name, and parameter type signature.
- Adds `/api/dxai/catalog` synchronization/policy endpoints and the `/dx-functions` LocalGPT page.
- The frontend user can independently control enabled state, AI-chat visibility, linked-application visibility, linked invocation, mandatory receiving-frontend confirmation, interaction editor, and allowed peer IDs.
- Public service methods are discoverable without becoming automatically invokable. Enabling them is a deliberate frontend policy choice, not a hard-coded limitation.

## Frontend-authoritative 1-Wire linking and interaction

- Shared wire contract remains an embedded reusable C# project and advances to protocol 1.6 / assembly 1.6.0.
- Adds exposure/invocation/confirmation/editor/configuration policy fields to shared capability and permission contracts.
- Adds bidirectional plain-text, rich-text, JSON, and confirmation-only interaction editor declarations.
- A PublisherStudio TCP connection no longer becomes a trusted link merely because bytes arrive. PublisherStudio initiates from its frontend; LocalGPT creates an exact link approval in the Human Collaboration Inbox; only the approved link receives capabilities and may exchange work.
- Non-Hello wire traffic from an unapproved peer is rejected.
- Sender-side `UserConfirmed` never substitutes for target-side authorization.
- The exact LocalGPT human response is copied back to the waiting envelope and correlation ID before the spooler resumes.
- Human input capacity is raised to 1,000,000 characters for offline rich/structured interaction instead of the former 4,000-character UI/service limit.

## Council, projects, knowledge, regex, and hardware

- Retains database-backed General, OpenSCAD, Spreadsheet, and Learning Round teams and editable Council phase/member instructions.
- Retains mandatory database/project/regex/DX/organ/hardware preflight, regex/subject expert preparation, leader synthesis, organic member round, and next-heartbeat evidence flow.
- Retains rejoinable Council spooler state and model-specific CPU/GPU/accelerator roads with 0–100% sliders in 5% steps and per-model min/max interpolation.
- Updates lossless permanent-project seeding with v0.1.8 version/revision records while preserving user-maintained values and all earlier revision history.
- Retains the permanent `LocalGPT Core` and `Humanitarian Collaboration Workspace` projects, runtime DX/skill directory synchronization, regex seeds, learning functions, repository/debug metadata inspection, and ordinary-chat access to Council maintenance capabilities according to catalog policy.

## Validation performed in this package

- LocalGPT installer/bootstrap and organic source contracts.
- Council spooler, shared protocol, permanent project and source-closure contracts.
- Frontend-authoritative link, function catalog, public-service discovery/invocation, interaction editor and large-input source contracts.
- XML/JSON parsing, project-reference closure, conflict-marker scan, shared protocol byte equality, and archive/source hash equality.
- Workspace preservation verification against the v0.1.7 source baseline.

A native .NET/WinUI/DevExpress compilation was not available in the packaging environment and is not claimed.
