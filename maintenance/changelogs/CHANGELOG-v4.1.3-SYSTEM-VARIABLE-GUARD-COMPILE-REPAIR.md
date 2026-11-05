# LocalGPT 4.1.3 — system-variable guard compile repair

LocalGPT 4.1.3 repairs the release-blocking `Assert-SystemVariableInitialization.ps1` failure reported by the macOS release build while preserving the 4.1.2 adaptive low-memory documentation policy and the 4.1.1 provider-management behavior.

## Changes

- The Ollama `/api/generate` and `/api/delete` protocol routes are now named immutable service constants rather than string literals embedded directly in `HttpRequestMessage` constructor expressions.
- This removes the two new constructor-literal findings that the system-variable initialization guard correctly treated as unreviewed runtime initialization.
- No system-variable baseline exception was added and the guard itself was not weakened.
- Ollama unload and permanent-delete behavior is unchanged; only ownership of the provider protocol route literals moved.
- The 4.1.2 memory-adaptive DocFX/PDF policy, 4.1.1 provider runtime/model workbench, 4.1.0 Matrix operator controls, signing protections, Pages hygiene, renderer-affinity policy, and installer behavior remain intact.

## Build failure addressed

The failing 4.1.2 release reached the RID-neutral LocalGPT build and stopped in `Directory.Build.targets` at `Assert-SystemVariableInitialization.ps1`. Static reproduction identified exactly two findings in `ProviderRuntimeManagementService.cs`, corresponding to the Ollama generate and delete `HttpRequestMessage` constructor lines. 4.1.3 removes those findings without suppressing the policy.
