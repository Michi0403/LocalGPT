# LocalGPT 4.7.3

## ASCII popup interaction regression repair

- Fixes the 4.7.2 DevExpress dropdown portal override so a closed `dxbl-popup-cell` cannot remain a full-page pointer/wheel shield after a combo box is used.
- Keeps dropdown portals above the open LocalGPT modal, but enables pointer events only while their dropdown is actually visible.
- Restores wheel/trackpad scrolling and normal page interaction after using Fullscreen scale, Display, Control, AI-step, language, and other DevExpress combo boxes.
- Expands the popup to a responsive `min(1920px, 98vw)` by `96dvh` workspace while retaining bounded fallbacks for smaller screens.
- Reflows the ASCII wall header into a responsive title/control grid so the title and subtitle do not get crushed by the selector/action row.
- Keeps a bounded header scrollbar only as a narrow-screen fallback instead of forcing the entire modal or terminal into nested scrolling.

## Terminal fitting and browser responsiveness

- Reworks ASCII fitting so scale calculation derives a target font directly from measured glyph/line ratios instead of first resetting the terminal to 16px on every resize pass.
- Stops observing the popup parent for scale changes; the ASCII wall observes its own geometry and browser resize events, removing a resize-observer feedback path.
- Applies fitted density to Chat and Operator scrollback surfaces as well as game frames; scrollback modes always keep vertical scrolling instead of trying to compress an entire long history into one viewport.
- Keeps game `Fit whole frame`, `Fit width + vertical scroll`, and `Native size + scroll` semantics while preserving the Pixel renderer path.
- Removes whole-subtree MutationObservers from follow-tail behavior. Follow-tail is now synchronized from the existing Blazor layout refresh path, avoiding a DOM query/scroll callback for every streamed text mutation.

## Large Council ASCII mirror protection

- Caches unchanged Council participant terminal projections by activity signature, so one model token does not re-run normalization across every unchanged model lane.
- Bounds only the mirrored terminal copy of very large participant traces/final answers and inserts an explicit omission marker; canonical Chat/Council history remains authoritative and unmodified.
- Keeps the newest streamed tail plus opening context so active model progress remains useful while avoiding multi-megabyte terminal reprocessing during 60-model runs.

## Compatibility and scope

- Preserves the 4.7.2 Council provider/model identity reconciliation and explicit-team-binding behavior.
- Preserves the 4.7.2 Kernel Creature Tournament active-pair scheduling, generated HUD/effect motifs, ASCII/Pixel rendering, greetings, attack animations, finishers, statements, and deterministic engine state.
- Preserves existing `@rendermode InteractiveServer` coverage.
- PublisherStudio source is unchanged in this release.
- No GitHub/network repository access or .NET build tooling was used while preparing this source package.
