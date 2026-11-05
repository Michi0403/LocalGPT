# LocalGPT 2.8.6 changelog

## Provider-visible reasoning and function traces

- Native Ollama chat requests now explicitly request provider-supplied thinking with the `think` option when the selected runtime/model supports it. Compatibility probing is bounded per provider-qualified model; older runtimes keep working without permanently disabling reasoning after unrelated request failures.
- The existing Ollama thinking formatter remains the rendering path, so reasoning is still separated from the final answer in the established `Model thinking` disclosure instead of being mixed into answer text.
- Ollama automatic DX function calls and their bounded results are now emitted as normal user-visible transcript fragments in both streaming and non-streaming paths. They therefore survive `/chat` persistence instead of existing only as transient status updates; call arguments and results are expanded by default so the trace is actually visible.
- Provider SDK metadata is inspected for provider-supplied reasoning/thinking/analysis and function/tool call/result objects or additional properties. When a provider exposes that data, LocalGPT projects it into durable transcript markup without assuming a single vendor-specific content type.
- Council participant streaming now carries the same provider-visible reasoning and function metadata into live Council output, step content, saved sessions, and logs.
- Thinking extraction now concatenates every LocalGPT thinking disclosure in a message/step rather than retaining only the first block. This matters for providers that emit reasoning as multiple structured updates.
- Completed provider-supplied thought panels stay expanded in `/chat` and restored sessions; only an unfinished block keeps the live-thinking indicator.

## Council and PublisherStudio session durability

- 1-Wire/PublisherStudio Council runs are always persisted as LocalGPT `/chat` sessions, independent of the remote caller's previous `SaveToMemory` value.
- 1-Wire Council runs now register a live Council session in LocalGPT, including participant status and streamed output, so externally started teams/rounds use the same user-visible trace path as locally started Council work.
- Council markdown logging no longer waits for a normal successful completion before the first file exists. A run creates an early `CouncilLogs` checkpoint and atomically refreshes that same file on success or failure.
- Council log and session persistence are no longer canceled merely because the UI/transport request token was canceled after the Council had already produced useful state.
- Failed or partially completed Council runs are also persisted when session saving is enabled, retaining their partial steps and diagnostic outcome.

## Release policy

- LocalGPT: 2.8.6.
- LocalGPTWebviewWrapper: 2.8.6.
- LocalGPTInstallerConsole: 2.8.6.
- 1-Wire protocol: 2.1.1 (unchanged).
- Existing InteractiveServer boundaries are unchanged.
- No generated/compiled output is included in the source package.

