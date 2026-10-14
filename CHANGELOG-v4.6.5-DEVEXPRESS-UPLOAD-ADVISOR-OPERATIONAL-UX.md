# LocalGPT 4.6.5 — DevExpress upload advisor and operational UX

## DevExpress-first upload experience

- Added a streamed `DxFileInput` upload path that writes selected files directly into the bounded quarantine workspace instead of treating file selection as a browser-memory attachment feature.
- Added a DevExpress upload/review overlay using `DxPopup`, `DxGridLayout`, `DxFormLayout`, `DxAccordion`, `DxButton`, and the shared operation-status component.
- Replaced the remaining setup/import native file inputs with `DxFileInput`.

## Data-domain-driven processing recommendations

- Added curated `project.evidence::*` regex seed records for source-code ecosystems, Minecraft/game projects, documentation, structured data, images, audio/video, archives, engineering/CAD designs, and embedded projects.
- The evidence classifier reads approved regex data rather than using a hard-coded file-extension decision tree for project/data-domain identity.
- Added an upload-processing recommendation service that combines deterministic quarantine evidence, user-approved Knowledge Database entries, and real persisted Council team definitions.
- When a configured AI is available, it can refine the recommendation, but any suggested Council team is validated against the persisted team registry.
- Recommendations can be handed to normal Chat or a suggested Council team, refreshed, or sent through independent Council review. They cannot bypass quarantine, deterministic checks, review counts, user approval, or the existing promotion gate.
- Added controller and DXFunction access for processing recommendations so the same capability is available outside the visual upload overlay.

## Waiting and operational feedback

- Added a reusable `DxWaitIndicator` operation status strip and placed it near long-running setup actions such as hardware detection, CanIRun.ai lookup, provider work, model resolution/pulls/checks, and benchmark-team setup.
- Added a shared operational activity feed combining route-local setup status, bounded application activity, and the shared command/ASCII console.
- `/install` Setup Log and the first-run setup assistant now expose real ongoing activity rather than a disconnected route-only text buffer.

## Numeric editing

- Restored the invariant numeric helper to a real DevExpress `DxSpinEdit`.
- Uses `DxNumericMaskProperties` with invariant culture so RAM/VRAM and similar decimal values remain stable across localized UI cultures without using sliders or plain text boxes.

## Preserved repairs

- Keeps the 4.6.4 XML-documentation warning cleanup.
- Keeps the 4.6.3 `/install`, CanIRun.ai and editor/layout repairs.
- Keeps the 4.6.2 quarantine/ingestion, reviewed regex, generic toolchain, 1-Wire and semantic ASCII automation work.
- Keeps the 4.6.1 singleton/scoped service-lifetime repair.

PublisherStudio was not changed.
