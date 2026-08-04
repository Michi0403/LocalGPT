# Compiler validation and generated-code rules

This document defines the build truth and code-generation rules for LocalGPT maintenance. It is architecture guidance, not permission to run or publish software.

## Build truth

- A delimiter counter, regex scan, JSON/XML parse, IDE inspection, or visual review is not a C# compilation.
- `build/Assert-CSharpSyntax.ps1` must parse every maintained `.cs` file with the Roslyn parser from the SDK pinned by `global.json`.
- `build/Invoke-RepositoryValidation.ps1` must then restore and build the full solution in both Debug and Release.
- A normal release/source ZIP must be created only by `build/New-VerifiedSourcePackage.ps1`. It requires a successful build stamp whose source fingerprint still matches the exact files being packaged.
- When the SDK, licensed DevExpress feed, Windows workload, or another required dependency is unavailable, stop and label the result as unverified. Do not describe it as compiler-ready, build-verified, complete, or release-ready.
- `CS0006` and `WMC1006` in the WebView wrapper are usually downstream symptoms when the referenced `LocalGPT.dll` was not produced. Fix the first LocalGPT compiler error before changing wrapper references.

## Generated C# text

- Never place a physical newline inside an ordinary quoted or interpolated string such as `$"..."`. Use `Environment.NewLine`, `string.Concat`, `StringBuilder`, or a correctly tested raw string.
- Prefer `StringBuilder` for generated `.sln`, project, script, XML, JSON, and source templates that contain many quotes, braces, tabs, or interpolated values.
- For interpolated raw strings, the number of `$` characters defines the interpolation-brace count. Literal brace runs must be reviewed against that count. A template containing both literal braces and interpolation must be Roslyn-parsed before it is accepted.
- Do not repair a raw-string compiler error by adding braces blindly. Rewrite the template to an unambiguous builder when the output format is structured.
- Generated code must be treated as untrusted input until syntax parsing, path validation, review, and the configured build gate succeed.

## Warning triage

Fix warnings that can hide real behavior defects before release, including nullable contract mismatches, invalid exception parameter names, resource lifetime errors, serializer allocation in hot paths, ignored task/cancellation results, and potentially incorrect platform assumptions.

Style-only suggestions and logging-template performance messages may be deferred when they do not change behavior, but they must not be used to hide compiler errors or failed workflows.

## Required sequence for every coding session

1. Read `AGENTS.md`, this file, and the architecture documents relevant to the edited area.
2. Make the smallest coherent source change.
3. Review the complete diff, including unchanged baseline code touched by the workflow.
4. Run the protected-file, formatting, security, and Roslyn syntax guards.
5. Run restore and full Debug/Release solution builds.
6. Address the earliest root compiler error first; re-run until no errors remain.
7. Triage behavior-relevant warnings.
8. Run feature-specific tests and inspect package contents.
9. Package only through the verified packaging script.
10. Report exactly what ran and what did not run.
