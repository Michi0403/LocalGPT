# LocalGPT 6.9.7 — graphical Council lanes, rejoin restore on start, game display fixes

## 1. Graphical Council lanes are back in the chat message

The Council chat message shows the complete ordered transcript first and the participant lanes underneath, while the Council runs and after it finished:

- one card per member with a state dot and a coloured edge: **streaming** (green, pulsing), **waiting for the model runtime** (amber), **finished with an answer** (blue), **closed without an answer** (grey);
- model, phase · role · hardware route, current status;
- **Show stream / Hide stream**: running lanes start expanded, finished lanes start collapsed; the choice is kept per lane while the board refreshes;
- finished lanes: **Inspect answer, thinking and provider evidence** opens the existing evidence view with the authoritative server-side lane;
- header summary: streaming / waiting / finished / closed counts.

New component `Components/Shared/LiveCouncilLaneBoard.razor` (+ `.razor.cs`, `.razor.css`). It refreshes itself at most once per second, reads the bounded display lanes, renders Markdown only for expanded streams, on the thread pool, and reuses the HTML of a stream that did not change. A stream card shows the newest 16 K characters (`CouncilTextService.TakeMarkdownTail` cuts at a line start and reopens a code fence that was open at the cut); the complete stream remains in the transcript above and in the evidence view. The previous inline board in `Chat.razor` and its handler `ToggleCompletedCouncilActivityEvidence` were replaced by the component and `ToggleCouncilLaneEvidence`.

## 2. Rejoin link / reconnect restores the Council chat on start

A Council restore requested during start-up (for example `?rejoinCouncilRunId=…`) ran before the provider sessions were built and was skipped (`chat restore skipped: no Council chat session is registered`). The restore is now remembered and resumed right after `RebuildDynamicSessions` created the Council session. Log: `chat restore deferred until the provider sessions … are ready`, then the usual `Restored Council … into Chat from …`.

## 3. Game display keeps playing when the browser window is not focused

Animation sequences (game frames, ASCII sequences) stopped on every window blur, e.g. while working in another application with the game on a second monitor; they only continued after focusing the browser again. Animations now pause only while the page is hidden (background tab, minimised). Gamepad/keyboard polling still stops without focus.

## 4. Kernel Creature Tournament text

- Echoed prompt placeholders such as `<one short trainer line>` or `<one new compact printable-ASCII arena HUD>` are removed from tagged values (`VOICE`, `HUD`, `FLAVOR`, `EFFECT`, `NAME`, …) — also when real text follows the placeholder. HTML-like tags are not touched.
- Repeated sentences inside one line are shown once; identical trainer and creature lines are joined once (subtitle, greetings, flavour, effects, HUD).
- Member sprites need a shape: drawings whose rows are all alike (bars, stripes) are not accepted as a creature; the neutral "no sprite drawn by …" placeholder is shown instead.
- The separate subtitle overlay is hidden for the tournament battle view (it covered the battle message box, which already shows the same text). It stays available to screen readers.

## 5. Member failure recovery

- A member stopped by the repetition watchdog never got the low-level same-member retry. With the team setting "same member, then role pool" (`RetrySameThenEligibleRolePool`), the round recovery now starts with the same member in that case and then continues with the configured role pool, as the setting says. New step flag `MultiModelCouncilStep.RepetitionWatchdogStopped`.
- Round recovery decisions are logged: failed member, failure kind (repetition watchdog / provider failure / empty answer), policy and attempts, and each attempt's recovery member and outcome.

## Version

`6.9.6` → `6.9.7`.
