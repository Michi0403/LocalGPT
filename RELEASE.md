# LocalGPT 2.8.6 reasoning/function trace and Council-log durability release

LocalGPT 2.8.6 restores and broadens user-visible AI trace continuity without reverting the current architecture. Native Ollama requests explicitly opt into provider-supplied thinking where supported, provider-exposed reasoning/tool metadata is projected into the normal transcript, automatic DX function calls/results become durable chat content, and Council runs keep these traces across local, team, round, and PublisherStudio/1-Wire entry points. Completed provider-supplied thought panels stay expanded in `/chat` and restored sessions instead of silently collapsing after answer text starts.

Council diagnostics are also hardened: a markdown log checkpoint is created early and atomically refreshed independently of request cancellation, while failed/partial runs can still be retained in chat memory. PublisherStudio-started Council work is intentionally treated as a normal LocalGPT `/chat` session.

This release exposes only reasoning/thinking information actually supplied by the configured model/provider. It does not invent or claim access to provider-internal reasoning that a provider does not return.

No GitHub access or .NET/MSBuild invocation was used to prepare this source release.

## Compatibility

- LocalGPT, LocalGPTWebviewWrapper and LocalGPTInstallerConsole are 2.8.6.
- 1-Wire protocol remains 2.1.1.
- Existing 19 InteractiveServer render-mode directives are unchanged.
- No database migration is required.
