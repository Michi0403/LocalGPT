# LocalGPT v0.1.4 organic-wire runtime hotfix 2

## Compatibility contracts preserved

- Preserved the installer/bootstrap application port contract: `Program.DefaultPort` remains `5000`, `Program.Port` remains public and read-only, the positional installer argument remains supported, and organic TCP/UDP ports remain separate.
- Kept every `Volatile.Read`/`Volatile.Write` reference explicitly qualified as `System.Threading.Volatile` to avoid the `DevExpress.CodeParser.Volatile` ambiguity.
- Kept the WinUI wrapper and installer wiring on `Program.BaseUrl`/`Program.Port`; organic-plugin failure remains optional and may not terminate the desktop bootstrap.

## Fixed in this run

- Fixed Council/chat autosave failures caused by clearing a tracked required `ChatMemoryConversation.Messages` relationship. Conversation snapshots now replace message rows atomically inside one transaction, preserve matching user feedback, use explicit `ConversationId` values and never create a conceptual-null relationship.
- Fixed theme-switch callbacks that could run outside the Blazor renderer dispatcher. Theme state and event callbacks now execute through `InvokeAsync`; the component no longer uses `ConfigureAwait(false)` in renderer-owned callbacks.
- Added a persistent bottom approval work bar for pending AI Council actions. It opens the existing Human Collaboration Inbox, can be hidden without losing the Human team launcher, and uses Bootstrap/DevExpress theme variables.
- Added wrapped DevExpress grid cells and enabled text wrapping in the database table list, knowledge grid and table preview so long prompts, paths and serialized values do not expand the entire layout.
- Improved native HTML button, checkbox/radio and suggestion-button theme integration without overriding DevExpress-owned selectors.
- Synchronized the EF model snapshot with hardware routes, organic skills, project/member skill links and editable Council team configurations.
- Strengthened lossless migration safeguards for partially introduced organic-skill and Council-team schemas. Existing compatibility backup, additive column repair, malformed-table archival and migration-history adoption are now covered by source-contract checks.

## 1-Wire and Council architecture included

- The authoritative `LocalGPT.WireProtocolVersion` project remains inside this repository and builds as a reusable DLL. PublisherStudio receives a synchronized source mirror only so it can build offline without a second checkout.
- Protocol v1.3 includes bidirectional target-system interaction requirements, serialized interaction value/content type, capability/skill/UI state exchange, hardware descriptions and routes, min/max token limits, scheduling/recurrence metadata, hashes/error checks and transport-neutral interfaces.
- Ordinary LocalGPT chat can request bounded spreadsheet evidence through `publisher.spreadsheet.inspect`; it does not require a Council run.
- Organic skills remain database-maintainable and linkable to projects and Council members, including self-revealed DX functions, controller methods, capabilities and proficiency evidence.
- Council teams, roles, expert preparation, leader synthesis, main-round instructions and workflow steps are preseeded losslessly, selectable and editable in the database-backed Council Team configuration.
- General corrections entered during a running Council are queued as information for the next heartbeat instead of cancelling the active generation.
- CPU/GPU/accelerator roads, per-model token bounds and bounded parallel hardware-road participation remain available to Council scheduling.

## New safeguards

- Added `build/Assert-ProjectClosure.ps1` to validate every project reference, the shared protocol assembly, required architecture sources and the protected application-port contract before validation or packaging.
- Extended EF snapshot, migration bootstrap, theme and human-collaboration checks for the regressions fixed above.
- The verified packaging path now runs source-closure checks before creating an archive.

## Validation performed here

- Source inventory, project-reference closure, XML/JSON parsing, shared-protocol mirror equality, protected-file SHA-256 manifest and targeted regression-contract checks passed in the delivery workspace.
- A native .NET 10/WinUI/DevExpress compile was intentionally not claimed in this environment. The owner workstation remains the compiler authority for the returned source ZIP.

## Missing features / next bounded run

See `MISSING_FEATURES-v0.1.4-organic-wire-runtime-hotfix-2.md`. Deferred work is recorded there rather than represented as completed.
