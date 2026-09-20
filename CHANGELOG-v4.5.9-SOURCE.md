# LocalGPT 4.5.9 — Council recovery, knowledge freshness, tournament teams and DevExpress overlay repair

## Changed

- Completed the Kernel Creature Tournament team-state work: each trainer owns a database-configurable creature roster (default 3), may initiate a bounded number of switches per fight (default 3), reserve creatures recover while resting, and deterministic FOCUS/BRACE/REST/SWITCH trainer actions are resolved separately from creature moves.
- Replaced pose-only tournament art with stable joint-anchored trainer and creature rigs, preserving recognizable connected actors across command, movement, clash, impact, recovery and victory frames while retaining detailed text in the same ASCII movie.
- Restored a bounded engine-owned trainer/creature timeline to the scoreboard and ASCII presentation so team state remains visible between exchanges and switches.
- Added stale-knowledge reporting (`localgpt.knowledge.freshness.report`) and exact-source refresh (`localgpt.knowledge.source.refresh`). Learning Round can report an actually used stale knowledge ID automatically; keep/review/refresh/reject choices surface through the existing Chat, ASCII and Approvals & team collaboration UI.
- Exact external source refresh is approval-bound to the complete knowledge ID and URL parameters. Local/private-network sources remain on LocalGPT's existing local/LAN path rather than being routed through the public remote importer.
- Added stable knowledge IDs to knowledge briefings so Council members can report the exact persisted entry that proved stale.
- Added bounded Chat Council rejoin retries while the server-owned run is still active; failed browser attachment no longer implies that the server run stopped.
- Converted the Chat provider/settings row to a one-column `DxFormLayout` while retaining the existing typed `DxComboBox`, `DxCheckBox` controls and callbacks.
- Repaired desktop DevExpress combo/list overlays by selectively elevating only body-level `dxbl-popup-cell` hosts that contain actual dropdown list dialogs; modal/fullscreen popup stacking is not globally changed.
- Bumped Council runtime-class seed to 8 and Council team seed to 36. The copyable tournament rules now persist roster size, switch cap, reserve recovery and trainer-action tuning.

## Preserved

- InteractiveServer render-mode topology remains unchanged (15 direct page/layout declarations plus 5 non-prerender component islands; `Error.razor` remains the intentional page exception).
- The deterministic tournament engine remains authoritative for HP, damage, switch legality, recovery, eliminations, bracket advancement and the champion.
- Existing role/member recovery (`RetrySameThenEligibleRolePool`), Compiler/Repository Curator and Project Maintenance/Cross-Platform Publisher teams remain in place.
- No PublisherStudio source change was required for this LocalGPT-only repair.

## Validation scope

Source/static validation only. No `dotnet` command, package restore, build, publish, GitHub access or online repository access was used for this source package.
