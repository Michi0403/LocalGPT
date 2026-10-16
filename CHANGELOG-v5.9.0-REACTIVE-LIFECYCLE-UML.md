# LocalGPT 5.9.0 — reactive lifecycle and UML business-process equivalence

## Intent

Distinguish **business-process identity** from **method signature identity** and **Blazor renderer phase**. The same C# lifecycle callback can be executed for different component instances, with different safe side effects. The repository documents that distinction using UML activity/process, state-machine, sequence and class diagrams.

## Changes

- Added `docs/architecture/reactive-lifecycle-and-process-equivalence.md` with four Mermaid/UML diagrams, a six-axis service/process equivalence decision table, and concrete LocalGPT lifecycle contracts. Registered the chapter in the DocFX architecture TOC and linked it from the architecture home, frontend/theming, and Council-runtime pages.
- Moved `Chat` Council live service subscriptions from `OnInitializedAsync` (also executed during prerender) to the first interactive `OnAfterRenderAsync`. Attachment is idempotent and partially attached handlers are rolled back if an event subscription throws.
- Unsubscribes only for an instance that actually attached. Prerender-only `Dispose` no longer saves its transient defaults to Council preparation storage or mutates the circuit presentation state. An interactive instance persists preparation only after initial state is ready.
- Extended the *existing PowerShell* `Assert-InteractiveServerRenderModes.ps1` build guard with lifecycle boundary checks and documentation registration checks. No Python or new mandatory build tool was added.
- Updated LocalGPT version 5.8.9 → 5.9.0 (single-digit release numbering).

## Scope and compatibility

No Council execution service was merged or converted to a singleton. Existing Game, Learning, General, and persisted Council definitions are unchanged; this repair concerns when their **Blazor UI adapter** attaches to their events. Existing render-mode declarations and other PowerShell architecture guards were preserved. PDFsharp 6.2.4 MIT and PublisherStudio remain untouched.

## Verification still required on Windows

A full .NET / DevExpress compilation and PowerShell audit run were not executed in the packaging environment. Test prerender response, circuit activation, visible Council progress, navigation/disposal, reconnect, settings persistence, and DocFX Mermaid diagrams in generated HTML/PDF. See `VALIDATION-v5.9.0-source.md`.
