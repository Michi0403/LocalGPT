# LocalGPT 4.9.4 — Microphone / Web Search / Hugging Face / Coding Routing

- Keeps the microphone action visible inside the Chat composer action area beside the attachment/send controls. Recording no longer depends on Whisper being installed; missing Whisper is explained when recording starts and the audio is still persisted/attached.
- Moves the microphone component after `DxAIChat` in render order and raises its composer overlay stacking so DevExpress chat internals cannot paint over the action.
- Fixes the stale startup overlay by explicitly hiding it when either the Blazor start promise or the InteractiveStartupMarker confirms the interactive circuit is ready.
- Adds a bounded `IWebSearchService` / provider abstraction, DuckDuckGo as the shipped default provider, `/api/web-search/search`, and the approval-gated `localgpt.web.search` DXFunction. The DuckDuckGo implementation uses its public Instant Answer API and clearly reports that the result is not a complete organic-results API.
- Extends the dynamic web-research Council seed to search first when the user supplied a topic rather than a URL, then use rendered-page extraction for selected attributed URLs.
- Strengthens the persisted assistant/code-generation routing prompts: a concrete greenfield program request must author the requested files instead of searching unrelated workspaces. Python and other language projects use `SourceFiles` when no language-specific output kind exists.
- Adds upgrade-safe legacy prompt defaults so untouched 4.9.3 persisted prompt rows receive the corrected routing while user-edited rows remain authoritative.
- Adds Hugging Face sort modes plus a `Recent liked in category` workflow that fetches recently updated metadata and ranks recent compatible models by likes. Result cards now show last-modified dates when available.
- Bumps active LocalGPT, installer, wrapper and network identity strings from 4.9.3 to 4.9.4.

No .NET/MSBuild/NuGet build, publish, signing, notarization, or installer execution was performed in the handoff environment.
