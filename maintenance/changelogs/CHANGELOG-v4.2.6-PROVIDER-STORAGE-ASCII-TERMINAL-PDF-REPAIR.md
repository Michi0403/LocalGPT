# LocalGPT 4.2.6 — provider storage, ASCII terminal and macOS PDF repair

LocalGPT 4.2.6 keeps the multi-host Council architecture while making local runtime setup, model storage and the shared ASCII console independent of remote-host reachability.

## Provider setup and model storage

- Shows configured Ollama and OpenAI-compatible host bindings independently from reachability, so an offline remote Council host does not hide configuration for Ollama on the current computer.
- Adds an explicit loopback Ollama candidate in System → Install without replacing an existing remote primary binding.
- Reports Ollama stores per physical drive/mount, including free/total capacity, store path, manifest count and physical blob bytes. Per-model size remains a logical size and may overlap when models share blobs.
- Resolves symbolic-link store targets for storage attribution so a linked default store is not double-counted against its physical target or attributed to the wrong volume.
- Recovers model inventory from the effective local Ollama manifest store while Ollama is offline.
- Keeps permanent deletion confirmation. When the local Ollama API is unavailable, deletion may remove the exact manifest and only SHA blobs proven unreferenced by every retained readable manifest. If retained references cannot be verified, blob cleanup is skipped rather than guessed.

## Shared ASCII terminal and Council display

- Preserves the existing LocalGPT operator terminal and canonical chat mirror while extending the same surface as a Council/game presentation and interaction plane.
- Adds bidirectional Council display functions for readback, text writes, single-cell edits, rectangular fills, multiline blits, full-frame submission and pregenerated 2–12-frame animations.
- Supports configurable Council display dimensions instead of requiring renderers to assume 80×25. Existing 80×25 values remain fallbacks for older presets.
- Plays generated animation frames locally in the browser, avoiding one model call per frame and keeping transcript flow continuous.
- Seeds Council renderers with database-backed regex/knowledge discovery guidance so established parsing/layout conventions can be reused instead of regenerated from tokens.
- Crazy ASCII mode remains additive to the meaningful answer and requests a contextual visual reaction on each enabled assistant/Council turn, with compact decorations preferred over disruptive full-screen art.

## macOS release repair

- Fixes PowerShell array unwrapping in the chunked documentation print-book path. A final slice containing exactly one DocFX page now remains an array, so `.Count` remains valid after all prior chunks complete.
- Keeps the bounded low-memory chunked PDF path and durable recovery behavior intact; no monolithic fallback is enabled implicitly.

InteractiveServer render-mode ownership is preserved. No page render mode was removed and child components continue to inherit their existing interactive parent boundary.
