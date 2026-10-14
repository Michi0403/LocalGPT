# LocalGPT 4.4.9 — Kernel tournament engine and role-context isolation

LocalGPT 4.4.9 turns the maintained **Kernel Creature Tournament** from a transcript-generated ASCII sequence into a real incremental LocalGPT game-session runtime. The change follows the same architecture used by the interactive ASCII corridor: AI participants make small bounded decisions, the service-owned engine applies one authoritative state transition, and the shared terminal presents the resulting game state.

## Tournament runtime

- Added the persisted, copyable `games.ascii.kernel-tournament.rules` runtime class with starting HP, bounded damage, guard reduction, recovery, maximum exchanges, movie frame delay and subtitle hold.
- The maintained tournament assigns that runtime class, so Council game bootstrap creates a real `kernel-creature-tournament` session before model workflow execution.
- AI trainers and creature kernels contribute compact `COMMAND` / `MOVE` / `FLAVOR` / `VOICE` evidence. They do not own HP, damage, status, eliminations, bracket advancement or the champion.
- The deterministic game-session service owns those consequences and publishes the authoritative scoreboard plus `[[TOURNAMENT_CONTINUE]]` / `[[TOURNAMENT_COMPLETE]]` control markers.
- Every authoritative exchange produces successive one-shot arena frames. The game view does not loop those frames indefinitely; its subtitle remains visible for the configured hold period after the final frame.
- Maintained defaults are 320 ms per frame and a 1500 ms subtitle hold.

## Role-context isolation

- Added persisted `localgpt.council.context.role-isolated` capability handling. Role-isolated teams do not receive unrelated project/file/Markdown inventories, saved memory, prior project conversations, external project context, or the general connected-capability briefing.
- The maintained isolated presets are Kernel Creature Tournament, ASCII DOOM Council Adventure, Green Dragon, GameDirector Runtime, ASCII hot-seat, adaptive benchmark and Initial Setup.
- Project/development, spreadsheet, OpenSCAD, learning and general-purpose presets intentionally keep broad context because repository/project/evidence access is part of those jobs.
- This specifically prevents narrow role players from drifting into unrelated repository or Minecraft/mod-development work merely because such context exists elsewhere in the application session.

## Compatibility and presentation

- Preserved the existing game-screen CSS contract while layering the tournament subtitle as a separate overlay; the terminal screen itself keeps its prior sizing, font and selectable-text behavior.
- One-shot game sequences no longer restart their final-frame timer after focus/visibility changes, and subtitle timers are cancelled when the console detaches.
- Corrected the tournament runtime capability spelling to the existing `localgpt.runtime-class.get` identifier.
- Release-visible LocalGPT version surfaces, documentation cover/download naming, browser cache-busters and LocalGPT HTTP user-agent strings now identify 4.4.9 consistently.
- Existing conversation ASCII sequences keep their prior repeating presentation behavior; deterministic game-session movie sequences are one-shot.
- The existing `InteractiveServer` page/component conventions and deployment structure are preserved. The only route without `@rendermode InteractiveServer` remains the error page, which is intentionally non-interactive.
- The reviewed JavaScript diagnostics manifest and game-console cache-buster were refreshed for the intentional one-shot/subtitle change.

4.4.9 makes both boundaries explicit: the game engine owns state and frames; each isolated AI receives only the small role evidence needed for its current task.
