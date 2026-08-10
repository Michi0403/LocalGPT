# LocalGPT 2.6.0 source validation

This package was reviewed using source/static validation only. No .NET restore, build, publish or runtime execution was performed.

## Passed static audits

- `build/audit_provider_qualified_council.py`: provider-qualified Council audit passed with the new role binding, workflow visibility, logical-round, classifier and Ollama tool-compatibility checks.
- `build/audit_application_architecture.py --product localgpt`: architecture policy passed.
- `build/audit_service_resilience.py --product localgpt`: service resilience passed with 1,734 reviewed service methods owning diagnostics/exception boundaries; 30 iterator methods and 3 direct Program/Startup methods remain intentionally skipped by that audit.
- `build/audit_async_continuations.py`: passed for 152 source files with 2,243 await tokens, 2,038 `ConfigureAwait(false)`, 30 reviewed renderer-affine `ConfigureAwait(true)`, 2 preconfigured awaitables, 171 reviewed await-using disposals and 2 configured async streams.
- `build/audit_documentation_onewire_contracts.py --product localgpt`: documentation/1-Wire contracts passed.
- Text-service ownership policy was reproduced against `build/text-service-ownership-baseline.json`: zero new direct component/controller `Regex`, `Replace`, `Split`, `string.Join`, or HTML-decode ownership violations.
- Localization catalogs parse as UTF-8 JSON with 1,497 matching English/German keys.

## Targeted source checks

- Team roles persist exact provider-qualified model keys.
- Team-bound model identities are applied before Council participant resolution.
- Bare legacy team model names are accepted only when they resolve uniquely; ambiguous same-name multi-host assignments fail closed.
- Missing configured workflow roles no longer escape to all selected models.
- `AssignedModelSingle` cannot substitute another model or host.
- Workflow transcript visibility filters both accumulated transcript and previous-step data.
- Logical Council rounds are independent of workflow step index.
- Human profile enabling now establishes trusted local-human ambient context.
- Frustration token `mad` no longer matches the substring in `made`.
- Ollama native-tool rejection is cached by endpoint + model for the current process.
- The neutral General Council and separate Organic Project Team seed are both present.

## Version policy

Validated application project versions are `2.6.0`; the wire protocol remains `2.1.1`. The rollover from `2.5.9` to `2.6.0` follows the maintained single-digit minor/patch-slot policy.
