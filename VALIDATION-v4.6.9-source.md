# LocalGPT 4.6.9 source validation

GitHub was not used and `dotnet`, restore, build, MSBuild, NuGet or publish were not invoked in this environment.

LocalGPT 4.6.9 is a source-level repair of the Local Chat workspace upload interaction. The persistent Upload advisor page panel is removed; external desktop file drag is the only trigger for the workspace landing overlay.

## Required interaction contract

- `/chat` contains the existing invisible `UploadProcessingAdvisor` bridge but no idle upload panel, upload grid, goal editor, `DxFileInput` or Select File control from that advisor.
- A temporary browser overlay is created only for an external file drag intersecting the Local Chat surface and is removed on drag leave, drop or component disposal.
- Dropped files are posted as multipart streams to the bounded quarantine endpoint; the endpoint enforces configured file-count, single-file and total-byte limits and delegates persistence to `IChatUploadWorkspaceService`.
- Existing project-ingestion inspection and recommendation execute after quarantine. The review popup is post-drop UI, not permanent Chat chrome.
- Promotion remains behind deterministic ingestion checks, independent-review quorum and explicit user approval.
- The normal Chat attachment/paperclip path remains separate.

## Preserved regression boundaries

- 4.6.8 exact Kernel Creature Tournament session correlation and high-resolution tournament frame contract;
- shared ASCII/Pixel game presentation over one deterministic game state;
- bounded same-run Council rejoin and Operator-to-Game return;
- non-fullscreen ASCII terminal scroll/layout repairs;
- InteractiveServer route ownership;
- DevExpress-first ordinary Razor controls and the existing GridLayout template guard.

## Static validation

The final source archive is validated without .NET using the maintained Python architecture/release audits, JavaScript syntax and diagnostics-hash checks, JSON parsing, XML/MSBuild parsing, ZIP CRC/path-safety checks, clean extraction and byte-for-byte source comparison. Runtime compilation and browser interaction confirmation remain for the user's .NET 10/DevExpress environments.
