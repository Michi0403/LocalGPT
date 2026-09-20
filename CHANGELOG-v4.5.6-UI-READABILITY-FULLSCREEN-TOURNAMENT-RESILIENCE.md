# LocalGPT 4.5.6 — UI readability, fullscreen and tournament resilience

## Interface readability

- Reworked the shared configuration workbench navigation so each button reserves a dedicated title row and description row. Long labels wrap at word boundaries instead of breaking inside words.
- Widened the install workbench rail and changed setup/provider hardware editors to a single-column flow where controls need full horizontal space.
- Removed nested max-height/overflow regions from the initial setup hardware/model lists so the page owns scrolling instead of several small inner panes competing for wheel input.
- Changed Council role, workflow, coordination and policy form layouts to one-column editing surfaces.
- Rebuilt Council runtime-class and provider-model checkbox rows around dedicated DevExpress checkbox classes rather than Bootstrap `form-check` positioning, eliminating overlapping checkboxes/text.
- Replaced the selected runtime-class eight-column preview table with stacked field cards, so ownership, input, gating and controller bindings remain readable without a horizontal scrollbar.
- Applied the same one-column editing rule to top-level Council team fields and the benchmark Council configuration/reviewer picker.
- Tightened Chat prompt-suggestion detection so live Council controls are not restyled as welcome suggestions.
- Added theme-safe prompt-suggestion contrast and readable wrapping for Chat recommendation cards.

## ASCII fullscreen

- Browser fullscreen still belongs to the surrounding DevExpress popup root so popup-created dropdown/menu portals remain in the fullscreen DOM tree.
- Fullscreen presentation now stretches the popup root, popup cell, modal root, modal dialog, modal content/body and ASCII surface to the viewport as one layout.
- Removed the fixed-size centered modal effect, black surround and nested popup scrollbars that appeared after entering fullscreen.
- Fullscreen presentation classes are removed deterministically on exit and scaling is recalculated afterward.

## Kernel Creature Tournament resilience and presentation

- Trainer naming now asks the model to derive one nickname from concrete fictional visual/tactical features, commit once and never brainstorm/reconsider alternate names.
- Creature introduction has a compact fallback-naming rule when its paired trainer provider failed before producing a usable `NAME`.
- The existing provider repetition watchdog can now be explicitly forced by a bounded feature-owned workflow without changing the general persisted operator policy.
- The tournament forces repetition protection for Trainer selection, Creature introduction and ASCII Team Artist generation only.
- Repetition-watchdog failures no longer immediately invoke the same-model participant safe retry, avoiding another burst against an already looping local runtime.
- Compact tournament identity/art steps disable role-compliance retry, round-member recovery and final-answer continuation; failed evidence remains visible and deterministic engine fallback naming remains available.
- Added the `ASCII Team Artist` role and `team-building-ascii` workflow step. It creates 2–4 pregenerated terminal-safe frames showing fictional trainer arena avatars/emblems and their paired creatures.
- Added presentation-frame transport to `CouncilKernelTournamentAdvanceRequest`; the deterministic engine prepends bounded artist frames to the engine-owned lineup animation without granting the artist any game-state authority.
- An explicitly ended tournament session is recognized from completed session history and returns `[[TOURNAMENT_COMPLETE]]` instead of throwing a missing-prebootstrap-session exception.
- Supplied Council team seed version advanced from 33 to 34 so maintained, non-user-modified templates can receive the new role/workflow definition.

## Compatibility

- No EF migration.
- No wire-protocol change.
- InteractiveServer declarations remain at the maintained baseline: 15 direct InteractiveServer pages/islands plus 5 explicit non-prerender InteractiveServer islands.
- PublisherStudio is unchanged.
