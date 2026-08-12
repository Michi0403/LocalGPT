# LocalGPT 2.7.4 changelog

## Parallel Council result visibility

- Every provider-qualified Council participant still owns exactly one normal participant turn per configured phase/round, regardless of whether the Ollama endpoint is local or on another configured AI host.
- The live Council activity board now stores the authoritative completed participant answer as soon as that participant finishes instead of waiting for an earlier member's ordered transcript stream to drain.
- Completed live lanes show a **Result ready · expand** affordance with the participant's final answer immediately; provider thinking, DXFunction/tool activity and stream history remain available in a separate nested expandable section.
- Ordered transcript integration is intentionally retained so concurrent provider HTML/thinking streams cannot corrupt one another. The live lanes are the immediate parallel view; the main transcript remains the stable chronological Council record.
- The live-session snapshot/service contract now carries `FinalContent` and the Council runner commits it before marking the participant complete.

## Model prose spacing

- Added a conservative render-time repair for recurring model prose such as `output24,576`, `context262,144`, `connected1-Wire` and `detailed1-Wire`.
- The spacing repair is display-only and deliberately skips fenced code, lines containing inline code and raw HTML-tag lines; URLs, model identifiers, paths and serialized/code content are not generically split.
- Council bootstrap instructions now explicitly ask models to preserve normal prose spaces while leaving identifiers/code/URLs untouched, reducing the malformed text before the renderer fallback is needed.

## Cancellation diagnostics

- Final-answer recovery canceled because the user stops/cancels the Council is now logged as an expected debug cancellation instead of a service failure. Real final-answer recovery faults remain error diagnostics.
- Blazor/rejoin behavior and the server-side Council spooler remain unchanged; no browser BFCache-disabling workaround was introduced.

## Existing orchestration retained

- X-Rounds/X-Functions, revisable round history, single-consumer live heartbeat restart behavior, later shared heartbeat context, per-host/per-road scheduling controls, per-model road settings and user-editable model timeout from 2.7.3 remain intact.
- Code generation/file-writing, PowerShell, generic source outputs, CodeDOM fallback, review/approval gating and Council DXFunction discovery remain intact.

## Version

- LocalGPT application/wrapper/installer: **2.7.4**.
- `LocalGPT.WireProtocolVersion` remains **2.1.1** because this release changes LocalGPT Council/UI state only and adds no wire message contract.
