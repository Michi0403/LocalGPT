# LocalGPT 2.6.1 — Provider canonicalization and localization coverage

## Ollama endpoint identity repair

- `/install` discovery now uses the same `ProviderModelIdentity` endpoint normalization as Chat, Council and persisted provider configuration.
- `localhost` and `127.0.0.1` therefore represent one loopback Ollama identity instead of producing two discovery rows/cards for the same server.
- Configured remote Ollama hosts remain independent endpoint-qualified providers and are not removed or promoted when the loopback host is discovered.
- Detached provider drafts canonicalize the primary and additional Ollama endpoints and discard alias duplicates before the editor receives them. Existing user configuration containing both `localhost` and `127.0.0.1` is therefore cleaned in the UI draft without deleting distinct remote hosts.
- The provider/Council release audit now rejects a discovery path that stops using shared endpoint canonicalization or a detached registry that stops deduplicating canonical Ollama identities.

## English/German localization coverage

- The browser localization runtime now builds its source-to-target map from every aligned English/selected-culture catalog entry, not only `Text.*` entries. Structured keys such as `Install.Workbench.*`, `Common.*` and other service-owned UI labels therefore participate in live DOM localization.
- The localization runtime fetches the maintained English source catalog alongside the selected catalog, translates dynamically inserted text nodes and attributes through a `TreeWalker`/`MutationObserver`, and continues to exclude maintained user/content surfaces.
- English and German catalogs were expanded from 1,497 to 1,800 matching keys, including recent `/install`, Council Teams, Chat configuration, provider model, Theme Fusion and setup/reconnect UI.
- `/install` section descriptions, provider-count summary and last-connectivity-check text are localized server-side so dynamic count/time text does not depend on exact DOM text matching.
- Theme Fusion is explicitly localized, including the dynamic route-step/theme-count summary and Base/Style route labels.
- The shared provider-model panel localizes status badges, actions and reusable model-property labels.
- The MainLayout scroll-assist now reads the maintained `Navigation.Scroll*` keys correctly instead of deriving nonexistent `Text.Navigation.*` keys.
- Localization integrity now requires at least 1,700 aligned English/German entries and guards the new source-map/TreeWalker runtime contract.

## Versions

- LocalGPT: `2.6.1`
- LocalGPTInstallerConsole: `2.6.1`
- LocalGPTWebviewWrapper: `2.6.1`
- LocalGPT Wire Protocol: unchanged `2.1.1`
