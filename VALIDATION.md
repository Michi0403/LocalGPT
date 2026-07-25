# Validation — v0.1.2 DXAIFunction and reviewed-generation wiring

## Authoritative input

This pass builds on the sanitized v0.1.1 source and the service-boundary refactor. It retains the owner-updated package references and does not include or modify a private runtime database.

## Confirmed seed continuity

The deterministic database catalog still includes the original response-protocol regex records:

- `HarmonyFinal`;
- `HarmonyThinking`;
- `ThinkTag`;
- `SafeKnowledgeFile`.

The prompt-policy and system-variable feeds remain present. Repository knowledge still uses an explicit reviewed allowlist rather than a wildcard. Seed version 5 adds `docs/DXAI_FUNCTIONS_AND_CHANGE_REVIEWS.md` without removing the earlier approved architecture, security, peaceful-use, or collaboration documents.

## DXAIFunction and generation checks

- DI registers every concrete `IDxAiFunctionHandler` and publishes descriptors through `IDxAiFunctionRegistry`.
- Parameter schemas live with handler descriptors rather than in a function-name switch inside the Ollama client.
- Native Ollama tool calls are limited to handlers that are read-only, direct-invocation capable, explicitly automatic-safe, and confirmation-free.
- Review creation, rejection, source generation, and builds are not available to automatic tool calling.
- Council consensus may emit a bounded `<localgpt-change-review>` JSON proposal with exact files, CodeDOM types, and outputs.
- The review payload is persisted with project/topic/council links and a SHA-256 hash before files are written.
- One-use source generation requires a fresh decision and the exact review hash.
- A build requires a separate current confirmation and runs only through the bounded artifact-build executor.
- Generated C# source is included in reviewed project outputs; reviewed `.csx` and `.js` source is reused for script/module outputs.
- Paths are relative, workspace-contained, Windows-portable, and reject traversal, invalid characters, and reserved device names.
- Generated scripts, DLLs, executables, and addons are never executed or loaded automatically.
- New and modified services log operation IDs, safe identifiers, counts, and status while omitting prompts, generated source, secrets, private reasoning, and full request bodies.

## Structural validation completed in the cloud workspace

- 153 C# files passed delimiter/string/comment structure checks.
- 22 Razor files passed structural balance checks.
- Ten dynamic DXAIFunction parameter schemas parsed as JSON objects.
- Project/property XML and repository JSON parsed successfully.
- Production static classes remain limited to `Program`, `StringExtensions`, and `ObservableCollectionExtensions`; generated regex methods, constants, and security checks remain static members where appropriate.
- No active `CouncilChatStaticsGeneral`, `Extensions/PlainStatics`, or `using static System.Net.WebRequestMethods` reference remains.
- The explicit seed catalog contains the four protocol/safety regexes and repository knowledge seed version 5.
- Line-length, merge-marker, generated-artifact, path-boundary, and forbidden-static checks passed.

## Limits of this review

The cloud workspace does not contain the required .NET 10 SDK/compiler, the owner’s DevExpress feed or license, Windows App SDK workloads, WebView2 runtime, MSIX/signing tooling, or the owner’s localhost/provider configuration. Therefore this review does not claim that the modified solution restores private packages, compiles, launches the wrapper, migrates a production database, builds every generated proposal, publishes, or signs successfully.

The licensed owner-side Windows build remains authoritative.

## Required owner-side checks

```powershell
./build/Assert-SourceFormatting.ps1
./build/Assert-SecurityPolicy.ps1
./build/Audit-Dependencies.ps1

dotnet restore ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln
dotnet package list ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln --include-transitive --vulnerable --format json
dotnet build ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln -c Debug
dotnet build ./LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.sln -c Release
```

Review every compiler warning rather than suppressing it globally. Prioritize nullability, disposal/lifetime, async/cancellation, unsafe path handling, EF migration drift, package advisories, tool-call serialization, generated project compilation, streaming, and persistence.
