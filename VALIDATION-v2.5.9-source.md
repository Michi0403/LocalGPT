# LocalGPT 2.5.9 source validation

This package was validated without invoking `dotnet`, MSBuild, restore, build or publish.

## Static release checks executed

- `build/audit_provider_qualified_council.py --root .`: PASS — 131 checks.
- `build/audit_service_resilience.py --root . --product localgpt`: PASS — 1,727 service methods own try/catch + diagnostics; 30 yield methods and 3 direct boot methods skipped by maintained policy.
- `build/audit_application_architecture.py --root . --product localgpt`: PASS.
- `build/audit_async_continuations.py --source-root src/LocalGPT`: PASS — 152 source files, 2,240 await tokens, 2,035 `ConfigureAwait(false)`, 30 renderer-affine `ConfigureAwait(true)`, 2 preconfigured awaitables, 171 reviewed await-using disposals, 2 configured async streams.
- `build/audit_chat_ascii_console.py --root .`: PASS — 17 checks.
- `build/audit_documentation_onewire_contracts.py --root .`: PASS.
- `build/audit_kawaii_documentation_layout.py --root .`: PASS.
- Localization catalog key parity/JSON validation: PASS — 1,497 strings in both `en-US` and `de-DE`.
- Text-service ownership PowerShell policy was reproduced against `text-service-ownership-baseline.json`: PASS — 0 new direct component/controller string/regex operations.
- Additive Ollama registry scenario mirror: PASS — adding localhost preserves remote primary; explicit promotion swaps primary; explicit removal removes only the requested endpoint.

## Regression contract added

`audit_provider_qualified_council.py` now requires all of the following:

- Install edits a detached provider draft.
- Durable save delegates provider-registry merge semantics to `IAiProviderConfigurationRegistryService`.
- Direct aliasing of `current.OllamaCore` / `current.OllamaCores` back into the component is forbidden.
- Explicit Ollama removals and primary promotion are tracked independently.
- Persisted Ollama hosts are retained unless explicitly removed.
- Existing primary endpoint wins a normal save; a new endpoint cannot silently replace it.

## Version policy

Validated project versions are `2.5.9`; the wire protocol remains `2.1.1`. This respects the maintained single-digit second/third version slot policy.
