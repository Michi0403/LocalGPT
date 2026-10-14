# LocalGPT 2.8.2 — Benchmark rejoin and build repair

- Fixes the benchmark JSON parser so DXFunction/model JSON that arrives with HTML entities is normalized before parsing. The parser now consumes the first JSON object, tolerates comments and trailing commas, and ignores trailing prose.
- Fixes the observed BFCache rejoin failure: `localgpt-reconnect.js` intercepts persisted back/forward-cache restores before Blazor starts reusing preserved DevExpress/Blazor event bookkeeping, then performs a clean reload while retaining the active Council rejoin target.
- Keeps server-owned Council work independent from the browser circuit; the attached run in the supplied log completed even after the browser circuit died.
- Moves human-visible entity decoding out of Razor components into `CouncilTextService`, satisfying the maintained text-service ownership boundary.
- Removes the stale `presets` XML documentation parameter from `GetHardwarePerformancePresetFunction`.
- LocalGPT, WebView wrapper and installer versions are **2.8.2**. Wire protocol remains **2.1.1**.
