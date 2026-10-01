# LocalGPT 5.2.4

LocalGPT 5.2.4 repairs the async-only maintenance architecture introduced in 5.2.3 without weakening the useful asynchronous ownership rules.

The 5.2.3 scanner treated ordinary synchronous .NET/Blazor contracts, pure helpers, atomic state, and synchronous-only disposal as architecture violations and also contained lexical false positives for domain `.Result` properties, `_ => await ...` lambdas, and `_ = await ...` result discards. The corrected gate now validates asynchronous boundaries rather than demanding fake asynchronous signatures everywhere.

Real async ownership violations remain build-breaking. Methods ending in `Async` must be awaitable, `async void` is rejected, asynchronous work may not be hidden in getters, renderer/awaitable workflows may not block on Tasks, blocking coordination remains forbidden, and direct fire-and-forget starts are rejected.

LocalGPT also removes all maintained direct discarded async starts found by the corrected rule. UI/event bridges, Council provider discovery, coalesced console render workers, documentation refresh, delayed Council-spool persistence, and Council autoplay now transfer intentional concurrency to the existing `ISupervisedTaskRunner`; existing cancellation tokens are retained where the worker already owned one.

The corrected async-boundary audit passes all 613 reviewed LocalGPT source files. The application-architecture, async-continuation, and Razor-maintenance source audits also pass. No .NET build was attempted in this environment.

See `CHANGELOG-v5.2.4-ASYNC-BOUNDARY-ARCHITECTURE-REPAIR.md` and `VALIDATION-v5.2.4-source.md`.
