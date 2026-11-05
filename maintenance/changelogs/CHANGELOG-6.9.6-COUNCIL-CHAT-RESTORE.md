# LocalGPT 6.9.6 — recorded Councils are restored into the chat box and can be continued

## Reported (6.9.5)

A finished/failed Kernel Creature Tournament (`70fae698`, *Failed · Creature introduction*, ended by the process restart) showed its **Saved Council transcript** evidence, but the chat box stayed empty, so the conversation could not be continued there.

## Cause

Selecting a run already called `RestoreCouncilChatFromSpoolAsync`. It loads the saved SQLite chat that carries the run marker and puts it into DxAiChat. But the Council message in that saved chat is often **only the run marker** (`<!-- localgpt-live-council:… -->`):

- while a Council runs, a rejoined/second window stores the marker only (the live view reads the server transcript);
- the transcript is written into the message only when that window sees the run *complete* — a run ended by a process restart never completes;
- the owner window's copy is the DxAiChat stream, whose delivery channel drops the oldest chunks under load (`DropOldest`), so it can be incomplete.

After a restart the live session is gone, so the template renders the stored content: an HTML comment → an empty bubble. The fallback without a saved chat produced a shortened preview (64 steps × 2048 characters).

## Change

- `CouncilTextService.BuildCouncilChatTranscriptFromSpool(receipt, marker)`: builds the complete chat representation of a recorded run from the durable spool — marker, header (team, status, round, phase, members), one collapsed panel per step in the live panel format (`<details class="council-step" data-localgpt-stream-id=…>` … `<!--localgpt-council-stream-complete:…-->`, so `ChatContentRenderer` isolates each step exactly like live streams) with thinking, visible answer and provider error, then the saved warnings and the final answer. Nothing is shortened.
- `CouncilTextService.SelectMostCompleteCouncilMessage(...)`: chooses the most complete of stored content, in-memory live transcript of a finished run, and spool transcript (all are append-only views of the same run, so the longest is the most complete). The stored instance is kept when nothing is more complete.
- `Chat.MaterializeCouncilMessages(...)`: applies this to every assistant message linked to a run that is **not running** (running runs keep their live view). Used by
  - `RestoreCouncilChatFromSpoolAsync` (selecting a run, *Load selected transcript & evidence*, rejoin on startup),
  - `LoadSavedConversationAsync` (saved-chat list, *Load latest*, reconnect restore, ASCII operator load).
- The completed message is written back by the next regular save into the **same** SQLite conversation (the save signature is taken from the stored messages), so the chat stays complete even after the spool entry is gone.
- Without any saved chat, the conversation is rebuilt as *original request* + *one complete Council message* — the same shape as the live chat — instead of the shortened per-step preview.
- The saved-chat search no longer requires the Council provider name (Council chats are searched first), so a chat that was continued with another session is found too. Continuing with another session uses the existing *reuse context when switching* option (default on).
- Rendering: in-memory Councils (running **and** finished) use the self-refreshing `LiveCouncilChatTranscript` component, so a finished transcript is not re-rendered on every later chat update. Other messages render through a per-content HTML cache (`ConditionalWeakTable`), so a large restored Council message is not re-rendered for every token of the next answer.
- Logs: every restore states its source (`SQLite conversation <id>` or `durable spool`), message count and Council content size before/after (no content), and why a restore was skipped.

## Configurability

No new hard limits. Everything is derived from the run's own spool, the live session and the saved chat; the marker prefix, Council session name and panel format are the existing ones.

## Version

`6.9.5` → `6.9.6`.
