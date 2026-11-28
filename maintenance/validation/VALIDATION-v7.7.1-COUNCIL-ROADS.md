# LocalGPT 7.7.1 — Council roads / runtime policy validation

## Confirmed input diagnostics

The supplied Council transcript reports `OllamaContextBudgetExceeded;required=35341;allowed=32768;output=1024;max=262144` for qwen3.5:9b. The same row states Ollama was configured with `num_ctx=32768` despite advertising a 262144 model context; it does **not** establish a model capability limit of 32768. Another model advertises only 16384 context and legitimately rejects a larger prompt.

Source audit found the independent context/output hardcodes in participant qualification, missing planner routes, Chat route selection/UI limits, preset seeding, OneWire Council entrypoint, an old database seed migration and native Ollama request fallback.

## Source-only check on Windows / Linux / macOS with PowerShell

From the repository root:

```powershell
pwsh ./build/Assert-CouncilRoadRuntimePolicy.ps1
pwsh ./build/Invoke-RepositoryValidation.ps1
```

The new contract script runs within the existing `AssertLocalGptNativeArchitectureMaintenance` build target. Running the repository validation may invoke .NET on your host; **it was not run by the source editor**. Use the maintained project validation process and do not treat these static contracts as proof of runtime behavior.

## Host acceptance cases (not executed)

1. In Chat, configure a Council context window of 262144, output budget 4096, power 100%, and select a newly discovered, provider-qualified qwen3.5:9b. The visible automatically generated road and run lane should report 262144 context, **not 32768**. The Ollama request must still respect its actual advertised maximum.
2. Select a second newly discovered small Ollama model with an advertised context below 262144. Verify route creation does not invent a 32k limit or unconditionally send a context window the provider does not support. For oversized prompt content, expect the existing explicit budget error/recovery path instead of silent prompt truncation.
3. Change an unmodified auto road's Council global context from 32768 to 262144 and back, including a reload and preserved preparation configuration; confirm the automatic road follows new budget values. Change a manually edited route instead and verify its distinct maximum and hardware affinity remain unchanged.
4. Apply an existing benchmark/preset route with its own context or output maximum and increase the global Council ceiling. Verify the measured/preset road still limits that model; it must not be mistaken for a newly generated auto road.
5. Reload LocalGPT with an existing `DefaultCouncilContextTokens=262144` system-variable row; confirm the database initialization no longer changes it to 32768. Verify explicit `DefaultContextTokens` (UI preference) and `DefaultCouncilContextTokens` (seed fallback) remain independent.
6. Apply a built-in preset that existed before 7.7.1 and was never edited: verify its authored template values restore on seeding without replacing manually customized, archived or benchmark-saved presets.
7. Invoke screenreader OneWire Council and native Ollama chat without explicit token budgets; inspect that their persisted runtime default keys are used. Verify Configuration Steward's load-percent finding agrees with the current automatic-road configuration service.
8. On standalone Model Council, provider benchmark panel and runtime management panel, verify token editor maxima match the persisted general catalog limits and that saving provider defaults still passes the management service's validation.
9. Compile and run regular repository tests, validate Blazor session reload and run-scoped settings, and repeat the reported Qwen learning round. Watch for `OllamaContextBudgetExceeded` with effective `num_ctx` and route logs, not only superficial UI values.

## Not validated in this environment

- No PowerShell runtime (`pwsh`/`powershell`) installed here; contract script is supplied but **not executed**.
- No .NET SDK or compiler used; no build, package restore, unit test or integration test was attempted.
- No Ollama endpoints or provider hardware were contacted.
- No GitHub/network access was used.

The ZIP is a complete, updated source candidate, **not** a verified build or verified release artifact.
