# Validation — LocalGPT v0.1.4 compile-fix candidate

## Authoritative inputs

This release starts from `LocalGPT-v0.1.3-thinking-panels`. The v0.1.2 DXFunctions archive, the local 2026-07-25 archive, the current missing-feature report, the supplied chat log, and the older partly broken reports were reviewed as secondary inputs. Runtime databases, logs, binaries, generated build workspaces, private package material, and the obsolete `Extensions/PlainStatics` branch were not merged.

## v0.1.4 integration checks

The final source tree contains and wires the following contracts:

- scoped `IDxAiFunctionServiceClient` / `DxAiFunctionServiceClient` with operation IDs, one active scoped call, cancellation, and conversation/project/project-version/application-version context;
- `DxAiFunctionsController` routed through the typed client rather than directly through the registry;
- project and exact project-version selectors in the main Chat page;
- SQLite-backed assistant feedback with optional comments and reload support;
- feedback preservation keyed by message order, role, and content so ratings cannot silently move to a replaced response;
- EF migration `20260725150000_AddChatSessionControl` plus matching model snapshot fields and indexes;
- separate DI protocol profiles for Harmony/gpt-oss, DeepSeek, Gemma, Apple/OpenELM/MLX, `<think>` tags, and plain text;
- application, installer, and desktop-wrapper version metadata set to `0.1.4`;
- readable but agent-immutable governance files, CODEOWNERS, normalized SHA-256 manifest validation, CI enforcement, and optional owner-run read-only attributes.

## Previous static validation and compile-fix review

The source-only tree was reviewed without contacting Hugging Face, Ollama, or any local model runtime. The earlier structural checks did not constitute compilation and missed two baseline C# syntax errors. Those exact failures are corrected in this candidate.

- 702 maintained/package files were inventoried.
- 166 C# files were structurally scanned; this check is now explicitly classified as insufficient without Roslyn and a real build.
- 22 Razor files passed structural balance checks.
- 9 JSON files parsed successfully.
- 18 XML/project/property files parsed successfully.
- Workflow YAML parsed successfully in the validation environment.
- The protected-file manifest matched every normalized protected file.
- Required DI registrations, controller routing, migration/snapshot fields, feedback safety guard, project/version context, and version strings were found.
- No merge markers, forbidden runtime/private artifacts, `Extensions/PlainStatics`, forbidden `WebRequestMethods` static import, repository `AGENTS.override.md`, or local Claude settings override was packaged.
- Production static classes remain limited to the application/installer composition roots and the two reviewed extension classes.
- Maintained lines comply with the 600-character source guard after splitting one pre-existing architecture paragraph.

The earlier static validation command reported:

```text
files=702 csharp=166 razor=22 json=9 xml=18
ALL STATIC VALIDATION CHECKS PASSED
```

## Preserved v0.1.3 behavior

The streamed thinking/final-answer formatter, stable panel keys, per-message panel-state host, browser state helper, project collaboration domain, reviewed knowledge lifecycle, bounded artifact workspaces, and automatic-safe DXAIFunction gating remain present. The earlier browser regression result documented for v0.1.3 was not rerun in this environment; the source contracts that protect it remain in place.

## Limits of this review

This workspace does not contain the required .NET 10 SDK/compiler, the owner’s DevExpress private feed or license, Windows App SDK workloads, WebView2 runtime, MSIX/signing tooling, or the owner’s provider configuration. Therefore this review does not claim a licensed restore/build, application launch, production-database migration, WebView2 smoke test, publish, or signed installer.

The licensed owner-side Windows build is authoritative. This candidate is not claimed to be build-verified in this environment. A real `dotnet restore` and full Debug/Release build are mandatory. The repository now includes Roslyn syntax validation and fingerprint-gated packaging so future normal release ZIPs cannot be created from structural checks alone.

## Required owner-side checks

```powershell
./build/Invoke-RepositoryValidation.ps1
./build/Audit-Dependencies.ps1

dotnet package list ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln --include-transitive --vulnerable --format json

# Only after both builds succeed for the unchanged source tree:
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```

Then test migration on a copy of the existing SQLite database, chat project/version restore, feedback save/reload/clear, failed-save UI behavior, DXAIFunction cancellation, and explicit/automatic protocol selection for Harmony, DeepSeek, Gemma, Apple/OpenELM/MLX, `<think>`, and plain-text model output.


## Human collaboration debug candidate

The source gate now includes `Assert-HumanCollaboration.ps1`. Manual runtime validation must cover exact controller approval, changed-parameter rejection, decline guidance, live human contribution during a running council, later peer assessment, deferred sensitive DXAI execution on a later heartbeat, and the no-auto-restart behavior after a run has completed. See `DEBUG-NEXT-STEPS.md`.
