# LocalGPT 6.8.5 — Council chat, reconnect and tournament presentation repair

Source baseline: 6.8.4. No GitHub fetch and no .NET compilation.

## Fixed / improved

- **Live Council transcript:** added a bounded server-owned transcript and participant/function-activity feed outside DevExpress AIChat's cached message template. While a Council is running, the cached template displays a lightweight pointer to this feed rather than repeatedly Markdown-rendering all participant traces; completed-run detail inspection stays available in the original message template.
- **ASCII Chat:** corrected `AsciiChatTextService.BuildConversationTranscript` so server-side Council participant activities remain visible even when the ordinary chat message collection is empty. Preserves function/thinking trace conversion and original messages when present.
- **Reconnect / rejoin:** automatically restores the selected run's spooler transcript and evidence on startup rejoin and explicit manual rejoin. Manual rejoin now forces a fresh DevExpress message bind even if the component already had the run attached with an unchanged snapshot. Chat identity and Council run identity remain distinct.
- **Game auto-open:** when a selected Council team requires the ASCII surface, request creation schedules a supervised renderer render. The popup is no longer dependent on an unrelated subsequent user click to appear.
- **UI responsiveness:** while a Council is running, the periodic ASCII mirror uses server-owned session messages instead of walking DevExpress `SaveMessages()` repeatedly. Passive live-session rerenders are debounced to 1.5 seconds. The reconnect module observes only direct body children instead of every descendant updated by model streams.
- **Reconnect isolation:** uses tab-scoped run identity rather than falling back to a different browser tab's localStorage Council run ID; the browser JS cache version and SHA-256 manifest were refreshed.
- **Fullscreen:** strengthened the fullscreen DevExpress popup sizing chain so the ASCII console can occupy the available viewport rather than leaving a grey unfilled modal region.
- **Tournament look:** applied a scoped VGA/ANSI-era navy/cyan/magenta/yellow console palette **only** when the active game is `kernel-creature-tournament`. Other game styles, deterministic mechanics, sprites and game state are unchanged.
- **Guard coverage:** the mandatory `Assert-CouncilXRoundWiring.ps1` audit now checks contracts for these recovery/presentation paths.

## Deliberately unchanged

The deterministic game/tournament engine, Council game bootstrap and model pairing, prompt approval policy, and `ProviderStreamRepetitionWatchdog` are unchanged. A high-confidence repetition watchdog exception must remain a bounded participant failure, not a reason to disable the safety mechanism.

## Remaining verification

The edits passed offline source/manifest/JS syntax and archive checks. PowerShell was not available, and no .NET/Windows/macOS build or real Council/game run was performed here. The observed browser WebSocket timeout may still have causes beyond the narrowed rendering paths and needs a real regression run. The CSS fullscreen fit needs a browser/device test.
