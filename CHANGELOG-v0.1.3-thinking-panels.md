# LocalGPT v0.1.3 — Stable streamed thinking panels

## Fixed

- Preserved the user-selected expanded/collapsed state of model-thinking and AI Council panels while streamed tokens rerender a message.
- Added stable per-message panel keys for thinking, Council prompt, Council step, and live Council stream sections.
- Kept unfinished panels open by default, but stopped later stream updates from overriding an explicit user toggle.
- Kept a user-opened thinking panel open after generation completes; panels still collapse on completion when the user did not choose a state.
- Preserved Council stream identifiers after completion so the same panel remains identifiable throughout its lifecycle.

## Implementation

- `ChatContentRenderer` assigns deterministic `data-localgpt-panel-key` attributes.
- `chat-details-state.js` stores state per rendered chat-message host and restores it after DOM replacement.
- `Chat.razor` marks each message-content host as an isolated state boundary.

## Safety

The browser helper only observes and updates the `open` property of LocalGPT-owned `<details>` elements. It performs no network, filesystem, command, storage, or localhost operations.
