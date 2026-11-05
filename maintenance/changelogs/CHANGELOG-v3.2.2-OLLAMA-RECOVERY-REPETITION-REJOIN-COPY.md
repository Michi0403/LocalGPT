# LocalGPT 3.2.2 — Ollama recovery, repetition watchdog, rejoin copy

## Scope

This source release advances the corrected LocalGPT version line to **3.2.2**. It is based on the latest 3.1.11 source payload (the release that corresponds to corrected-lineage 3.2.1) and preserves the working Chat configuration, quick-selector row, attachment repair, and session restore behavior.

## Repairs

- Extends the provider stream repetition watchdog from short 1–32-token loops to bounded sentence/paragraph cycles up to 512 normalized tokens.
- Keeps the historical short-loop thresholds intact while requiring stricter agreement for long loops.
- Expands only the watchdog rolling buffer required for long-cycle evidence; it remains bounded.
- Removes the generic Ollama recovery demotion to `num_gpu=0`.
- Before an Ollama retry, checks the exact provider-qualified endpoint/model for bounded reavailability and keeps the run-scoped CPU/GPU road unchanged.
- Keeps bounded context/output retry limits, but no longer interprets an Ollama restart as a reason to switch a GPU-dependent model to CPU.
- Repairs Copy on rejoined live Council messages: the stable persisted `localgpt-live-council` marker remains the run identity, while the native Copy action for that rendered live message copies the authoritative visible transcript instead of the marker.
- Further normalizes nested HTML entities in inert structured JSON/code surfaces and fenced Markdown code without globally decoding arbitrary model HTML.
- Advances LocalGPT, installer, wrapper, browser cache key, and outbound LocalGPT user-agent version to 3.2.2.

## Real-build corrections included

- Avoids the iterator-policy scanner false positive caused by a literal `{` in `StartsWith('{')` without weakening the existing iterator guard.
- Resolves the C# `CS0136` local-name collision by renaming the earlier running-Council pattern variable to `activeRunId`.

## Protected behavior

- `Chat.razor` is byte-identical to the previous source baseline.
- `Chat.razor.css` is byte-identical to the previous source baseline.
- No new CSS was added.
- The quick selector row and Chat Configuration structure are unchanged.
- Existing attachment MIME/draft recovery remains in place.
- EF migrations and database compatibility code are unchanged.
- 1-Wire remains version 2.1.1.

## Validation note

The environment used to prepare this source does not contain the .NET SDK or Windows PowerShell, so no LocalGPT/DevExpress compile is claimed. Source/static validation, JavaScript syntax validation, regression audits, and an equivalent run of the repository's iterator brace-scanner policy were performed before packaging.
