# LocalGPT 4.4.3 — ASCII surface, installer identity and documentation repair

## Scope

This release finishes the deferred ASCII-terminal presentation and DevExpress-control work on top of the proven 4.4.2 shell/menu restoration. It fixes the reported hot-seat visibility failure, macOS Installer product naming and Mermaid label collapse without replacing the existing chat/Council/game services, changing the established responsive CSS/flex layout model, or altering the restored navigation drawer implementation.

## ASCII hot-seat and chat presentation

- Added the persisted Council capability marker `localgpt.ascii.surface.required` to the Neon Relay ASCII hot-seat team. Team policy is interpreted by `AsciiChatTextService` rather than being hard-coded in the Razor page.
- A Council team carrying that capability now opens and publishes the shared ASCII surface before its provider request begins. The same check is applied when teams are loaded, refreshed or selected and when direct Council starter prompts are dispatched.
- Both ASCII Display Director phases now inspect `localgpt.ascii.surface.get` before mutating the game display. If the shared surface is closed, the phase stops instead of pretending a hidden frame was rendered.
- The shared `ChatGameConsole` is hosted in a DevExpress `DxPopup` instead of adding another row to the `/chat` grid. Closing the popup hides the presentation only; it does not terminate the chat, Council run, game session or Operator jobs.
- The old `chat-game-visible` / `chat-game-ribbon` grid coupling was removed so opening the ASCII wall no longer squeezes the DevExpress chat/composer. Responsive sizing remains CSS-driven and the chat host retains a bounded usable minimum height.

## DevExpress control alignment

- Replaced ordinary `ChatGameConsole` buttons, selectors and text editors with DevExpress `DxButton`, `DxComboBox`, `DxTextBox` and `DxMemo` controls while retaining the existing CSS classes/layout intent and Theme Builder ownership.
- Kept the small low-level game-control button pad as native buttons because its `data-game-action` DOM contract is consumed directly by the existing keyboard/gamepad JavaScript. No second interaction path was introduced.
- Preserved Chat, Game and Operator mode behavior, fullscreen scaling, Council hot-seat input, AI/Council approval/free-text responses and scenario restart flows.

## macOS Installer identity

- `New-MacPkg` now emits an Installer Distribution document with a visible `<title>` containing `LocalGPT <version>` and builds the final package with `productbuild --distribution` / `--package-path`.
- Packaging validation expands the resulting PKG again, parses its final `Distribution` metadata and removes the output when the expected Installer title is missing or different. This makes the title a validated packaging contract rather than an unchecked string.

## Documentation diagrams

- Mermaid rendering now waits for browser font readiness before layout, keeps strict SVG labels, and uses a bounded flowchart wrapping width so long labels do not collapse inside individual architecture-flow nodes.
- The authored documentation JavaScript, embedded help JavaScript, generated help pages and tracked Pages snapshot use the same updated script/cache key.

## Regression protection

- `MainLayout.razor` and `Drawer.razor` remain byte-for-byte identical to the supplied 4.4.2 source, preserving the restored menu implementation.
- Routable pages keep the maintained `@rendermode InteractiveServer` contract.
- Added `build/audit_release_4_4_3.py` for the release-specific source contracts and reran the maintained Python architecture/resilience/configuration audits that are available without .NET.
- No .NET restore/build/publish, GitHub access, GitHub API operation or online repository access was performed for this source-only handoff.
