# LocalGPT 4.2.5 source validation

LocalGPT 4.2.5 repairs build-gate regressions found by the authoritative Windows build after 4.2.4. The ASCII experience remains additive to the canonical chat history.

## Source validation scope

- text-service ownership: ASCII transcript/sequence/signature manipulation is injected-service-owned rather than Razor-owned;
- DXAiChat role mapping: terminal speaker mapping consumes DevExpress `ChatMessageRole`;
- JavaScript diagnostics: `localgpt-game-console.js` remains guarded and its reviewed normalized SHA-256 manifest entry is current;
- ASCII experience: replay, Council lanes, optional contextual art, bounded sequences, games and state-aware gamepad support remain wired;
- DXFunctions and seeds: `localgpt.ascii.surface.get`, existing `localgpt.game.*` handlers, automatic catalog/system seeding, and shipped ASCII/game Council capabilities remain intact;
- compiler-risk overload guard: invalid char + `StringComparison` call shapes remain forbidden repository-wide;
- standard source audits: application architecture, cross-platform boundaries, provider-qualified Council, configurable behavior, DXFunction wiring, X-Rounds, ConfigurationRoot qualification, async continuations, service resilience, and XML/Razor documentation are rerun without invoking `dotnet`.

The user machine remains authoritative for compiler/runtime validation.
