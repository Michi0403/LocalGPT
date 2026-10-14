# LocalGPT 2.6.5 — Council provider route, localization and coding-output repair

- Fixed provider discovery so native Ollama and the same host's OpenAI-compatible `/v1` API can coexist as distinct provider-qualified Council routes.
- Hardened saved provider-qualified preflight for transiently offline but still configured endpoints; same-name fallback remains forbidden.
- Retains stale Council selections visibly instead of silently dropping them. Unavailable routes are red, explanatory, removable, and block a false "Council started" message.
- Configured local provider cards are visibly red when currently unreachable.
- Fixed first-run onboarding localization with stable EN/DE keys for all visible headings, status cards, installer profiles and quick starts.
- Fixed the stale first-run product version by deriving `ICustomVersion` from the running assembly instead of a hard-coded 2.5.0 literal.
- Added a Council visible-output contract: raw JSON/work-order metadata cannot replace the user answer; coding requests must expose concrete code/source or an authorized artifact result before any internal machine-readable proposal.
- Version bumped to 2.6.5. Wire protocol remains unchanged.
