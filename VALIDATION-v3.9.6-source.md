# LocalGPT 3.9.6 source validation

## Scope

This validation is source/static only. The user explicitly requested that this environment not run the LocalGPT .NET build or release pipeline. No `dotnet build`, `dotnet publish`, Windows PowerShell release build, macOS signing/notarization, or GitHub source access was performed here. Windows/macOS/Linux packaged runtime behavior remains subject to the user's native build/runtime testing.

## Executed source checks

The following maintained audits were executed against the 3.9.6 working tree after the provider-model, hardware, and persisted-binding changes:

- `build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `build/audit_cross_platform_boundaries.py` — passed: 22 checks; no platform leaks detected.
- `build/audit_async_continuations.py --source-root src/LocalGPT` — passed for 259 source files: 3045 await tokens, 2688 `ConfigureAwait(false)`, 135 renderer-affine `ConfigureAwait(true)`, 217 explicitly configured await-using disposals, and 5 configured async streams.
- `build/audit_codegen_dxfunction_wiring.py` — passed.
- `build/audit_configurable_behavior_policy.py --root .` — passed.
- `build/audit_provider_qualified_council.py --root .` — passed: 282 checks.
- `build/audit_provider_stream_repetition_policy.py` — passed.
- `build/audit_xround_wiring.py` — passed.
- `build/audit_council_sql_seed.py` — passed: 60 executable current-schema `INSERT OR IGNORE` rows with deterministic source hashes and second-run idempotency.
- `build/audit_service_resilience.py --root . --product localgpt` — passed: 2235 service methods own try/catch + diagnostics; 29 yield methods and 3 direct Program/Startup methods are intentionally skipped by that audit.
- `build/Assert-XmlDocumentationCoverage.py .` — passed: 10,390 direct C# declarations across 656 maintained source files and 789 direct Razor `@code` members across 45 component types.
- `build/audit_release_3_9_6.py` — release-specific source contract. This check is executed again after this validation file is written and before packaging.

## 3.9.6-specific source contracts

The release-specific audit verifies, among other retained and new invariants:

- all executable LocalGPT project versions and documentation metadata are 3.9.6 and obey the one-digit minor/patch-slot policy;
- the 3.9.4 Windows PowerShell 5.1 `Join-Path` compatibility repair and the 3.9.5 Debug-versus-Release PDF contract remain present;
- the release source-fingerprint contract remains present so same-version native/notarized artifacts cannot silently represent changed source;
- packaged runtime version reconciliation retains the assembly-version fallback and does not require a source-tree `.csproj`;
- the setup assistant keeps Ollama lifecycle controls, manual model installation, provider model inventory, provider-owned catalog search, and optional CanIRun.ai hardware-fit discovery independent from one another;
- CPU/SoC, RAM/unified memory, GPU facts, and the separate CanIRun hardware catalog label are represented without moving OS branching out of `IHardwarePlatformProbeService`;
- ordinary unmapped CanIRun recommendations do not use `InvalidDataException` as control flow;
- persisted provider/model reconciliation requires a unique provider protocol + normalized host/port + concrete model match and does not collapse model sizes/variants or guess another host;
- direct `@rendermode InteractiveServer` and `AddHostedService<T>` ownership counts remain unchanged at 15 and 8 respectively;
- repository-local build/cache artifacts are absent at final packaging time.

## Limitations

These checks do not prove compilation or runtime behavior. The authoritative next validation is the user's real Windows/macOS/Linux build and packaged `/install`/Council runtime test. Any resulting compiler/runtime log should be treated as stronger evidence than a source-only audit.
