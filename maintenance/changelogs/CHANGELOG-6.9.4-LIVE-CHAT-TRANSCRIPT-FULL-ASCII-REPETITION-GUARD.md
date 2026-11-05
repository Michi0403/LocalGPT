# LocalGPT 6.9.4 — Live Council transcript in the chat, full ASCII chat, repetition guard for every Council stream

Reported against the 6.9.2 build (`assembly=6.9.2.0` in the attached log). The ANSI battle view with member-drawn sprites was introduced in 6.9.3; 6.9.4 builds on 6.9.3.

## 1. Running Council transcript is shown inside the DxAiChat message

Before: while a Council ran, the chat message only said *"Council is running. The live transcript … are displayed in the independent Council feed above."* (a placeholder introduced earlier to keep the render pipeline free).

Now:

- New component `Components/Shared/LiveCouncilChatTranscript.razor` renders the ordered, server-owned transcript inside the Council chat message.
  - It subscribes to `ICouncilLiveSessionService.Changed` itself, so it updates even when DevExpress does not re-render the cached message template (rejoined / second window).
  - At most one refresh per second; Markdown rendering (`IChatContentRenderer.Render`) runs on the thread pool via `Task.Run`, never on the renderer dispatcher.
  - The transcript is split at the Council's own stream-completion delimiters (`<!--localgpt-council-stream-complete:…-->`). Every completed provider stream is rendered once and kept as a stable `@key` render entry; only the open tail is rendered again. Blazor therefore sends only new or changed segments to the browser instead of the complete transcript on every update.
  - If the server trims or replaces its transient transcript buffer, a prefix probe detects it and the projection is rebuilt once.
  - The text operations (finding the completion delimiter, the prefix probe) live in `CouncilTextService` (`FindCouncilStreamSegmentEnd`, `TakeTextProbeBefore`, `HasTextProbeBefore`), as required by the text-service ownership guard.
- `Chat.razor` `MessageContentTemplate`: running Council → `<LiveCouncilChatTranscript RunId=… />`; finished Council → unchanged (`LiveCouncilTranscript(...)` + `ChatContentRenderer.Render`).

## 2. Live Council feed no longer shows raw `<details …>` markup

The feed above the chat printed the transcript in a `<pre>`, so provider panel markup appeared as text. The feed now shows a short hint (the transcript is in the chat message) plus the participant lanes. A **Show transcript in this feed** button renders the same transcript there with the same component (rendered HTML, not raw markup). The feed still reads `CouncilLiveSessions.GetTranscriptForDisplay(currentLiveCouncil.RunId)` (wiring guard contract) to decide between the waiting text and the hint.

## 3. ASCII chat shows the complete chat

Before: `AsciiChatTextService.BoundParticipantSource` cut running lanes at 8 KB, completed traces at 12 KB and final answers at 16 KB and inserted `[... earlier streamed trace remains available in canonical Chat/Council history ...]`; in addition the terminal received the 64 KB / 8 KB display windows of `GetParticipantActivitiesForDisplay`.

Now:

- `BoundParticipantSource` and its three limits are removed. Lanes are mirrored completely.
- New `ICouncilLiveSessionService.GetParticipantActivitiesForTerminal(runId)`: complete lane text. Each lane keeps a text snapshot plus a mutation counter (`CouncilLiveParticipantActivityState.ContentVersion/ContentSnapshot/ContentSnapshotVersion`), so an unchanged lane is returned as the same string without copying its buffer again. Chat's ASCII mirror uses it; the Chat page's own lane cards keep the display window.
- New `AsciiChatTextService.BuildConversationTranscriptBlocks(...)`: the transcript as ordered blocks (one per message / lane, separator blocks between). Messages are cached per immutable content string (`ConditionalWeakTable`), lanes per content-free signature, so unchanged blocks keep their string identity. `BuildConversationTranscript` returns the concatenation (unchanged API).
- `ChatGameConsole.razor` renders the blocks inside the same `<pre class="chat-game-screen">`; Blazor only transmits blocks whose text changed.
- The ASCII-animation search (`ExtractSequenceFrames`) is bounded separately (newest 48 lanes, newest 64 K characters each), so the search cost does not grow with the now complete transcript.

## 4. Repetition watchdog fires for every Council stream

Before: the watchdog's operator switch `ProviderStreamRepetitionWatchdogEnabled` is seeded `0` (also in 6.4.1), and only three game phases forced it on (Trainer selection, Creature introduction, Team building ASCII). The looping *Trainer commands* streams in the screenshots were therefore never observed.

Now:

- `RunParticipantAsync` and both Council recovery streams (corrective role retry, final-answer recovery) create the watchdog with `forceEnabled: true`. The operator switch still governs non-Council chat streams. Detection thresholds are unchanged (runtime policy).
- New `ProviderStreamRepetitionWatchdog.TrimRepeatedTail`: when the stopped stream ends in a character-exact loop of at least three copies (period 24–16384 characters), the text before the loop plus exactly one copy is kept (the line the copy ends on is completed). `RunParticipantAsync` then stops only that provider request, logs a warning with period/agreement/removed characters, writes a visible watchdog note into the live stream and continues the step with the kept answer (so e.g. a trainer command is still parsed). Without a character-exact copy the previous behavior applies (bounded participant failure, no same-model retry).

## 5. Pixel screen draws the ANSI battle view

`renderPixelFrame` (`ChatGameConsole.razor.js`) skipped every space, including styled spaces, so sky, grass and panel backgrounds of the battle view were missing in **Pixel screen**. Styled spaces now paint their cell background. Frames that carry background colors use 6×12 cells (the text-screen proportion the battle layout is designed for); plain frames keep the classic 6×8 grid.

The JavaScript review manifest `build/javascript-diagnostics-files.sha256` was updated for this file.

## 6. Debug-symbol scan is reused for chat briefings

`BuildDebugInventoryService` (singleton) keeps the newest-250 symbol listing for 5 minutes and serializes scans with a `SemaphoreSlim`; briefings reuse it. `copyFiles=true` (explicit diagnostics copy) always scans again.

## Version

`6.9.3` → `6.9.4`.

## Not changed

- Kernel tournament engine, ANSI battle renderer (6.9.3), sprite parsing, Council seeds.
- Runtime policy seeds (the watchdog operator switch stays `0`; Council streams force the guard in code).
- Installer.
