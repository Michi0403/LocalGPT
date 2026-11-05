# LocalGPT 2.1.10 — Scoped diagnostics and adaptive Ollama benchmark

- Excluded singleton registrations from `ServiceMethodLoggingDispatchProxy` decoration, preventing root-provider resolution of scoped dependencies.
- Registered the stateless `IRegexPatternService` through its correct singleton lifetime.
- Implemented the user-confirmed `localgpt.models.benchmark.autotune` DXFunction against the configured loopback Ollama API.
- Added bounded installed-model selection, deterministic and peer-authored tasks, hardware-aware profiles, five-percent improvement stopping, cancellation/timeouts, score reporting, and optional creation of a new model preset.
- Moved every new benchmark request/result/API data definition into `BusinessObjects`.
- Removed every `ConfigureAwait(true)` occurrence. Renderer-owned code now uses ordinary `await`; context-free service code continues with `ConfigureAwait(false)`.
- Updated the async-continuation policy so `ConfigureAwait(true)` has an unconditional maximum of zero.
- Raised the LocalGPT application and organic application advertisement to 2.1.10. The separately versioned Wire Protocol package remains unchanged.
