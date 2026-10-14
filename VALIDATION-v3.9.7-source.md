# LocalGPT 3.9.7 source validation

## Scope

This validation is source/static only. No `dotnet build`, `dotnet publish`, Windows PowerShell release build, macOS signing/notarization, or GitHub source access was performed here. The user's native Windows/macOS/Linux build and packaged runtime are authoritative.

## Compiler-log-driven repair

The 3.9.6 Windows Debug build reached the C# compiler after all maintained pre-build guards passed and reported three errors:

- two `CS1673` errors in `BusinessObjects/ProviderModelModels.cs`, caused by LINQ lambdas inside the readonly `ProviderModelIdentity` struct accessing instance methods through implicit `this`;
- one `CS0136` error in `Services/InitialSetupAssistantService.cs`, caused by two `cpuName` declarations occupying overlapping C# local declaration spaces.

3.9.7 fixes those exact source constructs without changing provider-model reconciliation or hardware-selection semantics. The readonly struct remains a readonly struct; the predicates capture local copies of the struct instead. The configured-profile CPU value is now named `profileCpuName`, leaving the later locally detected `cpuName` declaration independent.

## Executed source checks

The following maintained audits were executed against the 3.9.7 working tree after the compiler-log repair:

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
- `build/audit_release_3_9_7.py` — passed after the validation file was finalized.

Direct ownership counts were also rechecked: 15 explicit `@rendermode InteractiveServer` boundaries and 8 `AddHostedService<T>` registrations remain unchanged.

## 3.9.7-specific source contracts

The release-specific audit verifies the compiler-log repair as well as the retained 3.9.6 feature contracts:

- all executable LocalGPT project versions and documentation metadata are 3.9.7 and obey the one-digit minor/patch-slot policy;
- `ProviderModelIdentity.ResolveEquivalentCandidate` does not let its LINQ lambdas capture implicit struct `this` for provider/model reconciliation predicates;
- `BuildHardwareListAsync` uses separate configured-profile and detected-local CPU variables so the reported `CS0136` declaration-space conflict cannot be reintroduced by this path;
- provider model inventory remains independent of CanIRun.ai, while CanIRun.ai remains optional hardware-aware model discovery;
- CPU/SoC, RAM/unified memory, GPU facts, and the separate CanIRun hardware catalog label remain represented behind the maintained cross-platform probe boundary;
- legacy provider/model reconciliation remains limited to a unique provider protocol + normalized host/port + concrete model match and does not collapse sizes/variants or guess another host;
- the 3.9.4 Windows PowerShell 5.1 documentation-cache repair and 3.9.5 Debug-versus-Release PDF contract remain present;
- release source fingerprints, packaged-runtime source-version fallback, macOS durable working-directory/logger behavior, and repository packaging hygiene remain present.

## Limitations

These checks do not prove compilation. A successful user-side Debug build is the required next gate; a later compiler/runtime log supersedes these source-only checks.
