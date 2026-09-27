# LocalGPT 5.1.2 — scoped registry lifetime repair

- Replaced the DXAIFunction registry's deferred `IServiceProvider` capture with cycle-safe same-scope handler initialization.
- Added a re-entrant initialization boundary so handler constructors may depend on `IDxAiFunctionRegistry` without recursively rebuilding the handler graph.
- Kept handler instances in the owning Blazor/request scope rather than moving them into an unrelated child scope that would lose ambient session/circuit state.
- Excluded `IDxAiFunctionRegistry` from generic `DispatchProxy` decoration while retaining the registry's explicit method-local structured logging.
- Extended service-architecture validation to reject future provider-backed lazy registry resolution and to require the maintained DI wiring.
