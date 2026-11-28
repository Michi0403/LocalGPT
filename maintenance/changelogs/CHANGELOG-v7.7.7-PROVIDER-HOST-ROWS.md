# LocalGPT 7.7.7 · Mediocre Marmot

The Undersuperiority Series. Baseline: 7.7.6 (commit 82b2ca02).

## Fixed — Setup › AI providers
- Additional Ollama and OpenAI-compatible hosts each render as their own full-width group (caption = endpoint). Before, each row's fields used the default half-width columns and zig-zagged across the grid.
- "Save provider settings" no longer repeats on every additional host row. Each provider tab (LM Studio / local OpenAI-compatible and Ollama) keeps one save action, placed below its host list.
- A per-host "Test connection" result stays on that host row (reachable / not reachable, time, provider message) instead of only in a short toast.
- New localized texts: `Install.HostTest.Reachable`, `Install.HostTest.Unreachable`, `Install.HostGroup.New` (all six languages).

## Checks actually performed
- Reproduced the layout, duplicate save and short-lived test toast on the installed macOS LocalGPT 7.2.2 while registering four LAN Ollama hosts.
- Source edits checked by brace balance and diff review only. **No .NET build or UI test of 7.7.7 has been run yet.**
- Not changed: "Refresh Connectivity" not listing saved LAN hosts was seen on 7.2.2; 7.7.6 discovery already includes `OllamaCores`, so verify on 7.7.7 before changing it.
