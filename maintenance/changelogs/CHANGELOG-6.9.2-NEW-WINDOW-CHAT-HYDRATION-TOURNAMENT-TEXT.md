# LocalGPT 6.9.2 — New-window Council chat hydration and tournament text repair

## 1. A second browser window showed the running Council everywhere except in DXAiChat

Observed (6.9.1): opening `/chat` in another window while Council `b33f0e2a` was running showed the Live Council panel and the ASCII console chat (server-owned live session), but DXAiChat stayed on its empty starter cards ("0 shared chat message(s)").

Cause: a new window has its own circuit and its own per-tab `sessionStorage`, so `localgpt-reconnect.js` supplies no rejoin run id. The session-list refreshes (`RefreshHumanCollaborationAsync`, `ScheduleLiveCouncilListRefresh`) still auto-selected the newest running Council via `SelectedCouncilRunId`. That property drives the live panel and the ASCII mirror, but only `AttachToLiveCouncilSessionAsync` hydrates DXAiChat and rebinds the composer to the Council session; it was never called on this path. The composer stayed bound to the provider fallback session.

Fix:
- `ScheduleAutomaticLiveCouncilAttach` attaches once per run and circuit when the session list auto-selected a running Council, this circuit is not attached to any run, and the Chat is otherwise empty (no active conversation, no visible messages). It is also evaluated when the Chat runtime finishes activating, because the list may select the run first.
- A window that already shows its own conversation is not taken over; the explicit rejoin control is unchanged.
- "Start new chat" suppresses further automatic attachment for that circuit, so a deliberate empty chat is not hijacked by a running Council.

## 2. Kernel Creature Tournament text showed HTML entities and prompt placeholders

Observed: `&lt;complete frame 1&gt;`, `&lt;adopted or single fallba…`, `We&#39;ll`, `&quot;Brace yourself…&quot;`, `HUD LIGHT & 127775;`.

Cause: the tournament ingests `MultiModelCouncilStep.VisibleContent`, which is the HTML-encoded chat transcript form of the provider stream. The arena renders through `textContent`, so encoded entities appeared literally. `&#127775;` is the encoded 🌟; its `#` was additionally dimmed by the border colorizer. Separately, small models copied angle-bracket placeholders from the workflow prompts verbatim.

Fix (`MultiModelCouncilService.KernelTournament.cs`, ingestion boundary only):
- `PlainModelText` decodes HTML entities (at most two passes for double-encoded copies) and removes characters that do not occupy exactly one terminal cell (surrogate pairs/emoji, ZWJ, variation selectors, BOM, other control characters). Applied to tag reads, ASCII Team Artist frames and per-exchange evidence. Safe because the console renders text through `textContent`, never `innerHTML`.
- `IsTemplatePlaceholder` rejects an echoed `<…>` placeholder as a tag value (the engine's existing name fallback applies) and drops artist frames that consist only of placeholders (the engine's own frames are shown instead).

## Version

`6.9.1` → `6.9.2`.

## Not in this release

The member-drawn sprites and the ANSI battle presentation for the Kernel Creature Tournament are being implemented separately; the 70s ASCII renderers of the other games are not touched.
