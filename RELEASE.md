# LocalGPT 3.1.11

LocalGPT 3.1.11 is the **Council Formatting, Topic-Neutral Learning & Session Restore** release. It is additive over 3.1.10 and keeps the working chat composer, attachment recovery and quick-preset row intact.

## Toolchain state

- .NET SDK policy: 10.0.400 / `net10.0`
- DevExpress: existing `25.2.*` package lane retained
- 1-Wire protocol: `2.1.1`

## Main changes

Council provider/function payloads and LocalGPT self-assessment envelopes now render through controlled structured/code boundaries so JSON stays readable instead of leaking serializer escapes or being corrupted by Markdown URL parsing. The Learning Round seed is topic-neutral and uses a persisted four-step evidence workflow rather than assuming a software project. Running Council snapshots now retain the saved team/model/performance identities and the remaining Chat configuration needed to repopulate the existing editors and quick selectors when a browser circuit rejoins the session.

## Protected UI

`Chat.razor`, `Chat.razor.css`, and `wwwroot/js/localgpt-chat-ui.js` are byte-identical to 3.1.10. This release does not alter composer geometry, Attach/Send/Stop, the quick-selector row or Running session tools placement.

## Validation boundary

This source package is not compiled in the preparation environment. Validation is source/static only. See `CHANGELOG-v3.1.11-COUNCIL-FORMATTING-LEARNING-SESSION-RESTORE.md` and `VALIDATION-v3.1.11-source.md`.
