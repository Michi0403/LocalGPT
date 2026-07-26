# LocalGPT 2.0.0 organic suite debug source candidate

## Purpose

This source candidate synchronizes LocalGPT and PublisherStudio at major version 2 and makes LocalGPT the sole source owner of `LocalGPT.WireProtocolVersion`. It is intended for maintainer compilation and runtime debugging; no native .NET/DevExpress build is claimed from this environment.

## Compiler and component fixes

- Fixed the `PublicServiceMethodInvoker` nested-type/member collision by renaming the request DTO to `InvocationParameters` while preserving its JSON `parameters` property.
- Kept the Council self-assessment prompt on a non-interpolated raw string with explicit placeholder replacement, preventing JSON braces from becoming C# interpolation expressions.
- Restored the shared `OneWireModelSelfAssessment` contract in the authoritative protocol project.
- Corrected the new time/state DX function to project the real `OneWireHardwareDescriptor` properties.
- Every maintained Razor page has a typed `ILogger<T>`, `INotificationService`, and `IComponentActivityService` injection.

## Versioned 1-Wire package authority

- LocalGPT owns the only maintained protocol source project at `LocalGPTWebviewWrapper/LocalGPT.WireProtocolVersion`.
- Protocol compatibility is version `2.0`; assembly/package version is `2.0.0`.
- The protocol project is packable and produces `LocalGPT.WireProtocolVersion.2.0.0.nupkg`.
- LocalGPT publish copies the matching package into a `protocol` release folder.
- The package contract remains transport-neutral for TCP now and later UART/SPI/MQTT adapters.

## Council readiness, introductions and runtime awareness

- Added an editable, database-backed member-readiness/introduction workflow step.
- Each member introduction can use model, team and member-list placeholders and is prepared before expert and main Council rounds.
- Council preflight verifies per-model hardware roads, min/max output and context ranges, approved DX functions, organic skills, linked 1-Wire organs, project/regex/knowledge/debug evidence and missing user requirements.
- Added the read-only `localgpt.time_state.now` DX function with current UTC/local time, process state, the three newest logs, three newest Council spool snapshots, linked peers and configured hardware roads.
- Runtime DX functions and organic capabilities continue to synchronize into the database catalog at startup.

## Database continuity

- Existing LocalGPT Core rows from known older seed versions are upgraded losslessly to version 2.0.0.
- LocalGPT Core and Humanitarian Collaboration Workspace remain permanent seeded projects.
- Added seed requirements and artifacts for the authoritative 1-Wire package, frontend-confirmed organic transactions, PublisherStudio Story Editor workflow, time/state awareness, portable PDB metadata and transaction-correlation regexes.
- User-edited Council teams are not overwritten; only missing required built-in steps are merged.

## User-authoritative organic execution

- Public service methods remain discoverable in the database-backed catalog and may be exposed to AI Chat or linked applications by the local frontend user.
- Exact stored service/method signatures are resolved and parameters are bound to their declared types.
- Consequential generic service invocation requires LocalGPT frontend confirmation.
- A transport connection does not grant authority; PublisherStudio pairing is completed only after LocalGPT frontend approval.
- Human input and confirmation responses are correlated to the exact waiting 1-Wire transaction.

## Validation performed here

- LocalGPT organic-wiring source contract: passed.
- Council spooler/shared protocol/source closure contract: passed.
- Frontend-authority and public-service catalog contract: passed.
- Version 2 package authority, introduction, database feed, time/state and Razor diagnostics contract: passed.
- JSON/XML/project-reference/archive checks are recorded in the delivery verification report.

## Build truth

A Windows .NET 10/DevExpress compiler and runtime were not available in this workspace. The package is therefore a **debug source candidate**, not a compiler-verified release.
