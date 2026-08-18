# LocalGPT v3.1.0 source patch — benchmark evidence and live-view responsiveness

This forward-only source patch keeps the existing benchmark/Council architecture and improves the observed benchmark transparency, streamed-text fidelity, and long-run browser responsiveness without reverting the existing work.

## Benchmark evidence is now inspectable

Provider benchmark measurements use `GetStreamingResponseAsync` instead of hiding the subject behind one non-streaming response. Exact streamed fragments are forwarded to the benchmark live transcript (or the owning Council participant lane), and provider-native reasoning/function metadata is projected through the existing `CouncilRuntimeService` trace formatter.

Each `ProviderModelBenchmarkTaskResult` now retains bounded, user-auditable evidence for the assignment, captured provider stream, and visible final answer that was actually scored. Both the single-model benchmark panel and Benchmark Council panel expose that evidence with on-demand task cards. The heavy Markdown evidence is not rendered until the developer clicks **inspect evidence**, so a large completed benchmark does not eagerly materialize every model/profile transcript into the browser DOM.

Thinking/status markup is retained for inspection but removed before benchmark quality and throughput scoring, so reasoning verbosity does not inflate the result.

## Ollama thinking token spacing

Whitespace-only `message.thinking` fragments are no longer discarded. Ollama may emit a space as a standalone streaming fragment; filtering it with `IsNullOrWhiteSpace` caused visible joins such as `from0`, `is5`, or `times.So`.

## Long live Council browser rendering

The server still owns the existing full transient transcript and participant buffers. New display-only accessors return bounded head/tail projections to the recurrent Blazor render path:

- ordered live transcript: 128,000 characters;
- running participant stream: 64,000 characters;
- completed participant transient stream: 8,000 characters;
- completed participant final answer in the recurrent live-board projection: 16,000 characters.

Completed participant answer/provider evidence is now Markdown-rendered only after the developer explicitly opens that evidence. Running lanes remain live. This avoids repeatedly formatting every historical completed lane while a long Council is still producing tokens. The authoritative server-owned `FinalContent`, full technical buffers, and original full accessors remain intact for completion/persistence code; no Council state is deleted by the display projection.

## Validation scope

This package is source-only. No .NET SDK/runtime was assumed or used to compile the project. Python/static repository audits can be run separately; a real .NET/DevExpress build remains required on the intended development machine.
