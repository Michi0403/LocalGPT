# LocalGPT 3.9.9 — provider model links, safe install resolution, console repair, and guided Ollama setup

## Provider model discovery and installation

- Gives the combined `/install` model list a provider-owned model/catalog link in addition to the attributed CanIRun.ai source. Safely mapped provider identifiers open their model-detail page; unmapped recommendations open a provider-owned search/catalog page rather than manufacturing a detail URL.
- Keeps existing direct one-click installation for provider-safe mappings without widening heuristic model-ID inference.
- Adds an explicit **Resolve & install model** path for compatible CanIRun.ai recommendations whose provider ID is not already known. It searches only the selected provider's maintained public catalog, exposes the returned candidates for review, and installs only when exactly one conservative identity match is found.
- Ambiguous or absent catalog matches remain review-only. LocalGPT does not guess an Ollama/LM Studio install ID from hardware-fit evidence.

## ASCII command-console repair

- Decodes redirected stdout/stderr as UTF-8 so provider progress glyphs are not displayed as Windows-code-page mojibake.
- Normalizes common ANSI CSI/OSC controls, carriage-return rewrites, cursor-column updates, erase-to-end behavior, backspaces, tabs, and non-printing controls into bounded plain Unicode text before console events reach `/install` or Chat's shared ASCII console.
- Keeps the setup console as a real monospace `white-space: pre` surface with horizontal scrolling instead of wrapping terminal columns and progress bars into broken shapes.

## Guided Ollama setup

- Adds one explicitly confirmed guided Ollama action that installs only when missing, re-detects the executable, starts the existing Ollama process service only when stopped, and registers the maintained loopback provider endpoint after the runtime is actually running.
- On macOS, a completed installer that still does not expose a discoverable executable stops with a concrete first-launch/vendor-source recovery path; completed steps remain intact for a retry.
- Existing granular Detect, Install, Start/Stop/Restart, model, and Register actions remain available and unchanged.

## Regression boundaries

- Leaves the Council/team reconciliation and self-cleaning behavior unchanged because the supplied runtime observation showed it recovering the stale team state successfully.
- Preserves the routed `InteractiveServer` ownership model. `InitialSetupAssistantPanel` remains a child of the interactive `/install` route and does not introduce a nested render boundary; the intentionally static Error page remains static.
- This handoff is source/static only: no `dotnet build`, `dotnet publish`, GitHub access, or platform packaging was used.
