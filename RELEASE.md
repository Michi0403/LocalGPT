# LocalGPT 3.2.3

LocalGPT 3.2.3 is the **User AI Functions, X Automation & Source-Backed Learning Projects** release. It builds forward from 3.2.2 without changing the working chat composer or its CSS.

## Toolchain state

- .NET SDK policy: `10.0.400`
- Application target framework: `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Main changes

- The existing user DX/AI-function editor now has a simple JSON/OData source mode backed by the existing Remote Control connector and pipeline records, while the advanced pipeline mode remains available.
- The DX Functions page links directly to the existing X-Round automation controls; Council Teams exposes those controls as **X Functions & automation** without creating a second execution engine.
- Learning maintenance now synchronizes repository-shaped source from the active chat upload workspace into the existing project/version/revision/workspace-root/tracked-file model. Project identity, exact source version, `global.json` SDK, target frameworks, source snapshot hash and full file structure become auditable database state.
- Repository-derived runtime requirements supersede stale repository metadata so a .NET 10 source tree cannot retain an older repository-derived framework claim as current. Council project briefings are instructed to use source-backed requirements instead of inventing .NET 7/8 questions.
- The reported nullable X-Round recovery handoff warning is normalized with the existing empty-string representation.
- The 3.2.2 long-cycle repetition watchdog, provider-qualified Ollama recovery and rejoin Copy fixes remain intact.

## Protected UI and persistence

`Chat.razor` and `Chat.razor.css` remain byte-identical to the 3.2.2 baseline. No EF migration was added; project learning uses the already-existing project persistence model.

## Validation boundary

This source package was not compiled with .NET/DevExpress in the preparation environment. Validation is source/static only. See `CHANGELOG-v3.2.3-AI-FUNCTIONS-X-AUTOMATION-LEARNING-PROJECT-SYNC.md` and `VALIDATION-v3.2.3-source.md`.
