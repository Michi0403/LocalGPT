# LocalGPT 2.6.5 source validation

This package was validated statically only. No `dotnet`, MSBuild, restore, build, publish, application launch, or GitHub access was performed.

## Maintained audits

- Async continuation audit: PASS — 152 source files, 2243 await tokens, 2038 `ConfigureAwait(false)`, 30 renderer-affine `ConfigureAwait(true)`, 2 preconfigured awaitables, 171 reviewed await-using disposals, 2 configured async streams.
- Application architecture audit: PASS.
- Service resilience audit: PASS — 1734 service methods with owned try/catch + diagnostics; 30 yield methods and 3 direct Program/Startup methods excluded by policy.
- Provider-qualified Council audit: PASS — 173 checks.
- Chat ASCII-console audit: PASS — 17 checks.
- Documentation / 1-Wire contract audit: PASS.
- Kawaii documentation layout audit: PASS.
- Text-service ownership guard emulation: PASS — 0 new component/controller findings relative to the maintained baseline.

## Localization and version integrity

- `en-US.json`: 1852 keys, 0 case-insensitive duplicate keys.
- `de-DE.json`: 1852 keys, 0 case-insensitive duplicate keys.
- EN/DE key sets: identical.
- First-run onboarding, provider-unavailable Council UI and touched Chat strings have maintained EN/DE entries.
- Application projects: 2.6.5.
- Wire protocol project: 2.1.1 (unchanged).
- `ICustomVersion` is now derived from the running LocalGPT assembly rather than a stale hard-coded `2.5.0` value.

## Targeted regression contracts

- Native Ollama and OpenAI-compatible `/v1` on the same host/port are no longer mutually exclusive in `ProviderModelRuntimeService`.
- Exact provider-qualified model keys remain provider + endpoint + model identities; same-name fallback remains forbidden.
- Saved provider-qualified keys can be parsed for configured/offline endpoint preflight without changing host or provider identity.
- Missing selected routes remain visible in Chat configuration and are rendered as red unavailable entries with explicit removal controls.
- Configured Ollama/OpenAI-compatible provider cards render unavailable state when the endpoint is currently unreachable.
- Chat does not emit the normal "AI Council started" banner for routes known to be truly removed/unconfigured.
- Configured but temporarily offline exact routes remain eligible for exact-route runtime preflight instead of being mistaken for a removed host.
- Council prompts explicitly prohibit raw JSON/work-order/tool metadata from replacing the human-visible answer unless JSON itself was requested.
- For coding/source requests, configured workflow final-output instructions require concrete visible source/code/file content before optional LocalGPT machine-readable metadata.
- First-run page strings are localized directly via stable keys rather than depending only on post-render DOM translation.

## Static format checks

- All four project XML files parsed successfully.
- Localization JSON parsed successfully.
- Source ZIP integrity is checked after packaging with `unzip -tq`.
