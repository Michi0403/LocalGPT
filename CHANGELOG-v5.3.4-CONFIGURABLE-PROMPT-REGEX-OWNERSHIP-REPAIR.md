# LocalGPT 5.3.4 — configurable prompt/regex ownership repair

## Why this release exists

5.3.3 added a baseline-free runtime-text ownership audit but intentionally left the reported migration inventory unresolved. That was not sufficient: the reported Council role-synthesis prompt was still hardcoded, other model-facing prompt bodies still lived in operational code, direct regex literals still bypassed the established regex ownership path, and the audit itself could fail opaquely on Windows console encodings before displaying its findings.

5.3.4 repairs the regression instead of merely reporting it.

## Model prompt ownership restored

- Model/system prompt bodies migrated in this maintenance pass resolve through the existing `ILocalGptRuntimePolicyDataService` / database-backed `SystemVariables` ownership path and its serializable `LocalGptRuntimeValue` BusinessObject keys.
- The shipped defaults live in `LocalGptRuntimePolicySeedDataService`, so an existing database receives missing defaults during normal database initialization while existing persisted values remain authoritative.
- `MultiModelCouncilService.BuildConfiguredRoleSynthesisPrompt` no longer constructs the reported raw prompt. It resolves `CouncilRoleResultSynthesisPromptTemplate` and replaces bounded placeholders with current run data.
- Council workflow steps now persist optional `RoleResultSynthesisPromptTemplate` and `RolePeerReviewPromptTemplate` overrides. The Council Teams editor exposes both fields; leaving them blank uses the database-backed global defaults.
- Provider/Council output-policy, upload-workspace, tool-continuation, readiness, human-collaboration, benchmarking, setup, and related model instructions migrated in the same ownership pass keep their prior behavior while moving authored policy text out of operational methods.

## Regex ownership restored

- Direct operational regex patterns found across Council/runtime formatting, repository/project maintenance, remote-control parsing, code generation, model benchmarking, Minecraft/project discovery, plugin support, translation/rendering, and related services now resolve through `LocalGptRuntimePattern` and the existing database-backed runtime regex store/compiler path.
- The final repository-tag tokenizer reuses the existing `NonAlphanumeric` database-backed regex rather than introducing a duplicate pattern.
- No replacement database or regex service was introduced.

## Ownership guard repaired

`build/audit_runtime_text_ownership.py` remains baseline-free for model-prompt and regex ownership, but now checks the boundary it is intended to protect:

- literal `ChatRole.System` bodies;
- orchestration-owned model prompt bodies; and
- direct regex pattern arguments/assignments outside approved seed/catalog owners.

Ordinary localized UI copy, operational status text, logger templates, URLs, markup, CSS classes, replacement strings, regex group names, and other implementation text remain governed by their existing localization/text-service/architecture rules rather than being incorrectly classified as configurable model policy.

The audit writes its report as UTF-8 and renders console output through an ASCII-safe representation. This prevents Windows PowerShell/Python legacy console encodings from raising a Unicode traceback while preserving complete report content.

## Regression validation

Source-only validation performed without `dotnet`, MSBuild, restore, publish, GitHub, or online repository access:

- runtime model-prompt/regex ownership audit: **pass, 0 findings**;
- same audit under a simulated single-byte `cp1252` Python console: **pass**;
- application architecture audit: **pass**;
- async-continuation audit: **pass**;
- async-only component/service architecture audit: **pass**;
- provider-qualified Council audit: **pass** after moving its policy-text assertions to the database seed owner;
- Razor maintenance audit: **pass**;
- service-resilience audit: **pass**;
- transient UI-state ownership audit: **pass**;
- CodeGeneration/DXFunction wiring audit: **pass**;
- DevExpress Blazor control audit: **pass**;
- cross-platform boundary audit: **pass**; and
- runtime enum/use/seed coverage check: every runtime value/pattern/collection used by the maintained source has a declared key and seed entry.

A .NET compile was deliberately not attempted. The supplied build log remains the authority for compiler/runtime validation on the development machine.
