# LocalGPT 4.4.8 — Kernel Creature Tournament ASCII animations

LocalGPT 4.4.8 restores the missing presentation layer for the maintained **Kernel Creature Tournament** Council preset. The tournament already had the independent judge, paired trainers/creatures, bounded battle loop and evidence-based scoreboard, but it did not opt into LocalGPT's shared ASCII surface or emit replayable ASCII animation sequences.

## Changes

- The maintained `kernel-creature-tournament` seed now requires the shared ASCII presentation surface.
- Added a tool-free **Arena lineup animation** after trainer/creature introductions. It emits one bounded `ascii-sequence` block using only pairings and bracket facts already established in the transcript.
- Added a tool-free **Round replay animation** after each judge ruling, including the final ruling. It may visualize only actions/outcomes the judge already accepted and must preserve the published HP/status/bracket state.
- The shared terminal animation extractor now also watches the active Council participant lanes, preferring the newest complete live sequence before falling back to committed chat history. Tournament replays therefore animate while the Council is running instead of appearing only after transcript commit.
- Animation frames are bounded to compact terminal geometry and use the existing replayable transcript animation format; no second tournament/game engine was added.
- Surface-only Council presets are now a first-class bootstrap case: requiring the ASCII surface without declaring a deterministic game runtime class no longer produces a misleading unsupported-game warning.
- Council team seed version advanced to 31 so untouched maintained presets receive the animation behavior while user-owned/copied teams remain authoritative.
- Added `build/audit_kernel_creature_tournament_ascii.py` and extended the 4.4.8 release audit to lock the presentation/state boundary.

## Compatibility

The tournament remains harmless, non-graphic, transcript-owned, and tool-free. The animations are presentation replay only: they cannot decide damage, HP, status, elimination, bracket progression, or the champion. Existing ASCII DOOM, Green Dragon, hot-seat, shared terminal, DevExpress selectors, Council-run session correlation, and ThemeBuilder behavior are unchanged.

No .NET build, restore, publish, NuGet operation, GitHub access, GitHub API call, or other online repository access was performed in this environment.
