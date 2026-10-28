# Game Studio: project-owned scenes and regex-driven presentation (6.5.9)

This is an **authoring example**, not a built-in game and not a default Council role. Any enabled Council team whose configured workflow/function policy permits `project.game.scene.get`, `project.game.scene.patch.propose`, and `project.game.scene.review` may author and review scene data. The optional `game-scene-art-curation` team is a resettable example; no runtime switch depends on its name.

## Ownership and renderer choices

- The Game project owns confirmed requirements, the current Game Studio draft, revision JSON, sprites, screens, rules and selected `artworkMode`.
- `ClassicGlyph` preserves earlier single-character/skeleton-style presentation. The existing Kernel Creature Tournament rigs are **not removed**.
- `AuthoredAscii` draws preauthored multi-line `asciiRows` and conditional `textCanvases`. `AuthoredAnsi` uses the same source layout with the configured color/palette presentation capabilities. `AuthoredPixel` selects authored `pixelRows` and palette tokens for the browser canvas. The shared deterministic scene state/controls do not change with these choices.
- The browser canvas is a presentation adapter; the C# ASCII preview uses the same canonical scene/world clock. ANSI currently retains existing palette foreground/background behavior, not per-character 24-bit ANSI escape-sequence support; future richer ANSI metadata should extend the renderer, not embed terminal escape codes in content.
- The scene regex transitions are visual state assignments only. They do not change health, physics, damage, GameDirector approval or game controls.

## Small declarative project-scene patch

The patch below is an illustrative object for `project.game.scene.patch.propose`. It assumes the target Game Studio draft has a view at least 80 columns x 24 rows and that the user/team previously persisted a Regex pattern named `project.scene.phase-command` (for example `^(?<phase>idle|alert|victory)$` with flags `i`) using the existing regex catalog. Knowledge entries may link to that same regex using the maintained knowledge–regex relationship; the scene stores **only its catalog name**, not a copy of the regular-expression source.

```json
{
  "artworkMode": "AuthoredAscii",
  "textCanvases": [
    {
      "id": "arena-sky",
      "x": 1,
      "y": 1,
      "rows": [
        ".-------------------------[ NIGHT ARENA ]--------------------------.",
        "|   .  *      .     .       *      .           *     .            |",
        "|     /\\           /\\          /\\         /\\                   |",
        "|____/  \\_________/  \\________/  \\_______/  \\___________________|"
      ]
    },
    {
      "id": "arena-alert",
      "x": 4,
      "y": 18,
      "rows": [" >>> ALERT: {phase} <<<"],
      "visibleWhenVariable": "phase",
      "visibleWhenValue": "alert"
    }
  ],
  "regexTransitions": [
    {
      "regexPatternName": "project.scene.phase-command",
      "variable": "phase",
      "captureGroup": "phase"
    }
  ],
  "sprites": [
    {
      "id": "night-wyrm",
      "asciiRows": [
        "              /\\      /\\",
        "     /\\      /  \\____/  \\     /\\",
        "    /  \\____/   .----.   \\___/  \\",
        "   /              \\__/           \\",
        "  <_______  ()    /  \\   () ______>",
        "           \\____/    \\____/"
      ],
      "pixelRows": [],
      "palette": {}
    }
  ]
}
```

A separate actor can reference `"spriteId": "night-wyrm"`. Screen and sprite text are literal, whitespace-preserving rows. AI teams should design the full composition in the draft; the engine does **not** invent anatomy, shadows or a game-specific pose from the name. Multiple artist-curated sprite IDs can be referenced by regular Game Studio `animations` at world-time-based durations.

## Reliable authoring/review procedure

1. Have the selected, configured Council team read the Game project and `project.game.scene.get`, then narrow relevant Knowledge and stored regex patterns. New/renamed patterns must be created using the normal catalog workflow first.
2. AI proposes a **small** isolated patch or full scene. The current draft is not modified by the proposal.
3. A different curator reads the proposal using `project.game.scene.proposal.get`, checks the game's bounds, character-art alignment, referenced sprite IDs, regex captures, visibility predicates and sample events, then records `project.game.scene.review`.
4. The user explicitly accepts the candidate in Game Studio. `Game Studio > World` selects `artworkMode`. `Game Studio > JSON` edits complete scene declarations. The scene's regex tester can preview named catalog matches without saving changes.
5. A finished, approved Game project revision is compiled through `project.game.build` and consumed by the runtime. The build/launch validates catalog pattern names; the GameDirector remains the authority for gameplay actions.

### Scope of this release

This release makes authored character screens, multi-line character sprites and named-regex presentation variables functional in Game Studio previews and project-built game sessions. It does **not** transparently migrate the separate built-in Kernel Creature Tournament onto a Game project scene; that older skeleton renderer intentionally remains intact. It also does not add rich per-cell ANSI styling or replace the current pixel canvas with an unrelated renderer.
