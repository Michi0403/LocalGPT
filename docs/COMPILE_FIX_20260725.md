# Compile fix — 2026-07-25

## Root compiler failures fixed

1. `CodeGenerationWorkflowService.cs`: the generated `.sln` template used a one-dollar interpolated raw string containing a literal double opening-brace run. It was replaced with explicit `StringBuilder` output, removing raw-string brace ambiguity.
2. `MultiModelCouncilService.cs`: an ordinary interpolated string contained a physical newline. It now uses `string.Concat(request.Prompt, Environment.NewLine, result.FinalAnswer)`.

`LocalGPTWebviewWrapper` diagnostics `CS0006` and `WMC1006` are downstream consequences when `LocalGPT.dll` is not emitted; they should disappear after the LocalGPT project builds successfully.

## Behavior-relevant warnings fixed

- The shortcut icon is optional, so `CreateWindowsUrlShortcut` now accepts `string?` rather than claiming a non-null contract.
- Installer cache-manifest `JsonSerializerOptions` are reused instead of allocated for every write.
- The `--port` range exception now identifies the actual `args` method parameter and includes the invalid value.

IDE collection-expression suggestions and the large set of logging-template analyzer messages were deliberately not mass-rewritten because they are not responsible for the failed build and a mechanical logging rewrite would create unnecessary regression risk.

## New prevention gates

- Roslyn syntax parsing for every maintained `.cs` file.
- Full Debug and Release solution validation with logs.
- A source fingerprint written only after successful builds.
- Verified packaging that rejects missing or stale build evidence.
- CI syntax parsing with the SDK pinned by `global.json`.
- Protected agent instructions covering generated strings, compiler truth, and package gating.

## Required local command

```powershell
./build/Invoke-RepositoryValidation.ps1
```

After it passes without source changes:

```powershell
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```
